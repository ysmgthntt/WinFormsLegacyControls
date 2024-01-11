namespace WinFormsLegacyControls.Menus.Migration
{
    internal sealed class ContextMenuSupportControlNativeWindow : ContextMenuSupportNativeWindowBase<Control>
        , ISupportNativeWindow<Control, ContextMenu, ContextMenuSupportControlNativeWindow>
    {
        private ContextMenuSupportControlNativeWindow(Control control)
            : base(control)
        { }

        public static ContextMenuSupportControlNativeWindow Create(Control control)
            => new(control);

        ContextMenu? ISupportNativeWindow<Control, ContextMenu, ContextMenuSupportControlNativeWindow>.Property
        {
            get => ContextMenu;
            set => ContextMenu = value;
        }
    }
}
