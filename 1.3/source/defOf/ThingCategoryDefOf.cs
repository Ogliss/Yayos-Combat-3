using RimWorld;
using Verse;

namespace yayoCombat_Defs
{
    [DefOf]
    public static class ThingCategoryDefOf
    {
        public static ThingCategoryDef yy_ammo_category;

        static ThingCategoryDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ThingCategoryDefOf));
    }
}
