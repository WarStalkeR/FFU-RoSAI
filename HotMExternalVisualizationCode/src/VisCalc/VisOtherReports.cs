using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using DiffLib;
using System.Text;
using System.Runtime.CompilerServices;

namespace Arcen.HotM.ExternalVis
{    
    public static class VisOtherReports
    {
        #region CurrentReport
        private static float lastSet = 0;
        private static string currentReport = string.Empty;

        private static string CurrentReport
        {
            get 
            {
                if ( ArcenTime.AnyTimeSinceStartF - lastSet > 0.5f )
                    return string.Empty;
                return currentReport;
            }
        }
        #endregion

        private static ArcenDoubleCharacterBuffer ExtraReportBuffer = new ArcenDoubleCharacterBuffer( "VisOtherReports-ExtraReportBuffer" );
        private static ArcenDoubleCharacterBuffer GetAndResetReportBuffer()
        {
            ExtraReportBuffer.ResetForNextUpdate();
            return ExtraReportBuffer;
        }

        public static void GetIfShouldDoReport( string ReportID, out bool DoReport, out ArcenDoubleCharacterBuffer DebugBuffer )
        {
            DoReport = CurrentReport == "StreetSense_KeyLocationEventReport";
            DebugBuffer = DoReport ? VisOtherReports.GetAndResetReportBuffer() : null;
        }

        #region ShowOtherReport
        public static void ShowOtherReport( string ReportID )
        {
            Engine_Universal.NonModalPopupsSideSecondary.Clear(); //keep these from stacking up on repeat clicks
            ModalPopupData.CreateAndLogSelfUpdatingOKStyle( PopupSizeStyle.TallSidePrimary, null, LocalizedString.AddLang_New( "OtherReports" ), LocalizedString.AddLang_New( "Popup_Common_Close" ), null,
                delegate ( ArcenDoubleCharacterBuffer Buffer, string[] TooltipLinkData )
                {
                    if ( TooltipLinkData != null && TooltipLinkData.Length > 0 )
                        return;
                    else
                        WriteAReport( ReportID, Buffer );
                }, 0.1f, delegate( MouseHandlingInput handlingInput, string[] TooltipLinkData )
                {
                    ReportMouseHandling( handlingInput, TooltipLinkData );
                    return MouseHandlingResult.None;
                }
                );
        }
        #endregion

        #region WriteAReport
        public static void WriteAReport( string ReportID, ArcenDoubleCharacterBuffer Buffer )
        {
            lastSet = ArcenTime.AnyTimeSinceStartF;
            currentReport = ReportID;

            int debugStage = 0;

            try
            {
                switch ( ReportID )
                {
                    case "MinorEventList":
                        WriteMinorEventList( Buffer );
                        break;
                    case "StreetSense_KeyLocationEventReport":
                        WriteTransferredExtraReportBuffer( Buffer );
                        break;
                    case "StreetSense_MinorEventLocationEventReport":
                        WriteTransferredExtraReportBuffer( Buffer );
                        break;
                    default:
                        Buffer.AddNeverTranslated( "Error! No Report with ID: '", true ) .AddRaw( ReportID ).AddNeverTranslated( "'", true );
                        break;
                }
                
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "WriteAReport Error", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        private static void ReportMouseHandling( MouseHandlingInput handlingInput, string[] TooltipLinkData )
        {
            //switch ( TooltipLinkData[0] )
            //{
            //    case "ToggleSubCells":
            //        RenderManager_Streets.SubCellRenderingForcedOff = !RenderManager_Streets.SubCellRenderingForcedOff;
            //        break;
            //}
        }

        #region WriteMinorEventList
        public static void WriteMinorEventList( ArcenDoubleCharacterBuffer Buffer )
        {
            ISimMachineActor selectedMachineActorOrNull = Engine_HotM.SelectedActor as ISimMachineActor;

            foreach ( NPCEvent minorEvent in EventAggregation.ValidMinorEvents.GetDisplayList() )
            {
                Buffer.Line();
                Buffer.AddRaw( minorEvent.ID ).HyphenSeparator();
                if ( selectedMachineActorOrNull == null )
                    Buffer.AddNeverTranslated( "No Actor", true, ColorTheme.Gray );
                else if ( !minorEvent.Event_CalculateMeetsPrerequisites( selectedMachineActorOrNull, GateByLogicCheckType.ActorSpecific, EventCheckReason.StandardSeeding, false ) )
                    Buffer.AddNeverTranslated( "Actor Fails", true, ColorTheme.RedOrange3 );
                else
                    Buffer.AddRaw( LangCommon.Popup_Common_Ok.Text, ColorTheme.HeaderLighterBlue );
            }
        }
        #endregion

        #region WriteTransferredExtraReportBuffer
        public static void WriteTransferredExtraReportBuffer( ArcenDoubleCharacterBuffer Buffer )
        {
            Buffer.AddNeverTranslated( ExtraReportBuffer.Debug_GetWrittenSoFarInefficient(), true );
        }
        #endregion
    }
}
