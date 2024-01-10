namespace Windows.Win32
{
    partial class PInvoke
    {
        public static BOOL SetForegroundWindow(IWin32Window hWnd)
        {
            BOOL result = SetForegroundWindow((HWND)hWnd.Handle);
            GC.KeepAlive(hWnd);
            return result;
        }
    }
}
