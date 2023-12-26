namespace WinFormsLegacyControls.Menus.Migration
{
    internal interface ISupportNativeWindow<TControl, TProperty, TSelf>
    {
        static abstract TSelf Create(TControl control);
        void Detach();
        TProperty Property { get; set; }
    }
}
