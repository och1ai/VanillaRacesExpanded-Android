using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    public class Gene_SelfDestructProtocols : Gene
    {
        public float reactorPower;

        public bool selfDestruct;

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (selfDestruct)
            {
                var reactor = pawn.GetPowerCore();
                if (reactor != null)
                {
                    reactor.Energy -= 1f / 600f * delta;
                    if (reactor.Energy <= 0)
                    {
                        var map = pawn.MapHeld;
                        if (map != null)
                        {
                            var radius = 14 * reactorPower;
                            GenExplosion.DoExplosion(pawn.PositionHeld, pawn.MapHeld, radius, DamageDefOf.Bomb, pawn);
                        }
                        pawn.Destroy();
                    }
                }
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (pawn.IsColonistPlayerControlled)
            {
                var reactor = pawn.GetPowerCore();
                if (reactor != null)
                {
                    if (selfDestruct)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "VREA.CancelSelfDestruct".Translate(),
                            defaultDesc = "VREA.CancelSelfDestructDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/SelfDestruct_Cancel"),
                            action = delegate
                            {
                                selfDestruct = false;
                                reactorPower = 0;
                            }
                        };
                    }
                    else
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "VREA.TriggerSelfDestruct".Translate(),
                            defaultDesc = "VREA.TriggerSelfDestructDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/SelfDestruct_Initiate"),
                            action = delegate
                            {
                                selfDestruct = true;
                                reactorPower = reactor.Energy;
                            }
                        };
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref reactorPower, "reactorPower");
            Scribe_Values.Look(ref selfDestruct, "selfDestruct");
        }
    }
}
