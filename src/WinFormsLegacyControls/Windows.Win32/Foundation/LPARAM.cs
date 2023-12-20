namespace Windows.Win32.Foundation
{
    partial struct LPARAM
    {
        public static unsafe implicit operator void*(LPARAM value) => (void*)value.Value;
        public static unsafe implicit operator LPARAM(void* value) => new((nint)value);

        public static explicit operator uint(LPARAM value) => (uint)(int)value.Value;
        public static explicit operator LPARAM(uint value) => new((nint)(nuint)value);

        public static LPARAM MAKELPARAM(int low, int high) => (LPARAM)(uint)((int)(((ushort)(((nuint)low) & 0xffff))
            | ((uint)((ushort)(((nuint)high) & 0xffff))) << 16));
    }
}
