using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL DestroyMenu(Menu menu)
        {
            BOOL result = DestroyMenu((HMENU)menu._handle);
            GC.KeepAlive(menu);
            return result;
        }
    }
}
