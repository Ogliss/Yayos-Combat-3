using RimWorld;
using Verse;

namespace yayoCombat_Defs
{
    [DefOf]
    public static class JobDefOf
    {
        public static JobDef EjectAmmo;

        static JobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
    }
}
