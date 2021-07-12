using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    public class HarmonyInstance : Mod
    {
		public HarmonyInstance(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.yayo.combat");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
    }
}
