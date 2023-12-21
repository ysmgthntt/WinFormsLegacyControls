using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL TrackPopupMenuEx(Menu menu, uint uFlags, int x, int y, Control control, nint lptpm)
        {
            BOOL result;
            unsafe
            {
                result = TrackPopupMenuEx((HMENU)menu.Handle, uFlags, x, y, (HWND)control.Handle, (TPMPARAMS*)lptpm);
            }
            GC.KeepAlive(menu);
            GC.KeepAlive(control);
            return result;
        }
    }
}
