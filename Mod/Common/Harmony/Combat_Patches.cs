using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;

using XRL.World;
using XRL.World.Parts;

using UD_Missile_Chooser.Mod.Events;

namespace UD_Missile_Chooser.Mod.Harmony
{
    [HarmonyPatch(typeof(Combat))]
    public static class Combat_Patches
    {
        public static bool Success { get; private set; }

        [HarmonyPatch(
            declaringType: typeof(Combat),
            methodName: nameof(Combat.FireMissileWeapon))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FireMissileWeapon_FireEventForSingleMissile_Transpiler(
            IEnumerable<CodeInstruction> Instructions,
            ILGenerator Generator,
            MethodBase OriginalMethod
            )
        {
            string patchMethodName = $"{nameof(Combat_Patches)}.{nameof(Combat.FireMissileWeapon)}";

            CodeMatcher codeMatcher = new(Instructions, Generator);

            bool doVomit = false;
            int metricsCheckSteps = 0;

            if (doVomit)
                Utils.Info($"{patchMethodName}, untouched");
            codeMatcher.Vomit(Generator, doVomit);

            // if (list2.Count > 1 && !CanFireAllMissileWeaponsEvent.Check(Attacker, list))
            //
            //      IL_01f3: ldloc.s 4
            //      IL_01f5: callvirt instance int32 class [mscorlib] System.Collections.Generic.List`1<class XRL.World.Parts.MissileWeapon>::get_Count()
            //      IL_01fa: ldc.i4.1
            //      IL_01fb: ble.s IL_023b
            // 
            //      IL_01fd: ldloc.0
            //      IL_01fe: ldfld class XRL.World.GameObject XRL.World.Parts.Combat/'<>c__DisplayClass17_0'::Attacker
            //      IL_0203: ldloc.3
            // 
            //      IL_0204: call bool XRL.World.CanFireAllMissileWeaponsEvent::Check(class XRL.World.GameObject, class [mscorlib] System.Collections.Generic.List`1<class XRL.World.GameObject>)
            //      IL_0209: brtrue.s IL_023b

            var local_at_4 = OriginalMethod.GetLocalBuilderAtIndex(4);

            var listMissileWeapon_Count = AccessTools.PropertyGetter(typeof(List<MissileWeapon>), nameof(List<MissileWeapon>.Count));
            var combat_DisplayClass_Attacker = AccessTools.Field("XRL.World.Parts.Combat+<>c__DisplayClass17_0:Attacker");

            var canFireAllMissileWeaponsEvent_Check = AccessTools.Method(
                type: typeof(CanFireAllMissileWeaponsEvent),
                name: nameof(CanFireAllMissileWeaponsEvent.Check),
                parameters: new Type[] { typeof(GameObject), typeof(List<GameObject>) });

            var match_CountBle1_AndNot_CanFireAllMissileEvent = new CodeMatch[9]
            {
                new(OpCodes.Ldloc_S, local_at_4),
                new(OpCodes.Callvirt, listMissileWeapon_Count),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ble_S),

                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldfld, combat_DisplayClass_Attacker),
                new(OpCodes.Ldloc_3),

                new(OpCodes.Call, canFireAllMissileWeaponsEvent_Check),
                new(OpCodes.Brtrue_S),
            };

            if (codeMatcher.Start().MatchEndForward(match_CountBle1_AndNot_CanFireAllMissileEvent).IsInvalid)
            {
                Success = false;
                Utils.LogTranspilationError(
                    PatchMethodName: patchMethodName,
                    Pos: codeMatcher.Pos,
                    MetricsCheckSteps: metricsCheckSteps,
                    CodeMatchMethod: nameof(CodeMatcher.MatchStartForward),
                    CodeMatchesName: nameof(match_CountBle1_AndNot_CanFireAllMissileEvent),
                    CodeMatches: match_CountBle1_AndNot_CanFireAllMissileEvent,
                    Indent: 1);
                codeMatcher.Vomit(Generator, doVomit);
                return Instructions;
            }
            metricsCheckSteps++;

            int removeSnippetStart = codeMatcher.Pos;

            // if (Part.LastFired != null)
            //
            //      IL_020b: ldloc.2
            //      IL_020c: ldfld class XRL.World.Parts.MissileWeapon XRL.World.Parts.Combat::LastFired
            //      IL_0211: brfalse.s IL_0221
            //
            // list2.Remove(Part.LastFired);
            //
            //      IL_0213: ldloc.s 4
            //      IL_0215: ldloc.2
            //      IL_0216: ldfld class XRL.World.Parts.MissileWeapon XRL.World.Parts.Combat::LastFired
            //      IL_021b: callvirt instance bool class [mscorlib] System.Collections.Generic.List`1<class XRL.World.Parts.MissileWeapon>::Remove(!0)
            //      IL_0220: pop

            var combat_LastFired = AccessTools.Field(typeof(Combat), nameof(Combat.LastFired));

            var match_PartLastFired_Not_Null = new CodeMatch[3]
            {
                new(OpCodes.Ldloc_2),
                new(OpCodes.Ldfld, combat_LastFired),
                new(OpCodes.Brfalse_S),
            };

            var listMissileWeapon_Remove = AccessTools.Method(
                type: typeof(List<MissileWeapon>),
                name: nameof(List<MissileWeapon>.Remove),
                parameters: new Type[] { typeof(MissileWeapon) });

            var match_List2_Remove_LastFired = new CodeMatch[5]
            {
                new(OpCodes.Ldloc_S, local_at_4),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Ldfld, combat_LastFired),
                new(OpCodes.Callvirt, listMissileWeapon_Remove),
                new(OpCodes.Pop),
            };
            
            if (codeMatcher.MatchEndForward(match_PartLastFired_Not_Null).IsInvalid)
            {
                Success = false;
                Utils.LogTranspilationError(
                    PatchMethodName: patchMethodName,
                    Pos: codeMatcher.Pos,
                    MetricsCheckSteps: metricsCheckSteps,
                    CodeMatchMethod: nameof(CodeMatcher.MatchStartForward),
                    CodeMatchesName: nameof(match_PartLastFired_Not_Null),
                    CodeMatches: match_PartLastFired_Not_Null,
                    Indent: 1);
                codeMatcher.Vomit(Generator, doVomit);
                return Instructions;
            }
            metricsCheckSteps++;

            if (codeMatcher.MatchEndForward(match_List2_Remove_LastFired).IsInvalid)
            {
                Success = false;
                Utils.LogTranspilationError(
                    PatchMethodName: patchMethodName,
                    Pos: codeMatcher.Pos,
                    MetricsCheckSteps: metricsCheckSteps,
                    CodeMatchMethod: nameof(CodeMatcher.MatchStartForward),
                    CodeMatchesName: nameof(match_List2_Remove_LastFired),
                    CodeMatches: match_List2_Remove_LastFired,
                    Indent: 1);
                codeMatcher.Vomit(Generator, doVomit);
                return Instructions;
            }
            metricsCheckSteps++;

            int removeSnippetEnd = codeMatcher.Pos;
            int instructionsToMoveBack = removeSnippetStart - removeSnippetEnd;
            int incstructionsToRemove = -instructionsToMoveBack;
            codeMatcher
                .Advance(instructionsToMoveBack + 1)
                .RemoveInstructions(incstructionsToRemove);

            if (doVomit)
                Utils.Info($"{patchMethodName}, removed {nameof(match_PartLastFired_Not_Null)} and {nameof(match_List2_Remove_LastFired)}");
            codeMatcher.Vomit(doVomit, 6);

            // Modify below
            // 
            // MissileWeapon randomElement = list2.GetRandomElement();
            // 
            //      IL_0221: ldloc.s 4
            //      IL_0223: ldnull
            //      IL_0224: call !!0 Extensions::GetRandomElement<class XRL.World.Parts.MissileWeapon>(class [mscorlib] System.Collections.Generic.List`1<!!0>, class [mscorlib] System.Random)
            //      IL_0229: stloc.s 18
            // 
            // Into:
            // 
            // MissileWeapon randomElement = GetMissileWeaponToFireEvent.Get(Attacker, LastFired, list2);
            // 
            //      IL_####: ldloc.0
            //      IL_####: ldfld class XRL.World.GameObject XRL.World.Parts.Combat/'<>c__DisplayClass17_0'::Attacker
            //      IL_####: ldfld class XRL.World.Parts.MissileWeapon XRL.World.Parts.Combat::LastFired
            //      IL_####: ldloc.s 4
            //      IL_####: call class XRL.World.Parts.MissileWeapon UD_Missile_Chooser.Mod.Events.GetMissileWeaponToFireEvent::Get(class XRL.World.GameObject, class XRL.World.Parts.MissileWeapon, class [mscorlib] System.Collections.Generic.List`1<class XRL.World.Parts.MissileWeapon>)
            //      IL_####: stloc.s 18


            // Add
            //      IL_####: ldloc.0
            //      IL_####: ldfld class XRL.World.GameObject XRL.World.Parts.Combat/'<>c__DisplayClass17_0'::Attacker
            //      IL_####: ldfld class XRL.World.Parts.MissileWeapon XRL.World.Parts.Combat::LastFired
            // 
            // before
            //      IL_0221: ldloc.s 4
            //      IL_0223: ldnull

            codeMatcher.InsertAndAdvance(
                new CodeInstruction[]
                {
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, combat_DisplayClass_Attacker),
                    new(OpCodes.Ldloc_2),
                    new(OpCodes.Ldfld, combat_LastFired)
                });

            if (doVomit)
                Utils.Info($"{patchMethodName}, added {OpCodes.Ldloc_0}, {OpCodes.Ldfld} {nameof(combat_DisplayClass_Attacker)}, {OpCodes.Ldloc_2}, and {OpCodes.Ldfld} {nameof(combat_LastFired)}");
            codeMatcher.Vomit(doVomit, 6);

            var extensions_GetRandomElement = AccessTools.Method(
                type: typeof(Extensions),
                name: nameof(global::Extensions.GetRandomElement),
                parameters: new Type[] { typeof(List<MissileWeapon>), typeof(Random) },
                generics: new Type[] { typeof(MissileWeapon) });

            // Find
            //      IL_0223: ldnull
            //      IL_0224: call !!0 Extensions::GetRandomElement<class XRL.World.Parts.MissileWeapon>(class [mscorlib] System.Collections.Generic.List`1<!!0>, class [mscorlib] System.Random)
            // and remove it

            if (codeMatcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldnull),
                new CodeMatch(OpCodes.Call, extensions_GetRandomElement)
                ).IsInvalid)
            {
                Success = false;
                Utils.LogTranspilationError(
                    PatchMethodName: patchMethodName,
                    Pos: codeMatcher.Pos,
                    MetricsCheckSteps: metricsCheckSteps,
                    CodeMatchMethod: nameof(CodeMatcher.MatchStartForward),
                    CodeMatchesName: $"{OpCodes.Ldnull} and {OpCodes.Call} {nameof(extensions_GetRandomElement)}",
                    CodeMatches: new CodeMatch[]
                    {
                        new(OpCodes.Ldnull),
                        new(OpCodes.Call, extensions_GetRandomElement)
                    },
                    Indent: 1);
                codeMatcher.Vomit(Generator, doVomit);
                return Instructions;
            }
            metricsCheckSteps++;

            if (doVomit)
                Utils.Info($"{patchMethodName}, matched {OpCodes.Ldnull} and {OpCodes.Call} {nameof(extensions_GetRandomElement)}");
            codeMatcher.Vomit(doVomit, 6);

            codeMatcher.RemoveInstructions(2);

            if (doVomit)
                Utils.Info($"{patchMethodName}, removed 2 instructions");
            codeMatcher.Vomit(doVomit, 6);

            // Add
            //      IL_####: call class XRL.World.Parts.MissileWeapon UD_Missile_Chooser.Mod.Events.GetMissileWeaponToFireEvent::Get(class XRL.World.GameObject, class XRL.World.Parts.MissileWeapon, class [mscorlib] System.Collections.Generic.List`1<class XRL.World.Parts.MissileWeapon>)
            // after
            //      IL_0221: ldloc.s 4

            var call_GetMissileWeaponToFireEvent = AccessTools.Method(
                type: typeof(GetMissileWeaponToFireEvent),
                name: nameof(GetMissileWeaponToFireEvent.Get),
                parameters: new Type[] { typeof(GameObject), typeof(MissileWeapon), typeof(List<MissileWeapon>) });

            codeMatcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call, call_GetMissileWeaponToFireEvent));

            if (doVomit)
                Utils.Info($"{patchMethodName}, added {nameof(call_GetMissileWeaponToFireEvent)}");
            codeMatcher.Vomit(doVomit, 15);

            codeMatcher.Advance(1).CreateLabel(out var label_After_StoreMissileWeapon);

            if (doVomit)
                Utils.Info($"{patchMethodName}, created {nameof(label_After_StoreMissileWeapon)}");
            codeMatcher.Vomit(doVomit, 6);

            var local_at_18 = OriginalMethod.GetLocalBuilderAtIndex(18);

            if (doVomit)
            {
                Utils.Log($"{nameof(OriginalMethod)}.{nameof(MethodBody.LocalVariables)}:");
                OriginalMethod.GetMethodBody().LocalVariables.Loggregate(
                    Proc: lvi => $"{lvi}",
                    Empty: "none",
                    PostProc: s => $"{1.Indent()}: {s}");
            }

            codeMatcher.InsertAndAdvance(
                new CodeInstruction[]
                {
                    new(OpCodes.Ldloc_S, (byte)18),
                    new(OpCodes.Brtrue_S, label_After_StoreMissileWeapon),
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Ret)
                });

            if (doVomit)
                Utils.Info($"{patchMethodName}, added \"if (missileWeapon == null) return false;\"");
            codeMatcher.Vomit(doVomit, 6);

            // success??

            return codeMatcher.Vomit(Generator, doVomit).InstructionEnumeration();
        }

        [HarmonyCleanup]
        public static Exception FireMissileWeapon_FireEventForSingleMissile_Cleanup(MethodBase OriginalMethod, Exception Exception)
        {
            if (OriginalMethod == null)
                return Exception;

            string patchMethodName = $"{nameof(Combat_Patches)}.{nameof(Combat.FireMissileWeapon)}";

            if (Exception != null)
            {
                Success = false;
                Utils.Warn($"Failed to transpile {patchMethodName}");
                return null;
            }

            Success = true;
            Utils.Info($"Successfully transpiled {patchMethodName}");
            return null;
        }
    }
}
