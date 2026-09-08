using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using ConsoleLib.Console;

using Qud.UI;
using static Qud.UI.MissileWeaponArea;

using XRL.Collections;
using XRL.Rules;
using XRL.UI;
using XRL.World.Text;

using UD_Missile_Chooser.Mod;
using UD_Missile_Chooser.Mod.Events;
using UD_Missile_Chooser.Mod.Harmony;

using Options = UD_Missile_Chooser.Mod.Options;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_ChooseMissileWeapon
        : IScribedPartExtension<Combat>
        , IModEventHandler<GetMissileWeaponToFireEvent>
    {
        public static string AmmoPip = "\u00de"; // ▐

        public static string ChooseWeaponCommand => "UD_Missile_Chooser::ChooseWeaponToggleCmd";

        public static List<QudMenuItem> _DefaultButton = new List<QudMenuItem>
        {
            new QudMenuItem
            {
                text = "{{hotkey|[" + ControlManager.getCommandInputFormatted("Cmd_UD_DefaultMissile", UI.Options.ModernUI) + "]}} {{y|Default}}",
                command = "option:-2",
                hotkey = "Cmd_UD_DefaultMissile"
            },
        };

        public static List<QudMenuItem> _LastButton = new List<QudMenuItem>
        {
            new QudMenuItem
            {
                text = "{{hotkey|[" + ControlManager.getCommandInputFormatted("Cmd_UD_LastMissile", UI.Options.ModernUI) + "]}} {{y|Last Fired}}",
                command = "option:-3",
                hotkey = "Cmd_UD_LastMissile"
            },
        };

        public static List<QudMenuItem> DefaultButton
            => ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad
            ? new List<QudMenuItem>
                {
                    new QudMenuItem
                    {
                        text = ControlManager.getCommandInputFormatted("Cmd_UD_DefaultMissile", UI.Options.ModernUI) + " {{y|Default}}",
                        command = "option:-2",
                        hotkey = "Cmd_UD_DefaultMissile"
                    },
                }
            : _DefaultButton
            ;

        public static List<QudMenuItem> LastButton
            => ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad
            ? new List<QudMenuItem>
                {
                    new QudMenuItem
                    {
                        text = ControlManager.getCommandInputFormatted("Cmd_UD_LastMissile", UI.Options.ModernUI) + " {{y|Last Fired}}",
                        command = "option:-3",
                        hotkey = "Cmd_UD_LastMissile"
                    },
                }
            : _LastButton
            ;

        public static Renderable ToggledOnIcon = new(
            Tile: "Abilities/Choose_Missile_On",
            ColorString: $"&Y",
            TileColor: $"&Y",
            DetailColor: 'C');

        public static Renderable ToggledOffIcon = new(
            Tile: "Abilities/Choose_Missile_Off.png",
            ColorString: $"&y",
            TileColor: $"&y",
            DetailColor: 'c');

        public Guid ChooseWeapon_ActivatedAbilityID;

        private bool Silent;

        private bool Active
        {
            get => IsMyActivatedAbilityToggledOn(ChooseWeapon_ActivatedAbilityID, ParentObject);
            set => ToggleMyActivatedAbility(ChooseWeapon_ActivatedAbilityID, ParentObject, Silent: true, SetState: value);
        }

        public UD_ChooseMissileWeapon()
            : base()
        { }

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            base.Write(Basis, Writer);
            Writer.Write(Silent);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            base.Read(Basis, Reader);
            Silent = Reader.ReadBoolean();
        }

        public override void FinalizeRead(SerializationReader Reader)
        {
            base.FinalizeRead(Reader);
            if (Combat_Patches.Success)
                AddAbilities(ParentObject);
        }

        public override void Attach()
        {
            base.Attach();
            AddAbilities(ParentObject, RemoveFirst: true);
        }

        public override void Remove()
        {
            base.Remove();
            RemoveAbilities(ParentObject);
        }

        public void AddAbilities(GameObject Who = null, bool RemoveFirst = false)
        {
            if (RemoveFirst)
                RemoveAbilities(Who);

            if (ChooseWeapon_ActivatedAbilityID.IsEmptyOrDefault())
            {
                ChooseWeapon_ActivatedAbilityID = AddMyActivatedAbility(
                    Name: "Choose Missile Weapon",
                    Command: ChooseWeaponCommand,
                    Class: "Ability",
                    Description: null,
                    Icon: "C",
                    Toggleable: true,
                    DefaultToggleState: Combat_Patches.Success,
                    IsWorldMapUsable: true,
                    Silent: !Combat_Patches.Success || Silent,
                    who: Who,
                    UITileDefault: ToggledOffIcon);
                Silent = true;
            }
        }

        public void RemoveAbilities(GameObject Who = null)
        {
            RemoveMyActivatedAbility(ref ChooseWeapon_ActivatedAbilityID, Who);
        }

        public static string GetStatusAmmoText(MissileWeaponAreaWeaponStatus Status)
        {
            string ammoBars = null;
            if (Status.displayAmmoTotalBars > 0)
            {
                int currentAmmo = Status.displayAmmoRemainingBars;
                int spentAmmo = Status.displayAmmoTotalBars - Status.displayAmmoRemainingBars;
                ammoBars = $" {AmmoPip.ThisManyTimes(currentAmmo).Colored("G")}{AmmoPip.ThisManyTimes(spentAmmo).Colored("K")}";
            }

            string statusText = null;
            if (!Status.text.IsNullOrEmpty())
                statusText = $" {Status.text}";

            return $"{statusText}{ammoBars}";
        }

        public static string GetOptionDisplayName(
            GameObject GameObject,
            GameObject LastFired,
            MissileWeaponAreaWeaponStatus Status,
            bool IsDefault
            )
        {
            string displayName = GameObject.GetDisplayName(Cutoff: 1160, Reference: true);
            string ammoText = GetStatusAmmoText(Status);

            string defaultLabel = IsDefault
                ? " [{{W|default}}]"
                : null;

            string lastFiredLabel = GameObject == LastFired
                    && LastFired != null
                ? " [{{B|last fired}}]" 
                : null;

            return $"{displayName}{ammoText}{defaultLabel}{lastFiredLabel}";
        }

        public static char GetOptionHotkey(GameObject GameObject, int Index, GameObject LastFired)
        {
            if (Index == 0)
                return 'f';
            if (GameObject == LastFired
                && LastFired != null)
                return 'F';
            return ' ';
        }

        public virtual void CollectStats(Templates.StatCollector stats)
        {
            using var tB = TextBuilder.Get();
            if (Options.EnableDefaultFallbackOnCancel)
            {
                tB.Append("Cancelling out of the selection defaults to the base game behavior of selecting a random weapon (excluding [").AppendColored("B", "last fired").Append("]), ")
                    .Append("which is predetermined when the popup is displayed and will be marked [").AppendColored("W", "default").Append("].")
                    .AppendLine()
                    .AppendLine().Append("To cancel firing from the weapon selection, press cancel/escape again after the selection popup is closed.");
            }
            else
            {
                tB.Append("Cancelling out of the selection cancels the ").AppendQuote("fire missile weapon").Append(" action.");
            }

            if (!Combat_Patches.Success)
            {
                tB.Clear().AppendColored("r", "Warning:")
                    .Append(" The patch this skill depends on has failed, please contact ")
                    .Append(Utils.AuthorOnPlatforms)
                    .Append(" to see if the issue can be resolved.");
            }

            stats.Set("CancelBehavior", tB.ToString());
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == BeforeObjectCreatedEvent.ID
            || ID == CommandEvent.ID
            || ID == GetMissileWeaponToFireEvent.ID
            || ID == BeforeAbilityManagerOpenEvent.ID
            ;

        public override bool HandleEvent(BeforeObjectCreatedEvent E)
        {
            if (E.Object == ParentObject)
                AddAbilities(E.Object);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == ChooseWeaponCommand)
            {
                if (!Combat_Patches.Success)
                    Active = false;
                else
                    Active = !Active;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            DescribeMyActivatedAbility(ChooseWeapon_ActivatedAbilityID, CollectStats, ParentObject);
            return base.HandleEvent(E);
        }

        public virtual bool HandleEvent(GetMissileWeaponToFireEvent E)
        {
            if (E.Actor == ParentObject)
            {
                int defaultOption = E.MissileWeapons.IndexOf(E.MissileWeapons.Where(mw => mw != E.LastFired).GetRandomElementCosmetic());

                if (Active
                    && E.Actor.IsPlayer())
                {
                    using var missileWeaponStatuses = ScopeDisposedList<MissileWeaponAreaWeaponStatus>.GetFromPoolFilledWith(E.MissileWeapons.Select(delegate (MissileWeapon mw)
                    {
                        var status = MissileWeaponAreaWeaponStatus.next();
                        mw.Status(status);
                        return status;
                    }));

                    try
                    {
                        using var options = ScopeDisposedList<string>.GetFromPoolFilledWith(E.MissileWeapons.Select((mw, i) =>
                        {
                            return GetOptionDisplayName(
                                GameObject: mw.ParentObject,
                                LastFired: E.LastFired?.ParentObject,
                                Status: missileWeaponStatuses[i],
                                IsDefault: i == defaultOption);
                        }));
                        using var hotkeys = ScopeDisposedList<char>.GetFromPoolFilledWith(E.MissileWeapons.Select((mw, i) => i < 10 ? i.ToString()[0] : ' '));
                        if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
                        {
                            hotkeys.Clear();
                            hotkeys.AddRange(E.MissileWeapons.Select(mw => ' '));
                        }
                        using var icons = ScopeDisposedList<IRenderable>.GetFromPoolFilledWith(missileWeaponStatuses.Select(s => s.renderable));

                        using var buttons = ScopeDisposedList<QudMenuItem>.GetFromPoolFilledWith(DefaultButton);

                        if (E.LastFired != null)
                            buttons.AddRange(LastButton);

                        Popup.PickOption(
                            Title: "Choose missile weapon",
                            Options: options,
                            Hotkeys: hotkeys,
                            Icons: icons,
                            Buttons: buttons,
                            OnResult: delegate (int r)
                            {
                                if (r >= 0
                                    && r < E.MissileWeapons.Count)
                                    E.Selection = E.MissileWeapons[r];
                                else
                                if (r == -3)
                                    E.Selection = E.LastFired;
                                else
                                if (r == -2)
                                    E.Selection = E.MissileWeapons[defaultOption];
                            },
                            DefaultSelected: defaultOption,
                            AllowEscape: true);
                    }
                    finally
                    {
                        foreach (var status in missileWeaponStatuses)
                            status.pool();
                    }
                }

                if (!E.Actor.IsPlayer()
                    || Options.EnableDefaultFallbackOnCancel)
                    E.Selection ??= E.MissileWeapons[defaultOption];

                if (E.Selection != null)
                    return true;
            }
            return base.HandleEvent(E);
        }
    }
}
