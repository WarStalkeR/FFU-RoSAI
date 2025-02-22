using Arcen.HotM.Core;
using Arcen.Universal;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace Arcen.HotM.FFU.RoSAI {
    public static partial class ModPatch {
        public static VisSimpleDrawingObject FloorCubePurple;
        public static VisSimpleDrawingObject FloorCubeRed;
        public static VisSimpleDrawingObject FloorCubeGreen;
        public static VisSimpleDrawingObject FloorCubeOrange;
        public static VisSimpleDrawingObject GetFloorCubePurple => AccessTools.StaticFieldRefAccess<VisSimpleDrawingObject>(typeof(ExternalVis.RenderHelper_Objects), "FloorCubePurple");
        public static VisSimpleDrawingObject GetFloorCubeRed => AccessTools.StaticFieldRefAccess<VisSimpleDrawingObject>(typeof(ExternalVis.RenderHelper_Objects), "FloorCubeRed");
        public static VisSimpleDrawingObject GetFloorCubeGreen => AccessTools.StaticFieldRefAccess<VisSimpleDrawingObject>(typeof(ExternalVis.RenderHelper_Objects), "FloorCubeGreen");
        public static VisSimpleDrawingObject GetFloorCubeOrange => AccessTools.StaticFieldRefAccess<VisSimpleDrawingObject>(typeof(ExternalVis.RenderHelper_Objects), "FloorCubeOrange");
        public static bool DrawFloorsInner_IsLoaded() {
            return !GetFloorCubeOrange.IsNull();
        }
        public static void DrawFloorsInner_LoadRefs() {
            FloorCubePurple = GetFloorCubePurple;
            FloorCubeRed = GetFloorCubeRed;
            FloorCubeGreen = GetFloorCubeGreen;
            FloorCubeOrange = GetFloorCubeOrange;
        }
        public static void CallDrawFloorCube(VisSimpleDrawingObject floorCubePurple, 
            BuildingFloor floor, Vector3 pos, Quaternion rotation) {
            MethodInfo drawFloorCubeMethod = AccessTools.Method(
                typeof(ExternalVis.RenderHelper_Objects),
                "DrawFloorCube",
                new Type[] {
                    typeof(VisSimpleDrawingObject),
                    typeof(BuildingFloor),
                    typeof(Vector3),
                    typeof(Quaternion)
                });
            if (drawFloorCubeMethod != null) {
                drawFloorCubeMethod.Invoke(null,new object[] { 
                    floorCubePurple, floor, pos, rotation 
                });
            }
        }
        public static bool DrawFloorsInner_FixPre(MapItem mapItem, BuildingPrefab buildingPref) {
            Vector3 center = mapItem.OBBCache.Center;
            center += mapItem.OBBCache.GetOBB_ExpensiveToUse().Rotation * buildingPref.FloorsOffset;
            float yBot = (mapItem.Type.AlwaysDropTo >= -900f) ? 0f : mapItem.OBBCache.BottomCenter.y;
            Quaternion rotation = mapItem.OBBCache.GetOBB_ExpensiveToUse().Rotation;
            float yPos = yBot;
            int maxFloorToDrawAsAbnormalBasement = -10000;
            int minFloorToDrawAsAbnormalBasement = 10000;
            int minFloorToDrawAsAbnormalUpper = 10000;
            int minFloor = buildingPref.MinFloor;
            yPos = yBot;
            DrawShape_WireBoxOriented wireBox = default;
            DrawShape_WireBoxOriented wireBox2 = default;
            for (int floorIndex = -1; floorIndex >= minFloor; floorIndex--) {
                BuildingFloor floor = buildingPref.BuildingFloors[floorIndex];
                yPos -= floor.FloorSize.y / 2f;
                Vector3 scale = floor.FloorSize.ComponentWiseMult(new Vector3(1f, 0.99f, 1f));
                Vector3 pos = new Vector3(center.x, yPos, center.z);
                yPos -= floor.FloorSize.y / 2f;
                if (floorIndex >= minFloorToDrawAsAbnormalBasement || floorIndex < maxFloorToDrawAsAbnormalBasement) {
                    wireBox.Center = pos;
                    wireBox.Rotation = rotation;
                    wireBox.Size = scale;
                    wireBox.Color = ColorMath.Purple;
                    wireBox.Thickness = 1f;
                    VisCurrent.PerFloorWireShapes.Add(wireBox);
                    CallDrawFloorCube(FloorCubePurple, floor, pos, rotation);
                } else {
                    wireBox2.Center = pos;
                    wireBox2.Rotation = rotation;
                    wireBox2.Size = scale;
                    wireBox2.Color = ColorMath.Red;
                    wireBox2.Thickness = 1f;
                    VisCurrent.PerFloorWireShapes.Add(wireBox2);
                    CallDrawFloorCube(FloorCubeRed, floor, pos, rotation);
                }
            }
            yPos = yBot;
            DrawShape_WireBoxOriented wireBox3 = default;
            DrawShape_WireBoxOriented wireBox4 = default;
            for (int i = 0; i <= buildingPref.MaxFloor; i++) {
                BuildingFloor floor2 = buildingPref.BuildingFloors[i];
                yPos += floor2.FloorSize.y / 2f;
                Vector3 scale2 = floor2.FloorSize.ComponentWiseMult(new Vector3(1f, 0.99f, 1f));
                Vector3 pos2 = new Vector3(center.x, yPos, center.z);
                yPos += floor2.FloorSize.y / 2f;
                if (i > minFloorToDrawAsAbnormalUpper) {
                    wireBox3.Center = pos2;
                    wireBox3.Rotation = rotation;
                    wireBox3.Size = scale2;
                    wireBox3.Color = ColorMath.LeafGreen;
                    wireBox3.Thickness = 1f;
                    VisCurrent.PerFloorWireShapes.Add(wireBox3);
                    CallDrawFloorCube(FloorCubeGreen, floor2, pos2, rotation);
                } else {
                    wireBox4.Center = pos2;
                    wireBox4.Rotation = rotation;
                    wireBox4.Size = scale2;
                    wireBox4.Color = ColorMath.Gold;
                    wireBox4.Thickness = 1f;
                    VisCurrent.PerFloorWireShapes.Add(wireBox4);
                    CallDrawFloorCube(FloorCubeOrange, floor2, pos2, rotation);
                }
            }
            return false;
        }
    }
}