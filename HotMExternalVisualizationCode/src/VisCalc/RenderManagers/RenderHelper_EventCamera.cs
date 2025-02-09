using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using System.Diagnostics;
using DiffLib;
using System.Net;

namespace Arcen.HotM.ExternalVis
{
    public static class RenderHelper_EventCamera
    {
        private readonly static MersenneTwister workingRand = new MersenneTwister( 0 );
        private static int LastReloadCount = 0;
        private static Int64 LastNumberOfContextChanges = 0;
        private static int LastTurnNumber = 0;
        private static EventCharacterSourceCode LastSourceCode = 0;
        private static int LastSourceWithinCodeID = 0;
        private static int LastBuildingID = 0;

        private static bool PlayerCharacterShouldShowAsAggressive = false;
        private static float AddedPlayerRotationY = 0;
        internal static float AddedRightHandRotationY = 0;
        private static float TimeWithNoRendering = 0f;
        private static bool HasResetAfterNotRendering = false;
        private static MaterializeType PlayerCharacterMaterializing = MaterializeType.LowEventCamera_PlayerAppear;
        private static float PlayerCharacterMaterializeProgress = 0f;

        private static float RightHandXRecenter = 0;
        private static float RightHandZRecenter = 0;

        #region ValidateAndAddAnyNeededCharactersOnRight
        private static void ValidateAndAddAnyNeededCharactersOnRight( EventCharacterSourceCode SourceCode, int SourceWithinCodeID, int BuildingID,
            string RowIDForErrors, Dictionary<string, EventCharacterPosition> CharactersToAddOnRight )
        {
            if ( LastReloadCount == Engine_HotM.NumberOfSelectDataReloads &&
                LastNumberOfContextChanges == Engine_HotM.NumberOfContextChanges &&
                LastTurnNumber == SimCommon.Turn &&
                LastSourceCode == SourceCode &&
                LastSourceWithinCodeID == SourceWithinCodeID &&
                LastBuildingID == BuildingID )
                return; //all of this stuff matched, so just keep whatever is already here

            //if the above did not match, then hard-stop and recreate this
            LastReloadCount = Engine_HotM.NumberOfSelectDataReloads;
            LastNumberOfContextChanges = Engine_HotM.NumberOfContextChanges;
            LastTurnNumber = SimCommon.Turn;
            LastSourceCode = SourceCode;
            LastSourceWithinCodeID = SourceWithinCodeID;
            LastBuildingID = BuildingID;

            RightHandXRecenter = 0;
            RightHandZRecenter = 0;
            AddedRightHandRotationY = 0;
            VisCurrent.LowEventCameraRightHandParent.localRotation = Quaternion.Euler( 0, AddedRightHandRotationY, 0 );

            #region Clear Out Characters On The Right
            foreach ( RenderCharacterObject character in VisCurrent.CharactersOnRight )
            {
                character.SetFadeOut( VisCurrent.ShouldCurrentVisCharactersDie );
                VisCurrent.FadingOutCharactersOnRight.Add( character );
            }
            VisCurrent.CharactersOnRight.Clear();
            VisCurrent.ShouldCurrentVisCharactersDie = false;
            VisCurrent.ShouldCurrentVisCharactersFadeOutRightAway = false;            
            #endregion

            if ( CharactersToAddOnRight == null || CharactersToAddOnRight.Count == 0 )
                return;

            workingRand.ReinitializeWithSeed( (int)SourceCode + SourceWithinCodeID + BuildingID + SimCommon.CurrentRandSeed );

            float minX = 1000;
            float minZ = 1000;
            float maxX = -1000;
            float maxZ = -1000;
            foreach ( KeyValuePair<string, EventCharacterPosition> kv in CharactersToAddOnRight )
            {
                RenderCharacterObject obj = new RenderCharacterObject( RowIDForErrors, kv.Value, workingRand );
                VisCurrent.CharactersOnRight.Add( obj );

                if ( obj.PositionOffset.x < minX )
                    minX = obj.PositionOffset.x;
                if ( obj.PositionOffset.x > maxX )
                    maxX = obj.PositionOffset.x;

                if ( obj.PositionOffset.z < minZ )
                    minZ = obj.PositionOffset.z;
                if ( obj.PositionOffset.z > maxZ )
                    maxZ = obj.PositionOffset.z;
            }

            float centerX = minX + (( maxX - minX ) / 2 );
            float centerZ = minZ + ((maxZ - minZ) / 2);

            RightHandXRecenter = centerX;
            RightHandZRecenter = centerZ;

            foreach ( RenderCharacterObject obj in VisCurrent.CharactersOnRight )
            {
                obj.PositionOffset.x -= centerX;
                obj.PositionOffset.z -= centerZ;
            }

            VisCurrent.LowEventCameraRightHandParent.localPosition = new Vector3( MathRefs.EventCameraRightOffsetX.FloatMin + RightHandXRecenter,
                MathRefs.EventCameraRightOffsetY.FloatMin, MathRefs.EventCameraRightOffsetZ.FloatMin + RightHandZRecenter );
        }
        #endregion

        public static void RenderIfNeeded()
        {
            int debugStage = 0;
            try
            {
                debugStage = 1100;
                if ( !TryRenderDimmingPlaneForEventIfNeeded() )
                    TryRenderFocusDimmingPlaneForIfNeeded();

                debugStage = 5100;

                #region If Killing The Characters On The Right
                if ( VisCurrent.ShouldCurrentVisCharactersDie )
                {
                    VisCurrent.ShouldCurrentVisCharactersDie = false;
                    VisCurrent.ShouldCurrentVisCharactersFadeOutRightAway = false;
                    foreach ( RenderCharacterObject character in VisCurrent.CharactersOnRight )
                    {
                        character.SetFadeOut( true );
                        VisCurrent.FadingOutCharactersOnRight.Add( character );
                    }
                    VisCurrent.CharactersOnRight.Clear();
                }
                #endregion

                debugStage = 9100;

                #region If Fading Out The Characters On The Right Sooner Than Later
                if ( VisCurrent.ShouldCurrentVisCharactersFadeOutRightAway )
                {
                    VisCurrent.ShouldCurrentVisCharactersDie = false;
                    VisCurrent.ShouldCurrentVisCharactersFadeOutRightAway = false;
                    foreach ( RenderCharacterObject character in VisCurrent.CharactersOnRight )
                    {
                        character.SetFadeOut( false );
                        VisCurrent.FadingOutCharactersOnRight.Add( character );
                    }
                    VisCurrent.CharactersOnRight.Clear();
                }
                #endregion

                debugStage = 16100;

                NPCEvent cEvent = SimCommon.CurrentEvent;
                bool rendered = false;
                if ( SimCommon.CurrentSimpleChoice != null )
                {
                    debugStage = 112100;

                    if ( SimCommon.CurrentSimpleChoice == MinorEventHandler.Instance )
                    {
                        debugStage = 142100;

                        //if a minor event, we need to render that also
                        ISimMachineActor machineActorOnLeftOrNull = MinorEventHandler.Instance.MinorEventActorOrNull;
                        cEvent = MinorEventHandler.Instance.MinorEvent;
                        if ( machineActorOnLeftOrNull != null )
                            Engine_HotM.SetSelectedActor( machineActorOnLeftOrNull, false, true, true );
                        PlayerCharacterShouldShowAsAggressive = cEvent.PlayerCharacterShowsAggressive;
                        if ( machineActorOnLeftOrNull != null )
                            RenderMachineActorOnLeft( machineActorOnLeftOrNull );

                        ValidateAndAddAnyNeededCharactersOnRight( EventCharacterSourceCode.NPCEvent,
                            cEvent.FixedEventNumberForCharacters >= 0 ? cEvent.FixedEventNumberForCharacters : cEvent.RowIndexNonSim,
                            ((SimCommon.CurrentEventActor as ISimMachineUnit)?.ContainerLocation.Get() as ISimBuilding)?.GetBuildingID() ?? -1, cEvent.ID, cEvent.CharactersToShow );

                        foreach ( RenderCharacterObject character in VisCurrent.CharactersOnRight )
                            character.Render();

                        rendered = true;
                    }
                    else
                    {
                        debugStage = 182100;

                        ISimpleChoiceProvider provider = SimCommon.CurrentSimpleChoice;
                        if ( provider != null )
                        {
                            if ( Engine_HotM.SelectedActor is ISimMachineActor machineActor )
                            {
                                PlayerCharacterShouldShowAsAggressive = provider.GetShouldPlayerCharacterShowAsAggressive();
                                RenderMachineActorOnLeft( machineActor );

                                ValidateAndAddAnyNeededCharactersOnRight( provider.GetCharacterSourceCode(), provider.GetCharacterSourceWithinCodeID(),
                                    provider.GetCharacterSourceBuildingID(), provider.GetCharacterRowIDForErrors(), provider.GetCharactersToAddOnRight() );

                                foreach ( RenderCharacterObject character in VisCurrent.CharactersOnRight )
                                    character.Render();

                                rendered = true;
                            }
                        }
                    }
                }
                else if ( VisCurrent.IsShowingActualEvent && cEvent != null )//&& (Window_EventWindow.Instance?.GetShouldDrawThisFrame()??false) )
                {
                    debugStage = 312100;

                    if ( SimCommon.CurrentEventActor is ISimMachineActor machineActor )
                    {
                        debugStage = 314100;

                        Engine_HotM.SetSelectedActor( machineActor, false, true, true );

                        PlayerCharacterShouldShowAsAggressive = cEvent.PlayerCharacterShowsAggressive;
                        RenderMachineActorOnLeft( machineActor );

                        ValidateAndAddAnyNeededCharactersOnRight( EventCharacterSourceCode.NPCEvent, cEvent.RowIndexNonSim,
                            ((SimCommon.CurrentEventActor as ISimMachineUnit)?.ContainerLocation.Get() as ISimBuilding)?.GetBuildingID() ?? -1, cEvent.ID, cEvent.CharactersToShow );

                        foreach ( RenderCharacterObject character in VisCurrent.CharactersOnRight )
                            character.Render();

                        rendered = true;
                    }
                }
                else if ( Window_RewardWindow.Instance?.IsOpen ?? false )
                {
                    debugStage = 512100;

                    if ( SimCommon.RewardProvider == NPCDialogChoiceHandler.Instance )
                    {
                        debugStage = 513100;
                        //if a dialog, then we need to render the speakers
                        ISimMachineActor machineActorOnLeft = NPCDialogChoiceHandler.Instance.MachineActor;
                        PlayerCharacterShouldShowAsAggressive = NPCDialogChoiceHandler.Instance.Dialog?.PlayerCharacterShowsAggressive ?? false;
                        if ( RenderMachineActorOnLeft( machineActorOnLeft ) )
                            rendered = true;

                        ISimNPCUnit npcUnitOnRight = NPCDialogChoiceHandler.Instance.NPCUnit;
                        if ( RenderNPCUnitOnRight( npcUnitOnRight ) )
                            rendered = true;
                    }
                    else
                    {
                        //nothing to do here, I don't think
                    }
                }
                else if ( Window_Debate.Instance?.IsOpen ?? false )
                {
                    debugStage = 712100;

                    //if a debate, then we need to render the participants
                    ISimMachineActor machineActorOnLeft = SimCommon.DebateSource;
                    PlayerCharacterShouldShowAsAggressive = false;// NPCDialogChoiceHandler.Instance.Dialog?.PlayerCharacterShowsAggressive ?? false;
                    if ( RenderMachineActorOnLeft( machineActorOnLeft ) )
                        rendered = true;

                    ISimNPCUnit npcUnitOnRight = SimCommon.DebateTarget;
                    if ( RenderNPCUnitOnRight( npcUnitOnRight ) )
                        rendered = true;
                }

                debugStage = 912100;

                if ( VisCurrent.FadingOutCharactersOnRight.Count > 0 )
                {
                    debugStage = 913100;

                    for ( int i = VisCurrent.FadingOutCharactersOnRight.Count - 1; i >= 0; i-- )
                    {
                        RenderCharacterObject character = VisCurrent.FadingOutCharactersOnRight[i];
                        character.Render();
                        if ( character.IsReadyToRemove )
                            VisCurrent.FadingOutCharactersOnRight.RemoveAt( i );
                    }

                    rendered = true;
                }

                debugStage = 1212100;

                if ( rendered )
                {
                    debugStage = 1214100;

                    TimeWithNoRendering = 0;
                    HasResetAfterNotRendering = false;

                    #region Input Since Rendered
                    bool isMovingSlow = InputCaching.inputMovementSlowDown.CalculateIsKeyDownNow_IgnoreConflicts();
                    bool isMovingFast = InputCaching.inputMovementSpeedUp.CalculateIsKeyDownNow_IgnoreConflicts();
                    //leftRight = InputCaching.CalculateMovementForwardBackwardAxis();
                    //forwardBack = InputCaching.CalculateMovementLeftRightAxis();

                    float rotateSpeed = isMovingFast ? 180 : (isMovingSlow ? 20 : 70f);

                    float leftRotation = InputCaching.CalculateMovementLeftRightAxis();
                    if ( leftRotation != 0f )
                        AddedPlayerRotationY += leftRotation * rotateSpeed * ArcenTime.AnyDeltaTime;

                    float rightRotation = InputCaching.CalculateMovementRotateLeftRightAxis();
                    if ( rightRotation != 0f )
                    {
                        AddedRightHandRotationY += rightRotation * rotateSpeed * ArcenTime.AnyDeltaTime;
                        VisCurrent.LowEventCameraRightHandParent.localRotation = Quaternion.Euler( 0, AddedRightHandRotationY, 0 );
                    }

                    if ( PlayerCharacterMaterializing != MaterializeType.None )
                    {
                        PlayerCharacterMaterializeProgress += (ArcenTime.SmoothUnpausedDeltaTime * PlayerCharacterMaterializing.GetSpeed());
                        if ( PlayerCharacterMaterializeProgress >= 1f )
                            PlayerCharacterMaterializing = MaterializeType.None;
                    }

                    #endregion Input Since Rendered

                    debugStage = 1412100;
                }
                else
                {
                    debugStage = 1812100;

                    if ( !HasResetAfterNotRendering )
                    {
                        debugStage = 1815100;

                        if ( TimeWithNoRendering > 0.25f )
                        {
                            HasResetAfterNotRendering = true;
                            AddedPlayerRotationY = 0f;
                            AddedRightHandRotationY = 0f;
                            PlayerCharacterMaterializing = MaterializeType.LowEventCamera_PlayerAppear;
                            PlayerCharacterMaterializeProgress = 0f;
                            VisCurrent.LowEventCameraRightHandParent.localRotation = Quaternion.identity;
                            VisCurrent.CharactersOnRight.Clear();
                        }
                        else
                            TimeWithNoRendering += ArcenTime.AnyDeltaTime;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "RenderHelperEventCamera-RenderIfNeeded", debugStage, e, Verbosity.ShowAsError );
            }
        }

        public static bool IsRenderingCharactersOnTheRight()
        {
            if ( VisCurrent.CharactersOnRight.Count > 0 )
                return true;

            if ( (Window_RewardWindow.Instance?.IsOpen ?? false) &&
                SimCommon.RewardProvider == NPCDialogChoiceHandler.Instance ) //this case does not include
                return true;

            if ( Window_Debate.Instance?.IsOpen ?? false )
                return true;

            return false;
        }

        #region TryRenderDimmingPlaneForEventIfNeeded
        private static bool TryRenderDimmingPlaneForEventIfNeeded()
        {
            if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
                return false;
            if ( VisCurrent.IsShowingActualEvent || !FlagRefs.HasEmergedIntoMap.DuringGameplay_IsTripped )
            {
                VisDrawingObjectTag tag = SimMetagame.CurrentChapterNumber == 0 ? CommonRefs.EventDimming_ChapterZero : CommonRefs.EventDimming_General;
                if ( tag == null || tag.SimpleObjects.Count <= 0 )
                    return false;

                if ( SimCommon.CurrentEvent?.MajorData == null )
                    return false; //if not a major event, also do not dim

                VisSimpleDrawingObject drawingObject = tag.SimpleObjects[0];
                if ( drawingObject == null )
                    return false;

                A5RendererGroup rendGroup = drawingObject.RendererGroup as A5RendererGroup;
                if ( rendGroup == null )
                    return false;

                Vector3 pos = VisCurrent.LowEventCameraTransform.TransformPoint( 0, 0, 7 );

                Vector3 scale = new Vector3( 40, 20, 1 );
                rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, Quaternion.identity, scale,
                    RenderColorStyle.LowEventCameraNoColor );
                return true;
            }
            return false;
        }
        #endregion

        #region TryRenderFocusDimmingPlaneForIfNeeded
        private static bool TryRenderFocusDimmingPlaneForIfNeeded()
        {
            if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
                return false;
            if ( Engine_HotM.IsBigBannerBeingShown )
                return false;

            if ( Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_AlsoDim )
            {
                VisDrawingObjectTag tag = CommonRefs.FocusDimming_General;
                if ( tag == null || tag.SimpleObjects.Count <= 0 )
                    return false;

                VisSimpleDrawingObject drawingObject = tag.SimpleObjects[0];
                if ( drawingObject == null )
                    return false;

                A5RendererGroup rendGroup = drawingObject.RendererGroup as A5RendererGroup;
                if ( rendGroup == null )
                    return false;

                Vector3 pos = VisCurrent.LowEventCameraTransform.TransformPoint( 0, 0, 7 );

                Vector3 scale = new Vector3( 40, 20, 1 );
                rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, Quaternion.identity, scale,
                    RenderColorStyle.LowEventCameraNoColor );
                return true;
            }
            return false;
        }
        #endregion

        

        #region RenderOnLeft
        private static bool RenderMachineActorOnLeft( ISimMachineActor machineActor)
        {
            if ( machineActor == null )
                return false;

            if ( machineActor is ISimMachineUnit unit )
                RenderMachineUnitOnLeft( unit );
            else if ( machineActor is ISimMachineVehicle vehicle )
                RenderMachineVehicleOnLeft( vehicle );
            return true;
        }

        private static void RenderMachineUnitOnLeft( ISimMachineUnit unit )
        {
            if ( unit == null )
                return;

            MachineUnitType unitType = unit.UnitType;
            if ( unitType == null ) 
                return;

            int lodToDraw = 0;
            FrameBufferManagerData.LOD0Count.Construction++;

            VisLODDrawingObject drawingData = PlayerCharacterShouldShowAsAggressive ? unitType.VisObjectAggressive : unitType.VisObjectCasual;
            if ( drawingData == null )
                return;
            A5RendererGroup rendGroup = drawingData.LODRenderGroups[lodToDraw] as A5RendererGroup;
            if ( rendGroup == null )
                return;

            Vector3 pos = VisCurrent.LowEventCameraTransform.TransformPoint( MathRefs.EventCameraLeftOffsetX.FloatMin, 
                MathRefs.EventCameraLeftOffsetY.FloatMin + drawingData.EventCameraExtraYOffset,
                MathRefs.EventCameraLeftOffsetZ.FloatMin );
            Quaternion rot = Quaternion.Euler( 0, MathRefs.EventCameraLeftRotationY.FloatMin + AddedPlayerRotationY + drawingData.EventCameraExtraRotation, 0 );

            Vector3 scale = Vector3.one * drawingData.EventCameraScale;

            switch ( PlayerCharacterMaterializing )
            {
                case MaterializeType.LowEventCamera_PlayerAppear:
                    if ( PlayerCharacterMaterializeProgress >= 0.8f ) //stop early
                        break;
                    {
                        PlayerCharacterMaterializing.RenderPrep( PlayerCharacterMaterializeProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.LowEventCameraMaterializeAppear, maskOffset );
                    }
                    return;
            }

            rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, scale,
                RenderColorStyle.LowEventCameraNoColor );
        }

        private static void RenderMachineVehicleOnLeft( ISimMachineVehicle vehicle )
        {
            if ( vehicle == null )
                return;

            VisSimpleDrawingObject drawingData = vehicle.VehicleType?.VisObjectToDraw;
            if ( drawingData == null )
                return;
            A5RendererGroup rendGroup = drawingData.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
                return;

            Vector3 pos = VisCurrent.LowEventCameraTransform.TransformPoint( MathRefs.EventCameraLeftOffsetX.FloatMin, 
                MathRefs.EventCameraLeftOffsetY.FloatMin + drawingData.EventCameraExtraYOffset,
                MathRefs.EventCameraLeftOffsetZ.FloatMin );
            Quaternion rot = Quaternion.Euler( 0, MathRefs.EventCameraLeftRotationY.FloatMin + AddedPlayerRotationY + drawingData.EventCameraExtraRotation, 0 );

            Vector3 scale = Vector3.one * drawingData.EventCameraScale;

            switch ( PlayerCharacterMaterializing )
            {
                case MaterializeType.LowEventCamera_PlayerAppear:
                    if ( PlayerCharacterMaterializeProgress >= 0.8f ) //stop early
                        break;
                    {
                        Vector3 fakePos = pos; //don't do the position part
                        PlayerCharacterMaterializing.RenderPrep( PlayerCharacterMaterializeProgress, 12f,
                            ref fakePos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.LowEventCameraMaterializeAppear, maskOffset );
                    }
                    return;
            }

            rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, scale,
                RenderColorStyle.LowEventCameraNoColor );
        }
        #endregion RenderOnLeft

        #region RenderNPCUnitOnRight
        private static bool RenderNPCUnitOnRight( ISimNPCUnit unit )
        {
            if ( unit == null )
                return false;

            if ( unit.GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject floatingLODObject,
                out IAutoPooledFloatingObject floatingSimpleObject, out Color drawColor ) )
            {
                if ( floatingLODObject != null )
                    return RenderNPCUnitOnRight_LOD( unit, floatingLODObject );
                else
                    return RenderNPCUnitOnRight_Simple( unit, floatingSimpleObject );
            }
            return false;
        }

        private static bool RenderNPCUnitOnRight_LOD( ISimNPCUnit unit, IAutoPooledFloatingLODObject floatingLODObject )
        {
            if ( unit == null || floatingLODObject == null )
                return false;
            int lodToDraw = 0;
            FrameBufferManagerData.LOD0Count.Construction++;

            VisLODDrawingObject drawingData = floatingLODObject.Object;
            if ( drawingData == null )
                return false;
            A5RendererGroup rendGroup = drawingData.LODRenderGroups[lodToDraw] as A5RendererGroup;
            if ( rendGroup == null )
                return false;

            Vector3 pos = VisCurrent.LowEventCameraTransform.TransformPoint( MathRefs.EventCameraRightOffsetX.FloatMin,
                MathRefs.EventCameraRightOffsetY.FloatMin + drawingData.EventCameraExtraYOffset,
                MathRefs.EventCameraRightOffsetZ.FloatMin );
            Quaternion rot = Quaternion.Euler( 0, MathRefs.EventCameraRightRotationY.FloatMin + AddedRightHandRotationY + drawingData.EventCameraExtraRotation, 0 );

            Vector3 scale = Vector3.one * drawingData.EventCameraScale;

            switch ( PlayerCharacterMaterializing )
            {
                case MaterializeType.LowEventCamera_PlayerAppear:
                    if ( PlayerCharacterMaterializeProgress >= 0.8f ) //stop early
                        break;
                    {
                        PlayerCharacterMaterializing.RenderPrep( PlayerCharacterMaterializeProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.LowEventCameraMaterializeAppear, maskOffset );
                    }
                    return true;
            }

            rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, scale,
                RenderColorStyle.LowEventCameraNoColor );
            return true;
        }

        private static bool RenderNPCUnitOnRight_Simple( ISimNPCUnit unit, IAutoPooledFloatingObject floatingSimpleObject )
        {
            if ( unit == null || floatingSimpleObject == null )
                return false;

            VisSimpleDrawingObject drawingData = floatingSimpleObject.EffectiveObject;
            if ( drawingData == null )
                return false;
            A5RendererGroup rendGroup = drawingData.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
                return false;

            Vector3 pos = VisCurrent.LowEventCameraTransform.TransformPoint( MathRefs.EventCameraRightOffsetX.FloatMin,
                MathRefs.EventCameraRightOffsetY.FloatMin + drawingData.EventCameraExtraYOffset,
                MathRefs.EventCameraRightOffsetZ.FloatMin );
            Quaternion rot = Quaternion.Euler( 0, MathRefs.EventCameraRightRotationY.FloatMin + AddedRightHandRotationY + drawingData.EventCameraExtraRotation, 0 );

            Vector3 scale = Vector3.one * drawingData.EventCameraScale;

            switch ( PlayerCharacterMaterializing )
            {
                case MaterializeType.LowEventCamera_PlayerAppear:
                    if ( PlayerCharacterMaterializeProgress >= 0.8f ) //stop early
                        break;
                    {
                        Vector3 fakePos = pos; //don't do the position part
                        PlayerCharacterMaterializing.RenderPrep( PlayerCharacterMaterializeProgress, 12f,
                            ref fakePos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.LowEventCameraMaterializeAppear, maskOffset );
                    }
                    return true;
            }

            rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, scale,
                RenderColorStyle.LowEventCameraNoColor );
            return true;
        }
        #endregion
    }
}
