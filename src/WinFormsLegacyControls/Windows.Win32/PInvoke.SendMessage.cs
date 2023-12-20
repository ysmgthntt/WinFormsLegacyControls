using System;
using static Interop.User32;

namespace Windows.Win32
{
    partial class PInvoke
    {
        public static LRESULT SendMessage(
            IHandle hWnd,
            WM Msg,
            WPARAM wParam = default,
            LPARAM lParam = default)
        {
            LRESULT result = SendMessage((HWND)hWnd.Handle, (uint)Msg, wParam, lParam);
            GC.KeepAlive(hWnd);
            return result;
        }

        public static LRESULT SendMessage(
            IHandle hWnd,
            uint Msg,
            WPARAM wParam = default,
            LPARAM lParam = default)
        {
            LRESULT result = SendMessage((HWND)hWnd.Handle, Msg, wParam, lParam);
            GC.KeepAlive(hWnd);
            return result;
        }

        public static unsafe LRESULT SendMessage(
            IHandle hWnd,
            uint Msg,
            WPARAM wParam,
            string? lParam)
        {
            fixed (char* c = lParam)
            {
                return SendMessage(hWnd, Msg, wParam, (LPARAM)c);
            }
        }

        public static unsafe LRESULT SendMessage(
            IHandle hWnd,
            WM Msg,
            WPARAM wParam,
            string? lParam)
        {
            fixed (char* c = lParam)
            {
                return SendMessage(hWnd, Msg, wParam, (LPARAM)c);
            }
        }

        public static unsafe nint SendMessage<TLParam>(
            IHandle hWnd,
            uint Msg,
            WPARAM wParam,
            ref TLParam lParam)
            where TLParam : unmanaged
        {
            fixed (void* l = &lParam)
            {
                return SendMessage(hWnd, Msg, wParam, (LPARAM)l);
            }
        }

        public static unsafe nint SendMessage<TLParam>(
            IHandle hWnd,
            WM Msg,
            WPARAM wParam,
            ref TLParam lParam)
            where TLParam : unmanaged
        {
            fixed (void* l = &lParam)
            {
                return SendMessage(hWnd, Msg, wParam, (LPARAM)l);
            }
        }
    }
}
