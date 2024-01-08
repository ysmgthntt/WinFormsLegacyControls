using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static unsafe BOOL SetMenuItemInfo(Menu menu, uint item, BOOL fByPositon, ref MENUITEMINFOW mii)
        {
            BOOL result;
            fixed (MENUITEMINFOW* lpmii = &mii)
            {
                result = SetMenuItemInfo((HMENU)menu.handle, item, fByPositon, lpmii);
            }
            GC.KeepAlive(menu);
            return result;
        }
    }
}
