using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL RemoveMenu(Menu menu, uint uPosition, MENU_ITEM_FLAGS uFlags)
        {
            BOOL result = RemoveMenu((HMENU)menu._handle, uPosition, uFlags);
            GC.KeepAlive(menu);
            return result;
        }
    }
}
