using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_MainMenu : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_MainMenu Instance;
        public Window_MainMenu()
        {
            Instance = this;
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

        public override void OnHideAfterNotShowing()
        {
            publisherFadeInProgress = 0.2f;
            developerFadeInProgress = 0.2f;

            if ( publisherMat )
            {
                publisherMat.SetFloat( "_Animation_Factor", publisherFadeInProgress );
                developerMat.SetFloat( "_Animation_Factor", developerFadeInProgress );
            }

            base.OnHideAfterNotShowing();
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_Universal.GameLoop.IsLevelEditor )
                return false; //never show if not the main game

            if ( WorldSaveLoad.IsSavingOrLoadingAtTheMoment )
                return false;

            if ( !A5ObjectAggregation.IsLoadingCompletelyDone )
                return false;
            if ( SimCommon.CurrentEvent != null )
                return false; //for testing reasons

            if ( Window_NewProfile.Instance?.IsOpen ?? false )
                return false;

            if ( VisCurrent.GetShouldShowAnimatedLoadingWindow() )
                return false;

            return Engine_HotM.GameStatus == MainGameStatus.MainMenu;   
        }

        private static Material publisherMat;
        private static Material developerMat;
        private static float publisherFadeInProgress = 0.2f;
        private static float developerFadeInProgress = 0.2f;
        private static float fadeInSpeed = 1.2f;

        private static bool LastFileExists = false;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasShownEarlyAccessMessage = false;

            public override void OnUpdate()
            {
                if ( !hasShownEarlyAccessMessage )
                {
                    hasShownEarlyAccessMessage = true;
                    if ( !Engine_HotM.IsDemoVersion && !GameSettings.Current.GetBool( "Debug_SkipEarlyAccessMessage" ) )
                    {
                        Window_WelcomeToEA.Instance.Open();
                    }
                }

                if ( !publisherMat )
                {
                    this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                    this.WindowController.Window.MaxDeltaTimeBeforeUpdates= 0;

                    ArcenUI_CustomUI custom = this.Element as ArcenUI_CustomUI;
                    publisherMat = custom.RelatedImages[0].material;
                    developerMat = custom.RelatedImages[1].material;
                }
                else if ( Instance.GetShouldDrawThisFrame_Subclass() )
                {
                    if ( publisherFadeInProgress < 2f )
                    {
                        publisherFadeInProgress += ArcenTime.AnyDeltaTime * fadeInSpeed;

                        publisherMat.SetFloat( "_Animation_Factor", publisherFadeInProgress );
                    }

                    if ( developerFadeInProgress < 2f && publisherFadeInProgress >= 0.7f )
                    {
                        developerFadeInProgress += ArcenTime.AnyDeltaTime * fadeInSpeed;
                        if ( developerFadeInProgress >= 2f )
                        {
                            developerFadeInProgress = 2f;
                            fadeInSpeed = 2.5f; //next time do it faster!
                        }

                        developerMat.SetFloat( "_Animation_Factor", developerFadeInProgress );
                    }
                }

                #region Calculate LastFileExists
                {
                    string lastFile = GameSettings.Current.GetString( "MainGameLastFile" );
                    if ( lastFile == null || lastFile.Length == 0 || lastFile == "null" )
                        LastFileExists = false;
                    else
                        LastFileExists = ArcenIO.FileExists( Engine_Universal.CurrentPlayerDataDirectory + Engine_Universal.GameLoop.WorldSaveFolder + lastFile + ".save" );
                }
                #endregion
            }
        }

        public class bContinue : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );
                Buffer.AddLang( "MainMenu_Continue" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                string lastFile = GameSettings.Current.GetString( "MainGameLastFile" );
                if ( lastFile.IsEmpty() || lastFile == "null" )
                    return MouseHandlingResult.InvalidResult;

                VisPlanner.Instance.OnMainGameStarted();
                ParticleSoundRefs.StartGame.DuringGame_PlaySoundOnlyAtCamera();
                WorldSaveLoad.TryStartLoadFullSave( lastFile );
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return !LastFileExists;
            }

            public override void HandleMouseover()
            {
                if ( !LastFileExists )
                    return;

                string lastFile = GameSettings.Current.GetString( "MainGameLastFile" );
                if ( lastFile.IsEmpty() || lastFile == "null" )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MainMenu", "Continue" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "MainMenu_Continue" );

                    novel.Main.StartColor( ColorTheme.NarrativeColor );
                    if ( lastFile.Contains( "/" ) )
                    {
                        string[] parts = lastFile.Split( '/' );

                        novel.Main.AddRaw( parts[1] ).Line();
                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Profile_Header", ColorTheme.DataLabelWhite ).AddRaw( parts[0] );
                    }
                    else
                    {
                        novel.Main.AddLang( lastFile );
                    }
                }
            }
        }

        public class bStartNew : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );

                if ( LastFileExists )
                    Buffer.AddLang( "MainMenu_StartAnotherProfile" );
                else
                    Buffer.AddLang( "MainMenu_Begin" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                SFXItemTable.TryPlayItemByName( "RobotSpeakPop", 1f );
                Window_NewProfile.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MainMenu", "StartNew" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    if ( LastFileExists )
                    {
                        novel.TitleUpperLeft.AddLang( "MainMenu_StartAnotherProfile" );
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_StartAnotherProfile_Details" ).Space1x()
                            .AddLang( "MainMenu_StartAnotherProfile_Details2" );
                    }
                    else
                    {
                        novel.TitleUpperLeft.AddLang( "MainMenu_Begin" );
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_Begin_Details" );
                    }
                }
            }
        }

        public class bLoad : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );
                Buffer.AddRaw( LangCommon.Popup_Common_Load.Text );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_LoadGameMenu.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MainMenu", "Load" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddRaw( LangCommon.Popup_Common_Load.Text );
                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_Load_Details" );
                }
            }
        }

        public class bCredits : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );
                Buffer.AddLang( "MainMenu_Credits" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_Credits.Instance.Open();

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MainMenu", "Credits" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "MainMenu_Credits" );
                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_Credits_Details" );
                }
            }
        }

        public class bReleaseNotesOrWishlist : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Engine_HotM.IsDemoVersion )
                {
                    Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );
                    Buffer.AddLang( "MainMenu_Wishlist" );
                }
                else
                {
                    Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );
                    Buffer.AddLang( "MainMenu_ReleaseNotes" );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Engine_HotM.IsDemoVersion )
                    VisCommands.GoToWebsite( "https://store.steampowered.com/app/2001070" ); //store page for the game
                else
                    VisCommands.GoToWebsite( "https://wiki.arcengames.com/index.php?title=HotM:Post_Launch" );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MainMenu", "Wishlist" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    if ( Engine_HotM.IsDemoVersion )
                    {
                        novel.TitleUpperLeft.AddLang( "MainMenu_Wishlist" );
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_Wishlist_Details" );
                    }
                    else
                    {
                        novel.TitleUpperLeft.AddLang( "MainMenu_ReleaseNotes" );
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_ReleaseNotes_Details" );
                    }
                }
            }
        }

        public class bControls : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );
                Buffer.AddLang( "MainMenu_Controls" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ControlBindingsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MainMenu", "Controls" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "MainMenu_Controls" );
                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_Controls_Details" );
                }
            }
        }

        //public class bAboutGame : ButtonAbstractBase
        //{
        //    public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
        //    {
        //        Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );
        //        Buffer.AddLang( "MainMenu_AboutGame" );
        //    }

        //    private readonly ArcenDoubleCharacterBuffer popupBuffer = new ArcenDoubleCharacterBuffer( "Window_MainMenu-bAboutGame-popupBuffer" );

        //    public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
        //    {
        //        popupBuffer.EnsureResetForNextUpdate();
        //        AddPair( "Introduction" );
        //        AddPair( "BigPicture" );
        //        AddPair( "EarlyFlow" );
        //        AddPair( "OngoingFlow" );
        //        AddPair( "Relax" );
        //        AddPair( "Difficulty" );
        //        AddPair( "Tactical" );
        //        AddPair( "Heart" );
        //        AddPair( "Checklist" );
        //        AddPair( "Endings" );
        //        AddPair( "CityBuilding" );
        //        AddPair( "NewStuff" );
        //        AddPair( "Tapestry" );
        //        AddPair( "Subterfuge" );
        //        AddPair( "Humans" );
        //        AddPair( "Last" );

        //        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Tall, null, LocalizedString.AddLang_New( "AboutGame_Header" ),
        //            LocalizedString.AddRaw_New( popupBuffer.GetStringAndResetForNextUpdate() ), LangCommon.Popup_Common_Close.LocalizedString );
        //        return MouseHandlingResult.None;
        //    }

        //    private void AddPair( string center )
        //    {
        //        WriteHeader( "AboutGame_" + center + "_Header" );
        //        WriteBody( "AboutGame_" + center + "_Body" );
        //    }

        //    private void WriteHeader( string LangKey )
        //    {
        //        popupBuffer.StartSize90().StartBold().AddLang( LangKey, ColorTheme.HeaderGoldMoreRich ).EndBold().EndSize().Line();
        //    }

        //    private void WriteBody( string LangKey )
        //    {
        //        popupBuffer.StartSize90().AddLang( LangKey, ColorTheme.PurpleBarelyDim ).EndSize().Line();
        //    }

        //    public override void HandleMouseover()
        //    {
        //        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

        //        if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MainMenu", "AboutGame" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
        //        {
        //            novel.TitleUpperLeft.AddLang( "MainMenu_AboutGame" );
        //            novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_AboutGame_Details" );
        //        }
        //    }
        //}

        public class bSettings : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );
                Buffer.AddLang( "MainMenu_Settings" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_SettingsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MainMenu", "Settings" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "MainMenu_Settings" );
                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_Settings_Details" );
                }
            }
        }

        public class bExitToOS : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetNone( this.Element.LastHadMouseWithin ) );
                Buffer.AddLang( "MainMenu_Exit" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ExitToOSCheck();
                return MouseHandlingResult.None;
            }

            public static void ExitToOSCheck()
            {
                WorldSaveLoad.ExitToOS();
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MainMenu", "Exit" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "MainMenu_Exit" );
                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainMenu_Exit_Details" );
                }
            }
        }

        #region tVersionText
        public class tVersionText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( GameVersionTable.Instance.CurrentVersion != null )
                    Buffer.AddFormat1( "MainMenu_VersionPrefix", GameVersionTable.Instance.CurrentVersion.GetAsString() );
                if ( A5ObjectAggregation.TotalLoadTime <= 0 )
                {
                    if ( !A5ObjectAggregation.GetHasLoadedAllLevelItems() )
                    {
                        Buffer.Line().AddLang( "MainMenu_BackgroundLoading" );
                        //The below are never translated because they are just small codes expressing what is still loading in a way only understandable to us.
                        if ( A5ObjectAggregation.A5LevelItems_TimeLastLoaded <= 0 )
                            Buffer.AddNeverTranslated( " NTL", true );
                        if ( A5ObjectAggregation.A5LevelItems_Loaded < 100 )
                            Buffer.AddNeverTranslated( " IL" + A5ObjectAggregation.A5LevelItems_Loaded, true );
                        if ( A5ObjectAggregation.A5LevelItemCategories_Loaded < 10 )
                            Buffer.AddNeverTranslated( " IC" + A5ObjectAggregation.A5LevelItemCategories_Loaded, true );
                        if ( A5ObjectAggregation.A5LevelItems_StillLoading < 10 )
                            Buffer.AddNeverTranslated( " ISL" + A5ObjectAggregation.A5LevelItems_StillLoading, true );
                        if ( A5ObjectAggregation.A5LevelItemCategories_StillLoading < 10 )
                            Buffer.AddNeverTranslated( " ICL" + A5ObjectAggregation.A5LevelItemCategories_StillLoading, true );
                    }
                    else
                    {
                        Buffer.Line().AddLang( "MainMenu_BackgroundLinking" );
                    }
                }
                else
                    Buffer.Line().AddFormat1( "MainMenu_LoadTime", A5ObjectAggregation.TotalLoadTime.ToStringSmallFixedDecimal( 1 ) );
            }
        }
        #endregion

        #region dLanguage
        public class dLanguage : DropdownAbstractBase
        {
            public static dLanguage Instance;
            public dLanguage()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                LocalizationType localizationType = Item as LocalizationType;

                if ( localizationType != null )
                {
                    if ( GameSettings.CurrentLanguage != localizationType )
                    {
                        GameSettings.CurrentLanguage = localizationType;
                        GameSettings.CurrentLanguage.LoadLanguageFile();
                        GameSettings.SaveToDisk(); //make sure this is not forgotten
                    }
                }
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                if ( elementAsType == null )
                    return;

                //int tempValue = setting.TempValue_Int;
                if ( elementAsType.GetItemCount() != LocalizationTypeTable.Instance.Rows.Length )
                {
                    elementAsType.ClearItems();
                    foreach ( LocalizationType row in LocalizationTypeTable.Instance.Rows )
                        elementAsType.AddItem( row, row == GameSettings.CurrentLanguage );
                }
            }
            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( GameSettings.CurrentLanguage ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "LanguageSetting_Name" );
                    novel.TitleUpperLeft.AddRaw( GameSettings.CurrentLanguage.GetDisplayName() );

                    novel.Main.StartColor( ColorTheme.NarrativeColor );
                    if ( !GameSettings.CurrentLanguage.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( GameSettings.CurrentLanguage.GetDescription() ).Line();

                    novel.Main.AddLang( "SetDefaults_Ignores", ColorTheme.DataBlue ).Line();
                }
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                LocalizationType localizationType = Item as LocalizationType;
                if ( localizationType == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( GameSettings.CurrentLanguage ), ItemElement, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddRaw( localizationType.GetDisplayName() );

                    if ( !localizationType.GetDescription().IsEmpty() )
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( localizationType.GetDescription() ).Line();
                }
            }
        }
        #endregion

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            //switch ( InputActionType.ID )
            //{
            //    case "Cancel":
            //    case "Return":
            //        //make sure no other input is processed for 0.2 of a second, so that for instance this doesn't open the escape menu.
            //        ArcenInput.BlockForAJustPartOfOneSecond();
            //        bExitToOS.ExitToOSCheck();
            //        break;
            //}
        }
    }
}
