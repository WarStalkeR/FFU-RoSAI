using System;



using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    /// <summary>
    /// This is how the main game and/or the UI ask questions of the community sim
    /// </summary>
    public class VisQueriesImplementation : VisQueries
    {
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public override void InitializePoolsIfNeeded( ref long poolCount, ref long poolItemCount )
        {
        }

        public VisQueriesImplementation()
        {
            VisQueries.Instance = this;
        }

        public override bool GetHasInitialCameraPositionBeenSet()
        {
            float lastSetTime = VisCentralData.LastInitialCameraPositionBeenSet;
            if ( lastSetTime <= 0 )
                return false;
            if ( ArcenTime.AnyTimeSinceStartF - lastSetTime > 1.4f ) //many items happen on a 1 second interval, so let's make sure we get past that threshold.
                return true; //it must wait 1.4 seconds before telling us it is ready, because other things need to be set up, such as the actual list of simulation tiles need to be recalculated.
            return false;
        }

        public override void ShowInformationAboutUIXExaminedDataItem<T>( T item )
        {
            VisCommands.ShowInformationAboutUIXExaminedDataItem( item );
        }

        public override void AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString Text, TooltipID ToolID, TooltipWidth Width, int MaxLinesToAdd )
        {
            Window_SingleFloatingNote.bPanel.Instance.AddToExistingFloatingTextAtCurrentMousePosition( Text, ToolID, Width, MaxLinesToAdd, MathRefs.ScreenSpaceTextTimeToLast.FloatMin );
        }

        public override void AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString Text, TooltipID ToolID, TooltipWidth Width, int MaxLinesToAdd, float OverridingTime )
        {
            Window_SingleFloatingNote.bPanel.Instance.AddToExistingFloatingTextAtCurrentMousePosition( Text, ToolID, Width, MaxLinesToAdd, OverridingTime );
        }

        public override void AddToExistingFloatingTextAtCurrentMousePosition( ArcenDoubleCharacterBuffer Buffer, TooltipID ToolID, TooltipWidth Width, int MaxLinesToAdd )
        {
            Window_SingleFloatingNote.bPanel.Instance.AddToExistingFloatingTextAtCurrentMousePosition( Buffer, ToolID, Width, MaxLinesToAdd, MathRefs.ScreenSpaceTextTimeToLast.FloatMin );
        }

        public override void AddToExistingFloatingTextAtCurrentMousePosition( ArcenDoubleCharacterBuffer Buffer, TooltipID ToolID, TooltipWidth Width, int MaxLinesToAdd, float OverridingTime )
        {
            Window_SingleFloatingNote.bPanel.Instance.AddToExistingFloatingTextAtCurrentMousePosition( Buffer, ToolID, Width, MaxLinesToAdd, OverridingTime );
        }
    }
}
