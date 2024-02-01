using System.Reflection;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal sealed class ContextMenuSupportNotifyIconNativeWindow : NativeWindow
        , ISupportNativeWindow<NotifyIcon, ContextMenu, ContextMenuSupportNotifyIconNativeWindow>
    {
        private readonly NotifyIcon _notifyIcon;
        private ContextMenu? _contextMenu;

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
            get => _contextMenu;
            set => _contextMenu = value;
        }

        private static FieldInfo? s_fiWindow;

        private void NotifyIcon_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _notifyIcon.ContextMenuStrip is null && _contextMenu is not null)
            {
                if (Handle == 0)
                {
                    s_fiWindow ??= typeof(NotifyIcon).GetField("_window", BindingFlags.NonPublic | BindingFlags.Instance)!;
                    NativeWindow window = (NativeWindow)s_fiWindow.GetValue(_notifyIcon)!;
                    AssignHandle(window.Handle);
                }

                _contextMenu.ShowAtCursorPos(this, null, TRACK_POPUP_MENU_FLAGS.TPM_VERTICAL | TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN);
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

                // [spec]
                case PInvoke.WM_MENUSELECT:
                    CommonMessageHandlers.WmMenuSelect(ref m);
                    DefWndProc(ref m);
                    break;

                case PInvoke.WM_INITMENUPOPUP:
                    WmInitMenuPopup(ref m);
                    break;

                // [spec]
                case PInvoke.WM_MENUCHAR:
                    _contextMenu?.WmMenuChar(ref m);
                    break;

                // [spec]
                case PInvoke.WM_EXITMENULOOP:
                    if (_contextMenu is not null && m.WParam != 0)
                        _contextMenu.RaiseCollapse();
                    DefWndProc(ref m);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void WmInitMenuPopup(ref Message m)
        {
            if (_contextMenu is not null)
            {
                if (_contextMenu.ProcessInitMenuPopup(m.WParam))
                {
                    return;
                }
            }

            DefWndProc(ref m);
        }
    }
}
