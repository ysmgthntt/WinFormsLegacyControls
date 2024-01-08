using System.Windows.Forms;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal sealed class MenuShortcutProcessMessageFilter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == PInvoke.WM_KEYDOWN || m.Msg == PInvoke.WM_SYSKEYDOWN)
            {
                // Shortcut であり得る範囲内のみ処理
                Keys modifierKeys = Control.ModifierKeys;
                Keys keyData = (Keys)m.WParam;
                if (modifierKeys != 0 || (keyData >= Keys.F1 && keyData <= Keys.F12) || keyData == Keys.Insert || keyData == Keys.Delete)
                {
                    Control? target = Control.FromChildHandle(m.HWnd);
                    if (target is not null)
                    {
                        keyData |= modifierKeys;
                        Debug.WriteLine($"MenuShortcutProcessMessageFilter hWnd: {m.HWnd}, MSG: {m.Msg}, keyData: {keyData}, target: {target.Name}");
                        do
                        {
                            ContextMenu? contextMenu = target.GetContextMenu();
                            if (contextMenu is not null && contextMenu.ProcessCmdKey(ref m, keyData, target))
                                return true;

                            if (target is Form form && form.TryGetMainMenuSupportFormNativeWindow(out var window))
                            {
                                if (window.ProcessCmdKey(ref m, keyData))
                                    return true;
                            }

                            target = target.Parent;
                        }
                        while (target is not null);
                    }
                }
            }

            return false;
        }
    }
}
