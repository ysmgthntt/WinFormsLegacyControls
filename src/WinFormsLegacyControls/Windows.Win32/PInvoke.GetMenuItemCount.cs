﻿using System.Windows.Forms;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static int GetMenuItemCount(Menu menu)
        {
            int result = GetMenuItemCount((HMENU)menu._handle);
            GC.KeepAlive(menu);
            return result;
        }
    }
}
