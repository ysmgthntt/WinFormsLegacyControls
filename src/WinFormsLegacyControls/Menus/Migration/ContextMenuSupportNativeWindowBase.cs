// https://github.com/dotnet/winforms/pull/2157/files#diff-07a0a87cedab0d76c974ce8b105912a1b986c87116c7ee0ac73d6d5d65e4b48a

#nullable disable

using WinFormsLegacyControls.Migration;
using static Interop;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal abstract class ContextMenuSupportNativeWindowBase<TControl> : NativeWindow
        where TControl : Control
    {
        private readonly TControl _control;
        private ContextMenu _contextMenu;

        protected ContextMenuSupportNativeWindowBase(TControl control)
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
            OnDetach();
        }

        protected virtual void OnDetach() { }

        private void Control_Disposed(object sender, EventArgs e)
        {
            ControlDispose();
        }

        private void Control_HandleCreated(object sender, EventArgs e)
        {
            AssignHandle(_control.Handle);
        }

        protected TControl Target => _control;

        public Control? SourceControl { get; set; }

        /// <summary>
        ///  The contextMenu associated with this control. The contextMenu
        ///  will be shown when the user right clicks the mouse on the control.
        ///
        ///  Whidbey: ContextMenu is browsable false.  In all cases where both a context menu
        ///  and a context menu strip are assigned, context menu will be shown instead of context menu strip.
        /// </summary>
        public /*virtual*/ ContextMenu ContextMenu
        {
            get => _contextMenu;
            set
            {
                ContextMenu oldValue = _contextMenu;

                if (oldValue != value)
                {
                    EventHandler disposedHandler = new EventHandler(DetachContextMenu);

                    if (oldValue != null)
                    {
                        oldValue.Disposed -= disposedHandler;
                    }

                    _contextMenu = value;

                    if (value != null)
                    {
                        value.Disposed += disposedHandler;
                    }

                    //OnContextMenuChanged(EventArgs.Empty);

                    if (_control is UpDownBase)
                    {
                        foreach (Control child in _control.Controls)
                        {
                            if (child is TextBoxBase)
                            {
                                child.SetContextMenu(value, _control);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void DetachContextMenu(object sender, EventArgs e) => ContextMenu = null;

        //protected override void Dispose(bool disposing)
        private void ControlDispose()
        {
            ContextMenu contextMenu = _contextMenu;
            if (contextMenu != null)
            {
                contextMenu.Disposed -= new EventHandler(DetachContextMenu);
            }
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

        protected virtual bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Debug.WriteLineIf(s_controlKeyboardRouting.TraceVerbose, "Control.ProcessCmdKey " + msg.ToString());
            ContextMenu contextMenu = (ContextMenu)Properties.GetObject(s_contextMenuProperty);
            if (contextMenu != null && contextMenu.ProcessCmdKey(ref msg, keyData, this))
            {
                return true;
            }

            if (_parent != null)
            {
                return _parent.ProcessCmdKey(ref msg, keyData);
            }
            return false;
        }
          -> MenuShortcutProcessMessageFilter
        */

        // overridable so nested controls can provide a different source control.
        internal /*virtual*/ void WmContextMenu(ref Message m)
        {
            //WmContextMenu(ref m, _control);
            WmContextMenu(ref m, SourceControl ?? _control);
        }

        /// <summary>
        ///  Handles the WM_CONTEXTMENU message
        /// </summary>
        internal void WmContextMenu(ref Message m, Control sourceControl)
        {
            ContextMenu contextMenu = _contextMenu;

            if (contextMenu is not null)
            {
                Point client;
                // lparam will be exactly -1 when the user invokes the context menu
                // with the keyboard.
                //
                if (unchecked((int)(long)m.LParam) == -1)
                {
                    client = new Point(_control.Width / 2, _control.Height / 2);
                }
                else
                {
                    Point pt = new Point((int)m.LParam);
#if DEBUG
                    int x = PARAM.SignedLOWORD(m.LParam);
                    int y = PARAM.SignedHIWORD(m.LParam);
                    Debug.Assert(pt.X == x && pt.Y == y);
#endif
                    client = _control.PointToClient(pt);
                }

                // VisualStudio7 # 156, only show the context menu when clicked in the client area
                if (_control.ClientRectangle.Contains(client))
                {
                    contextMenu.Show(sourceControl, client);
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
        ///  Handles the WM_EXITMENULOOP message. If this control has a context menu, its
        ///  Collapse event is raised.
        /// </summary>
        private void WmExitMenuLoop(ref Message m)
        {
            // call Form.OnMenuComplete
            DefWndProc(ref m);

            bool isContextMenu = (unchecked((int)(long)m.WParam) == 0) ? false : true;

            if (isContextMenu)
            {
                ContextMenu contextMenu = _contextMenu;
                if (contextMenu != null)
                {
                    //contextMenu.OnCollapse(EventArgs.Empty);
                    contextMenu.RaiseCollapse();
                }
            }

            //DefWndProc(ref m);
        }

        /// <summary>
        ///  Handles the WM_INITMENUPOPUP message
        /// </summary>
        private void WmInitMenuPopup(ref Message m)
        {
            ContextMenu contextMenu = _contextMenu;
            if (contextMenu != null)
            {

                if (contextMenu.ProcessInitMenuPopup(m.WParam))
                {
                    return;
                }
            }
            //DefWndProc(ref m);
            // for MainMenu
            base.WndProc(ref m);
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

            // add
            // for MainMenu
            base.WndProc(ref m);
        }

        protected override void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                case PInvoke.WM_COMMAND:
                    if (!CommonMessageHandlers.WmCommand(ref m))
                        base.WndProc(ref m);
                    break;

                case PInvoke.WM_CONTEXTMENU:
                    WmContextMenu(ref m);
                    break;

                case PInvoke.WM_DRAWITEM:
                    if (!CommonMessageHandlers.WmDrawItem(ref m))
                        base.WndProc(ref m);
                    break;

                case PInvoke.WM_EXITMENULOOP:
                    WmExitMenuLoop(ref m);
                    break;

                case PInvoke.WM_INITMENUPOPUP:
                    WmInitMenuPopup(ref m);
                    break;

                case PInvoke.WM_MEASUREITEM:
                    if (!CommonMessageHandlers.WmMeasureItem(ref m))
                        base.WndProc(ref m);
                    break;

                case PInvoke.WM_MENUCHAR:
                    WmMenuChar(ref m);
                    break;

                case PInvoke.WM_MENUSELECT:
                    CommonMessageHandlers.WmMenuSelect(ref m);
                    ControlAccessors.DefWndProc(_control, ref m);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }
}
