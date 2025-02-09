using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using Arcen.Universal;
using DiffLib;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Arcen.HotM.ExternalVis
{
    public class Window_StructureChoiceList : ToggleableWindowController, IInputActionHandler
    {
        public static MachineStructure RelatedStructure = null;
        public static StructureChoiceMode Mode = StructureChoiceMode.AltTriggerList;

        #region HandleOpenCloseToggle
        public static void HandleOpenCloseToggle( StructureChoiceMode NewMode, MachineStructure Structure )
        {
            if ( Instance.IsOpen && RelatedStructure == Structure && Mode == NewMode )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                //ArcenDebugging.LogSingleLine( "close from match", Verbosity.DoNotShow );
            }
            else
            {
                Window_ActorStanceChange.Instance.Close( WindowCloseReason.OtherWindowCausingClose );
                Window_ScrapUnitList.Instance.Close( WindowCloseReason.OtherWindowCausingClose );

                RelatedStructure = Structure;
                Mode = NewMode;
                Instance.Open();
                //ArcenDebugging.LogSingleLine( "open", Verbosity.DoNotShow );
            }
        }
        #endregion

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_HotM.IsBigBannerBeingShown )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    return false;
            }

            if ( RelatedStructure != Engine_HotM.SelectedActor )
            {
                this.IsOpen = false;
                RelatedStructure = null;
                return false;
            }

            return base.GetShouldDrawThisFrame_Subclass();
        }

        public static Window_StructureChoiceList Instance;
        public Window_StructureChoiceList()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
		}

        public override void OnClose( WindowCloseReason CloseReason )
        {
            RelatedStructure = null;
            base.OnClose( CloseReason );
        }

        private static float runningY = 0;

        #region RenderStructureChoice
        public static bool RenderStructureChoice( IA5Sprite Sprite, GetOrSetUIData UIData )
        {
            bIconRow row = customParent.bIconRowPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
            if ( row == null )
                return false; //this was just time-slicing, so ignore that failure for now

            float x = 2.769974f;
            customParent.bIconRowPool.ApplySingleItemInRow( row, x, runningY );
            runningY -= 20.73f;
            row.SetRelatedImage0SpriteIfNeeded( Sprite.GetSpriteForUI() );

            row.Assign( UIData );
            return true;
        }
        #endregion RenderStructureChoice

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_StructureChoiceList.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            internal static ButtonAbstractBase.ButtonPool<bIconRow> bIconRowPool;

            private int heightToShow = 0;
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = 1.1f * GameSettings.Current.GetFloat( "Scale_StatsSidebar" );

                float offset = 0;
                if ( Window_BuildSidebar.Instance.GetShouldDrawThisFrame() )
                    offset = 208;

                this.WindowController.ExtraOffsetY = -(Window_ActorSidebarStatsLowerLeft.Instance.GetCurrentHeight_Scaled()) + offset;

                if ( Window_StructureChoiceList.Instance != null )
                {
                    #region Global Init
                    if ( bIconRow.Original != null && !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0; //make as responsive as possible
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;
                        hasGlobalInitialized = true;
                        bIconRowPool = new ButtonAbstractBase.ButtonPool<bIconRow>( bIconRow.Original, 10, "bIconRow" );
                    }
                    #endregion

                    bIconRowPool.Clear( 60 );

                    this.OnUpdate_Content();
                }

                #region Expand or Shrink Size Of This Window
                int newHeight = 25 + MathA.Min( lastYHeightOfInterior, 400 );
                if ( heightToShow != newHeight )
                {
                    heightToShow = newHeight;
                    this.Element.RelevantRect.anchorMin = new Vector2( 0.5f, 0f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0.5f, 0f );
                    this.Element.RelevantRect.pivot = new Vector2( 0.5f, 0f );
                    this.Element.RelevantRect.UI_SetHeight( heightToShow );
                }
                #endregion

                if ( RelatedStructure != Engine_HotM.SelectedActor )
                    Instance.Close( WindowCloseReason.ShowingRefused );
            }

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                runningY = -3.069992f; //starting

                MachineStructure structure = RelatedStructure;
                if ( structure == null )
                    return;
                MachineJob job = structure.CurrentJob;
                if ( job == null )
                    return;

                switch ( Mode )
                {
                    case StructureChoiceMode.SpecialtyList:
                        job.Implementation.HandleJobSpecialty( structure, job, null, JobSpecialtyLogic.HandleSpecialtyListPopulation, out _, out _ );
                        break;
                    case StructureChoiceMode.TriggerList:
                        job.Implementation.HandleJobActivationLogic( structure, job, null, JobActivationLogic.HandleActivationListPopulation, out _, out _ );
                        break;
                    case StructureChoiceMode.AltTriggerList:
                        job.Implementation.HandleJobActivationLogic( structure, job, null, JobActivationLogic.HandleActivationAltListPopulation, out _, out _ );
                        break;
                }

                if ( bMainContentParent.ParentRT )
                    bMainContentParent.ParentRT.UI_SetHeight( MathA.Abs( runningY ) );
                lastYHeightOfInterior = Mathf.CeilToInt( MathA.Abs( runningY ) );
            }
            #endregion
        }

        private static int lastYHeightOfInterior = 0;

		/// <summary>
		/// Top header
		/// </summary>
		public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                MachineStructure structure = RelatedStructure;
                if ( structure == null )
                    return;
                MachineJob job = structure.CurrentJob;
                if ( job == null )
                    return;

                switch ( Mode )
                {
                    case StructureChoiceMode.SpecialtyList:
                        if ( job.SpecialtyHeaderLangKey.IsEmpty() )
                            Buffer.AddNeverTranslated( "Error No specialty_header_lang_key", true );
                        else
                            Buffer.AddLang( job.SpecialtyHeaderLangKey );
                        break;
                    case StructureChoiceMode.TriggerList:
                        if ( job.ActivationListHeaderLangKey.IsEmpty() )
                            Buffer.AddNeverTranslated( "Error No activation_list_header_lang_key", true );
                        else
                            Buffer.AddLang( job.ActivationListHeaderLangKey );
                        break;
                    case StructureChoiceMode.AltTriggerList:
                        if ( job.ActivationAltListHeaderLangKey.IsEmpty() )
                            Buffer.AddNeverTranslated( "Error No activation_alt_list_header_lang_key", true );
                        else
                            Buffer.AddLang( job.ActivationAltListHeaderLangKey );
                        break;
                }                
            }
            public override void OnUpdate() { }
        }

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

		public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            //think about it, we'll see
        }

        #region bIconRow
        public class bIconRow : ButtonAbstractBaseWithImage
        {
            public static bIconRow Original;
            public bIconRow() { if ( Original == null ) Original = this; }

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

        #region bExit
        public class bExit : ButtonAbstractBaseWithImage
        {
            public static bExit Original;
            public bExit() { if ( Original == null ) Original = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion
    }

    public enum StructureChoiceMode
    {
        SpecialtyList,
        TriggerList,
        AltTriggerList
    }
}
