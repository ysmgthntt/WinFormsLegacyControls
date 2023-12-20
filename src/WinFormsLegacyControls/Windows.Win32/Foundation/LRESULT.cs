namespace Windows.Win32.Foundation
{
    partial struct LRESULT
    {
        public ushort HIWORD => (ushort)((((nuint)Value) >> 16) & 0xffff);

        public ushort LOWORD => (ushort)(((nuint)Value) & 0xffff);
    }
}
