using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.FFU.RoSAI {
    public class ModInit : IDataCalculatorImplementation {
        public void DoAfterDeserialization(DataCalculator Calculator) {
            ArcenDebugging.LogSingleLine("[FFU: RoSAI] Loading data references...", Verbosity.DoNotShow);
            if (DataRefs.Eviction == null) DataRefs.Eviction = AbilityTypeTable.Instance.GetRowByID("Eviction");
            if (DataRefs.EvictionTag == null) DataRefs.EvictionTag = BuildingTagTable.Instance.GetRowByID("EvictionTarget");
            if (DataRefs.EvictionVis == null) DataRefs.EvictionVis = VisColorUsageTable.Instance.GetRowByID("BuildingValidEvictionTarget");
            if (DataRefs.LiquidMetal == null) DataRefs.LiquidMetal = ResourceTypeTable.Instance.GetRowByID("GadoliniumMesosilicate");
            if (DataRefs.InfoVis == null) DataRefs.InfoVis = VisColorUsageTable.Instance.GetRowByID("BuildingValidInfoTarget");
        }
        public void DoAfterNewCityRankUpChapterOrIntelligenceChange(DataCalculator Calculator) {}
        public void DoPerTurn_Early(DataCalculator Calculator, MersenneTwister RandForThisTurn) {}
        public void DoPerTurn_Late(DataCalculator Calculator, MersenneTwister RandForThisTurn) {}
        public void DoPerQuarterSecond(DataCalculator Calculator, MersenneTwister RandForBackgroundThread) {}
        public void DoPerFrameForMusicTag(DataCalculator Calculator) {}
        public void DoDuringRelatedResourceCalculation(DataCalculator Calculator) {}
        public void DoDuringAttackPowerCalculation(DataCalculator Calculator, ISimMapActor Attacker, ISimMapActor Target, CalculationType CalcType, MersenneTwister RandIfNotPrediction, bool IsAndroidLeapingFromBeyondAttackRange, float IntensityFromAOE, bool CheckCloakedStatus, bool CheckTakeCoverStatus, bool ImagineWillBeInCover, bool ImagineAttackerWillHaveMoved, Vector3 NewAttackerLocation, int ImagineThisAmountOfAttackerHealthWasLost, bool DoFullPrecalculation, bool SkipCaringAboutRange, ArcenCharacterBufferBase BufferOrNull, ArcenCharacterBufferBase SecondaryBufferOrNull, ref int attackerPhysicalPower, ref int attackerFearAttackPower, ref int attackerArgumentAttackPower) { }
        public void DoAfterLanguageChanged(DataCalculator Calculator) {}
    }

    public static class DataRefs {
        public const float LEGAL_MILITARY = 25f;
        public const float LEGAL_SECURITY = 17.5f;
        public const float LEGAL_SYNDICATE = 5f;
        public const float LEGAL_HACKERS = 10f;
        public const float LEGAL_CULT = 7.5f;
        public const float LEGAL_GANGS = 2.5f;
        public const float LEGAL_MARKET = 3.5f;
        public const float LEGAL_VERMIN = 0.75f;
        public const float LEGAL_BACTERIA = 0.25f;
        public const float LEGAL_DEFAULT = 1f;

        public const float EVICT_VOLUME_MULT = 0.05f;
        public const float EVICT_STORAGE_MULT = 0.1f;
        public const float EVICT_AREA_MULT = 0.25f;

        public static AbilityType Eviction = null;
        public static BuildingTag EvictionTag = null;
        public static VisColorUsage EvictionVis = null;
        public static ResourceType LiquidMetal = null;
        public static VisColorUsage InfoVis = null;
    }
}