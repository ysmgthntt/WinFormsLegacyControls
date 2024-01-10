using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL PostMessage(IWin32Window hWnd, uint Msg, WPARAM wParam = default, LPARAM lParam = default)
        {
            BOOL result = PostMessage((HWND)hWnd.Handle, Msg, wParam, lParam);
            GC.KeepAlive(hWnd);
            return result;
        }
    }
}
