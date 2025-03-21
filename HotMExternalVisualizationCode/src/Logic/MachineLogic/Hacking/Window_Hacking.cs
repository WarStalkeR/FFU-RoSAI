using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.ExternalVis.Hacking;

namespace Arcen.HotM.ExternalVis
{
    using scene = HackingScene;
    using vScene = Arcen.HotM.Visualization.HackingScene;
    using vCell = Arcen.HotM.Visualization.HackCell;

    public class Window_Hacking : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_Hacking Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_Hacking()
        {
            Instance = this;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            if ( lowerMode == null || lowerMode.ID != "HackingScene" )
                return false;
            if ( VisCurrent.IsInPhotoMode )
                return false;

            return true;
        }

        #region tResources
        public class tResources : TextAbstractBase
        {
            public static Vector2 size;
            public static bool hasText;

            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( scene.HasLoggedFailure )
                {
                    Buffer.AddLang( "Hacking_FailedItsTimeToLeave_Brief", ColorTheme.RedOrange2 );
                    return;
                }

                Buffer.StartLink( false, string.Empty, "MentalEnergy", string.Empty )
                    .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_4, ColorTheme.HeaderGoldMoreRich );
                Buffer.AddRaw( ResourceRefs.MentalEnergy.Current.ToStringWholeBasic() ).EndLink( false, false );

                HandleResource( Buffer, ResourceRefs.Determination );
                HandleResource( Buffer, ResourceRefs.Wisdom );
                HandleResource( Buffer, ResourceRefs.Creativity );

                //if ( scene.HackerUnit != null )
                //{
                //    Buffer.Space3x().StartLink( false, string.Empty, "ActionPoint", string.Empty )
                //        .AddLangAndAfterLineItemHeader( "ActionPoint_Abbrev", ColorTheme.HeaderGoldMoreRich );
                //    Buffer.AddRaw( scene.HackerUnit.CurrentActionPoints.ToStringWholeBasic() ).EndLink( false, false );
                //}

                if ( scene.DaemonMovesSoFar > 0 )
                {
                    Buffer.Space3x().StartLink( false, string.Empty, "MovesSoFar", string.Empty )
                        .AddLangAndAfterLineItemHeader( "Hacking_MovesSoFar_Brief", ColorTheme.HeaderGoldMoreRich );
                    Buffer.AddRaw( scene.DaemonMovesSoFar.ToStringWholeBasic() ).EndLink( false, false );
                }
            }

            private void HandleResource( ArcenDoubleCharacterBuffer Buffer, ResourceType Resource )
            {
                Buffer.Space3x().StartLink( false, string.Empty, "Resource", Resource.ID )
                    .AddSpriteStyled_NoIndent( Resource.Icon, AdjustedSpriteStyle.InlineLarger1_4, Resource.IconColorHex );
                Buffer.AddRaw( Resource.Current.ToStringWholeBasic() ).EndLink( false, false );
            }

            public override void DoAfterTextIsUpdated( ArcenUIWrapperedTMProText Text )
            {
                size = Text.SetTextNowIfNeededAndGetSize( false, 0f );
                hasText = true;

                if ( CustomParentInstance != null )
                    CustomParentInstance.Element.RelatedTransforms[0].UI_SetWidth( size.x + 32f );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {

            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                switch ( TooltipLinkData[0] )
                {
                    case "MentalEnergy":
                        {
                            ResourceRefs.MentalEnergy.WriteResourceTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipInstruction.ForExistingObject, TooltipExtraText.None );
                        }
                        break;
                    case "Resource":
                        {
                            ResourceTypeTable.Instance.GetRowByID( TooltipLinkData[1] ).WriteResourceTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipInstruction.ForExistingObject, TooltipExtraText.None );
                        }
                        break;
                    case "MovesSoFar":
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Hacking", TooltipLinkData[0] ), null, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                            {
                                novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Hacking_MovesSoFar_Header" )
                                    .AddRaw( scene.DaemonMovesSoFar.ToStringWholeBasic() );
                                novel.Main.AddLang( "Hacking_MovesSoFar_Description" );
                            }
                        }
                        break;
                }
            }
        }
        #endregion

        #region tHackingInterfaceHeader
        public class tHackingInterfaceHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "HackingInterface_Header" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {

            }
        }
        #endregion

        #region tHackingLog
        public class tHackingLog : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                for ( int i = MathA.Max( 0, scene.HackingHistory.Count - 7 ); i < scene.HackingHistory.Count; i++ )
                    Buffer.AddRaw( scene.HackingHistory.GetElement( i ) ).Line();
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Hacking", "tHackingLog" ), null, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "Hacking_SessionLog_Header" );

                    novel.Main.StartStyleLineHeightA();
                    foreach ( string text in scene.HackingHistory )
                        novel.Main.AddRaw( text ).Line();
                }
            }
        }
        #endregion

        public override bool GetIsCloseBlocked( WindowCloseReason CloseReason )
        {
            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            if ( lowerMode == null || lowerMode.ID != "HackingScene" )
                return false; //we already left, don't ask us things

            switch ( CloseReason )
            {
                case WindowCloseReason.UserCasualRequest:
                    {
                        if ( scene.PlayerShards.Count == 0 || scene.HasLoggedFailure )
                            break; //go ahead and close, as the player is dead

                        ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small,
                            delegate { Engine_HotM.CurrentLowerMode = null; FlagRefs.ResetHackingTutorial(); }, null,
                            LocalizedString.AddLang_New( "Hacking_Close_AreYouSure_Header" ),
                            LocalizedString.AddLang_New( "Hacking_Close_AreYouSure_Body" ),
                            LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                    }
                    return true;
            }

            return false;
        }

        public override void Close( WindowCloseReason CloseReason )
        {
            switch (CloseReason)
            {
                case WindowCloseReason.UserCasualRequest:
                    {
                        if ( scene.PlayerShards.Count == 0 || scene.HasLoggedFailure )
                            break; //go ahead and close, as the player is dead

                        ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small,
                            delegate { Engine_HotM.CurrentLowerMode = null; FlagRefs.ResetHackingTutorial(); }, null,
                            LocalizedString.AddLang_New( "Hacking_Close_AreYouSure_Header" ),
                            LocalizedString.AddLang_New( "Hacking_Close_AreYouSure_Body" ),
                            LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                    }
                    return;
            }

            FlagRefs.ResetHackingTutorial();
            Engine_HotM.CurrentLowerMode = null;
        }

        public static ButtonAbstractBase.ButtonPool<bHackingAction> bHackingActionPool;
        public static MeshControllerAbstractBase.Pool<iHacker> iHackerPool;
        public static WorldSpriteControllerAbstractBase.Pool<iParticle> iParticlePool;

        public static MeshControllerAbstractBase.Pool<iIndicatorBlood> iIndicatorBloodPool;
        public static MeshControllerAbstractBase.Pool<iIndicatorTargetingB> iIndicatorTargetingBPool;
        public static MeshControllerAbstractBase.Pool<iIndicatorWater> iIndicatorWaterPool;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {   
            public customParent()
            {
                Window_Hacking.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            public const float CATEGORY_ICON_X_START = -8f;
            public const float CATEGORY_ICON_Y = 5f;
            public const float CATEGORY_ICON_SIZE = 46f;
            public const float CATEGORY_ICON_X_SPACING = 2f;

            //private float TimeLastChanged = 0f;

            private static readonly List<hCell> priorRotated = List<hCell>.Create_WillNeverBeGCed( 20, "Window_Hacking-priorRotated" );
            
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( VisCurrent.IsUIHiddenExceptForSidebar || scene.HackerUnit == null || scene.TargetUnit == null )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                if ( Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0f;

                        this.WindowController.myScale = 0.95f;

                        if ( vScene.Instance )
                        {
                            hasGlobalInitialized = true;
                            InitializeObjects();
                        }
                    }
                    #endregion

                    if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                        Instance.Close( WindowCloseReason.ShowingRefused );

                    if ( bHackingActionPool == null )
                        return;

                    bHackingActionPool.Clear( 5 );

                    if ( scene.CurrentHack != null && scene.CurrentHack.DuringGame_IsLocked( scene.IsInfiltrationSysOpScenario ) )
                        scene.CurrentHack = null;

                    #region Draw Category Icons
                    if ( !scene.HasLoggedFailure)
                    {
                        float currentX = CATEGORY_ICON_X_START;
                        int categoryIndex = 1;
                        foreach ( HackingAction action in HackingActionTable.Instance.Rows )
                        {
                            if ( action.DuringGame_IsInvisible( scene.IsInfiltrationSysOpScenario ) )
                                continue;

                            if ( scene.CurrentHack == null && !action.DuringGame_IsLocked( scene.IsInfiltrationSysOpScenario ) )
                                scene.CurrentHack = action;

                            scene.HacksByIndex[categoryIndex] = action;
                            DrawHackAction( categoryIndex, action, ref currentX );
                            categoryIndex++;
                        }
                    }
                    #endregion

                    #region Rotate-Jiggle Cells
                    //if ( ArcenTime.UnpausedTimeSinceStartF > TimeLastChanged + 0.25f )
                    //{
                    //    TimeLastChanged = ArcenTime.UnpausedTimeSinceStartF;
                    //    RotateJiggle();
                    //}
                    #endregion

                    if ( !scene.HasPopulatedScene && hasGlobalInitialized )
                        scene.PopulateScene( Engine_Universal.PermanentQualityRandom );
                    else if ( scene.HasPopulatedScene && hasGlobalInitialized )
                        scene.Scenario.Implementation.TryHandleHackingScenarioLogic( scene.Scenario, HackingScenarioLogic.DoPerFrame, Engine_Universal.PermanentQualityRandom );

                    if ( Engine_HotM.MarkableUnderMouse is vCell vc )
                        scene.HoveredCell = vc.RelatedObject as hCell;
                    else
                        scene.HoveredCell = null;

                    scene.DoPerFrame( scene.HasPopulatedScene && hasGlobalInitialized );

                }
            }

            private void RotateJiggle()
            {
                foreach ( hCell prior in priorRotated )
                    prior.Trans.localRotation = Quaternion.identity;
                priorRotated.Clear();

                for ( int i = 0; i < 10; i++ )
                {
                    hCell item = scene.AllHackingCells.GetRandom( Engine_Universal.PermanentQualityRandom );

                    item.Trans.localRotation = Quaternion.Euler( 0f, 0f, Engine_Universal.PermanentQualityRandom.NextFloat( -5f, 5f ) );
                    priorRotated.Add( item );
                }
            }

            private void InitializeObjects()
            {
                InitializeCells();
                bHackingActionPool = new ButtonAbstractBase.ButtonPool<bHackingAction>( bHackingAction.Original, 10, "bHackingAction" );
                InitializeIndicators();
            }

            #region InitializeCells
            private void InitializeCells()
            {
                scene.ArrayWidth = vScene.Instance.MaxArrayWidth;
                scene.ArrayHeight = vScene.Instance.ArrayHeight;

                scene.HackingCellArray = new hCell[scene.ArrayWidth, scene.ArrayHeight];

                foreach ( vCell cell in vScene.Instance.AllCells )
                {
                    if ( cell == null )
                        continue;

                    hCell hCell = cell.gameObject.AddComponent<hCell>();
                    hCell.Root = cell;
                    cell.RelatedObject = hCell;
                    hCell.ArrayX = cell.ArrayX;
                    hCell.ArrayY = cell.ArrayZ;
                    hCell.Fire = cell.Fire;
                    hCell.Threaten = cell.Threaten;
                    hCell.Text = ArcenWorldWrapperedTMProText.Create( "Window_Hacking-" + cell.name, cell.Text );
                    hCell.Trans = cell.Trans;
                    cell.CenterPos = cell.Trans.position;
                    cell.CenterPos.y += vScene.Instance.CenterOffsetY;
                    hCell.CenterPos = cell.CenterPos;
                    if ( !hCell.Trans )
                        hCell.Trans = cell.transform;

                    scene.HackingCellArray[hCell.ArrayX, hCell.ArrayY] = hCell;
                    scene.AllHackingCells.Add( hCell );
                }

                foreach ( hCell item in scene.AllHackingCells )
                {
                    int x = item.ArrayX;
                    int y = item.ArrayY;
                    if ( y > 0 )
                        item.North = scene.HackingCellArray[x, y - 1];
                    if ( y < scene.ArrayHeight - 1 )
                        item.South = scene.HackingCellArray[x, y + 1];

                    if ( x > 0 )
                        item.West = scene.HackingCellArray[x - 1, y];
                    if ( x < scene.ArrayWidth - 1 )
                        item.East = scene.HackingCellArray[x + 1, y];

                    if ( item.North != null )
                        item.AdjacentCells.Add( item.North );
                    if ( item.South != null )
                        item.AdjacentCells.Add( item.South );
                    if ( item.East != null )
                        item.AdjacentCells.Add( item.East );
                    if ( item.West != null )
                        item.AdjacentCells.Add( item.West );

                    for ( x = item.ArrayX - 1; x <= item.ArrayX + 1; x++ )
                    {
                        for ( y = item.ArrayY - 1; y <= item.ArrayY + 1; y++ )
                        {
                            if ( y > 0 && y < scene.ArrayHeight - 1 &&
                                x > 0 && x < scene.ArrayWidth - 1 )
                            {
                                hCell other = scene.HackingCellArray[x, y];
                                if ( other != item && other != null )
                                    item.AdjacentCellsAndDiagonal.Add( other );
                            }
                        }
                    }

                    for ( x = item.ArrayX - 2; x <= item.ArrayX + 2; x++ )
                    {
                        for ( y = item.ArrayY - 2; y <= item.ArrayY + 2; y++ )
                        {
                            if ( y > 0 && y < scene.ArrayHeight - 1 &&
                                x > 0 && x < scene.ArrayWidth - 1 )
                            {
                                hCell other = scene.HackingCellArray[x, y];
                                if ( other != item && other != null )
                                    item.AdjacentCellsAndDiagonal2X.Add( other );
                            }
                        }
                    }

                    x = item.ArrayX;
                    y = item.ArrayY;

                    //ArcenDebugging.LogSingleLine( "item: " + x + "," + y + " adjacentCells: " + item.AdjacentCells.Count +
                    //    " AdjacentCellsAndDiagonal: " + item.AdjacentCellsAndDiagonal.Count +
                    //    " AdjacentCellsAndDiagonal2X: " + item.AdjacentCellsAndDiagonal2X.Count, Verbosity.DoNotShow );

                    //ArcenDebugging.LogSingleLine( "item: " + x + "," + y +
                    //    " north: " + (item.North?.ArrayX ?? -3) + "," + (item.North?.ArrayY ?? -3) +
                    //    " south: " + (item.South?.ArrayX ?? -3) + "," + (item.South?.ArrayY ?? -3) +
                    //    " east: " + (item.East?.ArrayX ?? -3) + "," + (item.East?.ArrayY ?? -3) +
                    //    " west: " + (item.West?.ArrayX ?? -3) + "," + (item.West?.ArrayY ?? -3), Verbosity.DoNotShow );
                }
            }
            #endregion

            #region InitializeIndicators
            private void InitializeIndicators()
            {
                InitializeHacker();
                InitializeParticle();
                InitializeBlood();
                InitializeTargetingB();
                InitializeWater();
                InitializeIndicatorBorder();
            }

            private void InitializeHacker()
            {
                iHacker orig = vScene.Instance.iHacker.gameObject.AddComponent<iHacker>();
                orig.DoLinkages();
                iHackerPool = new MeshControllerAbstractBase.Pool<iHacker>( orig, 10, "iHacker" );
            }

            private void InitializeParticle()
            {
                iParticle orig = vScene.Instance.iParticle.gameObject.AddComponent<iParticle>();
                orig.DoLinkages();
                iParticlePool = new WorldSpriteControllerAbstractBase.Pool<iParticle>( orig, 10, "iParticle", true );
            }


            private void InitializeBlood()
            {
                iIndicatorBlood orig = vScene.Instance.iIndicatorBlood.gameObject.AddComponent<iIndicatorBlood>();
                orig.DoLinkages();
                iIndicatorBloodPool = new MeshControllerAbstractBase.Pool<iIndicatorBlood>( orig, 10, "iIndicatorBlood" );
                }

            private void InitializeTargetingB()
            {
                iIndicatorTargetingB orig = vScene.Instance.iIndicatorTargetingB.gameObject.AddComponent<iIndicatorTargetingB>();
                orig.DoLinkages();
                iIndicatorTargetingBPool = new MeshControllerAbstractBase.Pool<iIndicatorTargetingB>( orig, 10, "iIndicatorTargetingB" );
            }


            private void InitializeWater()
            {
                iIndicatorWater orig = vScene.Instance.iIndicatorWater.gameObject.AddComponent<iIndicatorWater>();
                orig.DoLinkages();
                iIndicatorWaterPool = new MeshControllerAbstractBase.Pool<iIndicatorWater>( orig, 10, "iIndicatorWater" );
            }

            private void InitializeIndicatorBorder()
            {
                iIndicatorBorder.Instance = vScene.Instance.iIndicatorBorder.gameObject.AddComponent<iIndicatorBorder>();
                iIndicatorBorder.Instance.DoLinkages();
            }
            #endregion

            #region DrawHackAction
            private void DrawHackAction( int CategoryIndex, HackingAction hack, ref float currentX )
            {
                bHackingAction icon = bHackingActionPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    float iconY = CATEGORY_ICON_Y;
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref iconY, false, false, CATEGORY_ICON_SIZE, CATEGORY_ICON_SIZE, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool isPerformable = !hack.DuringGame_IsLocked( scene.IsInfiltrationSysOpScenario );
                        bool isSelected = scene.CurrentHack == hack && isPerformable;

                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.AddRaw( hack.ShortName.Text, isPerformable ? string.Empty : "545380" );
                                    icon.SetRelatedImage0SpriteIfNeeded( hack.Icon.GetSpriteForUI() );

                                    if ( isSelected )
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( "ffffff" );
                                        icon.SetRelatedImage1SpriteIfNeeded( element.RelatedSprites[1] );
                                    }
                                    else
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? "ffffff" : "545380" );
                                        icon.SetRelatedImage1SpriteIfNeeded( element.RelatedSprites[isPerformable ? 0 : 2] );
                                    }
                                }
                                break;
                            case UIAction.GetOtherTextToShowFromVolatile:
                                {
                                    switch ( ExtraData.Int )
                                    {
                                        case 0:
                                            ExtraData.Buffer.AddRaw( CategoryIndex.ToString(), isSelected ? "8DE2D7" :( isPerformable ? "5184B7" : "545380") );
                                            break;
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( !isPerformable )
                                        break;
                                    hack?.RenderActionTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None );
                                }
                                break;
                            case UIAction.OnClick:
                                if ( hack != null )
                                {
                                    if ( hack.DuringGame_IsLocked( scene.IsInfiltrationSysOpScenario ) )
                                        break;

                                    switch ( hack.ID )
                                    {
                                        case "Run":
                                            if ( FlagRefs.HackTut_RunRules.DuringGameplay_CompleteIfActive() )
                                                FlagRefs.HackTut_MakeAMove.DuringGameplay_StartIfNeeded();
                                            break;
                                    }

                                    scene.CurrentHack = hack;
                                }
                                break;
                        }
                    } );

                    currentX += CATEGORY_ICON_SIZE + CATEGORY_ICON_X_SPACING;
                }

            }
            #endregion
        }

        #region hCell
        public class hCell : MonoBehaviour
        {
            public Transform Trans;
            public Vector3 CenterPos;

            public ArcenWorldWrapperedTMProText Text;
            public vCell Root;
            public MeshRenderer Fire;
            public MeshRenderer Threaten;

            public hCell North;
            public hCell South;
            public hCell East;
            public hCell West;

            public readonly List<hCell> AdjacentCells = List<hCell>.Create_WillNeverBeGCed( 5, "hCell", 1 );
            public readonly List<hCell> AdjacentCellsAndDiagonal = List<hCell>.Create_WillNeverBeGCed( 9, "hCell", 1 );
            public readonly List<hCell> AdjacentCellsAndDiagonal2X = List<hCell>.Create_WillNeverBeGCed( 26, "hCell", 1 );

            public int ArrayX;
            public int ArrayY;

            public IHEntity CurrentEntity = null;
            public bool IsOnFire = false;
            public bool IsBeingThreatened = false;

            public bool IsBlockedFromDaemonsMovingHere = false;

            public void SetBlockedFromDaemonsMovingHere( bool val )
            {
                this.IsBlockedFromDaemonsMovingHere = val;
            }

            #region ClearExistingData
            public void ClearExistingData()
            {
                CurrentNumber = 0;
                CurrentNumberString = string.Empty;
                this.IsBlocked = false;
                this.CurrentEntity = null;

                if ( this.IsOnFire )
                {
                    this.IsOnFire = false;
                    this.Fire.enabled = false;
                }

                if ( this.IsBeingThreatened )
                {
                    this.IsBeingThreatened = false;
                    this.Threaten.enabled = false;
                }
            }
            #endregion

            #region CalculateScreenPos
            public Vector3 CalculateScreenPos()
            {
                Camera cam = Engine_HotM.CurrentLowerMode?.MainCamera;
                if ( !cam )
                    return this.CenterPos;
                return cam.WorldToScreenPoint( this.CenterPos );
            }
            #endregion

            public int CurrentNumber = 0;
            public string CurrentNumberString = string.Empty;
            public bool IsBlocked = false;

            public void SetCurrentNumber( int number )
            {
                this.CurrentNumber = number;
                this.CurrentNumberString = number.ToString( "00" );
            }

            public void SetIsOnFire( bool isOnFire )
            {
                if ( this.IsOnFire == isOnFire )
                    return;

                this.IsOnFire = isOnFire;
                this.Fire.enabled= isOnFire;
            }

            public void SetIsBeingThreatened( bool isBeingThreatened )
            {
                if ( this.IsBlocked || this.CurrentNumber == 0 ) //this looks wrong, otherwise
                    isBeingThreatened = false;

                if ( this.IsBeingThreatened == isBeingThreatened )
                    return;

                this.IsBeingThreatened = isBeingThreatened;
                this.Threaten.enabled = isBeingThreatened;
            }

            public bool GetIsHovered()
            {
                return this == scene.HoveredCell;
            }

            public bool GetAreAdjacent( hCell Other )
            {
                if ( Other == null || Other == this ) 
                    return false;
                return Other == this.North ||
                    Other == this.South ||
                    Other == this.East ||
                    Other == this.West;
            }

            public void Update()
            {
                bool isHovered = this.GetIsHovered();

                ArcenDoubleCharacterBuffer buffer = this.Text.StartWritingToBuffer();
                this.GetText( buffer, isHovered );
                this.Text.FinishWritingToBuffer();
                this.Text.MustBeCalledPerFrame();

                if ( isHovered )
                {
                    if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                    {
                        if ( scene.CurrentHack != null )
                            scene.CurrentHack.Implementation.TryHandleHackingAction( scene.CurrentHack, HackingActionLogic.LeftClick );
                    }
                    else if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                    {
                        if ( scene.CurrentHack != null )
                            scene.CurrentHack.Implementation.TryHandleHackingAction( scene.CurrentHack, HackingActionLogic.RightClick );
                    }
                }
            }

            private void GetText( ArcenDoubleCharacterBuffer Buffer, bool isHovered )
            {
                if ( this.CurrentEntity != null && this.CurrentEntity is Daemon )
                { } //no text
                else
                {
                    if ( scene.PlayerShards.Count <= 0 || scene.HasLoggedFailure )
                        Buffer.AddRaw( this.CurrentNumberString, ColorTheme.GetHack_IsOnFire( isHovered ) );
                    else if ( isHovered && scene.LastColorSetForHoveredCell == this && !scene.SpecialColorForHoveredCell.IsEmpty() )
                    {
                        if ( this.IsBeingThreatened )
                            Buffer.AddRaw( this.CurrentNumberString, ColorTheme.GetHack_IsBeingThreatened( isHovered ) );
                        else
                            Buffer.AddRaw( this.CurrentNumberString, scene.SpecialColorForHoveredCell );
                    }
                    else if ( this.CurrentNumber <= 0 )
                        Buffer.AddRaw( this.CurrentNumberString, ColorTheme.GetHack_IsBurnedOut( isHovered ) );
                    else if ( this.IsOnFire )
                        Buffer.AddRaw( this.CurrentNumberString, ColorTheme.GetHack_IsOnFire( isHovered ) );
                    else if ( this.IsBeingThreatened )
                        Buffer.AddRaw( this.CurrentNumberString, ColorTheme.GetHack_IsBeingThreatened( isHovered ) );
                    else
                        Buffer.AddRaw( this.CurrentNumberString, this.IsBlocked ? ColorTheme.GetHack_Blocked( isHovered ) : ColorTheme.GetHack_Normal( isHovered ) );
                }
            }
        }
        #endregion

        #region bClose
        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( LangCommon.Popup_Common_Close );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region  bMainContentParent
        //not actually needed at this time, but needed for compilation
        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }
        #endregion

        #region Handle
        private static void SelectHackByIndex( int index )
        {
            if ( index < 0 || index >= scene.HacksByIndex.Length )
                return;
            HackingAction hack = scene.HacksByIndex[index];
            if ( hack != null && !hack.DuringGame_IsLocked( scene.IsInfiltrationSysOpScenario ) )
                scene.CurrentHack = hack;
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                //case "Return":
                //    Instance.Close( WindowCloseReason.UserDirectRequest );
                //    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                //    ArcenInput.BlockForAJustPartOfOneSecond();
                //    break;
                case "Ability1":
                    SelectHackByIndex( 1 );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "Ability2":
                    SelectHackByIndex( 2 );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "Ability3":
                    SelectHackByIndex( 3 );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "Ability4":
                    SelectHackByIndex( 4 );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "Ability5":
                    SelectHackByIndex( 5 );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "Ability6":
                    SelectHackByIndex( 6 );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "Ability7":
                    SelectHackByIndex( 7 );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "Ability8":
                    SelectHackByIndex( 8 );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "Ability9":
                    SelectHackByIndex( 9 );
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "ToggleVisualPause":
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        if ( Time.timeScale == 0 )
                        {
                            Time.timeScale = SimCommon.TimeScaleIndexes[SimCommon.DesiredTimeScaleIndexNotPaused];
                        }
                        else
                            Time.timeScale = 0;
                    }
                    break;
                case "DecreaseVisualSpeed":
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        SimCommon.DesiredTimeScaleIndexNotPaused++;
                        if ( SimCommon.DesiredTimeScaleIndexNotPaused > SimCommon.TimeScaleIndexes.Length - 1 )
                            SimCommon.DesiredTimeScaleIndexNotPaused = SimCommon.TimeScaleIndexes.Length - 1;
                        if ( Time.timeScale > 0 )
                            Time.timeScale = SimCommon.TimeScaleIndexes[SimCommon.DesiredTimeScaleIndexNotPaused];
                    }
                    break;
                case "IncreaseVisualSpeed":
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        SimCommon.DesiredTimeScaleIndexNotPaused--;
                        if ( SimCommon.DesiredTimeScaleIndexNotPaused < 0 )
                            SimCommon.DesiredTimeScaleIndexNotPaused = 0;
                        if ( Time.timeScale > 0 )
                            Time.timeScale = SimCommon.TimeScaleIndexes[SimCommon.DesiredTimeScaleIndexNotPaused];
                    }
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
        #endregion

        #region iParticle
        public class iParticle : WorldSpriteControllerAbstractBase
        {
            private IA5Sprite lastIcon = null;

            #region SetIconDataAsNeeded
            public void SetIconDataAsNeeded( IA5Sprite Icon, Color color, float Scale, float HDRIntensity )
            {
                if ( this.lastIcon != Icon )
                {
                    this.lastIcon = Icon;
                    this.Rend.sprite = Icon.GetSpriteForUI();
                }

                this.SetColorIfNeeded( color );
                this.SetScaleIfNeeded( Scale );
                this.SetHDRIntensityIfNeeded( HDRIntensity );
            }
            #endregion

        }
        #endregion

        #region iHacker
        public class iHacker : MeshControllerAbstractBase
        {
        }
        #endregion

        #region iIndicatorBlood
        public class iIndicatorBlood : MeshControllerAbstractBase
        {
        }
        #endregion

        #region iIndicatorTargetingB
        public class iIndicatorTargetingB : MeshControllerAbstractBase
        {
            public static iIndicatorTargetingB Original;
            public iIndicatorTargetingB() { if ( Original == null ) Original = this; }
        }
        #endregion

        #region iIndicatorWater
        public class iIndicatorWater : MeshControllerAbstractBase
        {
        }
        #endregion

        #region iIndicatorBorder
        public class iIndicatorBorder : MeshControllerAbstractBase
        {
            public static iIndicatorBorder Instance;

            public bool WantsToDraw = false;

            public const float TILE_SIZE = 1.1f;

            #region DrawBetween
            public void DrawBetween( Vector3 Point1, Vector3 Point2, bool IsHorizontal )
            {
                if ( IsHorizontal  )
                {
                    float minX = MathA.Min( Point1.x, Point2.x );
                    float maxX = MathA.Max( Point1.x, Point2.x );   
                    float centerX = minX + ( maxX - minX ) / 2;

                    this.Trans.localPosition = new Vector3( centerX, Point1.y, Point1.z );
                    float xScale = ((maxX - minX) + TILE_SIZE) / TILE_SIZE;
                    this.Trans.localScale = new Vector3( xScale, 1f, 1f );
                }   
                else
                {
                    float minZ = MathA.Min( Point1.z, Point2.z );
                    float maxZ = MathA.Max( Point1.z, Point2.z );
                    float centerZ = minZ + (maxZ - minZ) / 2;

                    this.Trans.localPosition = new Vector3( Point1.x, Point1.y, centerZ );
                    float zScale = ((maxZ - minZ) + TILE_SIZE) / TILE_SIZE;
                    this.Trans.localScale = new Vector3( 1f, 1f, zScale );
                }

                this.WantsToDraw = true;
            }
            #endregion

            private bool wasRendOn = true;
            public void UpdateToMatchDrawDesires()
            {
                if ( this.WantsToDraw != this.wasRendOn )
                {
                    this.wasRendOn = this.WantsToDraw;
                    this.Rend.enabled = this.WantsToDraw;
                }
            }
        }
        #endregion

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

        #region bHackingAction
        public class bHackingAction : ButtonAbstractBaseWithImage
        {
            public static bHackingAction Original;
            public bHackingAction() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override void Clear()
            {
                this.UIDataController = null;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.UIDataController == null;
            }
        }
        #endregion
    }
}
