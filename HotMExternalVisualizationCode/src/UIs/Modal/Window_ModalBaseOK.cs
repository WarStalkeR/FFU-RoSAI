using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public abstract class Window_ModalBaseOK : ToggleableWindowController
    {
		public sealed override void Close( WindowCloseReason Reason )
        {
			base.Close( Reason );
			ClearPopupData();
		}

		protected virtual void ClearPopupData()
		{
			Engine_Universal.CurrentPopups.SafeTryRemoveAt(0);
		}

		public abstract class tBodyTextBase : TextAbstractBase
        {
            public virtual bool UsesModal => true;
            public virtual bool UsesNonModalLeft => false;

            public abstract float WidthForBodyText { get; }

            private static float lastUpdatedBodyText = -1000;
            private static readonly ArcenDoubleCharacterBuffer updaterBuffer = new ArcenDoubleCharacterBuffer( "Window_ModalBaseOK-tBodyTextBase" );
            private static BodyTextUpdater lastUpdater = null;
            private static string lastUpdaterText = string.Empty;

            public static UnityEngine.RectTransform bodyTextTransform = null;
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                ModalPopupData popupData = null;
                if ( UsesModal )
                    popupData = Engine_Universal.CurrentPopups.FirstOrDefault;
                else
                    popupData = Engine_Universal.GetFirstPopupDataFromSideOrNull( UsesNonModalLeft ? FromSide.Left : FromSide.Right, false );

                if ( popupData == null )
                {
                    Buffer.AddRaw( "Null data \n\n\n\n\n \n" );
                    return;
                }

                if ( popupData.Updater != null )
                {
                    if ( ArcenTime.AnyTimeSinceStartF - lastUpdatedBodyText > popupData.UpdateFrequency || lastUpdater != popupData.Updater )
                    {
                        //need to do an update
                        lastUpdatedBodyText = ArcenTime.AnyTimeSinceStartF;
                        lastUpdater = popupData.Updater;

                        try
                        {
                            lastUpdater( updaterBuffer, null );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogSingleLine( "Exception in lastUpdater, lastUpdater == null: " + (lastUpdater == null) + "\n" + e, Verbosity.ShowAsError );
                        }
                        lastUpdaterText = updaterBuffer.GetStringAndResetForNextUpdate();
                    }
                    
                    Buffer.AddRaw( lastUpdaterText );
                }
                else
                {
                    lastUpdater = null;
                    Buffer.AddRaw( popupData.BodyText, ColorTheme.NarrativeColor );
                }

                Buffer.AddRaw( "\n\n\n\n\n \n" ); //add extra spacing to prevent clipping
            }

            #region HandleHyperlinkHover
            private static readonly ArcenDoubleCharacterBuffer linkTooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_ModalBaseOK-tBodyTextBase-linkTooltipBuffer" );

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                if ( TooltipLinkData == null || TooltipLinkData.Length == 0 )
                    return;

                ModalPopupData popupData = null;
                if ( UsesModal )
                    popupData = Engine_Universal.CurrentPopups.FirstOrDefault;
                else
                    popupData = Engine_Universal.GetFirstPopupDataFromSideOrNull( UsesNonModalLeft ? FromSide.Left : FromSide.Right, false );
                if ( popupData == null )
                    return;

                if ( popupData.Updater != null )
                    popupData.Updater( linkTooltipBuffer, TooltipLinkData );
            }
            #endregion

            #region HandleHyperlinkClick
            public override MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string[] TooltipLinkData )
            {
                if ( TooltipLinkData == null || TooltipLinkData.Length == 0 )
                    return MouseHandlingResult.None;

                ModalPopupData popupData = null;
                if ( UsesModal )
                    popupData = Engine_Universal.CurrentPopups.FirstOrDefault;
                else
                    popupData = Engine_Universal.GetFirstPopupDataFromSideOrNull( UsesNonModalLeft ? FromSide.Left : FromSide.Right, false );

                if ( popupData == null )
                    return MouseHandlingResult.None;

                if ( popupData.LinkClickHandler != null )
                    return popupData.LinkClickHandler( Input, TooltipLinkData );
                else
                    linkTooltipBuffer.AddNeverTranslated( "No popupData.LinkClickHandler found!", true );

                return MouseHandlingResult.None;
            }
            #endregion

            public override void OnUpdate()
            {
                if ( bodyTextTransform == null )
                {
                    bodyTextTransform = this.Element.RelevantRect;
                    bodyTextTransform.UI_SetWidth( this.WidthForBodyText );
                }

                ArcenUI_Text textElement = this.Element as ArcenUI_Text;
                if ( textElement )
                    textElement.FontScale = InputCaching.Scale_PopupWindowText;
            }
        }
    }
}
