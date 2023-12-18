namespace Windows.Win32.Foundation
{
    partial struct WPARAM
    {
        public static explicit operator BOOL(WPARAM value) => (BOOL)(nint)value.Value;
        public static explicit operator WPARAM(BOOL value) => new((nuint)(nint)value);

        public static explicit operator int(WPARAM value) => (int)(nint)value.Value;
        public static explicit operator WPARAM(int value) => new((nuint)(nint)value);
    }
}
