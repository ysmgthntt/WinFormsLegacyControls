#if DEBUG
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static NativeMethods;

namespace WinFormsLegacyControls.Migration
{
    internal static class Check
    {
        [ModuleInitializer]
        internal static unsafe void SizeOfCheck()
        {
            Debug.Assert(sizeof(INITCOMMONCONTROLSEX) == Marshal.SizeOf<INITCOMMONCONTROLSEX>());
            Debug.Assert(sizeof(MENUITEMINFOW) == Marshal.SizeOf<MENUITEMINFOW>());
            Debug.Assert(sizeof(TBBUTTON) == Marshal.SizeOf<TBBUTTON>());
            Debug.Assert(sizeof(TBBUTTONINFOW) == Marshal.SizeOf<TBBUTTONINFOW>());
            Debug.Assert(sizeof(TPMPARAMS) == Marshal.SizeOf<TPMPARAMS>());
        }
    }
}
#endif
