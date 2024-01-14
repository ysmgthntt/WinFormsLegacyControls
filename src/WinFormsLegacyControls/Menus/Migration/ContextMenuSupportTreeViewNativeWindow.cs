using WinFormsLegacyControls.Migration;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal sealed class ContextMenuSupportTreeViewNativeWindow : ContextMenuSupportNativeWindowBase<TreeView>
        , ISupportNativeWindow<TreeView, TreeNodeContextMenuProperty, ContextMenuSupportTreeViewNativeWindow>
    {
        private ContextMenuSupportTreeViewNativeWindow(TreeView treeView)
            : base(treeView)
        {
            Target.NodeMouseClick += TreeView_NodeMouseClick;
        }

        public static ContextMenuSupportTreeViewNativeWindow Create(TreeView treeView)
            => new(treeView);

        protected override void OnDetach()
        {
            Target.NodeMouseClick -= TreeView_NodeMouseClick;
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
        private TreeNode? _lastClickedNode;
        private ContextMenu? _treeNodeContextMenu;

        private void TreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Node.ContextMenuStrip is null)
                _lastClickedNode = e.Node;
            else
                _lastClickedNode = null;
        }

        protected override void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                case MessageId.WM_REFLECT + PInvoke.WM_NOTIFY:
                    // NodeMouseClick -> WM_CONTEXTMENU が呼ばれる。
                    _lastClickedNode = null;
                    _showTreeViewContextMenu = true;
                    try
                    {
                        base.WndProc(ref m);
                    }
                    finally
                    {
                        _showTreeViewContextMenu = false;
                    }
                    _lastClickedNode = null;
                    break;

                case PInvoke.WM_CONTEXTMENU:
                    TreeView treeView = Target;
                    if (_showTreeViewContextMenu)
                    {
                        if (_lastClickedNode is not null && (_treeNodeContextMenu = _lastClickedNode.GetContextMenu()) is not null)
                            _treeNodeContextMenu.ShowAtCursorPos(treeView, treeView, TRACK_POPUP_MENU_FLAGS.TPM_VERTICAL);
                        else
                            base.WndProc(ref m);
                    }
                    else
                    {
                        // this is the Shift + F10 Case....
                        TreeNode treeNode = treeView.SelectedNode;
                        //if (treeNode != null && (treeNode.ContextMenu != null || treeNode.ContextMenuStrip != null))
                        if (treeNode is not null && (_treeNodeContextMenu = treeNode.GetContextMenu()) is not null)
                        {
                            Point client = new Point(treeNode.Bounds.X, treeNode.Bounds.Y + treeNode.Bounds.Height / 2);
                            // VisualStudio7 # 156, only show the context menu when clicked in the client area
                            if (treeView.ClientRectangle.Contains(client))
                            {
                                _treeNodeContextMenu.Show(treeView, client);
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

                // [spec]
                case PInvoke.WM_MENUCHAR:
                    if (_treeNodeContextMenu is not null)
                        _treeNodeContextMenu.WmMenuChar(ref m);
                    else
                        base.WndProc(ref m);
                    break;

                // [spec]
                case PInvoke.WM_EXITMENULOOP:
                    if (_treeNodeContextMenu is { } contextMenu)
                    {
                        _treeNodeContextMenu = null;
                        if (m.WParam != 0)
                            contextMenu.RaiseCollapse();
                        ControlAccessors.DefWndProc(Target, ref m);
                    }
                    else
                    {
                        base.WndProc(ref m);
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
