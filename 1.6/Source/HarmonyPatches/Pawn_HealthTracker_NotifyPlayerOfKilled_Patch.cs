using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // An android doesn't "die" like an organic - its chassis is destroyed but the subcore survives and
    // can be reprinted or resurrected at an android printer. Swap the grim organic death letter for a
    // mechanoid-style destruction notice that points the player at that recovery path.
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.NotifyPlayerOfKilled))]
    public static class Pawn_HealthTracker_NotifyPlayerOfKilled_Patch
    {
        public static bool Prefix(Pawn ___pawn)
        {
            // A deliberate surgical subcore extraction already told the player what is happening; skip
            // the death notice entirely so it does not read as an unexpected loss.
            if (Utils.extractingSubcore)
            {
                return false;
            }
            Pawn pawn = ___pawn;
            if (!pawn.IsAndroid())
            {
                return true;
            }
            // With its subcore (in the brain) intact this is a recoverable destruction; with the brain
            // gone the subcore died with it, so it is already a true, permanent kill.
            bool hasSubcore = Utils.HasSubcore(pawn, out _);
            TaggedString label = (hasSubcore ? "VREA.AndroidDestroyed" : "VREA.AndroidKilled").Translate()
                + ": " + pawn.LabelShortCap;
            TaggedString text = (hasSubcore ? "VREA.AndroidDestroyedDesc" : "VREA.AndroidKilledDesc")
                .Translate(pawn.Named("PAWN"));
            LetterDef letterDef = hasSubcore ? LetterDefOf.NeutralEvent : LetterDefOf.NegativeEvent;
            if (pawn.Faction == Faction.OfPlayer)
            {
                Find.LetterStack.ReceiveLetter(label, text, letterDef, pawn);
            }
            else
            {
                Messages.Message(text, pawn, MessageTypeDefOf.PawnDeath);
            }
            return false;
        }
    }
}
