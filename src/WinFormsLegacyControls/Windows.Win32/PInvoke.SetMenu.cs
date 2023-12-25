using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL SetMenu(IWin32Window hWnd, Menu menu)
        {
            BOOL result = SetMenu((HWND)hWnd.Handle, (HMENU)menu.handle);
            GC.KeepAlive(hWnd);
            GC.KeepAlive(menu);
            return result;
        }

        public static BOOL SetMenu(IWin32Window hWnd, HMENU hMenu)
        {
            BOOL result = SetMenu((HWND)hWnd.Handle, hMenu);
            GC.KeepAlive(hWnd);
            return result;
        }
    }
}
