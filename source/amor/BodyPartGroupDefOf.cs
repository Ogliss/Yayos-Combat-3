using System;
using Verse;
using RimWorld;

namespace yayoCombat
{
    // Token: 0x02000F0B RID: 3851
    [DefOf]
    public static class BodyPartGroupDefOf
    {
        // Token: 0x06005D8F RID: 23951 RVA: 0x002042FB File Offset: 0x002024FB
        static BodyPartGroupDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BodyPartGroupDefOf));
        }

        public static BodyPartGroupDef Torso;

        public static BodyPartGroupDef UpperHead;

        public static BodyPartGroupDef FullHead;

        public static BodyPartGroupDef Shoulders;

        public static BodyPartGroupDef Arms;

        public static BodyPartGroupDef Hands;

        public static BodyPartGroupDef LeftHand;

        public static BodyPartGroupDef RightHand;

        public static BodyPartGroupDef Legs;

        public static BodyPartGroupDef Feet;



    }
}
