using System.Drawing;

namespace System.Windows.Forms
{
    partial class MenuItem
    {
        private static IntPtr SetUpPalette(IntPtr dc, bool force, bool realizePalette)
        {
            IntPtr halftonePalette = Graphics.GetHalftonePalette();

            IntPtr result = PInvoke.SelectPalette((HDC)dc, (HPALETTE)halftonePalette, force);

            if (result != IntPtr.Zero && realizePalette)
            {
                PInvoke.RealizePalette((HDC)dc);
            }

            return result;
        }
    }
}
