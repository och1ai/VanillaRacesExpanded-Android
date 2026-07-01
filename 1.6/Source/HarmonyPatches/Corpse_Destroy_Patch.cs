using HarmonyLib;
using Verse;

namespace VREAndroids
{
    // Destroying (rotting away, shredding, etc.) an android corpse that still holds its subcore is the
    // moment the android is really and permanently dead - so grief and the "android killed" notice fire
    // now, not at the recoverable destruction earlier. During a resurrection the corpse's inner pawn is
    // already detached before the corpse is destroyed, so this correctly does nothing then.
    [HarmonyPatch(typeof(Corpse), nameof(Corpse.Destroy))]
    public static class Corpse_Destroy_Patch
    {
        public static void Prefix(Corpse __instance)
        {
            Pawn inner = __instance.InnerPawn;
            if (inner != null && inner.IsAndroid() && Utils.HasSubcore(inner, out _))
            {
                Utils.AndroidRealDeath(inner);
            }
        }
    }
}
