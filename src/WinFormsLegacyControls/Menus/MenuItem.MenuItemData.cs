// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    partial class MenuItem
    {
        internal sealed class MenuItemData : ICommandExecutor
        {
            internal MenuItem _baseItem;
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
                //AddItem(baseItem);
                Debug.Assert(baseItem._data is null);
                _firstItem = baseItem;
                _baseItem = baseItem;

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
    }
}

