using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LoadingWindow : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_LoadingWindow Instance;
        public Window_LoadingWindow()
        {
            Instance = this;
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            return VisCurrent.GetShouldShowAnimatedLoadingWindow();
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            public override void OnUpdate()
            {
                if ( CityMap.GetIsAddingMoreItems() || WorldSaveLoad.IsLoadingAtTheMoment || !SimCommon.HasFullyStarted )
                    TimeLastSeenLoading = ArcenTime.AnyTimeSinceStartF;
            }
        }

        private static float TimeStartedShowing = 0;
        private static float TimeLastSeenLoading = 0;
        public override void ChildOnShowAfterNotShowing()
        {
            TimeStartedShowing = ArcenTime.AnyTimeSinceStartF;
            TimeLastSeenLoading = ArcenTime.AnyTimeSinceStartF;
        }

        public override void OnHideAfterNotShowing()
        {
            //make sure this always happens
            UnityEngine.Time.timeScale = 1;
        }

        #region bContinue
        public class bContinue : ButtonAbstractBase
        {
            private float lastTimeChanged = 0;
            private int colorIndexToDraw = 0;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( !CityMap.GetIsAddingMoreItems() && !WorldSaveLoad.IsLoadingAtTheMoment && SimCommon.HasFullyStarted )
                {
                    if ( ArcenTime.AnyTimeSinceStartF - lastTimeChanged > 0.1f )
                    {
                        lastTimeChanged = ArcenTime.AnyTimeSinceStartF;
                        colorIndexToDraw++;
                        if ( colorIndexToDraw > 3 )
                            colorIndexToDraw = 0;
                    }
                    string colorToDraw = "c8e4f2";
                    switch ( colorIndexToDraw )
                    {
                        case 1:
                            colorToDraw = "b7daec";
                            break;
                        case 2:
                            colorToDraw = "aacde0";
                            break;
                        case 3:
                            colorToDraw = "b6cbd6";
                            break;
                    }
                    Buffer.AddLang( "LoadingScreen_Ready", colorToDraw );
                }
                //else
                //{
                //    Buffer.AddLang( LangCommon.Popup_Common_Cancel );
                //}
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( CityMap.GetIsAddingMoreItems() || WorldSaveLoad.IsLoadingAtTheMoment || !SimCommon.HasFullyStarted )
                {
                    ////these are for debugging purposes
                    //ArcenDebugging.LogSingleLine( "CityMap.IsCurrentlyAddingMoreMapTiles: " + CityMap.IsCurrentlyAddingMoreMapTiles, Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( "CityMap.Cells.Count: " + CityMap.Cells.Count, Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( "CityMap.TilesToPostProcess_Structural.Count: " + CityMap.TilesToPostProcess_Structural.Count, Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( "CityMap.TilesToPostProcess_Decoration.Count: " + CityMap.TilesToPostProcess_Decoration.Count, Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( "CityMap.WorkingTilesToPostProcess_Decoration.Count: " + CityMap.WorkingTilesToPostProcess_Decoration.Count, Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( "CityMap.TilesBeingPostProcessed_Structural: " + CityMap.TilesBeingPostProcessed_Structural, Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( "CityMap.TilesBeingPostProcessed_Decorations: " + CityMap.TilesBeingPostProcessed_Decorations, Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( "CityMap.GetIsMapStillGeneratingForSimPurposes(): " + CityMap.GetIsMapStillGeneratingForSimPurposes(), Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( "AbstractSimQueries.Instance.GetHasInitiatedStart(): " + World.CityData.GetHasInitiatedStart(), Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( "World.CityStoryState.GetHasFullyStarted(): " + SimCommon.HasFullyStarted, Verbosity.DoNotShow );

                    ////if there's a deadlock, this would give us more info, but also it just stops things
                    //ArcenThreading.AbortAllThreads( false );

                    ////do not normally use this directly!  We're using it directly so that we can have a callback on a background thread even when threading is being taken down
                    //System.Threading.Tasks.Task.Run( () =>
                    //{
                    //    System.Threading.Thread.Sleep( 300 ); //make sure this doesn't kick in before the other bits happen
                    //    Engine_Universal.QuitGameAndGoBackToMainMenu();
                    //} );

                    return MouseHandlingResult.None;
                }

                DoContinue();
                return MouseHandlingResult.None;
            }

            public static void DoContinue()
            {
                if ( CityMap.GetIsAddingMoreItems() || WorldSaveLoad.IsLoadingAtTheMoment || !SimCommon.HasFullyStarted )
                    return; //can't continue!
                UnityEngine.Time.timeScale = 1;
            }

            public override void HandleMouseover()
            {
            }
        }
        #endregion

        #region tLoadingText
        public class tLoadingText : TextAbstractBase
        {
            private float lastTimeChanged = 0;
            private int periodsToDraw = 0;
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                //Note: this text is drawn in a mostly-incomprehensible font, and should not be localized
                if ( ArcenTime.AnyTimeSinceStartF - lastTimeChanged > 0.5f )
                {
                    lastTimeChanged = ArcenTime.AnyTimeSinceStartF;
                    periodsToDraw++;
                    if ( periodsToDraw > 3 )
                        periodsToDraw = 0;
                }
                //Note: this is never translated because it's drawn in a crazy font and not readable even in english on purpose
                Buffer.AddNeverTranslated( "LOADING", true );
                switch ( periodsToDraw )
                {
                    case 3:
                        Buffer.AddNeverTranslated( "...", true );
                        break;
                    case 2:
                        Buffer.AddNeverTranslated( "..", true );
                        break;
                    case 1:
                        Buffer.AddNeverTranslated( ".", true );
                        break;
                    default:
                    case 0:
                        break;
                }
            }
        }
        #endregion

        #region tInformationText
        public class tInformationText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "LoadingScreen_Time" ).Space1x().AddRaw( ( TimeLastSeenLoading - TimeStartedShowing ).ToString_TimeSeconds() );
                if ( WorldSaveLoad.IsLoadingAtTheMoment )
                {
                    Buffer.Line().AddLang( "LoadingScreen_Existing", ColorTheme.RustCream );
                }
                else
                {
                    if ( CityMap.IsCurrentlyAddingMoreMapTiles > 0 )
                        Buffer.Line().AddLangAndAfterLineItemHeader( "LoadingScreen_GeneratingMoreMapTiles", ColorTheme.RustCream ).AddRaw( CityMap.Tiles.Count.ToString(), ColorTheme.RustLighter );
                    else
                        Buffer.Line().AddLangAndAfterLineItemHeader( "LoadingScreen_MapTilesGenerated", ColorTheme.RustDarker ).AddRaw( CityMap.Tiles.Count.ToString(), ColorTheme.RustLighter );
                    Buffer.Line().AddLangAndAfterLineItemHeader( "LoadingScreen_FilledMapCells", ColorTheme.RustDarker ).AddRaw( CityMap.GetFilledMapCellsCount().ToString(), ColorTheme.RustLighter );
                    if ( !CityMap.GetIsAddingMoreItems() )
                    {
                        Buffer.Line().AddLang( "LoadingScreen_PreparingBuildings" );
                        if ( SimCommon.HasFullyStarted )
                            Buffer.Line().AddLang( "LoadingScreen_Done" );
                    }
                }
            }
        }
        #endregion

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Cancel":
                case "Return":
                    //make sure no other input is processed for 0.2 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    bContinue.DoContinue();
                    break;
                //default: no cutthrough o this one!
                //    InputWindowCutthrough.HandleKey( InputActionType.ID );
                //    break;
            }
        }
    }
}
