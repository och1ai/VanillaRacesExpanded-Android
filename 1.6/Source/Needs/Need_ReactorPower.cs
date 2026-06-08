using System.Linq;
using RimWorld;
using Verse;

namespace VREAndroids
{
    public class Need_ReactorPower : Need
    {
        public Need_ReactorPower(Pawn pawn)
            : base(pawn)
        {

        }

        // The android's installed power core, whether that is a battery or a reactor.
        public Hediff_AndroidPowerCore PowerCore =>
            pawn.health?.hediffSet?.hediffs.OfType<Hediff_AndroidPowerCore>().FirstOrDefault();

        public override float CurLevel
        {
            get
            {
                var core = PowerCore;
                if (core != null)
                {
                    return core.Energy;
                }
                return 0f;
            }
            set
            {
                var core = PowerCore;
                if (core != null)
                {
                    if (core.pawn is null)
                    {
                        core.pawn = pawn;
                    }
                    core.Energy = value;
                    curLevelInt = core.Energy;
                }
            }
        }

        public override void SetInitialLevel()
        {
            var core = PowerCore;
            if (core != null)
            {
                curLevelInt = core.Energy;
            }
        }

        public override void NeedInterval()
        {

        }
    }
}
