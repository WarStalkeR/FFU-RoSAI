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
    public static class RenderHelper_Objects
    {
        private static readonly List<MapItem> staticWorkingBuildings = List<MapItem>.Create_WillNeverBeGCed( 20, "RenderHelper_Objects-staticWorkingBuildings" );

        public const float MIN_DISTANCE_BEHIND = 4.5f;
        public const float MIN_DISTANCE_SQUARED_BEHIND = MIN_DISTANCE_BEHIND * MIN_DISTANCE_BEHIND;

        #region DrawCityVehicle
        internal static void DrawCityVehicle( ISimCityVehicle Vehicle, IAutoPooledFloatingObject floatingObject, Color colorIncludingAlpha, bool DrawAsDarkened )
        {
            if ( floatingObject == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: floatingObject null", Verbosity.DoNotShow );
                return;
            }

            A5Renderer rend = floatingObject.Renderer as A5Renderer;
            if ( rend == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: rend null", Verbosity.DoNotShow );
                return;
            }
            A5RendererGroup rendGroup = rend.ParentGroup as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            if ( colorIncludingAlpha.a <= 0 )
            {
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: colorIncludingAlpha <= 0", Verbosity.DoNotShow );
                return;
            }

            RenderColorStyle colorStyle = DrawAsDarkened ? RenderColorStyle.FogOfWar : RenderColorStyle.SelfColor;
            RenderOpacity opacity = RenderOpacity.Normal;

            if ( colorIncludingAlpha.a < 1f )
                opacity = RenderOpacity.Transparent_Sorted;

            if ( DrawAsDarkened )
                colorIncludingAlpha.r = colorIncludingAlpha.g = colorIncludingAlpha.b = 0f;

            {
                Quaternion rot = floatingObject.Rotation;
                if ( rend.Rotates )
                    rot *= rend.RotationForInGameGlobal;

                Vector3 pos = floatingObject.WorldLocation;

                rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, floatingObject.EffectiveScale, colorStyle, colorIncludingAlpha, opacity, true );

                //ArcenDebugging.LogSingleLine( "Draw vehicle: " + rend.name + " at " + pos + " " + rot + " " + 
                //    floatingObject.EffectiveScale + " color: " + colorIncludingAlpha, Verbosity.DoNotShow );

                //if ( floatingObject.IsMouseover )
                //{
                //    rendGroup.WriteToDrawBufferForOneFrame( pos, rot, floatingObject.EffectiveScale.ComponentWiseMult( VehicleHighlight_ScaleMult ),
                //        RenderColorStyle.HighlightColor, ColorRefs.HoveredMachineVehicle.ColorHDR, RenderOpacity.Normal ); //already transparent if it will be, don't need to mess with it
                //}
            }
        }
        #endregion

        #region DrawMachineVehicle
        internal static void DrawMachineVehicle( ISimMachineVehicle Vehicle, IAutoPooledFloatingObject floatingObject, Color colorIncludingAlpha,
            bool DrawAsDarkened, bool IsSelectedUnit )
        {
            if ( floatingObject == null )
            {
                //Vehicle.DebugText = "floatingObject null!";
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: floatingObject null", Verbosity.DoNotShow );
                return;
            }

            A5Renderer rend = floatingObject.Renderer as A5Renderer;
            if ( rend == null )
            {
                //Vehicle.DebugText = "rend null!";
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: rend null", Verbosity.DoNotShow );
                return;
            }
            A5RendererGroup rendGroup = rend.ParentGroup as A5RendererGroup;
            if ( rendGroup == null )
            {
                //Vehicle.DebugText = "rendGroup null!";
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            if ( colorIncludingAlpha.a <= 0 )
            {
                //Vehicle.DebugText = "colorIncludingAlpha <= 0!";
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: colorIncludingAlpha <= 0", Verbosity.DoNotShow );
                return;
            }

            float halfHeight = Vehicle.GetHalfHeightForCollisions();
            Vector3 center = Vehicle.GetPositionForCollisions();
            float radius = Vehicle.GetRadiusForCollisions();

            if ( Engine_HotM.SelectedActor != Vehicle && !CameraCurrent.TestFrustumColliderInternalFast( center, halfHeight, radius ) )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return;
            }

            float distSquared = (CameraCurrent.CameraBodyPosition - center).sqrMagnitude;

            Quaternion rot = floatingObject.Rotation;
            if ( rend.Rotates )
                rot *= rend.RotationForInGameGlobal;

            Vector3 pos = floatingObject.WorldLocation;

            switch ( Vehicle.Materializing )
            {
                case MaterializeType.Appear:
                    if ( Vehicle.MaterializingProgress >= 1f )
                        break;
                    {
                        Vector3 scale = floatingObject.EffectiveScale;
                        Vehicle.Materializing.RenderPrep( Vehicle.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeAppear, maskOffset );
                    }
                    return;
                case MaterializeType.Reveal:
                    if ( Vehicle.MaterializingProgress >= 1f )
                        break;
                    {
                        Vector3 scale = floatingObject.EffectiveScale;
                        Vehicle.Materializing.RenderPrep( Vehicle.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeReveal, maskOffset );
                    }
                    return;
                case MaterializeType.BurnDownLarge:
                case MaterializeType.BurnDownSmall:
                    {
                        Vector3 scale = floatingObject.EffectiveScale;
                        Vehicle.Materializing.RenderPrep( Vehicle.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeBurnDown, maskOffset );
                    }
                    return;
            }

            bool shouldSkipGlowsAndIndicators = VisCurrent.IsInPhotoMode || VisCurrent.GetShouldBeBlurred();

            RenderColorStyle colorStyle = DrawAsDarkened ? RenderColorStyle.FogOfWar : RenderColorStyle.SelfColor;
            RenderOpacity opacity = RenderOpacity.Normal;

            if ( colorIncludingAlpha.a < 1f )
                opacity = RenderOpacity.Transparent_Sorted;

            if ( DrawAsDarkened )
                colorIncludingAlpha.r = colorIncludingAlpha.g = colorIncludingAlpha.b = 0f;

            bool isHighVisibility = InputCaching.UseHighVisibilityUnitColors;
            if ( InputCaching.IsInInspectMode_Any )
                isHighVisibility = !isHighVisibility;

            if ( !Vehicle.VehicleType.ShowsHighlightAsSmallVehicle )
                isHighVisibility = false;

            if ( IsSelectedUnit && !shouldSkipGlowsAndIndicators )
            {
                //rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingObject.EffectiveScale, RenderColorStyle.HighlightOutlineLarge );

                float addedYOffset = -Vehicle.VehicleType.VisObjectExtraOffset;
                float addedXZSize = Vehicle.VehicleType.RadiusForCollisions;

                SharedRenderManagerData.DrawSelectionHexAroundActor( Vehicle, ColorRefs.UnitNewStyleSelectionA,
                    MathRefs.SelectedHexA_XZ_Size.FloatMin, MathRefs.SelectedHexA_XZ_AddedSize.FloatMin, MathRefs.SelectedHexA_XZ_Speed.FloatMin,
                    MathRefs.SelectedHexA_XZ_Thickness.FloatMin, MathRefs.SelectedHexA_Y_Offset.FloatMin + addedYOffset );
                SharedRenderManagerData.DrawSelectionHexAroundActor( Vehicle, ColorRefs.UnitNewStyleSelectionB,
                    MathRefs.SelectedHexB_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexB_XZ_AddedSize.FloatMin, MathRefs.SelectedHexB_XZ_Speed.FloatMin,
                    MathRefs.SelectedHexB_XZ_Thickness.FloatMin, MathRefs.SelectedHexB_Y_Offset.FloatMin + addedYOffset );
                SharedRenderManagerData.DrawSelectionHexAroundActor( Vehicle, ColorRefs.UnitNewStyleSelectionC,
                    MathRefs.SelectedHexC_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexC_XZ_AddedSize.FloatMin, MathRefs.SelectedHexC_XZ_Speed.FloatMin,
                    MathRefs.SelectedHexC_XZ_Thickness.FloatMin, MathRefs.SelectedHexC_Y_Offset.FloatMin + addedYOffset );
            }

            {
                if ( isHighVisibility && !shouldSkipGlowsAndIndicators )
                    rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingObject.EffectiveScale, RenderColorStyle.OutlineUnitGoldB );
                else
                {
                    if ( Vehicle.IsCloaked || Vehicle.DrawAsInvisibleEvenThoughNotCloaked )
                    {
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingObject.EffectiveScale, RenderColorStyle.GlassyUncolored );
                    }
                    else
                    {
                        rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, floatingObject.EffectiveScale, colorStyle, colorIncludingAlpha, opacity, true );
                    }

                    if ( Vehicle.IsTakingCover ) //shields are up
                    {
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingObject.EffectiveScale.ComponentWiseMult( ShieldOverlay_ScaleMultVehicles ),
                            RenderColorStyle.ShieldUncolored );
                    }
                }

                if ( !isHighVisibility && distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && 
                    Vehicle.VehicleType.ShowsHighlightAsSmallVehicle && !shouldSkipGlowsAndIndicators )
                    rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingObject.EffectiveScale, RenderColorStyle.OutlineUnitGoldA );

                //ArcenDebugging.LogSingleLine( "Draw vehicle: " + rend.name + " at " + pos + " " + rot + " " + 
                //    floatingObject.EffectiveScale + " color: " + colorIncludingAlpha, Verbosity.DoNotShow );

                if ( InputCaching.Debug_ShowEntityRadii )
                {
                    DrawShape_WireCylinder shapeCylinder = new DrawShape_WireCylinder
                    {
                        Start = center.PlusY( -halfHeight ),
                        End = center.PlusY( halfHeight ),
                        Radius = radius,
                        Color = ColorMath.LeafGreen,
                        Thickness = 2f
                    };
                    VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeCylinder );
                }

                if ( InputCaching.Debug_ShowEntityShotEmissionPoints )
                {
                    ProtectedList<ShotEmissionGroup> list = Vehicle.GetShotEmissionGroups();
                    if ( list != null && list.Count > 0 )
                    {
                        Vector3 primeCenter = Vehicle.GetPositionForCollisions();
                        Quaternion rotation = Quaternion.Euler( 0, Vehicle.GetRotationYForCollisions(), 0 );

                        foreach ( ShotEmissionGroup group in list )
                        {
                            foreach ( ShotEmissionPoint emiss in group.EmissionPoints )
                            {
                                Vector3 subCenter = emiss.CalculatePointFrom( primeCenter, rotation );

                                DrawShape_WireBoxOriented shapeBox = new DrawShape_WireBoxOriented
                                {
                                    Center = subCenter,
                                    Size = new Vector3( 0.15f, 0.15f, 0.15f ),
                                    Color = group.DebugColor,
                                    Rotation = rotation,
                                    Thickness = 1f
                                };
                                VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeBox );
                            }

                            if ( Engine_HotM.SelectedActor == Vehicle )
                            {
                                if ( group.TargetingZones.Count > 0 )
                                {
                                    foreach ( ShotEmissionTargetingZone zone in group.TargetingZones )
                                    {
                                        Vector3 subBase = zone.CalculateSubBaseFrom( primeCenter, rotation );

                                        DrawShape_WireCylinder shapeCylinder = new DrawShape_WireCylinder
                                        {
                                            Start = subBase,
                                            End = subBase.PlusY( zone.Height ),
                                            Radius = zone.Radius,
                                            Color = group.DebugColor,
                                            Thickness = 1f
                                        };
                                        VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeCylinder );
                                    }
                                }
                            }
                        }
                    }
                }

                //if ( floatingObject.IsMouseover )
                //{
                //    rendGroup.WriteToDrawBufferForOneFrame( pos, rot, floatingObject.EffectiveScale.ComponentWiseMult( VehicleHighlight_ScaleMult ),
                //        RenderColorStyle.HighlightColor, ColorRefs.HoveredMachineVehicle.ColorHDR, RenderOpacity.Normal ); //already transparent if it will be, don't need to mess with it
                //}
            }
        }

        internal static readonly Vector3 VehicleHighlight_ScaleMult = new Vector3( 1.08f, 1.08f, 1.08f );
        #endregion

        #region DrawMachineUnit
        internal static void DrawMachineUnit( ISimMachineUnit Unit, IAutoPooledFloatingLODObject floatingLODObject, Color colorIncludingAlpha, 
            bool IsSelectedUnit, bool IsHovered, bool HasUnitActed )
        {
            if ( Unit == null )
                return;
            if ( floatingLODObject == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: floatingObject null", Verbosity.DoNotShow );
                return;
            }
            VisLODDrawingObject drawingData = floatingLODObject.Object;
            if ( drawingData == null )
                return;

            MachineUnitType unitType = Unit.UnitType;
            if ( unitType == null )
                return;

            Vector3 pos = floatingLODObject.WorldLocation;

            float distSquared = (CameraCurrent.CameraBodyPosition - pos ).sqrMagnitude;
            int lodToDraw = -1;
            List<float> lodDistanceSquares = drawingData.GetEffectiveDistanceSquares();
            for ( int i = 0; i < lodDistanceSquares.Count; i++ )
            {
                if ( distSquared <= lodDistanceSquares[i] )
                {
                    lodToDraw = i;
                    break;
                }
            }

            if ( IsSelectedUnit )
            {
                FrameBufferManagerData.SelectedLODDistance.Construction = Mathf.Sqrt( distSquared );
                
                if ( lodToDraw < 0 || lodToDraw >= drawingData.LODRenderGroups.Count )
                    FrameBufferManagerData.SelectedLOD.Construction = -1;
                else
                    FrameBufferManagerData.SelectedLOD.Construction = lodToDraw;
            }

            if ( lodToDraw < 0 || lodToDraw >= drawingData.LODRenderGroups.Count )
            {
                FrameBufferManagerData.LODCullCount.Construction++;
                return; //out of range, too far to bother drawing.  This is a perfectly valid case!
            }
            if ( VisCurrent.GetShouldBeBlurred() )
                lodToDraw = drawingData.LODRenderGroups.Count - 1; //if the world is blurred, then draw max lod

            float halfHeight = Unit.GetHalfHeightForCollisions();
            Vector3 center = Unit.GetPositionForCollisions();
            float radius = Unit.GetRadiusForCollisions();

            if ( Engine_HotM.SelectedActor != Unit && !CameraCurrent.TestFrustumColliderInternalFast( center, halfHeight, radius ) )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return;
            }

            IncrementLODCount( lodToDraw );

            A5RendererGroup rendGroup = drawingData.LODRenderGroups[lodToDraw] as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            Quaternion rot = floatingLODObject.Rotation;

            switch ( Unit.Materializing )
            {
                case MaterializeType.Appear:
                    if ( Unit.MaterializingProgress >= 1f )
                        break;
                    {
                        Vector3 scale = floatingLODObject.EffectiveScale;
                        Unit.Materializing.RenderPrep( Unit.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale, 
                            RenderColorStyle.MaterializeAppear, maskOffset );
                    }
                    return;
                case MaterializeType.Reveal:
                    if ( Unit.MaterializingProgress >= 1f )
                        break;
                    {
                        Vector3 scale = floatingLODObject.EffectiveScale;
                        Unit.Materializing.RenderPrep( Unit.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeReveal, maskOffset );
                    }
                    return;
                case MaterializeType.BurnDownLarge:
                case MaterializeType.BurnDownSmall:
                    {
                        Vector3 scale = floatingLODObject.EffectiveScale;
                        Unit.Materializing.RenderPrep( Unit.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeBurnDown, maskOffset );
                    }
                    return;
            }

            if ( colorIncludingAlpha.a <= 0 )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: colorIncludingAlpha <= 0", Verbosity.DoNotShow );
                return;
            }

            bool shouldSkipGlowsAndIndicators = VisCurrent.IsInPhotoMode || VisCurrent.GetShouldBeBlurred();

            RenderColorStyle colorStyle = RenderColorStyle.SelfColor;
            RenderOpacity opacity = RenderOpacity.Normal;

            if ( colorIncludingAlpha.a < 1f )
                opacity = RenderOpacity.Transparent_Sorted;

            if ( IsSelectedUnit && !shouldSkipGlowsAndIndicators )
            {
                if ( unitType.IsConsideredMech )
                {
                    //rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale,
                    //    unitType.IsConsideredMech ? RenderColorStyle.HighlightOutlineLarge : RenderColorStyle.HighlightOutlineSmall );

                    float addedYOffset = 0f;// -Unit.UnitType.VisObjectExtraOffset;
                    float addedXZSize = Unit.UnitType.RadiusForCollisions;

                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionA,
                        MathRefs.SelectedHexA_XZ_Size.FloatMin, MathRefs.SelectedHexA_XZ_AddedSize.FloatMin, MathRefs.SelectedHexA_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexA_XZ_Thickness.FloatMin, MathRefs.SelectedHexA_Y_Offset.FloatMin + addedYOffset );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionB,
                        MathRefs.SelectedHexB_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexB_XZ_AddedSize.FloatMin, MathRefs.SelectedHexB_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexB_XZ_Thickness.FloatMin, MathRefs.SelectedHexB_Y_Offset.FloatMin + addedYOffset );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionC,
                        MathRefs.SelectedHexC_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexC_XZ_AddedSize.FloatMin, MathRefs.SelectedHexC_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexC_XZ_Thickness.FloatMin, MathRefs.SelectedHexC_Y_Offset.FloatMin + addedYOffset );
                }
                else
                {
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionA,
                        MathRefs.SelectedHexA_XZ_Size.FloatMin, MathRefs.SelectedHexA_XZ_AddedSize.FloatMin, MathRefs.SelectedHexA_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexA_XZ_Thickness.FloatMin, MathRefs.SelectedHexA_Y_Offset.FloatMin );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionB,
                        MathRefs.SelectedHexB_XZ_Size.FloatMin, MathRefs.SelectedHexB_XZ_AddedSize.FloatMin, MathRefs.SelectedHexB_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexB_XZ_Thickness.FloatMin, MathRefs.SelectedHexB_Y_Offset.FloatMin );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionC,
                        MathRefs.SelectedHexC_XZ_Size.FloatMin, MathRefs.SelectedHexC_XZ_AddedSize.FloatMin, MathRefs.SelectedHexC_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexC_XZ_Thickness.FloatMin, MathRefs.SelectedHexC_Y_Offset.FloatMin );
                }
            }

            bool isCloaked = Unit.IsCloaked;
            bool isInCover = Unit.IsTakingCover;
            bool isOnGround = Unit.ContainerLocation.Get() == null || Unit.ContainerLocation.Get() is MapOutdoorSpot;
            if ( isCloaked && unitType.UnderlayWhenCloaked != null )
            {
                if ( !Unit.GetIsMovingRightNow() )
                    DrawUnitUnderlay_General( ((IsSelectedUnit && !HasUnitActed) || IsHovered), isOnGround, unitType.UnderlayWhenCloaked, colorIncludingAlpha, 
                        pos.PlusY( unitType.UnderlayExtraOffset ), rot );
            }
            else
            {
                if ( isCloaked || Unit.DrawAsInvisibleEvenThoughNotCloaked )
                {
                    rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.GlassyUncolored );
                }
                else
                {
                    bool isHighVisibility = InputCaching.UseHighVisibilityUnitColors;
                    if ( InputCaching.IsInInspectMode_Any )
                        isHighVisibility = !isHighVisibility;

                    bool skipMore = false;
                    if ( unitType != null && !unitType.IsConsideredMech && !shouldSkipGlowsAndIndicators )
                    {
                        A5RendererGroup rendGroupLow = drawingData.LODRenderGroups.LastOrDefault as A5RendererGroup;
                        if ( rendGroupLow == null )
                            rendGroupLow = rendGroup;

                        if ( isHighVisibility )
                        {
                            skipMore = true;
                            rendGroupLow.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitGoldB );
                        }
                        else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && 
                            !VisCurrent.IsInPhotoMode )
                            rendGroupLow.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitGoldA );
                    }

                    if ( !skipMore )
                    {
                        if ( opacity != RenderOpacity.Normal )
                            rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, floatingLODObject.EffectiveScale, colorStyle, colorIncludingAlpha, opacity, false );
                        else
                            rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, colorStyle );
                    }

                    if ( unitType.IsConsideredMech && Unit.IsTakingCover ) //shields are up
                    {
                        int shieldLOD = drawingData.LODRenderGroups.Count - 1; //if the world is blurred, then draw max lod
                        IncrementLODCount( shieldLOD );

                        A5RendererGroup rendGroupShield = drawingData.LODRenderGroups[shieldLOD] as A5RendererGroup;
                        if ( rendGroupShield != null )
                        {
                            rendGroupShield.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale.ComponentWiseMult( ShieldOverlay_ScaleMultMechs ), 
                                RenderColorStyle.ShieldUncolored );
                        }
                    }
                }

                if ( unitType.IsConsideredAndroid && !Unit.GetIsMovingRightNow() )
                {
                    DrawUnitUnderlay_General( !isCloaked && ((IsSelectedUnit && !HasUnitActed) || IsHovered), isOnGround,
                         isInCover ? unitType.UnderlayInCover : unitType.Underlay, colorIncludingAlpha, pos.PlusY( unitType.UnderlayExtraOffset ), rot );
                }
            }

            if ( InputCaching.Debug_ShowEntityRadii )
            {
                DrawShape_WireCylinder shapeCylinder = new DrawShape_WireCylinder
                {
                    Start = center.PlusY( -halfHeight ),
                    End = center.PlusY( halfHeight ),
                    Radius = radius,
                    Color = ColorMath.LeafGreen,
                    Thickness = 2f
                };
                VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeCylinder );
            }

            if ( InputCaching.Debug_ShowEntitySubCollidables )
            {
                List<SubCollidable> list = Unit.GetSubCollidablesOrNull();
                if ( list != null && list.Count > 0 )
                {
                    Vector3 primeCenter = Unit.GetPositionForCollisions();
                    float rotation = Unit.GetRotationYForCollisions();

                    foreach ( SubCollidable subCollidable in list )
                    {
                        Vector3 subCenter = subCollidable.CalculateSubCenterFrom( primeCenter, rotation );
                        halfHeight = subCollidable.HalfHeight;

                        DrawShape_WireCylinder shapeCylinder = new DrawShape_WireCylinder
                        {
                            Start = subCenter.PlusY( -halfHeight ),
                            End = subCenter.PlusY( halfHeight ),
                            Radius = subCollidable.Radius,
                            Color = ColorMath.AlliesYellow, 
                            Thickness = 2f
                        };
                        VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeCylinder );
                    }
                }
            }

            if ( InputCaching.Debug_ShowEntityShotEmissionPoints )
            {
                ProtectedList<ShotEmissionGroup> list = Unit.GetShotEmissionGroups();
                if ( list != null && list.Count > 0 )
                {
                    Vector3 primeCenter = Unit.GetPositionForCollisions();
                    Quaternion rotation = Quaternion.Euler( 0, Unit.GetRotationYForCollisions(), 0 );

                    foreach ( ShotEmissionGroup group in list )
                    {
                        foreach ( ShotEmissionPoint emiss in group.EmissionPoints )
                        {
                            Vector3 subCenter = emiss.CalculatePointFrom( primeCenter, rotation );

                            DrawShape_WireBoxOriented shapeBox = new DrawShape_WireBoxOriented
                            {
                                Center = subCenter,
                                Size = new Vector3( 0.15f, 0.15f, 0.15f ),
                                Color = group.DebugColor,
                                Rotation = rotation,
                                Thickness = 1f
                            };
                            VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeBox );
                        }

                        if ( Engine_HotM.SelectedActor == Unit )
                        {
                            if ( group.TargetingZones.Count > 0 )
                            {
                                foreach ( ShotEmissionTargetingZone zone in group.TargetingZones )
                                {
                                    Vector3 subBase = zone.CalculateSubBaseFrom( primeCenter, rotation );

                                    DrawShape_WireCylinder shapeCylinder = new DrawShape_WireCylinder
                                    {
                                        Start = subBase,
                                        End = subBase.PlusY( zone.Height ),
                                        Radius = zone.Radius,
                                        Color = group.DebugColor,
                                        Thickness = 1f
                                    };
                                    VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeCylinder );
                                }
                            }
                        }
                    }
                }
            }

            //if ( UnitType.IfMechShouldDestroyIntersectingBuildings && CollidableIfNeeded is ISimMachineUnit unit )
            //{
            //    staticWorkingBuildings.Clear();
            //    CityMap.FillListOfBuildingsIntersectingCollidable( unit, unit.GetDrawLocation(),
            //        unit.GetRotationYForCollisions(), staticWorkingBuildings );
            //}
        }
        #endregion

        #region IncrementLODCount
        private static void IncrementLODCount( int lodToDraw )
        {
            switch ( lodToDraw )
            {
                case 0:
                    FrameBufferManagerData.LOD0Count.Construction++;
                    break;
                case 1:
                    FrameBufferManagerData.LOD1Count.Construction++;
                    break;
                case 2:
                    FrameBufferManagerData.LOD2Count.Construction++;
                    break;
                case 3:
                    FrameBufferManagerData.LOD3Count.Construction++;
                    break;
                case 4:
                default:
                    FrameBufferManagerData.LOD4Count.Construction++;
                    break;
            }
        }
        #endregion

        internal static readonly Vector3 ShieldOverlay_ScaleMultVehicles = new Vector3( 1.01f, 1.01f, 1.01f );
        internal static readonly Vector3 ShieldOverlay_ScaleMultMechs = new Vector3( 1.03f, 1.03f, 1.03f );

        private static void DrawUnitUnderlay_General( bool IsSelectedOrHovered, bool IsOnGround, UnitUnderlayType Underlay, Color colorIncludingAlpha, Vector3 pos, Quaternion rot )
        {
            if ( Underlay == null )
            {
                return;
            }
            if ( VisCurrent.GetShouldBeBlurred() )
                return; //if the world is blurred, then don't draw this at all
            if ( colorIncludingAlpha.a < 1f )
                return; //if not faded in yet, skip also

            VisAnimatedCluster cluster = IsOnGround ? Underlay.GroundCluster : Underlay.BuildingCluster;

            if ( cluster == null )
                return;

            cluster.MarkAsBeingDrawn();
            foreach ( VisAnimatedObject obj in cluster.AnimatedObjects )
            {
                if ( !obj.IsDrawn )
                    continue;
                obj.ObjectToDraw.RendererGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos + obj.CurrentOffset,
                    obj.CurrentRotation, obj.CurrentScale, RenderColorStyle.NoColor );
            }
        }

        #region DrawNPCUnit_LOD
        internal static void DrawNPCUnit_LOD( ISimNPCUnit Unit, IAutoPooledFloatingLODObject floatingLODObject, Color colorIncludingAlpha,
            bool IsSelectedUnit, bool IsHovered, bool ShouldDrawAsSilhouette )
        {
            if ( floatingLODObject == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: floatingObject null", Verbosity.DoNotShow );
                return;
            }
            VisLODDrawingObject drawingData = floatingLODObject.Object;
            if ( drawingData == null )
                return;

            Vector3 pos = floatingLODObject.WorldLocation;
            if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                pos.y += (Unit?.UnitType?.VisObjectExtraOffsetOnCityMap ?? 0f);

            float distSquared = (CameraCurrent.CameraBodyPosition - pos).sqrMagnitude;
            int lodToDraw = -1;
            List<float> lodDistanceSquares = drawingData.GetEffectiveDistanceSquares();
            for ( int i = 0; i < lodDistanceSquares.Count; i++ )
            {
                if ( distSquared <= lodDistanceSquares[i] )
                {
                    lodToDraw = i;
                    break;
                }
            }

            if ( IsSelectedUnit )
            {
                FrameBufferManagerData.SelectedLODDistance.Construction = Mathf.Sqrt( distSquared );

                if ( lodToDraw < 0 || lodToDraw >= drawingData.LODRenderGroups.Count )
                    FrameBufferManagerData.SelectedLOD.Construction = -1;
                else
                    FrameBufferManagerData.SelectedLOD.Construction = lodToDraw;
            }

            if ( lodToDraw < 0 || lodToDraw >= drawingData.LODRenderGroups.Count )
            {
                FrameBufferManagerData.LODCullCount.Construction++;
                return; //out of range, too far to bother drawing.  This is a perfectly valid case!
            }
            if ( VisCurrent.GetShouldBeBlurred() )
                lodToDraw = drawingData.LODRenderGroups.Count - 1; //if the world is blurred, then draw max lod

            float halfHeight = Unit.GetHalfHeightForCollisions();
            Vector3 center = Unit.GetPositionForCollisions();
            float radius = Unit.GetRadiusForCollisions();

            if ( Engine_HotM.SelectedActor != Unit && !CameraCurrent.TestFrustumColliderInternalFast( center, halfHeight, radius ) )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return;
            }

            IncrementLODCount( lodToDraw );

            A5RendererGroup rendGroup = drawingData.LODRenderGroups[lodToDraw] as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            Quaternion rot = floatingLODObject.Rotation;

            switch ( Unit.Materializing )
            {
                case MaterializeType.Appear:
                    if ( Unit.MaterializingProgress >= 1f )
                        break;
                    {
                        Vector3 scale = floatingLODObject.EffectiveScale;
                        Unit.Materializing.RenderPrep( Unit.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        drawingData.PrepLocation( ref pos, floatingLODObject );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeAppear, maskOffset );
                    }
                    return;
                case MaterializeType.Reveal:
                    if ( Unit.MaterializingProgress >= 1f )
                        break;
                    {
                        Vector3 scale = floatingLODObject.EffectiveScale;
                        Unit.Materializing.RenderPrep( Unit.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        drawingData.PrepLocation( ref pos, floatingLODObject );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeReveal, maskOffset );
                    }
                    return;
                case MaterializeType.BurnDownLarge:
                case MaterializeType.BurnDownSmall:
                    {
                        Vector3 scale = floatingLODObject.EffectiveScale;
                        Unit.Materializing.RenderPrep( Unit.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        drawingData.PrepLocation( ref pos, floatingLODObject );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeBurnDown, maskOffset );
                    }
                    return;
            }

            if ( colorIncludingAlpha.a <= 0 )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: colorIncludingAlpha <= 0", Verbosity.DoNotShow );
                return;
            }

            bool shouldSkipGlowsAndIndicators = VisCurrent.IsInPhotoMode || VisCurrent.GetShouldBeBlurred();

            RenderColorStyle colorStyle = RenderColorStyle.SelfColor;
            RenderOpacity opacity = RenderOpacity.Normal;

            if ( colorIncludingAlpha.a < 1f )
                opacity = RenderOpacity.Transparent_Sorted;
            if ( ShouldDrawAsSilhouette )
                colorStyle = RenderColorStyle.FogOfWar;

            Vector3 basePos = pos;
            drawingData.PrepLocation( ref pos, floatingLODObject );

            NPCUnitType unitType = Unit.UnitType;
            NPCUnitStance stance = Unit.Stance;

            bool skipMore = false;
            if ( unitType != null && !unitType.IsMechStyleMovement && !unitType.IsVehicle && !shouldSkipGlowsAndIndicators )
            {
                A5RendererGroup rendGroupLow = drawingData.LODRenderGroups.LastOrDefault as A5RendererGroup;
                if ( rendGroupLow == null )
                    rendGroupLow = rendGroup;

                bool isHighVisibility = InputCaching.UseHighVisibilityUnitColors;
                if ( InputCaching.IsInInspectMode_Any )
                    isHighVisibility = !isHighVisibility;
                if ( VisCurrent.IsInPhotoMode )
                    isHighVisibility = false;

                if ( isHighVisibility )
                    skipMore = true;

                if ( Unit.GetIsPartOfPlayerForcesInAnyWay() )
                {
                    if ( isHighVisibility )
                        rendGroupLow.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitGoldB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroupLow.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitGoldA );
                }
                else if ( stance != null && stance.ShouldUseBlueOutline )
                {
                    if ( isHighVisibility )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitBlueB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitBlueA );
                }
                else if ( stance != null && stance.ShouldUsePurpleOutline )
                {
                    if ( isHighVisibility )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitPurpleB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitPurpleA );
                }
                else if ( Unit.GetWillFireOnMachineUnitsBaseline() )
                {
                    if ( isHighVisibility )
                        rendGroupLow.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitRedB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroupLow.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitRedA );
                }
                else
                {
                    if ( isHighVisibility )
                        rendGroupLow.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitBlueB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroupLow.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, RenderColorStyle.OutlineUnitBlueA );
                }
            }

            if ( IsSelectedUnit && !shouldSkipGlowsAndIndicators )
            {
                //A5RendererGroup rendGroupLow = drawingData.LODRenderGroups.LastOrDefault as A5RendererGroup;
                //if ( rendGroupLow == null )
                //    rendGroupLow = rendGroup;

                if ( unitType.IsMechStyleMovement || unitType.IsVehicle )
                {
                    float addedYOffset = -Unit.UnitType.VisObjectExtraOffset + (unitType.IsMechStyleMovement ? 0.1f : 0);
                    float addedXZSize = Unit.UnitType.RadiusForCollisions;

                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionA,
                        MathRefs.SelectedHexA_XZ_Size.FloatMin, MathRefs.SelectedHexA_XZ_AddedSize.FloatMin, MathRefs.SelectedHexA_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexA_XZ_Thickness.FloatMin, MathRefs.SelectedHexA_Y_Offset.FloatMin + addedYOffset );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionB,
                        MathRefs.SelectedHexB_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexB_XZ_AddedSize.FloatMin, MathRefs.SelectedHexB_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexB_XZ_Thickness.FloatMin, MathRefs.SelectedHexB_Y_Offset.FloatMin + addedYOffset );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionC,
                        MathRefs.SelectedHexC_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexC_XZ_AddedSize.FloatMin, MathRefs.SelectedHexC_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexC_XZ_Thickness.FloatMin, MathRefs.SelectedHexC_Y_Offset.FloatMin + addedYOffset );

                    //rendGroupLow.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale,
                    //    unitType.IsMechStyleMovement || unitType.IsVehicle ? RenderColorStyle.HighlightOutlineLarge : RenderColorStyle.HighlightOutlineSmall );
                }
                else
                {
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionA,
                        MathRefs.SelectedHexA_XZ_Size.FloatMin, MathRefs.SelectedHexA_XZ_AddedSize.FloatMin, MathRefs.SelectedHexA_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexA_XZ_Thickness.FloatMin, MathRefs.SelectedHexA_Y_Offset.FloatMin );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionB,
                        MathRefs.SelectedHexB_XZ_Size.FloatMin, MathRefs.SelectedHexB_XZ_AddedSize.FloatMin, MathRefs.SelectedHexB_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexB_XZ_Thickness.FloatMin, MathRefs.SelectedHexB_Y_Offset.FloatMin );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionC,
                        MathRefs.SelectedHexC_XZ_Size.FloatMin, MathRefs.SelectedHexC_XZ_AddedSize.FloatMin, MathRefs.SelectedHexC_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexC_XZ_Thickness.FloatMin, MathRefs.SelectedHexC_Y_Offset.FloatMin );
                }
            }

            if ( !skipMore )
            {
                if ( opacity != RenderOpacity.Normal )
                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, floatingLODObject.EffectiveScale, colorStyle, colorIncludingAlpha, opacity, false );
                else
                    rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingLODObject.EffectiveScale, colorStyle );
            }

            DrawNPCUnit_PostElements( Unit, IsSelectedUnit, IsHovered, ShouldDrawAsSilhouette, colorIncludingAlpha, basePos, rot, 
                floatingLODObject.EffectiveScale, distSquared, rendGroup, center, radius, halfHeight );
        }
        #endregion

        #region DrawNPCUnit_Simple
        private const float MAX_SIMPLE_DRAW_DIST_MAP = 3000 * 3000;
        private const float MAX_SIMPLE_DRAW_DIST_STREETS = 160 * 160;
        internal static void DrawNPCUnit_Simple( ISimNPCUnit Unit, IAutoPooledFloatingObject floatingSimpleObject, Color colorIncludingAlpha,
            bool IsSelectedUnit, bool IsHovered, bool ShouldDrawAsSilhouette )
        {
            if ( floatingSimpleObject == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: floatingObject null", Verbosity.DoNotShow );
                return;
            }
            VisSimpleDrawingObject drawingData = floatingSimpleObject.EffectiveObject;
            if ( drawingData == null )
                return;

            Vector3 pos = floatingSimpleObject.WorldLocation;
            if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                pos.y += (Unit?.UnitType?.VisObjectExtraOffsetOnCityMap ?? 0f);

            bool isSelected = Engine_HotM.SelectedActor == Unit;

            float distSquared = (CameraCurrent.CameraBodyPosition - pos).sqrMagnitude;
            if ( !isSelected )
            {
                if ( distSquared > (Engine_HotM.GameMode == MainGameMode.CityMap ? MAX_SIMPLE_DRAW_DIST_MAP : MAX_SIMPLE_DRAW_DIST_STREETS) )
                {
                    FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                    return; //out of range, too far to bother drawing.  This is a perfectly valid case!
                }
            }

            float halfHeight = Unit.GetHalfHeightForCollisions();
            Vector3 center = Unit.GetPositionForCollisions();
            float radius = Unit.GetRadiusForCollisions();

            if ( !isSelected && !CameraCurrent.TestFrustumColliderInternalFast( center, halfHeight, radius ) )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return;
            }

            FrameBufferManagerData.LOD0Count.Construction++;

            A5RendererGroup rendGroup = drawingData.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            if ( colorIncludingAlpha.a <= 0 )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: colorIncludingAlpha <= 0", Verbosity.DoNotShow );
                return;
            }

            Quaternion rot = floatingSimpleObject.Rotation;

            switch ( Unit.Materializing )
            {
                case MaterializeType.Appear:
                    if ( Unit.MaterializingProgress >= 1f )
                        break;
                    {
                        Vector3 scale = floatingSimpleObject.EffectiveScale;
                        Unit.Materializing.RenderPrep( Unit.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        drawingData.PrepLocation( ref pos, floatingSimpleObject );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeAppear, maskOffset );
                    }
                    return;
                case MaterializeType.Reveal:
                    if ( Unit.MaterializingProgress >= 1f )
                        break;
                    {
                        Vector3 scale = floatingSimpleObject.EffectiveScale;
                        Unit.Materializing.RenderPrep( Unit.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        drawingData.PrepLocation( ref pos, floatingSimpleObject );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeReveal, maskOffset );
                    }
                    return;
                case MaterializeType.BurnDownLarge:
                case MaterializeType.BurnDownSmall:
                    {
                        Vector3 scale = floatingSimpleObject.EffectiveScale;
                        Unit.Materializing.RenderPrep( Unit.MaterializingProgress, 12f,
                            ref pos, ref scale, out float maskOffset );
                        drawingData.PrepLocation( ref pos, floatingSimpleObject );
                        rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( pos, rot, scale,
                            RenderColorStyle.MaterializeBurnDown, maskOffset );
                    }
                    return;
            }

            bool shouldSkipGlowsAndIndicators = VisCurrent.IsInPhotoMode || VisCurrent.GetShouldBeBlurred();

            RenderColorStyle colorStyle = RenderColorStyle.SelfColor;
            RenderOpacity opacity = RenderOpacity.Normal;

            if ( colorIncludingAlpha.a < 1f )
                opacity = RenderOpacity.Transparent_Sorted;
            if ( ShouldDrawAsSilhouette )
                colorStyle = RenderColorStyle.FogOfWar;

            Vector3 basePos = pos;
            drawingData.PrepLocation( ref pos, floatingSimpleObject );

            NPCUnitType unitType = Unit.UnitType;
            NPCUnitStance stance = Unit.Stance;

            bool skipMore = false;
            if ( unitType != null && !unitType.IsMechStyleMovement && !unitType.IsVehicle && !shouldSkipGlowsAndIndicators )
            {
                bool isHighVisibility = InputCaching.UseHighVisibilityUnitColors;
                if ( InputCaching.IsInInspectMode_Any )
                    isHighVisibility = !isHighVisibility;

                if ( isHighVisibility )
                    skipMore = true;

                if ( Unit.GetIsPartOfPlayerForcesInAnyWay() )
                {
                    if ( isHighVisibility )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitGoldB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitGoldA );
                }
                else if ( stance != null && stance.ShouldUseBlueOutline )
                {
                    if ( isHighVisibility )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitBlueB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitBlueA );
                }
                else if ( stance != null && stance.ShouldUsePurpleOutline )
                {
                    if ( isHighVisibility )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitPurpleB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitPurpleA );
                }
                else if ( Unit.GetWillFireOnMachineUnitsBaseline() )
                {
                    if ( isHighVisibility )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitRedB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitRedA );
                }
                else
                {
                    if ( isHighVisibility )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitBlueB );
                    else if ( distSquared > MIN_DISTANCE_SQUARED_BEHIND && distSquared < InputCaching.MaxDistanceToShowUnitOutlinesBehindObjects_Squared && !VisCurrent.IsInPhotoMode )
                        rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale, RenderColorStyle.OutlineUnitBlueA );
                }
            }


            if ( IsSelectedUnit && !shouldSkipGlowsAndIndicators )
            {
                if ( unitType.IsMechStyleMovement || unitType.IsVehicle )
                {
                    float addedYOffset = -Unit.UnitType.VisObjectExtraOffset + (unitType.IsMechStyleMovement ? 0.1f : 0 );
                    float addedXZSize = Unit.UnitType.RadiusForCollisions;

                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionA,
                        MathRefs.SelectedHexA_XZ_Size.FloatMin, MathRefs.SelectedHexA_XZ_AddedSize.FloatMin, MathRefs.SelectedHexA_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexA_XZ_Thickness.FloatMin, MathRefs.SelectedHexA_Y_Offset.FloatMin + addedYOffset );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionB,
                        MathRefs.SelectedHexB_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexB_XZ_AddedSize.FloatMin, MathRefs.SelectedHexB_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexB_XZ_Thickness.FloatMin, MathRefs.SelectedHexB_Y_Offset.FloatMin + addedYOffset );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionC,
                        MathRefs.SelectedHexC_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexC_XZ_AddedSize.FloatMin, MathRefs.SelectedHexC_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexC_XZ_Thickness.FloatMin, MathRefs.SelectedHexC_Y_Offset.FloatMin + addedYOffset );

                    //rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, floatingSimpleObject.EffectiveScale,
                    //    unitType.IsMechStyleMovement || unitType.IsVehicle ? RenderColorStyle.HighlightOutlineLarge : RenderColorStyle.HighlightOutlineSmall );
                }
                else
                {
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionA,
                        MathRefs.SelectedHexA_XZ_Size.FloatMin, MathRefs.SelectedHexA_XZ_AddedSize.FloatMin, MathRefs.SelectedHexA_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexA_XZ_Thickness.FloatMin, MathRefs.SelectedHexA_Y_Offset.FloatMin );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionB,
                        MathRefs.SelectedHexB_XZ_Size.FloatMin, MathRefs.SelectedHexB_XZ_AddedSize.FloatMin, MathRefs.SelectedHexB_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexB_XZ_Thickness.FloatMin, MathRefs.SelectedHexB_Y_Offset.FloatMin );
                    SharedRenderManagerData.DrawSelectionHexAroundActor( Unit, ColorRefs.UnitNewStyleSelectionC,
                        MathRefs.SelectedHexC_XZ_Size.FloatMin, MathRefs.SelectedHexC_XZ_AddedSize.FloatMin, MathRefs.SelectedHexC_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexC_XZ_Thickness.FloatMin, MathRefs.SelectedHexC_Y_Offset.FloatMin );
                }
            }

            if ( !skipMore )
                rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, floatingSimpleObject.EffectiveScale, colorStyle, colorIncludingAlpha, opacity, false );

            DrawNPCUnit_PostElements( Unit, IsSelectedUnit, IsHovered, ShouldDrawAsSilhouette, colorIncludingAlpha, basePos, rot, 
                floatingSimpleObject.EffectiveScale, distSquared, rendGroup, center, radius, halfHeight );
        }
        #endregion

        #region DrawNPCUnit_PostElements
        private static void DrawNPCUnit_PostElements( ISimNPCUnit Unit, bool IsSelectedUnit, bool IsHovered, bool ShouldDrawAsSilhouette,
            Color colorIncludingAlpha, Vector3 pos, Quaternion rot, Vector3 scale, float distSquared, A5RendererGroup rendGroup,
            Vector3 center, float radius, float halfHeight )
        {
            if ( !Unit.UnitType.IsMechStyleMovement && !Unit.UnitType.IsVehicle && !ShouldDrawAsSilhouette && !Unit.GetIsMovingRightNow() )
            {
                bool isOnGround = Unit.ContainerLocation.Get() == null || Unit.ContainerLocation.Get() is MapOutdoorSpot;

                DrawUnitUnderlay_General( IsHovered || IsSelectedUnit, isOnGround,
                    Unit.UnitType.Underlay, colorIncludingAlpha, pos.PlusY( Unit.UnitType.UnderlayExtraOffset ), rot );
            }

            if ( (IsHovered || IsSelectedUnit) && !ShouldDrawAsSilhouette && !Unit.UnitType.SkipShowingGhostOfPreviousLocation )
            {
                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = pos.ReplaceY( groundLevel );
                float attackRange = Unit.GetAttackRange();
                DrawHelper.RenderRangeCircle( groundCenter.PlusY( -0.08f ), attackRange, ColorRefs.UnitAttackRangeBorder.ColorHDR, 2f );
                float moveRange = Unit.GetMovementRange();
                DrawHelper.RenderRangeCircle( groundCenter.PlusY( -0.08f ), moveRange, ColorRefs.UnitMoveRangeBorder.ColorHDR, 2f );

                Vector3 priorLoc = Unit.PriorLocation;
                if ( priorLoc.x > float.NegativeInfinity )
                {
                    if ( Unit.UnitType.EntireObjectAlwaysThisHeightAboveGround > 0 )
                        priorLoc = priorLoc.ReplaceY( Unit.UnitType.EntireObjectAlwaysThisHeightAboveGround );

                    Vector3 newLoc = pos;
                    if ( Unit.UnitType.IsVehicle )
                    {
                        newLoc = newLoc.PlusY( Unit.UnitType.HalfHeightForCollisions );

                        //draw the line from the old spot to the new
                        DrawHelper.RenderLine( priorLoc.PlusY( Unit.UnitType.HalfHeightForCollisions ), newLoc,
                            ColorRefs.PriorNPCUnitMoveLine.ColorHDR, 1.5f );
                    }
                    else if ( Unit.UnitType.IsMechStyleMovement )
                    {
                        //draw the line from the old spot to the new
                        DrawHelper.RenderLine( priorLoc.PlusY( Unit.UnitType.HalfHeightForCollisions ), newLoc,
                            ColorRefs.PriorNPCUnitMoveLine.ColorHDR, 1.5f );
                    }
                    else
                    {
                        //draw the line from the old spot to the new
                        DrawHelper.RenderCatmullLine( priorLoc.PlusY( Unit.UnitType.HalfHeightForCollisions ), newLoc,
                            ColorRefs.PriorNPCUnitMoveLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );
                    }

                    //draw the ghost of the unit where it was
                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( priorLoc.PlusY( Unit.UnitType.VisObjectExtraOffset ), Unit.PriorRotation, scale,
                        RenderColorStyle.HighlightColor, ColorRefs.PriorNPCUnitMoveGhost.ColorHDR,
                        RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
                }

                if ( InputCaching.Debug_ShowNPCGoals )
                {
                    ISimBuilding objectiveBuild = Unit.ObjectiveBuilding.Get();
                    if ( objectiveBuild != null )
                        DrawHelper.RenderCatmullLine( pos, objectiveBuild.GetMapItem().OBBCache.BottomCenter,
                            ColorRefs.NPCUnitGoalLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );
                    else if ( Unit.ObjectiveActor != null )
                        DrawHelper.RenderCatmullLine( pos, Unit.ObjectiveActor.GetDrawLocation(),
                            ColorRefs.NPCUnitGoalLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );
                }
            }

            if ( InputCaching.Debug_ShowEntityRadii && Unit != null )
            {
                DrawShape_WireCylinder shapeCylinder = new DrawShape_WireCylinder
                {
                    Start = center.PlusY( -halfHeight ),
                    End = center.PlusY( halfHeight ),
                    Radius = radius,
                    Color = ColorMath.LeafGreen,
                    Thickness = 2f
                };
                VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeCylinder );
            }

            if ( InputCaching.Debug_ShowEntitySubCollidables && Unit != null )
            {
                List<SubCollidable> list = Unit.GetSubCollidablesOrNull();
                if ( list != null && list.Count > 0 )
                {
                    Vector3 primeCenter = Unit.GetPositionForCollisions();
                    float rotation = Unit.GetRotationYForCollisions();

                    foreach ( SubCollidable subCollidable in list )
                    {
                        Vector3 subCenter = subCollidable.CalculateSubCenterFrom( primeCenter, rotation );
                        halfHeight = subCollidable.HalfHeight;

                        DrawShape_WireCylinder shapeCylinder = new DrawShape_WireCylinder
                        {
                            Start = subCenter.PlusY( -halfHeight ),
                            End = subCenter.PlusY( halfHeight ),
                            Radius = subCollidable.Radius,
                            Color = ColorMath.AlliesYellow,
                            Thickness = 2f
                        };
                        VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeCylinder );
                    }
                }
            }

            if ( InputCaching.Debug_ShowEntityShotEmissionPoints && Unit != null )
            {
                ProtectedList<ShotEmissionGroup> list = Unit.GetShotEmissionGroups();
                if ( list != null && list.Count > 0 )
                {
                    Vector3 primeCenter = Unit.GetPositionForCollisions();
                    Quaternion rotation = Quaternion.Euler( 0, Unit.GetRotationYForCollisions(), 0 );

                    foreach ( ShotEmissionGroup group in list )
                    {
                        foreach ( ShotEmissionPoint emiss in group.EmissionPoints )
                        {
                            Vector3 subCenter = emiss.CalculatePointFrom( primeCenter, rotation );

                            DrawShape_WireBoxOriented shapeBox = new DrawShape_WireBoxOriented
                            {
                                Center = subCenter,
                                Size = new Vector3( 0.15f, 0.15f, 0.15f ),
                                Color = group.DebugColor,
                                Rotation = rotation,
                                Thickness = 1f
                            };
                            VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeBox );
                        }

                        if ( Engine_HotM.SelectedActor == Unit )
                        {
                            if ( group.TargetingZones.Count > 0 )
                            {
                                foreach ( ShotEmissionTargetingZone zone in group.TargetingZones )
                                {
                                    Vector3 subBase = zone.CalculateSubBaseFrom( primeCenter, rotation );

                                    DrawShape_WireCylinder shapeCylinder = new DrawShape_WireCylinder
                                    {
                                        Start = subBase,
                                        End = subBase.PlusY( zone.Height ),
                                        Radius = zone.Radius,
                                        Color = group.DebugColor,
                                        Thickness = 1f
                                    };
                                    VisRangeAndSimilarDrawing.Instance.Shapes.Add( shapeCylinder );
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region GhostHovered
        public static void DrawNPCUnit_GhostHovered( ISimNPCUnit Unit, Color colorIncludingAlpha )
        {
            if ( Unit.GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject floatingLODObject,
                out IAutoPooledFloatingObject floatingSimpleObject, out _ ) )
            {
                if ( floatingLODObject != null )
                    DrawFloatingObject_LOD_GhostHovered( floatingLODObject, colorIncludingAlpha );
                else
                    DrawFloatingObject_Simple_GhostHovered( floatingSimpleObject, colorIncludingAlpha );
            }
        }

        public static void DrawMachineUnit_GhostHovered( ISimMachineUnit Unit, Color colorIncludingAlpha )
        {
            if ( Unit.GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject floatingLODObject, out _ ) )
            {
                DrawFloatingObject_LOD_GhostHovered( floatingLODObject, colorIncludingAlpha );
            }
        }

        public static void DrawMachineVehicle_GhostHovered( ISimMachineVehicle Vehicle, Color colorIncludingAlpha )
        {
            if ( Vehicle.GetDataForActualObjectDraw( out IAutoPooledFloatingObject floatingSimpleObject, out _ ) )
            {
                DrawFloatingObject_Simple_GhostHovered( floatingSimpleObject, colorIncludingAlpha );
            }
        }

        private static void DrawFloatingObject_Simple_GhostHovered( IAutoPooledFloatingObject floatingSimpleObject, Color colorIncludingAlpha )
        {
            if ( floatingSimpleObject == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: floatingObject null", Verbosity.DoNotShow );
                return;
            }
            VisSimpleDrawingObject drawingData = floatingSimpleObject.EffectiveObject;
            if ( drawingData == null )
                return;

            Vector3 pos = floatingSimpleObject.WorldLocation;

            FrameBufferManagerData.LOD0Count.Construction++;

            A5RendererGroup rendGroup = drawingData.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            if ( colorIncludingAlpha.a <= 0 )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: colorIncludingAlpha <= 0", Verbosity.DoNotShow );
                return;
            }

            Quaternion rot = floatingSimpleObject.Rotation;

            RenderColorStyle colorStyle = RenderColorStyle.HighlightGhost;

            rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, floatingSimpleObject.EffectiveScale.ComponentWiseMult( SharedRenderManagerData.highlight_ScaleMult ), 
                colorStyle, colorIncludingAlpha, RenderOpacity.Normal, false );
        }

        private static void DrawFloatingObject_LOD_GhostHovered( IAutoPooledFloatingLODObject floatingLODObject, Color colorIncludingAlpha )
        {
            if ( floatingLODObject == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: floatingObject null", Verbosity.DoNotShow );
                return;
            }
            VisLODDrawingObject drawingData = floatingLODObject.Object;
            if ( drawingData == null )
                return;

            Vector3 pos = floatingLODObject.WorldLocation;
            int lodToDraw = drawingData.LODRenderGroups.Count - 1; //always draw max lod

            IncrementLODCount( lodToDraw );

            A5RendererGroup rendGroup = drawingData.LODRenderGroups[lodToDraw] as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            Quaternion rot = floatingLODObject.Rotation;

            RenderColorStyle colorStyle = RenderColorStyle.HighlightGhost;

            rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, floatingLODObject.EffectiveScale.ComponentWiseMult( SharedRenderManagerData.highlight_ScaleMult ), 
                colorStyle, colorIncludingAlpha, RenderOpacity.Normal, false );
        }
        #endregion

        #region HighlightColor
        public static void DrawNPCUnit_HighlightColor( ISimNPCUnit Unit, Color colorIncludingAlpha )
        {
            if ( Unit.GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject floatingLODObject,
                out IAutoPooledFloatingObject floatingSimpleObject, out _ ) )
            {
                if ( floatingLODObject != null )
                    DrawFloatingObject_LOD_HighlightColor( floatingLODObject, colorIncludingAlpha );
                else
                    DrawFloatingObject_Simple_HighlightColor( floatingSimpleObject, colorIncludingAlpha );
            }
        }

        public static void DrawMachineUnit_HighlightColor( ISimMachineUnit Unit, Color colorIncludingAlpha )
        {
            if ( Unit.GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject floatingLODObject, out _ ) )
            {
                DrawFloatingObject_LOD_HighlightColor( floatingLODObject, colorIncludingAlpha );
            }
        }

        public static void DrawMachineVehicle_HighlightColor( ISimMachineVehicle Vehicle, Color colorIncludingAlpha )
        {
            if ( Vehicle.GetDataForActualObjectDraw( out IAutoPooledFloatingObject floatingSimpleObject, out _ ) )
            {
                DrawFloatingObject_Simple_HighlightColor( floatingSimpleObject, colorIncludingAlpha );
            }
        }

        private static void DrawFloatingObject_Simple_HighlightColor( IAutoPooledFloatingObject floatingSimpleObject, Color colorIncludingAlpha )
        {
            if ( floatingSimpleObject == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: floatingObject null", Verbosity.DoNotShow );
                return;
            }
            VisSimpleDrawingObject drawingData = floatingSimpleObject.EffectiveObject;
            if ( drawingData == null )
                return;

            Vector3 pos = floatingSimpleObject.WorldLocation;

            FrameBufferManagerData.LOD0Count.Construction++;

            A5RendererGroup rendGroup = drawingData.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            if ( colorIncludingAlpha.a <= 0 )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: colorIncludingAlpha <= 0", Verbosity.DoNotShow );
                return;
            }

            Quaternion rot = floatingSimpleObject.Rotation;

            RenderColorStyle colorStyle = RenderColorStyle.HighlightColor;

            rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, floatingSimpleObject.EffectiveScale.ComponentWiseMult( SharedRenderManagerData.highlight_ScaleMult ),
                colorStyle, colorIncludingAlpha, RenderOpacity.Normal, false );
        }

        private static void DrawFloatingObject_LOD_HighlightColor( IAutoPooledFloatingLODObject floatingLODObject, Color colorIncludingAlpha )
        {
            if ( floatingLODObject == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: floatingObject null", Verbosity.DoNotShow );
                return;
            }
            VisLODDrawingObject drawingData = floatingLODObject.Object;
            if ( drawingData == null )
                return;

            Vector3 pos = floatingLODObject.WorldLocation;
            int lodToDraw = drawingData.LODRenderGroups.Count - 1; //always draw max lod

            IncrementLODCount( lodToDraw );

            A5RendererGroup rendGroup = drawingData.LODRenderGroups[lodToDraw] as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            Quaternion rot = floatingLODObject.Rotation;

            RenderColorStyle colorStyle = RenderColorStyle.HighlightColor;

            rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, floatingLODObject.EffectiveScale.ComponentWiseMult( SharedRenderManagerData.highlight_ScaleMult ),
                colorStyle, colorIncludingAlpha, RenderOpacity.Normal, false );
        }
        #endregion

        private static VisSimpleDrawingObject FloorCubeBlue;
        private static VisSimpleDrawingObject FloorCubeCyan;
        private static VisSimpleDrawingObject FloorCubeGreen;
        private static VisSimpleDrawingObject FloorCubeOrange;
        private static VisSimpleDrawingObject FloorCubePink;
        private static VisSimpleDrawingObject FloorCubePurple;
        private static VisSimpleDrawingObject FloorCubeRed;
        private static VisSimpleDrawingObject FloorCubeYellow;

        #region InitFloorObjectsIfNeeded
        private static void InitFloorObjectsIfNeeded()
        {
            if ( FloorCubeBlue != null )
                return;
            FloorCubeBlue = VisSimpleDrawingObjectTable.Instance.GetRowByID( "FloorCubeBlue" );
            FloorCubeCyan = VisSimpleDrawingObjectTable.Instance.GetRowByID( "FloorCubeCyan" );
            FloorCubeGreen = VisSimpleDrawingObjectTable.Instance.GetRowByID( "FloorCubeGreen" );
            FloorCubeOrange = VisSimpleDrawingObjectTable.Instance.GetRowByID( "FloorCubeOrange" );
            FloorCubePink = VisSimpleDrawingObjectTable.Instance.GetRowByID( "FloorCubePink" );
            FloorCubePurple = VisSimpleDrawingObjectTable.Instance.GetRowByID( "FloorCubePurple" );
            FloorCubeRed = VisSimpleDrawingObjectTable.Instance.GetRowByID( "FloorCubeRed" );
            FloorCubeYellow = VisSimpleDrawingObjectTable.Instance.GetRowByID( "FloorCubeYellow" );
        }
        #endregion

        //public static MapItem DrawThisBuildingMachineUnitIsIn = null;
        //public static ISimMachineUnit DrawFloorForThisMachineUnit = null;

        #region DrawBuildingFloorsIfNeeded
        public static void DrawBuildingFloorsIfNeeded()
        {
            //VisCurrent.PerFloorWireShapes.Clear(); this is done on the streets now

            if (InputCaching.Debug_MainGameShowOBBOfSingleObjectUnderCursor )
                DebugDrawOBB();

            //if ( DrawThisBuildingMachineUnitIsIn != null )
            //{
            //    BuildingPrefab building = DrawThisBuildingMachineUnitIsIn.Type.Building;
            //    if ( building != null && building.BuildingFloors.Count > 0 )
            //    {
            //        InitFloorObjectsIfNeeded();
            //        DrawFloorsInner_OneFloorHighlighted( DrawThisBuildingMachineUnitIsIn, building, -2 ); //this was scrapped: floor location
            //    }
            //}

            DrawSelectedBuildingFloorsIfNeeded();
        }
        #endregion

        private static void DrawSelectedBuildingFloorsIfNeeded()
        {
            if ( !(SimCommon.CurrentCityLens?.ShowFloorsInInspectMode?.Display ?? false) )
                return;

            MapItem selectedBuildingItem = VisManagerVeryBase.GetSelectedBuilding();
            if ( selectedBuildingItem != null )
            {
                BuildingPrefab building = selectedBuildingItem.Type.Building;
                if ( Engine_HotM.ShowFloorsOfSelectedBuildings && building != null && building.BuildingFloors.Count > 0 )
                {
                    InitFloorObjectsIfNeeded();
                    DrawFloorsInner_OldStyle( selectedBuildingItem, building );
                }
            }

            MapItem buildingUnderCursor = MouseHelper.BuildingUnderCursor?.GetMapItem();
            if ( InputCaching.IsInInspectMode_Any && buildingUnderCursor != null && buildingUnderCursor != selectedBuildingItem )
            {
                BuildingPrefab building = buildingUnderCursor.Type.Building;
                if ( Engine_HotM.ShowFloorsOfSelectedBuildings && building != null && building.BuildingFloors.Count > 0 )
                {
                    InitFloorObjectsIfNeeded();
                    DrawFloorsInner_OldStyle( buildingUnderCursor, building );
                }
            }
        }

        #region DebugDrawOBB
        private static void DebugDrawOBB()
        {
            A5Placeable mouseOver = Engine_HotM.PlaceableUnderMouse as A5Placeable;
            if ( mouseOver )
            {
                Vector3 center = mouseOver.OBBAndBoundsCache.OBB.Center;
                Quaternion rotation = mouseOver.OBBAndBoundsCache.OBB.Rotation;

                {
                    DrawShape_WireBoxOriented wireBox;
                    wireBox.Center = center;
                    wireBox.Rotation = rotation;
                    wireBox.Size = mouseOver.OBBAndBoundsCache.OBB.Size * 0.99f;
                    wireBox.Color = ColorMath.Pink;
                    wireBox.Thickness = 1.5f;
                    VisCurrent.PerFloorWireShapes.Add( wireBox );
                }

                MapItem item = mouseOver.GetCityMapItem();
                if ( item != null )
                {
                    center = item.OBBCache.Center;
                    rotation = item.OBBCache.GetOBB_ExpensiveToUse().Rotation;

                    DrawShape_WireBoxOriented wireBox;
                    wireBox.Center = center;
                    wireBox.Rotation = rotation;
                    wireBox.Size = item.OBBCache.OBBSize;
                    wireBox.Color = ColorMath.LightGreen;
                    wireBox.Thickness = 1.5f;
                    VisCurrent.PerFloorWireShapes.Add( wireBox );

                    //{
                    //    AABB outerAABB = item.OBBCache.OBB.GetAABBFromCornerPoints();
                    //    Vector3 size = outerAABB.Size;
                    //    size.x += (0.4f + 0.4f);
                    //    size.z += (0.4f + 0.4f);
                    //    outerAABB.Size = size;

                    //    DrawShape_WireBox wireBox2;
                    //    wireBox2.Center = outerAABB.Center;
                    //    wireBox2.Size = outerAABB.Size;
                    //    wireBox2.Color = ColorMath.LighterRed;
                    //    wireBox2.Thickness = 1.5f;
                    //    VisCurrent.PerFloorWireShapes.Add( wireBox2 );
                    //}
                }
            }
            else
            {
                MapItem mapItemOver = MouseHelper.BuildingUnderCursor?.GetMapItem();
                if ( mapItemOver != null)
                {
                    Vector3 center = mapItemOver.OBBCache.Center;
                    Quaternion rotation = mapItemOver.OBBCache.GetOBB_ExpensiveToUse().Rotation;

                    DrawShape_WireBoxOriented wireBox;
                    wireBox.Center = center;
                    wireBox.Rotation = rotation;
                    wireBox.Size = mapItemOver.OBBCache.OBBSize;
                    wireBox.Color = ColorMath.LightBlue;
                    wireBox.Thickness = 1.5f;
                    VisCurrent.PerFloorWireShapes.Add( wireBox );

                    //{
                    //    AABB outerAABB = mapItemOver.OBBCache.OBB.GetAABBFromCornerPoints();
                    //    Vector3 size = outerAABB.Size;
                    //    size.x += (0.4f + 0.4f);
                    //    size.z += (0.4f + 0.4f);
                    //    outerAABB.Size = size;

                    //    DrawShape_WireBox wireBox2;
                    //    wireBox2.Center = outerAABB.Center;
                    //    wireBox2.Size = outerAABB.Size;
                    //    wireBox2.Color = ColorMath.LighterRed;
                    //    wireBox2.Thickness = 1.5f;
                    //    VisCurrent.PerFloorWireShapes.Add( wireBox2 );
                    //}
                }
            }
        }
        #endregion

        #region DrawFloorsInner_OneFloorHighlighted
        //private static void DrawFloorsInner_OneFloorHighlighted( MapItem mapItem, BuildingPrefab buildingPref, int HighlightedFloor )
        //{
        //    Vector3 center = mapItem.OBBCache.Center;
        //    center += buildingPref.FloorsOffset;
        //    float yBot = mapItem.Type.AlwaysDropTo >= -900 ? 0 : mapItem.OBBCache.BottomCenter.y;
        //    Quaternion rotation = mapItem.OBBCache.GetOBB_ExpensiveToUse().Rotation;

        //    float yPos = yBot;

        //    yPos = yBot;
        //    for ( int floorIndex = -1; floorIndex >= buildingPref.MinFloor; floorIndex-- )
        //    {
        //        BuildingFloor floor = buildingPref.BuildingFloors[floorIndex];

        //        yPos -= floor.FloorSize.y / 2f;

        //        Vector3 scale = floor.FloorSize.ComponentWiseMult( new Vector3( 1, 0.85f, 1f ) );
        //        Vector3 pos = new Vector3( center.x, yPos, center.z );

        //        yPos -= floor.FloorSize.y / 2f;

        //        if ( floorIndex == HighlightedFloor )
        //        {
        //            DrawFloorCube( FloorCubePurple, floor, pos, rotation );
        //        }
        //        else
        //        {
        //            DrawFloorCube( FloorCubeRed, floor, pos, rotation );
        //        }
        //    }

        //    //yPos = yBot;
        //    //for ( int floorIndex = 0; floorIndex <= buildingPref.MaxFloor; floorIndex++ )
        //    //{
        //    //    BuildingFloor floor = buildingPref.BuildingFloors[floorIndex];

        //    //    yPos += floor.FloorSize.y / 2f;

        //    //    Vector3 scale = floor.FloorSize.ComponentWiseMult( new Vector3( 1, 0.85f, 1f ) );
        //    //    Vector3 pos = new Vector3( center.x, yPos, center.z );

        //    //    yPos += floor.FloorSize.y / 2f;

        //    //    if ( floorIndex == HighlightedFloor )
        //    //    {
        //    //        DrawFloorCube( FloorCubePurple, floor, pos, rotation );
        //    //    }
        //    //    else
        //    //    {

        //    //        DrawFloorCube( FloorCubeOrange, floor, pos, rotation );
        //    //    }
        //    //}
        //}
        #endregion

        #region DrawFloorsInner_OldStyle
        private static void DrawFloorsInner_OldStyle( MapItem mapItem, BuildingPrefab buildingPref )
        {
            Vector3 center = mapItem.OBBCache.Center;
            center += mapItem.OBBCache.GetOBB_ExpensiveToUse().Rotation * buildingPref.FloorsOffset;
            float yBot = mapItem.Type.AlwaysDropTo >= -900 ? 0 : mapItem.OBBCache.BottomCenter.y;
            Quaternion rotation = mapItem.OBBCache.GetOBB_ExpensiveToUse().Rotation;

            float yPos = yBot;

            int maxFloorToDrawAsAbnormalBasement = -10000;
            int minFloorToDrawAsAbnormalBasement = 10000;
            int minFloorToDrawAsAbnormalUpper = 10000;

            int minFloor = buildingPref.MinFloor;

            //if ( socketInUse != null )
            //{
            //    if ( socketInUse.BasementFloorsTaken > 0 )
            //    {
            //        if ( socketInUse.Prefab.Type.BasementClaimsFromBottomUp )
            //            maxFloorToDrawAsAbnormalBasement = buildingPref.MinFloor + socketInUse.BasementFloorsTaken;
            //        else
            //            minFloorToDrawAsAbnormalBasement = -socketInUse.BasementFloorsTaken;
            //    }
            //    if ( socketInUse.UpperFloorsTaken > 0 )
            //        minFloorToDrawAsAbnormalUpper = buildingPref.MaxFloor - socketInUse.UpperFloorsTaken;
            //}

            yPos = yBot;
            for ( int floorIndex = -1; floorIndex >= minFloor; floorIndex-- )
            {
                BuildingFloor floor = buildingPref.BuildingFloors[floorIndex];

                yPos -= floor.FloorSize.y / 2f;

                Vector3 scale = floor.FloorSize.ComponentWiseMult( new Vector3( 1, 0.99f, 1f ) );
                Vector3 pos = new Vector3( center.x, yPos, center.z );

                yPos -= floor.FloorSize.y / 2f;

                if ( floorIndex >= minFloorToDrawAsAbnormalBasement || floorIndex < maxFloorToDrawAsAbnormalBasement )
                {
                    DrawShape_WireBoxOriented wireBox;
                    wireBox.Center = pos;
                    wireBox.Rotation = rotation;
                    wireBox.Size = scale;
                    wireBox.Color = ColorMath.Purple;
                    wireBox.Thickness = 1f;
                    VisCurrent.PerFloorWireShapes.Add( wireBox );

                    DrawFloorCube( FloorCubePurple, floor, pos, rotation );
                }
                else
                {
                    DrawShape_WireBoxOriented wireBox;
                    wireBox.Center = pos;
                    wireBox.Rotation = rotation;
                    wireBox.Size = scale;
                    wireBox.Color = ColorMath.Red;
                    wireBox.Thickness = 1f;
                    VisCurrent.PerFloorWireShapes.Add( wireBox );

                    DrawFloorCube( FloorCubeRed, floor, pos, rotation );
                }
            }

            yPos = yBot;
            for ( int floorIndex = 0; floorIndex <= buildingPref.MaxFloor; floorIndex++ )
            {
                BuildingFloor floor = buildingPref.BuildingFloors[floorIndex];

                yPos += floor.FloorSize.y / 2f;

                Vector3 scale = floor.FloorSize.ComponentWiseMult( new Vector3( 1, 0.99f, 1f ) );
                Vector3 pos = new Vector3( center.x, yPos, center.z );

                yPos += floor.FloorSize.y / 2f;

                if ( floorIndex > minFloorToDrawAsAbnormalUpper )
                {
                    DrawShape_WireBoxOriented wireBox;
                    wireBox.Center = pos;
                    wireBox.Rotation = rotation;
                    wireBox.Size = scale;
                    wireBox.Color = ColorMath.LeafGreen;
                    wireBox.Thickness = 1;
                    VisCurrent.PerFloorWireShapes.Add( wireBox );

                    DrawFloorCube( FloorCubeGreen, floor, pos, rotation );
                }
                else
                {
                    DrawShape_WireBoxOriented wireBox;
                    wireBox.Center = pos;
                    wireBox.Rotation = rotation;
                    wireBox.Size = scale;
                    wireBox.Color = ColorMath.Gold;
                    wireBox.Thickness = 1f;
                    VisCurrent.PerFloorWireShapes.Add( wireBox );

                    DrawFloorCube( FloorCubeOrange, floor, pos, rotation );
                }
            }
        }
        #endregion

        #region DrawFloorCube
        private static void DrawFloorCube( VisSimpleDrawingObject FloorCubeObject, BuildingFloor floor, Vector3 pos, Quaternion rot )
        {
            A5RendererGroup rendGroup = FloorCubeObject?.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: rend null", Verbosity.DoNotShow );
                return;
            }

            RenderColorStyle colorStyle = RenderColorStyle.NoColor;

            Vector3 scale = floor.FloorSize.ComponentWiseMult( new Vector3( 1, 0.85f, 1f ) );
            rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, scale, colorStyle );
        }
        #endregion

        #region DrawColoredFloorCube
        public static void DrawColoredFloorCube( FloorCubeColor CubeColor, Vector3 pos, Vector3 size, Quaternion rot )
        {
            InitFloorObjectsIfNeeded();

            VisSimpleDrawingObject floorCubeObject = null;
            switch (CubeColor)
            {
                case FloorCubeColor.Blue:
                    floorCubeObject = FloorCubeBlue;
                    break;
                case FloorCubeColor.Cyan:
                    floorCubeObject = FloorCubeCyan;
                    break;
                case FloorCubeColor.Green:
                    floorCubeObject = FloorCubeGreen;
                    break;
                case FloorCubeColor.Orange:
                    floorCubeObject = FloorCubeOrange;
                    break;
                case FloorCubeColor.Pink:
                    floorCubeObject = FloorCubePink;
                    break;
                case FloorCubeColor.Purple:
                    floorCubeObject = FloorCubePurple;
                    break;
                case FloorCubeColor.Red:
                    floorCubeObject = FloorCubeRed;
                    break;
                case FloorCubeColor.Yellow:
                    floorCubeObject = FloorCubeYellow;
                    break;
            }

            A5RendererGroup rendGroup = floorCubeObject?.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: rend null", Verbosity.DoNotShow );
                return;
            }

            RenderColorStyle colorStyle = RenderColorStyle.NoColor;
            rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot, size, colorStyle );
        }
        #endregion

        #region DrawPOICubeTransparent
        private static VisSimpleDrawingObject poiCube;
        public static void DrawPOICubeTransparent( Color color, Vector3 pos, Vector3 size, Quaternion rot )
        {
            if ( poiCube == null )
                poiCube = VisSimpleDrawingObjectTable.Instance.GetRowByID( "MapModeCube" );

            A5RendererGroup rendGroup = poiCube?.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw vehicle fail: rend null", Verbosity.DoNotShow );
                return;
            }

            RenderColorStyle colorStyle = RenderColorStyle.HighlightColor;
            rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, size, colorStyle, color, RenderOpacity.Transparent_Batched, false );
        }
        #endregion
    }

    public enum FloorCubeColor
    {
        Blue,
        Cyan,
        Green,
        Orange,
        Pink,
        Purple,
        Red,
        Yellow
    }
}
