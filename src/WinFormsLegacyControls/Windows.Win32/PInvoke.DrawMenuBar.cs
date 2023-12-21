using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL DrawMenuBar(Control control)
        {
            BOOL result = DrawMenuBar((HWND)control.Handle);
            GC.KeepAlive(control);
            return result;
        }
    }
}
