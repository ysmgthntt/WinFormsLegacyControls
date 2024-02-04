using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static unsafe BOOL SetMenuItemInfo(Menu menu, uint item, BOOL fByPositon, MENUITEMINFOW* lpmii)
        {
            BOOL result;
            result = SetMenuItemInfo((HMENU)menu._handle, item, fByPositon, lpmii);
            GC.KeepAlive(menu);
            return result;
        }
    }
}
