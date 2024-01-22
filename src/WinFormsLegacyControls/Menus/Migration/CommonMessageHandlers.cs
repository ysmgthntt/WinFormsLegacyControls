using static Interop;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal static class CommonMessageHandlers
    {
        /// <summary>
        ///  Handles the WM_COMMAND message
        /// </summary>
        public static bool WmCommand(ref Message m)
        {
            if (IntPtr.Zero == m.LParam)
            {
                if (Command.DispatchID(PARAM.LOWORD(m.WParam)))
                {
                    return true;
                }
            }
            //base.WndProc(ref m);
            return false;
        }

        /// <summary>
        ///  WM_DRAWITEM handler
        /// </summary>
        public static bool WmDrawItem(ref Message m)
        {
            // If the wparam is zero, then the message was sent by a menu.
            // See WM_DRAWITEM in MSDN.
            if (m.WParam == IntPtr.Zero)
            {
                return WmDrawItemMenuItem(ref m);
            }
            else
            {
                //WmOwnerDraw(ref m);
                //base.WndProc(ref m);
                return false;
            }
        }

        private static bool WmDrawItemMenuItem(ref Message m)
        {
            // Obtain the menu item object
            nint itemData;
            unsafe
            {
                DRAWITEMSTRUCT* mis = (DRAWITEMSTRUCT*)m.LParam;
                itemData = (nint)mis->itemData;
            }

            // A pointer to the correct MenuItem is stored in the draw item
            // information sent with the message.
            // (See MenuItem.CreateMenuItemInfo)
            MenuItem? menuItem = MenuItem.GetMenuItemFromItemData(itemData);

            // Delegate this message to the menu item
            if (menuItem is not null)
            {
                menuItem.WmDrawItem(ref m);
                return true;
            }
            else//add
            {
                //base.WndProc(ref m);
                return false;
            }
        }

        /// <summary>
        ///  WM_MEASUREITEM handler
        /// </summary>
        public static bool WmMeasureItem(ref Message m)
        {
            // If the wparam is zero, then the message was sent by a menu.
            // See WM_MEASUREITEM in MSDN.
            if (m.WParam == IntPtr.Zero)
            {

                // Obtain the menu item object
                nint itemData;
                unsafe
                {
                    MEASUREITEMSTRUCT* mis = (MEASUREITEMSTRUCT*)m.LParam;
                    itemData = (nint)mis->itemData;
                }

                Debug.Assert(m.LParam != IntPtr.Zero, "m.lparam is null");

                // A pointer to the correct MenuItem is stored in the measure item
                // information sent with the message.
                // (See MenuItem.CreateMenuItemInfo)
                MenuItem? menuItem = MenuItem.GetMenuItemFromItemData(itemData);
                Debug.Assert(menuItem is not null, "UniqueID is not associated with a menu item");

                // Delegate this message to the menu item
                if (menuItem is not null)
                {
                    menuItem.WmMeasureItem(ref m);
                    return true;
                }
                else//add
                {
                    //base.WndProc(ref m);
                    return false;
                }
            }
            else
            {
                //WmOwnerDraw(ref m);
                //base.WndProc(ref m);
                return false;
            }
        }

        /// <summary>
        ///  Handles the WM_MENUSELECT message
        /// </summary>
        public static void WmMenuSelect(ref Message m)
        {
            int item = PARAM.LOWORD(m.WParam);
            MENU_ITEM_FLAGS flags = (MENU_ITEM_FLAGS)PARAM.HIWORD(m.WParam);
            IntPtr hmenu = m.LParam;
            MenuItem? mi = null;

            if ((flags & MENU_ITEM_FLAGS.MF_SYSMENU) != 0)
            {
                // nothing
            }
            else if ((flags & MENU_ITEM_FLAGS.MF_POPUP) == 0)
            {
                if (Command.GetCommandFromID(item)?.Target is MenuItem.MenuItemData menuItemData)
                {
                    mi = menuItemData.baseItem;
                }
            }
            else
            {
                mi = GetMenuItemFromHandleId(hmenu, item);
            }

            mi?.PerformSelect();

            //DefWndProc(ref m);
        }

        private static MenuItem? GetMenuItemFromHandleId(IntPtr hmenu, int item)
        {
            int id = (int)PInvoke.GetMenuItemID((HMENU)hmenu, item);
            if (id == unchecked((int)0xFFFFFFFF))
            {
                IntPtr childMenu = IntPtr.Zero;
                childMenu = PInvoke.GetSubMenu((HMENU)hmenu, item);
                int count = PInvoke.GetMenuItemCount((HMENU)childMenu);
                for (int i = 0; i < count; i++)
                {
                    if (GetMenuItemFromHandleId(childMenu, i)?.Parent is MenuItem menuItem)
                    {
                        return menuItem;
                    }
                }
            }
            else
            {
                if (Command.GetCommandFromID(id)?.Target is MenuItem.MenuItemData menuItemData)
                {
                    return menuItemData.baseItem;
                }
            }
            return null;
        }
    }
}
