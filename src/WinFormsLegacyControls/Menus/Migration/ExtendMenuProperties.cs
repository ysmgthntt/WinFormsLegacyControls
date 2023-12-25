using System.Collections.Generic;
using System.Windows.Forms;
using WinFormsLegacyControls.Menus.Migration;

namespace WinFormsLegacyControls
{
    public static class ExtendMenuProperties
    {
        private static readonly Dictionary<Form, MainMenuSupportFormNativeWindow> _mainManus = new();

        private static void Key_Disposed<K, V>(this Dictionary<K, V> dictionary, object? sender, EventArgs e)
            where K : notnull
        {
            if (sender is K key)
                dictionary.Remove(key);
        }

        public static MainMenu? GetMenu(this Form form)
        {
            ArgumentNullException.ThrowIfNull(form);

            if (_mainManus.TryGetValue(form, out var window))
                return window.Menu;
            return null;
        }

        public static void SetMenu(this Form form, MainMenu? menu)
        {
            ArgumentNullException.ThrowIfNull(form);
            ObjectDisposedException.ThrowIf(form.IsDisposed, form);

            if (_mainManus.TryGetValue(form, out var window))
            {
                window.Menu = menu;
                if (menu is null)
                {
                    window.Detach();
                    form.Disposed -= _mainManus.Key_Disposed;
                    _mainManus.Remove(form);
                }
            }
            else if (menu is not null)
            {
                window = new MainMenuSupportFormNativeWindow(form);
                _mainManus[form] = window;
                form.Disposed += _mainManus.Key_Disposed;
                window.Menu = menu;
            }
        }

        internal static MainMenuSupportFormNativeWindow? GetMainMenuSupportFormNativeWindow(this Form form)
        {
            if (_mainManus.TryGetValue(form, out var window))
                return window;
            return null;
        }

        internal static bool TryGetMainMenuSupportFormNativeWindow(this Form form, [NotNullWhen(true)] out MainMenuSupportFormNativeWindow? window)
            => _mainManus.TryGetValue(form, out window);

        internal static void MenuChanged(this Form form, int change, Menu menu)
        {
            if (_mainManus.TryGetValue(form, out var window))
                window.MenuChanged(change, menu);
        }
    }
}
