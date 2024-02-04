using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static unsafe BOOL GetMenuItemInfo(Menu menu, uint item, BOOL fByPosition, MENUITEMINFOW* lpmii)
        {
            BOOL result;
            result = GetMenuItemInfo((HMENU)menu._handle, item, fByPosition, lpmii);
            GC.KeepAlive(menu);
            return result;
        }
    }
}
