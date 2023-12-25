using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL SetMenuDefaultItem(Menu menu, uint uItem, uint fByPos)
        {
            BOOL result = SetMenuDefaultItem((HMENU)menu.handle, uItem, fByPos);
            GC.KeepAlive(menu);
            return result;
        }
    }
}
