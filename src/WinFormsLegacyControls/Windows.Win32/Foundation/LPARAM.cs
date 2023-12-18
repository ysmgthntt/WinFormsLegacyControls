namespace Windows.Win32.Foundation
{
    partial struct LPARAM
    {
        public static unsafe implicit operator void*(LPARAM value) => (void*)value.Value;
        public static unsafe implicit operator LPARAM(void* value) => new((nint)value);
    }
}
