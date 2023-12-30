using System.ComponentModel;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    [ProvideProperty("ContextMenu", typeof(Control))]
    partial class ContextMenu : IExtenderProvider
    {
        bool IExtenderProvider.CanExtend(object extendee)
            => extendee is Control;

        [DefaultValue(false)]
        [SRCategory(nameof(SR.CatBehavior))]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool GetContextMenu(Control control)
            => control.GetContextMenu() == this;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetContextMenu(Control control, bool set)
            => control.SetContextMenu(set ? this : null);
    }
}
