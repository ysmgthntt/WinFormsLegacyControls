using System.Runtime.InteropServices;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal sealed class ContextMenuSupportComboBoxNativeWindow : ContextMenuSupportNativeWindowBase<ComboBox>
        , ISupportNativeWindow<ComboBox, ContextMenu, ContextMenuSupportComboBoxNativeWindow>
    {
        private ChildNativeWindow? _childEdit;
        /*
        private ChildNativeWindow? _childListBox;
        private ChildNativeWindow? _childDropDown;
        */

        private ContextMenuSupportComboBoxNativeWindow(ComboBox comboBox)
            : base(comboBox)
        { }

        public static ContextMenuSupportComboBoxNativeWindow Create(ComboBox comboBox)
            => new(comboBox);

        ContextMenu? ISupportNativeWindow<ComboBox, ContextMenu, ContextMenuSupportComboBoxNativeWindow>.Property
        {
            get => ContextMenu;
            set => ContextMenu = value;
        }

        protected override void OnHandleChange()
        {
            if (Handle != 0)
                OnHandleCreated();
            else
                ReleaseChildWindow();
        }

        public void OnHandleCreated()
        {
            ComboBox comboBox = Target;
            if (comboBox.DropDownStyle != ComboBoxStyle.DropDownList)
            {
                HWND hwnd = PInvoke.GetWindow(comboBox, GET_WINDOW_CMD.GW_CHILD);
                if (!hwnd.IsNull)
                {
                    if (comboBox.DropDownStyle == ComboBoxStyle.Simple)
                    {
                        /*
                        _childListBox = new ChildNativeWindow(this);
                        _childListBox.AssignHandle(hwnd);
                        */
                        hwnd = PInvoke.GetWindow(new HandleRef(comboBox, hwnd), GET_WINDOW_CMD.GW_HWNDNEXT);
                    }

                    _childEdit = new ChildNativeWindow(this);
                    _childEdit.AssignHandle(hwnd);
                }
            }
        }

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
        protected override void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                case PInvoke.WM_PARENTNOTIFY:
                    base.WndProc(ref m);
                    if ((int)m.WParam == ((int)PInvoke.WM_CREATE | 1000 << 16))
                    {
                        if (_childDropDown is not null)
                            _childDropDown.ReleaseHandle();
                        else
                            _childDropDown = new ChildNativeWindow(this);
                        _childDropDown.AssignHandle(m.LParam);
                    }
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }
        */

        private sealed class ChildNativeWindow : NativeWindow
        {
            private readonly ContextMenuSupportComboBoxNativeWindow _owner;

            public ChildNativeWindow(ContextMenuSupportComboBoxNativeWindow owner)
            {
                _owner = owner;
            }

            protected override void WndProc(ref Message m)
            {
                switch ((uint)m.Msg)
                {
                    case PInvoke.WM_CONTEXTMENU:
                        if (_owner.ContextMenu is not null)
                            PInvoke.SendMessage(_owner.Target, PInvoke.WM_CONTEXTMENU, (WPARAM)m.WParam, m.LParam);
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
