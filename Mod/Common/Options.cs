using System;
using System.Collections.Generic;

using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.WorldBuilders;

namespace UD_Missile_Chooser.Mod
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = "Option_UD_Missile_Chooser_")]
    public static class Options
    {
        [OptionFlag] public static bool EnableDefaultFallbackOnCancel;
    }
}
