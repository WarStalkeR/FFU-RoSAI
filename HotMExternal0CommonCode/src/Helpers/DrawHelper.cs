using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{    
    public static class DrawHelper
    {
        #region RenderFloatingTextAtBuilding
        public static void RenderFloatingTextAtBuilding( LocationCalculationCache cache, Vector3 Position, string RenderText )
        {
            if ( cache == null )
                return;

            cache.InitializeLocationText();
            cache.LocationFloatingText.WorldLocation = Position;// item.OBBCache.OBB.Center;

            if ( cache.LocationFloatingText != null )
            {
                float fontSize = 3f;

                float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - Position).sqrMagnitude;
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        fontSize *= 4f;
                    else
                        fontSize *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                }

                cache.LocationFloatingText.FontSize = fontSize;
                cache.LocationFloatingText.MarkAsStillInUseThisFrame();
                cache.LocationFloatingText.Text = RenderText;
                cache.RenderLocationFloatingTextUntil = ArcenTime.AnyTimeSinceStartF + 0.1f;
            }
        }
        #endregion

        #region RenderRangeCircle
        public static void RenderRangeCircle( Vector3 center, float Range, Color BorderColor, float Thickness )
        {
            DrawShape_WireCircleXZ rangeCircleBorder = new DrawShape_WireCircleXZ
            {
                Center = center,
                Radius = Range,
                Color = BorderColor,
                Thickness = Thickness
            };
            if ( VisCurrent.AreAllGameIconsHidden )
                return;
            DrawList.Add( rangeCircleBorder );
        }

        public static void RenderRangeCircle( Vector3 center, float Range, Color BorderColor )
        {
            DrawShape_WireCircleXZ rangeCircleBorder = new DrawShape_WireCircleXZ
            {
                Center = center,
                Radius = Range,
                Color = BorderColor
            };
            if ( VisCurrent.AreAllGameIconsHidden )
                return;
            DrawList.Add( rangeCircleBorder );
        }
        #endregion

        #region RenderRangeHex
        public static void RenderRangeHex( Vector3 center, float Range, Quaternion Rotation, Color BorderColor, float Thickness )
        {
            DrawShape_WirePolygon rangeCircleBorder = new DrawShape_WirePolygon
            {
                Center = center,
                Radius = Range,
                Color = BorderColor,
                Sides = 6,
                Rotation = Rotation,
                Thickness = Thickness
            };
            if ( VisCurrent.AreAllGameIconsHidden )
                return;
            DrawList.Add( rangeCircleBorder );
        }

        public static void RenderRangeHex( Vector3 center, float Range, Quaternion Rotation, Color BorderColor )
        {
            DrawShape_WirePolygon rangeCircleBorder = new DrawShape_WirePolygon
            {
                Center = center,
                Radius = Range,
                Sides = 6,
                Rotation = Rotation,
                Color = BorderColor
            };
            if ( VisCurrent.AreAllGameIconsHidden )
                return;
            DrawList.Add( rangeCircleBorder );
        }
        #endregion

        #region RenderLine
        public static void RenderLine( Vector3 p1, Vector3 p2, Color LineColor )
        {
            DrawShape_Line lineItself = new DrawShape_Line()
            {
                Start = p1,
                End = p2,
                Color = LineColor
            };
            if ( VisCurrent.AreAllGameIconsHidden )
                return;
            DrawList.Add( lineItself );
        }

        public static void RenderLine( Vector3 p1, Vector3 p2, Color LineColor, float Thickness )
        {
            DrawShape_Line lineItself = new DrawShape_Line()
            {
                Start = p1,
                End = p2,
                Color = LineColor,
                Thickness = Thickness
            };
            if ( VisCurrent.AreAllGameIconsHidden )
                return;
            DrawList.Add( lineItself );
        }
        #endregion

        public static List<IShapeToDraw> DrawList;

        private static readonly GenericListPool<Vector3> moveCatmull = new GenericListPool<Vector3>( "DrawHelper-moveCatmull", 5, 10, PoolBehaviorDuringShutdown.AllowAllThreads );
        private static readonly GenericListPool<Vector3> aggroCatmull = new GenericListPool<Vector3>( "DrawHelper-aggroCatmull", 200, 10, PoolBehaviorDuringShutdown.AllowAllThreads );
        private static readonly GenericListPool<Vector3> predictionCatmull = new GenericListPool<Vector3>( "DrawHelper-predictionCatmull", 50, 10, PoolBehaviorDuringShutdown.AllowAllThreads );

        private static readonly GenericListPool<Vector3> p3Polyline = new GenericListPool<Vector3>( "DrawHelper-p3Polyline", 200, 4, PoolBehaviorDuringShutdown.AllowAllThreads );
        private static readonly GenericListPool<Vector3> p4Polyline = new GenericListPool<Vector3>( "DrawHelper-p4Polyline", 200, 5, PoolBehaviorDuringShutdown.AllowAllThreads );
        private static readonly GenericListPool<Vector3> p5Polyline = new GenericListPool<Vector3>( "DrawHelper-p4Polyline", 200, 6, PoolBehaviorDuringShutdown.AllowAllThreads );
        private static readonly GenericListPool<Vector3> p6Polyline = new GenericListPool<Vector3>( "DrawHelper-p4Polyline", 200, 7, PoolBehaviorDuringShutdown.AllowAllThreads );

        #region ResetDrawPoolsForNewFrame
        public static void ResetDrawPoolsForNewFrame()
        {
            moveCatmull.Clear();
            aggroCatmull.Clear();
            predictionCatmull.Clear();

            p3Polyline.Clear();
            p4Polyline.Clear();
            p5Polyline.Clear();
            p6Polyline.Clear();
        }
        #endregion

        #region RenderCatmullLine
        public static void RenderCatmullLine( Vector3 p1, Vector3 p2, Color LineColor, float Thickness,
            CatmullSlotType SlotType, CatmullSlope Slope )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;
            if ( float.IsInfinity( p1.x ) || float.IsInfinity( p2.x ) )
                return;

            System.Collections.Generic.List<Vector3> reusableCatmullList = null;
            switch ( SlotType )
            {
                case CatmullSlotType.Move:
                    reusableCatmullList = moveCatmull.GetOrAddEntry();
                    break;
                case CatmullSlotType.Aggro:
                    reusableCatmullList = aggroCatmull.GetOrAddEntry();
                    break;
                case CatmullSlotType.Prediction:
                    reusableCatmullList = predictionCatmull.GetOrAddEntry();
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "No catmull list of slot type " + SlotType + "!", Verbosity.ShowAsError );
                    break;
            }

            reusableCatmullList.Clear();
            reusableCatmullList.Add( p1 );
            switch (Slope)
            {
                case CatmullSlope.Movement:
                    {
                        float yDiff = p1.y - p2.y;
                        if ( yDiff < 0 )
                            yDiff = -yDiff;
                        if ( yDiff < 0.4f )
                            yDiff = 0.4f;

                        float first = 0.9f;
                        float second = 0.4f;
                        if ( p1.y < p2.y )
                        {
                            first = 0.4f;
                            second = 0.9f;
                        }

                        Vector3 p1A = Vector3.Lerp( p1, p2, 0.2f ).PlusY( yDiff * first );
                        Vector3 p1B = Vector3.Lerp( p1, p2, 0.6f ).PlusY( yDiff * second );

                        reusableCatmullList.Add( p1A );
                        reusableCatmullList.Add( p1B );
                    }
                    break;
                case CatmullSlope.AndroidTargeting:
                    {
                        Vector3 p1A = Vector3.Lerp( p1, p2, MathRefs.AndroidTargetingP1Adjustments.FloatMin ).PlusY( MathRefs.AndroidTargetingP1Adjustments.FloatMax );
                        Vector3 p1B = Vector3.Lerp( p1, p2, MathRefs.AndroidTargetingP2Adjustments.FloatMin ).PlusY( MathRefs.AndroidTargetingP2Adjustments.FloatMax );

                        reusableCatmullList.Add( p1A );
                        reusableCatmullList.Add( p1B );
                    }
                    break;
                case CatmullSlope.MechTargeting:
                    {
                        Vector3 p1A = Vector3.Lerp( p1, p2, MathRefs.MechTargetingP1Adjustments.FloatMin ).PlusY( MathRefs.MechTargetingP1Adjustments.FloatMax );
                        Vector3 p1B = Vector3.Lerp( p1, p2, MathRefs.MechTargetingP2Adjustments.FloatMin ).PlusY( MathRefs.MechTargetingP2Adjustments.FloatMax );

                        reusableCatmullList.Add( p1A );
                        reusableCatmullList.Add( p1B );
                    }
                    break;
                case CatmullSlope.EnemyTargetingUs:
                    {
                        float yDiff = p1.y - p2.y;
                        if ( yDiff < 0 )
                            yDiff = -yDiff;
                        if ( yDiff < 2f )
                            yDiff = 2f;

                        yDiff *= 0.3f;
                        yDiff = Mathf.Pow( yDiff, 1.5f );

                        float first = 0.9f;
                        float second = 0.4f;
                        if ( p1.y < p2.y )
                        {
                            first = 0.4f;
                            second = 0.9f;
                        }

                        Vector3 p1A = Vector3.Lerp( p1, p2, 0.2f ).PlusY( yDiff * first );
                        Vector3 p1B = Vector3.Lerp( p1, p2, 0.6f ).PlusY( yDiff * second );

                        reusableCatmullList.Add( p1A );
                        reusableCatmullList.Add( p1B );
                    }
                    break;
                case CatmullSlope.VehicleTargeting:
                    {
                        Vector3 p1A = Vector3.Lerp( p1, p2, MathRefs.VehicleTargetingP1Adjustments.FloatMin ).PlusY( MathRefs.VehicleTargetingP1Adjustments.FloatMax );
                        Vector3 p1B = Vector3.Lerp( p1, p2, MathRefs.VehicleTargetingP2Adjustments.FloatMin ).PlusY( MathRefs.VehicleTargetingP2Adjustments.FloatMax );

                        reusableCatmullList.Add( p1A );
                        reusableCatmullList.Add( p1B );
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "CatmullSlope " + Slope + " not properly set up!", Verbosity.ShowAsError );
                    break;
            }
            
            reusableCatmullList.Add( p2 );

            DrawShape_CatmullRom lineItself = new DrawShape_CatmullRom()
            {
                points = reusableCatmullList,
                Color = LineColor,
                Thickness = Thickness
            };
            DrawList.Add( lineItself );
        }
        #endregion

        #region RenderPolyline_P3
        public static void RenderPolyline_P3( Vector3 p1, Vector3 p2, Vector3 p3, Color LineColor, bool CycleBackAroundToStart, float Thickness )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;

            System.Collections.Generic.List<Vector3> reusablePolylineList = p3Polyline.GetOrAddEntry();

            reusablePolylineList.Clear();
            reusablePolylineList.Add( p1 );
            reusablePolylineList.Add( p2 );
            reusablePolylineList.Add( p3 );

            DrawShape_Polyline lineItself = new DrawShape_Polyline()
            {
                points = reusablePolylineList,
                Color = LineColor,
                CycleBackAroundToStart = CycleBackAroundToStart,
                Thickness = Thickness
            };
            DrawList.Add( lineItself );
        }
        #endregion

        #region RenderPolyline_P4
        public static void RenderPolyline_P4( Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color LineColor, bool CycleBackAroundToStart, float Thickness )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;

            System.Collections.Generic.List<Vector3> reusablePolylineList = p4Polyline.GetOrAddEntry();

            reusablePolylineList.Clear();
            reusablePolylineList.Add( p1 );
            reusablePolylineList.Add( p2 );
            reusablePolylineList.Add( p3 );
            reusablePolylineList.Add( p4 );

            DrawShape_Polyline lineItself = new DrawShape_Polyline()
            {
                points = reusablePolylineList,
                Color = LineColor,
                CycleBackAroundToStart = CycleBackAroundToStart,
                Thickness = Thickness
            };
            DrawList.Add( lineItself );
        }
        #endregion

        #region RenderPolyline_P5
        public static void RenderPolyline_P5( Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5, Color LineColor, bool CycleBackAroundToStart, float Thickness )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;

            System.Collections.Generic.List<Vector3> reusablePolylineList = p5Polyline.GetOrAddEntry();

            reusablePolylineList.Clear();
            reusablePolylineList.Add( p1 );
            reusablePolylineList.Add( p2 );
            reusablePolylineList.Add( p3 );
            reusablePolylineList.Add( p4 );
            reusablePolylineList.Add( p5 );

            DrawShape_Polyline lineItself = new DrawShape_Polyline()
            {
                points = reusablePolylineList,
                Color = LineColor,
                CycleBackAroundToStart = CycleBackAroundToStart,
                Thickness = Thickness
            };
            DrawList.Add( lineItself );
        }
        #endregion

        #region RenderPolyline_P6
        public static void RenderPolyline_P6( Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5, Vector3 p6, Color LineColor, bool CycleBackAroundToStart, float Thickness )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;

            System.Collections.Generic.List<Vector3> reusablePolylineList = p6Polyline.GetOrAddEntry();

            reusablePolylineList.Clear();
            reusablePolylineList.Add( p1 );
            reusablePolylineList.Add( p2 );
            reusablePolylineList.Add( p3 );
            reusablePolylineList.Add( p4 );
            reusablePolylineList.Add( p5 );
            reusablePolylineList.Add( p6 );

            DrawShape_Polyline lineItself = new DrawShape_Polyline()
            {
                points = reusablePolylineList,
                Color = LineColor,
                CycleBackAroundToStart = CycleBackAroundToStart,
                Thickness = Thickness
            };
            DrawList.Add( lineItself );
        }
        #endregion

        #region RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct
        public static void RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( Vector3 p1, Vector3 p4, float AbsoluteHorizontalY, Color LineColor, float Thickness )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;

            Vector3 p2 = p1;
            p2.y = AbsoluteHorizontalY;

            Vector3 p3 = p4;
            p3.y = AbsoluteHorizontalY;

            RenderPolyline_P4( p1, p2, p3, p4, LineColor, false, Thickness );

            //then draw the arrow from the attacker towards the target

            Vector3 heading = p3 - p2; //pointing from p2 to p3
            heading = heading.normalized;
            float cornerLengths = 0.2f;

            Vector3 arrowCenter = p2 + ( heading * -cornerLengths );

            DrawShape_Arrowhead arrow;
            arrow.Color = LineColor;
            arrow.Thickness = Thickness;
            arrow.Up = Vector3.up;
            arrow.Direction = heading;
            arrow.Center = arrowCenter;
            arrow.CornerLengths = cornerLengths;

            DrawList.Add( arrow );
        }
        #endregion

        #region RenderPolylineFromSourceToTargetPoint_CaliperStyle_Overhead
        public static void RenderPolylineFromSourceToTargetPoint_CaliperStyle_Overhead( Vector3 p1, Vector3 p4, float EntityHeight1, float EntityHeight2, float RequiredSpacingAboveEitherEntity, Color LineColor, float Thickness )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;

            Vector3 p2 = p1;
            p2.y += EntityHeight1  + RequiredSpacingAboveEitherEntity;

            Vector3 p3 = p4;
            p3.y += EntityHeight2 + RequiredSpacingAboveEitherEntity;

            if ( p2.y > p3.y )
                p3.y = p2.y;
            else if ( p2.y < p3.y )
                p2.y = p3.y;

            RenderPolyline_P4( p1, p2, p3, p4, LineColor, false, Thickness );

            //then draw the arrow from the attacker towards the target

            Vector3 heading = p3 - p2; //pointing from p2 to p3
            heading = heading.normalized;
            float cornerLengths = 0.2f;

            Vector3 arrowCenter = p2 + (heading * -cornerLengths);

            DrawShape_Arrowhead arrow;
            arrow.Color = LineColor;
            arrow.Thickness = Thickness;
            arrow.Up = Vector3.up;
            arrow.Direction = heading;
            arrow.Center = arrowCenter;
            arrow.CornerLengths = cornerLengths;

            DrawList.Add( arrow );
        }
        #endregion

        #region RenderCircle
        public static void RenderCircle( Vector3 Center, float Radius, Color LineColor, float Thickness )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;

            DrawShape_WireCircleXZ circle;
            circle.Color = LineColor;
            circle.Thickness = Thickness;
            circle.Center = Center;
            circle.Radius = Radius;

            DrawList.Add( circle );
        }
        #endregion

        #region RenderCube
        public static void RenderCube( Vector3 Center, float Size, Color LineColor, float Thickness )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;

            DrawShape_WireBox box;
            box.Color = LineColor;
            box.Thickness = Thickness;
            box.Center = Center;
            box.Size = new Vector3( Size, Size, Size );

            DrawList.Add( box );
        }
        #endregion

        #region RenderBox
        public static void RenderBox( Vector3 Center, Vector3 Size, Color LineColor, float Thickness )
        {
            if ( VisCurrent.AreAllGameIconsHidden )
                return;

            DrawShape_WireBox box;
            box.Color = LineColor;
            box.Thickness = Thickness;
            box.Center = Center;
            box.Size = Size;

            DrawList.Add( box );
        }
        #endregion
    }

    public enum EnemyTargetingReason
    {
        CurrentLocation_NoOp,
        ProposedDestination,
        CurrentLocation_PlusCloaking,
        CurrentLocation_PlusTakeCover,
        CurrentLocation_Attacking,
    }

    public enum CatmullSlotType
    {
        Move,
        Aggro,
        Prediction,
    }

    public enum CatmullSlope
    {
        Movement,
        AndroidTargeting,
        VehicleTargeting,
        MechTargeting,
        EnemyTargetingUs
    }
}
