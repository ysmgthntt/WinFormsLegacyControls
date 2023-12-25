﻿// https://github.com/dotnet/winforms/pull/2157/files#diff-a9c810ef421afef2fe48558e9290e42db90f749eb89335109e130b8d679314c6

#nullable disable

using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal sealed class MainMenuSupportFormNativeWindow : NativeWindow
    {
        private readonly Form _form;
        private MainMenu? _mainMenu;
        private MainMenu? _dummyMenu;
        private MainMenu? _curMenu;
        private MainMenu? _mergedMenu;

        public MainMenuSupportFormNativeWindow(Form form)
        {
            _form = form;
            _form.Disposed += Form_Disposed;
            _form.HandleCreated += Form_HandleCreated;
            //_form.HandleDestroyed += Form_HandleDestroyed;
            _form.VisibleChanged += Form_VisibleChanged;
            if (_form.IsHandleCreated)
                AssignHandle(_form.Handle);
        }

        public void Detach()
        {
            _form.Disposed -= Form_Disposed;
            _form.HandleCreated -= Form_HandleCreated;
            //_form.HandleDestroyed -= Form_HandleDestroyed;
            _form.VisibleChanged -= Form_VisibleChanged;
            ReleaseHandle();
        }

        private void Form_Disposed(object sender, EventArgs e)
        {
            FormDispose();
        }

        private void Form_HandleCreated(object sender, EventArgs e)
        {
            AssignHandle(_form.Handle);
            //protected override void CreateHandle()
            if (Menu != null || !_form.TopLevel || _form.IsMdiContainer)
                UpdateMenuHandles();
        }

        private void Form_HandleDestroyed(object sender, EventArgs e)
        {
            // WM_NCDESTROY のハンドリングが必要。かつ NativeWindow 内の WM_NCDESTROY で ReleaseHandle される。
            //ReleaseHandle();
        }

        private void Form_VisibleChanged(object sender, EventArgs e)
        {
            //protected override void SetVisibleCore(bool value)
            if (_form.IsMdiChild && !_form.Visible)
                InvalidateMergedMenu();
        }

        /// <summary>
        ///  Gets or sets the <see cref='MainMenu'/>
        ///  that is displayed in the form.
        /// </summary>
        public MainMenu Menu
        {
            //return (MainMenu)Properties.GetObject(PropMainMenu);
            get => _mainMenu;
            set
            {
                MainMenu mainMenu = Menu;

                if (mainMenu != value)
                {
                    if (mainMenu != null)
                    {
                        mainMenu.form = null;
                    }

                    //Properties.SetObject(PropMainMenu, value);
                    _mainMenu = value;

                    if (value != null)
                    {
                        if (value.form != null)
                        {
                            //value.form.Menu = null;
                            value.form.SetMenu(null);
                        }
                        value.form = _form;
                    }

                    if (/*formState[FormStateSetClientSize] == 1 &&*/ !_form.IsHandleCreated)
                    {
                        _form.ClientSize = _form.ClientSize;
                    }

                    MenuChanged(global::System.Windows.Forms.Menu.CHANGE_ITEMS, value);
                }
            }
        }

        // TODO:
        //★private Form MdiParentInternal
        //    InvalidateMergedMenu();

        /// <summary>
        ///  Gets the merged menu for the
        ///  form.
        /// </summary>
        public MainMenu MergedMenu
        {
            get
            {
                //Form formMdiParent = (Form)Properties.GetObject(PropFormMdiParent);
                Form formMdiParent = _form.MdiParent;
                if (formMdiParent == null)
                {
                    return null;
                }

                //MainMenu mergedMenu = (MainMenu)Properties.GetObject(PropMergedMenu);
                MainMenu mergedMenu = _mergedMenu;
                if (mergedMenu != null)
                {
                    return mergedMenu;
                }

                //MainMenu parentMenu = formMdiParent.Menu;
                MainMenu parentMenu = formMdiParent.GetMenu();
                MainMenu mainMenu = Menu;

                if (mainMenu == null)
                {
                    return parentMenu;
                }

                if (parentMenu == null)
                {
                    return mainMenu;
                }

                // Create a menu that merges the two and save it for next time.
                mergedMenu = new MainMenu
                {
                    ownerForm = _form
                };
                mergedMenu.MergeMenu(parentMenu);
                mergedMenu.MergeMenu(mainMenu);
                //Properties.SetObject(PropMergedMenu, mergedMenu);
                _mergedMenu = mergedMenu;
                return mergedMenu;
            }
        }

        //protected override void SetVisibleCore(bool value)

        //protected override void CreateHandle()

        //protected override void Dispose(bool disposing)
        private void FormDispose()
        {
            MainMenu mainMenu = Menu;

            // We should only dispose this form's menus!
            if (mainMenu != null && mainMenu.ownerForm == _form)
            {
                mainMenu.Dispose();
                //Properties.SetObject(PropMainMenu, null);
                _mainMenu = null;
            }

            //if (Properties.GetObject(PropCurMenu) != null)
            if (_curMenu is not null)
            {
                //Properties.SetObject(PropCurMenu, null);
                _curMenu = null;
            }

            MenuChanged(global::System.Windows.Forms.Menu.CHANGE_ITEMS, null);

            //MainMenu dummyMenu = (MainMenu)Properties.GetObject(PropDummyMenu);
            MainMenu dummyMenu = _dummyMenu;

            if (dummyMenu != null)
            {
                dummyMenu.Dispose();
                //Properties.SetObject(PropDummyMenu, null);
                _dummyMenu = null;
            }

            //MainMenu mergedMenu = (MainMenu)Properties.GetObject(PropMergedMenu);
            MainMenu mergedMenu = _mergedMenu;

            if (mergedMenu != null)
            {
                if (mergedMenu.ownerForm == _form || mergedMenu.form == null)
                {
                    mergedMenu.Dispose();
                }
                //Properties.SetObject(PropMergedMenu, null);
                _mergedMenu = null;
            }
        }

        /// <summary>
        ///  Invalidates the merged menu, forcing the menu to be recreated if
        ///  needed again.
        /// </summary>
        private void InvalidateMergedMenu()
        {
            // here, we just set the merged menu to null (indicating that the menu structure
            // needs to be rebuilt).  Then, we signal the parent to updated its menus.
            //if (Properties.ContainsObject(PropMergedMenu))
            if (_mergedMenu is not null)
            {
                //if (Properties.GetObject(PropMergedMenu) is MainMenu menu && menu.ownerForm == this)
                if (_mergedMenu is MainMenu menu && menu.ownerForm == _form)
                {
                    menu.Dispose();
                }
                //Properties.SetObject(PropMergedMenu, null);
                _mergedMenu = null;
            }

            Form parForm = _form.ParentForm;
            if (parForm != null)
            {
                //parForm.MenuChanged(0, parForm.Menu);
                parForm.MenuChanged(0, parForm.GetMenu());
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MdiClient")]
        private static extern MdiClient? GetMdiClient(Form form);

        // Package scope for menu interop
        internal void MenuChanged(int change, Menu menu)
        {
            Form parForm = _form.ParentForm;
            if (parForm != null && _form == parForm.ActiveMdiChild/*Internal*/)
            {
                parForm.MenuChanged(change, menu);
                return;
            }

            MdiClient? ctlClient = GetMdiClient(_form);

            switch (change)
            {
                case global::System.Windows.Forms.Menu.CHANGE_ITEMS:
                case global::System.Windows.Forms.Menu.CHANGE_MERGE:
                    if (ctlClient == null || !ctlClient.IsHandleCreated)
                    {
                        if (menu == Menu && change == global::System.Windows.Forms.Menu.CHANGE_ITEMS)
                        {
                            UpdateMenuHandles();
                        }

                        break;
                    }

                    // Tell the children to toss their mergedMenu.
                    if (_form.IsHandleCreated)
                    {
                        UpdateMenuHandles(null, false);
                    }

                    Control.ControlCollection children = ctlClient.Controls;
                    for (int i = children.Count; i-- > 0;)
                    {
                        Control ctl = children[i];
                        //if (ctl is Form && ctl.Properties.ContainsObject(PropMergedMenu))
                        if (ctl is Form form && form.TryGetMainMenuSupportFormNativeWindow(out var window) && window._mergedMenu is not null)
                        {
                            //if (ctl.Properties.GetObject(PropMergedMenu) is MainMenu mainMenu && mainMenu.ownerForm == ctl)
                            MainMenu mainMenu = window._mergedMenu;
                            if (mainMenu.ownerForm == ctl)
                            {
                                mainMenu.Dispose();
                            }
                            //ctl.Properties.SetObject(PropMergedMenu, null);
                            window._mergedMenu = null;
                        }
                    }

                    UpdateMenuHandles();
                    break;
                case global::System.Windows.Forms.Menu.CHANGE_VISIBLE:
                    //if (menu == Menu || (ActiveMdiChildInternal != null && menu == ActiveMdiChildInternal.Menu))
                    if (menu == Menu || (_form.ActiveMdiChild/*Internal*/ != null && menu == _form.ActiveMdiChild/*Internal*/.GetMenu()))
                    {
                        UpdateMenuHandles();
                    }
                    break;
                case global::System.Windows.Forms.Menu.CHANGE_MDI:
                    if (ctlClient != null && ctlClient.IsHandleCreated)
                    {
                        UpdateMenuHandles();
                    }
                    break;
            }
        }

        // TODO:
        //★protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        /*
            MainMenu curMenu = (MainMenu)Properties.GetObject(PropCurMenu);
            if (curMenu != null && curMenu.ProcessCmdKey(ref msg, keyData))
            {
                return true;
            }
        */

        private void UpdateMenuHandles()
        {
            Form form;

            // Forget the current menu.
            //if (Properties.GetObject(PropCurMenu) != null)
            if (_curMenu is not null)
            {
                //Properties.SetObject(PropCurMenu, null);
                _curMenu = null;
            }

            if (_form.IsHandleCreated)
            {
                if (!_form.TopLevel)
                {
                    UpdateMenuHandles(null, true);
                }
                else
                {
                    form = _form.ActiveMdiChild/*Internal*/;
                    if (form != null)
                    {
                        //UpdateMenuHandles(form.MergedMenuPrivate, true);
                        UpdateMenuHandles(form.GetMainMenuSupportFormNativeWindow()?.MergedMenu, true);
                    }
                    else
                    {
                        UpdateMenuHandles(Menu, true);
                    }
                }
            }
        }

        private int _updateMenuHandlesSuspendCount;

        private void UpdateMenuHandles(MainMenu menu, bool forceRedraw)
        {
            Debug.Assert(_form.IsHandleCreated, "shouldn't call when handle == 0");

            //int suspendCount = formStateEx[FormStateExUpdateMenuHandlesSuspendCount];
            int suspendCount = _updateMenuHandlesSuspendCount;
            if (suspendCount > 0 && menu != null)
            {
                //formStateEx[FormStateExUpdateMenuHandlesDeferred] = 1;
                _updateMenuHandlesSuspendCount = 1;
                return;
            }

            MainMenu curMenu = menu;
            if (curMenu != null)
            {
                curMenu.form = _form;
            }

            //if (curMenu != null || Properties.ContainsObject(PropCurMenu))
            if (curMenu is not null || _curMenu is not null)
            {
                //Properties.SetObject(PropCurMenu, curMenu);
                _curMenu = curMenu;
            }

            MdiClient? ctlClient = GetMdiClient(_form);

            if (ctlClient == null || !ctlClient.IsHandleCreated)
            {
                if (menu != null)
                {
                    //UnsafeNativeMethods.SetMenu(new HandleRef(this, Handle), new HandleRef(menu, menu.Handle));
                    IntPtr createHandle = menu.Handle;
                    BOOL setMenuResult = PInvoke.SetMenu(_form, menu);
                    Debug.Assert(setMenuResult == BOOL.TRUE);
                }
                else
                {
                    //UnsafeNativeMethods.SetMenu(new HandleRef(this, Handle), NativeMethods.NullHandleRef);
                    PInvoke.SetMenu(_form, HMENU.Null);
                }
            }
            else
            {
                Debug.Assert(_form.IsMdiContainer, "Not an MDI container!");
                // when both MainMenuStrip and Menu are set, we honor the win32 menu over
                // the MainMenuStrip as the place to store the system menu controls for the maximized MDI child.

                //MenuStrip mainMenuStrip = MainMenuStrip;
                //if (mainMenuStrip == null || menu != null)
                if (menu is not null)
                {  // We are dealing with a Win32 Menu; MenuStrip doesn't have control buttons.

                    // We have a MainMenu and we're going to use it

                    // We need to set the "dummy" menu even when a menu is being removed
                    // (set to null) so that duplicate control buttons are not placed on the menu bar when
                    // an ole menu is being removed.
                    // Make MDI forget the mdi item position.
                    //MainMenu dummyMenu = (MainMenu)Properties.GetObject(PropDummyMenu);
                    MainMenu dummyMenu = _dummyMenu;

                    if (dummyMenu == null)
                    {
                        dummyMenu = new MainMenu
                        {
                            ownerForm = _form
                        };
                        //Properties.SetObject(PropDummyMenu, dummyMenu);
                        _dummyMenu = dummyMenu;
                    }
                    //UnsafeNativeMethods.SendMessage(new HandleRef(ctlClient, ctlClient.Handle), WindowMessages.WM_MDISETMENU, dummyMenu.Handle, IntPtr.Zero);
                    PInvoke.SendMessage(ctlClient, PInvoke.WM_MDISETMENU, (WPARAM)_dummyMenu.Handle);

                    if (menu != null)
                    {

                        // Microsoft, 5/2/1998 - don't use Win32 native Mdi lists...
                        //
                        //UnsafeNativeMethods.SendMessage(new HandleRef(ctlClient, ctlClient.Handle), WindowMessages.WM_MDISETMENU, menu.Handle, IntPtr.Zero);
                        PInvoke.SendMessage(ctlClient, PInvoke.WM_MDISETMENU, (WPARAM)menu.Handle);
                    }
                }

                // (New fix: Only destroy Win32 Menu if using a MenuStrip)
                /*
                if (menu == null && mainMenuStrip != null)
                { // If MainMenuStrip, we need to remove any Win32 Menu to make room for it.
                    IntPtr hMenu = UnsafeNativeMethods.GetMenu(new HandleRef(this, Handle));
                    if (hMenu != IntPtr.Zero)
                    {
                    }
                }
                */
            }
            if (forceRedraw)
            {
                //SafeNativeMethods.DrawMenuBar(new HandleRef(this, Handle));
                PInvoke.DrawMenuBar(_form);
            }
            //formStateEx[FormStateExUpdateMenuHandlesDeferred] = 0;
            _updateMenuHandlesSuspendCount = 0;
        }

        //private void UpdateMdiControlStrip(bool maximized)

        /// <summary>
        ///  WM_INITMENUPOPUP handler
        /// </summary>
        private void WmInitMenuPopup(ref Message m)
        {
            //MainMenu curMenu = (MainMenu)Properties.GetObject(PropCurMenu);
            MainMenu curMenu = _curMenu;
            if (curMenu != null)
            {

                //curMenu.UpdateRtl((RightToLeft == RightToLeft.Yes));

                if (curMenu.ProcessInitMenuPopup(m.WParam))
                {
                    return;
                }
            }
            base.WndProc(ref m);
        }

        /// <summary>
        ///  Handles the WM_MENUCHAR message
        /// </summary>
        private void WmMenuChar(ref Message m)
        {
            //MainMenu curMenu = (MainMenu)Properties.GetObject(PropCurMenu);
            MainMenu curMenu = _curMenu;
            if (curMenu == null)
            {

                //Form formMdiParent = (Form)Properties.GetObject(PropFormMdiParent);
                Form formMdiParent = _form.MdiParent;
                //if (formMdiParent != null && formMdiParent.Menu != null)
                if (formMdiParent?.GetMenu() is not null)
                {
                    //UnsafeNativeMethods.PostMessage(new HandleRef(formMdiParent, formMdiParent.Handle), WindowMessages.WM_SYSCOMMAND, new IntPtr(NativeMethods.SC_KEYMENU), m.WParam);
                    PInvoke.PostMessage(formMdiParent, PInvoke.WM_SYSCOMMAND, PInvoke.SC_KEYMENU, m.WParam);
                    //m.Result = (IntPtr)NativeMethods.Util.MAKELONG(0, 1);
                    m.Result = LRESULT.MAKELONG(0, 1);
                    return;
                }
            }
            if (curMenu != null)
            {
                curMenu.WmMenuChar(ref m);
                if (m.Result != IntPtr.Zero)
                {
                    // This char is a mnemonic on our menu.
                    return;
                }
            }

            base.WndProc(ref m);
        }

        /// <summary>
        ///  WM_NCDESTROY handler
        /// </summary>
        private void WmNCDestroy(ref Message m)
        {
            MainMenu mainMenu = Menu;
            /*
            MainMenu dummyMenu = (MainMenu)Properties.GetObject(PropDummyMenu);
            MainMenu curMenu = (MainMenu)Properties.GetObject(PropCurMenu);
            MainMenu mergedMenu = (MainMenu)Properties.GetObject(PropMergedMenu);
            */
            MainMenu dummyMenu = _dummyMenu;
            MainMenu curMenu = _curMenu;
            MainMenu mergedMenu = _mergedMenu;

            if (mainMenu != null)
            {
                mainMenu.ClearHandles();
            }
            if (curMenu != null)
            {
                curMenu.ClearHandles();
            }
            if (mergedMenu != null)
            {
                mergedMenu.ClearHandles();
            }
            if (dummyMenu != null)
            {
                dummyMenu.ClearHandles();
            }

            base.WndProc(ref m);
        }

        /// <summary>
        ///  WM_UNINITMENUPOPUP handler
        /// </summary>
        private void WmUnInitMenuPopup(ref Message m)
        {
            if (Menu != null)
            {
                //Whidbey addition - also raise the MainMenu.Collapse event for the current menu
                Menu.OnCollapse(EventArgs.Empty);
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                case PInvoke.WM_INITMENUPOPUP:
                    WmInitMenuPopup(ref m);
                    break;
                case PInvoke.WM_UNINITMENUPOPUP:
                    WmUnInitMenuPopup(ref m);
                    break;
                case PInvoke.WM_MENUCHAR:
                    WmMenuChar(ref m);
                    break;
                case PInvoke.WM_NCDESTROY:
                    WmNCDestroy(ref m);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }
}
