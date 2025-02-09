using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.ExternalVis.CityLifeEffects;

namespace Arcen.HotM.ExternalVis
{
    public class BuildingOverlayTypes_Basic : IBuildingOverlayTypeImplementation
    {
        public void FillListOfSecondaryDataSource( BuildingOverlayType OverlayType, List<IBuildingOverlaySecondaryDataSource> RowsToFill, 
            out IBuildingOverlaySecondaryDataSource DefaultRow )
        {
            string filterText = Engine_HotM.GetEffectiveOverlayType_Text();

            RowsToFill.Clear();
            switch ( OverlayType.ID )
            {
                case "Normal":
                case "Districts":
                case "ResidentPercentage":
                case "WorkerPercentage":
                case "DistanceFromMachines":
                case "Pollution":
                case "DispatchZones":
                case "BuildingsInPOIs":
                    DefaultRow = null;
                    return;
				case "MatchingBuildingStatus":
                    {
                        DefaultRow = BuildingStatusTable.Instance.DefaultRow;
                        bool searchForText = filterText != null && filterText.Length > 0;
                        foreach ( BuildingStatus row in BuildingStatusTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                            {
                                if ( searchForText && !row.GetDisplayName().Contains( filterText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                case "MatchingBuildingType":
                    {
                        DefaultRow = BuildingTypeTable.Instance.DefaultRow;
                        bool searchForText = filterText != null && filterText.Length > 0;
                        foreach ( BuildingType row in BuildingTypeTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                            {
                                if ( searchForText && !row.GetDisplayName().Contains( filterText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                case "MatchingBuildingTag":
                    {
                        DefaultRow = BuildingTagTable.Instance.DefaultRow;
                        bool searchForText = filterText != null && filterText.Length > 0;
                        foreach ( BuildingTag row in BuildingTagTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                            {
                                if ( searchForText && !row.GetDisplayName().Contains( filterText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                default:
                    throw new Exception( "BuildingOverlayTypes_Basic FillListOfSecondaryDataSource: Not set up for '" + OverlayType.ID + "'!" );
            }
        }

        private static readonly Color white = Color.white;
        private static readonly Color transparent = ColorMath.Transparent;

        private static readonly Color highlight_Gold = ColorMath.FromRGB( 255, 86, 0 );
        private static readonly Color highlight_Cyan = ColorMath.FromRGB( 0, 199, 255 );
        private static readonly Color highlight_Purple = ColorMath.FromRGB( 77, 0, 111 );
        private static readonly Color highlight_FaintRed = ColorMath.FromRGB( 137, 13, 36 );

        private static readonly Color highlight_GoldFaded = ColorMath.FromRGB( 202, 2, 0 );
        private static readonly Color highlight_CyanFaded = ColorMath.FromRGB( 0, 18, 120 );
        private static readonly Color highlight_PurpleFaded = ColorMath.FromRGB( 8, 0, 255 );

        public Color GetDefaultInCurrentOverlay( BuildingOverlayType OverlayType )
        {
            switch ( OverlayType.ID )
            {
                case "Normal":
                case "Districts":
                    return white;
				case "ResidentPercentage":
                case "WorkerPercentage":
                case "DistanceFromMachines":
                case "MatchingBuildingStatus":
                case "MatchingBuildingType":
                case "MatchingBuildingTag":
                case "BuildingsInPOIs":
                    return OverlayType.ColorGradient_Positive.GetColorByIndex( 0 );
                default:
                    throw new Exception( "BuildingOverlayTypes_Basic GetDefaultInCurrentOverlay: Not set up for '" + OverlayType.ID + "'!" );
            }
        }

        private static readonly ArcenDoubleCharacterBuffer overlayCharacterBuffer = new ArcenDoubleCharacterBuffer( "BuildingOverlayTypes_Basic-overlayCharacterBuffer" );

        public Color GetColorInCurrentOverlay( ISimBuilding Building, BuildingOverlayType OverlayType, System.Diagnostics.Stopwatch CurrentCycleTime )
        {
            switch ( OverlayType.ID )
            {
                case "Normal":
                case "Districts":
	                return white;
                case "ResidentPercentage":
                    return OverlayType.ColorGradient_Positive.GetColorByIndex( Building.GetTotalResidentPercentage( false ) );
                case "WorkerPercentage":
                    return OverlayType.ColorGradient_Positive.GetColorByIndex( Building.GetTotalWorkerPercentage( false ) );
                case "DistanceFromMachines":
                    {
                        int distanceFrom = Mathf.RoundToInt( Building.RoughDistanceFromMachines );
                        if ( distanceFrom <= 0 )
                            distanceFrom = 0;
                        else
                        {
                            if ( distanceFrom > 100 )
                                distanceFrom = 100;
                        }

                        distanceFrom = 100 - distanceFrom;

                        return OverlayType.ColorGradient_Positive.GetColorByIndex( distanceFrom );
                    }
                case "MatchingBuildingStatus":
                    {
                        BuildingStatus buildingStatus = Engine_HotM.GetEffectiveOverlayType_Selection() as BuildingStatus;
                        return OverlayType.ColorGradient_Positive.GetColorByIndex( Building.GetStatus() == buildingStatus ? 100 : 0 );
                    }
                case "BuildingsInPOIs":
                    if ( Building.GetMapItem().GetParentPOIOrNull() != null )
                        return OverlayType.ColorGradient_Positive.GetColorByIndex( 100 );
                    else
                        return ColorMath.Transparent;
      //          case "BuildApplianceModeOverlay":
      //              {
      //                  ISimPlayerInventory inventory = World.PlayerInventory;
						
						//Vector3 thisBuildingCenter = Building.GetMapItem().CenterPoint;

      //                  MachineApplianceType applianceType = Engine_HotM.GetEffectiveOverlayType_Selection() as MachineApplianceType;
						//ISimMachineAppliance existingApplianceOrNull = Building.GetMachineAppliance();
      //                  MachineApplianceType existingType = existingApplianceOrNull?.GetApplianceType();
      //                  if ( existingType != null)
      //                  {
						//	//if a type already assigned
						//	if ( existingType == applianceType )
      //                      {
      //                          //if ( canAfford )
      //                              return highlight_Cyan;
      //                          //else
      //                          //    return highlight_CyanFaded;
      //                      }
      //                      else
      //                      {
      //                          //if ( canAfford )
      //                              return Building.GetPrefab().ValidBuildingApplianceSocketTypes[applianceType?.RequiredSocketType] != null ? highlight_Purple : transparent;
      //                          //else
      //                          //    return Building.GetPrefab().ValidBuildingApplianceSocketTypes[applianceType?.RequiredSocketType] != null ? highlight_PurpleFaded : transparent;
      //                      }
      //                  }
      //                  else
      //                  {
      //                      //if no type assigned
      //                      bool canBeThisType = Building.GetPrefab().ValidBuildingApplianceSocketTypes[applianceType?.RequiredSocketType] != null;

      //                      if ( applianceType != null && Building.GetMapItem() is MapItem item)
      //                      {
      //                          bool isInRangeOfCursor = false;
      //                          if ( canBeThisType )
      //                              isInRangeOfCursor = item.CenterPoint.GetLargestAbsXZDistanceFrom( Engine_HotM.MouseWorldLocation ) < 4f;

      //                          if ( canBeThisType && isInRangeOfCursor )
      //                          {
      //                              if ( item.FloatingText != null && item.FloatingText.GetIsValidToUse( item ) )
      //                              { } //already exists, so just update it
      //                              else
      //                              {
      //                                  if ( CurrentCycleTime.ElapsedMilliseconds < 4 ) //if it's been more than 4ms, then don't do more
      //                                  {
      //                                      //does not already exist, so establish it
      //                                      item.FloatingText = CommonRefs.FloatingTextBasic.GetFromPool( item );
      //                                      item.FloatingText.FontSize = 4f;
      //                                      item.FloatingText.ObjectScale = 1f; //any other scale is likely blurry!
      //                                      item.FloatingText.WorldLocation = item.OBBCache.OBB.Center;
      //                                  }
      //                              }

      //                              if ( item.FloatingText != null )
      //                              {
      //                                  item.FloatingText.MarkAsStillInUse( 0.4f );

      //                                  //if ( canAfford )
      //                                      overlayCharacterBuffer.StartColor( "a5c8be" );
      //                                  //else
      //                                  //    overlayCharacterBuffer.StartColor( "dd6f66" );

      //                                  float mul = item.SimBuilding.GetPrefab().BuildingApplianceSockets.GetFirstOrDefaultWhere(av => av.Prefab.Type == applianceType.RequiredSocketType)?.RelativeApplianceSizeMultiplierForDisplay ?? -1;
      //                                  mul = Mathf.Clamp(mul, 0.25f, 5);
						//				if (mul > 0)
						//					overlayCharacterBuffer.Space1x().AddFormat1( "BuildingMachineAppliance_RelativeApplianceSizeMultiplier", (Math.Round(mul * 2000) / 20).ToStringSmallFixedDecimal(2));

						//				item.FloatingText.Text = overlayCharacterBuffer.GetStringAndResetForNextUpdate();
      //                              }
      //                          }
      //                          else
      //                          {
      //                              if ( item.FloatingText != null && item.FloatingText.GetIsValidToUse( item ) )
      //                                  item.FloatingText.ReturnToPool();
      //                          }
      //                      }

      //                      if (!canBeThisType) 
      //                          return transparent;

      //                      return highlight_Gold; //canAfford ? highlight_Gold : highlight_GoldFaded;


      //                  }
      //              }
                case "MatchingBuildingType":
                    {
                        BuildingType buildingType = Engine_HotM.GetEffectiveOverlayType_Selection() as BuildingType;
                        return OverlayType.ColorGradient_Positive.GetColorByIndex( Building.GetPrefab().Type == buildingType ? 100 : 0 );
                    }
                case "MatchingBuildingTag":
                    {
                        BuildingTag buildingTag = Engine_HotM.GetEffectiveOverlayType_Selection() as BuildingTag;
                        return OverlayType.ColorGradient_Positive.GetColorByIndex( Building.GetVariant().Tags.ContainsKey( buildingTag.ID ) ? 100 : 0 );
                    }
                default:
                    throw new Exception( "BuildingOverlayTypes_Basic GetColorInCurrentOverlay: Not set up for '" + OverlayType.ID + "'!" );
            }
        }

        private static readonly List<MapCell> cellList = List<MapCell>.Create_WillNeverBeGCed(25, "StreetPathfinder-CellList", 15);
		public bool GetShouldBeVisibleAtMoment( BuildingOverlayType OverlayType )
        {
            switch ( OverlayType.ID )
            {
                case "Normal":
                case "Districts":
				case "ResidentPercentage":
                case "WorkerPercentage":
                case "DistanceFromMachines":
                case "MatchingBuildingStatus":
                case "MatchingBuildingType":
                case "MatchingBuildingTag":
                case "BuildingsInPOIs":
                    return true;
                //case "BuildApplianceModeOverlay":
                //    return false; //never shown in the interface
                default:
                    throw new Exception( "BuildingOverlayTypes_Basic GetShouldBeVisibleAtMoment: Not set up for '" + OverlayType.ID + "'!" );
            }
        }

        public bool GetUsesSecondaryDataSources( BuildingOverlayType OverlayType )
        {
            switch ( OverlayType.ID )
            {
                case "Normal":
                case "Districts":
				case "ResidentPercentage":
                case "WorkerPercentage":
                case "DistanceFromMachines":
                case "BuildingsInPOIs":
                    return false;
                case "MatchingBuildingStatus":
                case "MatchingBuildingType":
                case "MatchingBuildingTag":
					return true;
                default:
                    throw new Exception( "BuildingOverlayTypes_Basic GetUsesSecondaryDataSources: Not set up for '" + OverlayType.ID + "'!" );
            }
        }

        public bool GetUsesTextbox( BuildingOverlayType OverlayType )
        {
            switch ( OverlayType.ID )
            {
                case "Normal":
                case "Districts":
				case "ResidentPercentage":
                case "WorkerPercentage":
                case "DistanceFromMachines":
                case "BuildingsInPOIs":
                    return false;
                case "MatchingBuildingStatus":
                case "MatchingBuildingType":
                case "MatchingBuildingTag":
                    return true;
                default:
                    throw new Exception( "BuildingOverlayTypes_Basic GetUsesTextbox: Not set up for '" + OverlayType.ID + "'!" );
            }
        }

        public void WriteExtraTooltipInformation( ArcenDoubleCharacterBuffer buffer, ISimBuilding Building, BuildingOverlayType OverlayType )
        {
            switch ( OverlayType.ID )
			{
				case "WorkerPercentage":
					buffer.Line();
					buffer.AddFormat1("PopulationStatistics_ResidencyPercent", Building.GetTotalResidentPercentage(false))
						.Space8x().AddFormat1("PopulationStatistics_StaffingPercent", Building.GetTotalWorkerPercentage(false));
					return;
                case "DistanceFromMachines":
                    buffer.Line();
                    buffer.AddLangAndAfterLineItemHeader( "DistanceFromMachines", ColorTheme.CyanDim )
                        .AddRaw( Building.RoughDistanceFromMachines.ToStringThousandsDecimal_Optional4() );
                    return;
            }
        }
    }
}
