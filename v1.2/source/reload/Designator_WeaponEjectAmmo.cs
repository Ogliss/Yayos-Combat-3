using Verse;
using RimWorld;
using UnityEngine;

namespace yayoCombat
{
    /*
    public class Designator_WeaponEjectAmmo : Designator
    {
        public Designator_WeaponEjectAmmo()
        {
            this.defaultLabel = (string)"DesignatorHarvest".Translate();
            this.defaultDesc = (string)"DesignatorHarvestDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/Harvest");
            this.soundDragSustain = SoundDefOf.Designate_DragStandard;
            this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            this.useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Designate_Harvest;
            this.hotKey = KeyBindingDefOf.Misc2;
            this.designationDef = DesignationDefOf.Flick;
        }

        protected DesignationDef designationDef;

        public override int DraggableDimensions => 2;

        protected override DesignationDef Designation => this.designationDef;

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(this.Map) || c.Fogged(this.Map))
                return (AcceptanceReport)false;
            Thing t = reloadUtility.getEjectableWeapon(c, this.Map);
            if (t == null)
                return (AcceptanceReport)false;
            AcceptanceReport acceptanceReport = this.CanDesignateThing(t);
            return !acceptanceReport.Accepted ? acceptanceReport : (AcceptanceReport)true;
        }

        public override void DesignateSingleCell(IntVec3 c) => this.DesignateThing(reloadUtility.getEjectableWeapon(c, this.Map));

        public override void SelectedUpdate() => GenUI.RenderMouseoverBracket();

        


        

    }
    */


}
