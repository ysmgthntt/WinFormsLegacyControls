using System.ComponentModel;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    [ProvideProperty("ContextMenu", typeof(Component))]
    partial class ContextMenu : IExtenderProvider
    {
        bool IExtenderProvider.CanExtend(object extendee)
            => extendee is Control or NotifyIcon;

        [DefaultValue(false)]
        [SRCategory(nameof(SR.CatBehavior))]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool GetContextMenu(Component component)
        {
            if (component is Control control)
                return control.GetContextMenu() == this;
            else if (component is NotifyIcon notifyIcon)
                return notifyIcon.GetContextMenu() == this;
            else
                return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetContextMenu(Component component, bool set)
        {
            if (component is Control control)
                control.SetContextMenu(set ? this : null);
            else if (component is NotifyIcon notifyIcon)
                notifyIcon.SetContextMenu(set ? this : null);
        }
    }
}
