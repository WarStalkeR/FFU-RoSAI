using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using System.IO;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LoadGameMenu : ToggleableWindowController, IInputActionHandler
    {
        public SaveFolder CurrentFolder;
        public SaveFile SelectedFile;
        public static Window_LoadGameMenu Instance;
		private SaveFileSortMethod SortMethod = SaveFileSortMethod.Date;
		private bool DeleteMode = false;
		public Window_LoadGameMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
            this.SuppressesUIScaling = true;
            this.ShouldBlurBackgroundGame = true;
            this.ShouldPauseGameWhenOpen = false;
		}

        public override void OnOpen()
        {
	        DeleteMode = false;
			this.priorPath = string.Empty;
            this.RecalculateFolders();

            hasSetFilterTextSinceLoad = false;
            Files_FilterText = string.Empty;

            if ( iSearch.Instance != null )
                iSearch.Instance.SetText( string.Empty );

            if ( this.folders.Count > 0 )
            {
                if ( this.CurrentFolder == null )
                    this.CurrentFolder = this.folders[0];
                string lastFile = GameSettings.Current.GetString( "MainGameLastFile" );
                if ( !lastFile.IsEmpty() && lastFile.Contains( "/" ) )
                {
                    string lastFolder = lastFile.Split( '/' )[0];
                    foreach ( SaveFolder folder in this.folders )
                    {
                        if ( folder.ShortName == lastFolder )
                        {
                            this.CurrentFolder = folder;
                            break;
                        }
                    }
                }

            }
            else
                this.CurrentFolder = null;

            if ( bCategory.Original != null ) //scroll left panel back to top when opening
                bCategory.Original.Element.TryScrollToTop();
        }

        private static bool hasSetFilterTextSinceLoad = false;
        private static string Files_FilterText = string.Empty;

        public override void OnClose( WindowCloseReason CloseReason )
        {
        }

        #region RecalculateFolders
        public readonly List<SaveFolder> folders = List<SaveFolder>.Create_WillNeverBeGCed( 400, "Window_LoadGameMenu-subfolders" );
        public void RecalculateFolders()
        {
            folders.Clear();

            //folders.Add( new SaveFolder( "-", Engine_Universal.CurrentPlayerDataDirectory + Engine_Universal.GameLoop.WorldSaveFolder, true ) );

            string[] dirs = ArcenIO.GetDirectories( Engine_Universal.CurrentPlayerDataDirectory + Engine_Universal.GameLoop.WorldSaveFolder );
            foreach ( string dir in dirs )
            {
                string dirShort = ArcenIO.GetFileNameWithoutExtension( dir );
                if ( dirShort.Length <= 0 )
                    continue;
                switch ( dirShort[0] )
                {
                    case '.':
                    case '~':
                        continue;
                }

                SaveFolder folder = new SaveFolder( dirShort, dir, false );
                folder.LastChanged = DateTime.Now.AddYears( -100 );
                folder.CountOfFiles = 0;

                string[] files = ArcenIO.GetFilesSorted( dir, "*.save" );

                foreach ( string file in files )
                {
                    DateTime lastWrite = ArcenIO.GetLastFileWriteTime( file );
                    if ( lastWrite > folder.LastChanged )
                        folder.LastChanged = lastWrite;
                    folder.CountOfFiles++;
                }

                folders.Add( folder );
            }
        }
        #endregion

        #region RecalculateFilesInFolder
        private string priorPath = string.Empty;
        public readonly List<SaveFile> filesInFolder = List<SaveFile>.Create_WillNeverBeGCed( 400, "Window_LoadGameMenu-filesInFolder" );
        public void RecalculateFilesInFolder( bool ForceRecalculation )
        {
            string path = string.Empty;
            if ( this.CurrentFolder == null )
                path = Engine_Universal.CurrentPlayerDataDirectory + Engine_Universal.GameLoop.WorldSaveFolder;
            else
            {
                path = this.CurrentFolder.FullPath;
                if ( !Directory.Exists( path ) )
                {
                    this.CurrentFolder = null;
                    path = Engine_Universal.CurrentPlayerDataDirectory + Engine_Universal.GameLoop.WorldSaveFolder;
                }
            }

            if ( path == this.priorPath && !ForceRecalculation )
                return;
            this.priorPath = path;

            SelectedFile = null;

            foreach ( SaveFile file in filesInFolder )
            {
                if ( file != null )
                    file.IsDeprecatedData = true; //deprecate the old ones
            }

            filesInFolder.Clear();

            string[] files = ArcenIO.GetFilesSorted( path, "*.save" );

            foreach ( string file in files )
            {
                string fileShort = ArcenIO.GetFileNameWithoutExtension( file );
                FileInfo info = new FileInfo( file );
                SaveFile save = new SaveFile();
                save.FileSize = (int)info.Length;
                save.LastChanged = info.LastWriteTime;
                save.ShortName = fileShort;
                save.FullPath = file;

                filesInFolder.Add( save );
            }
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "LoadMenu_Header" );
            }
        }
        #endregion

        #region iSearch
        public class iSearch : InputAbstractBase
        {
            public int maxTextLength = 20;
            public static iSearch Instance;
            public iSearch() { Instance = this; }
            public override char ValidateInput( char addedChar )
            {
                if ( this.GetText().Length >= this.maxTextLength )
                    return '\0';
                return addedChar;
            }

            public override void OnValueChanged( string newString )
            {
                Files_FilterText = newString;
            }

            public override void OnEndEdit()
            {
                Files_FilterText = this.GetText();
            }

            private ArcenUI_Input inputField = null;

            public override void OnMainThreadUpdate()
            {
                if ( !hasSetFilterTextSinceLoad )
                {
                    hasSetFilterTextSinceLoad = true;
                    this.SetText( Files_FilterText );
                }

                if ( inputField == null )
                {
                    inputField = (this.Element as ArcenUI_Input);
                    inputField?.SetPlaceholderTextLangKey( "ResearchWindow_SearchText" );
                }
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.ID )
                {
                    case "Cancel": //escape key
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }
        #endregion

        private static int lastNumberOfCompleteClears = 0;

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            if ( Engine_HotM.NumberOfCompleteClears != lastNumberOfCompleteClears )
            {
                Set.RefreshAllElements = true;
                lastNumberOfCompleteClears = Engine_HotM.NumberOfCompleteClears;
                return;
			}

			//bMainContentParent.ParentRT.UI_SetHeight( runningY );
		}

        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public static bMainContentParent Instance;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    Instance = this;
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }


		private static ButtonAbstractBase.ButtonPool<bCategory> btnCategoryPool;
        private static ButtonAbstractBase.ButtonPool<bSavegame> btnSavegamePool;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_LoadGameMenu.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 10, "LoadGameFolder" );
                            btnSavegamePool = new ButtonAbstractBase.ButtonPool<bSavegame>( bSavegame.Original, 10, "LoadGameFile" );
                            this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                            this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;
                        }
                    }
                    #endregion
                }

                if ( Instance.DeleteMode )
                    MouseHelper.RenderSpecificUIMouseCursorAtMouse( IconRefs.MouseDelete );

                this.OnUpdateCategories();


                if ( !hasGlobalInitialized )
                    return;

                btnSavegamePool.Clear( 5 );

                Instance.RecalculateFilesInFolder( false );
                List<SaveFile> files = Instance.filesInFolder;
				files.Sort((x, y) => {
					switch(Instance.SortMethod)
					{
						case SaveFileSortMethod.Date:
                            {
                                int val = y.LastChanged.CompareTo( x.LastChanged ); //desc
                                if ( val != 0 )
                                    return val;
                                return x.ShortName.CompareTo( y.ShortName ); //asc
                            }
						case SaveFileSortMethod.Name:
                            {
                                int val = x.ShortName.CompareTo( y.ShortName ); //asc
                                if ( val != 0 )
                                    return val;
                                return y.LastChanged.CompareTo( x.LastChanged ); //desc
                            }
						case SaveFileSortMethod.Size:
                            {
                                int val = y.FileSize.CompareTo( x.FileSize );
                                if ( val != 0 )
                                    return val;
                                val = x.ShortName.CompareTo( y.ShortName ); //asc
                                if ( val != 0 )
                                    return val;
                                return y.LastChanged.CompareTo( x.LastChanged ); //desc
                            }
					}
					return 0;
				});

                bool hasFilter = !Files_FilterText.IsEmpty();

                int addedCount = 0;
                foreach ( SaveFile file in files )
                {
                    if ( hasFilter )
                    {
                        if ( !file.ShortName.Contains( Files_FilterText, StringComparison.InvariantCultureIgnoreCase ) )
                            continue;
                    }

                    bSavegame item = btnSavegamePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( file );
                    addedCount++;
                }

                float currentY = -2.55f; //position of the first entry

                #region Positioning Logic 1
                btnSavegamePool.ApplyItemsInRows( 5.1f, ref currentY, 25.7f, 809.9f, 20.6f, false );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bSavegame.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            public void OnUpdateCategories()
            {
                float currentY = -5; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                btnCategoryPool.Clear( 5 );

                List<SaveFolder> folders = Instance.folders;

                int addedCount = 0;
                foreach ( SaveFolder folder in folders )
                {
                    bCategory item = btnCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( folder );
                    addedCount++;
                }

                //ArcenDebugging.LogSingleLine( "Folders: " + folders.Count + " addedCount: " + addedCount, Verbosity.ShowAsError );

                #region Positioning Logic 1
                btnCategoryPool.ApplyItemsInRows( 6.1f, ref currentY, 27f, 218.5f, 25.6f, false );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bCategory.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
        }

        #region bCategory
        public class bCategory : ButtonAbstractBase
        {
            public static bCategory Original;
            public bCategory() { if ( Original == null ) Original = this; }

            private SaveFolder Folder = null;

            public void Assign( SaveFolder Folder )
            {
                this.Folder = Folder;
            }

            public override bool GetShouldBeHidden()
            {
                return this.Folder == null;
            }

            public override void Clear()
            {
                this.Folder = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.Folder == null )
                    return;

                bool isSelected = Instance.CurrentFolder == this.Folder;
                this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[isSelected ? 1 : 0] );
                buffer.StartColor( isSelected ? ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) :
                    ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                buffer.AddRaw( this.Folder.ShortName );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Folder == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( Instance.DeleteMode )
                {
                    if ( this.Folder.IsRootFolder )
                        return MouseHandlingResult.None; //cannot delete the root folder
                    string[] files = ArcenIO.GetFilesUnsorted( this.Folder.FullPath, "*.save" );
                    ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                    {
                        DoDeleteFolder( this.Folder.FullPath );
                        Instance.RecalculateFolders();
                    }, null, LocalizedString.AddLang_New( "DeleteFolder_Header" ), LocalizedString.AddFormat2_New( "DeleteFolder_Body", this.Folder.ShortName, 
                    files.Length.ToStringWholeBasic() ),
                    LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                }
                else
                {
                    Instance.CurrentFolder = this.Folder;
                    //scroll right panel back to top when change category
                    if ( bMainContentParent.Instance != null )
                        bMainContentParent.Instance.Element.TryScrollToTop();
                }
                return MouseHandlingResult.None;
            }

            #region DoDeleteFolder
            public bool DoDeleteFolder( string FolderName )
            {
                try
                {
                    Directory.Delete( FolderName, true );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogWithStack( "Deleting failed: " + e, Verbosity.ShowAsError );
                }
                return true;
            }
            #endregion

            public override void HandleMouseover()
            {
                if ( this.Folder == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "LoadWindow", this.Folder.ShortName ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Simple ) )
                {
                    if ( Instance.DeleteMode )
                    {
                        string[] files = ArcenIO.GetFilesUnsorted( this.Folder.FullPath, "*.save" );

                        novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "DeleteFolder", ColorTheme.RedOrange ).AddRaw( this.Folder.ShortName );
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( this.Folder.FullPath ).Line();
                        novel.Main.AddBoldLangAndAfterLineItemHeader( "SavesInFolder", ColorTheme.DataLabelWhite ).AddRaw( 
                            files.Length.ToStringWholeBasic(), ColorTheme.DataBlue ).Line();
                    }
                    else
                    {
                        novel.TitleUpperLeft.AddRaw( this.Folder.ShortName );
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( this.Folder.FullPath ).Line();
                    }
                }
            }
        }
        #endregion

        #region bSavegame
        public class bSavegame : ButtonAbstractBase
        {
            public static bSavegame Original;
            public bSavegame() { if ( Original == null ) Original = this; }

            private SaveFile Save = null;

            public void Assign( SaveFile Save )
            {
                this.Save = Save;
            }

            public override bool GetShouldBeHidden()
            {
                return this.Save == null || this.Save.IsDeprecatedData;
            }

            public override void Clear()
            {
                this.Save = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.Save == null )
                    return;

                bool isSelected = Instance.SelectedFile == this.Save;
                this.SetRelatedImage0EnabledIfNeeded( isSelected );
                buffer.StartColor( isSelected ? ColorTheme.GetInvertibleListTextBlue_Selected( this.Element.LastHadMouseWithin ) :
                    ColorTheme.GetInvertibleListTextBlue_Normal( this.Element.LastHadMouseWithin ) );

                buffer.AddRaw( this.Save.ShortName );

                buffer.Position500().StartSize90();
                buffer.AddRaw( this.Save.LastChanged.ToFullDateTimeString() );
                buffer.EndSize();

                if ( this.Save.FileSize > 0 )
                {
                    double sizeToWrite = this.Save.FileSize / 1024f / 1024f; //to kb and then to mb

                    buffer.Position700().StartSize90();
                    buffer.AddRaw( sizeToWrite.ToStringSmallFixedDecimal( 2 ) );
                    buffer.Space1x();
                    buffer.AddLang( "Megabytes_Abbrev" );
                    buffer.EndSize();
                }
            }

            #region DoDelete
            public bool DoDelete()
            {
                if ( this.Save == null )
                    return false;
                string deletePath = this.Save.FullPath;

                if ( InputCaching.ShouldKeepDoingAction )
                {
                    WorldSaveLoad.DeleteSaveFile( deletePath );
                    Instance.RecalculateFilesInFolder( true );
                }
                else
                {
                    ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                    {
                        WorldSaveLoad.DeleteSaveFile( deletePath );
                        Instance.RecalculateFilesInFolder( true );
                    }, null, LocalizedString.AddLang_New( "Popup_AreYouSure" ),
                        LocalizedString.AddFormat2_New( "DeleteSave_Body", deletePath, InputCaching.GetGetHumanReadableKeyComboForShouldKeepDoingAction() ),
                        LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                }

                return true;
            }
            #endregion

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Save == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( Instance.DeleteMode )
                {
                    if ( DoDelete() )
                        return MouseHandlingResult.None;
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                Instance.SelectedFile = this.Save;
				if (!bSave.DoLoad() )
                    return MouseHandlingResult.PlayClickDeniedSound;

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                if ( this.Save == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "LoadWindow", this.Save.ShortName ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                {
                    if ( Instance.DeleteMode )
                    {
                        novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "DeleteFile", ColorTheme.RedOrange ).AddRaw( this.Save.ShortName );
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( this.Save.FullPath ).Line();
                    }
                    else
                    {
                        novel.TitleUpperLeft.AddRaw( this.Save.ShortName );
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( this.Save.FullPath ).Line();
                    }
                }
            }
        }
		#endregion

		public class bSort : ButtonAbstractBase
		{
			public override void GetTextToShowFromVolatile(ArcenDoubleCharacterBuffer Buffer)
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                Buffer.AddRaw( Lang.Get($"LoadSave_Sort_{Instance.SortMethod}"));
			}

			public override MouseHandlingResult HandleClick_Subclass(MouseHandlingInput input)
			{
				Instance.SortMethod = (SaveFileSortMethod)(((int)Instance.SortMethod + 1) % (int)SaveFileSortMethod.Length);
				return MouseHandlingResult.None;
			}
        }

        public class bExit : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                ArcenInput.BlockForAJustPartOfOneSecond();
                return MouseHandlingResult.None;
            }
        }

        public class bCancel : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                Buffer.AddLang( LangCommon.Popup_Common_Cancel );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                ArcenInput.BlockForAJustPartOfOneSecond();
                return MouseHandlingResult.None;
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                Buffer.AddLang( LangCommon.Popup_Common_Load );
            }

            public override bool GetShouldBeHidden()
            {
                return Instance.DeleteMode;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
				if (!DoLoad() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                return MouseHandlingResult.None;
            }

            public static bool DoLoad()
            {
				if ( Instance.SelectedFile == null )
                    return false;
                if ( Instance.CurrentFolder == null )
                    return false;

                string fileName = Instance.CurrentFolder.ShortName == "-" ? string.Empty : Instance.CurrentFolder.ShortName + "/";
                fileName += Instance.SelectedFile.ShortName;

                GameSettings.Current.SetString( "MainGameLastFile", fileName );

                VisPlanner.Instance.OnMainGameStarted();
                WorldSaveLoad.TryStartLoadFullSave( fileName );
                Instance.Close( WindowCloseReason.UserDirectRequest_InstantlyHide );

                return true;
            }
		}

        public class bDeleteMode : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor(ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                if (Instance.DeleteMode)
					Buffer.AddLang( "DisableDeleteMode" );
				else
					Buffer.AddLang( "EnableDeleteMode" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.DeleteMode = !Instance.DeleteMode;
				return MouseHandlingResult.None;
            }
        }

        public override void Close( WindowCloseReason Reason )
        {
            base.Close( Reason );
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    this.Close( WindowCloseReason.UserDirectRequest );
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }

    public class SaveFolder //just let it be gc'd
    {
        public string ShortName;
        public string FullPath;
        public DateTime LastChanged;
        public int CountOfFiles;
        public bool IsRootFolder = false;

        public SaveFolder( string ShortName, string FullPath, bool IsRootFolder )
        {
            this.ShortName = ShortName;
            this.FullPath = FullPath;
            this.IsRootFolder = IsRootFolder;
        }
    }

    public class SaveFile //just let it be gc'd
    {
        public string ShortName;
        public string FullPath;
        public DateTime LastChanged;
        public int FileSize;
        public bool IsDeprecatedData = false;
    }
}
