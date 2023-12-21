using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL TrackPopupMenuEx(Menu menu, uint uFlags, int x, int y, IWin32Window hwnd, nint lptpm)
        {
            BOOL result;
            unsafe
            {
                result = TrackPopupMenuEx((HMENU)menu.Handle, uFlags, x, y, (HWND)hwnd.Handle, (TPMPARAMS*)lptpm);
            }
            GC.KeepAlive(menu);
            GC.KeepAlive(hwnd);
            return result;
        }
    }
}
