using System.Runtime.InteropServices;

namespace WinFormsLegacyControls.Menus.Migration
{
    partial class ContextMenuSupportControlNativeWindow
    {
        private sealed class ComboBoxSupport
        {
            private readonly ContextMenuSupportControlNativeWindow _owner;
            private readonly ComboBox _comboBox;
            private ChildNativeWindow? _childEdit;
            /*
            private ChildNativeWindow? _childListBox;
            private ChildNativeWindow? _childDropDown;
            */

            public ComboBoxSupport(ContextMenuSupportControlNativeWindow owner, ComboBox comboBox)
            {
                _owner = owner;
                _comboBox = comboBox;
            }

            public void OnHandleCreated()
            {
                if (_comboBox.DropDownStyle != ComboBoxStyle.DropDownList)
                {
                    HWND hwnd = PInvoke.GetWindow(_comboBox, GET_WINDOW_CMD.GW_CHILD);
                    if (!hwnd.IsNull)
                    {
                        if (_comboBox.DropDownStyle == ComboBoxStyle.Simple)
                        {
                            /*
                            _childListBox = new ChildNativeWindow(this);
                            _childListBox.AssignHandle(hwnd);
                            */
                            hwnd = PInvoke.GetWindow(new HandleRef(_comboBox, hwnd), GET_WINDOW_CMD.GW_HWNDNEXT);
                        }

                        _childEdit = new ChildNativeWindow(this);
                        _childEdit.AssignHandle(hwnd);
                    }
                }
            }

            public void OnHandleDestroyed(object sender, EventArgs e)
                => ReleaseChildWindow();

            public void ReleaseChildWindow()
            {
                if (_childEdit is not null)
                {
                    _childEdit.ReleaseHandle();
                    _childEdit = null;
                }

                /*
                if (_childListBox is not null)
                {
                    _childListBox.ReleaseHandle();
                    _childListBox = null;
                }

                if (_childDropDown is not null)
                {
                    _childDropDown.ReleaseHandle();
                    _childDropDown = null;
                }
                */
            }

            /*
            public void OnParentNotify(nint handle)
            {
                if (_childDropDown is not null)
                    _childDropDown.ReleaseHandle();
                else
                    _childDropDown = new ChildNativeWindow(this);
                _childDropDown.AssignHandle(handle);
            }
            */

            private sealed class ChildNativeWindow : NativeWindow
            {
                private readonly ComboBoxSupport _owner;

                public ChildNativeWindow(ComboBoxSupport owner)
                {
                    _owner = owner;
                }

                protected override void WndProc(ref Message m)
                {
                    switch ((uint)m.Msg)
                    {
                        case PInvoke.WM_CONTEXTMENU:
                            if (_owner._owner._contextMenu is not null)
                                PInvoke.SendMessage(_owner._comboBox, PInvoke.WM_CONTEXTMENU, (WPARAM)m.WParam, m.LParam);
                            else
                                base.WndProc(ref m);
                            break;

                        default:
                            base.WndProc(ref m);
                            break;
                    }
                }
            }
        }
    }
}
