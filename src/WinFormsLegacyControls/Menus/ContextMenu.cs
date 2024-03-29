﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    /// <summary>
    ///  This class is used to put context menus on your form and show them for
    ///  controls at runtime.  It basically acts like a regular Menu control,
    ///  but can be set for the ContextMenu property that most controls have.
    /// </summary>
    [DefaultEvent(nameof(Popup))]
    public partial class ContextMenu : Menu
    {
        private static readonly object s_popupEvent = new();
        private static readonly object s_collapseEvent = new();
        private Control? _sourceControl;

        private RightToLeft _rightToLeft = RightToLeft.Inherit;

        /// <summary>
        ///  Creates a new ContextMenu object with no items in it by default.
        /// </summary>
        public ContextMenu()
        {
        }

        /// <summary>
        ///  Creates a ContextMenu object with the given MenuItems.
        /// </summary>
        public ContextMenu(MenuItem[] menuItems)
            : base(menuItems)
        {
        }

        /// <summary>
        ///  The last control that was acted upon that resulted in this context
        ///  menu being displayed.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [SRDescription(nameof(SR.ContextMenuSourceControlDescr))]
        public Control? SourceControl => _sourceControl;

        [SRDescription(nameof(SR.MenuItemOnInitDescr))]
        public event EventHandler Popup
        {
            add => Events.AddHandler(s_popupEvent, value);
            remove => Events.RemoveHandler(s_popupEvent, value);
        }

        /// <summary>
        ///  Fires when the context menu collapses.
        /// </summary>
        [SRDescription(nameof(SR.ContextMenuCollapseDescr))]
        public event EventHandler Collapse
        {
            add => Events.AddHandler(s_collapseEvent, value);
            remove => Events.RemoveHandler(s_collapseEvent, value);
        }

        /// <summary>
        ///  This is used for international applications where the language
        ///  is written from RightToLeft. When this property is true,
        ///  text alignment and reading order will be from right to left.
        /// </summary>
        // Add a DefaultValue attribute so that the Reset context menu becomes
        // available in the Property Grid but the default value remains No.
        [Localizable(true)]
        [DefaultValue(RightToLeft.No)]
        [SRDescription(nameof(SR.MenuRightToLeftDescr))]
        public virtual RightToLeft RightToLeft
        {
            get
            {
                if (RightToLeft.Inherit == _rightToLeft)
                {
                    if (_sourceControl is not null)
                    {
                        return _sourceControl.RightToLeft;
                    }
                    else
                    {
                        return RightToLeft.No;
                    }
                }
                else
                {
                    return _rightToLeft;
                }
            }
            set
            {

                //valid values are 0x0 to 0x2.
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)RightToLeft.No, (int)RightToLeft.Inherit))
                {
                    throw new InvalidEnumArgumentException(nameof(RightToLeft), (int)value, typeof(RightToLeft));
                }
                if (RightToLeft != value)
                {
                    _rightToLeft = value;
                    UpdateRtl((value == RightToLeft.Yes));
                }

            }
        }

        internal override bool RenderIsRightToLeft => (_rightToLeft == RightToLeft.Yes);

        /// <summary>
        ///  Fires the popup event
        /// </summary>
        protected virtual void OnPopup(EventArgs e)
            => ((EventHandler?)Events[s_popupEvent])?.Invoke(this, e);

        /// <summary>
        ///  Fires the collapse event
        /// </summary>
        protected virtual void OnCollapse(EventArgs e)
            => ((EventHandler?)Events[s_collapseEvent])?.Invoke(this, e);

        protected internal virtual bool ProcessCmdKey(ref Message msg, Keys keyData, Control control)
        {
            _sourceControl = control;
            return ProcessCmdKey(ref msg, keyData);
        }

        /* DefaultValueAttribute が設定されているため、これらは使用されない。
        private void ResetRightToLeft()
        {
            RightToLeft = RightToLeft.No;
        }

        /// <summary>
        ///  Returns true if the RightToLeft should be persisted in code gen.
        /// </summary>
        internal virtual bool ShouldSerializeRightToLeft()
        {
            if (System.Windows.Forms.RightToLeft.Inherit == rightToLeft)
            {
                return false;
            }
            return true;
        }
        */

        /// <summary>
        ///  Displays the context menu at the specified position.  This method
        ///  doesn't return until the menu is dismissed.
        /// </summary>
        public void Show(Control control, Point pos)
        {
            Show(control, pos, TRACK_POPUP_MENU_FLAGS.TPM_VERTICAL | TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON);
        }

        /// <summary>
        ///  Displays the context menu at the specified position.  This method
        ///  doesn't return until the menu is dismissed.
        /// </summary>
        public void Show(Control control, Point pos, LeftRightAlignment alignment)
        {
            // This code below looks wrong but it's correct.
            // WinForms Left alignment means we want the menu to show up left of the point it is invoked from.
            // We specify TPM_RIGHTALIGN which tells win32 to align the right side of this
            // menu with the point (which aligns it Left visually)
            if (alignment == LeftRightAlignment.Left)
            {
                Show(control, pos, TRACK_POPUP_MENU_FLAGS.TPM_VERTICAL | TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN);
            }
            else
            {
                Show(control, pos, TRACK_POPUP_MENU_FLAGS.TPM_VERTICAL | TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN);
            }
        }

        private void Show(Control control, Point pos, TRACK_POPUP_MENU_FLAGS flags)
        {
            ArgumentNullException.ThrowIfNull(control);

            if (!control.IsHandleCreated || !control.Visible)
            {
                throw new ArgumentException(SR.ContextMenuInvalidParent, nameof(control));
            }

            _sourceControl = control;

            //OnPopup(EventArgs.Empty);
            RaisePopup();
            pos = control.PointToScreen(pos);
            //IntPtr createHandle = this.Handle;
            BOOL result = PInvoke.TrackPopupMenuEx(this, flags, pos.X, pos.Y, control, 0);
            Debug.Assert(result || ItemCount == 0);
        }

        //

        private static nint s_lastPopupHandle = -1;

        private void RaisePopup()
        {
            s_lastPopupHandle = this.Handle;
            OnPopup(EventArgs.Empty);
        }

        internal void RaiseCollapse()
        {
            // ToolBarButton.DropDownMenu と ToolBar.ContextMenu の両方設定されている場合、
            // ToolBarButton.DropDownMenu の Collapse 時 ToolBar.ContextMenu の Collapse も発生してしまう。
            // ContextMenu は NativeWindow で処理しており、 Control の WndProc より先に呼ばれるため、
            // ToolBarButton.DropDownMenu を ToolBar.WndProc で処理済でもキャンセルできない。
            if (s_lastPopupHandle == _handle)
            {
                s_lastPopupHandle = -1;
                OnCollapse(EventArgs.Empty);
            }
        }

        internal void ShowAtCursorPos(IWin32Window hWnd, Control? control, TRACK_POPUP_MENU_FLAGS flags)
        {
            // [spec]
            _sourceControl = control;

            PInvoke.GetCursorPos(out Point pt);

            // Summary: the current window must be made the foreground window
            // before calling TrackPopupMenuEx, and a task switch must be
            // forced after the call.
            PInvoke.SetForegroundWindow(hWnd);

            //OnPopup(EventArgs.Empty);
            //IntPtr createHandle = this.Handle;
            RaisePopup();

            BOOL result = PInvoke.TrackPopupMenuEx(this, flags, pt.X, pt.Y, hWnd, 0);
            Debug.Assert(result);

            // Force task switch (see above)
            PInvoke.PostMessage(hWnd, PInvoke.WM_NULL);
        }
    }
}
