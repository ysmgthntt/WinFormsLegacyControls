// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    /// <summary>
    ///  This is the base class for all menu components (MainMenu, MenuItem, and ContextMenu).
    /// </summary>
    //[ToolboxItemFilter("System.Windows.Forms")]
    [ListBindable(false)]
    public abstract partial class Menu : Component
    {
        /// <summary>
        ///  Used by findMenuItem
        /// </summary>
        public const int FindHandle = 0;
        /// <summary>
        ///  Used by findMenuItem
        /// </summary>
        public const int FindShortcut = 1;

        private MenuItemCollection? _itemsCollection;
        internal readonly List<MenuItem> _items = new();
        internal IntPtr _handle;
        internal bool _created;
        private object? _userData;
        private string? _name;

        /// <summary>
        ///  This is an abstract class.  Instances cannot be created, so the constructor
        ///  is only called from derived classes.
        /// </summary>
        protected Menu(MenuItem[]? items)
        {
            if (items is not null)
            {
                MenuItems.AddRange(items);
            }
        }

        /// <summary>
        ///  The HMENU handle corresponding to this menu.
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [SRDescription(nameof(SR.ControlHandleDescr))]
        public IntPtr Handle
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    _handle = CreateMenuHandle();
                }

                CreateMenuItems();
                return _handle;
            }
        }

        /// <summary>
        ///  Specifies whether this menu contains any items.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [SRDescription(nameof(SR.MenuIsParentDescr))]
        public virtual bool IsParent => _items.Count > 0;

        internal int ItemCount => _items.Count;

        /// <summary>
        ///  The MenuItem that contains the list of MDI child windows.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [SRDescription(nameof(SR.MenuMDIListItemDescr))]
        public MenuItem? MdiListItem
        {
            get
            {
                for (int i = 0; i < ItemCount; i++)
                {
                    MenuItem? item = _items[i];
                    if (item.MdiList)
                    {
                        return item;
                    }

                    if (item.IsParent)
                    {
                        item = item.MdiListItem;
                        if (item is not null)
                        {
                            return item;
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        ///  Name of this control. The designer will set this to the same
        ///  as the programatic Id "(name)" of the control - however this
        ///  property has no bearing on the runtime aspects of this control.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string Name
        {
            get => WindowsFormsUtils.GetComponentName(this, _name);
            set
            {
                if (value is null || value.Length == 0)
                {
                    _name = null;
                }
                else
                {
                    _name = value;
                }
                if (Site is not null)
                {
                    Site.Name = _name;
                }
            }
        }

        //[Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [SRDescription(nameof(SR.MenuMenuItemsDescr))]
        [MergableProperty(false)]
        public MenuItemCollection MenuItems
            => _itemsCollection ??= new MenuItemCollection(this);

        internal abstract bool RenderIsRightToLeft { get; }

        [SRCategory(nameof(SR.CatData))]
        [Localizable(false)]
        [Bindable(true)]
        [SRDescription(nameof(SR.ControlTagDescr))]
        [DefaultValue(null)]
        [TypeConverter(typeof(StringConverter))]
        public object? Tag
        {
            get => _userData;
            set => _userData = value;
        }

        /// <summary>
        ///  Notifies Menu that someone called Windows.DeleteMenu on its handle.
        /// </summary>
        internal void ClearHandles()
        {
            if (_handle != IntPtr.Zero)
            {
                PInvoke.DestroyMenu(this);
            }
            _handle = IntPtr.Zero;
            if (_created)
            {
                for (int i = 0; i < ItemCount; i++)
                {
                    _items[i].ClearHandles();
                }
                _created = false;
            }
        }

        /// <summary>
        ///  Sets this menu to be an identical copy of another menu.
        /// </summary>
        protected void CloneMenu(Menu menuSrc)
        {
            ArgumentNullException.ThrowIfNull(menuSrc);

            MenuItem[]? newItems = null;
            int count = menuSrc.ItemCount;
            if (count > 0)
            {
                newItems = new MenuItem[count];
                for (int i = 0; i < count; i++)
                {
                    newItems[i] = menuSrc.MenuItems[i].CloneMenu();
                }
            }
            MenuItems.Clear();
            if (newItems is not null)
            {
                MenuItems.AddRange(newItems);
            }
        }

        protected virtual IntPtr CreateMenuHandle()
            => (IntPtr)PInvoke.CreatePopupMenu();

        private protected void CreateMenuItems()
        {
            if (!_created)
            {
                for (int i = 0; i < ItemCount; i++)
                {
                    _items[i].CreateMenuItem();
                }
                _created = true;
            }
        }

        private void DestroyMenuItems()
        {
            if (_created)
            {
                for (int i = 0; i < ItemCount; i++)
                {
                    _items[i].ClearHandles();
                }
                while (PInvoke.GetMenuItemCount(this) > 0)
                {
                    PInvoke.RemoveMenu(this, 0, MENU_ITEM_FLAGS.MF_BYPOSITION);
                }
                _created = false;
            }
        }

        /// <summary>
        ///  Disposes of the component.  Call dispose when the component is no longer needed.
        ///  This method removes the component from its container (if the component has a site)
        ///  and triggers the dispose event.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    MenuItem item = _items[i];

                    // remove the item before we dispose it so it still has valid state
                    // for undo/redo
                    //
                    item.Site?.Container?.Remove(item);

                    item.Parent = null;
                    item.Dispose();
                }
                _items.Clear();
            }
            if (_handle != IntPtr.Zero)
            {
                PInvoke.DestroyMenu(this);
                _handle = IntPtr.Zero;
                if (disposing)
                {
                    ClearHandles();
                }
            }
            base.Dispose(disposing);
        }

        public MenuItem? FindMenuItem(int type, IntPtr value)
            => type switch
            {
                FindHandle => FindMenuItemByHandle(value),
                FindShortcut => FindMenuItemByShortcut((Shortcut)(int)value),
                _ => null
            };

        private MenuItem? FindMenuItemByHandle(IntPtr value)
        {
            for (int i = 0; i < ItemCount; i++)
            {
                MenuItem? item = _items[i];
                if (item._handle == value)
                {
                    return item;
                }
                item = item.FindMenuItemByHandle(value);
                if (item is not null)
                {
                    return item;
                }
            }
            return null;
        }

        private MenuItem? FindMenuItemByShortcut(Shortcut value)
        {
            for (int i = 0; i < ItemCount; i++)
            {
                MenuItem? item = _items[i];
                if (item.Shortcut == value)
                {
                    return item;
                }
                item = item.FindMenuItemByShortcut(value);
                if (item is not null)
                {
                    return item;
                }
            }
            return null;
        }

        protected int FindMergePosition(int mergeOrder)
        {
            int iMin, iLim, iT;

            for (iMin = 0, iLim = ItemCount; iMin < iLim;)
            {
                iT = (iMin + iLim) / 2;
                if (_items[iT].MergeOrder <= mergeOrder)
                {
                    iMin = iT + 1;
                }
                else
                {
                    iLim = iT;
                }
            }
            return iMin;
        }

        // A new method for finding the approximate merge position. The original
        // method assumed (incorrectly) that the MergeOrder of the target menu would be sequential
        // as it's guaranteed to be in the MDI imlementation of merging container and child
        // menus. However, user code can call MergeMenu independently on a source and target
        // menu whose MergeOrder values are not necessarily pre-sorted.
        private int xFindMergePosition(int mergeOrder)
        {
            int nPosition = 0;

            // Iterate from beginning to end since we can't assume any sequential ordering to MergeOrder
            for (int nLoop = 0; nLoop < ItemCount; nLoop++)
            {

                if (_items[nLoop].MergeOrder > mergeOrder)
                {
                    // We didn't find what we're looking for, but we've found a stopping point.
                    break;
                }
                else if (_items[nLoop].MergeOrder < mergeOrder)
                {
                    // We might have found what we're looking for, but we'll have to come around again
                    // to know.
                    nPosition = nLoop + 1;
                }
                else if (mergeOrder == _items[nLoop].MergeOrder)
                {
                    // We've found what we're looking for, so use this value for the merge order
                    nPosition = nLoop;
                    break;
                }
            }

            return nPosition;
        }

        //There's a win32 problem that doesn't allow menus to cascade right to left
        //unless we explicitely set the bit on the menu the first time it pops up
        internal void UpdateRtl(bool setRightToLeftBit)
        {
            foreach (MenuItem item in MenuItems)
            {
                item.UpdateItemRtl(setRightToLeftBit);
                item.UpdateRtl(setRightToLeftBit);
            }
        }

        /// <summary>
        ///  Returns the ContextMenu that contains this menu.  The ContextMenu
        ///  is at the top of this menu's parent chain.
        ///  Returns null if this menu is not contained in a ContextMenu.
        ///  This can occur if it's contained in a MainMenu or if it isn't
        ///  currently contained in any menu at all.
        /// </summary>
        public ContextMenu? GetContextMenu()
        {
            Menu? menuT;
            for (menuT = this; menuT is not ContextMenu;)
            {
                if (menuT is MenuItem menuItem)
                {
                    menuT = menuItem.Parent;
                }
                else
                {
                    return null;
                }
            }
            return (ContextMenu)menuT;

        }

        /// <summary>
        ///  Returns the MainMenu item that contains this menu.  The MainMenu
        ///  is at the top of this menu's parent chain.
        ///  Returns null if this menu is not contained in a MainMenu.
        ///  This can occur if it's contained in a ContextMenu or if it isn't
        ///  currently contained in any menu at all.
        /// </summary>
        public MainMenu? GetMainMenu()
        {
            Menu? menuT;
            for (menuT = this; menuT is not MainMenu;)
            {
                if (menuT is MenuItem menuItem)
                {
                    menuT = menuItem.Parent;
                }
                else
                {
                    return null;
                }
            }
            return (MainMenu)menuT;
        }

        internal virtual void ItemsChanged(MenuChangeKind change)
        {
            switch (change)
            {
                case MenuChangeKind.CHANGE_ITEMS:
                case MenuChangeKind.CHANGE_VISIBLE:
                    DestroyMenuItems();
                    break;
            }
        }

        /// <summary>
        ///  Walks the menu item collection, using a caller-supplied delegate to find one
        ///  with a matching access key. Walk starts at specified item index and performs one
        ///  full pass of the entire collection, looping back to the top if necessary.
        ///
        ///  Return value is intended for return from WM_MENUCHAR message. It includes both
        ///  index of matching item, and action for OS to take (execute or select). Zero is
        ///  used to indicate that no match was found (OS should ignore key and beep).
        /// </summary>
        private IntPtr MatchKeyToMenuItem(int startItem, char key, bool noMnemonic, out bool containsOwnerDraw)
        {
            int firstMatch = -1;
            bool multipleMatches = false;

            containsOwnerDraw = false;

            for (int i = 0; i < _items.Count && !multipleMatches; ++i)
            {
                int itemIndex = (startItem + i) % _items.Count;
                MenuItem mi = _items[itemIndex];
                if (mi is not null && mi.OwnerDraw)
                {
                    containsOwnerDraw = true;
                    if ((!noMnemonic && mi.Mnemonic == key) ||
                        (noMnemonic && mi.Mnemonic == 0 && mi.Text.Length > 0 && char.ToUpper(mi.Text[0], CultureInfo.CurrentCulture) == key))
                    {
                        if (firstMatch < 0)
                        {
                            // Using Index doesnt respect hidden items.
                            firstMatch = mi.MenuIndex;
                        }
                        else
                        {
                            multipleMatches = true;
                        }
                    }
                }
            }

            if (firstMatch < 0)
            {
                return IntPtr.Zero;
            }

            uint action = multipleMatches ? PInvoke.MNC_SELECT : PInvoke.MNC_EXECUTE;
            return (IntPtr)LRESULT.MAKELONG(firstMatch, (int)action);
        }

        /// <summary>
        ///  Merges another menu's items with this one's.  Menu items are merged according to their
        ///  mergeType and mergeOrder properties.  This function is typically used to
        ///  merge an MDI container's menu with that of its active MDI child.
        /// </summary>
        public virtual void MergeMenu(Menu menuSrc)
        {
            ArgumentNullException.ThrowIfNull(menuSrc);
            if (menuSrc == this)
            {
                throw new ArgumentException(SR.MenuMergeWithSelf, nameof(menuSrc));
            }

            //if (menuSrc.items is not null && items is null)
            if (menuSrc._items.Count > 0 && _items.Count == 0)
            {
                MenuItems.Clear();
            }

            for (int i = 0; i < menuSrc.ItemCount; i++)
            {
                MenuItem item = menuSrc._items[i];

                switch (item.MergeType)
                {
                    default:
                        continue;
                    case MenuMerge.Add:
                        MenuItems.Add(FindMergePosition(item.MergeOrder), item.MergeMenu());
                        continue;
                    case MenuMerge.Replace:
                    case MenuMerge.MergeItems:
                        break;
                }

                int mergeOrder = item.MergeOrder;
                // Can we find a menu item with a matching merge order?
                // Use new method to find the approximate merge position. The original
                // method assumed (incorrectly) that the MergeOrder of the target menu would be sequential
                // as it's guaranteed to be in the MDI imlementation of merging container and child
                // menus. However, user code can call MergeMenu independently on a source and target
                // menu whose MergeOrder values are not necessarily pre-sorted.
                for (int j = xFindMergePosition(mergeOrder); ; j++)
                {

                    if (j >= ItemCount)
                    {
                        // A matching merge position could not be found,
                        // so simply append this menu item to the end.
                        MenuItems.Add(j, item.MergeMenu());
                        break;
                    }
                    MenuItem itemDst = _items[j];
                    if (itemDst.MergeOrder != mergeOrder)
                    {
                        MenuItems.Add(j, item.MergeMenu());
                        break;
                    }
                    if (itemDst.MergeType != MenuMerge.Add)
                    {
                        if (item.MergeType != MenuMerge.MergeItems
                            || itemDst.MergeType != MenuMerge.MergeItems)
                        {
                            itemDst.Dispose();
                            MenuItems.Add(j, item.MergeMenu());
                        }
                        else
                        {
                            itemDst.MergeMenu(item);
                        }
                        break;
                    }
                }
            }
        }

        internal virtual bool ProcessInitMenuPopup(IntPtr handle)
        {
            MenuItem? item = FindMenuItemByHandle(handle);
            if (item is not null)
            {
                item.OnInitMenuPopup(EventArgs.Empty);
                item.CreateMenuItems();
                return true;
            }
            return false;
        }

        protected internal virtual bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            MenuItem? item = FindMenuItemByShortcut((Shortcut)(int)keyData);
            return item is not null ? item.ShortcutClick() : false;
        }

        /// <summary>
        ///  Returns index of currently selected menu item in
        ///  this menu, or -1 if no item is currently selected.
        /// </summary>
        private int SelectedMenuItemIndex
        {
            get
            {
                for (int i = 0; i < _items.Count; ++i)
                {
                    if (_items[i].Selected)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        /// <summary>
        ///  Returns a string representation for this control.
        /// </summary>
        public override string ToString()
            => $"{base.ToString()}, Items.Count: {ItemCount}";

        /// <summary>
        ///  Handles the WM_MENUCHAR message, forwarding it to the intended Menu
        ///  object. All the real work is done inside WmMenuCharInternal().
        /// </summary>
        internal void WmMenuChar(ref Message m)
        {
            Menu? menu;
            if (m.LParam == _handle)
            {
                menu = this;
            }
            else
            {
                menu = FindMenuItemByHandle(m.LParam);
                if (menu is null)
                {
                    return;
                }
            }

            char menuKey = char.ToUpper((char)Interop.PARAM.LOWORD(m.WParam), CultureInfo.CurrentCulture);

            m.Result = menu.WmMenuCharInternal(menuKey);
        }

        /// <summary>
        ///  Handles WM_MENUCHAR to provide access key support for owner-draw menu items (which
        ///  means *all* menu items on a menu when IsImageMarginPresent == true). Attempts to
        ///  simulate the exact behavior that the OS provides for non owner-draw menu items.
        /// </summary>
        private IntPtr WmMenuCharInternal(char key)
        {
            // Start looking just beyond the current selected item (otherwise just start at the top)
            int startItem = (SelectedMenuItemIndex + 1) % _items.Count;

            // First, search for match among owner-draw items with explicitly defined access keys (eg. "S&ave")
            IntPtr result = MatchKeyToMenuItem(startItem, key, false, out bool containsOwnerDraw);

            if (!containsOwnerDraw)
                return IntPtr.Zero;

            // Next, search for match among owner-draw items with no access keys (looking at first char of item text)
            if (result == IntPtr.Zero)
            {
                result = MatchKeyToMenuItem(startItem, key, true, out _);
            }

            return result;
        }
    }
}
