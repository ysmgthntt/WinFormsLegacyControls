// https://github.com/dotnet/winforms/pull/2157/files#diff-07a0a87cedab0d76c974ce8b105912a1b986c87116c7ee0ac73d6d5d65e4b48a

#nullable disable

using System.Drawing;
using System.Windows.Forms;
using static Interop;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal sealed class ContextMenuSupportControlNativeWindow : NativeWindow
    {
        private readonly Control _control;
        private ContextMenu _contextMenu;

        public ContextMenuSupportControlNativeWindow(Control control)
        {
            _control = control;
            _control.Disposed += Control_Disposed;
            _control.HandleCreated += Control_HandleCreated;
            if (_control.IsHandleCreated)
                AssignHandle(_control.Handle);
        }

        public void Detach()
        {
            _control.Disposed -= Control_Disposed;
            _control.HandleCreated -= Control_HandleCreated;
            ReleaseHandle();
        }

        private void Control_Disposed(object sender, EventArgs e)
        {
            ControlDispose();
        }

        private void Control_HandleCreated(object sender, EventArgs e)
        {
            AssignHandle(_control.Handle);
        }

        /// <summary>
        ///  The contextMenu associated with this control. The contextMenu
        ///  will be shown when the user right clicks the mouse on the control.
        ///
        ///  Whidbey: ContextMenu is browsable false.  In all cases where both a context menu
        ///  and a context menu strip are assigned, context menu will be shown instead of context menu strip.
        /// </summary>
        public /*virtual*/ ContextMenu ContextMenu
        {
            //get => (ContextMenu)Properties.GetObject(s_contextMenuProperty);
            get => _contextMenu;
            set
            {
                //ContextMenu oldValue = (ContextMenu)Properties.GetObject(s_contextMenuProperty);
                ContextMenu oldValue = _contextMenu;

                if (oldValue != value)
                {
                    EventHandler disposedHandler = new EventHandler(DetachContextMenu);

                    if (oldValue != null)
                    {
                        oldValue.Disposed -= disposedHandler;
                    }

                    //Properties.SetObject(s_contextMenuProperty, value);
                    _contextMenu = value;

                    if (value != null)
                    {
                        value.Disposed += disposedHandler;
                    }

                    //OnContextMenuChanged(EventArgs.Empty);
                }
            }
        }

        private void DetachContextMenu(object sender, EventArgs e) => ContextMenu = null;

        //protected override void Dispose(bool disposing)
        private void ControlDispose()
        {
            //ContextMenu contextMenu = (ContextMenu)Properties.GetObject(s_contextMenuProperty);
            ContextMenu contextMenu = _contextMenu;
            if (contextMenu != null)
            {
                contextMenu.Disposed -= new EventHandler(DetachContextMenu);
            }
        }

        private MenuItem GetMenuItemFromHandleId(IntPtr hmenu, int item)
        {
            MenuItem mi = null;
            //int id = UnsafeNativeMethods.GetMenuItemID(new HandleRef(null, hmenu), item);
            int id = (int)PInvoke.GetMenuItemID((HMENU)hmenu, item);
            if (id == unchecked((int)0xFFFFFFFF))
            {
                IntPtr childMenu = IntPtr.Zero;
                //childMenu = UnsafeNativeMethods.GetSubMenu(new HandleRef(null, hmenu), item);
                childMenu = PInvoke.GetSubMenu((HMENU)hmenu, item);
                //int count = UnsafeNativeMethods.GetMenuItemCount(new HandleRef(null, childMenu));
                int count = PInvoke.GetMenuItemCount((HMENU)childMenu);
                MenuItem found = null;
                for (int i = 0; i < count; i++)
                {
                    found = GetMenuItemFromHandleId(childMenu, i);
                    if (found != null)
                    {
                        Menu parent = found.Parent;
                        if (parent != null && parent is MenuItem)
                        {
                            found = (MenuItem)parent;
                            break;
                        }
                        found = null;
                    }
                }

                mi = found;
            }
            else
            {
                Command cmd = Command.GetCommandFromID(id);
                if (cmd != null)
                {
                    object reference = cmd.Target;
                    if (reference != null && reference is MenuItem.MenuItemData)
                    {
                        mi = ((MenuItem.MenuItemData)reference).baseItem;
                    }
                }
            }
            return mi;
        }

        /*
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual void OnContextMenuChanged(EventArgs e)
        {
            if (Events[s_contextMenuEvent] is EventHandler eh)
            {
                eh(this, e);
            }
        }

        // TODO:
        protected virtual bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //ContextMenu contextMenu = (ContextMenu)Properties.GetObject(s_contextMenuProperty);
            ContextMenu contextMenu = _contextMenu;
            if (contextMenu != null && contextMenu.ProcessCmdKey(ref msg, keyData, this))
            {
                return true;
            }

            return false;
        }
        */

        /// <summary>
        ///  Handles the WM_COMMAND message
        /// </summary>
        private void WmCommand(ref Message m)
        {
            if (IntPtr.Zero == m.LParam)
            {
                //if (Command.DispatchID(NativeMethods.Util.LOWORD(m.WParam)))
                if (Command.DispatchID(PARAM.LOWORD(m.WParam)))
                {
                    return;
                }
            }
            base.WndProc(ref m);
        }

        // overridable so nested controls can provide a different source control.
        internal /*virtual*/ void WmContextMenu(ref Message m)
        {
            WmContextMenu(ref m, _control);
        }

        /// <summary>
        ///  Handles the WM_CONTEXTMENU message
        /// </summary>
        internal void WmContextMenu(ref Message m, Control sourceControl)
        {
            //ContextMenu contextMenu = Properties.GetObject(s_contextMenuProperty) as ContextMenu;
            ContextMenu contextMenu = _contextMenu;
            //ContextMenuStrip contextMenuStrip = (contextMenu != null) ? null /*save ourselves a property fetch*/
            //                                                            : Properties.GetObject(s_contextMenuStripProperty) as ContextMenuStrip;

            //if (contextMenu != null || contextMenuStrip != null)
            if (contextMenu is not null)
            {
                //int x = NativeMethods.Util.SignedLOWORD(m.LParam);
                int x = PARAM.SignedLOWORD(m.LParam);
                //int y = NativeMethods.Util.SignedHIWORD(m.LParam);
                int y = PARAM.SignedHIWORD(m.LParam);
                Point client;
                bool keyboardActivated = false;
                // lparam will be exactly -1 when the user invokes the context menu
                // with the keyboard.
                //
                if (unchecked((int)(long)m.LParam) == -1)
                {
                    keyboardActivated = true;
                    client = new Point(_control.Width / 2, _control.Height / 2);
                }
                else
                {
                    client = _control.PointToClient(new Point(x, y));
                }

                // VisualStudio7 # 156, only show the context menu when clicked in the client area
                if (_control.ClientRectangle.Contains(client))
                {
                    //if (contextMenu != null)
                    {
                        contextMenu.Show(sourceControl, client);
                    }
                    /*
                    else if (contextMenuStrip != null)
                    {
                        contextMenuStrip.ShowInternal(sourceControl, client, keyboardActivated);
                    }
                    else
                    {
                        Debug.Fail("contextmenu and contextmenustrip are both null... hmm how did we get here?");
                        DefWndProc(ref m);
                    }
                    */
                }
                else
                {
                    //DefWndProc(ref m);
                    base.WndProc(ref m);
                }
            }
            else
            {
                //DefWndProc(ref m);
                base.WndProc(ref m);
            }
        }

        /// <summary>
        ///  WM_DRAWITEM handler
        /// </summary>
        private void WmDrawItem(ref Message m)
        {
            // If the wparam is zero, then the message was sent by a menu.
            // See WM_DRAWITEM in MSDN.
            if (m.WParam == IntPtr.Zero)
            {
                WmDrawItemMenuItem(ref m);
            }
            else
            {
                //WmOwnerDraw(ref m);
                base.WndProc(ref m);
            }
        }

        private void WmDrawItemMenuItem(ref Message m)
        {
            // Obtain the menu item object
            //NativeMethods.DRAWITEMSTRUCT dis = (NativeMethods.DRAWITEMSTRUCT)m.GetLParam(typeof(NativeMethods.DRAWITEMSTRUCT));
            nint itemData;
            unsafe
            {
                MEASUREITEMSTRUCT* mis = (MEASUREITEMSTRUCT*)m.LParam;
                itemData = (nint)mis->itemData;
            }

            // A pointer to the correct MenuItem is stored in the draw item
            // information sent with the message.
            // (See MenuItem.CreateMenuItemInfo)
            MenuItem menuItem = MenuItem.GetMenuItemFromItemData(/*dis.*/itemData);

            // Delegate this message to the menu item
            if (menuItem != null)
            {
                menuItem.WmDrawItem(ref m);
            }
            else//add
            {
                base.WndProc(ref m);
            }
        }

        /// <summary>
        ///  Handles the WM_EXITMENULOOP message. If this control has a context menu, its
        ///  Collapse event is raised.
        /// </summary>
        private void WmExitMenuLoop(ref Message m)
        {
            bool isContextMenu = (unchecked((int)(long)m.WParam) == 0) ? false : true;

            if (isContextMenu)
            {
                //ContextMenu contextMenu = (ContextMenu)Properties.GetObject(s_contextMenuProperty);
                ContextMenu contextMenu = _contextMenu;
                if (contextMenu != null)
                {
                    contextMenu.OnCollapse(EventArgs.Empty);
                }
            }

            DefWndProc(ref m);
        }

        /// <summary>
        ///  Handles the WM_INITMENUPOPUP message
        /// </summary>
        private void WmInitMenuPopup(ref Message m)
        {
            //ContextMenu contextMenu = (ContextMenu)Properties.GetObject(s_contextMenuProperty);
            ContextMenu contextMenu = _contextMenu;
            if (contextMenu != null)
            {

                if (contextMenu.ProcessInitMenuPopup(m.WParam))
                {
                    return;
                }
            }
            DefWndProc(ref m);
        }

        /// <summary>
        ///  WM_MEASUREITEM handler
        /// </summary>
        private void WmMeasureItem(ref Message m)
        {
            // If the wparam is zero, then the message was sent by a menu.
            // See WM_MEASUREITEM in MSDN.
            if (m.WParam == IntPtr.Zero)
            {

                // Obtain the menu item object
                //NativeMethods.MEASUREITEMSTRUCT mis = (NativeMethods.MEASUREITEMSTRUCT)m.GetLParam(typeof(NativeMethods.MEASUREITEMSTRUCT));
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
                MenuItem menuItem = MenuItem.GetMenuItemFromItemData(/*mis.*/itemData);
                Debug.Assert(menuItem != null, "UniqueID is not associated with a menu item");

                // Delegate this message to the menu item
                if (menuItem != null)
                {
                    menuItem.WmMeasureItem(ref m);
                }
                else//add
                {
                    base.WndProc(ref m);
                }
            }
            else
            {
                //WmOwnerDraw(ref m);
                base.WndProc(ref m);
            }
        }

        /// <summary>
        ///  Handles the WM_MENUCHAR message
        /// </summary>
        private void WmMenuChar(ref Message m)
        {
            Menu menu = ContextMenu;
            if (menu != null)
            {
                menu.WmMenuChar(ref m);
                if (m.Result != IntPtr.Zero)
                {
                    // This char is a mnemonic on our menu.
                    return;
                }
            }
        }

        /// <summary>
        ///  Handles the WM_MENUSELECT message
        /// </summary>
        private void WmMenuSelect(ref Message m)
        {
            //int item = NativeMethods.Util.LOWORD(m.WParam);
            int item = PARAM.LOWORD(m.WParam);
            //int flags = NativeMethods.Util.HIWORD(m.WParam);
            MENU_ITEM_FLAGS flags = (MENU_ITEM_FLAGS)PARAM.HIWORD(m.WParam);
            IntPtr hmenu = m.LParam;
            MenuItem mi = null;

            //if ((flags & NativeMethods.MF_SYSMENU) != 0)
            if ((flags & MENU_ITEM_FLAGS.MF_SYSMENU) != 0)
            {
                // nothing
            }
            //else if ((flags & NativeMethods.MF_POPUP) == 0)
            else if ((flags & MENU_ITEM_FLAGS.MF_POPUP) == 0)
            {
                Command cmd = Command.GetCommandFromID(item);
                if (cmd != null)
                {
                    object reference = cmd.Target;
                    if (reference != null && reference is MenuItem.MenuItemData)
                    {
                        mi = ((MenuItem.MenuItemData)reference).baseItem;
                    }
                }
            }
            else
            {
                mi = GetMenuItemFromHandleId(hmenu, item);
            }

            if (mi != null)
            {
                mi.PerformSelect();
            }

            DefWndProc(ref m);
        }

        protected override void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                //case WindowMessages.WM_COMMAND:
                case PInvoke.WM_COMMAND:
                    WmCommand(ref m);
                    break;

                //case WindowMessages.WM_CONTEXTMENU:
                case PInvoke.WM_CONTEXTMENU:
                    WmContextMenu(ref m);
                    break;

                //case WindowMessages.WM_DRAWITEM:
                case PInvoke.WM_DRAWITEM:
                    WmDrawItem(ref m);
                    break;

                //case WindowMessages.WM_EXITMENULOOP:
                case PInvoke.WM_EXITMENULOOP:
                    WmExitMenuLoop(ref m);
                    break;

                //case WindowMessages.WM_INITMENUPOPUP:
                case PInvoke.WM_INITMENUPOPUP:
                    WmInitMenuPopup(ref m);
                    break;

                //case WindowMessages.WM_MEASUREITEM:
                case PInvoke.WM_MEASUREITEM:
                    WmMeasureItem(ref m);
                    break;

                //case WindowMessages.WM_MENUCHAR:
                case PInvoke.WM_MENUCHAR:
                    WmMenuChar(ref m);
                    break;

                //case WindowMessages.WM_MENUSELECT:
                case PInvoke.WM_MENUSELECT:
                    WmMenuSelect(ref m);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }
}
