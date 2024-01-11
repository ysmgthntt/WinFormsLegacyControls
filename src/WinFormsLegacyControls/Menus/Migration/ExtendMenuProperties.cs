using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
            where K : notnull, Component
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

            public static V? SetValue(K key, P? value)
            {
                ArgumentNullException.ThrowIfNull(key);
                ObjectDisposedException.ThrowIf(key is Control { IsDisposed: true }, key);

                if (_property.TryGetValue(key, out var window))
                {
                    window.Property = value;
                    if (value is null)
                    {
                        window.Detach();
                        key.Disposed -= _property.Key_Disposed;
                        _property.Remove(key);
                        return default;
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
                return window;
            }

            public static bool TryGetWindow(K key, [NotNullWhen(true)] out V? window)
                => _property.TryGetValue(key, out window);
        }

        // Form.Menu Property

        public static MainMenu? GetMenu(this Form form)
            => Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.GetValue(form);

        public static void SetMenu(this Form form, MainMenu? menu)
            => Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.SetValue(form, menu);

        internal static MainMenuSupportFormNativeWindow? GetMainMenuSupportFormNativeWindow(this Form form)
        {
            Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.TryGetWindow(form, out var window);
            return window;
        }

        internal static bool TryGetMainMenuSupportFormNativeWindow(this Form form, [NotNullWhen(true)] out MainMenuSupportFormNativeWindow? window)
            => Holder<Form, MainMenuSupportFormNativeWindow, MainMenu>.TryGetWindow(form, out window);

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

        private static ConditionalWeakTable<TreeNode, ContextMenu?>? _treeNodeContextMenus;

        public static ContextMenu? GetContextMenu(this TreeNode treeNode)
        {
            if (_treeNodeContextMenus is null)
                return null;
            _treeNodeContextMenus.TryGetValue(treeNode, out var contextMenu);
            return contextMenu;
        }

        public static void SetContextMenu(this TreeNode treeNode, ContextMenu? contextMenu)
        {
            if (_treeNodeContextMenus is null)
            {
                if (contextMenu is null)
                    return;
                _treeNodeContextMenus = new();
            }
            _treeNodeContextMenus.AddOrUpdate(treeNode, contextMenu);
        }
    }
}
