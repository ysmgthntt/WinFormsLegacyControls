// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Windows.Win32.UI.Controls;

internal unsafe struct ToolInfoWrapper<T>
    where T : /*IHandle<HWND>*/global::System.Windows.Forms.IWin32Window
{
    public TTTOOLINFOW Info;
    public string? Text { get; set; }
    [MaybeNull]
    private readonly T _handle;

    public ToolInfoWrapper(T handle, TOOLTIP_FLAGS flags = default, string? text = null)
    {
        Info = new TTTOOLINFOW
        {
            hwnd = (HWND)handle.Handle,
            uId = (nuint)(IntPtr)handle.Handle,
            uFlags = flags | TOOLTIP_FLAGS.TTF_IDISHWND
        };
        Text = text;
        _handle = handle;
    }

    public ToolInfoWrapper(T handle, IntPtr id, TOOLTIP_FLAGS flags = default, string? text = null, RECT rect = default)
    {
        Info = new TTTOOLINFOW
        {
            hwnd = (HWND)handle.Handle,
            uId = (nuint)id,
            uFlags = flags,
            rect = rect
        };
        Text = text;
        _handle = handle;
    }

    public LRESULT SendMessage(/*IHandle<HWND>*/global::System.Runtime.InteropServices.HandleRef sender, /*MessageId*/uint message, bool state = false)
    {
        //Info.cbSize = (uint)sizeof(TTTOOLINFOW);
        // VisualStyle が無効な場合、 sizeof(TTTOOLINFOW) だと TTM_ADDTOOLW が失敗する。
        Info.cbSize = (uint)(sizeof(TTTOOLINFOW) - IntPtr.Size);
        fixed (char* c = Text)
        fixed (void* i = &Info)
        {
            if (Text is not null)
            {
                Info.lpszText = c;
            }

            LRESULT result = PInvoke.SendMessage(sender, message, (WPARAM)(BOOL)state, (LPARAM)i);
            GC.KeepAlive(_handle);
            return result;
        }
    }
}
