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
    [ToolboxItemFilter("System.Windows.Forms")]
    [ListBindable(false)]
    public abstract class Menu : Component
    {
        /// <summary>
        ///  Used by findMenuItem
        /// </summary>
        public const int FindHandle = 0;
        /// <summary>
        ///  Used by findMenuItem
        /// </summary>
        public const int FindShortcut = 1;

        private MenuItemCollection? itemsCollection;
        internal readonly List<MenuItem> _items = new();
        internal IntPtr handle;
        internal bool created;
        private object? userData;
        private string? name;

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
        [
        Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        SRDescription(nameof(SR.ControlHandleDescr))
        ]
        public IntPtr Handle
        {
            get
            {
                if (handle == IntPtr.Zero)
                {
                    handle = CreateMenuHandle();
                }

                CreateMenuItems();
                return handle;
            }
        }

        /// <summary>
        ///  Specifies whether this menu contains any items.
        /// </summary>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        SRDescription(nameof(SR.MenuIsParentDescr))
        ]
        public virtual bool IsParent
            => _items.Count > 0;

        internal int ItemCount
            => _items.Count;

        /// <summary>
        ///  The MenuItem that contains the list of MDI child windows.
        /// </summary>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        SRDescription(nameof(SR.MenuMDIListItemDescr))
        ]
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
        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        Browsable(false)
        ]
        public string Name
        {
            get
            {
                return WindowsFormsUtils.GetComponentName(this, name);
            }
            set
            {
                if (value is null || value.Length == 0)
                {
                    name = null;
                }
                else
                {
                    name = value;
                }
                if (Site is not null)
                {
                    Site.Name = name;
                }
            }
        }

        [
        //Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        SRDescription(nameof(SR.MenuMenuItemsDescr)),
        MergableProperty(false)
        ]
        public MenuItemCollection MenuItems
            => itemsCollection ??= new MenuItemCollection(this);

        internal abstract bool RenderIsRightToLeft { get; }

        [
        SRCategory(nameof(SR.CatData)),
        Localizable(false),
        Bindable(true),
        SRDescription(nameof(SR.ControlTagDescr)),
        DefaultValue(null),
        TypeConverter(typeof(StringConverter)),
        ]
        public object? Tag
        {
            get
            {
                return userData;
            }
            set
            {
                userData = value;
            }
        }

        /// <summary>
        ///  Notifies Menu that someone called Windows.DeleteMenu on its handle.
        /// </summary>
        internal void ClearHandles()
        {
            if (handle != IntPtr.Zero)
            {
                PInvoke.DestroyMenu(this);
            }
            handle = IntPtr.Zero;
            if (created)
            {
                for (int i = 0; i < ItemCount; i++)
                {
                    _items[i].ClearHandles();
                }
                created = false;
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
        {
            return (IntPtr)PInvoke.CreatePopupMenu();
        }

        private protected void CreateMenuItems()
        {
            if (!created)
            {
                for (int i = 0; i < ItemCount; i++)
                {
                    _items[i].CreateMenuItem();
                }
                created = true;
            }
        }

        private void DestroyMenuItems()
        {
            if (created)
            {
                for (int i = 0; i < ItemCount; i++)
                {
                    _items[i].ClearHandles();
                }
                while (PInvoke.GetMenuItemCount(this) > 0)
                {
                    PInvoke.RemoveMenu(this, 0, MENU_ITEM_FLAGS.MF_BYPOSITION);
                }
                created = false;
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
                    if (item.Site is not null && item.Site.Container is not null)
                    {
                        item.Site.Container.Remove(item);
                    }

                    item.Parent = null;
                    item.Dispose();
                }
                _items.Clear();
            }
            if (handle != IntPtr.Zero)
            {
                PInvoke.DestroyMenu(this);
                handle = IntPtr.Zero;
                if (disposing)
                {
                    ClearHandles();
                }
            }
            base.Dispose(disposing);
        }

        public MenuItem? FindMenuItem(int type, IntPtr value)
        {
            for (int i = 0; i < ItemCount; i++)
            {
                MenuItem? item = _items[i];
                switch (type)
                {
                    case FindHandle:
                        if (item.handle == value)
                        {
                            return item;
                        }

                        break;
                    case FindShortcut:
                        if (item.Shortcut == (Shortcut)(int)value)
                        {
                            return item;
                        }

                        break;
                }
                item = item.FindMenuItem(type, value);
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
            MenuItem? item = FindMenuItem(FindHandle, handle);
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
            MenuItem? item = FindMenuItem(FindShortcut, (IntPtr)(int)keyData);
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
        {
            string s = base.ToString();
            return s + ", Items.Count: " + ItemCount.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///  Handles the WM_MENUCHAR message, forwarding it to the intended Menu
        ///  object. All the real work is done inside WmMenuCharInternal().
        /// </summary>
        internal void WmMenuChar(ref Message m)
        {
            Menu? menu = (m.LParam == handle) ? this : FindMenuItem(FindHandle, m.LParam);

            if (menu is null)
            {
                return;
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

        [ListBindable(false)]
        public class MenuItemCollection : IList
        {
            private readonly Menu owner;

            ///  A caching mechanism for key accessor
            ///  We use an index here rather than control so that we don't have lifetime
            ///  issues by holding on to extra references.
            private int lastAccessedIndex = -1;

            public MenuItemCollection(Menu owner)
            {
                ArgumentNullException.ThrowIfNull(owner);
                this.owner = owner;
            }

            public virtual MenuItem this[int index]
            {
                get
                {
                    if (index < 0 || index >= owner.ItemCount)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                    }

                    return owner._items[index];
                }
                // set not supported
            }

            object? IList.this[int index]
            {
                get => this[index];
                set => throw new NotSupportedException();
            }

            /// <summary>
            ///  Retrieves the child control with the specified key.
            /// </summary>
            public virtual MenuItem? this[string key]
            {
                get
                {
                    // We do not support null and empty string as valid keys.
                    if (string.IsNullOrEmpty(key))
                    {
                        return null;
                    }

                    // Search for the key in our collection
                    int index = IndexOfKey(key);
                    if (IsValidIndex(index))
                    {
                        return this[index];
                    }
                    else
                    {
                        return null;
                    }

                }
            }

            public int Count => owner.ItemCount;

            object ICollection.SyncRoot => this;

            bool ICollection.IsSynchronized => false;

            bool IList.IsFixedSize => false;

            public bool IsReadOnly => false;

            /// <summary>
            ///  Adds a new MenuItem to the end of this menu with the specified caption.
            /// </summary>
            public virtual MenuItem Add(string caption)
            {
                MenuItem item = new MenuItem(caption);
                Add(item);
                return item;
            }

            /// <summary>
            ///  Adds a new MenuItem to the end of this menu with the specified caption,
            ///  and click handler.
            /// </summary>
            public virtual MenuItem Add(string caption, EventHandler onClick)
            {
                MenuItem item = new MenuItem(caption, onClick);
                Add(item);
                return item;
            }

            /// <summary>
            ///  Adds a new MenuItem to the end of this menu with the specified caption,
            ///  click handler, and items.
            /// </summary>
            public virtual MenuItem Add(string caption, MenuItem[] items)
            {
                MenuItem item = new MenuItem(caption, items);
                Add(item);
                return item;
            }

            /// <summary>
            ///  Adds a MenuItem to the end of this menu
            ///  MenuItems can only be contained in one menu at a time, and may not be added
            ///  more than once to the same menu.
            /// </summary>
            public virtual int Add(MenuItem item)
                => Add(owner.ItemCount, item);

            /// <summary>
            ///  Adds a MenuItem to this menu at the specified index.  The item currently at
            ///  that index, and all items after it, will be moved up one slot.
            ///  MenuItems can only be contained in one menu at a time, and may not be added
            ///  more than once to the same menu.
            /// </summary>
            public virtual int Add(int index, MenuItem item)
            {
                ArgumentNullException.ThrowIfNull(item);

                // MenuItems can only belong to one menu at a time
                if (item.Parent is not null)
                {

                    // First check that we're not adding ourself, i.e. walk
                    // the parent chain for equality
                    if (owner is MenuItem parent)
                    {
                        while (parent is not null)
                        {
                            if (parent.Equals(item))
                            {
                                throw new ArgumentException(string.Format(SR.MenuItemAlreadyExists, item.Text), nameof(item));
                            }
                            if (parent.Parent is MenuItem menuItem)
                            {
                                parent = menuItem;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    //if we're re-adding an item back to the same collection
                    //the target index needs to be decremented since we're
                    //removing an item from the collection
                    if (item.Parent.Equals(owner) && index > 0)
                    {
                        index--;
                    }

                    item.Parent.MenuItems.Remove(item);
                }

                // Validate our index
                if (index < 0 || index > owner.ItemCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                }

                owner._items.Insert(index, item);
                item.Parent = owner;
                owner.ItemsChanged(MenuChangeKind.CHANGE_ITEMS);
                if (owner is MenuItem ownerMenuItem)
                {
                    ownerMenuItem.ItemsChanged(MenuChangeKind.CHANGE_ITEMADDED, item);
                }

                return index;
            }

            public virtual void AddRange(MenuItem[] items)
            {
                ArgumentNullException.ThrowIfNull(items);
                foreach (MenuItem item in items)
                {
                    Add(item);
                }
            }

            int IList.Add(object? value)
            {
                if (value is MenuItem menuItem)
                {
                    return Add(menuItem);
                }
                else
                {
                    throw new ArgumentException(SR.MenuBadMenuItem, nameof(value));
                }
            }

            public bool Contains(MenuItem value)
                => IndexOf(value) != -1;

            bool IList.Contains(object? value)
            {
                if (value is MenuItem menuItem)
                {
                    return Contains(menuItem);
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            ///  Returns true if the collection contains an item with the specified key, false otherwise.
            /// </summary>
            public virtual bool ContainsKey(string key)
                => IsValidIndex(IndexOfKey(key));

            /// <summary>
            ///  Searches for Controls by their Name property, builds up an array
            ///  of all the controls that match.
            /// </summary>
            public MenuItem[] Find(string key, bool searchAllChildren)
            {

                if ((key is null) || (key.Length == 0))
                {
                    throw new ArgumentNullException(nameof(key), SR.FindKeyMayNotBeEmptyOrNull);
                }

                List<MenuItem> foundMenuItems = new();
                FindInternal(key, searchAllChildren, this, foundMenuItems);
                return foundMenuItems.ToArray();
            }

            /// <summary>
            ///  Searches for Controls by their Name property, builds up an array list
            ///  of all the controls that match.
            /// </summary>
            private static void FindInternal(string key, bool searchAllChildren, MenuItemCollection menuItemsToLookIn, List<MenuItem> foundMenuItems)
            {
                // Perform breadth first search - as it's likely people will want controls belonging
                // to the same parent close to each other.
                for (int i = 0; i < menuItemsToLookIn.Count; i++)
                {
                    if (menuItemsToLookIn[i] is null)
                    {
                        continue;
                    }

                    if (WindowsFormsUtils.SafeCompareStrings(menuItemsToLookIn[i].Name, key, ignoreCase: true))
                    {
                        foundMenuItems.Add(menuItemsToLookIn[i]);
                    }
                }

                // Optional recurive search for controls in child collections.
                if (searchAllChildren)
                {
                    for (int i = 0; i < menuItemsToLookIn.Count; i++)
                    {
                        if (menuItemsToLookIn[i] is null)
                        {
                            continue;
                        }
                        if (menuItemsToLookIn[i].ItemCount > 0)
                        {
                            // if it has a valid child collecion, append those results to our collection
                            FindInternal(key, searchAllChildren, menuItemsToLookIn[i].MenuItems, foundMenuItems);
                        }
                    }
                }
            }

            public int IndexOf(MenuItem value)
            {
                for (int index = 0; index < Count; ++index)
                {
                    if (this[index] == value)
                    {
                        return index;
                    }
                }
                return -1;
            }

            int IList.IndexOf(object? value)
            {
                if (value is MenuItem menuItem)
                {
                    return IndexOf(menuItem);
                }
                else
                {
                    return -1;
                }
            }

            /// <summary>
            ///  The zero-based index of the first occurrence of value within the entire CollectionBase, if found; otherwise, -1.
            /// </summary>
            public virtual int IndexOfKey(string key)
            {
                // Step 0 - Arg validation
                if (string.IsNullOrEmpty(key))
                {
                    return -1; // we dont support empty or null keys.
                }

                // step 1 - check the last cached item
                if (IsValidIndex(lastAccessedIndex))
                {
                    if (WindowsFormsUtils.SafeCompareStrings(this[lastAccessedIndex].Name, key, /* ignoreCase = */ true))
                    {
                        return lastAccessedIndex;
                    }
                }

                // step 2 - search for the item
                for (int i = 0; i < Count; i++)
                {
                    if (WindowsFormsUtils.SafeCompareStrings(this[i].Name, key, /* ignoreCase = */ true))
                    {
                        lastAccessedIndex = i;
                        return i;
                    }
                }

                // step 3 - we didn't find it.  Invalidate the last accessed index and return -1.
                lastAccessedIndex = -1;
                return -1;
            }

            void IList.Insert(int index, object? value)
            {
                if (value is MenuItem menuItem)
                {
                    Add(index, menuItem);
                }
                else
                {
                    throw new ArgumentException(SR.MenuBadMenuItem, nameof(value));
                }
            }

            /// <summary>
            ///  Determines if the index is valid for the collection.
            /// </summary>
            private bool IsValidIndex(int index)
                => ((index >= 0) && (index < Count));

            /// <summary>
            ///  Removes all existing MenuItems from this menu
            /// </summary>
            public virtual void Clear()
            {
                if (owner.ItemCount > 0)
                {

                    for (int i = 0; i < owner.ItemCount; i++)
                    {
                        owner._items[i].Parent = null;
                    }

                    owner._items.Clear();

                    owner.ItemsChanged(MenuChangeKind.CHANGE_ITEMS);

                    if (owner is MenuItem menuItem)
                    {
                        menuItem.UpdateMenuItem(true);
                    }
                }
            }

            public void CopyTo(Array dest, int index)
                => ((ICollection)owner._items).CopyTo(dest, index);

            public IEnumerator GetEnumerator()
                => owner._items.GetEnumerator();

            /// <summary>
            ///  Removes the item at the specified index in this menu.  All subsequent
            ///  items are moved up one slot.
            /// </summary>
            public virtual void RemoveAt(int index)
            {
                if (index < 0 || index >= owner.ItemCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                }

                MenuItem item = owner._items[index];
                item.Parent = null;
                owner._items.RemoveAt(index);
                owner.ItemsChanged(MenuChangeKind.CHANGE_ITEMS);

                //if the last item was removed, clear the collection
                //
                if (owner.ItemCount == 0)
                {
                    Clear();
                }

            }

            /// <summary>
            ///  Removes the menu iteml with the specified key.
            /// </summary>
            public virtual void RemoveByKey(string key)
            {
                int index = IndexOfKey(key);
                if (IsValidIndex(index))
                {
                    RemoveAt(index);
                }
            }

            /// <summary>
            ///  Removes the specified item from this menu.  All subsequent
            ///  items are moved down one slot.
            /// </summary>
            public virtual void Remove(MenuItem item)
            {
                if (item.Parent == owner)
                {
                    RemoveAt(item.Index);
                }
            }

            void IList.Remove(object? value)
            {
                if (value is MenuItem menuItem)
                {
                    Remove(menuItem);
                }
            }
        }
    }
}
