using XRL;

namespace UD_Missile_Chooser.Mod
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = "Option_UD_Missile_Chooser_")]
    public static class Options
    {
        [OptionFlag] public static bool EnableDefaultFallbackOnCancel;
    }
}
