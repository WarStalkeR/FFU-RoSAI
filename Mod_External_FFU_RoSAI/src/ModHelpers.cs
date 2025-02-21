using Arcen.Universal;
using Arcen.HotM.Core;
using System;
using UnityEngine;
using Arcen.HotM.ExternalVis;

namespace Arcen.HotM.FFU.RoSAI {
    public static partial class ModHelpers {
        internal const float EXTRA_Y_IN_MAP_MODE = 1.04f;

        internal static void DrawMapItemHighlightedBorder(MapItem item, Color ColorForHighlight, Vector3 ScaleMult, HighlightPass Pass, bool IsMapMode, Int64 framesPrepped) {
            if (item == null) return;

            switch (Pass) {
                case HighlightPass.First: {
                    if (item.LastFramePrepRendered_Highlight >= framesPrepped) return;
                    item.LastFramePrepRendered_Highlight = framesPrepped;
                    break;
                }
                case HighlightPass.Second: {
                    if (item.LastFramePrepRendered_HighlightSecond >= framesPrepped) return;
                    item.LastFramePrepRendered_HighlightSecond = framesPrepped;
                    break;
                }
                case HighlightPass.AlwaysHappen: {
                    break;
                }
            }

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if (primaryRend == null) return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if (rendGroup == null) return;

            Matrix4x4 parentMatrix; 
            {
                Quaternion rot = item.Rotation;
                if (primaryRend.Rotates) rot *= primaryRend.RotationForInGameGlobal;
                Vector3 pos = item.Position;
                if (IsMapMode) pos.y += EXTRA_Y_IN_MAP_MODE;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor(pos, rot, 
                item.Scale.ComponentWiseMult(ScaleMult), RenderColorStyle.HighlightColor, 
                ColorForHighlight, RenderOpacity.Normal, false);
            }

            if (item.Type.SecondarysRenderersOfThisRoot.Count > 0) {
                Quaternion rot;
                for (int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++) {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if (secondaryRend == null) continue;
                    rendGroup = secondaryRend.Group;
                    if (rendGroup == null) continue;

                    rot = secondaryRend.LocalRot;
                    if (secondaryRend.Rotates) rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor(secondaryRend.LocalPos, rot, 
                    secondaryRend.LocalScale, parentMatrix, RenderColorStyle.HighlightColor, 
                    ColorForHighlight, RenderOpacity.Normal, false);
                }
            }
        }

        internal static bool IsNull(this object refObject) {
            return refObject == null;
        }

        internal static bool IsLoaded(this ArcenTableInitializationStage initStage) {
            return initStage == ArcenTableInitializationStage.XmlLoadingDone;
        }

        internal static bool IsComplete(this ArcenTableInitializationStage initStage) {
            return initStage == ArcenTableInitializationStage.FullyComplete;
        }
    }
}