using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL SetWindowPos(IWin32Window hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, SET_WINDOW_POS_FLAGS uFlags)
        {
            BOOL result = SetWindowPos((HWND)hWnd.Handle, hWndInsertAfter, X, Y, cx, cy, uFlags);
            GC.KeepAlive(hWnd);
            return result;
        }
    }
}
