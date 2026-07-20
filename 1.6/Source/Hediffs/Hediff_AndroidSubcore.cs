using Verse;

namespace VREAndroids
{
    // The subcore installed inside an android. It is a whole-body implant (no body part) so it
    // survives any localized damage and can always be recovered from the corpse, mirroring a
    // mechlink. It snapshots the android's identity when the android dies so the persona can later
    // be reprinted or resurrected into a new body.
    public class Hediff_AndroidSubcore : HediffWithComps
    {
        public AndroidPersonaData personaData = new AndroidPersonaData();

        public override bool ShouldRemove => false;

        // The subcore is armoured and never shown in the health list - lore-wise it is a shielded core
        // (a Westworld host's brain), not a wound or implant the player manages.
        public override bool Visible => false;

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            personaData.CopyFromPawn(pawn);
            base.Notify_PawnDied(dinfo, culprit);
        }

        public override void Notify_PawnKilled()
        {
            personaData.CopyFromPawn(pawn);
            base.Notify_PawnKilled();
        }

        // Pops the subcore out as an item carrying the stored persona, and removes the implant so the
        // same body cannot yield a second core. Returns the spawned item (null if it could not be
        // placed).
        public AndroidSubcore SpawnSubcore(ThingPlaceMode placeMode = ThingPlaceMode.Near)
        {
            if (!personaData.ContainsData)
            {
                personaData.CopyFromPawn(pawn);
            }
            AndroidSubcore subcore = (AndroidSubcore)ThingMaker.MakeThing(VREA_DefOf.VREA_AndroidSubcore);
            subcore.personaData = personaData;
            Pawn corePawn = pawn;
            Map map = corePawn.MapHeld;
            IntVec3 pos = corePawn.PositionHeld;
            corePawn.health.RemoveHediff(this);
            if (map != null && pos.IsValid)
            {
                GenPlace.TryPlaceThing(subcore, pos, map, placeMode);
            }
            // The persona now lives in the popped subcore item, so the body left behind is an empty,
            // identity-less husk: disown it from the colony so it no longer counts as a colonist (it
            // drops off the colonist bar and the empty shell is not grieved for).
            if (corePawn.Faction != null)
            {
                corePawn.SetFactionDirect(null);
                // The colonist bar caches its entries; force a recache so the disowned husk drops off it
                // instead of lingering (its name greys out but it otherwise stays until the next recache).
                Find.ColonistBar?.MarkColonistsDirty();
            }
            // Pulling the core tears the head off, spraying the android's blood (nothing if bloodless).
            Utils.BlowOffHeadForSubcore(corePawn);
            return subcore;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref personaData, "personaData");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && personaData == null)
            {
                personaData = new AndroidPersonaData();
            }
        }
    }
}
