using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    public class Need_MemorySpace : Need
    {
        public Need_MemorySpace(Pawn pawn)
            : base(pawn)
        {

        }

        public override void SetInitialLevel()
        {
            this.curLevelInt = Rand.Range(0.1f, 0.9f);
        }
        public bool Overheating => pawn.health?.hediffSet?.HasHediff(VREA_DefOf.VREA_Overheating) ?? false;

        public override void NeedInterval()
        {
            // The memory-recharge subroutine self-defragments: outside of overheating it steadily
            // refills, so the android never has to reformat. Overheating still scrambles the drive,
            // so memory then drains as usual and can force a reboot.
            if (pawn.HasActiveGene(VREA_DefOf.VREA_MemoryRecharge) && !Overheating)
            {
                curLevelInt = Mathf.Min(1f, curLevelInt + ((1f / GenDate.TicksPerDay) * 300f));
                return;
            }
            curLevelInt = Mathf.Max(0, curLevelInt - ((1f / GenDate.TicksPerDay) * 150f * pawn.GetStatValue(VREA_DefOf.VREA_MemorySpaceDrainMultiplier)));
            if (curLevelInt == 0f && pawn.MentalStateDef != VREA_DefOf.VREA_Reformatting)
            {
                if (pawn.Spawned)
                {
                    if (pawn.InMentalState)
                    {
                        pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
                    }
                    pawn.mindState.mentalStateHandler.TryStartMentalState(VREA_DefOf.VREA_Reformatting);
                }
                else
                {
                    curLevelInt = 1f;
                }
            }
        }
    }
}
