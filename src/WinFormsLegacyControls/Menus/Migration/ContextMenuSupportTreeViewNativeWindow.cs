namespace WinFormsLegacyControls.Menus.Migration
{
    internal class ContextMenuSupportTreeViewNativeWindow : ContextMenuSupportControlNativeWindow
        , ISupportNativeWindow<TreeView, TreeNodeContextMenuProperty, ContextMenuSupportTreeViewNativeWindow>
    {
        private readonly TreeView _treeView;

        private ContextMenuSupportTreeViewNativeWindow(TreeView treeView)
            : base(treeView)
        {
            _treeView = treeView;
            _treeView.NodeMouseClick += TreeView_NodeMouseClick;
        }

        public static ContextMenuSupportTreeViewNativeWindow Create(TreeView treeView)
            => new(treeView);

        protected override void DetachCore()
        {
            _treeView.NodeMouseClick -= TreeView_NodeMouseClick;
        }

        private TreeNodeContextMenuProperty? _property;

        public TreeNodeContextMenuProperty? Property
        {
            get => _property;
            set
            {
                _property = value;
                ContextMenu = value?.ContextMenu;
            }
        }

        private bool _showTreeViewContextMenu;
        private TreeNode? _lastClickNode;

        private void TreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Node.ContextMenuStrip is null)
                _lastClickNode = e.Node;
            else
                _lastClickNode = null;
        }

        protected override void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                case MessageId.WM_REFLECT + PInvoke.WM_NOTIFY:
                    // NodeMouseClick -> WM_CONTEXTMENU が呼ばれる。
                    _lastClickNode = null;
                    _showTreeViewContextMenu = true;
                    try
                    {
                        base.WndProc(ref m);
                    }
                    finally
                    {
                        _showTreeViewContextMenu = false;
                    }
                    _lastClickNode = null;
                    break;

                case PInvoke.WM_CONTEXTMENU:
                    if (_showTreeViewContextMenu)
                    {
                        if (_lastClickNode?.GetContextMenu() is { } contextMenu)
                            contextMenu.ShowAtCursorPos(_treeView, _treeView, TRACK_POPUP_MENU_FLAGS.TPM_VERTICAL);
                        else
                            base.WndProc(ref m);
                    }
                    else
                    {
                        // this is the Shift + F10 Case....
                        TreeNode treeNode = _treeView.SelectedNode;
                        if (treeNode is not null && treeNode.GetContextMenu() is { } contextMenu)
                        {
                            Point client = new Point(treeNode.Bounds.X, treeNode.Bounds.Y + treeNode.Bounds.Height / 2);
                            // VisualStudio7 # 156, only show the context menu when clicked in the client area
                            if (_treeView.ClientRectangle.Contains(client))
                            {
                                contextMenu.Show(_treeView, client);
                            }
                        }
                        else
                        {
                            // in this case we dont have a selected node.  The base
                            // will ensure we're constrained to the client area.
                            base.WndProc(ref m);
                        }
                    }
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }

    internal sealed class TreeNodeContextMenuProperty(ContextMenu? contextMenu, bool contextMenuForTreeNodesEnabled)
    {
        public ContextMenu? ContextMenu = contextMenu;
        public bool ContextMenuForTreeNodesEnabled = contextMenuForTreeNodesEnabled;
    }
}
