// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using WinFormsLegacyControls.Menus.Migration;
using static Interop;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    partial class ToolBar
    {
        /// <summary>
        ///  Encapsulates a collection of <see cref='ToolBarButton'/> controls for use by the
        /// <see cref='ToolBar'/> class.
        /// </summary>
        public class ToolBarButtonCollection : IList
        {
            private readonly ToolBar _owner;
            private bool _suspendUpdate;
            ///  A caching mechanism for key accessor
            ///  We use an index here rather than control so that we don't have lifetime
            ///  issues by holding on to extra references.
            private int _lastAccessedIndex = -1;

            /// <summary>
            ///  Initializes a new instance of the <see cref='ToolBarButtonCollection'/> class and assigns it to the specified toolbar.
            /// </summary>
            public ToolBarButtonCollection(ToolBar owner)
            {
                ArgumentNullException.ThrowIfNull(owner);
                _owner = owner;
            }

            /// <summary>
            ///  Gets or sets the toolbar button at the specified indexed location in the
            ///  toolbar button collection.
            /// </summary>
            public virtual ToolBarButton this[int index]
            {
                get
                {
                    if (index < 0 || index >= _owner._buttons.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                    }

                    return _owner._buttons[index];
                }
                set
                {

                    // Sanity check parameters
                    //
                    if (index < 0 || index >= _owner._buttons.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                    }
                    ArgumentNullException.ThrowIfNull(value);

                    _owner.InternalSetButton(index, value, true, true);
                }
            }

            object? IList.this[int index]
            {
                get => this[index];
                set
                {
                    if (value is ToolBarButton toolBarButton)
                    {
                        this[index] = toolBarButton;
                    }
                    else
                    {
                        throw new ArgumentException(SR.ToolBarBadToolBarButton, nameof(value));
                    }
                }
            }

            /// <summary>
            ///  Retrieves the child control with the specified key.
            /// </summary>
            public virtual ToolBarButton? this[string key]
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

            /// <summary>
            ///  Gets the number of buttons in the toolbar button collection.
            /// </summary>
            [Browsable(false)]
            public int Count => _owner._buttons.Count;

            object ICollection.SyncRoot => this;

            bool ICollection.IsSynchronized => false;

            bool IList.IsFixedSize => false;

            public bool IsReadOnly => false;

            /// <summary>
            ///  Adds a new toolbar button to
            ///  the end of the toolbar button collection.
            /// </summary>
            public int Add(ToolBarButton button)
            {

                int index = _owner.InternalAddButton(button);

                if (!_suspendUpdate)
                {
                    _owner.UpdateButtons();
                }

                return index;
            }

            public int Add(string text)
            {
                ToolBarButton button = new ToolBarButton(text);
                return Add(button);
            }

            int IList.Add(object? button)
            {
                if (button is ToolBarButton toolBarButton)
                {
                    return Add(toolBarButton);
                }
                else
                {
                    throw new ArgumentException(SR.ToolBarBadToolBarButton, nameof(button));
                }
            }

            public void AddRange(ToolBarButton[] buttons)
            {
                ArgumentNullException.ThrowIfNull(buttons);
                try
                {
                    _suspendUpdate = true;
                    foreach (ToolBarButton button in buttons)
                    {
                        Add(button);
                    }
                }
                finally
                {
                    _suspendUpdate = false;
                    _owner.UpdateButtons();
                }
            }

            /// <summary>
            ///  Removes
            ///  all buttons from the toolbar button collection.
            /// </summary>
            public void Clear()
            {

                if (_owner._buttons.Count == 0)
                {
                    return;
                }

                for (int x = _owner._buttons.Count; x > 0; x--)
                {
                    if (_owner.IsHandleCreated)
                    {
                        PInvoke.SendMessage(_owner, PInvoke.TB_DELETEBUTTON, (WPARAM)(x - 1), 0);
                    }
                    _owner.RemoveAt(x - 1);
                }

                _owner._buttons.Clear();

                if (!_owner.Disposing)
                {
                    _owner.UpdateButtons();
                }
            }

            public bool Contains(ToolBarButton button)
                => IndexOf(button) != -1;

            bool IList.Contains(object? button)
            {
                if (button is ToolBarButton toolBarButton)
                {
                    return Contains(toolBarButton);
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

            void ICollection.CopyTo(Array dest, int index)
                => ((ICollection)_owner._buttons).CopyTo(dest, index);

            public int IndexOf(ToolBarButton button)
            {
                for (int index = 0; index < Count; ++index)
                {
                    if (this[index] == button)
                    {
                        return index;
                    }
                }
                return -1;
            }

            int IList.IndexOf(object? button)
            {
                if (button is ToolBarButton toolBarButton)
                {
                    return IndexOf(toolBarButton);
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

            public void Insert(int index, ToolBarButton button)
                => _owner.InsertButton(index, button);

            void IList.Insert(int index, object? button)
            {
                if (button is ToolBarButton toolBarButton)
                {
                    Insert(index, toolBarButton);
                }
                else
                {
                    throw new ArgumentException(SR.ToolBarBadToolBarButton, nameof(button));
                }
            }

            /// <summary>
            ///  Determines if the index is valid for the collection.
            /// </summary>
            private bool IsValidIndex(int index)
                => ((index >= 0) && (index < Count));

            /// <summary>
            ///  Removes
            ///  a given button from the toolbar button collection.
            /// </summary>
            public void RemoveAt(int index)
            {
                if (index < 0 || index >= _owner._buttons.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), string.Format(SR.InvalidArgument, "index", index.ToString(CultureInfo.CurrentCulture)));
                }

                if (_owner.IsHandleCreated)
                {
                    PInvoke.SendMessage(_owner, PInvoke.TB_DELETEBUTTON, (WPARAM)index, 0);
                }

                _owner.RemoveAt(index);
                _owner.UpdateButtons();

            }

            /// <summary>
            ///  Removes the child control with the specified key.
            /// </summary>
            public virtual void RemoveByKey(string key)
            {
                int index = IndexOfKey(key);
                if (IsValidIndex(index))
                {
                    RemoveAt(index);
                }
            }

            public void Remove(ToolBarButton button)
            {
                int index = IndexOf(button);
                if (index != -1)
                {
                    RemoveAt(index);
                }
            }

            void IList.Remove(object? button)
            {
                if (button is ToolBarButton toolBarButton)
                {
                    Remove(toolBarButton);
                }
            }

            /// <summary>
            ///  Returns an enumerator that can be used to iterate
            ///  through the toolbar button collection.
            /// </summary>
            public IEnumerator GetEnumerator() => _owner._buttons.GetEnumerator();
        }
    }
}
