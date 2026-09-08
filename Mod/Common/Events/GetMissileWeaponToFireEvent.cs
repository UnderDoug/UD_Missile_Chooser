using System.Collections.Generic;

using XRL.World;
using XRL.World.Parts;

namespace UD_Missile_Chooser.Mod.Events
{
    public class GetMissileWeaponToFireEvent : ModSingletonEvent<GetMissileWeaponToFireEvent>
    {
        public static readonly string RegisteredEventID = nameof(GetMissileWeaponToFireEvent);

        public new static readonly int CascadeLevel = CASCADE_ALL;

        public GameObject Actor;
        public MissileWeapon LastFired;
        public List<MissileWeapon> MissileWeapons;
        public MissileWeapon Selection;

        public GetMissileWeaponToFireEvent()
            : base()
        { }

        public override int GetCascadeLevel()
            => CascadeLevel
            ;

        public virtual string GetRegisteredEventID()
            => RegisteredEventID
            ;

        public override void Reset()
        {
            base.Reset();
            Actor = null;
            LastFired = null;
            MissileWeapons = null;
            Selection = null;
        }

        public static MissileWeapon Get(
            GameObject Actor,
            MissileWeapon LastFired,
            List<MissileWeapon> MissileWeapons
            )
        {
            try
            {
                Instance.Actor = Actor;
                Instance.LastFired = LastFired;
                Instance.MissileWeapons = MissileWeapons;
                if (GameObject.Validate(Instance.Actor)
                    && Instance.Actor.WantEvent(ID, CascadeLevel))
                    Instance.Actor.HandleEvent(Instance);

                var result = Instance.Selection;

                return result;
            }
            finally
            {
                Instance.Reset();
            }
        }
    }
}
