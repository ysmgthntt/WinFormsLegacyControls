using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static int GetMenuItemCount(Menu menu)
        {
            int result = GetMenuItemCount((HMENU)menu.Handle);
            GC.KeepAlive(menu);
            return result;
        }
    }
}
