// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;
using static Interop;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    partial class StatusBar
    {
        /// <summary>
        ///  This is a tooltip control that provides tips for a single
        ///  control. Each "tool" region is defined by a rectangle and
        ///  the string that should be displayed. This implementation
        ///  is based on System.Windows.Forms.ToolTip, but this control
        ///  is lighter weight and provides less functionality... however
        ///  this control binds to rectangular regions, instead of
        ///  full controls.
        /// </summary>
        private sealed class ControlToolTip : IWin32Window
        {
            public sealed class Tool
            {
                public Rectangle rect = Rectangle.Empty;
                public string? text;
                internal IntPtr _id = new IntPtr(-1);
            }

            private Dictionary<StatusBarPanel, Tool>? _tools;
            private ToolTipNativeWindow? _window;
            private readonly StatusBar _parent;
            private int _nextId;

            /// <summary>
            ///  Creates a new ControlToolTip.
            /// </summary>
            public ControlToolTip(StatusBar parent)
            {
                _parent = parent;
            }

            /// <summary>
            ///  Returns the createParams to create the window.
            /// </summary>
            private static CreateParams CreateParams
            {
                get
                {
                    // 親コントロールの StatusBar.CreateHandle() にて、
                    // ICC_BAR_CLASSES で InitCommonControlsEx されている。
                    // ICC_BAR_CLASSES は ToolTip も含んでいるため、本処理は不要。
                    /*
                    unsafe
                    {
                        PInvoke.InitCommonControlsEx(new INITCOMMONCONTROLSEX
                        {
                            dwSize = (uint)sizeof(INITCOMMONCONTROLSEX),
                            dwICC = INITCOMMONCONTROLSEX_ICC.ICC_TAB_CLASSES
                        });
                    }
                    */

                    CreateParams cp = new CreateParams();
                    cp.Parent = IntPtr.Zero;
                    cp.ClassName = PInvoke.TOOLTIPS_CLASS;
                    cp.Style |= (int)PInvoke.TTS_ALWAYSTIP;
                    cp.ExStyle = 0;
                    cp.Caption = null;
                    return cp;
                }
            }

            public IntPtr Handle
            {
                get
                {
                    if (!IsHandleCreated)
                    {
                        CreateHandle();
                    }
                    return _window.Handle;
                }
            }

            [MemberNotNullWhen(true, nameof(_window))]
            private bool IsHandleCreated
                => _window is not null && _window.Handle != IntPtr.Zero;

            private void AssignId(Tool tool)
            {
                tool._id = (IntPtr)_nextId;
                _nextId++;
            }

            /// <summary>
            ///  Sets the tool for the specified key. Keep in mind
            ///  that as soon as setTool is called, the handle for
            ///  the ControlToolTip is created, and the handle for
            ///  the parent control is also created. If the parent
            ///  handle is recreated in the future, all tools must
            ///  be re-added. The old tool for the specified key
            ///  will be removed. Passing null in for the
            ///  tool parameter will result in the tool
            ///  region being removed.
            /// </summary>
            public void SetTool(StatusBarPanel key, Tool? tool)
            {
                bool remove = false;
                bool add = false;
                bool update = false;

                Tool? toRemove = null;
                _tools?.TryGetValue(key, out toRemove);

                if (toRemove is not null)
                {
                    remove = true;
                }
                if (tool is not null)
                {
                    add = true;
                }
                if (tool is not null && toRemove is not null
                    && tool._id == toRemove._id)
                {
                    update = true;
                }

                if (update)
                {
                    UpdateTool(tool!);
                }
                else
                {
                    if (remove)
                    {
                        RemoveTool(toRemove!);
                    }
                    if (add)
                    {
                        AddTool(tool!);
                    }
                }

                if (tool is not null)
                {
                    _tools ??= new();
                    _tools[key] = tool;
                }
                else
                {
                    _tools?.Remove(key);
                }
            }

            /// <summary>
            ///  Returns the tool associated with the specified key,
            ///  or null if there is no area.
            /// </summary>
            public Tool? GetTool(StatusBarPanel key)
            {
                Tool? tool = null;
                _tools?.TryGetValue(key, out tool);
                return tool;
            }

            // [fixed]
            // 指定した StatusBarPanel の ToolTipText が変更、削除できない。
            // TTM_SETTOOLINFOW, TTM_DELTOOLW も TTM_ADDTOOLW した Handle に送信する必要がある。
            private LRESULT SendMessage(ToolInfoWrapper<Control> info, uint message)
            {
                ToolTip? t = _parent._mainToolTip;
                HandleRef handle;
                if (t is not null)
                    handle = new HandleRef(t, WinFormsLegacyControls.Migration.ToolTipAccessors.GetHandle(t));
                else
                    handle = new HandleRef(this, Handle);
                return info.SendMessage(handle, message);
            }

            private void AddTool(Tool tool)
            {
                if (tool is { text.Length: > 0 })
                {
                    ToolInfoWrapper<Control> info = GetTOOLINFO(tool);
                    if (SendMessage(info, PInvoke.TTM_ADDTOOLW) == 0)
                    {
                        throw new InvalidOperationException(SR.StatusBarAddFailed);
                    }
                }
            }

            private void RemoveTool(Tool tool)
            {
                if (tool is { text.Length: > 0, _id: >= 0 })
                {
                    ToolInfoWrapper<Control> info = GetMinTOOLINFO(tool);
                    SendMessage(info, PInvoke.TTM_DELTOOLW);
                }
            }

            private void UpdateTool(Tool tool)
            {
                if (tool is { text.Length: > 0, _id: >= 0 })
                {
                    ToolInfoWrapper<Control> info = GetTOOLINFO(tool);
                    SendMessage(info, PInvoke.TTM_SETTOOLINFOW);
                }
            }

            /// <summary>
            ///  Creates the handle for the control.
            /// </summary>
            [MemberNotNull(nameof(_window))]
            private void CreateHandle()
            {
                _window ??= new();
                _window.CreateHandle(CreateParams);
                PInvoke.SetWindowPos(
                    this,
                    HWND.HWND_TOPMOST,
                    0, 0, 0, 0,
                    SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

                // Setting the max width has the added benefit of enabling multiline tool tips
                PInvoke.SendMessage(this, PInvoke.TTM_SETMAXTIPWIDTH, 0, (LPARAM)SystemInformation.MaxWindowTrackSize.Width);
            }

            /// <summary>
            ///  Destroys the handle for this control.
            /// </summary>
            private void DestroyHandle(bool disposing)
            {
                if (IsHandleCreated)
                {
                    _window.DestroyHandle();
                }
                if (disposing)
                {
                    _tools?.Clear();
                }
            }

            /// <summary>
            ///  Disposes of the component.  Call dispose when the component is no longer needed.
            ///  This method removes the component from its container (if the component has a site)
            ///  and triggers the dispose event.
            /// </summary>
            public void Dispose()
            {
                DestroyHandle(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            ///  Returns a new instance of the TOOLINFO_T structure with the minimum
            ///  required data to uniquely identify a region. This is used primarily
            ///  for delete operations. NOTE: This cannot force the creation of a handle.
            /// </summary>
            private ToolInfoWrapper<Control> GetMinTOOLINFO(Tool tool)
            {
                if ((int)tool._id < 0)
                {
                    AssignId(tool);
                }

                return new ToolInfoWrapper<Control>(
                    _parent,
                    // [fixed]
                    // https://github.com/dotnet/winforms/pull/1612/files#diff-8d43c48c6ec7a62bb08bf0bb5f4669378adcba9417e33f554f9ff8fdab508aef
                    //id: parent is StatusBar sb ? sb.Handle : tool.id);
                    //id: parent is StatusBar sb && sb.mainToolTip is not null ? sb.Handle : tool.id);
                    // 指定した StatusBarPanel の ToolTipText が変更、削除できない。
                    // TTM_SETTOOLINFOW, TTM_DELTOOLW は TTTOOLINFOW.uId を使用して対象を識別する。
                    id: tool._id);
            }

            /// <summary>
            ///  Returns a detailed TOOLINFO_T structure that represents the specified
            ///  region. NOTE: This may force the creation of a handle.
            /// </summary>
            private ToolInfoWrapper<Control> GetTOOLINFO(Tool tool)
            {
                ToolInfoWrapper<Control> ti = GetMinTOOLINFO(tool);
                ti.Info.uFlags |= TOOLTIP_FLAGS.TTF_TRANSPARENT | TOOLTIP_FLAGS.TTF_SUBCLASS;

                // RightToLeft reading order
                if (_parent.RightToLeft == RightToLeft.Yes)
                {
                    ti.Info.uFlags |= TOOLTIP_FLAGS.TTF_RTLREADING;
                }

                ti.Text = tool.text;
                ti.Info.rect = tool.rect;
                return ti;
            }

            ~ControlToolTip()
            {
                DestroyHandle(false);
            }

            private sealed class ToolTipNativeWindow : NativeWindow
            {
                protected override void WndProc(ref Message m)
                {
                    switch ((uint)m.Msg)
                    {
                        case PInvoke.WM_SETFOCUS:
                            return;
                        default:
                            base.WndProc(ref m);
                            break;
                    }
                }
            }
        }
    }
}
