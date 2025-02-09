using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_Timelines : Sidebar_Base, IUISidebarTypeImplementation
    {
        private UISidebarBasicItem item_CurrentTimelineHeader;
        private UISidebarBasicItem item_StandardTimelinesHeader;
        private UISidebarBasicItem item_FailedTimelinesHeader;

        #region PopulateItemsIfNeeded
        private void PopulateItemsIfNeeded()
        {
            if ( item_CurrentTimelineHeader != null )
                return;

            item_CurrentTimelineHeader = UISidebarBasicItemTable.Instance.GetRowByID( "CurrentTimelineHeader" );
            item_StandardTimelinesHeader = UISidebarBasicItemTable.Instance.GetRowByID( "StandardTimelinesHeader" );
            item_FailedTimelinesHeader = UISidebarBasicItemTable.Instance.GetRowByID( "FailedTimelinesHeader" );
        }
        #endregion

        private List<CityTimeline> sortedTimelines = List<CityTimeline>.Create_WillNeverBeGCed( 30, "Sidebar_Timelines-sortedTimelines" );

        public void WriteAnySidebarItems( ref float currentY )
        {
            if ( Engine_HotM.GameMode != MainGameMode.TheEndOfTime )
            {
                Window_Sidebar.Instance.Close( WindowCloseReason.ShowingRefused );
                return;
            }

            PopulateItemsIfNeeded();

            sortedTimelines.Clear();
            foreach ( KeyValuePair<int, CityTimeline> kv in SimMetagame.AllTimelines )
            {
                CityTimeline city = kv.Value;
                if ( city == null )
                    continue;
                if ( city.SidebarItem_Timeline == null )
                    city.SidebarItem_Timeline = new SidebarItemFromOther<CityTimeline>( city, Sidebar_TimelineHandler.Instance );
                sortedTimelines.Add( city );
            }
            sortedTimelines.Sort( delegate ( CityTimeline Left, CityTimeline Right )
            {
                int val = Left.Name.CompareTo( Right.Name );
                if ( val != 0 )
                    return val;
                return Left.TimelineID.CompareTo( Right.TimelineID );
            } );

            AddItem( item_CurrentTimelineHeader, ref currentY );
            if ( SimCommon.CurrentTimeline != null )
                AddItem( SimCommon.CurrentTimeline.SidebarItem_Timeline, ref currentY );

            AddItem( item_StandardTimelinesHeader, ref currentY );
            foreach ( CityTimeline city in sortedTimelines )
            {
                if ( city.IsTimelineAFailure || city == SimCommon.CurrentTimeline )
                    continue;
                AddItem( city.SidebarItem_Timeline, ref currentY );
            }

            AddItem( item_FailedTimelinesHeader, ref currentY );
            foreach ( CityTimeline city in sortedTimelines )
            {
                if ( !city.IsTimelineAFailure || city == SimCommon.CurrentTimeline )
                    continue;
                AddItem( city.SidebarItem_Timeline, ref currentY );
            }
        }
    }


    public class Sidebar_TimelinesItems : ISidebarBasicItemImplementation
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
                case "CurrentTimelineHeader":
                case "StandardTimelinesHeader":
                case "FailedTimelinesHeader":
                    return SidebarItemType.TextHeader;
            }
            return SidebarItemType.ImgSingleLine;
        }
    }
}
