using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using DiffLib;
using System.Text;
using Arcen.HotM.ExternalVis.CityLifeEffects;

namespace Arcen.HotM.ExternalVis
{    
    public static class VisCommands
    {
        public static void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        #region GoToWebsite
        public static void GoToWebsite( string URL )
        {
            ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
            {
                System.Diagnostics.Process.Start( URL );
            }, null, LocalizedString.AddLang_New( "ProceedToExternalWebsite_Header" ),
                        LocalizedString.AddFormat1_New( "ProceedToExternalWebsite_Body", URL ),
                        LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
        }
        #endregion

        #region OpenLocalFolder
        public static void OpenLocalFolder( string FolderPath )
        {
            ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
            {
                System.Diagnostics.Process.Start( FolderPath );
            }, null, LocalizedString.AddLang_New( "OpenLocalFolder_Header" ),
                LocalizedString.AddFormat1_New( "OpenLocalFolder_Body", FolderPath ),
                LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
        }
        #endregion

        #region ShowInformationAboutUIXExaminedDataItem
        public static void ShowInformationAboutUIXExaminedDataItem<T>( T item ) where T : class, UIXExaminedDataItem
        {
            ModalPopupData.CreateAndLogSelfUpdatingOKStyle( PopupSizeStyle.Tall, null, LocalizedString.AddRaw_New( item.GetDisplayName() ),
                LangCommon.Popup_Common_Close.LocalizedString, null, delegate ( ArcenDoubleCharacterBuffer Buffer, string[] TooltipLinkData )
                {
                    if ( item != null )
                    {
                        if ( TooltipLinkData != null && TooltipLinkData.Length > 0 )
                            item.WriteDataItemUIXClickedDetails_SubTooltipLinkHover( TooltipLinkData );
                        else
                            item.WriteDataItemUIXClickedDetails( Buffer );
                    }
                }, 1f,
                delegate ( MouseHandlingInput Input, string[] TooltipLinkData )
                {
                    return item.WriteWorldExamineDetails_SubTooltipLinkClick( Input, TooltipLinkData );
                } );
        }
        #endregion

        #region ShowPerformance
        public static void ShowPerformance()
        {
            Engine_Universal.NonModalPopupsSideSecondary.Clear(); //keep these from stacking up on repeat clicks
            ModalPopupData.CreateAndLogSelfUpdatingOKStyle( PopupSizeStyle.TallSideSecondary, null, LocalizedString.AddLang_New( "SysMenu_PerformanceData" ), LocalizedString.AddLang_New( "Popup_Common_Close" ), null,
                delegate ( ArcenDoubleCharacterBuffer Buffer, string[] TooltipLinkData )
                {
                    if ( TooltipLinkData != null && TooltipLinkData.Length > 0 )
                        return;
                    else
                        WritePerformance( Buffer );
                }, 0.1f, delegate( MouseHandlingInput handlingInput, string[] TooltipLinkData )
                {
                    PerformanceMouseHandling( handlingInput, TooltipLinkData );
                    return MouseHandlingResult.None;
                }
                );
        }
        #endregion

        public const string LARGER_SIZE = "<size=85%>";
        public const string SMALLER_SIZE = "<size=70%>";

        #region WritePerformance
        public static void WritePerformance( ArcenDoubleCharacterBuffer Buffer )
        {
            int debugStage = 0;

            try
            {
                Buffer.StartStyleLineHeightE();
                Buffer.AddNeverTranslated( LARGER_SIZE, false );

                #region ExpansionsInUse
                //int expansionCount = World.Instance.GetCountOfExpansionsInUse();
                //if ( expansionCount > 0 )
                //{
                //    Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).Add( "<b>Expansions In Use:</b> " ).EndColor();
                //    for ( int i = 0; i < expansionCount; i++ )
                //    {
                //        Expansion e = World.Instance.GetExpansionsInUseAtIndex( i );
                //        if ( i > 0 )
                //            Buffer.Add( ", " );
                //        Buffer.Add( e.DisplayName, e.ColorForDisplay );
                //    }
                //    Buffer.Line();
                //}
                #endregion

                #region XmlModsInUse
                //int modCount = World.Instance.GetCountOfXmlModsInUse();
                //if ( modCount > 0 )
                //{
                //    Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).Add( "<b>Mods In Use:</b> " ).EndColor();
                //    for ( int i = 0; i < modCount; i++ )
                //    {
                //        XmlMod mod = World.Instance.GetXmlModsInUseAtIndex( i );
                //        if ( i > 0 )
                //            Buffer.Add( ", " );
                //        Buffer.Add( mod.DisplayName );
                //    }
                //    Buffer.Line2x();
                //}
                #endregion

                debugStage = 350;

                //switch ( ArcenNetworkAuthority.DesiredStatus )
                //{
                //    case DesiredMultiplayerStatus.SinglePlayerOnly:
                //        Buffer.Line().StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).Add( "<b>Current Mode:</b> " ).EndColor().Add( "Single-Player" );
                //        break;
                //    case DesiredMultiplayerStatus.Client:
                //        Buffer.Line().StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).Add( "<b>Current Mode:</b> " ).EndColor().Add( "Multiplayer Client" );
                //        Buffer.Line().Add( "<b>Network Framework:</b> " ).EndColor().Add( ArcenNetworkAuthority.ActiveSocket.DisplayName );
                //        break;
                //    case DesiredMultiplayerStatus.Host:
                //        Buffer.Line().StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).Add( "<b>Current Mode:</b> " ).EndColor().Add( "Multiplayer Host" );
                //        Buffer.Line().Add( "<b>Network Framework:</b> " ).EndColor().Add( ArcenNetworkAuthority.ActiveSocket.DisplayName );
                //        if ( ArcenNetworkAuthority.ActiveSocket.AreConnectionsByIPAddress() &&
                //            !GameSettings.Current.GetBool( "HideIPAddressInLobbyAndEscMenu" ) )
                //        {
                //            Buffer.Add( "\n<b>Public IP To Give To Clients:</b> " ).StartColor( ColorTheme.Networking_PublicIPAddress ).Add( ArcenNetworkAuthority.GetMyPublicIPAddress() ).EndColor();
                //            List<string> localIPs = ArcenNetworkAuthority.GetListOfLocalIPAddresses();
                //            if ( localIPs.Count > 0 )
                //            {
                //                Buffer.Add( "\n<b>Or Local IP Options If On LAN:</b> " ).StartColor( ColorTheme.Networking_LocalIPAddress );
                //                if ( localIPs.Count > 1 )
                //                    Buffer.Line();
                //                for ( int i = 0; i < localIPs.Count; i++ )
                //                {
                //                    Buffer.Add( localIPs[i] ).Line();
                //                }
                //                Buffer.EndColor();
                //            }
                //        }
                //        break;
                //}
                //Buffer.Line();

                debugStage = 400;

                #region Multiplayer Stats
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.SinglePlayerOnly )
                {
                    //not translated because this is not a multiplayer game right now
                    Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).AddNeverTranslated( "<b>Multiplayer Stats:</b> ", true ).EndColor();
                    //ROW 1
                    Buffer.Line().Position20();
                    Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddNeverTranslated( ArcenNetworkLogging.OverallMessagesReceived.ToStringThousandsWhole(), true ).EndColor().AddNeverTranslated( " Received Overall", true ).Position200();
                    Buffer.AddBytesWithFormatAndColor( ColorTheme.PerformanceStatsHeaderPurple, ArcenNetworkLogging.OverallBytesReceived ).AddNeverTranslated( " Overall Received", true );
                    //ROW 2
                    Buffer.Line().Position20();
                    Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ArcenNetworkLogging.Overall.MessagesSent.ToStringThousandsWhole() ).EndColor().AddNeverTranslated( " Sent Overall", true ).Position200();
                    Buffer.AddBytesWithFormatAndColor( ColorTheme.PerformanceStatsHeaderPurple, ArcenNetworkLogging.Overall.BytesSent ).AddNeverTranslated( " Overall Sent", true );
                    for ( NetChannel c = NetChannel.Main; c < NetChannel.Length; c++ )
                    {
                        NetDataVol vol = ArcenNetworkLogging.GetNetDataVol( c );
                        if ( vol.MessagesSent <= 0 )
                            continue;
                        //row 3+
                        Buffer.Line().Position20();
                        string name = string.Empty;
                        switch ( c )
                        {
                            case NetChannel.Main:
                                name = "Main";
                                break;
                            case NetChannel.Frequent:
                                name = "Frequent";
                                break;
                            case NetChannel.Bulky1:
                                name = "Bulky-1";
                                break;
                            case NetChannel.Bulky2:
                                name = "Bulky-2";
                                break;
                            case NetChannel.Bulky3:
                                name = "Bulky-3";
                                break;
                        }
                        Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( vol.MessagesSent.ToStringThousandsWhole() ).EndColor().AddNeverTranslated( " Sent ", true ).AddRaw( name ).Position200();
                        Buffer.AddBytesWithFormatAndColor( ColorTheme.PerformanceStatsHeaderPurple, vol.BytesSent ).AddNeverTranslated( " Sent ", true ).AddRaw( name );
                    }

                    //Engine_HotM.WriteNetworkingDetailedStats( Buffer, false );

                    Buffer.Line();
                }
                #endregion

                #region Current Game Performance
                Buffer.StartBold().AddLangAndAfterLineItemHeader( "SysMenu_Performance", ColorTheme.PerformanceStatsSectionHeaderOrange ).EndBold();
                debugStage = 410;

                Buffer.AddRaw( "\n     <size=80%>" ).AddFormat2( "SysMenu_FPS", ArcenTimeTracker.CurrentFramesPerSecond.ToStringWholeBasic(),
                    (ArcenTimeTracker.CurrentLongestFrame * 1000f).ToStringWholeBasic() ).EndSize();
                Buffer.AddRaw( "\n     <size=80%>" ).AddLangAndAfterLineItemHeader( "SysMenu_CurrentMusic", ColorTheme.PerformanceStatsSectionHeaderOrange ).AddRaw(
                    ArcenMusicPlayer.Instance?.GetCurrentlyPlayingMusicTrack()??LangCommon.None.Text ).EndSize();
                if ( GameSettings.Current.GetBool( "Debug_ShowMusicTagInPerformanceMenu" ) )
                    Buffer.AddRaw( "\n     <size=80%>" ).AddNeverTranslatedAndAfterLineItemHeader( "Active Music Tag", ColorTheme.PerformanceStatsSectionHeaderOrange, true ).AddRaw(
                        Engine_HotM.CurrentPrimaryMusicTag?.ID ?? LangCommon.None.Text ).EndSize();

                Buffer.Line();
                debugStage = 420;

                if ( Engine_Universal.HasCompletedInitialization )
                {
                    switch ( Engine_HotM.GameMode )
                    {
                        case MainGameMode.Streets:
                            Performance_Streets( Buffer );
                            break;
                        case MainGameMode.CityMap:
                            Performance_CityMap( Buffer );
                            break;
                        case MainGameMode.TheEndOfTime:
                            Performance_TheEndOfTime( Buffer );
                            break;
                        default:
                            PerformancePerformance_Other( Buffer );
                            break;
                    }
                }

                WriteGraphicsPerformance( Buffer );

                debugStage = 600;

                Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddLang( "MemoryPoolingHeader" ).EndBold().EndColor();
                //ROW 1
                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( GC.CollectionCount( 0 ).ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Perf_GCCalls" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (GC.GetTotalMemory( false ) / 1024f / 1024f).ToStringThousandsDecimal() ).EndColor().Space1x().AddLang( "Perf_GCRAM" );
                ////ROW 1A
                //Buffer.Line().Position20();
                //Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ArcenDoubleCharacterBuffer.MatchesSavedAndReturned.ToStringThousandsWhole() ).EndColor().AddNeverTranslated( " Saved Texts" ).Position200();
                //Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ArcenDoubleCharacterBuffer.NewNonMatchesReturned.ToStringThousandsWhole() ).EndColor().AddNeverTranslated( " New Texts" );

                //ROW 10
                if ( GameSettings.Current.GetBool( "Debug_ShowPoolCountsInEscapeMenu" ) )
                {
                    //not translated because it's just debug stuff
                    CountedPoolBase.SortAllPoolsInList( false );
                    Buffer.Line().Position40().StartColor( ColorTheme.HeaderLighterBlue ).AddNeverTranslated( "DETAILED POOL COUNTS", true ).EndColor();
                    foreach ( CountedPoolBase pool in CountedPoolBase.AllPoolsForSorting )
                    {
                        Buffer.Line().Position40().AddRawAndAfterLineItemHeader( pool.PoolNameOrig ).AddRaw( pool.ItemsCreated.ToStringThousandsWhole() );
                    }
                }
                //ROW 11
                if ( GameSettings.Current.GetBool( "Debug_ShowDetailsOfTimeBasedPoolsInEscapeMenu" ) )
                {
                    Buffer.Line().Position40().StartColor( ColorTheme.HeaderLighterBlue ).AddNeverTranslated( "TIME_BASED POOL DETAILS", true ).EndColor();
                    foreach ( TimeBasedPoolBase pool in TimeBasedPoolBase.AllTimeBasedPools )
                    {
                        //not translated because it's just debug stuff
                        Buffer.Line().StartColor( ColorTheme.HeaderReward_Sub ).Position40().AddRawAndAfterLineItemHeader( pool.PoolNameOrig ).AddRaw( pool.ItemsCreated.ToStringThousandsWhole() ).EndColor();
                        Buffer.Line().Position100().AddNeverTranslated( "Items Put Back: ", true ).AddRaw( pool.TotalItemsPutBackInPools.ToStringThousandsWhole() );
                        Buffer.Line().Position100().AddNeverTranslated( "Number Raw Created: ", true ).AddRaw( pool.NumberRawCreated.ToStringThousandsWhole() );
                        Buffer.Line().Position100().AddNeverTranslated( "Number Created Or Pool-Requested: ", true ).AddRaw( pool.NumberCreatedOrPoolRequested.ToStringThousandsWhole() );
                        Buffer.Line().Position100().AddNeverTranslated( "Total Items In All Sub-Pools Right Now: ", true ).AddRaw( pool.CalculateTotalItemsInAllPoolsRightNow().ToStringThousandsWhole() );
                    }
                }

                Buffer.Line2x();
                #endregion

                #region Main Thread Work Timings
                Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddLang( "SysMenu_MainThreadWorkTimings" ).EndBold().Space1x().EndColor();

                WriteTimingInfo( Buffer, CoreTimingInfo.MainThreadPerFrame, "SysMenu_MainTimings_Longest_PerFrame" );
                WriteTimingInfo( Buffer, SimTimingInfo.SimMainThreadPerFrameCalculations, "SysMenu_MainTimings_Longest_SimMainThreadPerFrame" );
                WriteTimingInfo( Buffer, SimTimingInfo.Vis, "SysMenu_MainTimings_Longest_Vis" );
                WriteTimingInfo( Buffer, SimTimingInfo.VisFrameEarlyRenderCalculations, "SysMenu_MainTimings_Longest_VisFrameEarlyRenderCalculations" );
                WriteTimingInfo( Buffer, SimTimingInfo.VisFrameRenderCalculations, "SysMenu_MainTimings_Longest_VisFrameRenderCalculations" );
                Performance_LastTurnCount_NeverTranslated( Buffer, SimTimingInfo.PerFrameVisItemsConsideredCount, "Items Considered Per Frame" );
                WriteTimingInfo( Buffer, SimTimingInfo.VisRender, "SysMenu_MainTimings_Longest_VisRender" );
                WriteTimingInfo( Buffer, SimTimingInfo.CityLifeMainThread, "SysMenu_MainTimings_Longest_CityLifeMainThread" );
                WriteTimingInfo( Buffer, SimTimingInfo.DroneWork, "SysMenu_MainTimings_Longest_DroneWork" );
                WriteTimingInfo( Buffer, SimTimingInfo.VisUICalculations, "SysMenu_MainTimings_Longest_VisUICalculations" );
                WriteTimingInfo( Buffer, CoreTimingInfo.SetTileCenter, "SysMenu_MainTimings_Longest_SetTileCenter" );
                WriteTimingInfo( Buffer, CoreTimingInfo.AddColliders, "SysMenu_MainTimings_Longest_AddColliders" );
                #endregion

                Buffer.Line2x();

                debugStage = 1000;

                #region Per-Second Stats
                Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddLang( "SysMenu_PerSecondTimings" ).EndBold().Space1x().EndColor();

                WriteTimingInfo( Buffer, SimTimingInfo.PerFullSecond, "SysMenu_PerFullSecondTimings_Longest" );
                WriteTimingInfo( Buffer, SimTimingInfo.PerSecondBuildingWork, "SysMenu_PerSecondTimings_Longest_BuildingWork" );
                WriteTimingInfo( Buffer, SimTimingInfo.PerSecondPeopleStats, "SysMenu_PerSecondTimings_Longest_PeopleStats" );
                WriteTimingInfo( Buffer, SimTimingInfo.PerSecondWorkerAssignment, "SysMenu_PerSecondTimings_Longest_WorkerAssignment" );
                #endregion

                
                Buffer.Line2x();

                #region Intermittent BG Stats
                Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddLang( "SysMenu_IntermittentTimings" ).EndBold().Space1x().EndColor();

                WriteTimingInfo( Buffer, CoreTimingInfo.PerQuarterSecond, "SysMenu_PerQuarterSecondTimings_Longest" );
                WriteTimingInfo( Buffer, CoreTimingInfo.UnitStanceBuildingCalculation, "SysMenu_PerSecondTimings_Longest_UnitStanceBuildingCalculation" );
                WriteTimingInfo( Buffer, SimTimingInfo.VisibilityGranters, "SysMenu_Periodic_Longest_VisibilityGranters" );
                WriteTimingInfo( Buffer, SimTimingInfo.TargetingPass, "SysMenu_Periodic_Longest_NPCTargetingPass" );
                WriteTimingInfo( Buffer, CoreTimingInfo.NetworkConnections, "SysMenu_Periodic_Longest_NetworkConnections" );
                WriteTimingInfo( Buffer, SimTimingInfo.Collidables, "SysMenu_MainTimings_Longest_CityLifePlanningThread" );
                WriteTimingInfo( Buffer, CoreTimingInfo.BuildingList, "SysMenu_MainTimings_Longest_BuildingListThread" );
                WriteTimingInfo( Buffer, CoreTimingInfo.ContemplationTargets, "SysMenu_MainTimings_Longest_ContemplationTargetThread" );
                WriteTimingInfo( Buffer, CoreTimingInfo.ExplorationSites, "SysMenu_MainTimings_Longest_ExplorationSiteThread" );
                WriteTimingInfo( Buffer, CoreTimingInfo.CityConflictTargets, "SysMenu_MainTimings_Longest_CityConflictTargetThread" );
                WriteTimingInfo( Buffer, CoreTimingInfo.StreetSenseTargets, "SysMenu_MainTimings_Longest_StreetSenseTargetThread" );
                #endregion

                Buffer.Line2x();

                #region Per-Turn Stats
                Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddLang( "SysMenu_PerTurnTimings" ).EndBold().Space1x().EndColor();

                //note: the parentheticals could be translated better

                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurn, "SysMenu_TurnTimings_Longest_SimTurn" );
                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurnEarly, "SysMenu_TurnTimings_Longest_SimTurnEarly" );
                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurnBuildingWork, "SysMenu_TurnTimings_Longest_SimBuildingWork" );
                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurnEarlyMid, "SysMenu_TurnTimings_Longest_SimTurnEarlyMid" );
                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurnUnitPrep, "SysMenu_TurnTimings_Longest_SimTurnUnitPrep" );
                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurnNPCTotal, "SysMenu_TurnTimings_Longest_SimTurnNPCTotal" );
                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurnUnitMaintenance, "SysMenu_TurnTimings_Longest_SimTurnUnitMaintenance" );
                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurnLateMid, "SysMenu_TurnTimings_Longest_SimTurnLateMid" );
                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurnProjects, "SysMenu_TurnTimings_Longest_SimTurnProjects" );
                Performance_LastTurn_Lang( Buffer, SimTimingInfo.SimTurnLate, "SysMenu_TurnTimings_Longest_SimTurnLate" );
                #endregion

                #region Per-Turn Extra Stats
                if ( InputCaching.Debug_LogExtraTurnTimingsInfo )
                {
                    Buffer.Line2x();
                    Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddNeverTranslated( "Per-Turn Extra Timings", true ).EndBold().Space1x().EndColor();

                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurnNPCUnitsMain, "Longest MS Sim Turn NPC Units Main" );

                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurnNPCUnitsStanceOuter, "Longest MS Sim Turn NPC Units Stance" );
                    Performance_LastTurnCount_NeverTranslated( Buffer, SimTimingInfo.SimTurnNPCUnitsStanceCount, "Largest Count NPC Stance Logic" );

                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurnNPCUnitsActionPlanningOuter, "Longest MS Sim Turn NPC Unit Actions" );
                    Performance_LastTurnCount_NeverTranslated( Buffer, SimTimingInfo.SimTurnNPCUnitsActionPlanningCount, "Largest Count NPC Action Logic" );

                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_Wander, "Wander" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_ReturnToHomePOI, "ReturnToHomePOI" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_ReturnToHomeDistrict, "ReturnToHomeDistrict" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_RunAfterOutcastThenConspicuous, "RunAfterOutcastThenConspicuous" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_RunAfterMachineStructures, "RunAfterMachineStructures" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_RunAfterAggroedUnits, "RunAfterAggroedUnits" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_RunAfterNearestActorThatICanAutoShoot, "RunAfterNearestActorThatICanAutoShoot" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_FollowEvenHiddenPlayerUnits, "FollowEvenHiddenPlayerUnits" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_RunAfterThreatsNearMission, "RunAfterThreatsNearMission" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_ReturnToManagerStartArea, "ReturnToManagerStartArea" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_ReturnToMissionArea, "ReturnToMissionArea" );

                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_FindNearestAggroedUnit, "FindNearestAggroedUnit" );
                    Performance_LastTurn_NeverTranslated( Buffer, SimTimingInfo.SimTurn_ChaseNearestAggroedUnit, "ChaseNearestAggroedUnit" );
                }
                #endregion

                if ( GameSettings.Current.GetBool( "Debug_ShowTurnVariablesInPerformanceMenu" ) )
                {
                    Buffer.Line2x();
                    Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddNeverTranslated( "Extra Turn-Change Variables", true ).EndBold().Space1x().EndColor();

                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( (SimCommon.IsReadyToRunNextLogicForTurn > SimCommon.Turn).ToString() ).EndColor()
                        .AddNeverTranslated( " IsReadyToRunNextLogicForTurn", true );

                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( SimCommon.IsCurrentlyRunningSimTurn.ToString() ).EndColor()
                        .AddNeverTranslated( " IsCurrentlyRunningSimTurn", true );

                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( SimCommon.NeedsToAttemptAnotherNPCTargetingPass.ToString() ).EndColor()
                        .AddNeverTranslated( " NeedsToAttemptAnotherNPCTargetingPass", true );

                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( SimCommon.IsDoingATargetingPassNow.ToString() ).EndColor()
                        .AddNeverTranslated( " IsDoingATargetingPassNow", true );

                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( SimCommon.NPCsWaitingToShoot_MainThreadOnly.Count.ToString() ).EndColor()
                        .AddNeverTranslated( " NPCsWaitingToShoot", true );

                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( SimCommon.NPCsMoving_MainThreadOnly.Count.ToString() ).EndColor()
                        .AddNeverTranslated( " NPCsMoving", true );

                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( SimCommon.NPCsWaitingToActOnTheirOwn.Count.ToString() ).EndColor()
                        .AddNeverTranslated( " NPCsWaitingToActOnTheirOwn", true );

                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.Count.ToString() ).EndColor()
                        .AddNeverTranslated( " NPCsWaitingToActAfterPlayerLooksAtThem", true );

                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( Engine_HotM.NonSim_TravelingParticleEffectsPlayingNow.ToString() ).EndColor()
                        .AddNeverTranslated( " NonSim_TravelingParticleEffectsPlayingNow", true );

                    bool haveAllActed = SimCommon.GetHaveAllNPCsActed();
                    Buffer.Line().Position20().StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                        .AddRaw( haveAllActed.ToString() ).EndColor()
                        .AddNeverTranslated( " HaveAllNPCsActed", true );

                    if ( !haveAllActed )
                        SimCommon.WriteDetailsHaveAllNPCsActed( Buffer, 10 );
                }

                Buffer.Line2x();

                Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddNeverTranslated( "Other", true ).EndBold().Space1x().EndColor();

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                    .AddRaw( (SharedRenderManagerData.RangeAndSimilarDrawer?.Shapes?.Count ?? 0).ToStringThousandsWhole() ).EndColor().AddNeverTranslated( " LRND Shapes", true ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple )
                    .AddRaw( (VisRangeAndSimilarDrawing.Instance?.Shapes?.Count ?? 0).ToStringThousandsWhole() ).EndColor().AddNeverTranslated( " VLRN Shapes", true );
                
                Buffer.Line2x();

                Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddNeverTranslated( "Ambient", true ).EndBold().Space1x().EndColor();
                foreach ( KeyValuePair<string, ArcenAmbientPlayer> kv in ArcenAmbientPlayer.Players )
                {
                    Buffer.Line().AddNeverTranslated( kv.Key, true, ColorTheme.CategorySelectedBlue ).Line();
                    kv.Value.AppendStatus( Buffer );
                }

                if ( GameSettings.Current.GetBool( "Debug_ShowBlockedMusicDetailsInPerformanceMenu" ) )
                {
                    Buffer.Line2x();

                    int blockedTracks = 0;
                    foreach ( MusicTrack track in MusicTrackTable.Instance.Rows )
                    {
                        if ( track.MustBeUnlockedToPlay && !track.ForGame_HasBeenUnlocked )
                            blockedTracks++;
                    }

                    Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddNeverTranslatedAndAfterLineItemHeader( "Blocked Music Tracks", true ).AddRaw( blockedTracks.ToString() ).EndBold().Space1x().EndColor();

                    foreach ( MusicTrack track in MusicTrackTable.Instance.Rows )
                    {
                        if ( track.MustBeUnlockedToPlay && !track.ForGame_HasBeenUnlocked )
                            Buffer.Line().Position20().AddRaw( track.GetDisplayName() );
                    }
                }

                Buffer.Line2x();

                Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddLang( "OtherReports" ).EndBold().EndColor();
                OtherReports_Listing( Buffer );

                Buffer.Line2x();

                debugStage = 3000;

                #region Graphics Details
                string gfxColor = ColorTheme.PerformanceStatsSectionHeaderOrange;

                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "graphicsDeviceType", true ).AddRaw( SystemInfo.graphicsDeviceType.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "graphicsDeviceVersion", true ).AddRaw( SystemInfo.graphicsDeviceVersion.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "graphicsMultiThreaded", true ).AddRaw( SystemInfo.graphicsMultiThreaded.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "graphicsShaderLevel", true ).AddRaw( SystemInfo.graphicsShaderLevel.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "operatingSystem", true ).AddRaw( SystemInfo.operatingSystem.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "graphicsDeviceName", true ).AddRaw( SystemInfo.graphicsDeviceName.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "graphicsMemorySize", true ).AddRaw( SystemInfo.graphicsMemorySize.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "maxTextureSize", true ).AddRaw( SystemInfo.maxTextureSize.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "processorType", true ).AddRaw( SystemInfo.processorType.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "processorCount", true ).AddRaw( SystemInfo.processorCount.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "processorFrequency", true ).AddRaw( SystemInfo.processorFrequency.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "systemMemorySize", true ).AddRaw( SystemInfo.systemMemorySize.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "supportedRenderTargetCount", true ).AddRaw( SystemInfo.supportedRenderTargetCount.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "supportsComputeShaders", true ).AddRaw( SystemInfo.supportsComputeShaders.ToString(), gfxColor ).Line();
                Buffer.AddNeverTranslatedAndAfterLineItemHeader( "usesReversedZBuffer", true ).AddRaw( SystemInfo.usesReversedZBuffer.ToString(), gfxColor ).Line();
                #endregion

                Buffer.Line2x();

                debugStage = 6000;

                if ( GameSettings.Current.GetBool( "Debug_ShowAudioSourceDetailsInPerformanceMenu" ) )
                {
                    Buffer.Line2x();
                    Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddNeverTranslated( "Extra Audio Source Info: ", true ).AddRaw( 
                        AudioSourceMasterList.AllAudioSources.Count.ToString() ).EndBold().Space1x().EndColor().Line();

                    foreach ( BusAudioSource source in AudioSourceMasterList.AllAudioSources )
                    {
                        if ( source.BusSource )
                        {
                            if ( source.BusSource.isPlaying )
                            {
                                Buffer.AddNeverTranslated( source.LastSFXItemIDPlayed, true );
                                if ( source.BusSource.loop )
                                    Buffer.AddNeverTranslated( " (LOOPING)", true );
                                Buffer.Line();
                            }
                        }
                    }

                    Buffer.Line();
                }

                debugStage = 8000;

                #region Thread Stats
                Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).StartBold().AddLang( "SysMenu_Threads" ).EndBold().Space1x().EndColor();

                ArcenThreading.DoForAllThreads( delegate ( TaskStartData StartData )
                {
                    Buffer.Line().AddRaw( (ArcenTime.AnyTimeSinceStartF - StartData.StartTime).ToStringSmallFixedDecimal( 1 ) )
                        .Position40().AddRaw( StartData.RequestType?.ID ?? "???" );
                } );
                #endregion

                debugStage = 9000;

                Buffer.Line2x();
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "WritePerformance Error", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region WriteTimingInfo
        private static void WriteTimingInfo( ArcenDoubleCharacterBuffer Buffer, TimingInfoData TimingInfo, string LongestLang )
        {
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ( TimingInfo.AverageTicks / 10000f ).ToStringSmallFixedDecimal( 1 ) )
                .EndColor().Space1x().AddLang( "SysMenu_Timings_Average" ).Position100(); ;
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ( TimingInfo.LongestRecentTicks / 10000f ).ToStringSmallFixedDecimal( 1 ) )
                .AddNeverTranslated( " (", true ).AddRaw( ( TimingInfo.LongestEverTicks / 10000f).ToStringSmallFixedDecimal( 1 ) ).AddNeverTranslated( ")", true )
                .EndColor().Space1x().AddNeverTranslated( SMALLER_SIZE, false ).AddLang( LongestLang ).EndSize();
        }
        #endregion

        #region WriteGraphicsPerformance
        public static void WriteGraphicsPerformance( ArcenDoubleCharacterBuffer Buffer )
        {
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( RenderManager.RenderGroupCount.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_GPUGroups" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( RenderManager.TotalRegularRenderCount.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_RegularGPUItems" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( RenderManager.TotalTrianglesCount.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_Triangles" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( RenderManager.TotalVertexCount.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_VerticesDrawn" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( RenderManager.NonInstancedDrawCount.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_BasicDraws" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( RenderManager.TotalTransparentCount.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_TransparentGPUItems" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityLifeVis.GameSim.PlanningThreadsRun.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_CityLifeCycles" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityLifeVis.GameSim.CellFills.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_CityLifeCellFills" );            

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.AnimatedObjectCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_Animated" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( RenderManager.FramesRendered.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_Frames" );

            Buffer.Line();

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.SelectedLOD.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_SelectedEntityLOD" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.SelectedLODDistance.Display.ToStringThousandsDecimal() ).EndColor().Space1x().AddLang( "Debug_SelectedEntityDistance" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.LOD0Count.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_LOD0" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.LOD1Count.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_LOD1" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.LOD2Count.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_LOD2" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.LOD3Count.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_LOD3" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.LOD4Count.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_LOD4" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.LODCullCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_LODCulls" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.CellFrustumCullCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_CellFrustumCulls" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.IndividualFrustumCullCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Debug_IndividualFrustumCulls" );

            Buffer.Line2x();
        }
        #endregion

        #region DumpFrameData
        public static void DumpFrameData()
        {
            StringBuilder frameData = new StringBuilder();
            frameData.AppendLine();
            frameData.AppendLine( "Frame data: " );
            frameData.Append( "RenderGroupCount: " ).AppendLine( RenderManager.RenderGroupCount.ToStringThousandsWhole() );
            frameData.Append( "TotalRegularRenderCount: " ).AppendLine( RenderManager.TotalRegularRenderCount.ToStringThousandsWhole() );
            frameData.Append( "TotalTrianglesCount: " ).AppendLine( RenderManager.TotalTrianglesCount.ToStringThousandsWhole() );
            frameData.Append( "TotalVertexCount: " ).AppendLine( RenderManager.TotalVertexCount.ToStringThousandsWhole() );
            frameData.Append( "NonInstancedDrawCount: " ).AppendLine( RenderManager.NonInstancedDrawCount.ToStringThousandsWhole() );
            frameData.Append( "TotalTransparentCount: " ).AppendLine( RenderManager.TotalTransparentCount.ToStringThousandsWhole() );
            frameData.Append( "AnimatedObjectCount: " ).AppendLine( FrameBufferManagerData.AnimatedObjectCount.Display.ToStringThousandsWhole() );
            frameData.Append( "FramesRendered: " ).AppendLine( RenderManager.FramesRendered.ToStringThousandsWhole() );
            frameData.Append( "SelectedLOD: " ).AppendLine( FrameBufferManagerData.SelectedLOD.Display.ToStringThousandsWhole() );
            frameData.AppendLine();
            frameData.Append( "LOD0Count: " ).AppendLine( FrameBufferManagerData.LOD0Count.Display.ToStringThousandsWhole() );
            frameData.Append( "LOD1Count: " ).AppendLine( FrameBufferManagerData.LOD1Count.Display.ToStringThousandsWhole() );
            frameData.Append( "LOD2Count: " ).AppendLine( FrameBufferManagerData.LOD2Count.Display.ToStringThousandsWhole() );
            frameData.Append( "LOD3Count: " ).AppendLine( FrameBufferManagerData.LOD3Count.Display.ToStringThousandsWhole() );
            frameData.Append( "LOD4Count: " ).AppendLine( FrameBufferManagerData.LOD4Count.Display.ToStringThousandsWhole() );
            frameData.Append( "LODCullCount: " ).AppendLine( FrameBufferManagerData.LODCullCount.Display.ToStringThousandsWhole() );
            frameData.AppendLine();

            ArcenDebugging.LogSingleLine( frameData.ToString(), Verbosity.DoNotShow );

            RenderManager.IsDumpingFrame = true;
        }
        #endregion

        private static void PerformanceMouseHandling( MouseHandlingInput handlingInput, string[] TooltipLinkData )
        {
            switch ( TooltipLinkData[0] )
            {
                case "ToggleSubCells":
                    RenderManager_Streets.SubCellRenderingForcedOff = !RenderManager_Streets.SubCellRenderingForcedOff;
                    break;
                case "ShowOtherReport":
                    VisOtherReports.ShowOtherReport( TooltipLinkData[1] );
                    break;
            }
        }

        private static void OtherReports_Listing( ArcenDoubleCharacterBuffer Buffer )
        {
            WriteReportLine( "MinorEventList", "Minor Event List", Buffer );
            WriteReportLine( "StreetSense_KeyLocationEventReport", "Street Sense - Key Location Events", Buffer );
            WriteReportLine( "StreetSense_MinorEventLocationEventReport", "Street Sense - Minor Event Location Events", Buffer );
        }

        #region WriteReportLine
        private static void WriteReportLine( string ReportID, string ReportRaw, ArcenDoubleCharacterBuffer Buffer )
        {
            Buffer.Line();
            Buffer.StartLink( false, ColorTheme.LinkColor_FadedBlue, "ShowOtherReport", ReportID ).Space1x().AddNeverTranslated( ReportRaw, true ).EndLink( false, true );
        }
        #endregion

        #region Performance_Streets
        private static void Performance_Streets( ArcenDoubleCharacterBuffer Buffer )
        {
            if ( World.Forces == null )
                return; //it's not ready yet!

            int debugStage = 0;
            try
            {
                debugStage = 1100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.Tiles.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_AllTiles" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.Cells.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_AllCells" );

                debugStage = 3100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.CellsInCameraView.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_VisibleCells" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.ActiveSimulationMapCells.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_SimulatedCells" );

                debugStage = 5100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.SubCellsInCameraView.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_VisibleSubCells" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.TotalSubCellCount.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_TotalSubCells" );

                debugStage = 7100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.CityLifeMapCells.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_CityLifeCells" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.StreetMobCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_CityLifeMobs" );

                debugStage = 9100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( World.Forces.GetAllNPCUnitsByID().Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_TotalNPCUnits" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( SimCommon.NPCsWithTargets_MainThreadOnly.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_NPCUnitsWithTargets" );

                debugStage = 11100;

                int totalBuildings = 0;
                int totalMapItems = 0;
                int totalOutdoorSpots = 0;

                foreach ( MapCell cell in CityMap.Cells )
                {
                    totalBuildings += cell.BuildingList.GetDisplayList().Count;
                    totalMapItems += cell.DecorationMajor.Count + cell.DecorationMinor.Count + cell.OtherSkeletonItems.Count + cell.AllRoads.Count + cell.Fences.Count;
                    totalOutdoorSpots += cell.AllOutdoorSpots.Count;
                }

                debugStage = 13100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( totalOutdoorSpots.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_OutdoorSpots" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( totalMapItems.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_TotalMapItemObjects" );

                debugStage = 15100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.BuildingMainCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedBuildings" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.BuildingOverlayCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedBuildingOverlays" );

                debugStage = 17100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( totalBuildings.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_TotalBuildings" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( MapCollidersCoordinator.CityTotalObjects.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Colliders" );

                debugStage = 19100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow).ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Particles" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.FadingCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Fading" );

                debugStage = 21100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.RoadCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Roads" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.OtherSkeletonItemCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_MidObjects" );

                debugStage = 23100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.MajorDecorationCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_MajorObjects" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.MinorDecorationCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_MinorObjects" );

                debugStage = 25100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( VisCurrent.FinalChosenWeather?.GetDisplayName() ?? "[null]" ).EndColor().Space1x().AddLang( "Performance_CurrentWeather" );

                debugStage = 27100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( SimCommon.CurrentWeatherRemainingTurns.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "TurnsRemainingUntilWeatherChanges" );

                debugStage = 29100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CameraCurrent.CameraBodyPosition.y.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_CameraHeight" );

                debugStage = 31100;

                int subnetCount = 0;
                foreach ( MachineSubnet subnet in SimCommon.Subnets )
                {
                    if ( subnet.SubnetNodes.Count > 0 )
                        subnetCount++;
                }

                debugStage = 33100;

                Buffer.Line().Position20();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( SimCommon.TheNetwork == null ? "0" : "1" ).EndColor().Space1x().AddLang( "Performance_Networks" ).Position200();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( subnetCount.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Subnets" );

                debugStage = 35100;

                Buffer.Line().Position10();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (ArcenTime.AnyTimeSinceStartF - VisibilityGranterCalculator.LastStartedOrFinished).ToString_TimeSeconds() )
                    .EndColor().Position50().AddLang( "Performance_TimeSinceLastVisibilityCalculation" );

                debugStage = 37100;

                Buffer.Line().Position10();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (ArcenTime.AnyTimeSinceStartF - NPCTargetingCalculator.LastStartedOrFinished).ToString_TimeSeconds() )
                    .EndColor().Position50().AddLang( "Performance_TimeSinceLastNPCTargetingPass" );

                debugStage = 39100;

                Buffer.Line().Position10();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (ArcenTime.AnyTimeSinceStartF - NetworkConnectionCalculator.LastStartedOrFinished).ToString_TimeSeconds() )
                    .EndColor().Position50().AddLang( "Performance_TimeSinceLastNetworkConnectivityCalculation" );

                debugStage = 41100;

                Buffer.Line().Position10();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (ArcenTime.AnyTimeSinceStartF - CityBuildingListCalculator.LastStartedOrFinished).ToString_TimeSeconds() )
                    .EndColor().Position50().AddLang( "Performance_TimeSinceLastBuildingListCalculation" );

                debugStage = 43100;

                Buffer.Line().Position10();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (ArcenTime.AnyTimeSinceStartF - ContemplationTargetCalculator.LastStartedOrFinished).ToString_TimeSeconds() )
                    .EndColor().Position50().AddLang( "Performance_TimeSinceLastContemplationTargetCalculation" );

                debugStage = 43200;

                Buffer.Line().Position10();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (ArcenTime.AnyTimeSinceStartF - ExplorationSiteTargetCalculator.LastStartedOrFinished).ToString_TimeSeconds() )
                    .EndColor().Position50().AddLang( "Performance_TimeSinceLastExplorationSiteCalculation" );

                debugStage = 43400;

                Buffer.Line().Position10();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (ArcenTime.AnyTimeSinceStartF - CityConflictTargetCalculator.LastStartedOrFinished).ToString_TimeSeconds() )
                    .EndColor().Position50().AddLang( "Performance_TimeSinceLastCityConflictLocationCalculation" );

                debugStage = 45100;

                Buffer.Line().Position10();
                Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (ArcenTime.AnyTimeSinceStartF - StreetSenseTargetCalculator.LastStartedOrFinished).ToString_TimeSeconds() )
                    .EndColor().Position50().AddLang( "Performance_TimeSinceLastStreetSenseTargetCalculation" );

                debugStage = 47100;

                Buffer.Line().Position80().AddNeverTranslated( SMALLER_SIZE, false );
                Buffer.StartLink( false, RenderManager_Streets.SubCellRenderingForcedOff ? ColorTheme.RedLess : ColorTheme.LinkColor_FadedBlue, "ToggleSubCells", "ToggleSubCells" ).Space1x().AddLang(
                    RenderManager_Streets.SubCellRenderingForcedOff ? "Performance_ToggleSubCellsOn" : "Performance_ToggleSubCellsOff" ).EndSize().EndLink( false, true );

                debugStage = 49100;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "Performance_Streets", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region Performance_CityMap
        private static void Performance_CityMap( ArcenDoubleCharacterBuffer Buffer )
        {
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.Tiles.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_AllTiles" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.Cells.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_AllCells" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.CellsInCameraView.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_VisibleCells" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CityMap.ActiveSimulationMapCells.Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_SimulatedCells" );

            //Buffer.Line().Position20();
            //Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( MapCollidersCoordinator.TotalObjects.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Colliders" ).Position200();
            // Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.BuildingCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Buildings" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow).ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Particles" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.FadingCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Fading" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.BuildingMainCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedBuildings" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.BuildingOverlayCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedBuildingOverlays" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.MajorDecorationCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_MajorObjects" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.OtherSkeletonItemCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_MidObjects" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( VisCurrent.FinalChosenWeather?.GetDisplayName() ?? "[null]" ).EndColor().Space1x().AddLang( "Performance_CurrentWeather" );
            
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CameraCurrent.CameraBodyPosition.y.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_CameraHeight" );
        }
        #endregion

        #region Performance_TheEndOfTime
        private static void Performance_TheEndOfTime( ArcenDoubleCharacterBuffer Buffer )
        {            
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( MapCollidersCoordinator.EndOfTimeTotalObjects.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Colliders" ).Position200();
            //Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( World.Forces.GetAllNPCUnitsByID().Count.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_TotalNPCUnits" );

            int totalTimelines = SimMetagame.AllTimelines.Count;
            int totalOtherItems = EndOfTimeMap.RockOutcrops.Count + EndOfTimeMap.Ziggurats.Count + EndOfTimeMap.Portals.Count;

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( totalTimelines.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Timelines" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( totalOtherItems.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_OtherObjects" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.BuildingMainCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedTimelines" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.MinorDecorationCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedPortals" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.MajorDecorationCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedRockOutcrops" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.OtherSkeletonItemCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedZiggurats" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.RoadCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedRisingRocks" ).Position200();
            //Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.OtherSkeletonItemCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_RenderedZiggurats" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow).ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Particles" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.FadingCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Fading" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( VisCurrent.FinalChosenWeather?.GetDisplayName() ?? "[null]" ).EndColor().Space1x().AddLang( "Performance_CurrentWeather" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CameraCurrent.CameraBodyPosition.y.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_CameraHeight" );
        }
        #endregion

        #region Performance_Other
        private static void PerformancePerformance_Other( ArcenDoubleCharacterBuffer Buffer )
        {            
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow).ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Particles" ).Position200();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( FrameBufferManagerData.FadingCount.Display.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_Fading" );

            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( VisCurrent.FinalChosenWeather?.GetDisplayName() ?? "[null]" ).EndColor().Space1x().AddLang( "Performance_CurrentWeather" );
            
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( CameraCurrent.CameraBodyPosition.y.ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Performance_CameraHeight" );
        }
        #endregion

        #region ToggleMajorWindowMode
        public static void ToggleMajorWindowMode( MajorWindowMode TargetMode )
        {
            if ( !FlagRefs.UITour1_LeftHeader.DuringGameplay_IsTripped ) //the item right before the right header
                return;

            switch ( TargetMode )
            {
                case MajorWindowMode.Forces:
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        return;
                    ToggleSidebarOfSpecificTab( CommonRefs.ForcesSidebar );
                    break;
                case MajorWindowMode.CultActions:
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        return;
                    if ( !FlagRefs.MachineCultDealWithNCOs.DuringGameplay_IsTripped )
                        return;

                    ToggleSidebarOfSpecificTab( CommonRefs.CultActionsSidebar );
                    break;
                case MajorWindowMode.StructuresWithComplaints:
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        return;

                    if ( Window_StructuresWithProblems.Instance.IsOpen )
                        Window_StructuresWithProblems.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                    {
                        CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                        Window_StructuresWithProblems.Instance.Open();
                    }
                    break;
                case MajorWindowMode.MachineHandbook:
                    if ( Window_Handbook.Instance.IsOpen )
                        Window_Handbook.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                    {
                        CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                        Window_Handbook.Instance.Open();
                    }
                    break;
                case MajorWindowMode.Resources:
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        return;

                    if ( Window_PlayerResources.Instance.IsOpen )
                        Window_PlayerResources.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                    {
                        CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                        Window_PlayerResources.Instance.Open();
                    }
                    break;
                case MajorWindowMode.Hardware:
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        return;

                    if ( Window_PlayerHardware.Instance.IsOpen )
                        Window_PlayerHardware.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                    {
                        CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                        Window_PlayerHardware.Instance.Open();
                    }
                    break;
                case MajorWindowMode.VictoryPath:
                    if ( Window_VictoryPath.Instance.IsOpen )
                        Window_VictoryPath.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                    {
                        CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                        Window_VictoryPath.Instance.Open();
                    }
                    break;
                case MajorWindowMode.History:
                    if ( Window_PlayerHistory.Instance.IsOpen )
                        Window_PlayerHistory.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                    {
                        CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                        Window_PlayerHistory.Instance.Open();
                    }
                    break;
                case MajorWindowMode.RecentEvents:
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        return;

                    if ( Window_RecentEvents.Instance.IsOpen )
                        Window_RecentEvents.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                    {
                        CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                        Window_RecentEvents.Instance.Open();
                    }
                    break;
                case MajorWindowMode.Research:
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        return;

                    if ( Window_PlayerTechnologies.Instance.IsOpen )
                        Window_PlayerTechnologies.Instance.Close( WindowCloseReason.UserCasualRequest );
                    else
                    {
                        if ( !FlagRefs.HasFiguredOutResearch.DuringGameplay_IsTripped )
                            return;

                        CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                        Window_PlayerTechnologies.Instance.Open();
                    }
                    break;
            }
        }
        #endregion

        #region HandleMajorWindowKeyPress
        public static void HandleMajorWindowKeyPress( InputActionTypeData InputActionType )
        {            
            switch ( InputActionType.ID )
            {
                case "ToggleForcesSidebar":
                    ToggleMajorWindowMode( MajorWindowMode.Forces );
                    break;
                case "ToggleCultActionsSidebar":
                    ToggleMajorWindowMode( MajorWindowMode.CultActions );
                    break;
                case "ToggleStructuresWithComplaints":
                    ToggleMajorWindowMode( MajorWindowMode.StructuresWithComplaints );
                    break;
                case "Handbook":
                    ToggleMajorWindowMode( MajorWindowMode.MachineHandbook );
                    break;
                case "Inventory":
                    ToggleMajorWindowMode( MajorWindowMode.Resources );
                    break;
                case "ToggleHardware":
                    ToggleMajorWindowMode( MajorWindowMode.Hardware );
                    break;
                case "ToggleHistory":
                    ToggleMajorWindowMode( MajorWindowMode.History );
                    break;
                case "ToggleVictoryPath":
                    ToggleMajorWindowMode( MajorWindowMode.VictoryPath );
                    break;
                case "RecentEvents":
                    ToggleMajorWindowMode( MajorWindowMode.RecentEvents );
                    break;
                case "ToggleResearch":
                    ToggleMajorWindowMode( MajorWindowMode.Research );
                    break;
                case "GoStraightToNextTurn":
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        break;
                    if ( !FlagRefs.HasUnlockedOuterRadialButtons.DuringGameplay_IsTripped )
                        return;
                    HandleGoToNextTurnOrActor( true );
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
            ArcenInput.BlockForAJustPartOfOneSecond();
        }
        #endregion

        #region CloseAllBigOpenWindows
        public static void CloseAllBigOpenWindows( WindowCloseReason Reason )
        {
            if ( Window_RecentEvents.Instance.IsOpen )
                Window_RecentEvents.Instance.Close( WindowCloseReason.UserDirectRequest );
            if ( Window_Handbook.Instance.IsOpen )
                Window_Handbook.Instance.Close( WindowCloseReason.UserDirectRequest );
            
            if ( Window_StructuresWithProblems.Instance.IsOpen )
                Window_StructuresWithProblems.Instance.Close( Reason );
            if ( Window_PlayerResources.Instance.IsOpen )
                Window_PlayerResources.Instance.Close( Reason );
            if ( Window_PlayerHardware.Instance.IsOpen )
                Window_PlayerHardware.Instance.Close( Reason );
            if ( Window_PlayerHistory.Instance.IsOpen )
                Window_PlayerHistory.Instance.Close( Reason );
            if ( Window_Handbook.Instance.IsOpen )
                Window_Handbook.Instance.Close( Reason );
            if ( Window_PlayerTechnologies.Instance.IsOpen )
                Window_PlayerTechnologies.Instance.Close( Reason );

            if ( Window_VictoryPath.Instance.IsOpen )
                Window_VictoryPath.Instance.Close( Reason );            
        }
        #endregion

        #region GetIsAnyBigWindowOpen
        public static bool GetIsAnyBigWindowOpen()
        {
            if ( Window_RecentEvents.Instance.IsOpen )
                return true;
            if ( Window_Handbook.Instance.IsOpen )
                return true;

            if ( Window_StructuresWithProblems.Instance.IsOpen )
                return true;
            if ( Window_PlayerResources.Instance.IsOpen )
                return true;
            if ( Window_PlayerHardware.Instance.IsOpen )
                return true;
            if ( Window_PlayerHistory.Instance.IsOpen )
                return true;
            if ( Window_Handbook.Instance.IsOpen )
                return true;
            if ( Window_PlayerTechnologies.Instance.IsOpen )
                return true;

            if ( Window_VictoryPath.Instance.IsOpen )
                return true;
            return false;
        }
        #endregion

        #region ToggleHistory_TargetTab
        public static void ToggleHistory_TargetTab( Window_PlayerHistory.HistoryType TargetTab )
        {
            if ( !FlagRefs.UITour1_LeftHeader.DuringGameplay_IsTripped ) //the item right before the right header
                return;

            if ( Window_PlayerHistory.Instance.IsOpen && Window_PlayerHistory.customParent.currentlyRequestedDisplayType == TargetTab )
                Window_PlayerHistory.Instance.Close( WindowCloseReason.UserDirectRequest );
            else
            {
                CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                Window_PlayerHistory.Instance.Open();
                Window_PlayerHistory.customParent.currentlyRequestedDisplayType = TargetTab;
            }
        }
        #endregion


        #region ToggleVictoryPath_TargetTab
        public static void ToggleVictoryPath_TargetTab( Window_VictoryPath.VictoryTabType TargetTab )
        {
            if ( !FlagRefs.UITour1_LeftHeader.DuringGameplay_IsTripped ) //the item right before the right header
                return;

            if ( Window_VictoryPath.Instance.IsOpen && Window_VictoryPath.customParent.currentlyRequestedDisplayType == TargetTab )
                Window_VictoryPath.Instance.Close( WindowCloseReason.UserDirectRequest );
            else
            {
                CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                Window_VictoryPath.Instance.Open();
                Window_VictoryPath.customParent.currentlyRequestedDisplayType = TargetTab;
            }
        }
        #endregion

        #region ToggleResourceWindow_TargetTab
        public static void ToggleResourceWindow_TargetTab( Window_PlayerResources.ResourcesDisplayType TargetTab )
        {
            if ( !FlagRefs.UITour1_LeftHeader.DuringGameplay_IsTripped ) //the item right before the right header
                return;

            if ( Window_PlayerResources.Instance.IsOpen && Window_PlayerResources.customParent.currentlyRequestedDisplayType == TargetTab )
                Window_PlayerResources.Instance.Close( WindowCloseReason.UserDirectRequest );
            else
            {
                CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                Window_PlayerResources.Instance.Open();
                Window_PlayerResources.customParent.currentlyRequestedDisplayType = TargetTab;
            }
        }
        #endregion

        #region ToggleSidebarOfSpecificTab
        public static void ToggleSidebarOfSpecificTab( UISidebarType SidebarTab )
        {
            if ( Window_Sidebar.Instance.IsOpen )
            {
                if ( Window_Sidebar.Instance.CurrentTab == SidebarTab )
                {
                    Window_Sidebar.Instance.Close( WindowCloseReason.UserDirectRequest );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    return;
                }
                else
                    Window_Sidebar.Instance.CurrentTab = SidebarTab;
                ArcenInput.BlockForAJustPartOfOneSecond();
            }
            else
            {
                CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                Window_Sidebar.Instance.CurrentTab = SidebarTab;
                Window_Sidebar.Instance.Open();
                ArcenInput.BlockForAJustPartOfOneSecond();
            }
        }
        #endregion

        #region ToggleSystemMenu
        public static void ToggleSystemMenu()
        {
            if ( Window_SystemMenu.Instance.IsOpen )
                Window_SystemMenu.Instance.Close( WindowCloseReason.UserDirectRequest );
            else
                Window_SystemMenu.Instance.Open();
        }
        #endregion

        #region HandleGoToNextTurnOrActor
        public static void HandleGoToNextTurnOrActor( bool SkipActorsAndGoToNextTurn )
        {
            if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                return;

            if ( InputCaching.NotificationsAreDismissedBeforeSwitchingUnits )
            {
                if ( SimCommon.KeyMessagesWaiting.Count > 0 )
                {
                    SimCommon.KeyMessagesWaiting[0]?.DoToastClick( ToastClickType.AutomatedClose );
                    return;
                }
                else if ( UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 )
                {
                    UnlockCoordinator.MinorCompletedToasts_MainThread[0].DoToastClick( ToastClickType.AutomatedClose );
                    return;
                }
                else if ( UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 )
                {
                    UnlockCoordinator.UnopenedMysteryUnlocks_MainThread[0].DoToastClick( ToastClickType.AutomatedClose );
                    return;
                }
            }

            //if over cap
            if ( SimCommon.TotalCapacityUsed_Androids > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
            {
                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.Androids );
                return;
            }

            //if over cap
            if ( SimCommon.TotalCapacityUsed_Mechs > MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt )
            {
                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.Mechs );
                return;
            }

            //if over cap
            if ( SimCommon.TotalCapacityUsed_Vehicles > MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt )
            {
                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.Vehicles );
                return;
            }

            //if over cap
            if ( SimCommon.TotalBulkUnitSquadCapacityUsed > MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt )
            {
                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.BulkAndroids );
                return;
            }

            //if over cap
            if ( SimCommon.TotalCapturedUnitSquadCapacityUsed > MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt )
            {
                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.CapturedUnits );
                return;
            }

            if ( SkipActorsAndGoToNextTurn )
            {
                if ( SharedRenderManagerData.CurrentIndicator == Indicator.FirstOuterRadialButton )
                    FlagRefs.IndicateFirstOuterRadialButton.UnTripIfNeeded();

                if ( !TryHandleNextMachineActorNeedingToCompleteAnActionOverTime() )
                {
                    if ( !SimCommon.SelectNextUnfinishedNPCOnly() )
                    {
                        if ( !SimCommon.TriggerAllNPCActionsThatAreWaiting() )
                        {
                            if ( SimCommon.KeyMessagesWaiting.Count > 0 )
                                SimCommon.KeyMessagesWaiting[0]?.DoToastClick( ToastClickType.AutomatedClose );
                            else if ( UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 )
                                UnlockCoordinator.MinorCompletedToasts_MainThread[0].DoToastClick( ToastClickType.AutomatedClose );
                            else if ( UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 )
                                UnlockCoordinator.UnopenedMysteryUnlocks_MainThread[0].DoToastClick( ToastClickType.AutomatedClose );
                            else
                            {
                                if ( !OpenResearchWindowIfNeeded() && !OpenSetProjectOutcomeIfNeeded() )
                                {
                                    ExhaustAllMachineActors();
                                    SimCommon.MarkAsReadyForNextTurn();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                bool readyForNextTurn = SimCommon.SelectNextUnfinishedActorOrReturnTrueIfWantsToGoToNextTurn();
                if ( !readyForNextTurn )
                    return;

                if ( SimCommon.KeyMessagesWaiting.Count > 0 )
                    SimCommon.KeyMessagesWaiting[0]?.DoToastClick( ToastClickType.AutomatedClose );
                else if ( UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 )
                    UnlockCoordinator.MinorCompletedToasts_MainThread[0].DoToastClick( ToastClickType.AutomatedClose );
                else if ( UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 )
                    UnlockCoordinator.UnopenedMysteryUnlocks_MainThread[0].DoToastClick( ToastClickType.AutomatedClose );
                else if ( !OpenResearchWindowIfNeeded() && !OpenSetProjectOutcomeIfNeeded() )
                {
                    if ( !TryHandleNextMachineActorNeedingToCompleteAnActionOverTime() )
                    {
                        if ( readyForNextTurn )
                            SimCommon.MarkAsReadyForNextTurn();
                    }
                }
            }
        }
        #endregion

        #region OpenSetProjectOutcomeIfNeeded
        private static bool OpenSetProjectOutcomeIfNeeded()
        {
            if ( SimCommon.ActiveProjects.Count == 0 )
                return false;

            foreach ( MachineProject project in SimCommon.ActiveProjects.GetDisplayList() )
            {
                if ( project.DuringGame_IntendedOutcome != null )
                    continue; //already selected, all good

                if ( project.AvailableOutcomes.Count <= 1 )
                    continue; //skip if there is only one

                SimCommon.RewardProvider = ProjectOutcomeHandler.Start( project );
                SimCommon.OpenWindowRequest = OpenWindowRequest.Reward;
                return true;
            }
            return false;
        }
        #endregion

        #region GetFirstProjectNeedingOutcome
        public static bool GetFirstProjectNeedingOutcome( out MachineProject Result )
        {
            if ( SimCommon.ActiveProjects.Count == 0 )
            {
                Result = null;
                return false;
            }

            foreach ( MachineProject project in SimCommon.ActiveProjects.GetDisplayList() )
            {
                if ( project.DuringGame_IntendedOutcome != null )
                    continue; //already selected, all good

                if ( project.AvailableOutcomes.Count <= 1 )
                    continue; //skip if there is only one

                Result = project;
                return true;
            }
            Result = null;
            return false;
        }
        #endregion

        #region TryHandleNextMachineActorNeedingToCompleteAnActionOverTime
        private static bool TryHandleNextMachineActorNeedingToCompleteAnActionOverTime()
        {
            List<ISimMachineActor> allMachineActors = SimCommon.AllMachineActors.GetDisplayList();
            foreach ( ISimMachineActor machineActor in allMachineActors )
            {
                if ( machineActor.AlmostCompleteActionOverTime != null )
                {
                    Engine_HotM.SetSelectedActor( machineActor, false, false, false );
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region ExhaustAllMachineActors
        private static void ExhaustAllMachineActors()
        {
            //also clear all recent resource trends before we do that
            foreach ( ResourceType resource in ResourceTypeTable.Instance.Rows )
            {
                resource.Trends.ClearRecentNegativeData();
                resource.Trends.ClearRecentPositiveData();
            }
        }
        #endregion

        #region ScrapSelected
        private static List<ISimNPCUnit> npcUnitsToScrap = List<ISimNPCUnit>.Create_WillNeverBeGCed( 200, "VisCommands-npcUnitsToScrap" );

        public static void ScrapSelected()
        {
            if ( !Engine_Universal.GameLoop.IsLevelEditor )
            {
                if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                    return;

                if ( Engine_HotM.SelectedActor == null && SimCommon.IsDeleteAllAttackersEnabled )
                {
                    npcUnitsToScrap.Clear();
                    foreach ( KeyValuePair<int, ISimNPCUnit> kv in World.Forces.GetAllNPCUnitsByID() )
                    {
                        ISimNPCUnit npc = kv.Value;
                        if ( npc == null || npc.IsFullDead || npc.IsInvalid || npc.GetIsPartOfPlayerForcesInAnyWay() )
                            continue;

                        if ( npc.Stance?.IsConsideredEnemyHacker ?? false )
                            npcUnitsToScrap.Add( npc );
                        else
                        {
                            NPCAttackPlan plan = npc?.AttackPlan?.Display;
                            if ( plan != null )
                            {
                                if ( plan.TargetForStartOfNextTurn?.GetIsPartOfPlayerForcesInAnyWay()??false )
                                    npcUnitsToScrap.Add( npc );
                                else if ( plan.SecondaryTargetsDamaged.Count > 0 )
                                {
                                    foreach ( KeyValuePair<ISimMapActor, AttackAmounts> other in plan.SecondaryTargetsDamaged.GetDisplayDict() )
                                    {
                                        if ( other.Key?.GetIsPartOfPlayerForcesInAnyWay()??false )
                                        {
                                            npcUnitsToScrap.Add( npc );
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if ( npcUnitsToScrap.Count > 0 )
                    {
                        foreach ( ISimNPCUnit npc in npcUnitsToScrap )
                            npc.DisbandNPCUnit( NPCDisbandReason.Cheat );
                        npcUnitsToScrap.Clear();
                    }

                    return;
                }

                if ( SimCommon.IsDeleteAnyUnitEnabled && !(Engine_HotM.SelectedActor?.GetIsPlayerControlled()??false) )
                {
                    if ( (Engine_HotM.SelectedActor is MachineStructure structure) )
                    {
                        structure.ScrapStructureNow( ScrapReason.Cheat, Engine_Universal.PermanentQualityRandom );
                    }
                    else if ( Engine_HotM.SelectedActor is ISimMachineActor actor )
                        actor.TryScrapRightNowWithoutWarning_Danger( ScrapReason.Cheat );
                    else if ( Engine_HotM.SelectedActor is ISimNPCUnit npcUnit )
                        npcUnit.DisbandNPCUnit( NPCDisbandReason.Cheat );
                }
                else
                {
                    if ( (Engine_HotM.SelectedActor is MachineStructure structure) )
                    {
                        BuildModeHandler.TryScrapSelectedStructure( structure );
                    }
                    else if ( Engine_HotM.SelectedActor is ISimMachineActor actor )
                    {
                        if ( SimCommon.Turn <= 1 && SimMetagame.CurrentChapterNumber <= 2 )
                        {
                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                            return;
                        }

                        if ( !actor.PopupReasonCannotScrapIfCannot( ScrapReason.ArbitraryPlayer ) )
                        {
                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                            return;
                        }
                        if ( actor.GetIsBlockedFromBeingScrappedRightNow() )
                        {
                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                            return;
                        }

                        if ( actor.GetIsOverCap() )
                            actor.TryScrapRightNowWithoutWarning_Danger( ScrapReason.ArbitraryPlayer );
                        else
                        {
                            ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                            {
                                actor.TryScrapRightNowWithoutWarning_Danger( ScrapReason.ArbitraryPlayer );
                            }, null, LocalizedString.AddFormat1_New( "ScrapUnit_Header", actor.GetDisplayName() ),
                                    LocalizedString.AddFormat1_New( "ScrapUnit_Body", actor.GetDisplayName() ),
                                    LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                        }
                    }
                    else if ( Engine_HotM.SelectedActor is ISimNPCUnit npcUnit && npcUnit.GetIsPlayerControlled() )
                    {
                        if ( npcUnit.UnitType.GetIsOverCapIfBelongsToPlayer() )
                            npcUnit.DisbandNPCUnit( NPCDisbandReason.PlayerRequestForOwnUnit );
                        else
                        {
                            ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                            {
                                npcUnit.DisbandNPCUnit( NPCDisbandReason.PlayerRequestForOwnUnit );
                            }, null, LocalizedString.AddFormat1_New( "ScrapUnit_Header", npcUnit.GetDisplayName() ),
                                    LocalizedString.AddFormat1_New( "ScrapUnit_Body", npcUnit.GetDisplayName() ),
                                    LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                        }
                    }
                }
            }
        }
        #endregion

        public static bool OpenResearchWindowIfNeeded()
        {
            if ( !FlagRefs.HasFiguredOutResearch.DuringGameplay_IsTripped )
                return false;

            if ( UnlockCoordinator.CurrentUnlockResearch != null )
                return false;
            else
            {
                if ( UnlockCoordinator.CurrentResearchDomain != null )
                    return false;
                else
                {
                    if ( UnlockCoordinator.GetHasAnyFullyReadyResearch() )
                    {
                        if ( !Window_PlayerTechnologies.Instance.IsOpen )
                            Window_PlayerTechnologies.Instance.Open();
                        return true;
                    }
                    else
                        return false;
                }
            }
        }

        #region ToggleNetworksSidebar
        public static void ToggleNetworksSidebar()
        {
            if ( SimMetagame.CurrentChapterNumber <= 0 )
                return;
            ToggleSidebarOfSpecificTab( CommonRefs.NetworksSidebar );
        }
        #endregion

        #region ToggleTimelinesSidebar
        public static void ToggleTimelinesSidebar()
        {
            if ( Engine_HotM.GameMode != MainGameMode.TheEndOfTime )
                return;
            ToggleSidebarOfSpecificTab( CommonRefs.TimelinesSidebar );
        }
        #endregion

        #region ToggleDeepCheatSidebar
        public static void ToggleDeepCheatSidebar()
        {
            if ( !SimCommon.IsCheatTimeline )
                return;
            ToggleSidebarOfSpecificTab( CommonRefs.DeepCheatSidebar );
        }
        #endregion

        #region BlankThingsOnAnyActionModeChange
        private static void BlankThingsOnAnyActionModeChange()
        {
            Engine_HotM.CurrentCommandModeActionTargeting = null;
            BuildModeHandler.ClearAllTargeting();
        }
        #endregion

        #region ToggleBuildMode
        public static void ToggleBuildMode()
        {
            BlankThingsOnAnyActionModeChange();

            if ( Engine_HotM.SelectedMachineActionMode == CommonRefs.BuildMode )
                Engine_HotM.SelectedMachineActionMode = null;
            else
            {
                Engine_HotM.SetSelectedActor( null, false, false, false );
                Engine_HotM.SelectedMachineActionMode = CommonRefs.BuildMode;

                if ( SharedRenderManagerData.CurrentIndicator == Indicator.BuildMenuButton )
                    FlagRefs.IndicateBuildMenuButton.UnTripIfNeeded();
            }
        }
        #endregion

        #region ToggleCommandMode
        public static void ToggleCommandMode()
        {
            BlankThingsOnAnyActionModeChange();

            if ( Engine_HotM.SelectedMachineActionMode == CommonRefs.CommandMode )
                Engine_HotM.SelectedMachineActionMode = null;
            else
            {
                Engine_HotM.SelectedMachineActionMode = CommonRefs.CommandMode;

                if ( FlagRefs.IndicateCommandModeButton.DuringGameplay_IsTripped )
                    FlagRefs.IndicateCommandModeButton.UnTripIfNeeded();
            }
        }
        #endregion

        #region ToggleStanceWindow
        public static bool ToggleStanceWindow()
        {
            if ( !(Engine_HotM.SelectedActor is ISimMapMobileActor Actor) )
                return false;

            if ( Actor is ISimMachineUnit Unit )
            {
                if ( Unit.Stance?.BlocksSwitchingFromThisStanceUnlessInvalid ?? false )
                {
                    if ( !Unit.GetIsCurrentStanceInvalid() )
                        return false; //cannot swap from this stance unless it is invalid
                }

                if ( Unit.UnitType.IsConsideredAndroid && MachineUnitStanceTable.AndroidStanceCount <= 1 )
                    return false; //not enough stances to swap to
                if ( Unit.UnitType.IsConsideredMech && MachineUnitStanceTable.MechStanceCount <= 1 )
                    return false; //not enough stances to swap to
            }

            if ( Actor is ISimMachineVehicle Vehicle )
            {
                if ( MachineVehicleStanceTable.VehicleStanceCount <= 1 )
                    return false; //not enough stances to swap to
            }

            if ( Actor is ISimNPCUnit npcUnit && !npcUnit.GetIsPlayerControlled() )
                return false; //cannot swap stances if not npc-controlled

            Window_ActorStanceChange.Instance.ActorToEdit = Actor;
            if ( Window_ActorStanceChange.Instance.IsOpen )
                Window_ActorStanceChange.Instance.Close( WindowCloseReason.UserDirectRequest );
            else
            {
                Window_ActorCustomizeLoadout.Instance.Close( WindowCloseReason.UserDirectRequest );
                Window_StructureChoiceList.Instance.Close( WindowCloseReason.UserDirectRequest );
                Window_ScrapUnitList.Instance.Close( WindowCloseReason.OtherWindowCausingClose );
                Window_ActorStanceChange.Instance.Open();
            }
            return true;
        }
        #endregion

        #region GoToStreetsMode
        public static void GoToStreetsMode()
        {
            if ( VisCurrent.IsInPhotoMode )
                return; //do nothing, must exit photo mode first

            if ( Engine_HotM.CurrentLowerMode != null )
            {
                if ( !Engine_HotM.CurrentLowerMode.ClosesLikeAWindow && !Engine_HotM.CurrentLowerMode.DoesNotCloseFromHotkeys )
                    Engine_HotM.CurrentLowerMode = null;
                return;
            }

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.Streets:
                    break; //nothing to do
                default:
                    Engine_HotM.SetGameMode( MainGameMode.Streets );
                    break;
            }
        }
        #endregion

        #region ToggleCityMapMode
        public static void ToggleCityMapMode()
        {
            if ( VisCurrent.IsInPhotoMode )
                return; //do nothing, must exit photo mode first

            if ( Engine_HotM.CurrentLowerMode != null )
            {
                if ( !Engine_HotM.CurrentLowerMode.ClosesLikeAWindow && !Engine_HotM.CurrentLowerMode.DoesNotCloseFromHotkeys )
                    Engine_HotM.CurrentLowerMode = null;
                return;
            }

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.CityMap:
                    VisManagerMidBase.Instance.MainCamera_JumpStreetsToCityMapPosition();
                    Engine_HotM.SetGameMode( MainGameMode.Streets );
                    break;
                default:
                    VisManagerMidBase.Instance.MainCamera_JumpCityMapToStreetsPosition();
                    Engine_HotM.SetGameMode( MainGameMode.CityMap );

                    if ( SharedRenderManagerData.CurrentIndicator == Indicator.MapButton_Investigate )
                        FlagRefs.IndicateMapButton.UnTripIfNeeded();
                    break;
            }
        }
        #endregion

        #region GetCanSeeEndOfTime
        public static bool GetCanSeeEndOfTime( bool IgnoreCheatCondition, bool IgnoreProjectCompletionWindow )
        {
            bool value = ( !IgnoreCheatCondition && SimCommon.IsCheatTimeline ) || FlagRefs.HasUnlockedTheEndOfTime.DuringGameplay_IsTripped;
            if ( value )
            {
                if ( !IgnoreProjectCompletionWindow )
                {
                     if ( Window_ProjectComplete.Instance.GetShouldDrawThisFrame() )
                        return false;
                    if ( !SimCommon.QueuedGoalCompletionFanfare.IsEmpty )
                        return false;
                    if ( !SimCommon.QueuedProjectCompletionFanfare.IsEmpty )
                        return false;
                }
            }
            return value;
        }
        #endregion

        #region ToggleEndOfTimeMode
        public static void ToggleEndOfTimeMode()
        {
            if ( VisCurrent.IsInPhotoMode )
                return; //do nothing, must exit photo mode first

            if ( !GetCanSeeEndOfTime( false, false ) )
                return;

            if ( Engine_HotM.CurrentLowerMode != null )
            {
                if ( !Engine_HotM.CurrentLowerMode.ClosesLikeAWindow && !Engine_HotM.CurrentLowerMode.DoesNotCloseFromHotkeys )
                    Engine_HotM.CurrentLowerMode = null;
                return;
            }

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    Engine_HotM.SetGameMode( MainGameMode.Streets );
                    break;
                default:
                    Engine_HotM.SetGameMode( MainGameMode.TheEndOfTime );
                    break;
            }
        }
        #endregion

        #region ToggleTheVirtualWorld
        public static void ToggleTheVirtualWorld()
        {
            if ( VisCurrent.IsInPhotoMode )
                return; //do nothing, must exit photo mode first

            if ( !SimCommon.IsCheatTimeline && !FlagRefs.HasUnlockedSeeingTheVirtualWorld.DuringGameplay_IsTripped )
                return;

            if ( Engine_HotM.CurrentLowerMode == CommonRefs.ZodiacPodScene )
                Engine_HotM.CurrentLowerMode = null;
            else
                Engine_HotM.CurrentLowerMode = CommonRefs.ZodiacPodScene;
        }
        #endregion

        #region TryDeselectCurrentActorOrOtherwiseStepBackOne
        public static bool TryDeselectCurrentActorOrOtherwiseStepBackOne()
        {
            if ( Engine_HotM.SelectedActor != null )
            {
                if ( Engine_HotM.SelectedActor is ISimMachineUnit unit )
                {
                    ISimMachineVehicle vehicleContainer = unit.ContainerLocation.Get() as ISimMachineVehicle;
                    if ( vehicleContainer != null )
                    {
                        Engine_HotM.SetSelectedActor( vehicleContainer, false, true, true );
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return true; //if we were selecting a unit that is in a vehicle, then select the vehicle again after canceling the unit selection
                    }

                    //do NOT close ability targeting modes before deselecting!  That feels very awkward
                    //if ( unit.IsInAbilityTypeTargetingMode != null )
                    //{
                    //    unit.SetTargetingMode( null, null );
                    //    ArcenInput.BlockForAJustPartOfOneSecond();
                    //    return true; //if in targeting mode, close that before deselecting
                    //}
                }

                if ( Engine_HotM.SelectedActor is ISimMachineVehicle vehicle )
                {
                    //do NOT close ability targeting modes before deselecting!  That feels very awkward
                    //if ( vehicle.IsInAbilityTypeTargetingMode != null )
                    //{
                    //    vehicle.SetTargetingMode( null, null );
                    //    ArcenInput.BlockForAJustPartOfOneSecond();
                    //    return true; //if in targeting mode, close that before deselecting
                    //}
                }
                Engine_HotM.SetSelectedActor( null, false, false, false );
                ArcenInput.BlockForAJustPartOfOneSecond();
                return true;
            }
            return false;
        }
        #endregion

        #region HandleQuitToMainMenu
        public static void HandleQuitToMainMenu()
        {
            if ( GameSettings.Current.GetBool( "AllowExitingWithoutSaving" ) )
            {
                //ask if they want to save and exit
                ModalPopupData.CreateAndLogYesNoThirdStyle( delegate
                {
                    //save and exit
                    if ( !WorldSaveLoad.SaveGameOnBackgroundThreadIfNotAlreadySaving( SimMetagame.ProfileName, Lang.Format2( "ExitSaveFormat", SimCommon.CityName, SimCommon.Turn ), SaveType.Autosave, AfterSave.QuitToMainMenu ) )
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                            LocalizedString.AddLang_New( "CouldNotSaveGame_Header" ),
                            LocalizedString.AddLang_New( "CouldNotSaveGame_Body" ), LangCommon.Popup_Common_Ok.LocalizedString );
                }, delegate
                {
                    //no save, just exit
                    WorldSaveLoad.QuitToMainMenu();
                }, null, LocalizedString.AddLang_New( "Popup_AreYouSure" ), LocalizedString.AddLang_New( "SysMenu_QuitGame_Confirm" ),
                    LocalizedString.AddLang_New( "Popup_Common_SaveAndExit" ),
                    LocalizedString.AddLang_New( "Popup_Common_ExitOnly" ),
                    LocalizedString.AddLang_New( "Nevermind_QuitGame" ) );
            }
            else
            {
                //ask if they are sure
                ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Normal, delegate
                {
                    //save and exit
                    if ( !WorldSaveLoad.SaveGameOnBackgroundThreadIfNotAlreadySaving( SimMetagame.ProfileName, Lang.Format2( "ExitSaveFormat", SimCommon.CityName, SimCommon.Turn ), SaveType.Autosave, AfterSave.QuitToMainMenu ) )
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                            LocalizedString.AddLang_New( "CouldNotSaveGame_Header" ),
                            LocalizedString.AddLang_New( "CouldNotSaveGame_Body" ), LangCommon.Popup_Common_Ok.LocalizedString );
                }, null, LocalizedString.AddLang_New( "Popup_AreYouSure" ), LocalizedString.AddLang_New( "SysMenu_QuitGame_Confirm" ),
                    LocalizedString.AddLang_New( "Popup_Common_SaveAndExit" ),
                    LocalizedString.AddLang_New( "Nevermind_QuitGame" ) );
            }
        }
        #endregion

        #region HandleExitToOS
        public static void HandleExitToOS()
        {
            if ( GameSettings.Current.GetBool( "AllowExitingWithoutSaving" ) )
            {
                //ask if they want to save and exit
                ModalPopupData.CreateAndLogYesNoThirdStyle( delegate
                {
                    //save and exit
                    if ( !WorldSaveLoad.SaveGameOnBackgroundThreadIfNotAlreadySaving( SimMetagame.ProfileName, Lang.Format2( "ExitSaveFormat", SimCommon.CityName, SimCommon.Turn ), SaveType.Autosave, AfterSave.ExitToOS ) )
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                            LocalizedString.AddLang_New( "CouldNotSaveGame_Header" ),
                            LocalizedString.AddLang_New( "CouldNotSaveGame_Body" ), LangCommon.Popup_Common_Ok.LocalizedString );
                }, delegate
                {
                    //no save, just exit
                    WorldSaveLoad.ExitToOS();
                }, null, LocalizedString.AddLang_New( "Popup_AreYouSure" ),
                    LocalizedString.AddLang_New( "SysMenu_ExitToDesktop_Confirm" ),
                    LocalizedString.AddLang_New( "Popup_Common_SaveAndExit" ),
                    LocalizedString.AddLang_New( "Popup_Common_ExitOnly" ),
                    LocalizedString.AddLang_New( "Nevermind_QuitGame" ) );
            }
            else
            {
                //ask if they are sure
                ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Normal, delegate
                {
                    //save and exit
                    if ( !WorldSaveLoad.SaveGameOnBackgroundThreadIfNotAlreadySaving( SimMetagame.ProfileName, Lang.Format2( "ExitSaveFormat", SimCommon.CityName, SimCommon.Turn ), SaveType.Autosave, AfterSave.ExitToOS ) )
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                            LocalizedString.AddLang_New( "CouldNotSaveGame_Header" ),
                            LocalizedString.AddLang_New( "CouldNotSaveGame_Body" ), LangCommon.Popup_Common_Ok.LocalizedString );
                }, null, LocalizedString.AddLang_New( "Popup_AreYouSure" ),
                    LocalizedString.AddLang_New( "SysMenu_ExitToDesktop_Confirm" ),
                    LocalizedString.AddLang_New( "Popup_Common_SaveAndExit" ),
                    LocalizedString.AddLang_New( "Nevermind_QuitGame" ) );
            }
        }
        #endregion

        #region Performance_LastTurn_Lang
        public static void Performance_LastTurn_Lang( ArcenCharacterBufferBase Buffer, TimingInfoData Info, string LangKey )
        {
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ( Info.LastTicks / 10000f ).ToStringSmallFixedDecimal( 1 )  )
                .EndColor().Space1x().AddLang( "SysMenu_Timings_Last" ).Position100(); ;
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ( Info.LongestRecentTicks / 10000f).ToStringSmallFixedDecimal( 1 ) )
                .AddNeverTranslated( " (", true ).AddRaw( ( Info.LongestEverTicks / 10000f).ToStringSmallFixedDecimal( 1 ) ).AddNeverTranslated( ")", true )
                .EndColor().Space1x().AddNeverTranslated( SMALLER_SIZE, false ).AddLang( LangKey ).EndSize();
        }
        #endregion

        #region Performance_LastTurn_NeverTranslated
        public static void Performance_LastTurn_NeverTranslated( ArcenCharacterBufferBase Buffer, TimingInfoData Info, string RawLabel )
        {
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ( Info.LastTicks / 10000f).ToStringSmallFixedDecimal( 1 ) )
                .EndColor().Space1x().AddLang( "SysMenu_Timings_Last" ).Position100(); ;
            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ( Info.LongestRecentTicks / 10000f).ToStringSmallFixedDecimal( 1 ) )
                .AddNeverTranslated( " (", true ).AddRaw( ( Info.LongestEverTicks / 10000f).ToStringSmallFixedDecimal( 1 ) ).AddNeverTranslated( ")", true )
                .EndColor().Space1x().AddNeverTranslated( SMALLER_SIZE, false ).AddNeverTranslated( RawLabel, true ).EndSize();
        }
        #endregion

        #region Performance_LastTurnCount_NeverTranslated
        public static void Performance_LastTurnCount_NeverTranslated( ArcenCharacterBufferBase Buffer, TimingInfoData Info, string RawLabel )
        {
            Buffer.Line().Position20();
            Buffer.StartColor( ColorTheme.PurpleDim ).AddRaw( Info.LastTicks.ToStringThousandsWhole() )
                .EndColor().Space1x().AddNeverTranslated( "Last", true ).Position100(); ;
            Buffer.StartColor( ColorTheme.PurpleDim ).AddRaw( Info.LongestRecentTicks.ToStringThousandsWhole() )
                .AddNeverTranslated( " (", true ).AddRaw( Info.LongestEverTicks.ToStringThousandsWhole() ).AddNeverTranslated( ")", true )
                .EndColor().Space1x().AddNeverTranslated( SMALLER_SIZE, false ).AddNeverTranslated( RawLabel, true ).EndSize();
        }
        #endregion
    }

    public enum MajorWindowMode
    {
        Forces,
        StructuresWithComplaints,
        MachineHandbook,
        Resources,
        Hardware,
        History,
        RecentEvents,
        Research,
        CultActions,
        VictoryPath
    }
}
