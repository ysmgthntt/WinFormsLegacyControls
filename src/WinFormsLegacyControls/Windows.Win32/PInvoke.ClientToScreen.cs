using System.Drawing;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL ClientToScreen<T>(T hWnd, ref Point lpPoint)
            where T : IHandle
        {
            BOOL result = ClientToScreen((HWND)hWnd.Handle, ref lpPoint);
            GC.KeepAlive(hWnd);
            return result;
        }
    }
}
