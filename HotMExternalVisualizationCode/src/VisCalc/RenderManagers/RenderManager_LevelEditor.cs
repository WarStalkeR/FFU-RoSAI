using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public static class RenderManager_LevelEditor
    {
        #region RenderFrame
        public static void RenderFrame( Camera TargetCamera, Camera IconMixedInCamera, Camera IconOverlayCamera )
        {
            if ( !Engine_HotM.OverrideShowAllBuildingMarkers )
                return; //nothing to do, looks like

            foreach ( A5ObjectRoot root in A5ObjectAggregation.WithBuildings )
            {
                if ( root.InEditorInstances_IncludesDeleted.Count == 0 )
                    continue; //skip it, there's no instances

                foreach ( IA5Placeable iPlace in root.InEditorInstances_IncludesDeleted )
                {
                    A5Placeable place = iPlace as A5Placeable;
                    DrawDebugBuildingMarkerOnA5Placeable( place );
                }
            }

            //don't actually care if this is post ui or not, this is just the level editor
            SharedRenderManagerData.ComeBackAndFinishFrameBuffersAndRender_PostUI( TargetCamera, IconMixedInCamera, IconOverlayCamera );
        }
        #endregion

        #region DrawDebugBuildingMarkerOnA5Placeable
        private static void DrawDebugBuildingMarkerOnA5Placeable( A5Placeable parentItem )
        {
            if ( !parentItem || !parentItem.IsObjectActive )
                return;
            A5ObjectRoot root = parentItem?.ObjRoot;
            if ( root == null )
                return;

            BuildingPrefab buildingPrefab = parentItem.ObjRoot.Building;
            if ( buildingPrefab.MarkerPrefab == null )
            {
                //ArcenDebugging.LogSingleLine( parentItem.name + " FAIL 1: " + buildingPrefab.MarkerUsages.Count, Verbosity.DoNotShow );
                return; //nevermind!
            }

            BuildingMarkerColor markerColor = BuildingMarkerColorTable.Instance.DefaultRow;
            if ( markerColor == null )
            {
                ArcenDebugging.LogSingleLine( "FAIL, No Default BuildingMarkerColor", Verbosity.DoNotShow );
                return;
            }

            A5ObjectRoot markerPlace = buildingPrefab.MarkerPrefab.PlaceableRoot;

            PrimaryRenderer primaryRend = markerPlace?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
            {
                ArcenDebugging.LogSingleLine( parentItem.name + " FAIL 2: missing FirstRendererOfThisRoot", Verbosity.DoNotShow );
                return;
            }
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
            {
                ArcenDebugging.LogSingleLine( parentItem.name + " FAIL 3: missing IA5RendererGroup", Verbosity.DoNotShow );
                return;
            }

            if ( !parentItem.OBBAndBoundsCache.HasBeenSet )
            {
                ArcenDebugging.LogSingleLine( parentItem.name + " FAIL 4: !OBBAndBoundsCache.HasBeenSet", Verbosity.DoNotShow );
                return;
            }

            //ArcenDebugging.LogSingleLine( "try to draw 2", Verbosity.Note );

            OBBUnity obb = parentItem.OBBAndBoundsCache.OBB;
            Vector3 pos = obb.TopCenter;
            pos += (obb.Rotation * buildingPrefab.MarkerOffset);

            Quaternion finalRot = obb.Rotation;
            if ( buildingPrefab.MarkerHasRotation )
                finalRot *= buildingPrefab.MarkerRotation;

            if ( primaryRend.Rotates )
                finalRot *= primaryRend.RotationForInGameGlobal;

            Vector3 scale = markerPlace.OriginalScale;
            if ( buildingPrefab.MarkerPrefab.ScaleMultiplier > 0 )
                scale *= buildingPrefab.MarkerPrefab.ScaleMultiplier;
            if ( buildingPrefab.MarkerScaleMultiplier > 0 )
                scale *= buildingPrefab.MarkerScaleMultiplier;

            Color emissionColor = markerColor.ColorHDR;
            if ( buildingPrefab.MarkerPrefab.ColorIntensityMultiplier > 0 )
                emissionColor *= buildingPrefab.MarkerPrefab.ColorIntensityMultiplier;

            Matrix4x4 parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, finalRot, scale,
                RenderColorStyle.EmissiveColor, emissionColor, RenderOpacity.Normal, false );

            if ( markerPlace.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                for ( int i = 0; i < markerPlace.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = markerPlace.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    finalRot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        finalRot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, finalRot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.EmissiveColor, emissionColor, RenderOpacity.Normal, false );
                }
            }
        }
        #endregion
    }
}
