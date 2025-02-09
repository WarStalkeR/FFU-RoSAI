using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_SystemMenu : ToggleableWindowController, IInputActionHandler
    {
        public static Window_SystemMenu Instance;
        public Window_SystemMenu()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
            this.ShouldBlurBackgroundGame = true;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
        }

        public override void OnHideAfterNotShowing()
        {
            base.OnHideAfterNotShowing();
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool wasVisible = true;
            public override void OnUpdate()
            {
                if ( wasVisible != InputCaching.ShowControlTipsWhenSystemMenuOpen )
                {
                    wasVisible = InputCaching.ShowControlTipsWhenSystemMenuOpen;
                    for ( int i = 0; i < 7; i++ )
                        this.Element.RelatedImages[i].enabled = wasVisible;
                }

                if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                    Instance.Close( WindowCloseReason.ShowingRefused );
            }
        }

        #region bExit
        public class bExit : ButtonAbstractBaseWithImage
        {
            public static bExit Original;
            public bExit() { if ( Original == null ) Original = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserCasualRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region RenderLineFromSidebar
        private static void RenderLineFromSidebar( UISidebarBasicItem Item, ArcenDoubleCharacterBuffer Buffer, bool IsHovered, AdjustedSpriteStyle SpriteStyle )
        {
            string colorHex = ColorTheme.GetBasicLightTextBlue( IsHovered );
            Buffer.StartColor( colorHex );
            Buffer.AddSpriteStyled_NoIndent( Item.GetIcon(), SpriteStyle, colorHex ).Space2x();
            Buffer.AddRaw( Item.GetDisplayName() );
            Buffer.AddNeverTranslated( "     ", false ).StartSize20().AddNeverTranslated( ".", true, ColorTheme.SpacerColor ).EndSize();
        }
        #endregion

        #region RenderTooltipFromSidebar
        private static void RenderTooltipFromSidebar( TooltipID ToolID, UISidebarBasicItem Item, IArcenUIElementForSizing DrawNextTo )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartBasicTooltip( ToolID, DrawNextTo, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
            {
                novel.TitleUpperLeft.AddRaw( Item.GetDisplayNameForSidebar() );
                string hotkeyToDisplay = Item.GetHotkeyToDisplayAsToggleForSidebar();
                if ( hotkeyToDisplay.Length > 0 )
                    novel.TitleLowerLeft.AddTooltipHotkeySecondLine( hotkeyToDisplay );
                novel.Main.StartColor( ColorTheme.NarrativeColor );
                novel.Main.AddRaw( Item.GetDescription() );
                novel.Icon = Item.Icon;
            }
        }
        #endregion

        #region RenderTooltipFromControls
        private static void RenderTooltipFromControls( string ControlsHeader, string ControlsTooltip, IArcenUIElementForSizing DrawNextTo, string ExtraLang, SideClamp Clamp )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Controls", ControlsHeader ), DrawNextTo, Clamp, TooltipNovelWidth.Smaller ) )
            {
                novel.TitleUpperLeft.AddLang( ControlsHeader );
                novel.Main.StartColor( ColorTheme.NarrativeColor );
                novel.Main.AddLang( ControlsTooltip );
                if ( !ExtraLang.IsEmpty() )
                    novel.Main.Line().AddLang( ExtraLang );
            }
        }

        private static void RenderTooltipFromControlsF1( string ControlsHeader, string ControlsTooltip, IArcenUIElementForSizing DrawNextTo, string ExtraLang, SideClamp Clamp, object o1 )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Controls", ControlsHeader ), DrawNextTo, Clamp, TooltipNovelWidth.Smaller ) )
            {
                novel.TitleUpperLeft.AddLang( ControlsHeader );
                novel.Main.StartColor( ColorTheme.NarrativeColor );
                novel.Main.AddFormat1( ControlsTooltip, o1 );
                if ( !ExtraLang.IsEmpty() )
                    novel.Main.Line().AddLang( ExtraLang );
            }
        }

        private static void RenderTooltipFromControlsF2( string ControlsHeader, string ControlsTooltip, IArcenUIElementForSizing DrawNextTo, string ExtraLang, SideClamp Clamp, object o1, object o2 )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Controls", ControlsHeader ), DrawNextTo, Clamp, TooltipNovelWidth.Smaller ) )
            {
                novel.TitleUpperLeft.AddLang( ControlsHeader );
                novel.Main.StartColor( ColorTheme.NarrativeColor );
                novel.Main.AddFormat2( ControlsTooltip, o1, o2 );
                if ( !ExtraLang.IsEmpty() )
                    novel.Main.Line().AddLang( ExtraLang );
            }
        }
        #endregion

        #region bResume
        public class bResume : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );
                Buffer.AddLang( "SysMenu_Resume" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserCasualRequest );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
            }
        }
        #endregion

        #region bSaveGame
        public class bSaveGame : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                RenderLineFromSidebar( IconRefs.SaveGame, Buffer, this.Element.LastHadMouseWithin, AdjustedSpriteStyle.InlineLarger1_4 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserCasualRequest );
                Window_SaveGameMenu.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromSidebar( TooltipID.Create( "SystemMenu", "bSaveGame" ), IconRefs.SaveGame, this.Element );
            }
        }
        #endregion

        #region bLoadGame
        public class bLoadGame : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                RenderLineFromSidebar( IconRefs.LoadGame, Buffer, this.Element.LastHadMouseWithin, AdjustedSpriteStyle.InlineLarger1_8 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserCasualRequest );
                Window_LoadGameMenu.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromSidebar( TooltipID.Create( "SystemMenu", "bLoadGame" ), IconRefs.LoadGame, this.Element );
            }
        }
        #endregion

        #region bSettings
        public class bSettings : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                RenderLineFromSidebar( IconRefs.Settings, Buffer, this.Element.LastHadMouseWithin, AdjustedSpriteStyle.InlineLarger1_8 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserCasualRequest );
                Window_SettingsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromSidebar( TooltipID.Create( "SystemMenu", "bSettings" ), IconRefs.Settings, this.Element );
            }
        }
        #endregion

        #region bControls
        public class bControls : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                RenderLineFromSidebar( IconRefs.Controls, Buffer, this.Element.LastHadMouseWithin, AdjustedSpriteStyle.InlineLarger1_8 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserCasualRequest );
                Window_ControlBindingsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromSidebar( TooltipID.Create( "SystemMenu", "bControls" ), IconRefs.Controls, this.Element );
            }
        }
        #endregion

        #region bDiscord
        public class bDiscord : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                RenderLineFromSidebar( IconRefs.Discord, Buffer, this.Element.LastHadMouseWithin, AdjustedSpriteStyle.InlineLarger2_5 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                VisCommands.GoToWebsite( "https://discord.gg/arcengames" );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromSidebar( TooltipID.Create( "SystemMenu", "bDiscord" ), IconRefs.Discord, this.Element );
            }
        }
        #endregion

        #region bMantis
        public class bMantis : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                RenderLineFromSidebar( IconRefs.MantisBugtracker, Buffer, this.Element.LastHadMouseWithin, AdjustedSpriteStyle.InlineLarger1_8 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                VisCommands.GoToWebsite( "https://bugtracker.arcengames.com/view_all_bug_page.php" );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromSidebar( TooltipID.Create( "SystemMenu", "bMantis" ), IconRefs.MantisBugtracker, this.Element );
            }
        }
        #endregion

        #region bSavesAndLogs
        public class bSavesAndLogs : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                RenderLineFromSidebar( IconRefs.SavesAndLogs, Buffer, this.Element.LastHadMouseWithin, AdjustedSpriteStyle.InlineLarger1_8 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                VisCommands.OpenLocalFolder( Engine_Universal.CurrentPlayerDataDirectory );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromSidebar( TooltipID.Create( "SystemMenu", "bSavesAndLogs" ), IconRefs.SavesAndLogs, this.Element );
            }
        }
        #endregion

        #region bQuit
        public class bQuit : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                RenderLineFromSidebar( IconRefs.Quit, Buffer, this.Element.LastHadMouseWithin, AdjustedSpriteStyle.InlineLarger1_4 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                VisCommands.HandleQuitToMainMenu();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromSidebar( TooltipID.Create( "SystemMenu", "bQuit" ), IconRefs.Quit, this.Element );
            }
        }
        #endregion

        #region bExitToDesktop
        public class bExitToDesktop : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                RenderLineFromSidebar( IconRefs.Exit, Buffer, this.Element.LastHadMouseWithin, AdjustedSpriteStyle.InlineLarger1_4 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                VisCommands.HandleExitToOS();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromSidebar( TooltipID.Create( "SystemMenu", "bExitToDesktop" ), IconRefs.Exit, this.Element );
            }
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "SysMenu_MainGame" );
            }
        }
        #endregion

        #region bToggleControls
        public class bToggleControls : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                bool isOn = InputCaching.ShowControlTipsWhenSystemMenuOpen;
                this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[isOn ? 0 : 1] );

                buffer.AddLang( "On", isOn ? string.Empty : "63647F" );

            }

            public override void GetOtherTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer, int OtherTextIndex )
            {
                if ( OtherTextIndex == 0 )
                {
                    bool isOn = InputCaching.ShowControlTipsWhenSystemMenuOpen;
                    buffer.AddLang( "Off", !isOn ? string.Empty : "63647F" );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                bool newVal = !GameSettings.Current.GetBool( "ShowControlTipsWhenSystemMenuOpen" );
                GameSettings.Current.SetBool( "ShowControlTipsWhenSystemMenuOpen", newVal );
                InputCaching.ShowControlTipsWhenSystemMenuOpen = newVal;
                GameSettings.SaveToDisk();
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region tToggleControls1
        public class tToggleControls1 : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_ToggleHeader" );
            }
        }
        #endregion

        #region tToggleControls2
        public class tToggleControls2 : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_ToggleSubHeader" );
            }
        }
        #endregion

        #region tCameraControlsHeader
        public class tCameraControlsHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_Camera" );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tPanHeader
        public class tPanHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_Panning_Header" );
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromControlsF1( "BasicControls_Panning_Header", "BasicControls_Panning_Tooltip", this.Element, string.Empty, SideClamp.LeftOrRight, Lang.GetLeftClickText() );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tPanText
        public class tPanText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Engine_Universal.IsSteamDeckActive )
                    Buffer.AddLang( "BasicControls_Panning_SteamDeck" );
                else
                    Buffer.AddFormat4( "BasicControls_Panning", InputCaching.GetStringForTraditionalCameraPanning1(), 
                            InputCaching.GetStringForTraditionalCameraPanning2(),
                            InputCaching.GetStringForTraditionalCameraPanning3(),
                            InputCaching.GetStringForTraditionalCameraPanning4() );
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromControlsF1( "BasicControls_Panning_Header", "BasicControls_Panning_Tooltip", this.Element, string.Empty, SideClamp.LeftOrRight, Lang.GetLeftClickText() );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region RotateTiltTooltip
        private static void RotateTiltTooltip( IArcenUIElementForSizing DrawNextTo )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ControlsInst", "RotateTilt" ), DrawNextTo, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
            {
                novel.TitleUpperLeft.AddLang( "BasicControls_RotateAndTilt_HeaderOneLine" );
                novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "BasicControls_RotateAndTilt_Tooltip_General" ).Line();
                if ( !Engine_Universal.IsSteamDeckActive )
                    novel.Main.AddFormat2( "BasicControls_RotateAndTilt_Normal_Tooltip_P2", InputCaching.GetStringForCameraRotation(), InputCaching.GetStringForCameraInclination() );
            }
        }
        #endregion

        #region tRTHeader
        public class tRTHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_RotateAndTilt_HeaderTwoLine" );
            }

            public override void HandleMouseover()
            {
                RotateTiltTooltip( this.Element );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tRTText
        public class tRTText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Engine_Universal.IsSteamDeckActive )
                    Buffer.AddLang( "BasicControls_RotateAndTilt_SteamDeck" );
                else
                    Buffer.AddLang( "BasicControls_RotateAndTilt_Normal" );
            }

            public override void HandleMouseover()
            {
                RotateTiltTooltip( this.Element );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region ZoomTooltip
        private static void ZoomTooltip( IArcenUIElementForSizing DrawNextTo )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ControlsInst", "Zoom" ), DrawNextTo, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
            {
                novel.TitleUpperLeft.AddLang( "BasicControls_Zooming_Header" );
                novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "BasicControls_Zooming_Tooltip" );
            }
        }
        #endregion

        #region tZoomHeader
        public class tZoomHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_Zooming_Header" );
            }

            public override void HandleMouseover()
            {
                ZoomTooltip( this.Element );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tZoomText
        public class tZoomText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddFormat1( Engine_Universal.IsSteamDeckActive ? "BasicControls_Zooming_SteamDeck" : "BasicControls_Zooming_General", InputCaching.GetStringForCameraZoom() );
            }

            public override void HandleMouseover()
            {
                ZoomTooltip( this.Element );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region bOtherControls
        public class bOtherControls : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_OthersInTooltip" );
            }

            public override void GetOtherTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer, int OtherTextIndex )
            {
                switch ( OtherTextIndex )
                {
                    case 0:
                        Buffer.AddLang( "BasicControls_OthersInTooltip_HoverHere" );
                        break;
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ControlsInst", "bOtherControls" ), Element, SideClamp.LeftOrRight, TooltipNovelWidth.Simple ) )
                {
                    novel.TitleUpperLeft.AddLang( "BasicControls_OthersInTooltip" );
                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "BasicControls_OthersInTooltip_Tooltip_P1" ).Line();


                    novel.FrameTitle.AddLang( "BasicControls_OthersInTooltip_Tooltip_P2" );
                    novel.FrameBody.StartStyleLineHeightA()

                        .AddFormat1( "BasicControls_OthersInTooltip_Tooltip_LoadoutWindow",
                        InputActionTypeDataTable.Instance.GetRowByID( "LoadoutWindow" ).GetHumanReadableKeyCombo() ).Line()

                        .AddFormat1( "BasicControls_OthersInTooltip_Tooltip_ToggleMapMode",
                        InputActionTypeDataTable.Instance.GetRowByID( "ToggleMapMode" ).GetHumanReadableKeyCombo() ).Line()

                        .AddFormat1( "BasicControls_OthersInTooltip_Tooltip_BuildMode",
                        InputActionTypeDataTable.Instance.GetRowByID( "ToggleBuildMode" ).GetHumanReadableKeyCombo() );

                    if ( FlagRefs.HasFiguredOutStructureConstruction.DuringGameplay_IsTripped )
                        novel.FrameBody.Line();
                    else
                        novel.FrameBody.Space2x().StartSize90().AddLang( "BasicControls_NotYetUnlocked", ColorTheme.GrayLess ).EndSize().Line();

                    novel.FrameBody.AddFormat1( "BasicControls_OthersInTooltip_Tooltip_ToggleResearch",
                        InputActionTypeDataTable.Instance.GetRowByID( "ToggleResearch" ).GetHumanReadableKeyCombo() );
                    if ( FlagRefs.HasFiguredOutResearch.DuringGameplay_IsTripped )
                        novel.FrameBody.Line();
                    else
                        novel.FrameBody.Space2x().StartSize90().AddLang( "BasicControls_NotYetUnlocked", ColorTheme.GrayLess ).EndSize().Line();

                    novel.FrameBody.AddFormat1( "BasicControls_OthersInTooltip_Tooltip_Inventory",
                        InputActionTypeDataTable.Instance.GetRowByID( "Inventory" ).GetHumanReadableKeyCombo() ).Line();

                    novel.FrameBody.AddFormat1( "BasicControls_OthersInTooltip_Tooltip_RecentEvents",
                        InputActionTypeDataTable.Instance.GetRowByID( "RecentEvents" ).GetHumanReadableKeyCombo() ).Line();

                    novel.FrameBody.Line();

                    novel.FrameBody.AddFormat1( "BasicControls_OthersInTooltip_Tooltip_HoldForInspectMode",
                        InputActionTypeDataTable.Instance.GetRowByID( "HoldForInspectMode" ).GetHumanReadableKeyCombo() ).Line();

                    novel.FrameBody.EndLineHeight();
                }
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tBasicGameControlsHeader
        public class tBasicGameControlsHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_BasicGame" );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tSelectHeader
        public class tSelectHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_SelectAndTakeActions_HeaderTwoLine" );
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromControls( "BasicControls_SelectAndTakeActions_HeaderOneLine", "BasicControls_SelectAndTakeActions_Tooltip", this.Element, string.Empty, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tSelectText
        public class tSelectText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddFormat1( "BasicControls_SelectAndTakeActions", Lang.GetLeftClickText() );
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromControls( "BasicControls_SelectAndTakeActions_HeaderOneLine", "BasicControls_SelectAndTakeActions_Tooltip", this.Element, string.Empty, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tOrdersHeader
        public class tOrdersHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_DeselectOrCancel_HeaderTwoLine" );
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromControls( "BasicControls_DeselectOrCancel_HeaderOneLine", "BasicControls_DeselectOrCancel_Tooltip", this.Element, string.Empty, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tOrdersText
        public class tOrdersText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddFormat1( "BasicControls_DeselectOrCancel", Lang.GetRightClickText() );
            }

            public override void HandleMouseover()
            {
                RenderTooltipFromControls( "BasicControls_DeselectOrCancel_HeaderOneLine", "BasicControls_DeselectOrCancel_Tooltip", this.Element, string.Empty, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region RenderDetailsTooltip
        private static void RenderDetailsTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Controls", "Details" ), DrawNextTo, Clamp, TooltipNovelWidth.Smaller ) )
            {
                novel.CanExpand = CanExpandType.Brief;
                novel.TitleUpperLeft.AddLang( "BasicControls_FullTooltips_HeaderOneLine" );
                novel.Main.StartColor( ColorTheme.NarrativeColor );
                novel.Main.AddLang( "BasicControls_FullTooltips_Tooltip" );
                if ( InputCaching.ShouldShowDetailedTooltips )
                {
                    novel.FrameTitle.AddLang( "BasicControls_FullTooltips_Tooltip_MoreHeader" );
                    novel.FrameBody.AddLang( "BasicControls_FullTooltips_Tooltip_MoreBody" );
                }
            }
        }
        #endregion

        #region tDetailsHeader
        public class tDetailsHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_FullTooltips_HeaderTwoLine" );
            }

            public override void HandleMouseover()
            {
                RenderDetailsTooltip( this.Element, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tDetailsSubHeader
        public class tDetailsSubHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_ThisIsImportant" );
            }

            public override void HandleMouseover()
            {
                RenderDetailsTooltip( this.Element, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tDetailsText
        public class tDetailsText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddFormat1( "BasicControls_FullTooltips", InputActionTypeDataTable.Instance.GetRowByID( "ShowDetailedTooltips" ).GetHumanReadableKeyCombo() );
            }

            public override void HandleMouseover()
            {
                RenderDetailsTooltip( this.Element, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region RenderNextTurnTooltip
        private static void RenderNextTurnTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Controls", "NextTurn" ), DrawNextTo, Clamp, TooltipNovelWidth.Smaller ) )
            {
                novel.TitleUpperLeft.AddLang( "BasicControls_NextActor_HeaderOneLine" );
                novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "BasicControls_NextActor_Tooltip_P1" ).Line()
                    .AddFormat1( "BasicControls_NextActor_Tooltip_P2", InputActionTypeDataTable.Instance.GetRowByID( "ToggleForcesSidebar" ).GetHumanReadableKeyCombo(),
                    ColorTheme.PurpleDim ).Line()
                    .AddLang( "BasicControls_NextActor_Tooltip_P3" );
            }
        }
        #endregion

        #region tNextTurnHeader
        public class tNextTurnHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_NextActor_HeaderTwoLine" );
            }

            public override void HandleMouseover()
            {
                RenderNextTurnTooltip( this.Element, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tNextTurnSubHeader
        public class tNextTurnSubHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BasicControls_ThisIsImportant" );
            }

            public override void HandleMouseover()
            {
                RenderNextTurnTooltip( this.Element, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        #region tNextTurnText
        public class tNextTurnText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddFormat1( "BasicControls_NextActor", InputActionTypeDataTable.Instance.GetRowByID( "JumpToNextActorOrEndTurn" ).GetHumanReadableKeyCombo() );
            }

            public override void HandleMouseover()
            {
                RenderNextTurnTooltip( this.Element, SideClamp.AboveOrBelow );
            }

            public override bool GetShouldBeHidden()
            {
                return !InputCaching.ShowControlTipsWhenSystemMenuOpen;
            }
        }
        #endregion

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Cancel":
                case "Return":
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}
