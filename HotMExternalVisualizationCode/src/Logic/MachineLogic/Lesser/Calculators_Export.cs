using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using System.Text;
using System.IO;

namespace Arcen.HotM.ExternalVis
{
    public class Calculators_Export : IDataCalculatorImplementation
    {
        #region Unused
        public void DoAfterDeserialization( DataCalculator Calculator )
        {
        }

        public void DoAfterNewCityRankUpChapterOrIntelligenceChange( DataCalculator Calculator )
        {
        }

        public void DoPerTurn_Early( DataCalculator Calculator, MersenneTwister RandForThisTurn )
        {
        }

        public void DoPerTurn_Late( DataCalculator Calculator, MersenneTwister RandForThisTurn )
        {
        }

        public void DoPerQuarterSecond( DataCalculator Calculator, MersenneTwister RandForBackgroundThread )
        {
        }

        public void DoPerFrameForMusicTag( DataCalculator Calculator )
        {
        }

        public void DoDuringRelatedResourceCalculation( DataCalculator Calculator )
        {
        }

        public void DoDuringAttackPowerCalculation( DataCalculator Calculator, ISimMapActor Attacker, ISimMapActor Target, CalculationType CalcType, MersenneTwister RandIfNotPrediction, bool IsAndroidLeapingFromBeyondAttackRange, float IntensityFromAOE, bool CheckCloakedStatus, bool CheckTakeCoverStatus, bool ImagineWillBeInCover, bool ImagineAttackerWillHaveMoved, Vector3 NewAttackerLocation, int ImagineThisAmountOfAttackerHealthWasLost, bool DoFullPrecalculation, bool SkipCaringAboutRange, ArcenCharacterBufferBase BufferOrNull, ArcenCharacterBufferBase SecondaryBufferOrNull, ref int attackerPhysicalPower, ref int attackerFearAttackPower, ref int attackerArgumentAttackPower )
        {
        }

        public void DoAfterGoalCompleted( DataCalculator Calculator, TimelineGoalPath Path )
        {
        }
        #endregion

        public void DoAfterLanguageChanged( DataCalculator Calculator )
        {
            if ( Calculator == null || Calculator.ID != "ExportsAfterLanguageChanged" )
                return;

            if ( !(GameSettings.Current?.GetBool( "Debug_ExportForWikiAfterLoad" )??false) )
                return;

            DoMachineHandbookWikiExport();
        }

        #region DoMachineHandbookWikiExport
        private void DoMachineHandbookWikiExport()
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                foreach ( MachineHandbookSection section in MachineHandbookSectionTable.Instance.Rows )
                {
                    if ( section.EntriesList.Count == 0 )
                        continue;

                    builder.Append( "=" ).Append( section.GetDisplayName() ).Append( "=\n\n");

                    foreach ( MachineHandbookEntry entry in section.EntriesList )
                    {
                        builder.Append( "==" ).Append( entry.GetDisplayName() ).Append( "==\n\n" );

                        builder.Append( entry.GetDescription().Replace( "\n", "\n\n" ) ).Append( "\n\n" );
                        builder.Append( entry.StrategyTip.Text.Replace( "\n", "\n\n" ) ).Append( "\n\n" );
                    }
                }

                string fullFilename = Engine_Universal.CurrentGameDataDirectory + "WikiNewExports/MachineHandbook_" + GameSettings.CurrentLanguage.SteamLangID + ".txt";

                File.WriteAllText( fullFilename, builder.ToString() );
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine( "DoMachineHandbookWikiExport error! " + e, Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}
