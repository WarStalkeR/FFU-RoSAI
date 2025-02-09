using System;

using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{    
    public static class CursorHelper
    {
        #region RenderSpecificMouseCursorAtSpot
        public static void RenderSpecificMouseCursorAtSpot( bool IsForOverlayCamera, VisIconUsage Usage, Vector3 EffectivePoint )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, EffectivePoint, Usage.DefaultScale, Usage.DefaultColorHDR,  false, false, true );
        }

        public static void RenderSpecificMouseCursorAtSpot( bool IsForOverlayCamera, VisIconUsage Usage, Vector3 EffectivePoint, float MultipliedScale )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, EffectivePoint, Usage.DefaultScale * MultipliedScale, Usage.DefaultColorHDR, false, false, true );
        }

        public static void RenderSpecificMouseCursorAtSpot( bool IsForOverlayCamera, IA5Sprite AltSprite, VisIconUsage Usage, Vector3 EffectivePoint )
        {
            if ( AltSprite == null || Usage == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            //draw the icon, normally
            AltSprite.WriteToDrawBufferForOneFrame( IsForOverlayCamera, EffectivePoint, Usage.DefaultScale, Usage.DefaultColorHDR, false, false, true );
        }
        #endregion

        #region RenderSpecificMouseCursorAtSpot
        public static void RenderSpecificMouseCursorAtSpotWithColor( bool IsForOverlayCamera, VisIconUsage Usage, Vector3 EffectivePoint, Color ColorHDR )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, EffectivePoint, Usage.DefaultScale, ColorHDR, false, false, true );
        }

        public static void RenderSpecificMouseCursorAtSpotWithColor( bool IsForOverlayCamera, VisIconUsage Usage, Vector3 EffectivePoint, float MultipliedScale, Color ColorHDR )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, EffectivePoint, Usage.DefaultScale * MultipliedScale, ColorHDR, false, false, true );
        }

        public static void RenderSpecificMouseCursorAtSpotWithColor( bool IsForOverlayCamera, IA5Sprite AltSprite, VisIconUsage Usage, Vector3 EffectivePoint, Color ColorHDR )
        {
            if ( AltSprite == null || Usage == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            //draw the icon, normally
            AltSprite.WriteToDrawBufferForOneFrame( IsForOverlayCamera, EffectivePoint, Usage.DefaultScale, ColorHDR, false, false, true );
        }
        #endregion

        #region RenderSpecificScalingIconAtSpot
        public static void RenderSpecificScalingIconAtSpot( bool IsForOverlayCamera, VisIconUsage Usage, Vector3 EffectivePoint, bool ShouldShowExtraBright )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            float scale = Usage.DefaultScale;

            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - EffectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 100 ) //10 squared
            {
                float squareDistanceAbove = squareDistanceFromCamera - 100;
                if ( squareDistanceAbove >= 1600 ) //40 squared
                    scale *= 4f;
                else
                    scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
            }

            Color color = Usage.DefaultColorHDR;
            if ( ShouldShowExtraBright )
                color = Usage.HoverDefaultColorHDR;

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, EffectivePoint, scale, color, false, false, true );
        }

        public static void RenderSpecificScalingIconAtSpot( bool IsForOverlayCamera, VisIconUsage Usage, Vector3 EffectivePoint, float ScaleMultiplier )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            float scale = Usage.DefaultScale;

            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - EffectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 100 ) //10 squared
            {
                float squareDistanceAbove = squareDistanceFromCamera - 100;
                if ( squareDistanceAbove >= 1600 ) //40 squared
                    scale *= 4f;
                else
                    scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
            }

            scale *= ScaleMultiplier;

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, EffectivePoint, scale, Usage.DefaultColorHDR, false, false, true );
        }

        public static void RenderSpecificScalingIconAtSpot( bool IsForOverlayCamera, IA5Sprite AltSprite, VisIconUsage Usage, Vector3 EffectivePoint, bool ShouldShowExtraBright )
        {
            if ( AltSprite == null || Usage == null || Usage.Icon == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            float scale = Usage.DefaultScale;

            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - EffectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 100 ) //10 squared
            {
                float squareDistanceAbove = squareDistanceFromCamera - 100;
                if ( squareDistanceAbove >= 1600 ) //40 squared
                    scale *= 4f;
                else
                    scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
            }

            Color color = Usage.DefaultColorHDR;
            if ( ShouldShowExtraBright )
                color = Usage.HoverDefaultColorHDR;

            //draw the icon, normally
            AltSprite.WriteToDrawBufferForOneFrame( IsForOverlayCamera, EffectivePoint, scale, color, false, false, true );
        }
        #endregion

        #region RenderSpecificScalingIconDictionaryAtSpot
        public static void RenderSpecificScalingIconDictionaryAtSpot<T>( bool IsForOverlayCamera, DictionaryView<IVisIconUsageHolder,T> UsageDict, float XMovementPer, 
            Vector3 EffectivePoint, bool ShouldShowExtraBright )
        {
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            int countToActuallyDraw = 0;
            foreach ( KeyValuePair<IVisIconUsageHolder, T> kv in UsageDict )
            {
                if ( kv.Key.GetShouldRenderVisIcon() )
                    countToActuallyDraw++;
            }

            if ( countToActuallyDraw == 0 )
                return;

            //float scale = Usage.DefaultScale;

            float scaleMultiplier = 1f;
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - EffectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 100 ) //10 squared
            {
                float squareDistanceAbove = squareDistanceFromCamera - 100;
                if ( squareDistanceAbove >= 1600 ) //40 squared
                    scaleMultiplier *= 4f;
                else
                    scaleMultiplier *= (1 + ((squareDistanceAbove / 1600f) * 3f));
            }

            Vector3 pos = EffectivePoint;
            Vector3 xMovementPer = (CameraCurrent.CameraRight * (XMovementPer * scaleMultiplier));
            if ( countToActuallyDraw > 1 )
                pos -= (xMovementPer * countToActuallyDraw * 0.5f);

            foreach ( KeyValuePair<IVisIconUsageHolder, T> kv in UsageDict )
            {
                if ( !kv.Key.GetShouldRenderVisIcon() )
                    continue;

                VisIconUsage usage = kv.Key.GetVisIcon();

                Color color = usage.DefaultColorHDR;
                if ( ShouldShowExtraBright )
                    color = usage.HoverDefaultColorHDR;

                float finalScale = usage.DefaultScale * scaleMultiplier;

                //draw the icon, normally
                usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, pos, finalScale, color, false, false, true );
                pos += xMovementPer;
            }
        }
        #endregion

        #region RenderSpecificScalingIconDictionaryAtSpot
        public static void RenderSpecificScalingIconDictionaryAtSpot<T>( bool IsForOverlayCamera, DictionaryView<ICustomIconHolder, T> UsageDict, float XMovementPer,
            Vector3 EffectivePoint, bool ShouldShowExtraBright )
        {
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            int countToActuallyDraw = UsageDict.Count;
            if ( countToActuallyDraw == 0 )
                return;

            //float scale = Usage.DefaultScale;

            float scaleMultiplier = 1f;
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - EffectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 100 ) //10 squared
            {
                float squareDistanceAbove = squareDistanceFromCamera - 100;
                if ( squareDistanceAbove >= 1600 ) //40 squared
                    scaleMultiplier *= 4f;
                else
                    scaleMultiplier *= (1 + ((squareDistanceAbove / 1600f) * 3f));
            }

            Vector3 pos = EffectivePoint;
            Vector3 xMovementPer = (CameraCurrent.CameraRight * (XMovementPer * scaleMultiplier));
            if ( countToActuallyDraw > 1 )
                pos -= (xMovementPer * countToActuallyDraw * 0.5f);

            foreach ( KeyValuePair<ICustomIconHolder, T> kv in UsageDict )
            {
                IA5Sprite sprite = kv.Key.GetIcon();

                Color color = kv.Key.GetColorHDR();
                if ( ShouldShowExtraBright )
                    color = kv.Key.GetColorHoveredHDR();

                float finalScale = kv.Key.GetIconScale() * scaleMultiplier;

                //draw the icon, normally
                sprite.WriteToDrawBufferForOneFrame( IsForOverlayCamera, pos, finalScale, color, false, false, true );
                pos += xMovementPer;
            }
        }
        #endregion

        
        #region RenderFrameStyleSpecificScalingIconAtSpot
        public static void RenderFrameStyleSpecificScalingIconAtSpot( bool IsForOverlayCamera, VisIconUsage Usage, Vector3 EffectivePoint, bool ShouldShowHoverFill )
        {
            if ( Usage == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            float scale = Usage.DefaultScale;

            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - EffectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 100 ) //10 squared
            {
                float squareDistanceAbove = squareDistanceFromCamera - 100;
                if ( squareDistanceAbove >= 1600 ) //40 squared
                    scale *= 4f;
                else
                    scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
            }

            Color mainColor = Usage.DefaultColorHDR;
            Color fillColor = Usage.FillColorHDR;
            if ( ShouldShowHoverFill )
            {
                mainColor = Usage.HoverDefaultColorHDR;
                fillColor = Usage.HoverFillColorHDR;
            }

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferFrameStyleForOneFrame( IsForOverlayCamera, EffectivePoint, scale, mainColor,
                Usage.FrameColorHDR, fillColor, Usage.FrameMaskRowCol_Vector4, Usage.UseBackingColor, true );

            //ArcenDebugging.LogSingleLine( AltSprite.FullName + ": " + Usage.ID + ": " + Usage.FrameMaskRowCol + " " + Usage.UseBackingColor, Verbosity.DoNotShow );
        }

        public static void RenderFrameStyleSpecificScalingIconAtSpot( bool IsForOverlayCamera, IA5Sprite AltSprite, VisIconUsage Usage, Vector3 EffectivePoint, bool ShouldShowHoverFill )
        {
            if ( Usage == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            float scale = Usage.DefaultScale;

            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - EffectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 100 ) //10 squared
            {
                float squareDistanceAbove = squareDistanceFromCamera - 100;
                if ( squareDistanceAbove >= 1600 ) //40 squared
                    scale *= 4f;
                else
                    scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
            }

            Color mainColor = Usage.DefaultColorHDR;
            Color fillColor = Usage.FillColorHDR;
            if ( ShouldShowHoverFill )
            {
                mainColor = Usage.HoverDefaultColorHDR;
                fillColor = Usage.HoverFillColorHDR;
            }

            //draw the icon, normally
            AltSprite.WriteToDrawBufferFrameStyleForOneFrame( IsForOverlayCamera, EffectivePoint, scale, mainColor,
                Usage.FrameColorHDR, fillColor, Usage.FrameMaskRowCol_Vector4, Usage.UseBackingColor, true );

            //ArcenDebugging.LogSingleLine( AltSprite.FullName + ": " + Usage.ID + ": " + Usage.FrameMaskRowCol + " " + Usage.UseBackingColor, Verbosity.DoNotShow );
        }
        #endregion

        #region RenderFrameStyleSpecificIconAtSpot
        public static void RenderFrameStyleSpecificIconAtSpot( bool IsForOverlayCamera, IA5Sprite AltSprite, VisIconUsage Usage, Vector3 EffectivePoint )
        {
            if ( AltSprite == null || Usage == null )
                return;
            if ( float.IsInfinity( EffectivePoint.x ) )
                return;

            //draw the icon, normally
            AltSprite.WriteToDrawBufferFrameStyleForOneFrame( IsForOverlayCamera, EffectivePoint, Usage.DefaultScale, Usage.DefaultColorHDR,
                Usage.FrameColorHDR, Usage.FillColorHDR, Usage.FrameMaskRowCol_Vector4, Usage.UseBackingColor, true );

            //ArcenDebugging.LogSingleLine( AltSprite.FullName + ": " + Usage.ID + ": " + Usage.FrameMaskRowCol + " " + Usage.UseBackingColor, Verbosity.DoNotShow );
        }
        #endregion

        #region RenderSpecificMouseCursor
        public static void RenderSpecificMouseCursor( bool IsForOverlayCamera, VisIconUsage Usage )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow )
                return;

            Vector3 mousePoint = Engine_HotM.MouseWorldHitLocation;
            if ( float.IsInfinity( mousePoint.x ) )
                return;

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, mousePoint, Usage.DefaultScale, Usage.DefaultColorHDR, false, false, true );
        }
        #endregion

        #region RenderSpecificScalingMouseCursor
        public static void RenderSpecificScalingMouseCursor( bool IsForOverlayCamera, VisIconUsage Usage )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow )
                return;

            Vector3 mousePoint = Engine_HotM.MouseWorldHitLocation;
            if ( float.IsInfinity( mousePoint.x ) )
                return;

            float scale = Usage.DefaultScale;

            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - mousePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 100 ) //10 squared
            {
                float squareDistanceAbove = squareDistanceFromCamera - 100;
                if ( squareDistanceAbove >= 1600 ) //40 squared
                    scale *= 3f;
                else
                    scale *= (1 + ((squareDistanceAbove / 1600f) * 2f));
            }

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, mousePoint, scale, Usage.DefaultColorHDR, false, false, true );
        }

        public static void RenderSpecificScalingMouseCursor( bool IsForOverlayCamera, VisIconUsage Usage, float ScalingRangeMin, float ScalingRangeMax, float MaxMultiplier )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow )
                return;

            Vector3 mousePoint = Engine_HotM.MouseWorldHitLocation;
            if ( float.IsInfinity( mousePoint.x ) )
                return;

            float scale = Usage.DefaultScale;

            float rangeSquaredMin = ScalingRangeMin * ScalingRangeMin;
            float rangeSquaredMax = ScalingRangeMax * ScalingRangeMax;

            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - mousePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > rangeSquaredMin )
            {
                float squareDistanceAbove = squareDistanceFromCamera - rangeSquaredMin;
                if ( squareDistanceAbove >= rangeSquaredMax )
                    scale *= MaxMultiplier;
                else
                    scale *= (1 + ((squareDistanceAbove / rangeSquaredMax) * (MaxMultiplier-1f)));
            }

            //draw the icon, normally
            Usage.Icon.WriteToDrawBufferForOneFrame( IsForOverlayCamera, mousePoint, scale, Usage.DefaultColorHDR, false, false, true );
        }

        public static void RenderSpecificScalingMouseCursor( bool IsForOverlayCamera, IA5Sprite AltSprite, VisIconUsage Usage, float ScalingRangeMin, float ScalingRangeMax, float MaxMultiplier )
        {
            if ( Usage == null || Usage.Icon == null )
                return;
            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow )
                return;

            Vector3 mousePoint = Engine_HotM.MouseWorldHitLocation;
            if ( float.IsInfinity( mousePoint.x ) )
                return;

            float scale = Usage.DefaultScale;

            float rangeSquaredMin = ScalingRangeMin * ScalingRangeMin;
            float rangeSquaredMax = ScalingRangeMax * ScalingRangeMax;

            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - mousePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > rangeSquaredMin )
            {
                float squareDistanceAbove = squareDistanceFromCamera - rangeSquaredMin;
                if ( squareDistanceAbove >= rangeSquaredMax )
                    scale *= MaxMultiplier;
                else
                    scale *= (1 + ((squareDistanceAbove / rangeSquaredMax) * (MaxMultiplier - 1f)));
            }

            //draw the icon, normally
            AltSprite.WriteToDrawBufferForOneFrame( IsForOverlayCamera, mousePoint, scale, Usage.DefaultColorHDR, false, false, true );
        }
        #endregion

        #region FindNPCMissionUnderCursor
        public static NPCMission FindNPCMissionUnderCursor()
        {
            if ( Engine_HotM.MarkableUnderMouse is IAutoPooledFloatingColliderIcon icon )
            {
                if ( icon != null )
                    return icon.GetCurrentRelated() as NPCMission;
            }
            return null;
        }
        #endregion

        #region FindMapActorUnderCursor
        public static ISimMapActor FindMapActorUnderCursor()
        {
            if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated icon )
            {
                if ( icon != null )
                    return icon.GetCurrentRelated() as ISimMapActor;
            }
            return null;
        }
        #endregion

        #region FindUnitUnderCursor
        public static ISimUnit FindUnitUnderCursor()
        {
            if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated icon )
            {
                if ( icon != null )
                    return icon.GetCurrentRelated() as ISimUnit;
            }
            return null;
        }
        #endregion

        #region FindMachineVehicleUnderCursorIfNotDowned
        public static ISimMachineVehicle FindMachineVehicleUnderCursorIfNotDowned()
        {
            if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated icon )
            {
                if ( icon != null )
                {
                    ISimMachineVehicle vehicle = icon.GetCurrentRelated() as ISimMachineVehicle;
                    if ( vehicle != null && !vehicle.IsFullDead )
                        return vehicle;
                }
            }
            return null;
        }
        #endregion

        #region FindBuildingUnderCursorNoFilters
        public static ISimBuilding FindBuildingUnderCursorNoFilters()
        {
            ISimBuilding building = null;

            if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated auto )
            {
                if ( auto.GetCurrentRelated() is ISimBuilding build )
                    return build;
            }

            if ( building != null )
                return building;

            if ( Engine_HotM.PlaceableUnderMouse != null )
                building = Engine_HotM.PlaceableUnderMouse?.GetCityMapItem()?.SimBuilding;


            return building;
        }
        #endregion
    }
}
