using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VREAndroids
{
    // Installs a power core (reactor or battery). The installed core inherits the stored energy of
    // the item used as the ingredient.
    public class Recipe_InstallReactor : Recipe_InstallAndroidPart
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            var core = ingredients.OfType<Reactor>().FirstOrDefault();
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
            var hediff = pawn.health.hediffSet.hediffs.OfType<Hediff_AndroidPowerCore>().FirstOrDefault();
            if (hediff != null && core != null)
            {
                hediff.Energy = core.curEnergy;
            }
        }
    }
}
