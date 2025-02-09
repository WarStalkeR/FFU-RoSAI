using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class Window_MajorEventWindow : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_MajorEventWindow Instance;
		
		public Window_MajorEventWindow()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.PreventsNormalInputHandlers = true;
        }

        public override void Close( WindowCloseReason Reason )
        {

        }

        #region ExtraData And AltChoices
        private static ArcenDoubleCharacterBuffer ExtraBodyTextBuffer = new ArcenDoubleCharacterBuffer( "Window_MajorEventWindow-ExtraBodyTextBuffer" );
        private static string ExtraBodyText = string.Empty;
        private static NPCEvent lastEventExtrasWereFor = null;

        private static void ClearAllExtraData()
        {
            ExtraBodyTextBuffer.GetStringAndResetForNextUpdate();
            ExtraBodyText = string.Empty;
        }

        private static readonly List<ISimpleChoice> altChoices = List<ISimpleChoice>.Create_WillNeverBeGCed( 6, "Window_MajorEventWindow-altChoices" );
        private static IList<ISimpleChoice> GetAltChoices()
        {
            altChoices.Clear();
            if ( SimCommon.ShouldShowCurrentEventResults )
            {
                altChoices.Add( SimpleTextOnlyChoiceOption.Create( Choice_DoClose,
                    LocalizedString.AddLang_New( "Popup_Common_Close" ),
                    LocalizedString.AddRaw_New( string.Empty ), true, true, true ) );
            }
            return altChoices;
        }

        private static void Choice_DoClose( ChoiceClickStyle ClickStyle, out PostChoiceResult PostResult )
        {
            PostResult = PostChoiceResult.CloseWindow;
        }
        #endregion ExtraData And AltChoices

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !VisCurrent.IsShowingActualEvent )
                return false; //only show this when in a city event
            if ( SimCommon.CurrentEvent?.MajorData == null ) 
                return false; //don't show this if there is no major event
            if ( VisCurrent.IsShowingTransitionCamera )
                return false; //if this is still transitioning too much, then don't show this window yet
            if ( !SimCommon.ShouldShowCurrentEventResults )
            {
                if ( VisCurrent.IsShowingBlockingEventVisEffect )
                    return false; //this is being blocked by an event vis effect
            }
            if ( SimCommon.CurrentEventActor == null || SimCommon.CurrentEventActor.IsFullDead )
            {
                ClearAllExtraData();
                SimCommon.CurrentEvent = null;
                SimCommon.NextScheduledEvent = null;
                SimCommon.ShouldShowCurrentEventResults = false;
                lastEventExtrasWereFor = null;
                return false;
            }
            if ( SimCommon.CurrentTimeline?.IsTimelineAFailure??false )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;

            return true;
        }

        public override void ChildOnShowAfterNotShowing()
        {
            if ( !SimCommon.CurrentEvent.DuringGameplay_HasPlayedSFXOnAppear )
            {
                SimCommon.CurrentEvent.DuringGameplay_HasPlayedSFXOnAppear = true;
                if ( SimCommon.CurrentEvent.SFXToPlayOnAppear != null )
                    SimCommon.CurrentEvent.SFXToPlayOnAppear.TryToPlayRandomAtCamera( 1f );

                if ( SimCommon.CurrentEvent.SFXToPlayOnAppear != null )
                    SimCommon.CurrentEvent.SFXToPlayOnAppear.TryToPlayRandomAtCamera( 1f );
            }
        }

        #region tTitleText
        public class tTitleText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( SimCommon.CurrentEvent?.GetDisplayName() ?? string.Empty,
                    SimCommon.CurrentEvent?.MajorData?.EventTitleTextColorHex ?? string.Empty );
            }
            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
            }
        }
        #endregion

        #region tLocationText
        public class tLocationText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                ISimMachineUnit unit = SimCommon.CurrentEventActor as ISimMachineUnit;
                if ( unit == null )
                    return;
                ISimUnitLocation containerLocation = unit.ContainerLocation.Get();
                if ( containerLocation == null )
                {
                    MapCell cell = CityMap.TryGetWorldCellAtCoordinates( unit.GroundLocation );
                    if ( cell == null ) return;
                    MapDistrict district = cell.ParentTile?.District;
                    if ( district == null ) return;

                    Buffer.AddRaw( district.DistrictName ?? string.Empty,
                        SimCommon.CurrentEvent?.MajorData?.EventLocationTextColorHex ?? string.Empty );
                }
                else
                    Buffer.AddRaw( containerLocation.GetLocationNameForNPCEvents()??string.Empty,
                        SimCommon.CurrentEvent?.MajorData?.EventLocationTextColorHex??string.Empty );
            }
            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
            }
        }
        #endregion

        #region tBodyText
        public class tBodyText : TextAbstractBase
        {
            public static ArcenUIWrapperedTMProText WrapperedText;
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( WrapperedText == null )
                    WrapperedText = Tex.Text;
                WrapperedText.CalculateHeightFromContentsButDoNotUpdate = true;

                NPCEvent cEvent = SimCommon.CurrentEvent;
                if ( cEvent != null )
                {
                    if ( ExtraBodyText != null && ExtraBodyText.Length > 0 ) //use this instead of the normal body text, not in addition to, for now
                    {
                        Buffer.LineIfLastWrittenWasNotLine();
                        Buffer.AddRaw( ExtraBodyText, ColorTheme.NarrativeColor );
                    }
                    else
                    {
                        Buffer.AddRaw( cEvent.GetDescription(), ColorTheme.NarrativeColor );
                        if ( cEvent.StrategyTip.Text.Length > 0 )
                        {
                            Buffer.Line().StartSize80();
                            Buffer.AddRaw( cEvent.StrategyTip.Text, ColorTheme.PurpleDim );
                        }
                    }
                }
            }
        }
        #endregion

        #region iHeader
        public class iHeader : ImageAbstractBase
        {
            public static iHeader Instance;
            public iHeader() { Instance = this; }

            private int lastNonSimRowIndex = -1;
            private int lastReloadCount = -1;
            private Sprite[] spriteArray;

            public override void UpdateImagesFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages )
            {
                if ( spriteArray == null && this.Element )
                    spriteArray = this.Element.RelatedSprites;

                if ( spriteArray == null )
                    return;
                MajorEventData majorData = SimCommon.CurrentEvent?.MajorData;
                if ( majorData == null )
                    return;

                if ( majorData.NonSimRowIndex != lastNonSimRowIndex ||
                    lastReloadCount != Engine_HotM.NumberOfSelectDataReloads )
                {
                    lastNonSimRowIndex = majorData.NonSimRowIndex;
                    lastReloadCount = Engine_HotM.NumberOfSelectDataReloads;

                    if ( majorData.EventBackgroundIndex >= 0 && majorData.EventBackgroundIndex < spriteArray.Length )
                        Image.UpdateWith( spriteArray[majorData.EventBackgroundIndex] );

                    Image refImage = Image.GetReferenceImage();
                    if ( refImage )
                    {
                        //note: this should not be required, this is overkill.  But otherwise they don't update once set a single time.
                        Material oldMat = refImage.material;
                        Material newMat = new Material( oldMat );
                        Material.Destroy( oldMat );

                        newMat.SetFloat( "_HsvShift", majorData.EventBackgroundHueShift );
                        newMat.SetFloat( "_HsvSaturation", majorData.EventBackgroundSaturation );
                        newMat.SetFloat( "_HsvBright", majorData.EventBackgroundBrightness );

                        refImage.material = newMat;
                    }
                }
            }
        }
        #endregion

        #region iHeaderIcon
        public class iHeaderIcon : ImageAbstractBase
        {
            public static iHeaderIcon Instance;
            public iHeaderIcon() { Instance = this; }

            private Material imageMaterial = null;

            public override void UpdateImagesFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages )
            {
                MajorEventData mEvent = SimCommon.CurrentEvent?.MajorData;
                if ( mEvent == null ) 
                    return;

                if ( imageMaterial == null )
                {
                    imageMaterial = Image.GetReferenceImage().material;
                    Image.SetColor( Color.black ); //this makes it so that the glow is the only contributor
                }
                imageMaterial.SetColor( "_GlowColor", mEvent.EventGlowColor );
                imageMaterial.SetFloat( "_Glow", mEvent.EventGlowIntensity );
                Image.UpdateWith( mEvent.ParentEvent.Icon.GetSpriteForUI() );
            }
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bChoice> bChoicePool;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = GameSettings.Current.GetFloat( "Scale_CentralChoicePopup" );

                float offsetFromSide = 0;
                if ( !Engine_Universal.PrimaryIsLeft ) //the sidebar is on, move left
                    offsetFromSide -= Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();
                else //the sidebar is on, and is on the left, move right
                    offsetFromSide += Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();

                this.WindowController.ExtraOffsetX = offsetFromSide;

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bChoice.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bChoicePool = new ButtonAbstractBase.ButtonPool<bChoice>( bChoice.Original, 10, "bChoice" );

                        this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                        this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;
                    }
                }
                #endregion

                this.UpdateButtons();
            }

            private const float HEIGHT_FROM_OTHER_BITS = 112.9f + -3f; //header plus buffer
            private const float BUTTON_WIDTH = 442f;
            private const float BUTTON_HEIGHT = 25.7f;
            private const float HEIGHT_PER_BUTTON = BUTTON_HEIGHT + 3f;
            private const float BUTTON_STARTING_Y = -3.47f;
            private const float BUTTON_X = -2f;            

            public void UpdateButtons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bChoicePool.Clear( 5 );

                int buttonCount = 0;

                NPCEvent cEvent = SimCommon.CurrentEvent;
                if ( cEvent != lastEventExtrasWereFor )
                    ClearAllExtraData();

                if ( cEvent != null )
                {
                    float buttonY = BUTTON_STARTING_Y;
                    float buttonX = BUTTON_X;

                    if ( SimCommon.ShouldShowCurrentEventResults )
                    {
                        IList<ISimpleChoice> altChoices = GetAltChoices();
                        for ( int i = altChoices.Count - 1; i >= 0; i-- ) //iterate in reverse order, as we are building from the bottom up
                        {
                            ISimpleChoice choice = altChoices[i];
                            HandleChoice( ref buttonX, ref buttonY, ref buttonCount, i, choice );
                        }
                    }
                    else
                    {
                        for ( int i = cEvent.ChoicesOrdered.Count - 1; i >= 0; i-- ) //iterate in reverse order, as we are building from the bottom up
                        {
                            EventChoice choice = cEvent.ChoicesOrdered[i];
                            if ( !choice.DuringGameplay_IsChoiceHidden() )
                                HandleChoice( ref buttonX, ref buttonY, ref buttonCount, i, choice );
                        }
                    }
                }

                #region Expand or Shrink Height Of This Window
                float heightForWindow = HEIGHT_FROM_OTHER_BITS + (HEIGHT_PER_BUTTON * buttonCount) + (tBodyText.WrapperedText?.LastHeightSet??0);
                if ( lastWindowHeight != heightForWindow )
                {
                    lastWindowHeight = heightForWindow;
                    this.Element.RelevantRect.anchorMin = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.pivot = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.UI_SetHeight( heightForWindow );
                }
                #endregion
            }
            private float lastWindowHeight = -1f;

            private void HandleChoice( ref float buttonX, ref float buttonY, ref int buttonCount, int choiceIndex, ISimpleChoice choice )
            {
                bChoice icon = bChoicePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.ApplyItemInPositionNoTextSizing( ref buttonX, ref buttonY, false, false, BUTTON_WIDTH, BUTTON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isBeingHoveredNow = false;
                                    if ( element is ArcenUI_Button but )
                                    {
                                        if ( but.LastHadMouseWithin )
                                            isBeingHoveredNow = true;
                                    }

                                    bool isPurple = choice.GetIsPurple();
                                    choice.RenderButtonText( ExtraData.Buffer, choice.GetIsPurple() ? ColorTheme.GetBasicLightTextPurple( isBeingHoveredNow ) :
                                        ColorTheme.GetBasicLightTextBlue( isBeingHoveredNow ) );
                                    icon.SetRelatedImage0SpriteIfNeeded( icon.Element.RelatedSprites[isPurple ? 0 : 1] );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                if ( !choice.RenderTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard ) )
                                    TooltipRefs.AtMouseTag.ClearAllImmediately();
                                break;
                            case UIAction.OnClick:
                                {
                                    ExtraBodyTextBuffer.EnsureResetForNextUpdate();
                                    choice.TryClick( ChoiceClickStyle.InitialAttempt, out PostChoiceResult PostResult, ExtraBodyTextBuffer );
                                    switch ( PostResult )
                                    {
                                        case PostChoiceResult.WasBlocked:
                                            ClearAllExtraData();
                                            break; //nothing to do
                                        case PostChoiceResult.CloseWindow:
                                            ClearAllExtraData();
                                            Instance.Close( WindowCloseReason.UserDirectRequest );
                                            ISimMachineActor currentActor = SimCommon.CurrentEventActor;
                                            SimCommon.CurrentEvent = null;
                                            SimCommon.CurrentEventActor = null;
                                            SimCommon.ShouldShowCurrentEventResults = false;
                                            SimCommon.NextScheduledEvent?.StartThisEvent( currentActor, false );
                                            lastEventExtrasWereFor = null;
                                            break;
                                        case PostChoiceResult.ShowAfterChoiceReport:
                                            SimCommon.ShouldShowCurrentEventResults = true;
                                            ExtraBodyText = ExtraBodyTextBuffer.GetStringAndResetForNextUpdate();
                                            lastEventExtrasWereFor = SimCommon.CurrentEvent;
                                            break;
                                    }
                                }
                                break;
                        }
                    } );

                    buttonY += HEIGHT_PER_BUTTON;
                    buttonCount++;
                }
            }
        }

        #region bChoice
        public class bChoice : ButtonAbstractBaseWithImage
        {
            public static bChoice Original;
            public bChoice() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
                if ( this.Element is ArcenUI_Button but )
                    but.ClickSoundEffect = string.Empty; //make sure that we don't get the regular sound effects, as we want to play custom ones

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

        public void Handle( int Int1, InputActionTypeData InputActionType )
        {
            //switch ( InputActionType.ID )
            //{
            //    case "Cancel":
            //        if ( choices != null && choices.Count > 0 )
            //        {
            //            foreach ( ISimpleChoice choice in choices )
            //            {
            //                if ( choice.GetShouldBeTriggeredByEscape() || choices.Count == 1 )
            //                {
            //                    choice.TryClick( ChoiceClickStyle.InitialAttempt, out _, null );
            //                    break;
            //                }
            //            }
            //        }
            //        break;
            //    case "JumpToNextActorOrEndTurn":
            //        if ( choices != null && choices.Count > 0 )
            //        {
            //            foreach ( ISimpleChoice choice in choices )
            //            {
            //                if ( choice.GetShouldBeTriggeredBySpacebar() || choices.Count == 1 )
            //                {
            //                    choice.TryClick( ChoiceClickStyle.InitialAttempt, out _, null );
            //                    break;
            //                }
            //            }
            //        }
            //        break;
            //}

            if ( InputWindowCutthrough.HandleKey( InputActionType.ID ) )
                return;
        }
    }
}
