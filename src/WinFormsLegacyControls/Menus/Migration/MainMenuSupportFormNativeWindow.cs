// https://github.com/dotnet/winforms/pull/2157/files#diff-a9c810ef421afef2fe48558e9290e42db90f749eb89335109e130b8d679314c6

#nullable disable

using System.Runtime.CompilerServices;
using System.Windows.Forms;
using static Interop;

namespace WinFormsLegacyControls.Menus.Migration
{
    internal sealed class MainMenuSupportFormNativeWindow : NativeWindow
        , ISupportNativeWindow<Form, MainMenu, MainMenuSupportFormNativeWindow>
    {
        private readonly Form _form;
        private MainMenu? _mainMenu;
        private MainMenu? _dummyMenu;
        private MainMenu? _curMenu;
        private MainMenu? _mergedMenu;

        private MainMenuSupportFormNativeWindow(Form form)
        {
            _form = form;
            _form.Disposed += Form_Disposed;
            _form.HandleCreated += Form_HandleCreated;
            //_form.HandleDestroyed += Form_HandleDestroyed;
            _form.VisibleChanged += Form_VisibleChanged;
            _form.MdiChildActivate += Form_MdiChildActivate;
            _form.ControlRemoved += Form_ControlRemoved;
            if (_form.IsHandleCreated)
                AssignHandle(_form.Handle);
        }

        public static MainMenuSupportFormNativeWindow Create(Form form)
            => new(form);

        public void Detach()
        {
            _form.Disposed -= Form_Disposed;
            _form.HandleCreated -= Form_HandleCreated;
            //_form.HandleDestroyed -= Form_HandleDestroyed;
            _form.VisibleChanged -= Form_VisibleChanged;
            _form.MdiChildActivate -= Form_MdiChildActivate;
            _form.ControlRemoved -= Form_ControlRemoved;
            ReleaseHandle();
        }

        MainMenu ISupportNativeWindow<Form, MainMenu, MainMenuSupportFormNativeWindow>.Property
        {
            get => Menu;
            set => Menu = value;
        }

        private void Form_HandleCreated(object sender, EventArgs e)
        {
            AssignHandle(_form.Handle);
            OnHandleCreated();
        }

        /*
        private void Form_HandleDestroyed(object sender, EventArgs e)
        {
            // WM_NCDESTROY のハンドリングが必要。かつ NativeWindow 内の WM_NCDESTROY で ReleaseHandle される。
            //ReleaseHandle();
        }
        */

        private void Form_ControlRemoved(object sender, ControlEventArgs e)
        {
            if (e.Control is MdiClient mdiClient)
                AfterControlRemoved(mdiClient);
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ActiveMdiChildInternal")]
        private static extern Form? GetActiveMdiChildInternal(Form form);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MdiClient")]
        private static extern MdiClient? GetMdiClient(Form form);

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
                            Debug.Assert(value.form != _form);
                            value.form.SetMenu(null);
                        }
                        value.form = _form;
                    }

                    if (/*formState[FormStateSetClientSize] == 1 &&*/ !_form.IsHandleCreated)
                    {
                        _form.ClientSize = _form.ClientSize;
                    }

                    MenuChanged(MenuChangeKind.CHANGE_ITEMS, value);
                }
            }
        }

        // TODO:
        //private Form MdiParentInternal
        //  InvalidateMergedMenu();
        //  UpdateMenuHandles();

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
        private void Form_VisibleChanged(object sender, EventArgs e)
        {
            if (_form.IsMdiChild && !_form.Visible)
                InvalidateMergedMenu();
        }

        private bool _mdiClientRemoving;

        //internal override void AfterControlRemoved(Control control, Control oldParent)
        private void AfterControlRemoved(Control control)
        {
            MdiClient? ctlClient = GetMdiClient(_form);

            if (control == ctlClient)
            {
                //ctlClient = null;
                _mdiClientRemoving = true;
                try
                {
                    UpdateMenuHandles();
                }
                finally
                {
                    _mdiClientRemoving = false;
                }
            }
        }

        //protected override void CreateHandle()
        private void OnHandleCreated()
        {
            // In the windows MDI code we have to suspend menu
            // updates on the parent while creating the handle. Otherwise if the
            // child is created maximized, the menu ends up with two sets of
            // MDI child ornaments.
            //Form form = (Form)Properties.GetObject(PropFormMdiParent);
            Form? formMdiParent = _form.MdiParent;
            MainMenuSupportFormNativeWindow? form = formMdiParent?.GetMainMenuSupportFormNativeWindow();
            if (form != null)
            {
                form.SuspendUpdateMenuHandles();
            }

            try
            {
                // avoid extra SetMenu calls for perf
                if (Menu != null || !_form.TopLevel || _form.IsMdiContainer)
                    UpdateMenuHandles();
            }
            finally
            {
                if (form != null)
                {
                    form.ResumeUpdateMenuHandles();
                }
            }
        }

        // Deactivates active MDI child and temporarily marks it as unfocusable,
        // so that WM_SETFOCUS sent to MDIClient does not activate that child.
        private void DeactivateMdiChild()
        {
            //Form activeMdiChild = ActiveMdiChildInternal;
            Form activeMdiChild = GetActiveMdiChildInternal(_form);
            if (null != activeMdiChild)
            {
                // Note: WM_MDIACTIVATE message is sent to the form being activated and to the form being deactivated, ideally
                // we would raise the event here accordingly but it would constitute a breaking change.
                //OnMdiChildActivate(EventArgs.Empty);

                // undo merge
                UpdateMenuHandles();
            }
        }

        //protected override void Dispose(bool disposing)
        private void Form_Disposed(object sender, EventArgs e)
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

            MenuChanged(MenuChangeKind.CHANGE_ITEMS, null);

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

        // Package scope for menu interop
        internal void MenuChanged(MenuChangeKind change, Menu menu)
        {
            Form parForm = _form.ParentForm;
            if (parForm != null && _form == /*parForm.ActiveMdiChildInternal*/GetActiveMdiChildInternal(parForm))
            {
                parForm.MenuChanged(change, menu);
                return;
            }

            MdiClient? ctlClient;

            switch (change)
            {
                case MenuChangeKind.CHANGE_ITEMS:
                case MenuChangeKind.CHANGE_MERGE:
                    ctlClient = GetMdiClient(_form);
                    if (ctlClient == null || !ctlClient.IsHandleCreated)
                    {
                        if (menu == Menu && change == MenuChangeKind.CHANGE_ITEMS)
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
                case MenuChangeKind.CHANGE_VISIBLE:
                    //if (menu == Menu || (ActiveMdiChildInternal != null && menu == ActiveMdiChildInternal.Menu))
                    if (menu == Menu || (GetActiveMdiChildInternal(_form) is Form activeMdiChild && menu == activeMdiChild.GetMenu()))
                    {
                        UpdateMenuHandles();
                    }
                    break;
                case MenuChangeKind.CHANGE_MDI:
                    ctlClient = GetMdiClient(_form);
                    if (ctlClient != null && ctlClient.IsHandleCreated)
                    {
                        UpdateMenuHandles();
                    }
                    break;
            }
        }

        //protected virtual void OnMdiChildActivate(EventArgs e)
        private void Form_MdiChildActivate(object sender, EventArgs e)
        {
            UpdateMenuHandles();
        }

        //protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        internal bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //MainMenu curMenu = (MainMenu)Properties.GetObject(PropCurMenu);
            MainMenu curMenu = _curMenu;
            if (curMenu != null && curMenu.ProcessCmdKey(ref msg, keyData))
            {
                return true;
            }
            return false;
        }

        private int _updateMenuHandlesSuspendCount;
        private bool _updateMenuHandlesDeferred;

        /// <summary>
        ///  Decrements updateMenuHandleSuspendCount. If updateMenuHandleSuspendCount
        ///  becomes zero and updateMenuHandlesDeferred is true, updateMenuHandles
        ///  is called.
        /// </summary>
        private void ResumeUpdateMenuHandles()
        {
            //int suspendCount = formStateEx[FormStateExUpdateMenuHandlesSuspendCount];
            if (/*suspendCount*/_updateMenuHandlesSuspendCount <= 0)
            {
                throw new InvalidOperationException(SR.TooManyResumeUpdateMenuHandles);
            }

            //formStateEx[FormStateExUpdateMenuHandlesSuspendCount] = --suspendCount;
            _updateMenuHandlesSuspendCount--;
            //if (suspendCount == 0 && formStateEx[FormStateExUpdateMenuHandlesDeferred] != 0)
            if (_updateMenuHandlesSuspendCount == 0 && _updateMenuHandlesDeferred)
            {
                UpdateMenuHandles();
            }
        }

        /// <summary>
        ///  Increments updateMenuHandleSuspendCount.
        /// </summary>
        private void SuspendUpdateMenuHandles()
        {
            /*
            int suspendCount = formStateEx[FormStateExUpdateMenuHandlesSuspendCount];
            formStateEx[FormStateExUpdateMenuHandlesSuspendCount] = ++suspendCount;
            */
            _updateMenuHandlesSuspendCount++;
        }

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
                    //form = ActiveMdiChildInternal;
                    form = GetActiveMdiChildInternal(_form);
                    if (form != null)
                    {
                        //UpdateMenuHandles(form.MergedMenuPrivate, true);
                        MainMenu? mergedMenu;
                        if (form.TryGetMainMenuSupportFormNativeWindow(out var window))
                            mergedMenu = window.MergedMenu;
                        else
                            mergedMenu = form.MdiParent?.GetMenu();
                        UpdateMenuHandles(mergedMenu, true);
                    }
                    else
                    {
                        UpdateMenuHandles(Menu, true);
                    }
                }
            }
        }

        private void UpdateMenuHandles(MainMenu menu, bool forceRedraw)
        {
            Debug.Assert(_form.IsHandleCreated, "shouldn't call when handle == 0");

            //int suspendCount = formStateEx[FormStateExUpdateMenuHandlesSuspendCount];
            int suspendCount = _updateMenuHandlesSuspendCount;
            if (suspendCount > 0 && menu != null)
            {
                //formStateEx[FormStateExUpdateMenuHandlesDeferred] = 1;
                _updateMenuHandlesDeferred = true;
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

            MdiClient? ctlClient = _mdiClientRemoving ? null : GetMdiClient(_form);

            if (ctlClient == null || !ctlClient.IsHandleCreated)
            {
                if (menu != null)
                {
                    //UnsafeNativeMethods.SetMenu(new HandleRef(this, Handle), new HandleRef(menu, menu.Handle));
                    IntPtr createHandle = menu.Handle;
                    BOOL result = PInvoke.SetMenu(_form, menu);
                    Debug.Assert(result);
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
            _updateMenuHandlesDeferred = false;
        }

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
        ///  WM_MDIACTIVATE handler
        /// </summary>
        private void WmMdiActivate(ref Message m)
        {
            base.WndProc(ref m);
            //Debug.Assert(Properties.GetObject(PropFormMdiParent) != null, "how is formMdiParent null?");
            //Debug.Assert(IsHandleCreated, "how is handle 0?");

            //Form formMdiParent = (Form)Properties.GetObject(PropFormMdiParent);
            Form formMdiParent = _form.MdiParent;

            if (formMdiParent != null)
            {
                // This message is propagated twice by the MDIClient window. Once to the
                // window being deactivated and once to the window being activated.
                if (Handle == m.WParam)
                {
                    //formMdiParent.DeactivateMdiChild();
                    formMdiParent.GetMainMenuSupportFormNativeWindow()?.DeactivateMdiChild();
                }
                /*
                else if (Handle == m.LParam)
                {
                    formMdiParent.ActivateMdiChild(this);
                }
                */
            }
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
                case PInvoke.WM_MDIACTIVATE:
                    WmMdiActivate(ref m);
                    break;
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

                // Control WndProc for MainMenu
                case PInvoke.WM_COMMAND:
                    if (!CommonMessageHandlers.WmCommand(ref m))
                        base.WndProc(ref m);
                    break;
                case PInvoke.WM_DRAWITEM:
                    if (!CommonMessageHandlers.WmDrawItem(ref m))
                        base.WndProc(ref m);
                    break;
                case PInvoke.WM_MEASUREITEM:
                    if (!CommonMessageHandlers.WmMeasureItem(ref m))
                        base.WndProc(ref m);
                    break;
                case PInvoke.WM_MENUSELECT:
                    CommonMessageHandlers.WmMenuSelect(ref m);
                    base.WndProc(ref m);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }
}
