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
        ///  The collection of StatusBarPanels that the StatusBar manages.
        ///  event.
        /// </summary>
        [ListBindable(false)]
        public class StatusBarPanelCollection : IList
        {
            private readonly StatusBar _owner;

            // A caching mechanism for key accessor
            // We use an index here rather than control so that we don't have lifetime
            // issues by holding on to extra references.
            private int _lastAccessedIndex = -1;

            /// <summary>
            ///  Constructor for the StatusBarPanelCollection class
            /// </summary>
            public StatusBarPanelCollection(StatusBar owner)
            {
                ArgumentNullException.ThrowIfNull(owner);
                _owner = owner;
            }

            /// <summary>
            ///  This method will return an individual StatusBarPanel with the appropriate index.
            /// </summary>
            public virtual StatusBarPanel this[int index]
            {
                get => _owner._panels[index];
                set
                {
                    ArgumentNullException.ThrowIfNull(value);

                    _owner._layoutDirty = true;

                    if (value.Parent is not null)
                    {
                        throw new ArgumentException(SR.ObjectHasParent, nameof(value));
                    }

                    int length = _owner._panels.Count;

                    if (index < 0 || index >= length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                    }

                    StatusBarPanel oldPanel = _owner._panels[index];
                    oldPanel.Parent = null;
                    value.Parent = _owner;
                    if (value.AutoSize == StatusBarPanelAutoSize.Contents)
                    {
                        value.Width = value.GetContentsWidth(true);
                    }
                    _owner._panels[index] = value;
                    value.Index = index;

                    if (_owner.ArePanelsRealized())
                    {
                        _owner.PerformLayout();
                        value.Realize();
                    }
                }
            }

            object? IList.this[int index]
            {
                get => this[index];
                set
                {
                    if (value is StatusBarPanel statusBarPanel)
                    {
                        this[index] = statusBarPanel;
                    }
                    else
                    {
                        throw new ArgumentException(SR.StatusBarBadStatusBarPanel, nameof(value));
                    }
                }
            }

            /// <summary>
            ///  Retrieves the child control with the specified key.
            /// </summary>
            public virtual StatusBarPanel? this[string key]
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
            ///  Returns an integer representing the number of StatusBarPanels
            ///  in this collection.
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            public int Count => _owner._panels.Count;

            object ICollection.SyncRoot => this;

            bool ICollection.IsSynchronized => false;

            bool IList.IsFixedSize => false;

            public bool IsReadOnly => false;

            /// <summary>
            ///  Adds a StatusBarPanel to the collection.
            /// </summary>
            public virtual StatusBarPanel Add(string text)
            {
                StatusBarPanel panel = new StatusBarPanel();
                panel.Text = text;
                Add(panel);
                return panel;
            }

            /// <summary>
            ///  Adds a StatusBarPanel to the collection.
            /// </summary>
            public virtual int Add(StatusBarPanel value)
            {
                int index = _owner._panels.Count;
                Insert(index, value);
                return index;
            }

            int IList.Add(object? value)
            {
                if (value is StatusBarPanel statusBarPanel)
                {
                    return Add(statusBarPanel);
                }
                else
                {
                    throw new ArgumentException(SR.StatusBarBadStatusBarPanel, nameof(value));
                }
            }

            public virtual void AddRange(StatusBarPanel[] panels)
            {
                ArgumentNullException.ThrowIfNull(panels);
                foreach (StatusBarPanel panel in panels)
                {
                    Add(panel);
                }
            }

            public bool Contains(StatusBarPanel panel)
                => IndexOf(panel) != -1;

            bool IList.Contains(object? panel)
            {
                if (panel is StatusBarPanel statusBarPanel)
                {
                    return Contains(statusBarPanel);
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

            public int IndexOf(StatusBarPanel panel)
            {
                for (int index = 0; index < Count; ++index)
                {
                    if (this[index] == panel)
                    {
                        return index;
                    }
                }
                return -1;
            }

            int IList.IndexOf(object? panel)
            {
                if (panel is StatusBarPanel statusBarPanel)
                {
                    return IndexOf(statusBarPanel);
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

            /// <summary>
            ///  Inserts a StatusBarPanel in the collection.
            /// </summary>
            public virtual void Insert(int index, StatusBarPanel value)
            {
                //check for the value not to be null
                ArgumentNullException.ThrowIfNull(value);
                //end check

                _owner._layoutDirty = true;
                if (value.Parent != _owner && value.Parent is not null)
                {
                    throw new ArgumentException(SR.ObjectHasParent, nameof(value));
                }

                int length = _owner._panels.Count;

                if (index < 0 || index > length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                }

                value.Parent = _owner;

                switch (value.AutoSize)
                {
                    case StatusBarPanelAutoSize.None:
                    case StatusBarPanelAutoSize.Spring:
                        break;
                    case StatusBarPanelAutoSize.Contents:
                        value.Width = value.GetContentsWidth(true);
                        break;
                }

                _owner._panels.Insert(index, value);
                _owner.UpdatePanelIndex();

                _owner.ForcePanelUpdate();
            }

            void IList.Insert(int index, object? value)
            {
                if (value is StatusBarPanel statusBarPanel)
                {
                    Insert(index, statusBarPanel);
                }
                else
                {
                    throw new ArgumentException(SR.StatusBarBadStatusBarPanel, nameof(value));
                }
            }

            /// <summary>
            ///  Determines if the index is valid for the collection.
            /// </summary>
            private bool IsValidIndex(int index)
                => ((index >= 0) && (index < Count));

            /// <summary>
            ///  Removes all the StatusBarPanels in the collection.
            /// </summary>
            public virtual void Clear()
            {
                _owner.RemoveAllPanelsWithoutUpdate();
                _owner.PerformLayout();
            }

            /// <summary>
            ///  Removes an individual StatusBarPanel in the collection.
            /// </summary>
            public virtual void Remove(StatusBarPanel value)
            {
                //check for the value not to be null
                ArgumentNullException.ThrowIfNull(value);
                //end check

                if (value.Parent != _owner)
                {
                    return;
                }
                RemoveAt(value.Index);
            }

            void IList.Remove(object? value)
            {
                if (value is StatusBarPanel statusBarPanel)
                {
                    Remove(statusBarPanel);
                }
            }

            /// <summary>
            ///  Removes an individual StatusBarPanel in the collection at the given index.
            /// </summary>
            public virtual void RemoveAt(int index)
            {
                int length = Count;
                if (index < 0 || index >= length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                }

                // clear any tooltip
                //
                StatusBarPanel panel = _owner._panels[index];

                _owner._panels.RemoveAt(index);
                panel.Parent = null;

                // this will cause the panels tooltip to be removed since it's no longer a child
                // of this StatusBar.
                _owner.UpdateTooltip(panel);

                // We must reindex the panels after a removal...
                _owner.UpdatePanelIndex();
                _owner.ForcePanelUpdate();
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

            void ICollection.CopyTo(Array dest, int index)
                => ((ICollection)_owner._panels).CopyTo(dest, index);

            /// <summary>
            ///  Returns the Enumerator for this collection.
            /// </summary>
            public IEnumerator GetEnumerator() => _owner._panels.GetEnumerator();
        }
    }
}
