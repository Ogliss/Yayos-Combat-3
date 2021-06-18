using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;

namespace yayoCombat
{
    class Stance_yyReload : Stance_Cooldown
    {
        public override bool StanceBusy
        {
            get
            {
                return false;
            }
        }
        public Stance_yyReload()
        {
        }
        public Stance_yyReload(int ticks, LocalTargetInfo focusTarg, Verb verb) : base(ticks, focusTarg, verb)
        {
        }
        


    }

}
