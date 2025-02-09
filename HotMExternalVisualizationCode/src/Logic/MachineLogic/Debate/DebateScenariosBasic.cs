using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis.Hacking;

namespace Arcen.HotM.ExternalVis
{
    using deb = Window_Debate;

    public class DebateScenariosBasic : INPCDebateScenarioImplementation
    {
        public static MersenneTwister Rand = new MersenneTwister( 0 );

        public void TryHandleDebateScenarioLogic( NPCDebateScenarioType Scenario, NPCDialogChoice StartingChoice, NPCDebateScenarioLogic Logic )
        {
            if ( Scenario == null || StartingChoice == null )
                return; //was error

            //all of the debate scenarios right now use the same logic.
            //this is mainly here so that it can be swapped out or forked if later desired.

            switch ( Logic )
            {
                case NPCDebateScenarioLogic.DoInitialPopulation:
                    {
                        Rand.ReinitializeWithSeed( Scenario.RowIndexNonSim + StartingChoice.ParentDialog.RowIndexNonSim + SimCommon.UnchangingRandSeed );

                        DebateHelper.SetStartingDebateStats( StartingChoice.DebateTarget, StartingChoice.DebateDiscardsAllowed, StartingChoice.DebateStartingMistrust, StartingChoice.DebateStartingDefiance );
                        DebateHelper.ClearTilesForFreshStart( false, false );

                        DebateHelper.AssignRewards( Rand );

                        NPCDebateAction priorAction = null;
                        foreach ( deb.bUpcoming up in deb.UpcomingList )
                        {
                            up.Action = DebateHelper.CalculateNextUpcomingAction( priorAction );
                            priorAction = up.Action;
                        }
                    }
                    break;
            }            
        }
    }
}
