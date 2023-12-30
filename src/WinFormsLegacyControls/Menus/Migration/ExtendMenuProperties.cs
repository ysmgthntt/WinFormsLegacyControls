using System.Collections.Generic;
using System.Windows.Forms;
using WinFormsLegacyControls.Menus.Migration;

namespace WinFormsLegacyControls
{
    public static class ExtendMenuProperties
    {
        private static bool _messageFilterInstalled;

        private static void Key_Disposed<K, V>(this Dictionary<K, V> dictionary, object? sender, EventArgs e)
            where K : notnull
        {
            if (sender is K key)
                dictionary.Remove(key);
        }

        private static class Holder<K, V, P>
            where K : notnull, Control
            where V : ISupportNativeWindow<K, P, V>
        {
            private static readonly Dictionary<K, V> _property = new();

            public static P? GetValue(K key)
            {
                ArgumentNullException.ThrowIfNull(key);

                if (_property.TryGetValue(key, out var window))
                    return window.Property;
                return default;
            }

            public static void SetValue(K key, P? value)
            {
                ArgumentNullException.ThrowIfNull(key);
                ObjectDisposedException.ThrowIf(key.IsDisposed, key);

                if (_property.TryGetValue(key, out var window))
                {
                    window.Property = value;
                    if (value is null)
                    {
                        window.Detach();
                        key.Disposed -= _property.Key_Disposed;
                        _property.Remove(key);
                    }
                }
                else if (value is not null)
                {
                    if (!_messageFilterInstalled)
                    {
                        Application.AddMessageFilter(new MenuShortcutProcessMessageFilter());
                        _messageFilterInstalled = true;
                    }
                    window = V.Create(key);
                    _property[key] = window;
                    key.Disposed += _property.Key_Disposed;
                    window.Property = value;
                }
            }

            public static bool TryGetValue(K key, [NotNullWhen(true)] out V? window)
                => _property.TryGetValue(key, out window);
        }

        // Form.Menu Property

        public static MainMenu? GetMenu(this Form form)
            => Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.GetValue(form);

        public static void SetMenu(this Form form, MainMenu? menu)
            => Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.SetValue(form, menu);

        internal static MainMenuSupportFormNativeWindow? GetMainMenuSupportFormNativeWindow(this Form form)
        {
            if (Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.TryGetValue(form, out var window))
                return window;
            return null;
        }

        internal static bool TryGetMainMenuSupportFormNativeWindow(this Form form, [NotNullWhen(true)] out MainMenuSupportFormNativeWindow? window)
            => Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.TryGetValue(form, out window);

        internal static void MenuChanged(this Form form, MenuChangeKind change, Menu? menu)
        {
            if (Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.TryGetValue(form, out var window))
                window.MenuChanged(change, menu);
        }

        // Control.ContextMenu Property

        public static ContextMenu? GetContextMenu(this Control control)
            => Holder<Control, ContextMenuSupportControlNativeWindow, ContextMenu>.GetValue(control);

        public static void SetContextMenu(this Control control, ContextMenu? contextMenu)
            => Holder<Control, ContextMenuSupportControlNativeWindow, ContextMenu>.SetValue(control, contextMenu);
    }
}
