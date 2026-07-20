using RimWorld;
using Verse;

namespace VREAndroids
{

    [DefOf]
    public static class VREA_DefOf
    {
        public static AndroidSettings VREA_AndroidSettings;
        // The "robot face" symbol pre-selected when creating a brand-new androidtype.
        public static XenotypeIconDef VRE_AndroidXenotypeIcon1;
        // The "drone" symbol - the icon the base mod's basic android xenotype uses; the designer's default
        // basic-android type shows this to match.
        public static XenotypeIconDef VRE_AndroidXenotypeIcon7;
        public static NeedDef VREA_MemorySpace;
        public static NeedDef VREA_ReactorPower;
        public static GeneDef VREA_ReactorPowered, VREA_BatteryPowered, VREA_MentalBreaksDisabled, VREA_Uninspired, VREA_NoSkillGain, VREA_SyntheticBody, VREA_ComponentOverheating,
            VREA_ComponentFreezing, VREA_SyntheticImmunity, VREA_NeutroCirculation, VREA_JoyDisabled, VREA_PsychologyDisabled;
        public static GeneDef VREA_NormalBlood, VREA_Bloodless;
        public static NeedDef VREA_FoodSuppressed;
        public static MentalStateDef VREA_Reformatting;
        [MayRequireBiotech]
        public static MentalStateDef VREA_AwaitingOverseer;
        public static JobDef VREA_FreeMemorySpace;
        public static ThingDef VREA_UnfinishedHealthItemAndroid;
        public static ResearchProjectDef VREA_AndroidTech;
        public static ThingCategoryDef VREA_BodyPartsAndroid;
        public static ThingDef VREA_AndroidPartWorkbench;
        public static EffecterDef Smith;
        public static EffecterDef ButcherMechanoid;
        // The assembler's printing effect (welding sparks + the machining-table working loop).
        public static EffecterDef VREA_AndroidAssembling;
        public static SoundDef Recipe_Smith;
        public static SoundDef Recipe_ButcherCorpseMechanoid;
        public static HediffDef VREA_Reactor;
        public static HediffDef VREA_Battery;
        public static HediffDef VREA_NeutroLoss;
        public static ThingDef VREA_NeutroCasket;
        public static ThingDef VREA_Filth_Neutroamine;
        public static HediffDef VREA_Freezing;
        public static DamageDef VREA_Freeze;
        public static HediffDef VREA_Overheating;
        public static StatDef VREA_MemorySpaceDrainMultiplier;
        public static SoundDef Interact_ConstructMetal;
        public static MentalStateDef VREA_SolarFlared;
        public static GeneCategoryDef VREA_Hardware;
        public static GeneCategoryDef VREA_Subroutine;
        public static RecipeDef VREA_RemoveArtificialPart;
        public static RecipeDef VREA_ResurrectAndroid;
        public static GeneDef VREA_EMPVulnerability;
        public static DamageDef VREA_EMPBurn;
        public static HediffDef VREA_ElectromagneticShock;
        public static StatCategoryDef VREA_Android;
        public static GeneDef VREA_SlowRAM, VREA_FastRAM;
        // Phase 7b subroutines that don't depend on a DLC. (VREA_Occultist / VREA_Gravpilot are gated to
        // Anomaly / Odyssey and are looked up with GetNamedSilentFail so a missing DLC never errors here.)
        public static GeneDef VREA_SleepNeed, VREA_CombatInefficiency, VREA_DelayedDeactivation;
        [MayRequireBiotech]
        public static GeneDef VREA_MechOversight;
        public static AndroidGeneTemplateDef VREA_AptitudeIncapable;
        public static ThingDef VREA_SubcorePolyanalyzer;
        public static ThingDef VREA_SubcorePolyanalyzer_North, VREA_SubcorePolyanalyzer_South, VREA_SubcorePolyanalyzer_East, VREA_SubcorePolyanalyzer_West;
        public static JobDef VREA_CreateAndroid;
        public static JobDef VREA_CompleteAndroidCycle;
        public static ThingDef VREA_AndroidCreationStation;
        public static ThingDef VREA_UnfinishedAndroid;
        public static PawnKindDef VREA_AndroidBasic;
        public static ThingDef VREA_AndroidSubcore;
        public static RecipeDef VREA_ExtractSubcoreSurgery;
        public static HediffDef VREA_AndroidSubcoreImplant;
        public static DesignationDef VREA_ExtractSubcoreDesignation;
        public static JobDef VREA_ExtractSubcore;
        public static RulePackDef VREA_AndroidTypeNameMaker;
        public static RulePackDef VREA_Transition_LowPower;
        public static ThingDef VREA_AndroidBehavioristStation;
        public static JobDef VREA_ModifyAndroid;
        public static EffecterDef VREA_ModifyingAndroidEffecter;
        public static GeneDef VREA_CombatIncapability;
        [MayRequireIdeology]
        public static GeneDef VREA_Ideological;

        [MayRequireIdeology]
        public static HistoryEventDef VRE_AndroidDied;
        [MayRequireIdeology]
        public static PreceptDef VRE_Androids_Tools;
        // "respected (awakened)" / "equals (awakened)": awakened androids are respected or treated as
        // ordinary people, while every non-awakened one is treated as a tool.
        [MayRequireIdeology]
        public static PreceptDef VRE_Androids_RespectedOnlyAwakened;
        [MayRequireIdeology]
        public static PreceptDef VRE_Androids_EqualOnlyAwakened;
        public static QuestScriptDef VREA_Quest_AndroidJoins;

        public static GeneDef VREA_FireVulnerability, VREA_RainVulnerability, VREA_ColdEfficiency, VREA_Uncontrollable,
            VREA_AntiAwakeningProtocols, VREA_EmotionSimulators, VREA_PresenceFirewall, VREA_ZeroWaste, VREA_SelfRecharge, VREA_MemoryDecay;
        public static GeneDef VREA_Coagulation, VREA_MemoryRecharge;
        public static PreceptDef MechanoidLabor_Enhanced;
        public static JobDef VREA_ReplaceReactor;
        public static BackstoryDef ColonyAndroidA01;
        public static ThingDef Neutroamine;
        public static RecipeDef VREA_ButcherCorpseAndroid;
        public static RecipeDef ButcherCorpseFlesh;
        public static GeneDef VREA_PainDisabled;
        public static XenotypeDef VREA_AndroidAwakened;
        public static EffecterDef VREA_AndroidAwakenedEffect;
        public static ThingDef VREA_AndroidAwakenedMote;
        public static ThingDef VREA_AndroidStand, VREA_AndroidStandSpot;
        public static BodyPartDef Skull;
        public static SoundDef Recipe_Machining;
        public static StatDef ButcheryMechanoidSpeed;
        public static ThingDef VREA_AndroidSleepMode;
        public static GeneDef VREA_MechlinkSupport;
        public static GeneDef VREA_SolarPowered;
        public static GeneDef VREA_ExtraWaste;
        public static GeneDef VREA_ClearInstructions;
        public static JobDef VREA_RepairAndroid;
        public static WorkGiverDef VREA_DoBillsAndroidOperation, DoBillsMedicalHumanOperation;
        public static JobDef VREA_RefuelWithNeutroamine;
        public static JobDef VREA_ChargeAndroid;
        public static LetterDef VREA_AndroidAwakenedLetter;
        public static ThingDef VREA_Mote_AndroidReformatting;
        public static BodyPartDef Brain;
        public static ThingDef VREA_SpentReactor;
    }
}
