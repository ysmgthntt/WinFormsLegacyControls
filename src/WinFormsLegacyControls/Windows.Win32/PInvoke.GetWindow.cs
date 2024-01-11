using System.Runtime.InteropServices;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static HWND GetWindow(IWin32Window hWnd, GET_WINDOW_CMD uCmd)
        {
            HWND result = GetWindow((HWND)hWnd.Handle, uCmd);
            GC.KeepAlive(hWnd);
            return result;
        }

        public static HWND GetWindow(HandleRef hWnd, GET_WINDOW_CMD uCmd)
        {
            HWND result = GetWindow((HWND)hWnd.Handle, uCmd);
            GC.KeepAlive(hWnd.Wrapper);
            return result;
        }
    }
}
