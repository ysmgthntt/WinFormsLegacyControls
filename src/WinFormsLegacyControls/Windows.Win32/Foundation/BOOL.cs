namespace Windows.Win32.Foundation
{
    partial struct BOOL
    {
        public static BOOL TRUE { get; } = new(true);

        public static BOOL FALSE { get; } = new(false);

        public bool IsTrue() => this;
    }
}
