using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public static class Sidebar_DistrictSingleInfo
    {
        public static void Open( MapDistrict District )
        {
            if ( District == null )
                return;

            ModalPopupData.CreateAndLogSelfUpdatingOKStyle( PopupSizeStyle.Tall, null, LocalizedString.AddRaw_New( District.DistrictName ),
            LangCommon.Popup_Common_Close.LocalizedString, null, delegate ( ArcenDoubleCharacterBuffer Buffer, string[] TooltipLinkData )
            {
                if ( TooltipLinkData != null && TooltipLinkData.Length > 0 )
                    District.WriteDataItemUIXClickedDetails_SubTooltipLinkHover( TooltipLinkData );
                else
                {
                    District.WriteDataItemUIXClickedDetails( Buffer );
                }
            }, 1f,
            delegate ( MouseHandlingInput Input, string[] TooltipLinkData )
            {
                return District.WriteWorldExamineDetails_SubTooltipLinkClick( Input, TooltipLinkData );
            } );
        }
    }
}
