using System.Runtime.InteropServices;

internal class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NMTOOLBAR
    {
        public NMHDR hdr;
        public int iItem;
        public TBBUTTON tbButton;
        public int cchText;
        public IntPtr pszText;
    }

    // CsWin32 だと x86 と x64 で定義が変わる。
    [StructLayout(LayoutKind.Sequential)]
    public struct TBBUTTON
    {
        public int iBitmap;
        public int idCommand;
        public byte fsState;
        public byte fsStyle;
        public byte bReserved0;
        public byte bReserved1;
        // x64 では 4 byte padding される。
        public IntPtr dwData;
        public IntPtr iString;
    }
}
