using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using WinFormsLegacyControls.Menus.Migration;

namespace WinFormsLegacyControls
{
    public static class ExtendMenuProperties
    {
        private static bool s_messageFilterInstalled;

        private static void Key_Disposed<K, V>(this Dictionary<K, V> dictionary, object? sender, EventArgs e)
            where K : notnull
        {
            if (sender is K key)
                dictionary.Remove(key);
        }

        private static class Holder<K, V, P>
            where K : notnull, Component
            where V : ISupportNativeWindow<K, P, V>
        {
            private static readonly Dictionary<K, V> s_property = new();

            public static P? GetValue(K key)
            {
                ArgumentNullException.ThrowIfNull(key);

                if (s_property.TryGetValue(key, out var window))
                    return window.Property;
                return default;
            }

            public static V? SetValue(K key, P? value)
            {
                ArgumentNullException.ThrowIfNull(key);
                ObjectDisposedException.ThrowIf(key is Control { IsDisposed: true }, key);

                if (s_property.TryGetValue(key, out var window))
                {
                    window.Property = value;
                    if (value is null)
                    {
                        window.Detach();
                        key.Disposed -= s_property.Key_Disposed;
                        s_property.Remove(key);
                        return default;
                    }
                }
                else if (value is not null)
                {
                    window = CreateWindow(key);
                    window.Property = value;
                }
                return window;
            }

            public static V CreateWindow(K key)
            {
                if (!s_messageFilterInstalled)
                {
                    Application.AddMessageFilter(new MenuShortcutProcessMessageFilter());
                    s_messageFilterInstalled = true;
                }
                V window = V.Create(key);
                s_property[key] = window;
                key.Disposed += s_property.Key_Disposed;
                return window;
            }

            public static bool TryGetWindow(K key, [NotNullWhen(true)] out V? window)
                => s_property.TryGetValue(key, out window);
        }

        // Form.Menu Property

        public static MainMenu? GetMenu(this Form form)
            => Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.GetValue(form);

        public static void SetMenu(this Form form, MainMenu? menu)
        {
            if (menu is null && form.IsMdiContainer && form.TryGetMainMenuSupportFormNativeWindow(out var window))
                window.Menu = menu;
            else
                Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.SetValue(form, menu);
        }

        internal static MainMenuSupportFormNativeWindow? GetMainMenuSupportFormNativeWindow(this Form form)
        {
            Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.TryGetWindow(form, out var window);
            return window;
        }

        internal static bool TryGetMainMenuSupportFormNativeWindow(this Form form, [NotNullWhen(true)] out MainMenuSupportFormNativeWindow? window)
            => Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.TryGetWindow(form, out window);

        internal static MainMenuSupportFormNativeWindow? CreateMainMenuSupportFormNativeWindow(this Form form)
            => Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.CreateWindow(form);

        internal static void MenuChanged(this Form form, MenuChangeKind change, Menu? menu)
        {
            if (Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.TryGetWindow(form, out var window))
                window.MenuChanged(change, menu);
        }

        // Control.ContextMenu Property

        public static ContextMenu? GetContextMenu(this Control control)
            => control switch
            {
                TreeView treeView => Holder<TreeView, ContextMenuSupportTreeViewNativeWindow, TreeNodeContextMenuProperty>.GetValue(treeView)?.ContextMenu,
                ComboBox comboBox => Holder<ComboBox, ContextMenuSupportComboBoxNativeWindow, ContextMenu>.GetValue(comboBox),
                _ => Holder<Control, ContextMenuSupportControlNativeWindow, ContextMenu>.GetValue(control),
            };

        public static void SetContextMenu(this Control control, ContextMenu? contextMenu)
        {
            if (control is TreeView treeView)
            {
                if (Holder<TreeView, ContextMenuSupportTreeViewNativeWindow, TreeNodeContextMenuProperty>.TryGetWindow(treeView, out var window))
                {
                    // Remove
                    if (contextMenu is null && (window.Property is null || !window.Property.ContextMenuForTreeNodesEnabled))
                        Holder<TreeView, ContextMenuSupportTreeViewNativeWindow, TreeNodeContextMenuProperty>.SetValue(treeView, null);
                    else if (window.Property is not null)
                        window.Property.ContextMenu = contextMenu;
                    else
                        window.Property = new(contextMenu, false);
                }
                else if (contextMenu is not null)
                {
                    Holder<TreeView, ContextMenuSupportTreeViewNativeWindow, TreeNodeContextMenuProperty>.SetValue(treeView, new(contextMenu, false));
                }
            }
            else if (control is ComboBox comboBox)
            {
                Holder<ComboBox, ContextMenuSupportComboBoxNativeWindow, ContextMenu>.SetValue(comboBox, contextMenu);
            }
            else
            {
                Holder<Control, ContextMenuSupportControlNativeWindow, ContextMenu>.SetValue(control, contextMenu);
            }
        }

        internal static void SetContextMenu(this Control control, ContextMenu? contextMenu, Control sourceControl)
        {
            if (Holder<Control, ContextMenuSupportControlNativeWindow, ContextMenu>.SetValue(control, contextMenu) is { } window)
                window.SourceControl = sourceControl;
        }

        // NotifyIcon.ContextMenu Property

        public static ContextMenu? GetContextMenu(this NotifyIcon notifyIcon)
        => Holder<NotifyIcon, ContextMenuSupportNotifyIconNativeWindow, ContextMenu>.GetValue(notifyIcon);

        public static void SetContextMenu(this NotifyIcon notifyIcon, ContextMenu? contextMenu)
            => Holder<NotifyIcon, ContextMenuSupportNotifyIconNativeWindow, ContextMenu>.SetValue(notifyIcon, contextMenu);

        // TreeView.ContextMenuForTreeNodesEnabled Property

        public static bool GetContextMenuForTreeNodesEnabled(this TreeView treeView)
        {
            if (Holder<TreeView, ContextMenuSupportTreeViewNativeWindow, TreeNodeContextMenuProperty>.TryGetWindow(treeView, out var window))
            {
                if (window.Property is not null)
                    return window.Property.ContextMenuForTreeNodesEnabled;
            }
            return false;
        }

        public static void SetContextMenuForTreeNodesEnabled(this TreeView treeView, bool enabled)
        {
            if (Holder<TreeView, ContextMenuSupportTreeViewNativeWindow, TreeNodeContextMenuProperty>.TryGetWindow(treeView, out var window))
            {
                // Remove
                if (!enabled && window.Property?.ContextMenu is null)
                    Holder<TreeView, ContextMenuSupportTreeViewNativeWindow, TreeNodeContextMenuProperty>.SetValue(treeView, null);
                else if (window.Property is not null)
                    window.Property.ContextMenuForTreeNodesEnabled = enabled;
                else
                    window.Property = new(null, enabled);
            }
            else if (enabled)
            {
                Holder<TreeView, ContextMenuSupportTreeViewNativeWindow, TreeNodeContextMenuProperty>.SetValue(treeView, new(null, enabled));
            }
        }

        // TreeNode.ContextMenu Property

        private static ConditionalWeakTable<TreeNode, ContextMenu?>? s_treeNodeContextMenus;

        public static ContextMenu? GetContextMenu(this TreeNode treeNode)
        {
            if (s_treeNodeContextMenus is null)
                return null;
            s_treeNodeContextMenus.TryGetValue(treeNode, out var contextMenu);
            return contextMenu;
        }

        public static void SetContextMenu(this TreeNode treeNode, ContextMenu? contextMenu)
        {
            if (s_treeNodeContextMenus is null)
            {
                if (contextMenu is null)
                    return;
                s_treeNodeContextMenus = new();
            }
            s_treeNodeContextMenus.AddOrUpdate(treeNode, contextMenu);
        }
    }
}
