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

        // The android has working memory to maintain when it either carries the memory hardware (a
        // permanent memory need) or is overheating with the component-overheating hardware (heat
        // scrambles the drive, so the need appears only while overheating).
        public bool HasMemoryHardware => pawn.HasActiveGene(VREA_DefOf.VREA_MemoryRecharge);
        public bool HeatScrambling => Overheating && pawn.HasActiveGene(VREA_DefOf.VREA_ComponentOverheating);
        public bool MemoryActive => HasMemoryHardware || HeatScrambling;

        public override void NeedInterval()
        {
            // No active memory system right now (e.g. an overheating-hardware android that has cooled
            // back down): memory just sits topped up and never forces a reformat.
            if (!MemoryActive)
            {
                curLevelInt = Mathf.Min(1f, curLevelInt + ((1f / GenDate.TicksPerDay) * 300f));
                return;
            }
            // Working memory fills up during operation and must be periodically reformatted; heat
            // scrambles the drive and drains it much faster.
            float drainPerDay = 150f;
            if (Overheating)
            {
                drainPerDay *= 3f;
            }
            curLevelInt = Mathf.Max(0, curLevelInt - ((1f / GenDate.TicksPerDay) * drainPerDay * pawn.GetStatValue(VREA_DefOf.VREA_MemorySpaceDrainMultiplier)));
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
