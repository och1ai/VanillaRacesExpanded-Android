using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(PawnGenerator), "TryGenerateNewPawnInternal")]
    public static class PawnGenerator_TryGenerateNewPawnInternal_Patch
    {
        public static PawnGenerationRequest? curRequest;
        public static void Prefix(PawnGenerationRequest request)
        {
            curRequest = request;
        }
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(ref Pawn __result)
        {
            curRequest = null;
            if (__result?.genes != null)
            {
                if (__result.HasActiveGene(VREA_DefOf.VREA_PsychologyDisabled))
                {
                    __result.story.traits = new TraitSet(__result);
                    __result.story.favoriteColor = null;
                }
                var gene = __result.genes.GetGene(VREA_DefOf.VREA_SyntheticBody) as Gene_SyntheticBody;
                if (gene != null && __result.IsAwakened() is false)
                {
                    if (__result.Name is NameTriple nameTriple)
                    {
                        gene.storedTripleName = nameTriple;
                        __result.Name = new NameSingle(nameTriple.First);
                    }
                }
                if (__result.IsAndroid())
                {
                    // Clear any duplicate genes (belt-and-suspenders against the old duplication bug),
                    // then reconcile the ideoligion: androids only follow one if they carry the
                    // ideological subroutine, otherwise they are left with none.
                    Utils.RemoveDuplicateGenes(__result);
                    // Guarantee the power core exists (its part can be missing when the power gene's PostAdd
                    // ran mid-generation), then re-evaluate downed state. Without a core an android is flagged
                    // ShouldBeDowned and the generator's downed check rejects it - this is what stopped basic
                    // and awakened androids from spawning at all via dev-mode / faction generation.
                    Utils.SyncPowerCore(__result);
                    var core = __result.GetPowerCore();
                    if (core != null)
                    {
                        core.Energy = 1f;
                    }
                    __result.health?.CheckForStateChange(null, null);
                    Utils.SyncAndroidIdeo(__result);
                }
            }
            PawnBioAndNameGenerator_GiveShuffledBioTo_Patch.xenotypeStatic = null;
        }
    }
}
