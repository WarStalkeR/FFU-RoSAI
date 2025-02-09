using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public abstract class Sidebar_Base
    {
        #region AddRange
        protected void AddRange<T>( List<T> ItemsToPutInRow, ref float currentY ) where T : ISidebarItem
        {
            foreach ( T item in ItemsToPutInRow )
            {
                if ( item == null )
                    continue;
                if ( VisCurrent.IsUIHiddenExceptForSidebar )
                {
                    if ( item is UISidebarBasicItem basicItem && basicItem.IsHiddenWhenUIIsHidden )
                        continue;
                }

                SidebarItemType itemType = item.GetItemType();
                switch ( itemType )
                {
                    case SidebarItemType.ImgDoubleLine:
                        {
                            Window_Sidebar.bSidebarItemDouble bItem = Window_Sidebar.bSidebarItemDoublePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( bItem == null )
                                break; //time slicing, too many added right now
                            bItem.Assign( item, null );

                            Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 26f, 180f, 24f );
                        }
                        break;
                    case SidebarItemType.ImgSingleLine:
                        {
                            Window_Sidebar.bSidebarItemSingle bItem = Window_Sidebar.bSidebarItemSinglePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( bItem == null )
                                break; //time slicing, too many added right now
                            bItem.Assign( item, null );

                            Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 26f, 180f, 24f );
                        }
                        break;
                    case SidebarItemType.Unit:
                        {
                            Window_Sidebar.bSidebarUnit bItem = Window_Sidebar.bSidebarUnitButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( bItem == null )
                                break; //time slicing, too many added right now
                            bItem.Assign( item, null );

                            Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 26f, 180f, 24f );
                        }
                        break;
                    case SidebarItemType.TextHeader:
                        {
                            Window_Sidebar.bSidebarTextHeader bItem = Window_Sidebar.bSidebarTextHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( bItem == null )
                                break; //time slicing, too many added right now
                            bItem.Assign( item, null );

                            Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 41f, 180f, 41f );
                        }
                        break;
                }
            }
        }
        #endregion

        #region AddItem
        protected void AddItem<T>( T Item, ref float currentY ) where T : ISidebarItem
        {
            if ( Item == null )
                return;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
            {
                if ( Item is UISidebarBasicItem basicItem && basicItem.IsHiddenWhenUIIsHidden )
                    return;
            }

            SidebarItemType itemType = Item.GetItemType();
            switch ( itemType )
            {
                case SidebarItemType.ImgDoubleLine:
                    {
                        Window_Sidebar.bSidebarItemDouble bItem = Window_Sidebar.bSidebarItemDoublePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( bItem == null )
                            break; //time slicing, too many added right now
                        bItem.Assign( Item, null );

                        Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 26f, 180f, 24f );
                    }
                    break;
                case SidebarItemType.ImgSingleLine:
                    {
                        Window_Sidebar.bSidebarItemSingle bItem = Window_Sidebar.bSidebarItemSinglePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( bItem == null )
                            break; //time slicing, too many added right now
                        bItem.Assign( Item, null );

                        Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 26f, 180f, 24f );
                    }
                    break;
                case SidebarItemType.Unit:
                    {
                        Window_Sidebar.bSidebarUnit bItem = Window_Sidebar.bSidebarUnitButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( bItem == null )
                            break; //time slicing, too many added right now
                        bItem.Assign( Item, null );

                        Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 26f, 180f, 24f );
                    }
                    break;
                case SidebarItemType.TextHeader:
                    {
                        Window_Sidebar.bSidebarTextHeader bItem = Window_Sidebar.bSidebarTextHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( bItem == null )
                            break; //time slicing, too many added right now
                        bItem.Assign( Item, null );

                        Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 41f, 180f, 41f );
                    }
                    break;
            }
        }
        #endregion

        #region AddItemAndHandler
        protected void AddItemAndHandler<T>( T Item, ISidebarCustomHandler Handler, ref float currentY ) where T : ISidebarItem
        {
            if ( Item == null || Handler == null )
                return;

            SidebarItemType itemType = Item.GetItemType();
            switch ( itemType )
            {
                case SidebarItemType.ImgDoubleLine:
                    {
                        Window_Sidebar.bSidebarItemDouble bItem = Window_Sidebar.bSidebarItemDoublePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( bItem == null )
                            break; //time slicing, too many added right now
                        bItem.Assign( Item, Handler );

                        Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 26f, 180f, 24f );
                    }
                    break;
                case SidebarItemType.ImgSingleLine:
                    {
                        Window_Sidebar.bSidebarItemSingle bItem = Window_Sidebar.bSidebarItemSinglePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( bItem == null )
                            break; //time slicing, too many added right now
                        bItem.Assign( Item, Handler );

                        Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 26f, 180f, 24f );
                    }
                    break;
                case SidebarItemType.Unit:
                    {
                        Window_Sidebar.bSidebarUnit bItem = Window_Sidebar.bSidebarUnitButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( bItem == null )
                            break; //time slicing, too many added right now
                        bItem.Assign( Item, Handler );

                        Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 26f, 180f, 24f );
                    }
                    break;
                case SidebarItemType.TextHeader:
                    {
                        Window_Sidebar.bSidebarTextHeader bItem = Window_Sidebar.bSidebarTextHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( bItem == null )
                            break; //time slicing, too many added right now
                        bItem.Assign( Item, Handler );

                        Window_Sidebar.customParent.ApplySingleItemInRow( bItem, 5.1f, ref currentY, 41f, 180f, 41f );
                    }
                    break;
            }
        }
        #endregion
    }
}
