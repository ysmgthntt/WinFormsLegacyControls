using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace WinFormsLegacyControls.Migration
{
    internal static class ToolTipSupport
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Handle")]
        internal static extern IntPtr GetToolTipHandle(ToolTip toolTip);
    }
}
