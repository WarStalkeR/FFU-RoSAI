using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using System.Diagnostics;
using DiffLib;

namespace Arcen.HotM.ExternalVis
{
    public enum Indicator
    {
        None,
        MapButton_StreetSense,
        MapButton_Investigate,
        BuildMenuButton,
        Toast,
        FirstOuterRadialButton,
        CommandModeButton,
        ForcesSidebar,
        MapRadialContemplation,
        PointAtCountdown,
        RadialStreetSense,
        RadialInvestigation,

        UITour1_LeftHeader,
        UITour2_RightHeader,
        UITour3_TaskStack,
        UITour4_RadialMenu,
        UITour5_Toast,
        UITour6_UnitSidebar,
        UITour7_AbilityBar,
        VirtualWorldButton,

        DebateTour1_Target,
        DebateTour2_Failures,
        DebateTour3_ActiveSlot,
        DebateTour4_Discard,
        DebateTour5_Queue,
        DebateTour6_Bonus,
        DebateTour7_Opponent,
        DebateTour8_FinalAdvice,

        EndOfTimeButton,
        EndOfTimeCreatorLens,
    }
}
