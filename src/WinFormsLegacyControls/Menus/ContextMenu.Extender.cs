using System.ComponentModel;

namespace System.Windows.Forms
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
