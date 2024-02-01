using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static unsafe BOOL InsertMenuItem(Menu menu, uint item, BOOL fByPosition, ref MENUITEMINFOW mi)
        {
            BOOL result;
            fixed (MENUITEMINFOW* lpmi = &mi)
            {
                result = InsertMenuItem((HMENU)menu._handle, item, fByPosition, lpmi);
            }
            GC.KeepAlive(menu);
            return result;
        }
    }
}
