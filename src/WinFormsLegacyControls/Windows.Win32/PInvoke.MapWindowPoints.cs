using System.Drawing;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static unsafe int MapWindowPoints(HWND hWndFrom, HWND hWndTo, ref RECT rect)
        {
            fixed (void* lpPoint = &rect)
            {
                return MapWindowPoints(hWndFrom, hWndTo, (Point*)lpPoint, 2);
            }
        }
    }
}
