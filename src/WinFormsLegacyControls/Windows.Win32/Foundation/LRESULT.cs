namespace Windows.Win32.Foundation
{
    partial struct LRESULT
    {
        public ushort HIWORD => (ushort)((((nuint)Value) >> 16) & 0xffff);

        public ushort LOWORD => (ushort)(((nuint)Value) & 0xffff);

        public static LRESULT MAKELONG(int low, int high) => (LRESULT)((int)(((ushort)(((nuint)low) & 0xffff))
            | ((uint)((ushort)(((nuint)high) & 0xffff))) << 16));
    }
}
