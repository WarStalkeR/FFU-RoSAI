using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using System.Runtime.CompilerServices;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public static class RenderManager_TheEndOfTime
    {
        #region PreRenderFrame
        public static void PreRenderFrame()
        {
            if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
            {
                return;
            }

            SharedRenderManagerData.stopwatch.Restart();

            Color defaultColor = SharedRenderManagerData.defaultColor_Global;

            Int64 renderManagerFramesPrepped = RenderManager.FramesPrepped;

            DrawItemList( EndOfTimeMap.RockOutcrops, UseColorType.Normal, defaultColor, FrameBufferManagerData.MajorDecorationCount );
            DrawItemList( EndOfTimeMap.Ziggurats, UseColorType.Normal, defaultColor, FrameBufferManagerData.OtherSkeletonItemCount );
            DrawTimelines( UseColorType.Normal, defaultColor, FrameBufferManagerData.BuildingMainCount );
            DrawItemList( EndOfTimeMap.Portals, UseColorType.Normal, defaultColor, FrameBufferManagerData.MinorDecorationCount );
            DrawRisingRocks( UseColorType.Normal, defaultColor, FrameBufferManagerData.RoadCount );

            if ( CityMap.MaterializingItems_MainThreadOnly.Count > 0 ) //do them all!
            {
                FrameBufferManagerData.FadingCount.Construction += CityMap.MaterializingItems_MainThreadOnly.Count;

                for ( int i = CityMap.MaterializingItems_MainThreadOnly.Count - 1; i >= 0; i-- )
                {
                    if ( CityMap.MaterializingItems_MainThreadOnly[i].DoPerFrame( renderManagerFramesPrepped ) ) //returns true when it is done
                        CityMap.MaterializingItems_MainThreadOnly.RemoveAt( i, true );
                }
            }

            RenderHelper_EventCamera.RenderIfNeeded();
            A5ObjectAggregation.FloatingIconListPool.DrawAllActiveInstructions();
            A5ObjectAggregation.FloatingIconColliderPool.DrawAllActiveItems();

            SharedRenderManagerData.stopwatch.Stop();
        }
        #endregion

        #region DrawItemList
        private static void DrawItemList( List<EndOfTimeItem> Items, UseColorType colorTypeToUse, Color defaultColor, DoubleBufferedValue<int> CountType )
        {
            foreach ( EndOfTimeItem item in Items )
            {
                if ( TryDrawEndOfTimeItem( item, colorTypeToUse, defaultColor ) )
                    CountType.Construction++;
            }
        }
        #endregion

        #region TryDrawEndOfTimeItem
        internal static bool TryDrawEndOfTimeItem( EndOfTimeItem item, UseColorType ColorType, Color ColorForVis )
        {
            if ( item == null )
                return false;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            if ( item.LastFramePrepRendered_General >= RenderManager.FramesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation + 
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return false;
            }
            item.LastFramePrepRendered_General = RenderManager.FramesPrepped;

            float extraY = 0;

            RenderColorStyle colorStyle = RenderColorStyle.NoColor;
            RenderOpacity opacity = RenderOpacity.Normal;
            switch (ColorType)
            {
                case UseColorType.VisColor:
                    colorStyle = RenderColorStyle.VisColor;
                    break;
                case UseColorType.ColorOverride:
                    colorStyle = RenderColorStyle.SelfColor;
                    break;
            }

            MatrixCache matrixCache = item.OBBCache.Normal_MatrixCache;

            Matrix4x4 parentMatrix;
            if ( matrixCache.HasMatrixBeenSet && !primaryRend.Rotates && extraY == 0 )
            {
                parentMatrix = matrixCache.MatrixForRendering;
                rendGroup.WriteToDrawBufferForOneFrame_BasicColor( parentMatrix, colorStyle, ColorForVis, opacity, false );
            }
            else
            {
                Quaternion rot = item.Rotation;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                Vector3 pos = item.Position;
                pos.y += extraY;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, item.Scale, colorStyle, ColorForVis, opacity, false );
                if ( !primaryRend.Rotates )
                {
                    matrixCache.MatrixForRendering = parentMatrix;
                    matrixCache.HasMatrixBeenSet = true;
                }
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, colorStyle, ColorForVis, opacity, false );
                }
            }
            return true;
        }
        #endregion

        #region DrawTimelines
        private static void DrawTimelines( UseColorType colorTypeToUse, Color defaultColor, DoubleBufferedValue<int> CountType )
        {
            foreach ( KeyValuePair<int, CityTimeline> kv in SimMetagame.AllTimelines )
            {
                if ( !kv.Value.IsValidToDraw ) 
                    continue;
                if ( TryDrawTimeline( kv.Value, colorTypeToUse, defaultColor ) )
                    CountType.Construction++;
            }
        }
        #endregion

        #region TryDrawTimeline
        internal static bool TryDrawTimeline( CityTimeline item, UseColorType ColorType, Color ColorForVis )
        {
            if ( item == null )
                return false;

            PrimaryRenderer primaryRend = item.ObjectRoot?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            if ( item.LastFramePrepRendered_General >= RenderManager.FramesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation + 
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return false;
            }
            item.LastFramePrepRendered_General = RenderManager.FramesPrepped;

            float extraY = 0;

            RenderColorStyle colorStyle = RenderColorStyle.NoColor;
            RenderOpacity opacity = RenderOpacity.Normal;
            switch ( ColorType )
            {
                case UseColorType.VisColor:
                    colorStyle = RenderColorStyle.VisColor;
                    break;
                case UseColorType.ColorOverride:
                    colorStyle = RenderColorStyle.SelfColor;
                    break;
            }

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.Rotation;
                Vector3 pos = item.Position;
                pos.y += extraY;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, item.ObjectRoot.OriginalScale, colorStyle, ColorForVis, opacity, false );
            }

            if ( item.ObjectRoot.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.ObjectRoot.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.ObjectRoot.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, colorStyle, ColorForVis, opacity, false );
                }
            }
            return true;
        }
        #endregion

        #region TryDrawTimelineGhost
        internal static bool TryDrawTimelineGhost( EndOfTimeSubItemPosition item, A5ObjectRoot cityGhost )
        {
            if ( item.SlotIndex < 0 )
                return false;

            PrimaryRenderer primaryRend = cityGhost?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            RenderColorStyle colorStyle = RenderColorStyle.NoColor;
            RenderOpacity opacity = RenderOpacity.Normal;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.Rotation;
                Vector3 pos = item.Position;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, cityGhost.OriginalScale, colorStyle, ColorMath.White, opacity, false );
            }

            if ( cityGhost.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < cityGhost.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = cityGhost.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, colorStyle, ColorMath.White, opacity, false );
                }
            }
            return true;
        }
        #endregion

        #region DrawRisingRocks
        private static void DrawRisingRocks( UseColorType colorTypeToUse, Color defaultColor, DoubleBufferedValue<int> CountType )
        {
            foreach ( EndOfTimeRisingRock rock in EndOfTimeMap.RisingRocks )
            {
                if ( TryDrawRisingRock_Rock( rock, colorTypeToUse, defaultColor ) )
                {
                    CountType.Construction++;
                    //TryDrawRisingRock_Water( rock );
                }
            }
        }
        #endregion

        #region TryDrawRisingRock_Rock
        internal static bool TryDrawRisingRock_Rock( EndOfTimeRisingRock item, UseColorType ColorType, Color ColorForVis )
        {
            if ( item == null )
                return false;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            if ( item.LastFramePrepRendered_Rock >= RenderManager.FramesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation + 
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return false;
            }
            item.LastFramePrepRendered_Rock = RenderManager.FramesPrepped;

            float extraY = 0;
            //}

            RenderColorStyle colorStyle = RenderColorStyle.NoColor;
            RenderOpacity opacity = RenderOpacity.Normal;
            switch ( ColorType )
            {
                case UseColorType.VisColor:
                    colorStyle = RenderColorStyle.VisColor;
                    break;
                case UseColorType.ColorOverride:
                    colorStyle = RenderColorStyle.SelfColor;
                    break;
            }

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.Rotation;
                Vector3 pos = item.Position;
                pos.y += extraY;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, item.Type.OriginalScale, colorStyle, ColorForVis, opacity, false );
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, colorStyle, ColorForVis, opacity, false );
                }
            }
            return true;
        }
        #endregion

        #region TryDrawRisingRock_Water
        internal static bool TryDrawRisingRock_Water( EndOfTimeRisingRock item )
        {
            if ( item == null || item.WaterPosition.y <= MathRefs.EndOfTimeRisingRocksWaterMinDrawHeight.IntMin )
                return false;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            if ( item.LastFramePrepRendered_Water >= RenderManager.FramesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation + 
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return false;
            }
            item.LastFramePrepRendered_Water = RenderManager.FramesPrepped;

            {
                Quaternion rot = item.Rotation;
                Vector3 pos = item.WaterPosition;

                rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, item.Type.OriginalScale.ComponentWiseMult( item.WaterScale ), 
                    RenderColorStyle.WaterDistortionUncolored, 
                    new Color( 0.1f, 0.1f, 0.1f, 0.01f ), RenderOpacity.Transparent_Sorted, false );
            }
            return true;
        }
        #endregion

        #region GetIsPointInRangeEfficient
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private static bool GetIsPointInRangeEfficient( Vector3 Origin, Vector3 TestPoint, float Range, float RangeSquared )
        {
            float num = TestPoint.x - Origin.x;
            if ( num < 0f )
            {
                num = 0f - num;
            }

            if ( num > Range )
            {
                return false;
            }

            float num2 = TestPoint.z - Origin.z;
            if ( num2 < 0f )
            {
                num2 = 0f - num2;
            }

            if ( num2 > Range )
            {
                return false;
            }

            float num3 = num * num + num2 * num2;
            return num3 <= RangeSquared;
        }
        #endregion
    }
}
