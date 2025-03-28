using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class CityTasks_Basics : ICityTaskImplementation
    {
        public string GetText( CityTask Task, CityTaskTextType TextType )
        {
            switch ( Task.ID )
            {
                case "Ch0_CameraPanning":
                    if ( TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format1( TextType, InputCaching.GetGetHumanReadableKeyComboForCancel() );
                    if ( Engine_Universal.IsSteamDeckActive )
                        return Task.HandleText_Lang_SteamDeck( TextType );// BasicControls_PanningRotateAndTilt_SteamDeck
                    else
                        return Task.HandleText_Format4( TextType, InputCaching.GetStringForTraditionalCameraPanning1(),
                            InputCaching.GetStringForTraditionalCameraPanning2(),
                            InputCaching.GetStringForTraditionalCameraPanning3(),
                            InputCaching.GetStringForTraditionalCameraPanning4() ); //BasicControls_Panning
                case "Ch0_CameraZoom":
                    if ( TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format1( TextType, InputCaching.GetGetHumanReadableKeyComboForCancel() );
                    if ( Engine_Universal.IsSteamDeckActive )
                        return Task.HandleText_Format1_SteamDeck( TextType, InputCaching.GetStringForCameraZoom() ); //BasicControls_Zooming_SteamDeck
                    else
                        return Task.HandleText_Format1( TextType, InputCaching.GetStringForCameraZoom() ); //BasicControls_Zooming_General
                case "Ch0_CameraRotation":
                    if ( TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format1( TextType, InputCaching.GetGetHumanReadableKeyComboForCancel() );
                    if ( Engine_Universal.IsSteamDeckActive )
                        return Task.HandleText_Lang_SteamDeck( TextType );
                    else
                        return Task.HandleText_Format2( TextType, InputCaching.GetStringForCameraRotation(), InputCaching.GetStringForCameraInclination() ); //BasicControls_RotateAndTilt_Normal_Tooltip_P2
                case "Ch0_UnitMovement":
                    if ( TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format1( TextType, InputCaching.GetGetHumanReadableKeyComboForCancel() );
                    return Task.HandleText_Format2( TextType, Lang.GetLeftClickText(), Lang.GetRightClickText() );
                case "Ch0_FindSafety":
                    if ( TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format2( TextType, Lang.GetRightClickText(), InputActionTypeDataTable.Instance.GetRowByID( "ToggleMapMode" ).GetHumanReadableKeyCombo() );
                    return Task.HandleText_Lang( TextType );
                case "Ch0_SpreadToMoreAndroids":
                    if ( TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format1( TextType, Lang.GetRightClickText() );
                    return Task.HandleText_Lang( TextType );
                case "Ch0_InvestigateViaMap":
                    if ( TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format1( TextType, InputActionTypeDataTable.Instance.GetRowByID( "ToggleMapMode" ).GetHumanReadableKeyCombo() );
                    return Task.HandleText_Lang( TextType );
                case "Ch0_ReadHandbookEntry":
                    if ( TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format1( TextType, InputActionTypeDataTable.Instance.GetRowByID( "Handbook" ).GetHumanReadableKeyCombo() );
                    return Task.HandleText_Lang( TextType );
                case "Ch0_BuildTower":
                    if ( TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format1( TextType, InputActionTypeDataTable.Instance.GetRowByID( "ToggleBuildMode" ).GetHumanReadableKeyCombo() );
                    return Task.HandleText_Lang( TextType );
                case "Ch1_Water_BuildWell":
                    if ( TextType == CityTaskTextType.Description )
                        return Task.HandleText_Format1( TextType, InputActionTypeDataTable.Instance.GetRowByID( "ToggleResearch" ).GetHumanReadableKeyCombo() );
                    return Task.HandleText_Lang( TextType );
                case "Ch1_Water_StructuralEngineering":
                    if ( TextType == CityTaskTextType.Description || TextType == CityTaskTextType.StrategyText )
                        return Task.HandleText_Format1( TextType, Lang.GetRightClickText() );
                    return Task.HandleText_Lang( TextType );
                case "Ch1_Water_NewStyleOfInvestigation":
                    {
                        if ( TextType == CityTaskTextType.Description )
                            return Task.HandleText_Format1( TextType, Lang.GetLeftClickText() );
                        if ( TextType == CityTaskTextType.StrategyText )
                            return Task.HandleText_Format1( TextType, InputActionTypeDataTable.Instance.GetRowByID( "ToggleMapMode" ).GetHumanReadableKeyCombo() );
                        return Task.HandleText_Lang( TextType );
                    }
                case "Ch1_Water_DeathAndKnowledge":
                case "Ch1_Water_Tungsten":
                case "Ch1_Water_UsingItems":
                case "Ch1_Water_DefeatingTheCruiser":
                case "Ch1_Water_BuildAFiltrationTower":
                case "Ch1_Food_Intro":
                case "HackTut_OverallGoal":
                case "HackTut_LeafNode":
                case "HackTut_RunRules":
                case "HackTut_MakeAMove":
                case "HackTut_DestroyADaemon":
                case "HackTut_FinishTheJob":
                case "HackTut_CorruptThePilotCradle":
                case "Ch0_RunTalkOrShoot":
                default:
                    return Task.HandleText_Lang( TextType );
            }
        }

        public void DoPerQuarterSecondIfActive( CityTask Task, MersenneTwister Rand )
        {
            switch ( Task.ID )
            {
                case "Ch0_CameraPanning":
                    if ( Task.DuringGameplay_HasDoneOnStartActions )
                    {
                        if ( SimCommon.HasDoneAnyCameraPanning )
                            Task.DuringGameplay_CompleteIfActive();
                    }
                    else
                    {
                        Task.DuringGameplay_HasDoneOnStartActions = true;
                        SimCommon.HasDoneAnyCameraPanning = false;
                    }
                    break;
                case "Ch0_CameraZoom":
                    if ( Task.DuringGameplay_HasDoneOnStartActions )
                    {
                        if ( SimCommon.HasDoneAnyCameraZoom )
                            Task.DuringGameplay_CompleteIfActive();
                    }
                    else
                    {
                        Task.DuringGameplay_HasDoneOnStartActions = true;
                        SimCommon.HasDoneAnyCameraZoom = false;
                    }
                    break;
                case "Ch0_CameraRotation":
                    if ( Task.DuringGameplay_HasDoneOnStartActions )
                    {
                        if ( SimCommon.HasDoneAnyCameraRotation )
                            Task.DuringGameplay_CompleteIfActive();
                    }
                    else
                    {
                        Task.DuringGameplay_HasDoneOnStartActions = true;
                        SimCommon.HasDoneAnyCameraRotation = false;
                    }
                    break;
                case "Ch0_HoverProject":
                    if ( Task.DuringGameplay_HasDoneOnStartActions )
                    {
                        if ( SimCommon.HasHoveredAnyProject )
                            Task.DuringGameplay_CompleteIfActive();
                    }
                    else
                    {
                        Task.DuringGameplay_HasDoneOnStartActions = true;
                        SimCommon.HasHoveredAnyProject = false;
                    }
                    break;
                case "Ch0_UnitMovement":
                    if ( Task.DuringGameplay_HasDoneOnStartActions )
                    {
                        if ( SimCommon.HasGivenAnyAndroidMovementOrders )
                        {
                            Task.DuringGameplay_CompleteIfActive();
                            FlagRefs.Ch0_RunTalkOrShoot.DuringGameplay_StartIfNeeded();
                        }
                    }
                    else
                    {
                        Task.DuringGameplay_HasDoneOnStartActions = true;
                        SimCommon.HasGivenAnyAndroidMovementOrders = false;
                    }
                    break;
                case "Ch0_RunTalkOrShoot":
                    if ( FlagRefs.Ch0_FindSafety.DuringGameplay_State != CityTaskState.NeverStarted )
                        Task.DuringGameplay_CompleteIfActive();
                    break;
                case "Ch0_FindSafety":
                    //nothing to do, actually
                    break;
                case "Ch0_SpreadToMoreAndroids":
                    if ( SimCommon.AllMachineActors.Count >= 4 )
                        Task.DuringGameplay_CompleteIfActive();
                    break;
                case "Ch0_InvestigateViaMap":
                    if ( Task.DuringGameplay_HasDoneOnStartActions )
                    {
                        if ( SimCommon.VisibleAndroidInvestigations.Count > 0 )
                        {
                            if ( SimCommon.VisibleAndroidInvestigations[0]?.InvestigationAttemptsSoFar > Task.NonSim_WorkingInt )
                            {
                                if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                                    Task.DuringGameplay_CompleteIfActive();
                                else
                                    Task.NonSim_WorkingInt = SimCommon.VisibleAndroidInvestigations[0]?.InvestigationAttemptsSoFar ?? 0;
                            }
                        }
                        else
                            Task.DuringGameplay_CompleteIfActive();
                    }
                    else
                    {
                        Task.DuringGameplay_HasDoneOnStartActions = true;
                        if ( SimCommon.VisibleAndroidInvestigations.Count > 0 )
                            Task.NonSim_WorkingInt = SimCommon.VisibleAndroidInvestigations[0]?.InvestigationAttemptsSoFar ?? 0;
                        else
                            Task.NonSim_WorkingInt = 0;
                    }
                    break;
                case "Ch1_Water_BuildWell":
                    if ( JobRefs.WellAndCistern.DuringGame_NumberFunctional.Display > 0 )
                    {
                        Task.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch1_Water_NewStyleOfInvestigation.DuringGameplay_StartIfNeeded();
                    }
                    else if ( JobRefs.WellAndCistern.DuringGame_NumberInstalling.Display > 0 )
                        FlagRefs.Ch1_Water_StructuralEngineering.DuringGameplay_StartIfNeeded();
                        break;
                case "Ch1_Water_StructuralEngineering":
                    if ( JobRefs.WellAndCistern.DuringGame_NumberFunctional.Display > 0 )
                    {
                        Task.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch1_Water_NewStyleOfInvestigation.DuringGameplay_StartIfNeeded();
                    }
                    break;
                case "Ch1_Water_NewStyleOfInvestigation":
                    if ( CommonRefs.InvestigateLocationActionOverTimeType_Regular.DuringGame_RelatedActors.Count > 0 || UnlockRefs.SublimatingShells.DuringGameplay_IsInvented )
                    {
                        Task.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch1_Water_DeathAndKnowledge.DuringGameplay_StartIfNeeded();
                    }
                    break;
                case "Ch1_Water_DeathAndKnowledge":
                    if ( FlagRefs.Ch1_MIN_PrismTung.DuringGameplay_TurnStarted > 0 )
                    {
                        Task.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch1_Water_Tungsten.DuringGameplay_StartIfNeeded();
                    }
                    break;
                case "Ch1_Water_Tungsten":
                    if ( FlagRefs.Ch1_MIN_ManArmorPierce.DuringGameplay_TurnStarted > 0 )
                    {
                        Task.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch1_Water_UsingItems.DuringGameplay_StartIfNeeded();
                        FlagRefs.Ch1_Water_DefeatingTheCruiser.DuringGameplay_StartIfNeeded();
                    }
                    break;
                case "Ch1_Water_UsingItems":
                    if ( UnlockRefs.AcousticNanotubeFiltration.DuringGame_ReadiedByInspiration != null )
                    {
                        Task.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch1_Water_BuildAFiltrationTower.DuringGameplay_StartIfNeeded();
                    }
                    break;
                case "Ch1_Water_DefeatingTheCruiser":
                    if ( UnlockRefs.AcousticNanotubeFiltration.DuringGame_ReadiedByInspiration != null )
                    {
                        Task.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch1_Water_BuildAFiltrationTower.DuringGameplay_StartIfNeeded();
                    }
                    break;
                case "Ch1_Water_BuildAFiltrationTower":
                    if ( FlagRefs.Ch1_FindingFood.DuringGameplay_TurnStarted > 0 )
                    {
                        Task.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch1_Food_Intro.DuringGameplay_StartIfNeeded();
                    }
                    break;
                case "Ch1_Food_Intro":
                    if ( FlagRefs.Ch1_FindingFood.DuringGame_IntendedOutcome != null )
                        Task.DuringGameplay_CompleteIfActive();
                    break;
                case "Ch0_ReadHandbookEntry":
                case "HackTut_OverallGoal":
                case "HackTut_LeafNode":
                case "HackTut_RunRules":
                case "HackTut_MakeAMove":
                case "HackTut_DestroyADaemon":
                case "HackTut_FinishTheJob":
                case "HackTut_CorruptThePilotCradle":
                    //nothing to do here
                    break;
                case "Ch0_BuildTower":
                    if ( SimCommon.TheNetwork != null )
                        Task.DuringGameplay_CompleteIfActive();
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "CityTasks_Basics: Called DoPerFrameIfActive for '" + Task.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }

        public bool GetShouldBeVisible( CityTask Task )
        {
            switch ( Task.ID )
            {
                case "Ch1_Water_NewStyleOfInvestigation":
                    {
                        if ( FlagRefs.Ch1_MIN_WeaponsAndArmor.DuringGame_ActualOutcome == null )
                            return false;
                    }
                    break;
            }
            return true;
        }
    }
}
