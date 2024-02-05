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
    partial class Menu
    {
        [ListBindable(false)]
        public class MenuItemCollection : IList
        {
            private readonly Menu _owner;

            ///  A caching mechanism for key accessor
            ///  We use an index here rather than control so that we don't have lifetime
            ///  issues by holding on to extra references.
            private int _lastAccessedIndex = -1;

            public MenuItemCollection(Menu owner)
            {
                ArgumentNullException.ThrowIfNull(owner);
                _owner = owner;
            }

            public virtual MenuItem this[int index]
            {
                get
                {
                    if (index < 0 || index >= _owner.ItemCount)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                    }

                    return _owner._items[index];
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

            public int Count => _owner.ItemCount;

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
                => Add(_owner.ItemCount, item);

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
                    if (_owner is MenuItem parent)
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
                    if (item.Parent.Equals(_owner) && index > 0)
                    {
                        index--;
                    }

                    item.Parent.MenuItems.Remove(item);
                }

                // Validate our index
                if (index < 0 || index > _owner.ItemCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                }

                _owner._items.Insert(index, item);
                item.Parent = _owner;
                _owner.ItemsChanged(MenuChangeKind.CHANGE_ITEMS);
                if (_owner is MenuItem ownerMenuItem)
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
                if (IsValidIndex(_lastAccessedIndex))
                {
                    if (WindowsFormsUtils.SafeCompareStrings(this[_lastAccessedIndex].Name, key, /* ignoreCase = */ true))
                    {
                        return _lastAccessedIndex;
                    }
                }

                // step 2 - search for the item
                for (int i = 0; i < Count; i++)
                {
                    if (WindowsFormsUtils.SafeCompareStrings(this[i].Name, key, /* ignoreCase = */ true))
                    {
                        _lastAccessedIndex = i;
                        return i;
                    }
                }

                // step 3 - we didn't find it.  Invalidate the last accessed index and return -1.
                _lastAccessedIndex = -1;
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
                if (_owner.ItemCount > 0)
                {

                    for (int i = 0; i < _owner.ItemCount; i++)
                    {
                        _owner._items[i].Parent = null;
                    }

                    _owner._items.Clear();

                    _owner.ItemsChanged(MenuChangeKind.CHANGE_ITEMS);

                    if (_owner is MenuItem menuItem)
                    {
                        menuItem.UpdateMenuItem(true);
                    }
                }
            }

            public void CopyTo(Array dest, int index)
                => ((ICollection)_owner._items).CopyTo(dest, index);

            public IEnumerator GetEnumerator() => _owner._items.GetEnumerator();

            /// <summary>
            ///  Removes the item at the specified index in this menu.  All subsequent
            ///  items are moved up one slot.
            /// </summary>
            public virtual void RemoveAt(int index)
            {
                if (index < 0 || index >= _owner.ItemCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                }

                MenuItem item = _owner._items[index];
                item.Parent = null;
                _owner._items.RemoveAt(index);
                _owner.ItemsChanged(MenuChangeKind.CHANGE_ITEMS);

                //if the last item was removed, clear the collection
                //
                if (_owner.ItemCount == 0)
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
                if (item.Parent == _owner)
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
