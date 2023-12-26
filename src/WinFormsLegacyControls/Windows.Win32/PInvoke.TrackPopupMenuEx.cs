using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL TrackPopupMenuEx(Menu menu, TRACK_POPUP_MENU_FLAGS uFlags, int x, int y, IWin32Window hwnd, nint lptpm)
        {
            BOOL result;
            unsafe
            {
                result = TrackPopupMenuEx((HMENU)menu.handle, (uint)uFlags, x, y, (HWND)hwnd.Handle, (TPMPARAMS*)lptpm);
            }
            GC.KeepAlive(menu);
            GC.KeepAlive(hwnd);
            return result;
        }

        public static BOOL TrackPopupMenuEx(Menu menu, TRACK_POPUP_MENU_FLAGS uFlags, int x, int y, IWin32Window hwnd, ref TPMPARAMS tpm)
        {
            BOOL result;
            unsafe
            {
                fixed (TPMPARAMS* lptpm = &tpm)
                {
                    result = TrackPopupMenuEx((HMENU)menu.handle, (uint)uFlags, x, y, (HWND)hwnd.Handle, lptpm);
                }
            }
            GC.KeepAlive(menu);
            GC.KeepAlive(hwnd);
            return result;
        }
    }
}
