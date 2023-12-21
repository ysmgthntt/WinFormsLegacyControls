using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL DrawMenuBar(IWin32Window hWnd)
        {
            BOOL result = DrawMenuBar((HWND)hWnd.Handle);
            GC.KeepAlive(hWnd);
            return result;
        }
    }
}
