using System.Drawing;
using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static unsafe BOOL ClientToScreen(IWin32Window hWnd, Point* lpPoint)
        {
            BOOL result = ClientToScreen((HWND)hWnd.Handle, lpPoint);
            GC.KeepAlive(hWnd);
            return result;
        }
    }
}
