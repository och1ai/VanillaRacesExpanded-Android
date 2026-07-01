using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    public class Building_AndroidStand : Building_Bed
    {
        public static HashSet<Building_AndroidStand> stands = new HashSet<Building_AndroidStand>();

        public CompPowerTrader compPower;

        // Toxic waste produced while charging, stored as wastepacks like a mech recharger.
        // A full cycle (matching a wastepack stack) deposits one wastepack into the container.
        public const int WasteProducedPerChargingCycle = 5;
        private float wasteProduced;
        private CompWasteProducer wasteProducerInt;
        private CompThingContainer wasteContainerInt;
        public CompWasteProducer WasteProducer => wasteProducerInt ??= this.TryGetComp<CompWasteProducer>();
        public CompThingContainer WasteContainer => wasteContainerInt ??= this.TryGetComp<CompThingContainer>();

        // The stand is jammed once a full cycle of waste has built up and a wastepack already
        // occupies the container; it must be hauled out before charging can resume.
        public bool IsFullOfWaste =>
            wasteProduced >= WasteProducedPerChargingCycle && WasteContainer != null && WasteContainer.innerContainer.Any;

        // Accumulates charging pollution; when a cycle fills and the container is empty, deposits a
        // wastepack. ZeroWaste suppresses it and ExtraWaste triples the rate.
        public void AddChargingWaste(float energyGained, Pawn android)
        {
            if (WasteProducer == null || android.HasActiveGene(VREA_DefOf.VREA_ZeroWaste))
            {
                return;
            }
            float factor = android.HasActiveGene(VREA_DefOf.VREA_ExtraWaste) ? 3f : 1f;
            wasteProduced = Mathf.Clamp(wasteProduced + WasteProducedPerChargingCycle * energyGained * factor, 0f, WasteProducedPerChargingCycle);
            if (wasteProduced >= WasteProducedPerChargingCycle && (WasteContainer == null || WasteContainer.innerContainer.Any is false))
            {
                wasteProduced = 0f;
                WasteProducer.ProduceWaste(WasteProducedPerChargingCycle);
            }
        }

        public Pawn CurOccupant
        {
            get
            {
                List<Thing> list = Map.thingGrid.ThingsListAt(this.Position);
                for (int i = 0; i < list.Count; i++)
                {
                    Pawn pawn = list[i] as Pawn;
                    if (pawn != null && pawn.IsAndroid() && pawn.pather.moving is false)
                    {
                        return pawn;
                    }
                }
                return null;
            }
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            stands.Add(this);
            compPower = this.TryGetComp<CompPowerTrader>();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            stands.Remove(this);
        }

        public override void Tick()
        {
            base.Tick();
            var occupant = CurOccupant;
            if (occupant != null)
            {
                occupant.Rotation = Rot4.South;
                if (occupant.jobs.curDriver is JobDriver_LayDown)
                {
                    occupant.jobs.curDriver.rotateToFace = TargetIndex.C;
                }
            }
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var opt in base.GetFloatMenuOptions(selPawn))
            {
                yield return opt;
            }
            if (this.Faction == Faction.OfPlayer && selPawn.HasActiveGene(VREA_DefOf.VREA_MemoryRecharge))
            {
                var cannotUseReason = CannotUseNowReason(selPawn);
                if (cannotUseReason.NullOrEmpty())
                {
                    yield return new FloatMenuOption("VREA.FreeMemorySpace".Translate(), delegate
                    {
                        if (CompAssignableToPawn.AssignedPawns.Contains(selPawn) is false)
                        {
                            CompAssignableToPawn.TryAssignPawn(selPawn);
                        }
                        selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VREA_DefOf.VREA_FreeMemorySpace, this));
                    });
                }
                else
                {
                    yield return new FloatMenuOption("VREA.FreeMemorySpace".Translate() + ": " + cannotUseReason, null);
                }
            }
            // Only battery androids can charge, and only at a powered stand drawing from the grid.
            if (this.Faction == Faction.OfPlayer && selPawn.GetPowerCore()?.CanRecharge == true)
            {
                if (compPower == null || compPower.PowerOn is false)
                {
                    yield return new FloatMenuOption("VREA.Recharge".Translate() + ": " + "NoPower".Translate().CapitalizeFirst(), null);
                }
                else if (IsFullOfWaste)
                {
                    yield return new FloatMenuOption("VREA.Recharge".Translate() + ": " + "VREA.AndroidStandFullOfWaste".Translate(), null);
                }
                else
                {
                    var cannotUseReason = CannotUseNowReason(selPawn);
                    if (cannotUseReason.NullOrEmpty())
                    {
                        yield return new FloatMenuOption("VREA.Recharge".Translate(), delegate
                        {
                            if (CompAssignableToPawn.AssignedPawns.Contains(selPawn) is false)
                            {
                                CompAssignableToPawn.TryAssignPawn(selPawn);
                            }
                            selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VREA_DefOf.VREA_ChargeAndroid, this));
                        });
                    }
                    else
                    {
                        yield return new FloatMenuOption("VREA.Recharge".Translate() + ": " + cannotUseReason, null);
                    }
                }
            }
        }

        public string CannotUseNowReason(Pawn selPawn)
        {
            if (compPower != null && !compPower.PowerOn)
            {
                return "NoPower".Translate().CapitalizeFirst();
            }
            if (!selPawn.CanReach(this, PathEndMode.OnCell, Danger.Deadly))
            {
                return "NoPath".Translate().CapitalizeFirst();
            }
            if (!selPawn.CanReserve(this))
            {
                Pawn pawn = selPawn.Map.reservationManager.FirstRespectedReserver(this, selPawn);
                if (pawn == null)
                {
                    pawn = selPawn.Map.physicalInteractionReservationManager.FirstReserverOf(selPawn);
                }
                if (pawn != null)
                {
                    return "ReservedBy".Translate(pawn.LabelShort, pawn);
                }
                else
                {
                    return "Reserved".Translate();
                }
            }
            if (CurOccupant != null)
            {
                return "VREA.AndroidStandIsOccupied".Translate();
            }
            if (!RestUtility.CanUseBedNow(this, selPawn, checkSocialProperness: false))
            {
                return "VREA.CannotUse".Translate();
            }
            return null;
        }

        public override string GetInspectString()
        {
            string s = base.GetInspectString();
            if (WasteProducer != null)
            {
                string waste = "WasteLevel".Translate() + ": " + (wasteProduced / WasteProducedPerChargingCycle).ToStringPercent();
                s = s.NullOrEmpty() ? waste : s + "\n" + waste;
            }
            return s;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref wasteProduced, "wasteProduced", 0f);
        }
    }
}
