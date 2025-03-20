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
    public static class RenderHelper_MapItems
    {
        #region TryDrawMapItemFogOfWar_AlreadyValidated
        public static bool TryDrawMapItemFogOfWar_AlreadyValidated( MapItem item, MapCellDrawGroup DrawGroup, Int64 framesPrepped, Color effectiveColor )
        {
            item.LastFramePrepRendered_General = framesPrepped;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            float extraY = 0f;
            if ( item.RiseSpeed > 0 )
            {
                if ( item.NonSimDrawOffset < 0 )
                {
                    item.NonSimDrawOffset += ArcenTime.UnpausedDeltaTime * item.RiseSpeed;
                    if ( item.NonSimDrawOffset >= 0 )
                    {
                        item.NonSimDrawOffset = -10000;
                        item.RiseSpeed = -1;
                        if ( item.AfterRiseComplete != null )
                        {
                            item.AfterRiseComplete.DuringGame_PlayAtLocation( item.CenterPoint );
                            item.AfterRiseComplete = null;
                        }
                    }
                    else
                        extraY += item.NonSimDrawOffset;
                }
            }

            bool isOpaque = DrawGroup.DrawsWithFullOpacity;

            MatrixCache matrixCache = item.OBBCache.Normal_MatrixCache;

            Matrix4x4 parentMatrix;
            if ( matrixCache.HasMatrixBeenSet && !primaryRend.Rotates && extraY == 0 )
            {
                parentMatrix = matrixCache.MatrixForRendering;
                if ( isOpaque )
                    rendGroup.WriteToDrawBufferForOneFrame_FogOfWarOpaque( parentMatrix );
                else
                    rendGroup.WriteToDrawBufferForOneFrame_FogOfWarFading( parentMatrix, effectiveColor );
            }
            else
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                Vector3 pos = item.rawReadPos;
                pos.y += extraY;

                if ( isOpaque )
                    parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_FogOfWarOpaque( pos, rot,
                        item.Scale//.ComponentWiseMult( SharedRenderManagerData.highlight_ScaleMult ), //we only do this here, not on the children, because their scale will be relative to this
                        );
                else
                    parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_FogOfWarFading( pos, rot,
                        item.Scale,//.ComponentWiseMult( SharedRenderManagerData.highlight_ScaleMult ), //we only do this here, not on the children, because their scale will be relative to this
                        effectiveColor );

                if ( !primaryRend.Rotates )
                {
                    matrixCache.MatrixForRendering = parentMatrix;
                    matrixCache.HasMatrixBeenSet = true;
                }
            }

            List<SecondaryRenderer> secondaries = item.Type.SecondarysRenderersOfThisRoot;
            if ( secondaries.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < secondaries.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = secondaries[i];
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    Matrix4x4 secondaryMatrix = parentMatrix * Matrix4x4.TRS( secondaryRend.LocalPos, rot, secondaryRend.LocalScale );
                    if ( isOpaque )
                        rendGroup.WriteToDrawBufferForOneFrame_FogOfWarOpaque( secondaryMatrix );
                    else
                        rendGroup.WriteToDrawBufferForOneFrame_FogOfWarFading( secondaryMatrix, effectiveColor );
                }
            }
            return true;
        }
        #endregion

        #region TryDrawMapItemBurned_AlreadyValidated
        public static bool TryDrawMapItemBurned_AlreadyValidated( MapItem item, MapCellDrawGroup DrawGroup, BurnedRuinsType BurnType, Int64 framesPrepped )
        {
            item.LastFramePrepRendered_General = framesPrepped;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            RenderColorStyle colorStyle = RenderColorStyle.BurnRuinsMasked_Building;
            switch ( BurnType )
            {
                case BurnedRuinsType.Road:
                    colorStyle = RenderColorStyle.BurnRuinsMasked_Road;
                    break;
                case BurnedRuinsType.Other:
                    colorStyle = RenderColorStyle.BurnRuinsMasked_Other;
                    break;
                case BurnedRuinsType.Decoration:
                    colorStyle = RenderColorStyle.BurnRuinsMasked_Decoration;
                    break;
            }

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                //if ( primaryRend.Rotates ) no rotation of burned things!
                //    rot *= primaryRend.RotationForInGameGlobal;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( item.rawReadPos, rot,
                    item.Scale.ComponentWiseMult( SharedRenderManagerData.highlight_ScaleMult ), //we only do this here, not on the children, because their scale will be relative to this
                    colorStyle, item.NonSimBurnMaskOffset );
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
                    //if ( secondaryRend.Rotates ) no rotation of burned things!
                    //    rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_MaskOffset( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, colorStyle, item.NonSimBurnMaskOffset );
                }
            }
            return true;
        }
        #endregion

        #region TryDrawMapItemSimple_AlreadyValidated
        public static bool TryDrawMapItemSimple_AlreadyValidated( MapItem item, MapCellDrawGroup DrawGroup, Color effectiveColorIfTransparent,
            bool IsSortedIfTransparent, bool IsMapMode, float extraY, Int64 framesPrepped )
        {
            //if ( item.Type.AnimatesAndSoAlwaysIsGameObject && item.PhysicalPlaceableObjectDuringGameplay != null )
            //{
            //    FrameBufferManagerData.AnimatedObjectCount.Construction++;
            //    return true; //this happens for things that are meant to draw directly because they have animators
            //}

            item.LastFramePrepRendered_General = framesPrepped;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            if ( item.RiseSpeed > 0 )
            {
                if ( item.NonSimDrawOffset < 0 )
                {
                    item.NonSimDrawOffset += ArcenTime.UnpausedDeltaTime * item.RiseSpeed;
                    if ( item.NonSimDrawOffset >= 0 )
                    {
                        item.NonSimDrawOffset = -10000;
                        item.RiseSpeed = -1;
                        if ( item.AfterRiseComplete != null )
                        {
                            item.AfterRiseComplete.DuringGame_PlayAtLocation( item.CenterPoint );
                            item.AfterRiseComplete = null;
                        }
                    }
                    else
                        extraY += item.NonSimDrawOffset;
                }
            }

            bool isOpaque = DrawGroup.DrawsWithFullOpacity;

            MatrixCache matrixCache = IsMapMode ? item.OBBCache.Map_MatrixCache : item.OBBCache.Normal_MatrixCache;

            Matrix4x4 parentMatrix;
            if ( matrixCache.HasMatrixBeenSet && !primaryRend.Rotates && !IsMapMode && extraY == 0 )
            {
                parentMatrix = matrixCache.MatrixForRendering;
                if ( isOpaque )
                    rendGroup.WriteToDrawBufferForOneFrame_SimpleNoColorOpaque( parentMatrix );
                else
                    rendGroup.WriteToDrawBufferForOneFrame_SimpleNoColorFading( parentMatrix, effectiveColorIfTransparent, IsSortedIfTransparent );
            }
            else
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                Vector3 pos = item.rawReadPos;
                if ( IsMapMode )
                    pos.y += RenderManager_Streets.EXTRA_Y_IN_MAP_MODE;
                pos.y += extraY;

                if ( isOpaque )
                    parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_SimpleNoColorOpaque( pos, rot, item.Scale );
                else
                    parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_SimpleNoColorFading( pos, rot, item.Scale, effectiveColorIfTransparent, IsSortedIfTransparent );
                if ( !primaryRend.Rotates )
                {
                    matrixCache.MatrixForRendering = parentMatrix;
                    matrixCache.HasMatrixBeenSet = true;
                }
            }

            List<SecondaryRenderer> secondaries = item.Type.SecondarysRenderersOfThisRoot;
            if ( secondaries.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < secondaries.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = secondaries[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    Matrix4x4 secondaryMatrix = parentMatrix * Matrix4x4.TRS( secondaryRend.LocalPos, rot, secondaryRend.LocalScale );
                    if ( isOpaque )
                        rendGroup.WriteToDrawBufferForOneFrame_SimpleNoColorOpaque( secondaryMatrix );
                    else
                        rendGroup.WriteToDrawBufferForOneFrame_SimpleNoColorFading( secondaryMatrix, effectiveColorIfTransparent, IsSortedIfTransparent );
                }
            }
            return true;
        }
        #endregion

        #region TryDrawMapItemVisualization_AlreadyValidated
        public static bool TryDrawMapItemVisualization_AlreadyValidated( MapItem item, MapCellDrawGroup DrawGroup, Color ColorForVis,
            bool IsSortedIfTransparent, bool IsMapMode, float extraY, Int64 framesPrepped )
        {
            item.LastFramePrepRendered_General = framesPrepped;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            if ( item.RiseSpeed > 0 )
            {
                if ( item.NonSimDrawOffset < 0 )
                {
                    item.NonSimDrawOffset += ArcenTime.UnpausedDeltaTime * item.RiseSpeed;
                    if ( item.NonSimDrawOffset >= 0 )
                    {
                        item.NonSimDrawOffset = -10000;
                        item.RiseSpeed = -1;
                        if ( item.AfterRiseComplete != null )
                        {
                            item.AfterRiseComplete.DuringGame_PlayAtLocation( item.CenterPoint );
                            item.AfterRiseComplete = null;
                        }
                    }
                    else
                        extraY += item.NonSimDrawOffset;
                }
            }

            bool isOpaque = DrawGroup.DrawsWithFullOpacity;

            MatrixCache matrixCache = IsMapMode ? item.OBBCache.Map_MatrixCache : item.OBBCache.Normal_MatrixCache;

            Matrix4x4 parentMatrix;
            if ( matrixCache.HasMatrixBeenSet && !primaryRend.Rotates && !IsMapMode && extraY == 0 )
            {
                parentMatrix = matrixCache.MatrixForRendering;
                if ( isOpaque )
                    rendGroup.WriteToDrawBufferForOneFrame_VisColorOpaque( parentMatrix, ColorForVis );
                else
                    rendGroup.WriteToDrawBufferForOneFrame_VisColorFading( parentMatrix, ColorForVis, IsSortedIfTransparent );
            }
            else
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                Vector3 pos = item.rawReadPos;
                if ( IsMapMode )
                    pos.y += RenderManager_Streets.EXTRA_Y_IN_MAP_MODE;
                pos.y += extraY;

                if ( isOpaque )
                    parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_VisColorOpaque( pos, rot, item.Scale, ColorForVis );
                else
                    parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_VisColorFading( pos, rot, item.Scale, ColorForVis, IsSortedIfTransparent );
                if ( !primaryRend.Rotates )
                {
                    matrixCache.MatrixForRendering = parentMatrix;
                    matrixCache.HasMatrixBeenSet = true;
                }
            }

            List<SecondaryRenderer> secondaries = item.Type.SecondarysRenderersOfThisRoot;
            if ( secondaries.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < secondaries.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = secondaries[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    Matrix4x4 secondaryMatrix = parentMatrix * Matrix4x4.TRS( secondaryRend.LocalPos, rot, secondaryRend.LocalScale );
                    if ( isOpaque )
                        rendGroup.WriteToDrawBufferForOneFrame_VisColorOpaque( secondaryMatrix, ColorForVis );
                    else
                        rendGroup.WriteToDrawBufferForOneFrame_VisColorFading( secondaryMatrix, ColorForVis, IsSortedIfTransparent );
                }
            }
            return true;
        }
        #endregion
    }
}
