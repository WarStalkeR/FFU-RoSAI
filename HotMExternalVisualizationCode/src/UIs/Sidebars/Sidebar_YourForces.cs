using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_YourForces : Sidebar_Base, IUISidebarTypeImplementation
    {
        private UISidebarBasicItem item_VehiclesHeader;
        private UISidebarBasicItem item_DeployedAndroidsHeader;
        private UISidebarBasicItem item_DeployedMechsHeader;
        private UISidebarBasicItem item_BulkUnitsHeader;
        private UISidebarBasicItem item_ConvertedUnitsHeader;

        #region PopulateItemsIfNeeded
        private void PopulateItemsIfNeeded()
        {
            if ( item_VehiclesHeader != null )
                return;

            item_VehiclesHeader = UISidebarBasicItemTable.Instance.GetRowByID( "VehiclesHeader" );
            item_DeployedAndroidsHeader = UISidebarBasicItemTable.Instance.GetRowByID( "DeployedAndroidsHeader" );
            item_DeployedMechsHeader = UISidebarBasicItemTable.Instance.GetRowByID( "DeployedMechsHeader" );
            item_BulkUnitsHeader = UISidebarBasicItemTable.Instance.GetRowByID( "BulkUnitsHeader" );
            item_ConvertedUnitsHeader = UISidebarBasicItemTable.Instance.GetRowByID( "ConvertedUnitsHeader" );
        }
        #endregion

        private readonly List<ISimMachineVehicle> vehiclesWithRiders = List<ISimMachineVehicle>.Create_WillNeverBeGCed( 10, "Sidebar_YourForces-vehiclesWithRiders" );

        public void WriteAnySidebarItems( ref float currentY )
        {
            if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
            {
                Window_Sidebar.Instance.Close( WindowCloseReason.ShowingRefused );
                return;
            }

            PopulateItemsIfNeeded();

            vehiclesWithRiders.Clear();

            if ( SimCommon.AllSortedPlayerVehicles.Count > 0 )
            {
                AddItem( item_VehiclesHeader, ref currentY );

                foreach ( ISimMachineVehicle vehicle in SimCommon.AllSortedPlayerVehicles.GetDisplayList() )
                {
                    if ( vehicle.SidebarItem_MainMapActor == null )
                        vehicle.SidebarItem_MainMapActor = new SidebarItemFromOther<ISimMapActor>( vehicle, Sidebar_MainMapActor.Instance );
                    AddItem( vehicle.SidebarItem_MainMapActor, ref currentY );

                    if ( vehicle.GetStoredUnits().Count > 0 )
                        vehiclesWithRiders.Add( vehicle );
                }
            }

            if ( SimCommon.AllSortedDeployedPlayerAndroids.Count > 0 )
            {
                AddItem( item_DeployedAndroidsHeader, ref currentY );

                foreach ( ISimMachineUnit unit in SimCommon.AllSortedDeployedPlayerAndroids.GetDisplayList() )
                {
                    if ( unit.SidebarItem_MainMapActor == null )
                        unit.SidebarItem_MainMapActor = new SidebarItemFromOther<ISimMapActor>( unit, Sidebar_MainMapActor.Instance );
                    AddItem( unit.SidebarItem_MainMapActor, ref currentY );
                }
            }

            if ( SimCommon.AllSortedDeployedPlayerMechs.Count > 0 )
            {
                AddItem( item_DeployedMechsHeader, ref currentY );

                foreach ( ISimMachineUnit unit in SimCommon.AllSortedDeployedPlayerMechs.GetDisplayList() )
                {
                    if ( unit.SidebarItem_MainMapActor == null )
                        unit.SidebarItem_MainMapActor = new SidebarItemFromOther<ISimMapActor>( unit, Sidebar_MainMapActor.Instance );
                    AddItem( unit.SidebarItem_MainMapActor, ref currentY );
                }
            }

            if ( vehiclesWithRiders.Count > 0 )
            {
                foreach ( ISimMachineVehicle vehicle in vehiclesWithRiders )
                {
                    if ( vehicle.SidebarHeader_VehicleContainer == null )
                        vehicle.SidebarHeader_VehicleContainer = new SidebarItemFromOther<ISimMachineVehicle>( vehicle, Sidebar_VehicleHeader.Instance );

                    AddItem( vehicle.SidebarHeader_VehicleContainer, ref currentY );

                    foreach ( ISimMachineUnit unit in vehicle.GetStoredUnits() )
                    {
                        if ( unit.SidebarItem_MainMapActor == null )
                            unit.SidebarItem_MainMapActor = new SidebarItemFromOther<ISimMapActor>( unit, Sidebar_MainMapActor.Instance );
                        AddItem( unit.SidebarItem_MainMapActor, ref currentY );
                    }
                }
            }

            if ( SimCommon.AllSortedPlayerBulkNPCUnits.Count > 0 )
            {
                AddItem( item_BulkUnitsHeader, ref currentY );

                foreach ( ISimNPCUnit unit in SimCommon.AllSortedPlayerBulkNPCUnits.GetDisplayList() )
                {
                    if ( unit.SidebarItem_MainMapActor == null )
                        unit.SidebarItem_MainMapActor = new SidebarItemFromOther<ISimMapActor>( unit, Sidebar_MainMapActor.Instance );
                    AddItem( unit.SidebarItem_MainMapActor, ref currentY );
                }
            }

            if ( SimCommon.AllSortedPlayerConvertedNPCUnits.Count > 0 )
            {
                AddItem( item_ConvertedUnitsHeader, ref currentY );

                foreach ( ISimNPCUnit unit in SimCommon.AllSortedPlayerConvertedNPCUnits.GetDisplayList() )
                {
                    if ( unit.SidebarItem_MainMapActor == null )
                        unit.SidebarItem_MainMapActor = new SidebarItemFromOther<ISimMapActor>( unit, Sidebar_MainMapActor.Instance );
                    AddItem( unit.SidebarItem_MainMapActor, ref currentY );
                }
            }
        }
    }


    public class Sidebar_YourForcesItems : ISidebarBasicItemImplementation
    {
        public void Sidebar_GetOrSetUIData( UISidebarBasicItem Item, ImageButtonAbstractBase ImageController, ButtonAbstractBase ButtonController,
            ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( Action )
            {
                case UIAction.GetTextToShowFromVolatile:
                    ExtraData.Buffer.AddRaw( Item.GetDisplayName() );
                    switch ( Item.ID )
                    {
                        case "VehiclesHeader":
                            ExtraData.Buffer.Space3x().AddFormat2( "OutOF", SimCommon.TotalCapacityUsed_Vehicles, MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt,
                                SimCommon.TotalCapacityUsed_Vehicles != MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataGood );
                            break;
                        case "DeployedAndroidsHeader":
                            ExtraData.Buffer.Space3x().AddFormat2( "OutOF", SimCommon.TotalCapacityUsed_Androids, MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt,
                                SimCommon.TotalCapacityUsed_Androids != MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataGood );
                            break;
                        case "DeployedMechsHeader":
                            ExtraData.Buffer.Space3x().AddFormat2( "OutOF", SimCommon.TotalCapacityUsed_Mechs, MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt,
                                SimCommon.TotalCapacityUsed_Mechs != MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataGood );
                            break;
                        case "BulkUnitsHeader":
                            ExtraData.Buffer.Space3x().AddFormat2( "OutOF", SimCommon.TotalBulkUnitSquadCapacityUsed, MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt,
                                SimCommon.TotalBulkUnitSquadCapacityUsed != MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataGood );
                            break;
                        case "ConvertedUnitsHeader":
                            ExtraData.Buffer.Space3x().AddFormat2( "OutOF", SimCommon.TotalCapturedUnitSquadCapacityUsed, MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt,
                                SimCommon.TotalCapturedUnitSquadCapacityUsed != MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataGood );
                            break;
                    }
                    break;
                case UIAction.HandleMouseover:
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Item ), Element, SideClamp.LeftOrRight, TooltipNovelWidth.Simple, 
                        TooltipExtraText.None, TooltipExtraRules.ClampToSidebar ) )
                    {
                        novel.TitleUpperLeft.AddRaw( Item.GetDisplayNameForSidebar() );
                        string hotkeyToDisplay = Item.GetHotkeyToDisplayAsToggleForSidebar();
                        if ( hotkeyToDisplay.Length > 0 )
                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( hotkeyToDisplay );
                        if ( !Item.GetDescription().IsEmpty() )
                            novel.Main.AddRaw( Item.GetDescription(), ColorTheme.NarrativeColor );
                    }
                    break;
            }
        }

        public SidebarItemType GetItemType( UISidebarBasicItem Item )
        {
            switch ( Item.ID )
            {
                case "VehiclesHeader":
                case "DeployedAndroidsHeader":
                case "DeployedMechsHeader":
                case "BulkUnitsHeader":
                case "ConvertedUnitsHeader":
                    return SidebarItemType.TextHeader;
            }
            return SidebarItemType.ImgSingleLine;
        }
    }
}
