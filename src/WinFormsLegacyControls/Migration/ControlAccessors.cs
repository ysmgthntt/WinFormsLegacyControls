using System.Runtime.CompilerServices;

namespace WinFormsLegacyControls.Migration
{
    internal static class ControlAccessors
    {
        // DefWndProc is virtual method
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DefWndProc")]
        public static extern void DefWndProc(Control control, ref Message msg);
    }
}
