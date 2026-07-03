using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // Reconciles every android once per load. All three helpers are idempotent, so this is a no-op for
    // androids that are already correct:
    //  - blood organs vs blood type (androids built before per-blood-type organs, or with a mismatch),
    //  - ideoligion vs the ideological subroutine (an android without it must follow no ideoligion;
    //    loading re-assigns colony ideoligions, so this clears them again),
    //  - duplicate genes left over from the old single-instance removal bug.
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
                        Utils.RemoveDuplicateGenes(pawns[j]);
                        Utils.SyncBloodOrgans(pawns[j]);
                        Utils.SyncAndroidIdeo(pawns[j]);
                    }
                }
            }
        }
    }
}
