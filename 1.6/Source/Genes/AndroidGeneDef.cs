using System.Collections.Generic;
using Verse;

namespace VREAndroids
{
    public class AndroidGeneDef : GeneDef
    {
        public bool isCoreComponent;
        public bool removeWhenAwakened;

        // Hardware this component depends on: the gene can only be installed if at least one of these
        // genes is also present on the android (e.g. the waste subroutines need a reactor, coagulation
        // needs a blood type). Shown as a "Requires: ..." line in the printer / behaviorist UI and
        // enforced before an android can be created, reprogrammed or reprinted. Mirrors how Biotech
        // sanguophage genes require the hemogenic gene.
        public List<GeneDef> requiresOneOf;

        // Components this gene cannot coexist with, referenced by defName so it can point at genes that
        // are generated at load time (e.g. the ideological matrix conflicts with "incapable of social").
        // Enforced and shown as a "Conflicts with: ..." line just like requiresOneOf.
        public List<string> conflictsWith;
    }
}
