using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LevelEditorSystemMenu : ToggleableWindowController, IInputActionHandler
    {
        public static Window_LevelEditorSystemMenu Instance;
        public Window_LevelEditorSystemMenu()
        {
            Instance = this;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !Engine_Universal.GameLoop.IsLevelEditor )
                return false; //never show if not the level editor
            return base.GetShouldDrawThisFrame_Subclass();
        }

        public override void OnOpen()
        {
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            public override void OnUpdate()
            {

            }
        }

        public class bResume : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "SysMenu_Resume" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_LevelEditorSystemMenu.Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        public class bSettings : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "MainMenu_Settings" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_SettingsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }
        }

        public class bControls : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "MainMenu_Controls" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ControlBindingsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }
        }

        public class bExitToOS : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "SysMenu_ExitToDesktop" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                {
                    WorldSaveLoad.ExitToOS();
                }, null, LocalizedString.AddLang_New( "Popup_AreYouSure" ), LocalizedString.AddLang_New( "SysMenu_ExitToDesktop_Confirm" ), 
                LangCommon.Popup_Common_YesExit.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );

                return MouseHandlingResult.None;
            }
        }

        public class tLeftHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "SysMenu_LevelEditor" );
            }
        }

        public class tRightHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "SysMenu_LevelEditor_OtherInfo" );
            }
        }

        public class tAboutContent : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );

                int debugStage = 0;
                try
                {
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
                        //game is not multiplayer right now, so not bothering to translate this stuff

                        Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).AddNeverTranslated( "<b>Multiplayer Stats:</b> ", true ).EndColor();
                        //ROW 1
                        Buffer.Line().Position20();
                        Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddNeverTranslated( ArcenNetworkLogging.OverallMessagesReceived.ToStringThousandsWhole(), true ).EndColor().AddNeverTranslated( " Received Overall", true ).Position200();
                        Buffer.AddBytesWithFormatAndColor( ColorTheme.PerformanceStatsHeaderPurple, ArcenNetworkLogging.OverallBytesReceived ).AddNeverTranslated( " Overall Received", true );
                        //ROW 2
                        Buffer.Line().Position20();
                        Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddNeverTranslated( ArcenNetworkLogging.Overall.MessagesSent.ToStringThousandsWhole(), true ).EndColor().AddNeverTranslated( " Sent Overall", true ).Position200();
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
                            Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddNeverTranslated( vol.MessagesSent.ToStringThousandsWhole(), true ).EndColor().AddNeverTranslated( " Sent ", true ).AddNeverTranslated( name, true ).Position200();
                            Buffer.AddBytesWithFormatAndColor( ColorTheme.PerformanceStatsHeaderPurple, vol.BytesSent ).AddNeverTranslated( " Sent ", true ).AddNeverTranslated( name, true );
                        }

                        //Engine_HotM.WriteNetworkingDetailedStats( Buffer, false );

                        Buffer.Line();
                    }
                    #endregion

                    #region Current Game Performance
                    Buffer.StartBold().AddLangAndAfterLineItemHeader( "PerformanceHeader", ColorTheme.PerformanceStatsSectionHeaderOrange ).EndBold();
                    debugStage = 410;
                    //if ( World.Instance.IsPaused )
                    //    Buffer.StartColor( ColorTheme.PerformanceStatsSectionHeaderOrange ).Add( " - PAUSED" ).EndColor();
                    //else
                    //    Buffer.StartColor( QuickColors.Danger ).Add( " - RUNNING" ).EndColor();

                    Buffer.Space1x().AddFormat1( "FramerateParenthetical", ArcenTimeTracker.CurrentFramesPerSecond.ToStringWholeBasic() );

                    Buffer.Line();
                    debugStage = 420;

                    VisCommands.WriteGraphicsPerformance( Buffer );

                    Buffer.Line2x();

                    debugStage = 600;

                    Buffer.StartBold().AddLangAndAfterLineItemHeader( "MemoryPoolingHeader", ColorTheme.PerformanceStatsSectionHeaderOrange ).EndBold();
                    //ROW 1
                    Buffer.Line().Position20();
                    Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( GC.CollectionCount( 0 ).ToStringThousandsWhole() ).EndColor().Space1x().AddLang( "Perf_GCCalls" ).Position200();
                    Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( (GC.GetTotalMemory( false ) / 1024f / 1024f).ToStringThousandsDecimal() ).EndColor().Space1x().AddLang( "Perf_GCRAM" );
                    ////ROW 1A
                    //Buffer.Line().Position20();
                    //Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ArcenDoubleCharacterBuffer.MatchesSavedAndReturned.ToStringThousandsWhole() ).EndColor().Add( " Saved Texts" ).Position200()
                    //Buffer.StartColor( ColorTheme.PerformanceStatsHeaderPurple ).AddRaw( ArcenDoubleCharacterBuffer.NewNonMatchesReturned.ToStringThousandsWhole() ).EndColor().Add( " New Texts" );

                    //ROW 10
                    if ( GameSettings.Current.GetBool( "Debug_ShowPoolCountsInEscapeMenu" ) )
                    {
                        //not translated because this is just a debug thing
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
                        //not translated because this is just a debug thing
                        Buffer.Line().Position40().StartColor( ColorTheme.HeaderLighterBlue ).AddNeverTranslated( "TIME_BASED POOL DETAILS", true ).EndColor();
                        foreach ( TimeBasedPoolBase pool in TimeBasedPoolBase.AllTimeBasedPools )
                        {
                            Buffer.Line().StartColor( ColorTheme.HeaderReward_Sub ).Position40().AddRawAndAfterLineItemHeader( pool.PoolNameOrig ).AddRaw( pool.ItemsCreated.ToStringThousandsWhole() ).EndColor();
                            Buffer.Line().Position100().AddNeverTranslated( "Items Put Back: ", true ).AddRaw( pool.TotalItemsPutBackInPools.ToStringThousandsWhole() );
                            Buffer.Line().Position100().AddNeverTranslated( "Number Raw Created: ", true ).AddRaw( pool.NumberRawCreated.ToStringThousandsWhole() );
                            Buffer.Line().Position100().AddNeverTranslated( "Number Created Or Pool-Requested: ", true ).AddRaw( pool.NumberCreatedOrPoolRequested.ToStringThousandsWhole() );
                            Buffer.Line().Position100().AddNeverTranslated( "Total Items In All Sub-Pools Right Now: ", true ).AddRaw( pool.CalculateTotalItemsInAllPoolsRightNow().ToStringThousandsWhole() );
                        }
                    }

                    Buffer.Line2x();
                    #endregion

                    debugStage = 1000;
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogDebugStageWithStack( "Error at about campaign", debugStage, e, Verbosity.ShowAsError );
                }
            }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    //make sure no other input is processed for 0.2 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    this.Close( WindowCloseReason.UserDirectRequest );
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}
