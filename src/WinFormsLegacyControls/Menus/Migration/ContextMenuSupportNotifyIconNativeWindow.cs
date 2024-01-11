using System.Reflection;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal sealed class ContextMenuSupportNotifyIconNativeWindow : NativeWindow
        , ISupportNativeWindow<NotifyIcon, ContextMenu, ContextMenuSupportNotifyIconNativeWindow>
    {
        private readonly NotifyIcon _notifyIcon;
        private ContextMenu? contextMenu;

        private ContextMenuSupportNotifyIconNativeWindow(NotifyIcon notifyIcon)
        {
            _notifyIcon = notifyIcon;
            _notifyIcon.MouseUp += NotifyIcon_MouseUp;
        }

        public static ContextMenuSupportNotifyIconNativeWindow Create(NotifyIcon notifyIcon)
            => new(notifyIcon);

        public void Detach()
        {
            _notifyIcon.MouseUp -= NotifyIcon_MouseUp;
            ReleaseHandle();
        }

        ContextMenu? ISupportNativeWindow<NotifyIcon, ContextMenu, ContextMenuSupportNotifyIconNativeWindow>.Property
        {
            get => contextMenu;
            set => contextMenu = value;
        }

        private static FieldInfo? _fiWindow;

        private void NotifyIcon_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _notifyIcon.ContextMenuStrip is null && contextMenu is not null)
            {
                if (Handle == 0)
                {
                    if (_fiWindow is null)
                        _fiWindow = typeof(NotifyIcon).GetField("_window", BindingFlags.NonPublic | BindingFlags.Instance)!;
                    NativeWindow window = (NativeWindow)_fiWindow.GetValue(_notifyIcon)!;
                    AssignHandle(window.Handle);
                }

                //private void ShowContextMenu()
                /*
                UnsafeNativeMethods.GetCursorPos(out Point pt);

                // Summary: the current window must be made the foreground window
                // before calling TrackPopupMenuEx, and a task switch must be
                // forced after the call.
                UnsafeNativeMethods.SetForegroundWindow(new HandleRef(window, window.Handle));

                if (contextMenu != null)
                {
                    contextMenu.OnPopup(EventArgs.Empty);

                    SafeNativeMethods.TrackPopupMenuEx(new HandleRef(contextMenu, contextMenu.Handle),
                                             NativeMethods.TPM_VERTICAL | NativeMethods.TPM_RIGHTALIGN,
                                             pt.X,
                                             pt.Y,
                                             new HandleRef(window, window.Handle),
                                             null);

                    // Force task switch (see above)
                    UnsafeNativeMethods.PostMessage(new HandleRef(window, window.Handle), WindowMessages.WM_NULL, IntPtr.Zero, IntPtr.Zero);
                }
                */
                contextMenu.ShowAtCursorPos(this, null, TRACK_POPUP_MENU_FLAGS.TPM_VERTICAL | TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN);
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                case PInvoke.WM_COMMAND:
                    if (!CommonMessageHandlers.WmCommand(ref m))
                        base.WndProc(ref m);
                    break;

                case PInvoke.WM_DRAWITEM:
                    if (!CommonMessageHandlers.WmDrawItem(ref m))
                        base.WndProc(ref m);
                    break;

                case PInvoke.WM_MEASUREITEM:
                    if (!CommonMessageHandlers.WmMeasureItem(ref m))
                        base.WndProc(ref m);
                    break;

                case PInvoke.WM_MENUSELECT:
                    CommonMessageHandlers.WmMenuSelect(ref m);
                    base.WndProc(ref m);
                    break;

                case PInvoke.WM_INITMENUPOPUP:
                    WmInitMenuPopup(ref m);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void WmInitMenuPopup(ref Message m)
        {
            if (contextMenu != null)
            {
                if (contextMenu.ProcessInitMenuPopup(m.WParam))
                {
                    return;
                }
            }

            /*window.*/DefWndProc(ref m);
        }
    }
}
