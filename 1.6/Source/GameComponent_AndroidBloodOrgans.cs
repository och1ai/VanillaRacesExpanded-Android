using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // Reconciles every android's circulatory organs with its blood type once per load, so androids
    // built before the per-blood-type organs existed (or with a mismatched organ) get corrected.
    // SyncBloodOrgans is idempotent, so this is a no-op for androids that are already correct.
    public class GameComponent_AndroidBloodOrgans : GameComponent
    {
        public GameComponent_AndroidBloodOrgans(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                var pawns = maps[i].mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    if (pawns[j].IsAndroid())
                    {
                        Utils.SyncBloodOrgans(pawns[j]);
                    }
                }
            }
        }
    }
}
