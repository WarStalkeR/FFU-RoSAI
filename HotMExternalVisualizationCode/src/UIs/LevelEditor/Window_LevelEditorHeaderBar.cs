using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using System.IO;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LevelEditorHeaderBar : WindowControllerAbstractBase
    {
        public static readonly Color Color_Dark = ColorMath.HexToColor( "333333" );
        public static readonly Color Color_VeryDark = ColorMath.HexToColor( "000000" );

        public static Window_LevelEditorHeaderBar Instance;
        public Window_LevelEditorHeaderBar()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !Engine_Universal.GameLoop.IsLevelEditor )
                return false; //never show if not the level editor
            return true;// base.GetShouldDrawThisFrame_Subclass();
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            public override void OnUpdate()
            {
                #region Calculate IsLevelEditorInputDisabledByModalPopup
                bool isLevelEditorInputDisabled = false;
                if ( !isLevelEditorInputDisabled )
                    isLevelEditorInputDisabled = Window_ModalBlocker.ShouldShowModalBlocker;
                if ( !isLevelEditorInputDisabled )
                    isLevelEditorInputDisabled = Engine_Universal.CurrentPopups.Count >= 1;
                if ( !isLevelEditorInputDisabled )
                    isLevelEditorInputDisabled = Window_LevelEditorSystemMenu.Instance.GetShouldDrawThisFrame();
                if ( !isLevelEditorInputDisabled )
                    isLevelEditorInputDisabled = Window_ControlBindingsMenu.Instance.GetShouldDrawThisFrame();
                if ( !isLevelEditorInputDisabled )
                    isLevelEditorInputDisabled = Window_SettingsMenu.Instance.GetShouldDrawThisFrame();

                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsLevelEditorInputDisabledByModalPopup = isLevelEditorInputDisabled;
                #endregion

                //grab the path that was loaded from the front-end
                if ( Engine_HotM.LastFullLevelEditorLoadedFromFrontEnd.Length > 0 )
                {
                    LastSaveOrLoadPath = Engine_HotM.LastFullLevelEditorLoadedFromFrontEnd;
                    Engine_HotM.LastFullLevelEditorLoadedFromFrontEnd = string.Empty;
                }
            }
        }

        private static string _lsORLP = string.Empty;
        public static string LastSaveOrLoadPath
        {
            get { return _lsORLP; }
            set
            {
                if ( value == _lsORLP )
                    return;
                if ( value != null && value.Length > 0 )
                    _lsORLP = value.Replace( @"\", "/" );
                else
                    _lsORLP = value;

                if ( _lsORLP != null && _lsORLP.Length > 0 && GameSettings.Current != null )
                {
                    string filePath = _lsORLP;
                    int indexOf = filePath.IndexOf( "LevelEditorOutput/" );
                    filePath = filePath.Substring( indexOf );
                    //results should look like "LevelEditorOutput/ParkAndHighway.lvl" or similar

                    GameSettings.Current.SetString( "LevelEditorLastFile", filePath );
                    GameSettings.SaveToDisk();
                }
            }
        }

        #region RequiredPathBase
        /// <summary>
        /// Given the LevelType, what is the folder we have to be in?
        /// </summary>
        public static string RequiredPathBase
        {
            get
            {
                string pathBase = Engine_Universal.CurrentGameDataDirectory + "LevelEditorOutput";
                LevelType levelType = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CurrentLevelType;
                if ( levelType != null )
                    pathBase += "/" + levelType.Subfolder;
                return pathBase;
            }
        }
        #endregion

        #region bMenu
        public class bMenu : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_LevelEditorSystemMenu.Instance.Open();
                return MouseHandlingResult.None;
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bMenu-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddLang( "HeaderBar_SystemMenu_Top" )
                    .AddTooltipHotkeySuffix( "Cancel" );
                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bList
        public class bList : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_LevelEditorPalette.IsShowingSceneObjectListInstead = !Window_LevelEditorPalette.IsShowingSceneObjectListInstead;
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {

                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bList-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "Switches between the object palette and the list of objects in the scene.", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_SwitchPaletteAndSceneObjectList" );

                tooltipBuffer.StartColor( ColorTheme.HeaderGold ).AddNeverTranslated( "\n\nOther critical keybinds to know:", true );

                tooltipBuffer.EndColor().Line().AddNeverTranslated( "Left click", true, ColorTheme.HeaderGold ).AddNeverTranslated( " to select one object.", true )
                    .Line().AddNeverTranslated( "Left click and drag", true, ColorTheme.HeaderGold ).AddNeverTranslated( " to select many objects.", true )
                    .AddNeverTranslated( "\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Append to selection" ).AddNeverTranslated( " while selecting to add to your selection.", true )
                    .AddNeverTranslated( "\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Multi deselect" ).AddNeverTranslated( " while selecting to remove from your selection.", true )
                    .AddNeverTranslated( "\nPress ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Delete selected" ).AddNeverTranslated( " to delete the selected objects.", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bSave
        public class bSave : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                SaveOrSaveAs( input.RightButtonClicked ); //save-as when right mouse click
                return MouseHandlingResult.None;
            }

            public static void SaveOrSaveAs( bool TrySaveAs )
            {
                bool didSaveOver = false;
                if ( LastSaveOrLoadPath != string.Empty && !TrySaveAs )
                {
                    //we can only save over top of the existing one when our required path matches
                    if ( LastSaveOrLoadPath.Replace( @"\", "/").Replace( "//", "/" ).Contains( RequiredPathBase, StringComparison.InvariantCultureIgnoreCase ) )
                    {
                        DoSave( LastSaveOrLoadPath );
                        didSaveOver = true;
                    }
                }
                
                if ( !didSaveOver )
                {
                    Window_ModalBlocker.ShouldShowModalBlocker = true;
                    (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.ShowSaveDialog( delegate ( string[] files )
                    {
                        Window_ModalBlocker.ShouldShowModalBlocker = false;
                        if ( files == null || files.Length == 0 )
                        {
                            ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                                LocalizedString.AddNeverTranslated_New( "Error Saving" ),
                                LocalizedString.AddNeverTranslated_New( "Could not save, as no target file was selected." ), LangCommon.Popup_Common_Ok.LocalizedString );
                            return;
                        }
                        string fileName = files[0];
                        LastSaveOrLoadPath = fileName;
                        DoSave( fileName );

                    }, delegate
                    {
                        Window_ModalBlocker.ShouldShowModalBlocker = false;
                    }, FilePickMode.Files, false, RequiredPathBase, Path.GetFileNameWithoutExtension( LastSaveOrLoadPath ), "Save", "Save" );
                }
            }

            #region DoSave
            private static void DoSave( string fileName )
            {
                string xmlData = LevelEditorXmlWriter.SaveContentsOfCurrentLevelEditorChunkInstanceToString();
                string newFilenameFull = string.Empty;

                try
                {
                    if ( ArcenIO.FileExists( fileName ) )
                    {
                        string targetDirector = Engine_Universal.CurrentRootApplicationDirectory + "LevelEditorBackups/";
                        if ( !ArcenIO.DirectoryExists( targetDirector ) )
                            ArcenIO.CreateDirectory( targetDirector );
                        targetDirector += DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "/";
                        if ( !ArcenIO.DirectoryExists( targetDirector ) )
                            ArcenIO.CreateDirectory( targetDirector );

                        string oldFilenameBase = ArcenIO.GetFileNameWithoutExtension( fileName );

                        string newFilenameCore = targetDirector + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" +
                            oldFilenameBase;
                        newFilenameFull = newFilenameCore + ".lvl";
                        int number = 0;

                        while ( ArcenIO.FileExists( newFilenameFull ) )
                        {
                            number++;
                            newFilenameFull = newFilenameCore + "-" + number + ".lvl";
                        }

                        File.Move( fileName, newFilenameFull );
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogSingleLine( "Could not save, there was an existing file in the way and the file system wouldn't let us delete it.  File: '" +
                        fileName + "' attempting to move to '" + newFilenameFull + "'\n\nError: " + e, Verbosity.ShowAsError );
                    return;
                }

                try
                {
                    ArcenIO.WriteAllText( fileName, xmlData );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogSingleLine( "Could not save, there was some kind of error.  File: '" +
                        fileName + "'\n\nError: " + e, Verbosity.ShowAsError );
                }

                Window_LevelEditorBottomRightPopup.AddPopupMessage( "Level Saved!", 3f );
            }
            #endregion

            public override void OnMainThreadUpdate()
            {
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bSave-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( LastSaveOrLoadPath != string.Empty )
                {
                    tooltipBuffer.AddNeverTranslated( "Left click to Save", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_Save" );
                    tooltipBuffer.AddNeverTranslated( " immediately to the path: ", true );
                    tooltipBuffer.Line().StartColor( ColorTheme.Gray ).AddNeverTranslated( LastSaveOrLoadPath, true );

                    tooltipBuffer.AddNeverTranslated( "\n\nRight-click to Save As", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_SaveAs" );
                    tooltipBuffer.AddNeverTranslated( " to a new path you choose.", true );
                }
                else
                {
                    tooltipBuffer.AddNeverTranslated( "Click to Save", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_Save" );
                    tooltipBuffer.AddNeverTranslated( " to a location you choose from a popup.", true );
                    tooltipBuffer.Line().StartColor( ColorTheme.Gray ).AddNeverTranslated( LastSaveOrLoadPath, true );
                }
                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bLoad
        public class bLoad : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( input.RightButtonClicked )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                    {
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).ContentLoadingHook.ClearAllContentsOfCurrentChunkInstance();
                        LastSaveOrLoadPath = string.Empty; //this keeps us from saving over the old one
                    }, null, LocalizedString.AddNeverTranslated_New( "Clear Entire Level?" ),
                    LocalizedString.AddNeverTranslated_New( "Clear this entire level and create an empty one?" ), 
                    LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                }
                else
                {
                    string pathToStartAt;
                    if ( LastSaveOrLoadPath.Length == 0 )
                        pathToStartAt = Engine_Universal.CurrentGameDataDirectory + "LevelEditorOutput";
                    else
                        pathToStartAt = RequiredPathBase;

                    Window_ModalBlocker.ShouldShowModalBlocker = true;
                    (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.ShowLoadDialog( delegate ( string[] files )
                    {
                        int debugIndex = 0;
                        try
                        {
                            debugIndex = 100;
                            Window_ModalBlocker.ShouldShowModalBlocker = false;
                            debugIndex = 200;
                            if ( files == null || files.Length == 0 )
                            {
                                debugIndex = 210;
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                                    LocalizedString.AddNeverTranslated_New( "Error Loading" ),
                                    LocalizedString.AddNeverTranslated_New( "Could not load, as no target file was selected." ),
                                    LangCommon.Popup_Common_Ok.LocalizedString );
                                return;
                            }
                            debugIndex = 300;
                            string fileName = files[0];
                            debugIndex = 310;
                            if ( !ArcenIO.FileExists( fileName ) )
                            {
                                debugIndex = 320;
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                                    LocalizedString.AddNeverTranslated_New( "Error Loading" ),
                                    LocalizedString.AddNeverTranslated_New( "Could not load, as the file '" + fileName + "' does not exist." ),
                                    LangCommon.Popup_Common_Ok.LocalizedString );
                                return;
                            }
                            debugIndex = 500;
                            ReferenceLevelData levelData = new ReferenceLevelData( fileName );
                            debugIndex = 510;
                            levelData.StartLoadingProcessFromMainThread();

                            //now do the load
                            debugIndex = 600;
                            LastSaveOrLoadPath = fileName;
                            debugIndex = 610;
                            (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).ContentLoadingHook.LoadContentIntoCurrentChunkInstanceFromReferenceLevel( levelData );
                            debugIndex = 620;
                            (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CalculateLevelTypeFromPath( fileName );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "LoadEditor", debugIndex, e, Verbosity.ShowAsError );
                        }

                    }, delegate
                    {
                        Window_ModalBlocker.ShouldShowModalBlocker = false;
                    }, FilePickMode.Files, false, pathToStartAt, null, LangCommon.Popup_Common_Load.Text, LangCommon.Popup_Common_Load.Text );
                }
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bLoad-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "Left click to load a new level.  Right-click to clear the entire level.", true );
                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bExtrude
        public class bExtrude : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoExtrudeMode = true;
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                ( this.Element as ArcenUI_Button )?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoExtrudeMode ? Color.white : Color_Dark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bExtrude-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "The Extrude Gizmo", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_Activate extrude gizmo" );
                tooltipBuffer.AddNeverTranslated( "\nThe extrude gizmo allows you to drag out entire lines of objects.  ", true );
                tooltipBuffer.AddNeverTranslated( "\nSuppose you have one concrete block.  Drag it in one direction using this tool to make a concrete pier.  Now select all those blocks at once, and drag in a second direction and you have a concrete platform.", true );

                tooltipBuffer.StartColor( ColorTheme.Gray ).AddNeverTranslated( "\n\nThis is a fairly nonstandard tool, but incredibly helpful.  If you need to make lines of objects, raise, a wall, or anything similar, then this is your go-to.", true );

                tooltipBuffer.EndColor().AddNeverTranslated( "\n\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable overlap test" ).AddNeverTranslated( " to disable the overlap test.  This is almost always a bad idea, so... maybe don't?", true )
                    .AddNeverTranslated( "\nPress ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Toggle transform space (global/local)" ).AddNeverTranslated( " to toggle between global or local coordinate-spaces for extrusion.  This can give very interesting results if you have partially rotated an object.", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bMove
        public class bMove : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoMoveMode = true;
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoMoveMode ? Color.white : Color_Dark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bMove-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "The Move Gizmo", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_Activate move gizmo" );
                tooltipBuffer.AddNeverTranslated( "\nThe primary way of interacting with objects in the level editor.", true );
                tooltipBuffer.AddNeverTranslated( "\nYou can drag objects in either one dimension (the gizmo arrows), or in two (the gizmo planes near the center.", true );

                tooltipBuffer.StartColor( ColorTheme.Gray ).AddNeverTranslated( "\n\nBecause you do not have true depth perception (without a VR headset your binocular vision is gone), editing via this style of handle is how most 3D software works.  Even if you did have depth perception, your mouse only moves in two axes at once.  This editor does support 3D mice for camera movement, but trying to manipulate objects with 3D mice is usually slower than just getting good with these handles, which are an industry-wide standard.", true );

                tooltipBuffer.EndColor().AddNeverTranslated( "\n\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable snapping" ).AddNeverTranslated( " to enable basic grid snapping.", true )
                    .AddNeverTranslated( "\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable vertex snapping" ).AddNeverTranslated( " and grab a specific part of the object to do MUCH more sophisticated vertex-snapping.", true )
                    .AddNeverTranslated( "\nHold ", true ).AddColorizedKeyCombo( "c1a25d", ColorTheme.Grayer, "LevelEditor_Enable 2D mode" ).AddNeverTranslated( " to use 2D movement mode, which is not very useful.  Instead of this, use the gizmo planes.", true )
                    .AddNeverTranslated( "\nPress ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Toggle transform space (global/local)" ).AddNeverTranslated( " to toggle between global or local coordinate-spaces for movement.", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bRotate
        public class bRotate : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoRotateMode = true;
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoRotateMode ? Color.white : Color_Dark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bRotate-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "The Rotate Gizmo", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_Activate rotation gizmo" );
                tooltipBuffer.AddNeverTranslated( "\nSpin an object or group of objects on one or more axes.", true );
                tooltipBuffer.AddNeverTranslated( "\nGrab the colored handles to rotate in just one axis.", true );
                tooltipBuffer.AddNeverTranslated( "\nGrab in the empty space next to the handles to rotate in all three (this often leads to mistakes).", true );

                tooltipBuffer.EndColor().AddNeverTranslated( "\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable snapping" ).AddNeverTranslated( " to enable basic snapping to every five degrees or so.", true )
                    .AddNeverTranslated( "\nPress ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Toggle transform space (global/local)" ).AddNeverTranslated( " to toggle between global or local coordinate-spaces for rotation.", true );

                tooltipBuffer.StartColor( "ffa030" ).AddNeverTranslated( "\n\nFor rotations at 90 degree angles, you don't even need to select this gizmo!  Use the below instead:", true );

                tooltipBuffer.EndColor().AddNeverTranslated( "\nPress ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Rotate around X" ).AddNeverTranslated( " to quick-rotate around the X axis.", true )
                    .AddNeverTranslated( "\nPress ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Rotate around Y" ).AddNeverTranslated( " to quick-rotate around the Y axis.", true )
                    .AddNeverTranslated( "\nPress ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Rotate around Z" ).AddNeverTranslated( " to quick-rotate around the Z axis.", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bScale
        public class bScale : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoScaleMode = true;
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoScaleMode ? Color.white : Color_Dark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bScale-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "The Scale Gizmo", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_Activate scale gizmo" );
                tooltipBuffer.AddNeverTranslated( "\nIn this mode, you can scale objects up and down on one or more axes.", true );
                tooltipBuffer.AddNeverTranslated( "\nGrab the endpoints of the gizmo to scale in one dimension, or grab the center to scale in all three at once.", true );
                tooltipBuffer.AddNeverTranslated( "\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Change multi-axis mode" ).AddNeverTranslated( " to add triangular handles which allow you to scale in two dimensions.", true );

                tooltipBuffer.StartColor( ColorTheme.Gray ).AddNeverTranslated( "\n\nDo be careful with scale.  Non-uniform scale leads to distortion.  And scaling up or down objects in general can make them look wrong when placed near actual characters.", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bBoxScale
        public class bBoxScale : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoBoxScaleMode = true;
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoBoxScaleMode ? Color.white : Color_Dark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bBoxScale-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "The Box-Scale Gizmo", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_Activate box scale gizmo" );
                tooltipBuffer.AddNeverTranslated( "\nAn alternative way of changing object scale.  Works best for cubes, but can also be useful for other things.", true );
                tooltipBuffer.AddNeverTranslated( "\nGrab the colored endpoints of the gizmo to scale the object off to one side or another (changes scale and moves the center).", true );
                tooltipBuffer.AddNeverTranslated( "\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable center pivot" ).AddNeverTranslated( " to instead mirror the scale changes (changes scale without moving the center).", true );

                tooltipBuffer.StartColor( ColorTheme.Gray ).AddNeverTranslated( "\n\nDo be careful with scale.  Non-uniform scale leads to distortion.  And scaling up or down objects in general can make them look wrong when placed near actual characters.", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bUniversal
        public class bUniversal : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoUniversalMode = true;
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoUniversalMode ? Color.white : Color_Dark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bUniversal-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "The Universal Gizmo", true )
                    .AddTooltipHotkeySuffix( "LevelEditor_Activate universal gizmo" );
                tooltipBuffer.AddNeverTranslated( "\nCombines movement, rotation, and scale into one ugly (but useful) mess.", true );

                tooltipBuffer.StartColor( ColorTheme.Gray ).AddNeverTranslated( "\n\nThe XZ plane is really hard to grab, and scale only works in all three dimensions at once.  Single-axis movement, and any form of rotation, is very convenient, however.", true );

                tooltipBuffer.EndColor().AddNeverTranslated( "\n\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable snapping" ).AddNeverTranslated( " to enable basic grid snapping.", true )
                    .AddNeverTranslated( "\nHold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable vertex snapping" ).AddNeverTranslated( " and grab a specific part of the object to do MUCH more sophisticated vertex-snapping.", true )
                    .AddNeverTranslated( "\nHold ", true ).AddColorizedKeyCombo( "c1a25d", ColorTheme.Grayer, "LevelEditor_Enable 2D mode" ).AddNeverTranslated( " to use 2D movement mode, which is not very useful.  Instead of this, use the gizmo planes.", true )
                    .AddNeverTranslated( "\nPress ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Toggle transform space (global/local)" ).AddNeverTranslated( " to toggle between global or local coordinate-spaces for movement and rotation.", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bDuplicate
        public class bDuplicate : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.DuplicateSelectedObjects();
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.GetCountOfSelectedObjects( false ) > 0 ? Color.white : Color_VeryDark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bDuplicate-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "Duplicating objects is KEY to being an effective level designer!", true );
                tooltipBuffer.AddNeverTranslated( "\nClicking the sidebar over and over again is very slow by comparison.", true );
                tooltipBuffer.AddNeverTranslated( "\nCreate a few objects, then duplicate them individually or in groups to quickly make larger patterns.", true );
                tooltipBuffer.AddNeverTranslated( "\nTo duplicate your current selection, click this button or press ", true )
                    .AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Duplicate selected" ).AddNeverTranslated( ".", true );
                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bGrab
        public class bGrab : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoGrabMode = true;
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoGrabMode ? Color.white : Color_Dark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bGrab-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoGrabMode )
                {
                    tooltipBuffer.StartColor( ColorTheme.HeaderGold ).AddNeverTranslated( "Currently in selection-grab mode!", true ).EndColor()
                        .Line().AddNeverTranslated( "Press ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Grab Toggle on/off" ).AddNeverTranslated( " or left-click to exit this mode.", true );
                }
                else
                    tooltipBuffer.AddNeverTranslated( "Press ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Grab Toggle on/off" ).AddNeverTranslated( " or click this button to enter selection-grab mode.", true );

                tooltipBuffer.Line2x().AddNeverTranslated( "During a selection-grab session:", true )
                    .Line().AddNeverTranslated( "Hold down ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable rotation" ).AddNeverTranslated( " and move the mouse to rotate objects around surface normal.", true )
                    .Line().AddNeverTranslated( "Hold down ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable rotation around anchor" ).AddNeverTranslated( " and move the mouse horizontally to orbit objects around anchor point.", true )
                    .Line().AddNeverTranslated( "Hold down ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable scaling" ).AddNeverTranslated( " and move the mouse horizontally to scale objects.", true )
                    .Line().AddNeverTranslated( "Hold down ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable offset from surface" ).AddNeverTranslated( " and move the mouse horizontally to offset objects from the surface on which they are sitting.", true )
                    .Line().AddNeverTranslated( "Hold down ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable anchor adjust" ).AddNeverTranslated( " and move the mouse to adjust anchor point.", true )
                    .Line().AddNeverTranslated( "Hold down ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable offset from anchor" ).AddNeverTranslated( " and move the mouse horizontally to offset objects from the anchor point.", true )
                    .Line().AddNeverTranslated( "Press ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Next alignment axis" ).AddNeverTranslated( " to toggle to the next surface alignment axis (i.e. can be used to cycle through all available surface alignment axes).", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bObjectToObject
        public class bObjectToObject : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( input.RightButtonClicked )
                {
                    VisCommands.GoToWebsite( "https://www.youtube.com/watch?v=7_iRf-hYhJM" );
                }
                else
                    (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoObjectToObjectMode = true;
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoObjectToObjectMode ? Color.white : Color_Dark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bObjectToObject-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsGizmoObjectToObjectMode )
                {
                    tooltipBuffer.StartColor( ColorTheme.HeaderGold ).AddNeverTranslated( "Currently in object-to-object snap mode!", true ).EndColor()
                        .Line().AddNeverTranslated( "Press ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Snap Toggle on/off" ).AddNeverTranslated( " or left-click to exit this mode.", true );
                }
                else
                    tooltipBuffer.AddNeverTranslated( "Press ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Snap Toggle on/off" ).AddNeverTranslated( " or click this button to enter object-to-object snap mode.", true );

                tooltipBuffer.Line2x().AddNeverTranslated( "During an object-to-object snap session:", true )
                    .Line().AddNeverTranslated( "Move the mouse to snap the selected objects to nearby objects.", true )
                    .Line().AddNeverTranslated( "Hold down ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable flexi-snap" ).AddNeverTranslated( " for flexi-snap mode, though we're not exactly sure what that does yet.", true )
                    .Line().AddNeverTranslated( "Hold down ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Enable more control" ).AddNeverTranslated( " for slightly more control, which lets you make slight corrections to what it calculates for you.", true )
                    .Line().AddNeverTranslated( "Press ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Toggle sit below surface" ).AddNeverTranslated( " to 'Toggle sit below surface,' which is also not yet understood.", true );

                tooltipBuffer.Line2x().AddNeverTranslated( "Right-click this button to see a YouTube Video explaining some of how to use this mode.", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bUndo
        public class bUndo : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.Undo();
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.GetCountOfUndoActions() > 0 ? Color.white : Color_VeryDark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bUndo-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "Undo a previous action.", true )
                    .AddTooltipHotkeySuffix( "Undo" );
                tooltipBuffer.Line2x();
                tooltipBuffer.AddNeverTranslated( "Current actions that can be undone:  ", true ).AddNeverTranslated( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.GetCountOfUndoActions().ToString(), true, "5df5ff" );
                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bRedo
        public class bRedo : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.Redo();
                return MouseHandlingResult.None;
            }

            public override void OnMainThreadUpdate()
            {
                (this.Element as ArcenUI_Button)?.SetColor( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.GetCountOfRedoActions() > 0 ? Color.white : Color_VeryDark );
                base.OnMainThreadUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bRedo-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "Redo a previously undone action.", true )
                    .AddTooltipHotkeySuffix( "Redo" );
                tooltipBuffer.Line2x();
                tooltipBuffer.AddNeverTranslated( "Current actions that can be redone:  ", true ).AddNeverTranslated( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.GetCountOfRedoActions().ToString(), true, "5df5ff" );
                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bGrid
        public class bGrid : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.SnapGridToZero();
                return MouseHandlingResult.None;
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bGrid-tooltipBuffer" );
            public override void HandleMouseover()
            {
                LevelEditorHookBase hook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;

                tooltipBuffer.AddNeverTranslated( "Grid is currently: ", true ).AddNeverTranslated( hook.IsLevelEditorGridOn ? "Visible" : "Hidden", true, "5df5ff" )
                    .AddTooltipHotkeySuffix( "LevelEditor_ToggleGrid" );
                tooltipBuffer.Line();

                tooltipBuffer.AddNeverTranslated( "Click to snap the visible grid to Y position 0.  ", true ).AddNeverTranslated( "Current Y: ", true, "5df5ff" ).AddNeverTranslated( hook.CurrentGridY.ToString( "0.##" ), true, "5df5ff" );
                tooltipBuffer.Line2x();
                tooltipBuffer.AddNeverTranslated( "Raise grid by pressing ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Grid up" )
                    .AddNeverTranslated( ", and use ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Grid down" ).AddNeverTranslated( " to lower it.", true );
                tooltipBuffer.Line();
                tooltipBuffer.AddNeverTranslated( "Left click on an object while holding", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Snap to cursor pick point" )
                    .AddNeverTranslated( " to snap the grid Y position to the intersection between the mouse cursor that object.", true );
                tooltipBuffer.Line2x();
                tooltipBuffer.AddNeverTranslated( "Double-left click on an object while holding ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Snap to cursor pick point" )
                    .AddNeverTranslated( " to do the same as above, but snap to either the top or bottom of the object, depending which one is closer to the cursor pick point. Useful when you need to quickly place the grid on top or below objects. Works with arbitrary object and grid rotations.", true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bCamera
        public class bCamera : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.ReturnCameraToWorldCenter();
                return MouseHandlingResult.None;
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bCamera-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "Click this button to recenter the camera at world center.\n\n", true );

                {
                    tooltipBuffer.AddNeverTranslated( "Camera controls: ", true ).AddNeverTranslated( "hold Right Mouse", true, ColorTheme.HeaderGold ).AddNeverTranslated( " and use ", true ).AddNeverTranslated( "WASD", true, ColorTheme.HeaderGold )
                        .AddNeverTranslated( " to move", true );
                    tooltipBuffer.Line();
                    tooltipBuffer.AddNeverTranslated( "Look around by ", true ).AddNeverTranslated( " holding ", true, ColorTheme.HeaderGold ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Look around" ).AddNeverTranslated( " and moving the mouse", true );
                    tooltipBuffer.Line();
                    tooltipBuffer.AddNeverTranslated( "Fly up and down by ", true ).AddNeverTranslated( "holding right mouse button", true, ColorTheme.HeaderGold ).AddNeverTranslated( " and using ", true ).AddNeverTranslated( "Q and E", true, ColorTheme.HeaderGold );
                    tooltipBuffer.Line();
                    tooltipBuffer.AddNeverTranslated( "Go faster by holding ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Alternate move speed" );
                    tooltipBuffer.Line();
                    tooltipBuffer.AddNeverTranslated( "Focus nicely on your current selection by pressing ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Focus camera on selection" );
                    tooltipBuffer.Line();
                    tooltipBuffer.AddNeverTranslated( "Hold ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Pan" ).AddNeverTranslated( " and move the mouse to pan", true )
                        .AddNeverTranslated( ", and scroll the ", true ).AddNeverTranslated( "mouse wheel", true, ColorTheme.HeaderGold ).AddNeverTranslated( " to zoom ", true );
                    tooltipBuffer.Line();
                    tooltipBuffer.AddNeverTranslated( "Hold down ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Orbit" ).AddNeverTranslated( " OR ", true ).AddColorizedKeyCombo( ColorTheme.HeaderGold, ColorTheme.Grayer, "LevelEditor_Orbit Alt" )
                        .AddNeverTranslated( " to make your mouse movements orbit your selection.", true );
                    tooltipBuffer.AddNeverTranslated( "Zoom in and out with the ", true ).AddNeverTranslated( "mouse wheel", true, ColorTheme.HeaderGold ).AddNeverTranslated( ".", true );
                }

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bLevelDetails
        public class bLevelDetails : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsLevelEditorDetailsWindowShowing =
                    !(Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsLevelEditorDetailsWindowShowing;
                return MouseHandlingResult.None;
            }

            private static readonly Dictionary<int, bool> WorkingRenderGroupsUsed = Dictionary<int, bool>.Create_WillNeverBeGCed( 24000, "bLevelDetails-WorkingRenderGroupsUsed", 1200 );

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "bCamera-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.AddNeverTranslated( "Click this button to open or close the Level Details window.\n\n", true );

                WorkingRenderGroupsUsed.Clear();
                int totalPlaceables = 0;
                int totalPlaceablesTypes = 0;
                int gpuInstancencounters = 0;
                int gpuInstanceTotal = 0;

                #region Do A Bunch Of Aggregation
                foreach ( A5ObjectRoot root in A5ObjectRootTable.Instance.Rows )
                {
                    if ( root == null )
                        continue;
                    if ( root.InEditorInstances_IncludesDeleted.Count > 0 || root.TestObjectsInLevelEditor > 0 )
                    {
                        int actualNonDeletedInstanceCount = 0;
                        foreach ( IA5Placeable iPlace in root.InEditorInstances_IncludesDeleted )
                        {
                            if ( !iPlace.IsValid || !iPlace.IsObjectActive )
                                continue; //this was deleted
                            actualNonDeletedInstanceCount++;
                        }

                        actualNonDeletedInstanceCount += root.TestObjectsInLevelEditor;

                        if ( actualNonDeletedInstanceCount > 0 )
                        {
                            totalPlaceables += actualNonDeletedInstanceCount;
                            totalPlaceablesTypes++;

                            foreach ( IA5Renderer rend in root.AllRenderersOfThisRoot )
                            {
                                int parentIndex = rend.ParentGroupIndex;
                                if ( !WorkingRenderGroupsUsed.ContainsKey( parentIndex ) )
                                {
                                    WorkingRenderGroupsUsed[parentIndex] = true;
                                    gpuInstancencounters++;
                                }

                                gpuInstanceTotal += actualNonDeletedInstanceCount;
                            }
                        }
                    }
                }
                #endregion

                {
                    LevelType levelType = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CurrentLevelType;
                    if ( levelType == null )
                        tooltipBuffer.AddNeverTranslated( "Level Type: ", true ).AddNeverTranslated( "NULL!", true, ColorTheme.HeaderGold );
                    else
                        tooltipBuffer.AddNeverTranslated( "Level Type: ", true ).AddNeverTranslated( levelType.GetDisplayName(), true, ColorTheme.HeaderGold )
                            .AddNeverTranslated( "   (", true ).AddNeverTranslated( levelType.SizeX.ToString(), true, ColorTheme.HeaderGoldOrangeDark ).AddNeverTranslated( "x", true ).AddNeverTranslated( levelType.SizeZ.ToString(), true, ColorTheme.HeaderGoldOrangeDark ).AddNeverTranslated( ")", true );
                    tooltipBuffer.Line();

                    tooltipBuffer.AddNeverTranslated( "Total Objects: ", true ).AddNeverTranslated( totalPlaceables.ToStringLargeNumberAbbreviated(), true, ColorTheme.HeaderGold );
                    tooltipBuffer.Line();
                    tooltipBuffer.AddNeverTranslated( "Total Object Types: ", true ).AddNeverTranslated( totalPlaceablesTypes.ToStringThousandsWhole(), true, ColorTheme.HeaderGold );
                    tooltipBuffer.Line();
                    tooltipBuffer.AddNeverTranslated( "Total GPU Instance Groups: ", true ).AddNeverTranslated( gpuInstancencounters.ToStringThousandsWhole(), true, "ff7c5d" ).AddNeverTranslated( "  (Biggest performance impact as increases)", true );
                    tooltipBuffer.Line();
                    tooltipBuffer.AddNeverTranslated( "Total GPU Instances: ", true ).AddNeverTranslated( gpuInstanceTotal.ToStringThousandsWhole(), true, ColorTheme.HeaderGold );
                }

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region tVersionText
        public class tVersionText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( GameVersionTable.Instance.CurrentVersion != null )
                    Buffer.AddFormat1( "MainMenu_VersionPrefix", GameVersionTable.Instance.CurrentVersion.GetAsString() );
            }
        }
        #endregion
    }
}
