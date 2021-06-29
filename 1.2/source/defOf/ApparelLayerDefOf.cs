using RimWorld;
using Verse;

namespace yayoCombat_Defs
{
    [DefOf]
    public static class ApparelLayerDefOf
    {
        public static ApparelLayerDef OnSkin_A;
        public static ApparelLayerDef Shell_A;
        public static ApparelLayerDef Middle_A;
        public static ApparelLayerDef Belt_A;
        public static ApparelLayerDef Overhead_A;

        static ApparelLayerDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ApparelLayerDefOf));
    }
}
