using System.ComponentModel;

namespace System.Windows.Forms
{
    [ProvideProperty("Menu", typeof(Form))]
    partial class MainMenu : IExtenderProvider
    {
        bool IExtenderProvider.CanExtend(object extendee)
            => extendee is Form;

        [DefaultValue(false)]
        [SRCategory(nameof(SR.CatWindowStyle))]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool GetMenu(Form form)
            => form.GetMenu() == this;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetMenu(Form form, bool set)
            => form.SetMenu(set ? this : null);
    }
}
