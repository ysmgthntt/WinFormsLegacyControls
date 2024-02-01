// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    /// <summary>
    ///  Represents an individual item that is displayed within a <see cref='Menu'/>
    ///  or <see cref='Menu'/>.
    /// </summary>
    [ToolboxItem(false)]
    [DesignTimeVisible(false)]
    [DefaultEvent(nameof(Click))]
    [DefaultProperty(nameof(Text))]
    public partial class MenuItem : Menu
    {
        private const int StateBarBreak = 0x00000020;
        private const int StateBreak = 0x00000040;
        private const int StateChecked = 0x00000008;
        private const int StateDefault = 0x00001000;
        private const int StateDisabled = 0x00000003;
        private const int StateRadioCheck = 0x00000200;
        private const int StateHidden = 0x00010000;
        private const int StateMdiList = 0x00020000;
        private const int StateCloneMask = 0x0003136B;
        private const int StateOwnerDraw = 0x00000100;
        private const int StateInMdiPopup = 0x00000200;
        //private const int StateHiLite = 0x00000080;

        private bool _hasHandle;
        private MenuItemData _data = null!;
        private int _dataVersion;
        private MenuItem? _nextLinkedItem; // Next item linked to the same MenuItemData.

        // We need to store a table of all created menuitems, so that other objects
        // such as ContainerControl can get a reference to a particular menuitem,
        // given a unique ID.
        private static readonly Dictionary<uint, WeakReference<MenuItem>> s_allCreatedMenuItems = new();
        private const uint FirstUniqueID = 0xC0000000;
        private static long s_nextUniqueID = FirstUniqueID;
        private uint _uniqueID;
        private IntPtr _msaaMenuInfoPtr = IntPtr.Zero;
        private bool _menuItemIsCreated;

#if DEBUG
        private string? _debugText;
        private readonly int _creationNumber;
        private static int s_createCount;
#endif

        /// <summary>
        ///  Initializes a <see cref='MenuItem'/> with a blank caption.
        /// </summary>
        public MenuItem() : this(MenuMerge.Add, 0, 0, null, null, null, null, null)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref='MenuItem'/> class
        ///  with a specified caption for the menu item.
        /// </summary>
        public MenuItem(string text) : this(MenuMerge.Add, 0, 0, text, null, null, null, null)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the class with a specified caption and event handler
        ///  for the menu item.
        /// </summary>
        public MenuItem(string text, EventHandler onClick) : this(MenuMerge.Add, 0, 0, text, onClick, null, null, null)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the class with a specified caption, event handler,
        ///  and associated shorcut key for the menu item.
        /// </summary>
        public MenuItem(string text, EventHandler onClick, Shortcut shortcut) : this(MenuMerge.Add, 0, shortcut, text, onClick, null, null, null)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the class with a specified caption and an array of
        ///  submenu items defined for the menu item.
        /// </summary>
        public MenuItem(string text, MenuItem[] items) : this(MenuMerge.Add, 0, 0, text, null, null, null, items)
        {
        }

        /*
        internal MenuItem(MenuItemData data) : base(null)
        {
            data.AddItem(this);

#if DEBUG
            _debugText = data._caption;
#endif
        }
        */

        /// <summary>
        ///  Initializes a new instance of the class with a specified caption, defined
        ///  event-handlers for the Click, Select and Popup events, a shortcut key,
        ///  a merge type, and order specified for the menu item.
        /// </summary>
        public MenuItem(MenuMerge mergeType, int mergeOrder, Shortcut shortcut,
                        string? text, EventHandler? onClick, EventHandler? onPopup,
                        EventHandler? onSelect, MenuItem[]? items) : base(items)
        {
            new MenuItemData(this, mergeType, mergeOrder, shortcut, true,
                             text, onClick, onPopup, onSelect, null, null);

#if DEBUG
            _debugText = text;
            _creationNumber = s_createCount++;
#endif
        }

        /// <summary>
        ///  Gets or sets a value indicating whether the item is placed on a new line (for a
        ///  menu item added to a <see cref='MainMenu'/> object) or in a
        ///  new column (for a submenu or menu displayed in a <see cref='ContextMenu'/>).
        /// </summary>
        [Browsable(false)]
        [DefaultValue(false)]
        public bool BarBreak
        {
            get
            {
                CheckIfDisposed();
                return (_data.State & StateBarBreak) != 0;
            }
            set
            {
                CheckIfDisposed();
                _data.SetState(StateBarBreak, value);
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether the item is placed on a new line (for a
        ///  menu item added to a <see cref='MainMenu'/> object) or in a
        ///  new column (for a submenu or menu displayed in a <see cref='ContextMenu'/>).
        /// </summary>
        [Browsable(false)]
        [DefaultValue(false)]
        public bool Break
        {
            get
            {
                CheckIfDisposed();
                return (_data.State & StateBreak) != 0;
            }
            set
            {
                CheckIfDisposed();
                _data.SetState(StateBreak, value);
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether a checkmark appears beside the text of
        ///  the menu item.
        /// </summary>
        [DefaultValue(false)]
        [SRDescription(nameof(SR.MenuItemCheckedDescr))]
        public bool Checked
        {
            get
            {
                CheckIfDisposed();
                return (_data.State & StateChecked) != 0;
            }
            set
            {
                CheckIfDisposed();

                if (value && (ItemCount != 0 || Parent is MainMenu))
                {
                    throw new ArgumentException(SR.MenuItemInvalidCheckProperty, nameof(value));
                }

                _data.SetState(StateChecked, value);
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether the menu item is the default.
        /// </summary>
        [DefaultValue(false)]
        [SRDescription(nameof(SR.MenuItemDefaultDescr))]
        public bool DefaultItem
        {
            get
            {
                CheckIfDisposed();
                return (_data.State & StateDefault) != 0;
            }
            set
            {
                CheckIfDisposed();
                if (Parent is not null)
                {
                    if (value)
                    {
                        PInvoke.SetMenuDefaultItem(Parent, (uint)MenuID, 0);
                    }
                    else if (DefaultItem)
                    {
                        PInvoke.SetMenuDefaultItem(Parent, unchecked((uint)-1), 0);
                    }
                }

                _data.SetState(StateDefault, value);
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether code that you provide draws the menu
        ///  item or Windows draws the menu item.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [DefaultValue(false)]
        [SRDescription(nameof(SR.MenuItemOwnerDrawDescr))]
        public bool OwnerDraw
        {
            get
            {
                CheckIfDisposed();
                return ((_data.State & StateOwnerDraw) != 0);
            }
            set
            {
                CheckIfDisposed();
                _data.SetState(StateOwnerDraw, value);
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether the menu item is enabled.
        /// </summary>
        [Localizable(true)]
        [DefaultValue(true)]
        [SRDescription(nameof(SR.MenuItemEnabledDescr))]
        public bool Enabled
        {
            get
            {
                CheckIfDisposed();
                return (_data.State & StateDisabled) == 0;
            }
            set
            {
                CheckIfDisposed();
                _data.SetState(StateDisabled, !value);
            }
        }

        /// <summary>
        ///  Gets or sets the menu item's position in its parent menu.
        /// </summary>
        [Browsable(false)]
        public int Index
        {
            get
            {
                if (Parent is not null)
                {
                    for (int i = 0; i < Parent.ItemCount; i++)
                    {
                        if (Parent._items[i] == this)
                        {
                            return i;
                        }
                    }
                }

                return -1;
            }
            set
            {
                int oldIndex = Index;
                if (oldIndex >= 0)
                {
                    if (value < 0 || value >= Parent!.ItemCount)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), string.Format(SR.InvalidArgument, nameof(Index), value));
                    }

                    if (value != oldIndex)
                    {
                        // The menu reverts to null when we're removed, so hold onto it in a
                        // local variable
                        Menu parent = Parent;
                        parent.MenuItems.RemoveAt(oldIndex);
                        parent.MenuItems.Add(value, this);
                    }
                }
            }
        }

        /// <summary>
        ///  Gets a value indicating whether the menu item contains child menu items.
        /// </summary>
        [Browsable(false)]
        public override bool IsParent
        {
            get
            {
                if (_data is not null && MdiList)
                {
                    for (int i = 0; i < ItemCount; i++)
                    {
                        if (_items[i]._data.UserData is not MdiListUserData)
                        {
                            return true;
                        }
                    }

                    if (FindMdiForms(out _).Length > 0)
                    {
                        return true;
                    }

                    if (Parent is not null && Parent is not MenuItem)
                    {
                        return true;
                    }

                    return false;
                }

                return base.IsParent;
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether the menu item will be populated with a
        ///  list of the MDI child windows that are displayed within the associated form.
        /// </summary>
        [DefaultValue(false)]
        [SRDescription(nameof(SR.MenuItemMDIListDescr))]
        public bool MdiList
        {
            get
            {
                CheckIfDisposed();
                return (_data.State & StateMdiList) != 0;
            }
            set
            {
                CheckIfDisposed();
                _data.MdiList = value;
                CleanListItems(this);
            }
        }

        /// <summary>
        ///  Gets the Windows identifier for this menu item.
        /// </summary>
        protected int MenuID
        {
            get
            {
                CheckIfDisposed();
                return _data.GetMenuID();
            }
        }

        /// <summary>
        ///  Is this menu item currently selected (highlighted) by the user?
        /// </summary>
        internal bool Selected
        {
            get
            {
                if (Parent is null)
                {
                    return false;
                }

                MENUITEMINFOW info = default;
                unsafe
                {
                    info.cbSize = (uint)sizeof(MENUITEMINFOW);
                }
                info.fMask = MENU_ITEM_MASK.MIIM_STATE;
                PInvoke.GetMenuItemInfo(Parent, (uint)MenuID, false, ref info);

                return (info.fState & MENU_ITEM_STATE.MFS_HILITE) != 0;
            }
        }

        /// <summary>
        ///  Gets the zero-based index of this menu item in the parent menu, or -1 if this
        ///  menu item is not associated with a parent menu.
        /// </summary>
        internal int MenuIndex
        {
            get
            {
                if (Parent is null)
                {
                    return -1;
                }

                _ = Parent.Handle;  // CreateHandle
                int count = PInvoke.GetMenuItemCount(Parent);
                int id = MenuID;
                MENUITEMINFOW info = default;
                unsafe
                {
                    info.cbSize = (uint)sizeof(MENUITEMINFOW);
                }
                info.fMask = MENU_ITEM_MASK.MIIM_ID | MENU_ITEM_MASK.MIIM_SUBMENU;

                for (int i = 0; i < count; i++)
                {
                    PInvoke.GetMenuItemInfo(Parent, (uint)i, true, ref info);

                    // For sub menus, the handle is always valid.
                    // For items, however, it is always zero.
                    if ((info.hSubMenu == IntPtr.Zero || info.hSubMenu == Handle) && info.wID == id)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        ///  Gets or sets a value that indicates the behavior of this
        ///  menu item when its menu is merged with another.
        /// </summary>
        [DefaultValue(MenuMerge.Add)]
        [SRDescription(nameof(SR.MenuItemMergeTypeDescr))]
        public MenuMerge MergeType
        {
            get
            {
                CheckIfDisposed();
                return _data._mergeType;
            }
            set
            {
                CheckIfDisposed();
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)MenuMerge.Add, (int)MenuMerge.Remove))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(MenuMerge));
                }

                _data.MergeType = value;
            }
        }

        /// <summary>
        ///  Gets or sets the relative position the menu item when its
        ///  menu is merged with another.
        /// </summary>
        [DefaultValue(0)]
        [SRDescription(nameof(SR.MenuItemMergeOrderDescr))]
        public int MergeOrder
        {
            get
            {
                CheckIfDisposed();
                return _data._mergeOrder;
            }
            set
            {
                CheckIfDisposed();
                _data.MergeOrder = value;
            }
        }

        /// <summary>
        ///  Retrieves the hotkey mnemonic that is associated with this menu item.
        ///  The mnemonic is the first character after an ampersand symbol in the menu's text
        ///  that is not itself an ampersand symbol. If no such mnemonic is defined this
        ///  will return zero.
        /// </summary>
        [Browsable(false)]
        public char Mnemonic
        {
            get
            {
                CheckIfDisposed();
                return _data.Mnemonic;
            }
        }

        /// <summary>
        ///  Gets the menu in which this menu item appears.
        /// </summary>
        [Browsable(false)]
        public Menu? Parent { get; internal set; }

        /// <summary>
        ///  Gets or sets a value that indicates whether the menu item, if checked,
        ///  displays a radio-button mark instead of a check mark.
        /// </summary>
        [DefaultValue(false)]
        [SRDescription(nameof(SR.MenuItemRadioCheckDescr))]
        public bool RadioCheck
        {
            get
            {
                CheckIfDisposed();
                return (_data.State & StateRadioCheck) != 0;
            }
            set
            {
                CheckIfDisposed();
                _data.SetState(StateRadioCheck, value);
            }
        }

        internal override bool RenderIsRightToLeft => Parent is not null && Parent.RenderIsRightToLeft;

        /// <summary>
        ///  Gets or sets the text of the menu item.
        /// </summary>
        [Localizable(true)]
        [SRDescription(nameof(SR.MenuItemTextDescr))]
        public string Text
        {
            get
            {
                CheckIfDisposed();
                return _data._caption;
            }
            set
            {
                CheckIfDisposed();
                _data.SetCaption(value);
            }
        }

        /// <summary>
        ///  Gets or sets the shortcut key associated with the menu item.
        /// </summary>
        [Localizable(true)]
        [DefaultValue(Shortcut.None)]
        [SRDescription(nameof(SR.MenuItemShortCutDescr))]
        public Shortcut Shortcut
        {
            get
            {
                CheckIfDisposed();
                return _data._shortcut;
            }
            set
            {
                CheckIfDisposed();
                if (!Enum.IsDefined(typeof(Shortcut), value))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(Shortcut));
                }

                _data._shortcut = value;
                UpdateMenuItem(force: true);
            }
        }

        /// <summary>
        ///  Gets or sets a value that indicates whether the shortcut key that is associated
        ///  with the menu item is displayed next to the menu item caption.
        /// </summary>
        [DefaultValue(true),
        Localizable(true)]
        [SRDescription(nameof(SR.MenuItemShowShortCutDescr))]
        public bool ShowShortcut
        {
            get
            {
                CheckIfDisposed();
                return _data._showShortcut;
            }
            set
            {
                CheckIfDisposed();
                if (value != _data._showShortcut)
                {
                    _data._showShortcut = value;
                    UpdateMenuItem(force: true);
                }
            }
        }

        /// <summary>
        ///  Gets or sets a value that indicates whether the menu item is visible on its
        ///  parent menu.
        /// </summary>
        [Localizable(true)]
        [DefaultValue(true)]
        [SRDescription(nameof(SR.MenuItemVisibleDescr))]
        public bool Visible
        {
            get
            {
                CheckIfDisposed();
                return (_data.State & StateHidden) == 0;
            }
            set
            {
                CheckIfDisposed();
                _data.Visible = value;
            }
        }

        /// <summary>
        ///  Occurs when the menu item is clicked or selected using a shortcut key defined
        ///  for the menu item.
        /// </summary>
        [SRDescription(nameof(SR.MenuItemOnClickDescr))]
        public event EventHandler Click
        {
            add
            {
                CheckIfDisposed();
                _data._onClick += value;
            }
            remove
            {
                CheckIfDisposed();
                _data._onClick -= value;
            }
        }

        /// <summary>
        ///  Occurs when when the property of a menu item is set to <see langword='true'/> and
        ///  a request is made to draw the menu item.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior)), SRDescription(nameof(SR.drawItemEventDescr))]
        public event DrawItemEventHandler DrawItem
        {
            add
            {
                CheckIfDisposed();
                _data._onDrawItem += value;
            }
            remove
            {
                CheckIfDisposed();
                _data._onDrawItem -= value;
            }
        }

        /// <summary>
        ///  Occurs when when the menu needs to know the size of a menu item before drawing it.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior)), SRDescription(nameof(SR.measureItemEventDescr))]
        public event MeasureItemEventHandler MeasureItem
        {
            add
            {
                CheckIfDisposed();
                _data._onMeasureItem += value;
            }
            remove
            {
                CheckIfDisposed();
                _data._onMeasureItem -= value;
            }
        }

        /// <summary>
        ///  Occurs before a menu item's list of menu items is displayed.
        /// </summary>
        [SRDescription(nameof(SR.MenuItemOnInitDescr))]
        public event EventHandler Popup
        {
            add
            {
                CheckIfDisposed();
                _data._onPopup += value;
            }
            remove
            {
                CheckIfDisposed();
                _data._onPopup -= value;
            }
        }

        /// <summary>
        ///  Occurs when the user hovers their mouse over a menu item or selects it with the
        ///  keyboard but has not activated it.
        /// </summary>
        [SRDescription(nameof(SR.MenuItemOnSelectDescr))]
        public event EventHandler Select
        {
            add
            {
                CheckIfDisposed();
                _data._onSelect += value;
            }
            remove
            {
                CheckIfDisposed();
                _data._onSelect -= value;
            }
        }

        private static void CleanListItems(MenuItem senderMenu)
        {
            // Remove dynamic items.
            for (int i = senderMenu.MenuItems.Count - 1; i >= 0; i--)
            {
                MenuItem item = senderMenu.MenuItems[i];
                if (item._data.UserData is MdiListUserData)
                {
                    item.Dispose();
                    continue;
                }
            }
        }

        /// <summary>
        ///  Creates and returns an identical copy of this menu item.
        /// </summary>
        public virtual MenuItem CloneMenu()
        {
            var newItem = new MenuItem();
            newItem.CloneMenu(this);
            return newItem;
        }

        /// <summary>
        ///  Creates a copy of the specified menu item.
        /// </summary>
        protected void CloneMenu(MenuItem itemSrc)
        {
            base.CloneMenu(itemSrc);
            int state = itemSrc._data.State;
            new MenuItemData(this,
                             itemSrc.MergeType, itemSrc.MergeOrder, itemSrc.Shortcut, itemSrc.ShowShortcut,
                             itemSrc.Text, itemSrc._data._onClick, itemSrc._data._onPopup, itemSrc._data._onSelect,
                             itemSrc._data._onDrawItem, itemSrc._data._onMeasureItem);
            _data.SetState(state & StateCloneMask, true);
        }

        internal void CreateMenuItem()
        {
            if ((_data.State & StateHidden) == 0)
            {
                bool setRightToLeftBit = RenderIsRightToLeft;
                MENUITEMINFOW info = CreateMenuItemInfo(setRightToLeftBit, out string dwTypeData);
                unsafe
                {
                    fixed (char* ptr = dwTypeData)
                    {
                        info.dwTypeData = ptr;
                        PInvoke.InsertMenuItem(Parent!, unchecked((uint)-1), true, ref info);
                        if (setRightToLeftBit)
                        {
                            // InsertMenuItem だけだと右からにならない。
                            PInvoke.SetMenuItemInfo(Parent!, info.wID, false, ref info);
                        }
                    }
                }

                _hasHandle = info.hSubMenu != IntPtr.Zero;
                _dataVersion = _data._version;

                _menuItemIsCreated = true;
                /*
                if (RenderIsRightToLeft)
                {
                    Parent!.UpdateRtl(true);
                }
                */

#if DEBUG
                MENUITEMINFOW infoVerify = default;
                unsafe
                {
                    infoVerify.cbSize = (uint)sizeof(MENUITEMINFOW);
                }
                infoVerify.fMask = MENU_ITEM_MASK.MIIM_ID | MENU_ITEM_MASK.MIIM_STATE |
                                   MENU_ITEM_MASK.MIIM_SUBMENU | MENU_ITEM_MASK.MIIM_TYPE;
                PInvoke.GetMenuItemInfo(Parent!, (uint)MenuID, false, ref infoVerify);
#endif
            }
        }

        private MENUITEMINFOW CreateMenuItemInfo(bool setRightToLeftBit, out string dwTypeData)
        {
            MENUITEMINFOW info = default;
            unsafe
            {
                info.cbSize = (uint)sizeof(MENUITEMINFOW);
            }
            info.fMask = MENU_ITEM_MASK.MIIM_ID | MENU_ITEM_MASK.MIIM_STATE |
                         MENU_ITEM_MASK.MIIM_SUBMENU | MENU_ITEM_MASK.MIIM_TYPE | MENU_ITEM_MASK.MIIM_DATA;
            info.fType = (MENU_ITEM_TYPE)(_data.State & (StateBarBreak | StateBreak | StateRadioCheck | StateOwnerDraw));

            // Top level menu items shouldn't have barbreak or break bits set on them.
            bool isTopLevel = Parent == GetMainMenu();

            if (_data._caption.Equals("-"))
            {
                if (isTopLevel)
                {
                    _data._caption = " ";
                    info.fType |= MENU_ITEM_TYPE.MFT_MENUBREAK;
                }
                else
                {
                    info.fType |= MENU_ITEM_TYPE.MFT_SEPARATOR;
                }
            }

            // [fixed]
            if (setRightToLeftBit)
            {
                info.fType |= MENU_ITEM_TYPE.MFT_RIGHTJUSTIFY | MENU_ITEM_TYPE.MFT_RIGHTORDER;
            }

            info.fState = (MENU_ITEM_STATE)(_data.State & (StateChecked | StateDefault | StateDisabled));

            info.wID = (uint)MenuID;
            if (IsParent)
            {
                info.hSubMenu = (HMENU)Handle;
            }

            info.hbmpChecked = HBITMAP.Null;
            info.hbmpUnchecked = HBITMAP.Null;

            // Assign a unique ID to this menu item object.
            // The ID is stored in the dwItemData of the corresponding Win32 menu item, so
            // that when we get Win32 messages about the item later, we can delegate to the
            // original object menu item object. A static hash table is used to map IDs to
            // menu item objects.
            if (_uniqueID == 0)
            {
                //lock (s_allCreatedMenuItems)
                {
                    _uniqueID = (uint)Interlocked.Increment(ref s_nextUniqueID);
                    Debug.Assert(_uniqueID >= FirstUniqueID); // ...check for ID range exhaustion (unlikely!)
                    // We add a weak ref wrapping a MenuItem to the static hash table, as
                    // supposed to adding the item ref itself, to allow the item to be finalized
                    // in case it is not disposed and no longer referenced anywhere else, hence
                    // preventing leaks.
                    lock (s_allCreatedMenuItems)
                        s_allCreatedMenuItems.Add(_uniqueID, new WeakReference<MenuItem>(this));
                }
            }

            if (IntPtr.Size == 4)
            {
                // Store the unique ID in the dwItemData..
                // For simple menu items, we can just put the unique ID in the dwItemData.
                // But for owner-draw items, we need to point the dwItemData at an MSAAMENUINFO
                // structure so that MSAA can get the item text.
                // To allow us to reliably distinguish between IDs and structure pointers later
                // on, we keep IDs in the 0xC0000000-0xFFFFFFFF range. This is the top 1Gb of
                // unmananged process memory, where an app's heap allocations should never come
                // from. So that we can still get the ID from the dwItemData for an owner-draw
                // item later on, a copy of the ID is tacked onto the end of the MSAAMENUINFO
                // structure.
                if (_data.OwnerDraw)
                {
                    info.dwItemData = (nuint)AllocMsaaMenuInfo();
                }
                else
                {
                    info.dwItemData = (nuint)unchecked((int)_uniqueID);
                }
            }
            else
            {
                // On Win64, there are no reserved address ranges we can use for menu item IDs. So instead we will
                // have to allocate an MSAMENUINFO heap structure for all menu items, not just owner-drawn ones.
                info.dwItemData = (nuint)AllocMsaaMenuInfo();
            }

            // We won't render the shortcut if: 1) it's not set, 2) we're a parent, 3) we're toplevel
            if (_data._showShortcut && _data._shortcut != 0 && !IsParent && !isTopLevel)
            {
                dwTypeData = _data._caption + "\t" + ShortcutToText((Keys)(int)_data._shortcut);
            }
            else
            {
                // Windows issue: Items with empty captions sometimes block keyboard
                // access to other items in same menu.
                dwTypeData = (_data._caption.Length == 0 ? " " : _data._caption);
            }
            info.cch = 0;

            return info;
        }

        private static KeysConverter? s_keysConverter;

        private static string? ShortcutToText(Keys key)
            => (s_keysConverter ??= new KeysConverter()).ConvertToString(null, CultureInfo.CurrentUICulture, key);

        /// <summary>
        ///  Disposes the <see cref='MenuItem'/>.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Parent?.MenuItems.Remove(this);
                _data?.RemoveItem(this);
                lock (s_allCreatedMenuItems)
                {
                    s_allCreatedMenuItems.Remove(_uniqueID);
                }

                _uniqueID = 0;

            }

            FreeMsaaMenuInfo();
            base.Dispose(disposing);
        }

        /// <summary>
        ///  Given a unique menu item ID, find the corresponding MenuItem
        ///  object, using the master lookup table of all created MenuItems.
        /// </summary>
        private static MenuItem GetMenuItemFromUniqueID(uint uniqueID)
        {
            if (s_allCreatedMenuItems.TryGetValue(uniqueID, out var weakRef) && weakRef.TryGetTarget(out var menuItem))
                return menuItem;
            Debug.Fail("Weakref for menu item has expired or has been removed!  Who is trying to access this ID?");
            return null;
        }

        /// <summary>
        ///  Given the "item data" value of a Win32 menu item, find the corresponding MenuItem object (using
        ///  the master lookup table of all created MenuItems). The item data may be either the unique menu
        ///  item ID, or a pointer to an MSAAMENUINFO structure with a copy of the unique ID tacked to the end.
        ///  To reliably tell IDs and structure addresses apart, IDs live in the 0xC0000000-0xFFFFFFFF range.
        ///  This is the top 1Gb of unmananged process memory, where an app's heap allocations should never be.
        /// </summary>
        internal static MenuItem? GetMenuItemFromItemData(IntPtr itemData)
        {
            uint uniqueID = (uint)(ulong)itemData;
            if (uniqueID == 0)
            {
                return null;
            }

            if (IntPtr.Size == 4)
            {
                if (uniqueID < FirstUniqueID)
                {
                    unsafe
                    {
                        MsaaMenuInfoWithId* msaaMenuInfo = (MsaaMenuInfoWithId*)itemData;
                        uniqueID = msaaMenuInfo->_uniqueID;
                    }
                }
            }
            else
            {
                // Its always a pointer on Win64 (see CreateMenuItemInfo)
                unsafe
                {
                    MsaaMenuInfoWithId* msaaMenuInfo = (MsaaMenuInfoWithId*)itemData;
                    uniqueID = msaaMenuInfo->_uniqueID;
                }
            }

            return GetMenuItemFromUniqueID(uniqueID);
        }

        /// <summary>
        ///  MsaaMenuInfoWithId is an MSAAMENUINFO structure with a menu item ID field tacked onto the
        ///  end. This allows us to pass the data we need to Win32 / MSAA, and still be able to get the ID
        ///  out again later on, so we can delegate Win32 menu messages back to the correct MenuItem object.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MsaaMenuInfoWithId
        {
            public MSAAMENUINFO _msaaMenuInfo;
            public uint _uniqueID;
        }

#if DEBUG
        static unsafe MenuItem()
        {
            Debug.Assert(sizeof(MsaaMenuInfoWithId) == Marshal.SizeOf<MsaaMenuInfoWithId>());
        }
#endif

        private char[]? _buffer;

        /// <summary>
        ///  Creates an MSAAMENUINFO structure (in the unmanaged heap) based on the current state
        ///  of this MenuItem object. Address of this structure is cached in the object so we can
        ///  free it later on using FreeMsaaMenuInfo(). If structure has already been allocated,
        ///  it is destroyed and a new one created.
        /// </summary>
        private IntPtr AllocMsaaMenuInfo()
        {
            FreeMsaaMenuInfo();
            unsafe
            {
                _msaaMenuInfoPtr = Marshal.AllocHGlobal(sizeof(MsaaMenuInfoWithId));
            }

            if (IntPtr.Size == 4)
            {
                // We only check this on Win32, irrelevant on Win64 (see CreateMenuItemInfo)
                // Check for incursion into menu item ID range (unlikely!)
                Debug.Assert(((uint)(ulong)_msaaMenuInfoPtr) < FirstUniqueID);
            }

            int length = _data._caption.Length;
            _buffer = GC.AllocateUninitializedArray<char>(length + 1, pinned: true);
            _data._caption.CopyTo(_buffer);
            _buffer[length] = '\0';
            unsafe
            {
                MsaaMenuInfoWithId* msaaMenuInfoStruct = (MsaaMenuInfoWithId*)_msaaMenuInfoPtr;
                msaaMenuInfoStruct->_msaaMenuInfo.dwMSAASignature = unchecked((uint)PInvoke.MSAA_MENU_SIG);
                msaaMenuInfoStruct->_msaaMenuInfo.cchWText = (uint)length;
                fixed (char* pszWText = _buffer)
                {
                    msaaMenuInfoStruct->_msaaMenuInfo.pszWText = pszWText;
                }
                msaaMenuInfoStruct->_uniqueID = _uniqueID;
            }
            Debug.Assert(_msaaMenuInfoPtr != IntPtr.Zero);
            return _msaaMenuInfoPtr;
        }

        /// <summary>
        ///  Frees the MSAAMENUINFO structure (in the unmanaged heap) for the current MenuObject
        ///  object, if one has previously been allocated. Takes care to free sub-structures too,
        ///  to avoid leaks!
        /// <summary>
        private void FreeMsaaMenuInfo()
        {
            if (_msaaMenuInfoPtr != IntPtr.Zero)
            {
                // 参照型なし
                //Marshal.DestroyStructure(_msaaMenuInfoPtr, typeof(MsaaMenuInfoWithId));
                Marshal.FreeHGlobal(_msaaMenuInfoPtr);
                _msaaMenuInfoPtr = IntPtr.Zero;
            }
        }

        internal override void ItemsChanged(MenuChangeKind change)
        {
            base.ItemsChanged(change);

            if (change == MenuChangeKind.CHANGE_ITEMS)
            {
                // when the menu collection changes deal with it locally
                Debug.Assert(!_created, "base.ItemsChanged should have wiped out our handles");
                if (Parent is not null && Parent._created)
                {
                    UpdateMenuItem(force: true);
                    CreateMenuItems();
                }
            }
            else
            {
                if (!_hasHandle && IsParent)
                {
                    UpdateMenuItem(force: true);
                }

                MainMenu? main = GetMainMenu();
                if (main is not null && ((_data.State & StateInMdiPopup) == 0))
                {
                    main.ItemsChanged(change, this);
                }
            }
        }

        internal void ItemsChanged(MenuChangeKind change, MenuItem item)
        {
            if (change == MenuChangeKind.CHANGE_ITEMADDED &&
                _data is not null &&
                _data._baseItem is not null &&
                _data._baseItem.MenuItems.Contains(item))
            {
                if (Parent is not null && Parent._created)
                {
                    UpdateMenuItem(force: true);
                    CreateMenuItems();
                }
                else if (_data is not null)
                {
                    MenuItem? currentMenuItem = _data._firstItem;
                    while (currentMenuItem is not null)
                    {
                        if (currentMenuItem._created)
                        {
                            MenuItem newItem = item.CloneMenu();
                            item._data.AddItem(newItem);
                            currentMenuItem.MenuItems.Add(newItem);
                            break;
                        }
                        currentMenuItem = currentMenuItem._nextLinkedItem;
                    }
                }
            }
        }

        private Form[] FindMdiForms(out Form? activeMdiChild)
        {
            Form[]? forms = null;
            Form? menuForm = null;
            activeMdiChild = null;
            if (GetMainMenu() is { } main)
            {
                menuForm = main./*GetFormUnsafe()*/_form;
            }
            // [spec]
            else if (GetContextMenu()?.SourceControl?.FindForm() is { } form)
            {
                menuForm = form;
            }
            if (menuForm is not null)
            {
                forms = menuForm.MdiChildren;
                activeMdiChild = menuForm.ActiveMdiChild;
            }

            return forms ?? Array.Empty<Form>();
        }

        /// <summary>
        ///  See the similar code in MdiWindowListStrip.PopulateItems, which is
        ///  unfortunately just different enough in its working environment that we
        ///  can't readily combine the two. But if you're fixing something here, chances
        ///  are that the same issue will need scrutiny over there.
        ///</summary>
// "-" is OK
        private void PopulateMdiList()
        {
            MenuItem senderMenu = this;
            _data.SetState(StateInMdiPopup, true);
            try
            {
                CleanListItems(this);

                // Add new items
                Form[] forms = FindMdiForms(out var activeMdiChild);
                if (forms is not null && forms.Length > 0)
                {

                    //Form activeMdiChild = GetMainMenu()./*GetFormUnsafe()*/form.ActiveMdiChild;

                    Type thisType = GetType();

                    if (senderMenu.MenuItems.Count > 0)
                    {
                        MenuItem sep = CreateSameTypeInstance(thisType);
                        sep._data.UserData = new MdiListUserData();
                        sep.Text = "-";
                        senderMenu.MenuItems.Add(sep);
                    }

                    // Build a list of child windows to be displayed in
                    // the MDIList menu item...
                    // Show the first maxMenuForms visible elements of forms[] as Window menu items, except:
                    // Always show the active form, even if it's not in the first maxMenuForms visible elements of forms[].
                    // If the active form isn't in the first maxMenuForms forms, then show the first maxMenuForms-1 elements
                    // in forms[], and make the active form the last one on the menu.
                    // Don't count nonvisible forms against the limit on Window menu items.

                    const int MaxMenuForms = 9; // Max number of Window menu items for forms
                    int visibleChildren = 0;    // the number of visible child forms (so we know to show More Windows...)
                    int accel = 1;              // prefix the form name with this digit, underlined, as an accelerator
                    int formsAddedToMenu = 0;
                    bool activeFormAdded = false;
                    for (int i = 0; i < forms.Length; i++)
                    {
                        if (forms[i].Visible)
                        {
                            visibleChildren++;
                            if ((activeFormAdded && (formsAddedToMenu < MaxMenuForms)) ||  // don't exceed max
                                (!activeFormAdded && (formsAddedToMenu < (MaxMenuForms - 1)) ||  // save room for active if it's not in yet
                                (forms[i].Equals(activeMdiChild))))
                            {
                                // there's always room for activeMdiChild
                                MenuItem windowItem = CreateSameTypeInstance(thisType);
                                windowItem._data.UserData = new MdiListFormData(this, i);

                                if (forms[i].Equals(activeMdiChild))
                                {
                                    windowItem.Checked = true;
                                    activeFormAdded = true;
                                }

                                windowItem.Text = string.Create(CultureInfo.CurrentUICulture, $"&{accel} {forms[i].Text}");
                                accel++;
                                formsAddedToMenu++;
                                senderMenu.MenuItems.Add(windowItem);
                            }
                        }
                    }

                    // Display the More Windows menu option when there are more than 9 MDI
                    // Child menu items to be displayed. This is necessary because we're managing our own
                    // MDI lists, rather than letting Windows do this for us.
                    if (visibleChildren > MaxMenuForms)
                    {
                        MenuItem moreWindows = CreateSameTypeInstance(thisType);
                        moreWindows._data.UserData = new MdiListMoreWindowsData(this);
                        moreWindows.Text = SR.MDIMenuMoreWindows;
                        senderMenu.MenuItems.Add(moreWindows);
                    }
                }
            }
            finally
            {
                _data.SetState(StateInMdiPopup, false);
            }
        }

        private static MenuItem CreateSameTypeInstance(Type thisType)
        {
            if (thisType == typeof(MenuItem))
                return new MenuItem();
            else
                return (MenuItem)Activator.CreateInstance(thisType)!;
        }

        /// <summary>
        ///  Merges this menu item with another menu item and returns the resulting merged
        /// <see cref='MenuItem'/>.
        /// </summary>
        public virtual MenuItem MergeMenu()
        {
            CheckIfDisposed();

            MenuItem newItem = CreateSameTypeInstance(GetType());
            _data.AddItem(newItem);
            newItem.MergeMenu(this);
            return newItem;
        }

        /// <summary>
        ///  Merges another menu item with this menu item.
        /// </summary>
        public void MergeMenu(MenuItem itemSrc)
        {
            base.MergeMenu(itemSrc);
            itemSrc._data.AddItem(this);
        }

        /// <summary>
        ///  Raises the <see cref='Click'/> event.
        /// </summary>
        protected virtual void OnClick(EventArgs e)
        {
            CheckIfDisposed();

            if (_data.UserData is MdiListUserData mdiList)
            {
                mdiList.OnClick(e);
            }
            else if (_data._baseItem != this)
            {
                _data._baseItem.OnClick(e);
            }
            else
            {
                _data._onClick?.Invoke(this, e);
            }
        }

        /// <summary>
        ///  Raises the <see cref='DrawItem'/> event.
        /// </summary>
        protected virtual void OnDrawItem(DrawItemEventArgs e)
        {
            CheckIfDisposed();

            if (_data._baseItem != this)
            {
                _data._baseItem.OnDrawItem(e);
            }
            else
            {
                _data._onDrawItem?.Invoke(this, e);
            }
        }

        /// <summary>
        ///  Raises the <see cref='MeasureItem'/> event.
        /// </summary>
        protected virtual void OnMeasureItem(MeasureItemEventArgs e)
        {
            CheckIfDisposed();

            if (_data._baseItem != this)
            {
                _data._baseItem.OnMeasureItem(e);
            }
            else
            {
                _data._onMeasureItem?.Invoke(this, e);
            }
        }

        /// <summary>
        ///  Raises the <see cref='Popup'/> event.
        /// </summary>
        protected virtual void OnPopup(EventArgs e)
        {
            CheckIfDisposed();

            bool recreate = false;
            for (int i = 0; i < ItemCount; i++)
            {
                if (_items[i].MdiList)
                {
                    recreate = true;
                    _items[i].UpdateMenuItem(force: true);
                }
            }
            if (recreate || (_hasHandle && !IsParent))
            {
                UpdateMenuItem(force: true);
            }

            if (_data._baseItem != this)
            {
                _data._baseItem.OnPopup(e);
            }
            else
            {
                _data._onPopup?.Invoke(this, e);
            }

            // Update any subitem states that got changed in the event
            for (int i = 0; i < ItemCount; i++)
            {
                MenuItem item = _items[i];
                if (item._dataVersion != item._data._version)
                {
                    item.UpdateMenuItem(force: true);
                }
            }

            if (MdiList)
            {
                if (_data._baseItem == this)
                {
                    // If inherited, does not change original behavior
                    if (GetType() == typeof(MenuItem) && GetMainMenu() is { } mainMenu && mainMenu.GetForm() is { } form)
                    {
                        if (form.TryGetMainMenuSupportFormNativeWindow(out var window) && window.CurrentMenu != mainMenu)
                        {
                            return;
                        }
                    }
                }

                PopulateMdiList();
            }
        }

        /// <summary>
        ///  Raises the <see cref='Select'/> event.
        /// </summary>
        protected virtual void OnSelect(EventArgs e)
        {
            CheckIfDisposed();

            if (_data._baseItem != this)
            {
                _data._baseItem.OnSelect(e);
            }
            else
            {
                _data._onSelect?.Invoke(this, e);
            }
        }

        protected internal virtual void OnInitMenuPopup(EventArgs e) => OnPopup(e);

        /// <summary>
        ///  Generates a <see cref='Control.Click'/> event for the MenuItem,
        ///  simulating a click by a user.
        /// </summary>
        public void PerformClick() => OnClick(EventArgs.Empty);

        /// <summary>
        ///  Raises the <see cref='Select'/> event for this menu item.
        /// </summary>
        public virtual void PerformSelect() => OnSelect(EventArgs.Empty);

        internal bool ShortcutClick()
        {
            if (Parent is MenuItem parent)
            {
                if (!parent.ShortcutClick() || Parent != parent)
                {
                    return false;
                }
            }
            if ((_data.State & StateDisabled) != 0)
            {
                return false;
            }
            if (ItemCount > 0)
            {
                OnPopup(EventArgs.Empty);
            }
            else
            {
                OnClick(EventArgs.Empty);
            }

            return true;
        }

        public override string ToString()
            => $"{base.ToString()}, Text: {_data?._caption}";

        internal unsafe void UpdateItemRtl(bool setRightToLeftBit)
        {
            if (!_menuItemIsCreated)
            {
                return;
            }

            MENUITEMINFOW info = default;
            info.cbSize = (uint)sizeof(MENUITEMINFOW);
            info.fMask = MENU_ITEM_MASK.MIIM_TYPE | MENU_ITEM_MASK.MIIM_STATE | MENU_ITEM_MASK.MIIM_SUBMENU;
            // [fixed]
            // Get text length containing shortcut
            PInvoke.GetMenuItemInfo(Parent!, (uint)MenuID, false, ref info);
            info.cch++;
            char* dwTypeData = stackalloc char[(int)info.cch];
            info.dwTypeData = dwTypeData;
            PInvoke.GetMenuItemInfo(Parent!, (uint)MenuID, false, ref info);
            if (setRightToLeftBit)
            {
                info.fType |= MENU_ITEM_TYPE.MFT_RIGHTJUSTIFY | MENU_ITEM_TYPE.MFT_RIGHTORDER;
            }
            else
            {
                info.fType &= ~(MENU_ITEM_TYPE.MFT_RIGHTJUSTIFY | MENU_ITEM_TYPE.MFT_RIGHTORDER);
            }

            PInvoke.SetMenuItemInfo(Parent!, (uint)MenuID, false, ref info);
        }

        internal void UpdateMenuItem(bool force)
        {
            if (Parent is null || !Parent._created)
            {
                return;
            }

            if (force || Parent is MainMenu || Parent is ContextMenu)
            {
                MENUITEMINFOW info = CreateMenuItemInfo(RenderIsRightToLeft, out string dwTypeData);
                unsafe
                {
                    fixed (char* ptr = dwTypeData)
                    {
                        info.dwTypeData = ptr;
                        PInvoke.SetMenuItemInfo(Parent, (uint)MenuID, false, ref info);
                    }
                }
#if DEBUG
                MENUITEMINFOW infoVerify = default;
                unsafe
                {
                    infoVerify.cbSize = (uint)sizeof(MENUITEMINFOW);
                }
                infoVerify.fMask = MENU_ITEM_MASK.MIIM_ID | MENU_ITEM_MASK.MIIM_STATE |
                                   MENU_ITEM_MASK.MIIM_SUBMENU | MENU_ITEM_MASK.MIIM_TYPE;
                PInvoke.GetMenuItemInfo(Parent, (uint)MenuID, false, ref infoVerify);
#endif

                if (_hasHandle && info.hSubMenu == IntPtr.Zero)
                {
                    ClearHandles();
                }

                _hasHandle = info.hSubMenu != IntPtr.Zero;
                _dataVersion = _data._version;
                if (Parent is MainMenu mainMenu)
                {
                    Form? f = mainMenu./*GetFormUnsafe()*/_form;
                    if (f is not null && ((_data.State & StateInMdiPopup) == 0))
                    {
                        PInvoke.DrawMenuBar(f);
                    }
                }
            }
        }

        internal unsafe void WmDrawItem(ref Message m)
        {
            // Handles the OnDrawItem message sent from ContainerControl
            DRAWITEMSTRUCT* dis = (DRAWITEMSTRUCT*)m.LParam;
            //Debug.WriteLineIf(Control.s_paletteTracing.TraceVerbose, Handle + ": Force set palette in MenuItem drawitem");
            using var paletteScope = SelectPaletteScope.HalftonePalette(dis->hDC, false, false);
            using Graphics g = Graphics.FromHdcInternal(dis->hDC);
            OnDrawItem(new DrawItemEventArgs(g, SystemInformation.MenuFont, dis->rcItem, Index, (DrawItemState)dis->itemState));

            m.Result = 1;
        }

        internal void WmMeasureItem(ref Message m)
        {
            // Handles the OnMeasureItem message sent from ContainerControl

            // The OnMeasureItem handler now determines the height and width of the item
            MeasureItemEventArgs mie;
            using (ScreenDC screendc = ScreenDC.Create())
            using (Graphics graphics = Graphics.FromHdcInternal(screendc))
            {
                mie = new MeasureItemEventArgs(graphics, Index);
                OnMeasureItem(mie);
            }

            unsafe
            {
                // Obtain the measure item struct
                MEASUREITEMSTRUCT* mis = (MEASUREITEMSTRUCT*)m.LParam;

                // Update the measure item struct with the new width and height
                mis->itemHeight = (uint)mie.ItemHeight;
                mis->itemWidth = (uint)mie.ItemWidth;
            }

            m.Result = 1;
        }

        private void CheckIfDisposed()
        {
            if (_data is null)
            {
                ObjectDisposedException.ThrowIf(_data is null, this);
            }
        }

        internal sealed class MenuItemData : ICommandExecutor
        {
            internal MenuItem _baseItem = null!;
            internal MenuItem? _firstItem;

            private int _state;
            internal int _version;
            internal MenuMerge _mergeType;
            internal int _mergeOrder;
            internal string _caption;
            private short _mnemonic;
            internal Shortcut _shortcut;
            internal bool _showShortcut;
            internal EventHandler? _onClick;
            internal EventHandler? _onPopup;
            internal EventHandler? _onSelect;
            internal DrawItemEventHandler? _onDrawItem;
            internal MeasureItemEventHandler? _onMeasureItem;

            private Command? _cmd;

            internal MenuItemData(MenuItem baseItem, MenuMerge mergeType, int mergeOrder, Shortcut shortcut, bool showShortcut,
                                  string? caption, EventHandler? onClick, EventHandler? onPopup, EventHandler? onSelect,
                                  DrawItemEventHandler? onDrawItem, MeasureItemEventHandler? onMeasureItem)
            {
                AddItem(baseItem);
                _mergeType = mergeType;
                _mergeOrder = mergeOrder;
                _shortcut = shortcut;
                _showShortcut = showShortcut;
                _caption = caption ?? string.Empty;
                _onClick = onClick;
                _onPopup = onPopup;
                _onSelect = onSelect;
                _onDrawItem = onDrawItem;
                _onMeasureItem = onMeasureItem;
                _version = 1;
                _mnemonic = -1;
            }

            internal bool OwnerDraw
            {
                get => ((State & StateOwnerDraw) != 0);
                set => SetState(StateOwnerDraw, value);
            }

            internal bool MdiList
            {
                get => ((State & StateMdiList) == StateMdiList);
                set
                {
                    if (((_state & StateMdiList) != 0) != value)
                    {
                        SetState(StateMdiList, value);
                        for (MenuItem? item = _firstItem; item is not null; item = item._nextLinkedItem)
                        {
                            item.ItemsChanged(MenuChangeKind.CHANGE_MDI);
                        }
                    }
                }
            }

            internal MenuMerge MergeType
            {
                get => _mergeType;
                set
                {
                    if (_mergeType != value)
                    {
                        _mergeType = value;
                        ItemsChanged(MenuChangeKind.CHANGE_MERGE);
                    }
                }
            }

            internal int MergeOrder
            {
                get => _mergeOrder;
                set
                {
                    if (_mergeOrder != value)
                    {
                        _mergeOrder = value;
                        ItemsChanged(MenuChangeKind.CHANGE_MERGE);
                    }
                }
            }

            internal char Mnemonic
            {
                get
                {
                    if (_mnemonic == -1)
                    {
                        _mnemonic = (short)WindowsFormsUtils.GetMnemonic(_caption, true);
                    }

                    return (char)_mnemonic;
                }
            }

            internal int State => _state;

            internal bool Visible
            {
                get => (_state & StateHidden) == 0;
                set
                {
                    if (((_state & StateHidden) == 0) != value)
                    {
                        _state = value ? _state & ~StateHidden : _state | StateHidden;
                        ItemsChanged(MenuChangeKind.CHANGE_VISIBLE);
                    }
                }
            }

            internal object? UserData { get; set; }

            internal void AddItem(MenuItem item)
            {
                if (item._data != this)
                {
                    item._data?.RemoveItem(item);

                    item._nextLinkedItem = _firstItem;
                    _firstItem = item;
                    _baseItem ??= item;

                    item._data = this;
                    item._dataVersion = 0;
                    item.UpdateMenuItem(false);
                }
            }

            public void Execute() => _baseItem?.OnClick(EventArgs.Empty);

            internal int GetMenuID()
            {
                _cmd ??= new Command(this);

                return _cmd.ID;
            }

            private void ItemsChanged(MenuChangeKind change)
            {
                for (MenuItem? item = _firstItem; item is not null; item = item._nextLinkedItem)
                {
                    item.Parent?.ItemsChanged(change);
                }
            }

            internal void RemoveItem(MenuItem item)
            {
                Debug.Assert(item._data == this, "bad item passed to MenuItemData.removeItem");

                if (item == _firstItem)
                {
                    _firstItem = item._nextLinkedItem;
                }
                else
                {
                    MenuItem? itemT;
                    for (itemT = _firstItem; item != itemT!._nextLinkedItem;)
                    {
                        itemT = itemT._nextLinkedItem;
                    }

                    itemT._nextLinkedItem = item._nextLinkedItem;
                }
                item._nextLinkedItem = null;
                item._data = null!;
                item._dataVersion = 0;

                if (item == _baseItem)
                {
                    _baseItem = _firstItem!;
                }

                if (_firstItem is null)
                {
                    // No longer needed. Toss all references and the Command object.
                    Debug.Assert(_baseItem is null, "why isn't baseItem null?");
                    _onClick = null;
                    _onPopup = null;
                    _onSelect = null;
                    _onDrawItem = null;
                    _onMeasureItem = null;
                    if (_cmd is not null)
                    {
                        _cmd.Dispose();
                        _cmd = null;
                    }
                }
            }

            internal void SetCaption(string value)
            {
                value ??= string.Empty;

                if (!_caption.Equals(value))
                {
                    _caption = value;
                    UpdateMenuItems();
                }

#if DEBUG
                if (value.Length > 0)
                {
                    _baseItem._debugText = value;
                }
#endif
            }

            internal void SetState(int flag, bool value)
            {
                if (((_state & flag) != 0) != value)
                {
                    _state = value ? _state | flag : _state & ~flag;
                    if (flag != StateInMdiPopup)
                    {
                        UpdateMenuItems();
                    }
                }
            }

            private void UpdateMenuItems()
            {
                _version++;
                for (MenuItem? item = _firstItem; item is not null; item = item._nextLinkedItem)
                {
                    item.UpdateMenuItem(force: true);
                }
            }

        }

        private class MdiListUserData
        {
            public virtual void OnClick(EventArgs e)
            {
            }
        }

        private sealed class MdiListFormData : MdiListUserData
        {
            private readonly MenuItem _parent;
            private readonly int _boundIndex;

            public MdiListFormData(MenuItem parentItem, int boundFormIndex)
            {
                _boundIndex = boundFormIndex;
                _parent = parentItem;
            }

            public override void OnClick(EventArgs e)
            {
                if (_boundIndex != -1)
                {
                    Form[] forms = _parent.FindMdiForms(out _);
                    Debug.Assert(forms is not null, "Didn't get a list of the MDI Forms.");

                    if (forms is not null && forms.Length > _boundIndex)
                    {
                        Form boundForm = forms[_boundIndex];
                        boundForm.Activate();
                        if (boundForm.ActiveControl is not null && !boundForm.ActiveControl.Focused)
                        {
                            boundForm.ActiveControl.Focus();
                        }
                    }
                }
            }
        }

        private sealed class MdiListMoreWindowsData : MdiListUserData
        {
            private readonly MenuItem _parent;

            public MdiListMoreWindowsData(MenuItem parent)
            {
                _parent = parent;
            }

            public override void OnClick(EventArgs e)
            {
                Form[] forms = _parent.FindMdiForms(out var active);
                Debug.Assert(forms is not null, "Didn't get a list of the MDI Forms.");
                //Form active = _parent.GetMainMenu()./*GetFormUnsafe()*/form.ActiveMdiChild;
                Debug.Assert(active is not null, "Didn't get the active MDI child");
                if (forms is not null && forms.Length > 0 && active is not null)
                {
                    Type? t = Type.GetType("System.Windows.Forms.MdiWindowDialog, System.Windows.Forms");
                    if (t is not null)
                    {
                        MethodInfo? miSetItems = t.GetMethod("SetItems");
                        if (miSetItems is not null)
                        {
                            PropertyInfo? piActiveChildForm = t.GetProperty("ActiveChildForm");
                            if (piActiveChildForm is not null)
                            {
                                using Form dialog = (Form)Activator.CreateInstance(t)!;
                                miSetItems.Invoke(dialog, [active, forms]);
                                if (dialog.ShowDialog() == DialogResult.OK)
                                {
                                    active = (Form)piActiveChildForm.GetValue(dialog)!;
                                    goto activate;
                                }
                                return;
                            }
                        }
                    }

                    using (var dialog = new MdiWindowDialog())
                    {
                        dialog.SetItems(active, forms);
                        DialogResult result = dialog.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            active = dialog.ActiveChildForm!;
                        }
                        else
                        {
                            return;
                        }
                    }

                activate:
                    active.Activate();
                    if (active.ActiveControl is not null && !active.ActiveControl.Focused)
                    {
                        active.ActiveControl.Focus();
                    }
                }
            }
        }
    }
}

