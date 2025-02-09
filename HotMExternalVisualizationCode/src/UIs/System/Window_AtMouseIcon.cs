using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_AtMouseIcon : WindowControllerAbstractBase
    {
        public Window_AtMouseIcon()
        {
            this.IsAtMouseTooltip = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !Engine_Universal.HasFocus || Engine_Universal.IsMouseOutsideGameWindow || InputCaching.IsMousePositionIgnored )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            return true;
        }

        public void ClearMyself()
        {
            AtMouseIcon.Instance?.ClearMyself();
        }

        public class AtMouseIcon : ImageAbstractBase
        {
            public static AtMouseIcon Instance;
            public AtMouseIcon() { Instance = this; }

            private bool hasSetCanvasOffset = false;
            private float TimeLastSet;

            private Material imageMaterial = null;

            public void ClearMyself()
            {
                this.lastIcon = null;
                this.lastColor = Color.white;
            }

            public override void OnMainThreadUpdate()
            {
                this.UpdatePositionAndSize();
            }

            public override void UpdateImagesFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages )
            {
                if ( imageMaterial == null )
                    imageMaterial = Image.GetReferenceImage().material;
                imageMaterial.SetColor( "_Color", this.lastColor );
                Image.SetColor( Color.white );
                Image.UpdateWith( this.lastIcon?.GetSpriteForUI() );
            }

            public void UpdatePositionAndSize()
            {
                if ( !this.hasSetCanvasOffset )
                {
                    if ( this.Element != null && this.Element.Window != null )
                    {
                        this.hasSetCanvasOffset = true;
                        this.Element.Window.DisableAllRaycasters(); //this prevents us from intercepting ourselves when hovering something
                        this.Element.Window.SetOverridingCanvasSortingOrder( 32764 ); //almost as high as it will go, so this is always on top except for tooltips
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;
                    }
                }

                float screenXPixel = ArcenInput.MouseScreenX + 10;// - 20;
                float screenYPixel = ArcenInput.MouseScreenY - 20;// + 10;

                this.Element.Window.IsAutomaticPositioningDisabled = true;

                Vector3 worldSpacePoint = ArcenUI.Instance.guiCamera.ScreenToWorldPoint( new Vector3( screenXPixel, screenYPixel, ArcenUI.POSITION_Z ) );

                Vector2 sizeDelta = ((RectTransform)this.Element.ReferenceImage.transform.parent).GetWorldSpaceSize();
                float width = sizeDelta.x;
                float height = sizeDelta.y;

                float maxXPixel = ArcenUI.Instance.world_BottomRight.x - width;
                float maxYPixel = ArcenUI.Instance.world_BottomRight.y + height;

                worldSpacePoint.x = MathA.Min( worldSpacePoint.x, maxXPixel );
                worldSpacePoint.y = MathA.Max( worldSpacePoint.y, maxYPixel );

                this.Element.Window.SetPositionIfNeeded( worldSpacePoint );

            }

            public override bool GetShouldBeHidden()
            {
                if ( ArcenTime.AnyTimeSinceStartF - this.TimeLastSet > ArcenUI.TimespanAfterWhichTooltipsDisappear )
                    return true;
                return false;
            }

            private IA5Sprite lastIcon = null;
            private Color lastColor = Color.white;
            public void SetIconAndColor( IA5Sprite Icon, Color color )
            {
                if ( Icon == null )
                    return;

                this.TimeLastSet = ArcenTime.AnyTimeSinceStartF;
                if ( Icon == this.lastIcon && this.lastColor == color )
                    return;
                this.lastIcon = Icon;
                this.lastColor = color;
            }

            public override void OnUpdate()
            {
                this.DoResizeIfNeeded();
                base.OnUpdate();
            }

            public void DoResizeIfNeeded()
            {                
                this.UpdatePositionAndSize();
            }
        }
    }
}
