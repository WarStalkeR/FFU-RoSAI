using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_Networks : Sidebar_Base, IUISidebarTypeImplementation
    {
        private UISidebarBasicItem item_ActiveNetworksHeader;
        private UISidebarBasicItem item_DamagedNetworksHeader;
        private UISidebarBasicItem item_SubnetsHeader;

        #region PopulateItemsIfNeeded
        private void PopulateItemsIfNeeded()
        {
            if ( item_ActiveNetworksHeader != null )
                return;

            item_ActiveNetworksHeader = UISidebarBasicItemTable.Instance.GetRowByID( "ActiveNetworksHeader" );
            item_DamagedNetworksHeader = UISidebarBasicItemTable.Instance.GetRowByID( "DamagedNetworksHeader" );
            item_SubnetsHeader = UISidebarBasicItemTable.Instance.GetRowByID( "SubnetsHeader" );
        }
        #endregion

        public void WriteAnySidebarItems( ref float currentY )
        {
            if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
            {
                Window_Sidebar.Instance.Close( WindowCloseReason.ShowingRefused );
                return;
            }

            PopulateItemsIfNeeded();

            bool hadAnyDamaged = false;

            AddItem( item_ActiveNetworksHeader, ref currentY );

            MachineNetwork network = SimCommon.TheNetwork;
            if ( network != null )
            {
                if ( !network.IsNetworkFunctional )
                    hadAnyDamaged = true;
                else
                {
                    if ( network.SidebarItem_Network == null )
                        network.SidebarItem_Network = new SidebarItemFromOther<MachineNetwork>( network, Sidebar_NetworkHandler.Instance );
                    AddItem( network.SidebarItem_Network, ref currentY );
                }
            }

            if ( hadAnyDamaged )
            {
                AddItem( item_DamagedNetworksHeader, ref currentY );

                if ( network != null )
                {
                    if ( network.IsNetworkFunctional )
                    { }
                    else
                    {
                        if ( network.SidebarItem_Network == null )
                            network.SidebarItem_Network = new SidebarItemFromOther<MachineNetwork>( network, Sidebar_NetworkHandler.Instance );
                        AddItem( network.SidebarItem_Network, ref currentY );
                    }
                }
            }

            AddItem( item_SubnetsHeader, ref currentY );

            foreach ( MachineSubnet subnet in SimCommon.Subnets )
            {
                if ( subnet.SubnetNodes.Count == 0 )
                    continue; //skip if out of use right now
                if ( subnet.SidebarItem_Subnet == null )
                    subnet.SidebarItem_Subnet = new SidebarItemFromOther<MachineSubnet>( subnet, Sidebar_SubnetHandler.Instance );
                AddItem(  subnet.SidebarItem_Subnet, ref currentY );
            }
        }
    }


    public class Sidebar_NetworksItems : ISidebarBasicItemImplementation
    {
        public void Sidebar_GetOrSetUIData( UISidebarBasicItem Item, ImageButtonAbstractBase ImageController, ButtonAbstractBase ButtonController,
            ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( Action )
            {
                case UIAction.GetTextToShowFromVolatile:
                    ExtraData.Buffer.AddRaw( Item.GetDisplayName() );
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
                case "ActiveNetworksHeader":
                case "DamagedNetworksHeader":
                case "SubnetsHeader":
                    return SidebarItemType.TextHeader;
            }
            return SidebarItemType.ImgSingleLine;
        }
    }
}
