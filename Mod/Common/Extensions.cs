using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using XRL;
using XRL.Collections;
using XRL.World;
using XRL.World.Text;

namespace UD_Missile_Chooser.Mod
{
    public static class Extensions
    {
        public static IEnumerable<T> IteratorSafe<T>(this IEnumerable<T> Source)
            => Source ?? Enumerable.Empty<T>()
            ;

        public static char GetNextHotKey(
            this IEnumerable<char> Source,
            IEnumerable<char> Excluding = null,
            char StartAt = 'a',
            char FinishAt = 'z'
            )
        {
            char lastHotkey = Source.LastOrDefault(c => Excluding?.Contains(c) is not true);

            if (lastHotkey == default)
                return StartAt;

            if (lastHotkey != ' ')
            {
                if (lastHotkey == '\0'
                    || lastHotkey == default)
                    lastHotkey = StartAt;

                while (Source.Contains(lastHotkey)
                    && lastHotkey <= FinishAt)
                    lastHotkey++;

                if (lastHotkey > FinishAt)
                    lastHotkey = ' ';
            }
            return lastHotkey;
        }

        public static IEnumerable<string> FramesToStrings(this StackTrace StackTrace, int? Count = null, int SkipLines = 0)
        {
            StackTrace ??= new(SkipLines + 1);
            var frames = StackTrace.GetFrames();
            int count = frames?.Length ?? 0;
            count = Math.Min(Count ?? count, count);
            for (int i = 0; i < count; i++)
                if (frames[i] is StackFrame frame)
                    yield return frame.ToString();
        }

        public static string FramesToString(this StackTrace StackTrace, int? Count = null, int SkipLines = 0, string TextLineBefore = null)
            => StackTrace.FramesToStrings(Count, SkipLines + 1)
                .Aggregate(
                    seed: TextLineBefore,
                    func: Utils.NewLineDelimitedAggregator)
            ;

        public static IEnumerable<T> Loggregate<T>(
            this IEnumerable<T> Source,
            Func<T, string> Proc = null,
            string Empty = null,
            Func<string, string> PostProc = null
            )
            => Utils.Loggregate(
                Source: Source,
                Proc: Proc,
                Empty: Empty,
                PostProc: PostProc)
            ;

        public static bool IsPooled(this GameObject Object)
            => Object != null
            && (Object.Flags & GameObject.FLAG_POOLED) != 0
            ;

        public static bool None<TSource>(this IEnumerable<TSource> Source, Func<TSource, bool> predicate)
            => !Source.Any(predicate)
            ;

        public static void PerformActionRecursively(this GameObject Object, Action<GameObject, int> Action, int Depth = 0)
        {
            Action.Invoke(Object, Depth);

            foreach (var inventoryObject in Object.GetInventoryAndEquipmentAndDefaultEquipment().IteratorSafe())
                inventoryObject.PerformActionRecursively(Action, Depth + 1);

            foreach (var installedCybernetic in Object.GetInstalledCybernetics().IteratorSafe())
                installedCybernetic.PerformActionRecursively(Action, Depth + 1);

            foreach (var contentsObject in Object.GetContents().IteratorSafe())
                contentsObject.PerformActionRecursively(Action, Depth + 1);
        }

        public static IEnumerable<GameObject> GetObjectsRecursively(
            this GameObject Object,
            Predicate<GameObject> Where = null,
            int Depth = 0,
            int? MaxDepth = null
            )
        {
            if (MaxDepth.HasValue
                && MaxDepth.GetValueOrDefault() > Depth)
                yield break;

            if (Where?.Invoke(Object) is not false)
                yield return Object;

            foreach (var inventoryObject in Object.GetInventoryAndEquipmentAndDefaultEquipment().IteratorSafe())
                foreach (var recursiveObject in inventoryObject.GetObjectsRecursively(Where, Depth + 1, MaxDepth))
                    if (Where?.Invoke(recursiveObject) is not false)
                        yield return recursiveObject;

            foreach (var installedCybernetic in Object.GetInstalledCybernetics().IteratorSafe())
                foreach (var recursiveObject in installedCybernetic.GetObjectsRecursively(Where, Depth + 1, MaxDepth))
                    if (Where?.Invoke(recursiveObject) is not false)
                        yield return recursiveObject;

            foreach (var contentsObject in Object.GetContents().IteratorSafe())
                foreach (var recursiveObject in contentsObject.GetObjectsRecursively(Where, Depth + 1, MaxDepth))
                    if (Where?.Invoke(recursiveObject) is not false)
                        yield return recursiveObject;
        }

        public static void PerformActionRecursivelyInRandomOrder(
            this GameObject Object,
            Action<GameObject> Action,
            Predicate<GameObject> Where = null,
            int Depth = 0,
            Random Rnd = null
            )
        {
            using var objectsList = ScopeDisposedList<GameObject>.GetFromPoolFilledWith(Object.GetObjectsRecursively(Where, Depth));
            objectsList.ShuffleInPlace(Rnd);
            foreach (var randomObject in objectsList)
                Action.Invoke(randomObject);
        }

        public static void PerformActionRecursively(this GameObject Object, Action<GameObject> Action, int Depth = 0)
            => Object.PerformActionRecursively(
                Action: delegate (GameObject go, int depth)
                {
                    Action.Invoke(go);
                },
                Depth: Depth)
            ;

        public static IEnumerable<T> PerformFunctionRecursively<T>(
            this GameObject Object,
            Func<GameObject, int, T> Func,
            int Depth = 0
            )
        {
            using var result = ScopeDisposedList<T>.GetFromPool();
            result.Add(Func.Invoke(Object, Depth));

            int newDepth = Depth + 1;
            var inventoryObjects = Object.GetInventoryAndEquipmentAndDefaultEquipment().IteratorSafe();
            foreach (var inventoryObject in inventoryObjects)
                foreach (var output in inventoryObject.PerformFunctionRecursively(Func, newDepth).IteratorSafe())
                    result.Add(output);

            var installedCybernetics = Object.GetInstalledCybernetics().IteratorSafe();
            foreach (var installedCybernetic in installedCybernetics)
                foreach (var output in installedCybernetic.PerformFunctionRecursively(Func, newDepth).IteratorSafe())
                    result.Add(output);

            var contentsObjects = Object.GetContents().IteratorSafe();
            foreach (var contentsObject in contentsObjects)
                foreach (var output in contentsObject.PerformFunctionRecursively(Func, newDepth).IteratorSafe())
                    result.Add(output);

            while (!result.IsNullOrEmpty()
                && result.TakeAt(0) is T output)
                yield return output;
        }

        public static string Colored(this string Text, string Color)
            => Color != null
            ? Text?.WithColor(Color)
            : Text
            ;

        public static void RemoveAll<T>(this ScopeDisposedList<T> Source, Predicate<T> Where)
        {
            if (Source.IsNullOrEmpty())
                return;

            for (int i = Source.Count - 1; i >= 0; i--)
                if (Where?.Invoke(Source[i]) is true)
                    Source.RemoveAt(i);
        }

        public static void RemoveLast<T>(this ScopeDisposedList<T> Source)
        {
            if (Source.IsNullOrEmpty())
                return;

            Source.RemoveAt(Source.Count - 1);
        }

        public static bool IsEmptyOrDefault(this Guid Guid)
            => Guid == default
            || Guid == Guid.Empty
            ;

        public static IEnumerable<GameObjectBlueprint> SafelyGetBlueprintsInheritingFrom(
            this GameObjectFactory Factory,
            string Name,
            bool ExcludeBase = true,
            bool IncludeSelf = false
            )
        {
            foreach (GameObjectBlueprint blueprint in Factory.BlueprintList.IteratorSafe())
                if (blueprint.InheritsFromSafe(Name, IncludeSelf)
                    && (!ExcludeBase
                        || !blueprint.IsBaseBlueprint()))
                    yield return blueprint;
        }

        public static List<string> InheritanceRoots => new()
        {
            nameof(Object),
            "SultanMuralController",
        };

        public static bool InheritsFromSafe(
            this GameObjectBlueprint GameObjectBlueprint,
            string What,
            bool IncludeSelf = true
            )
        {
            if (IncludeSelf
                && GameObjectBlueprint?.Name == What)
                return true;

            string parentBlueprint = GameObjectBlueprint.Inherits;
            while (!parentBlueprint.IsNullOrEmpty())
            {
                if (parentBlueprint == What)
                    return true;

                string inherits = parentBlueprint;
                parentBlueprint = GameObjectFactory.Factory?.GetBlueprintIfExists(parentBlueprint)?.Inherits;
                if (parentBlueprint.IsNullOrEmpty()
                    && !InheritanceRoots.Contains(inherits))
                {
                    Utils.WarnOnce($"{nameof(Extensions)}.{nameof(InheritsFromSafe)}(\"{What}\"):" +
                        $" bluprint ancestor \"{inherits}\" does not exist in blueprint list." +
                        $" The first mention of this blueprint in this log should reveal the mod with this inheritance issue.");
                }
            }
            return false;
        }

        public static TextBuilder AppendRule(this TextBuilder TB, object Value)
            => Value != null
            ? TB.AppendColored("rules", Value.ToString())
            : TB
            ;

        public static TextBuilder AppendQuote(this TextBuilder TB, object Value)
            => TB.Append("\"").Append(Value).Append("\"")
            ;

        public static TextBuilder AppendBullet(
            this TextBuilder TB,
            string Color = null,
            string Bullet = Utils.BULLET
            )
        {
            if (Color.IsNullOrEmpty())
                TB.Append(Bullet);
            else
                TB.AppendColored(Color, Bullet);

            return TB.Append(" ");
        }

        public static TextBuilder AppendBulletLine(
            this TextBuilder TB,
            string Color = null,
            string Bullet = Utils.BULLET
            )
            => TB.AppendLine().AppendBullet(Color, Bullet)
            ;


        public static string ToLiteral(this string String, bool Quotes = false)
        {
            if (String.IsNullOrEmpty())
                return null;

            string output = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(String, false);

            if (Quotes)
                output = $"\"{output}\"";

            return output;
        }

        public static TAccumulate Aggregate<TAccumulate>(
            this int Number,
            TAccumulate seed,
            Func<TAccumulate, int, TAccumulate> func
            )
        {
            for (int i = 0; i < Number; i++)
                seed = func(seed, i);

            return seed;
        }

        public static string ThisManyTimes(this string @string, int Times = 1)
            => Times.Aggregate("", (a, n) => a + @string)
            ;

        public static string ThisManyTimes(this char @char, int Times = 1)
            => @char.ToString().ThisManyTimes(Times)
            ;

        public static string CallChain(this string String, params string[] Calls)
            => Calls.Aggregate(String, (a, n) => a + "." + n)
            ;

        public static string CallChain(this Type Type, params string[] Calls)
            => Type.Name.CallChain(Calls)
            ;

        public static string Indent(this int Amount, int Factor = 2, int MaxIndent = 12, bool NBSP = false)
        {
            if (!NBSP)
                return Amount > 0
                    ? " ".ThisManyTimes(Math.Min(Amount * Math.Max(1, Factor), MaxIndent * Factor))
                    : null
                    ;
            else
                return Amount > 0
                    ? $"=ud_nbsp:{Math.Min(Amount * Math.Max(1, Factor), MaxIndent * Factor)}=".StartReplace().ToString()
                    : null
                    ;
        }

        public static TextBuilder AppendIndent(this TextBuilder SB, int Amount = 0, int Factor = 2, int MaxIndent = 12, bool AsNBSP = false)
            => SB.Append(Amount.Indent(Factor, MaxIndent, AsNBSP))
            ;

        public static TextBuilder AppendColored(this TextBuilder TB, string color, string text)
        {
            return TB.Append("{{").Append(color).Append("|")
                .Append(text)
                .Append("}}");
        }

        #region Transpilation

        public static bool IsEndOfSection(this OpCode OpCode)
            => OpCode.ToString() is not string opCodeString
            || opCodeString.StartsWith("pop")
            || opCodeString.StartsWith("br")
            || opCodeString.StartsWith("be")
            || opCodeString.StartsWith("bg")
            || opCodeString.StartsWith("bl")
            || opCodeString.StartsWith("leave")
            || opCodeString.StartsWith("ret")
            || opCodeString.StartsWith("st")
            || opCodeString.StartsWith("throw")
            ;

        public static LocalBuilder GetLocalBuilderAtIndex(this MethodBase MethodBase, int Index)
            => MethodBase.GetMethodBody().LocalVariables[Index] as LocalBuilder
            ;

        public static CodeInstruction Vomit(
            this CodeInstruction Instruction,
            int Pos,
            int PosPadding,
            Dictionary<Label, int> LabelInstructions = null,
            bool HaveILGen = false,
            bool IncludeEnd = false,
            bool Do = false
            )
        {
            if (!Do)
                return Instruction;

            string operandString = Instruction?.operand?.VomitOperand(PosPadding, LabelInstructions, HaveILGen, Do);
            string labelString = $"[{Pos.ToString().PadLeft(PosPadding, '0')}]";
            if (HaveILGen)
                labelString = $"IL_{Pos:X4}:";

            Utils.Log($"{labelString} {Instruction.opcode,-10} {operandString}");
            if (IncludeEnd
                && Instruction.opcode.IsEndOfSection())
                Utils.Log("");

            return Instruction;
        }

        public static CodeMatch Vomit(
            this CodeMatch CodeMatch,
            int Pos,
            int PosPadding,
            Dictionary<Label, int> LabelInstructions = null,
            bool HaveILGen = false,
            bool IncludeEnd = false,
            int Indent = 0,
            bool Do = false
            )
        {
            if (!Do)
                return CodeMatch;

            string operandString = CodeMatch?.operand?.VomitOperand(PosPadding, LabelInstructions, HaveILGen, Do);
            string labelString = $"[{Pos.ToString().PadLeft(PosPadding, '0')}]";
            if (HaveILGen)
                labelString = $"IL_{Pos:X4}:";

            Utils.Log($"{Indent.Indent()}{labelString} {CodeMatch.opcode,-10} {operandString}");
            if (IncludeEnd
                && CodeMatch.opcode.IsEndOfSection())
                Utils.Log("");

            return CodeMatch;
        }

        public static CodeMatch[] Vomit(
            this CodeMatch[] CodeMatchs,
            string Context = null,
            string EndContext = null,
            Dictionary<Label, int> LabelInstructions = null,
            bool HaveILGen = false,
            bool IncludeEnd = false,
            bool Do = false
            )
        {
            if (!Do)
                return CodeMatchs;

            int num = 0;
            int posPadding = Math.Max(4, (CodeMatchs.Length + 1).ToString().Length);
            if (!Context.IsNullOrEmpty())
                Utils.Log(Context);

            for (int i = 0; i < CodeMatchs.Length; i++)
                CodeMatchs[i].Vomit(
                    IncludeEnd: num < CodeMatchs.Length - 1 && IncludeEnd,
                    Pos: num++,
                    PosPadding: posPadding,
                    LabelInstructions: LabelInstructions,
                    HaveILGen: HaveILGen,
                    Do: Do);

            if (!EndContext.IsNullOrEmpty())
                Utils.Log(EndContext);

            return CodeMatchs;
        }

        public static CodeInstruction[] Vomit(
            this CodeInstruction[] CodeInstructions,
            string Context = null,
            string EndContext = null,
            Dictionary<Label, int> LabelInstructions = null,
            bool HaveILGen = false,
            bool IncludeEnd = false,
            bool Do = false
            )
        {
            if (!Do)
                return CodeInstructions;

            int num = 0;
            int posPadding = Math.Max(4, (CodeInstructions.Length + 1).ToString().Length);
            if (!Context.IsNullOrEmpty())
                Utils.Log(Context);

            for (int i = 0; i < CodeInstructions.Length; i++)
                CodeInstructions[i].Vomit(
                    IncludeEnd: num < CodeInstructions.Length - 1 && IncludeEnd,
                    Pos: num++,
                    PosPadding: posPadding,
                    LabelInstructions: LabelInstructions,
                    HaveILGen: HaveILGen,
                    Do: Do);

            if (!EndContext.IsNullOrEmpty())
                Utils.Log(EndContext);

            return CodeInstructions;
        }

        public static string VomitOperand(this object Operand, int PosPadding, Dictionary<Label, int> LabelInstructions = null, bool HaveILGen = false, bool Do = false)
        {
            if (!Do)
                return null;

            string result = Operand?.ToString();
            if (Operand?.GetType() == typeof(string))
                result = Operand?.ToString()?.ToLiteral(Quotes: true);
            else
            if (Operand is MethodInfo methodOp)
                result = $"{(methodOp.ReturnType.IsValueType ? "null" : "class ")}{methodOp.ReturnType} {methodOp.DeclaringType}:{methodOp.Name}({methodOp.GetParameters().Select(p => $"{(p.ParameterType.IsValueType ? "null" : "class ")}{p.ParameterType}").Aggregate((string)null, Utils.CommaSpaceDelimitedAggregator)})";
            else
            if (Operand is FieldInfo fieldOp)
                result = $"{(fieldOp.FieldType.IsValueType ? "null" : "class ")}{fieldOp.FieldType} {fieldOp.DeclaringType}:{fieldOp.Name}";
            else
            if (Operand is PropertyInfo propertyOp)
                result = $"{(propertyOp.PropertyType.IsValueType ? "null" : "class ")}{propertyOp.PropertyType} {propertyOp.DeclaringType}:{propertyOp.Name}";
            else
            if (Operand is Label key)
            {
                string text = "????";
                if (!LabelInstructions.IsNullOrEmpty()
                    && LabelInstructions.ContainsKey(key))
                {
                    text = LabelInstructions[key].ToString().PadLeft(PosPadding, '0');
                    if (HaveILGen)
                        text = $"IL_{LabelInstructions[key]:X4}";
                }

                result = "[" + text + "]";
            }

            return result;
        }

        public static CodeMatcher Vomit(
            this CodeMatcher CodeMatcher,
            ILGenerator Generator,
            bool Do = false,
            int? From = null,
            int? To = null
            )
        {
            if (Do)
            {
                bool haveGenerator = false; // Generator != null;
                var positionsByLabel = new Dictionary<Label, int>();
                int pos = CodeMatcher.Pos;

                CodeMatcher.Start();
                do
                {
                    var instruction = CodeMatcher.Instruction;

                    if (instruction.labels.IsNullOrEmpty())
                        continue;

                    foreach (var label in instruction.labels)
                        positionsByLabel[label] = CodeMatcher.Pos;
                }
                while (CodeMatcher.Advance(1).IsValid);

                int posPadding = Math.Max(4, (CodeMatcher.Instructions().Count + 1).ToString().Length);
                int from = From ?? 0;
                int to = To ?? CodeMatcher.Length - 1;
                CodeMatcher.Start();
                do
                {
                    if (CodeMatcher.Pos < from)
                        continue;
                    if (CodeMatcher.Pos > to)
                        break;

                    CodeMatcher.Instruction?.Vomit(CodeMatcher.Pos, posPadding, positionsByLabel, haveGenerator, IncludeEnd: true, Do);
                }
                while (CodeMatcher.Advance(1).IsValid);

                CodeMatcher.Start().Advance(pos);
            }

            return CodeMatcher;
        }

        public static CodeMatcher Vomit(
            this CodeMatcher CodeMatcher,
            bool Do,
            int? From,
            int? To
            )
            => CodeMatcher.Vomit(
                Generator: null,
                Do: Do,
                From: From,
                To: To)
            ;

        public static CodeMatcher Vomit(
            this CodeMatcher CodeMatcher,
            ILGenerator Generator,
            bool Do,
            int PosMargin
            )
            => CodeMatcher.Vomit(
                Generator: Generator,
                Do: Do,
                From: PosMargin >= 0 ? Math.Max(0, (CodeMatcher?.Pos ?? 0) - PosMargin) : null,
                To: PosMargin >= 0 ? Math.Min((CodeMatcher?.Pos ?? 0) + PosMargin, (CodeMatcher?.Length ?? 1) - 1) : null)
            ;

        public static CodeMatcher Vomit(
            this CodeMatcher CodeMatcher,
            bool Do = false,
            int PosMargin = -1
            )
            => CodeMatcher.Vomit(
                Generator: null,
                Do: Do,
                PosMargin: PosMargin)
            ;

        public static CodeMatcher Vomit(
            this CodeMatcher CodeMatcher,
            bool Do = false
            )
            => CodeMatcher.Vomit(
                Generator: null,
                Do: Do,
                From: null,
                To: null)
            ;

        public static IEnumerable<CodeInstruction> Vomit(this IEnumerable<CodeInstruction> Instructions, bool Do = false)
            => new CodeMatcher(Instructions).Vomit(Do).InstructionEnumeration()
            ;

        #endregion
    }
}
