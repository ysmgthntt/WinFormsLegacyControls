using System.Drawing;
using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL ClientToScreen(IWin32Window hWnd, ref Point lpPoint)
        {
            BOOL result = ClientToScreen((HWND)hWnd.Handle, ref lpPoint);
            GC.KeepAlive(hWnd);
            return result;
        }
    }
}
