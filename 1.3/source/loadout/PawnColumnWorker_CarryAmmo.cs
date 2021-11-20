using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace yayoCombat
{
	public class PawnColumnWorker_CarryAmmo : PawnColumnWorker
	{
		public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
		{
			if (pawn.inventoryStock != null)
			{
				float num = rect.width - 4f;
				int num2 = Mathf.FloorToInt(num * 0.333333343f);
				float x = rect.x;

				// lazy way for finding the appropiate def: just named them the same
				InventoryStockGroupDef group = DefDatabase<InventoryStockGroupDef>.AllDefs.First((def) => def.defName == this.def.defName); 

				// Type
				Widgets.Dropdown(
					new Rect(
						x, 
						rect.y + 2f, 
						num2, 
						rect.height - 4f),
					pawn, 
					(Pawn p) => p.inventoryStock.GetDesiredThingForGroup(group), 
					(Pawn p) => GenerateThingButtons(p, group), 
					buttonIcon: pawn.inventoryStock.GetDesiredThingForGroup(group).uiIcon, 
					paintable: true);

				// Amount
				string buffer = null;
				int localI = pawn.inventoryStock.GetDesiredCountForGroup(group);
				Widgets.TextFieldNumeric(
					new Rect(
						x + num2 + 4f,
						rect.y + 2f,
						Mathf.FloorToInt(num * (2f / 3f)),
						rect.height - 4f),
					ref localI, 
					ref buffer,
					max: 1e3f);
				pawn.inventoryStock.SetCountForGroup(group, localI);
			}
		}

		private IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateThingButtons(Pawn pawn, InventoryStockGroupDef group)
		{
			foreach (ThingDef thing in group.thingDefs)
			{
				yield return new Widgets.DropdownMenuElement<ThingDef>
				{
					option = new FloatMenuOption(thing.LabelCap, delegate
					{
						pawn.inventoryStock.SetThingForGroup(group, thing);
					}),
					payload = thing
				};
			}
		}

		public override int GetMinWidth(PawnTable table)
		{
			return Mathf.Max(base.GetMinWidth(table), Mathf.CeilToInt(54f));
		}

		public override int GetOptimalWidth(PawnTable table)
		{
			return Mathf.Clamp(Mathf.CeilToInt(104f), GetMinWidth(table), GetMaxWidth(table));
		}

		public override int GetMinHeaderHeight(PawnTable table)
		{
			return Mathf.Max(base.GetMinHeaderHeight(table), 65);
		}

		public override int Compare(Pawn a, Pawn b)
		{
			return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
		}

		private int GetValueToCompare(Pawn pawn)
		{
			if (pawn.inventoryStock != null)
			{
				// lazy way for finding the appropiate def: just named them the same
				InventoryStockGroupDef group = DefDatabase<InventoryStockGroupDef>.AllDefs.First((def) => def.defName == this.def.defName);
				return pawn.inventoryStock.GetDesiredCountForGroup(group);
			}
			return int.MinValue;
		}
	}
}
