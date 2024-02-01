// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using static Interop;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    /// <summary>
    ///  Stores the <see cref='StatusBar'/>
    ///  control panel's information.
    /// </summary>
    [
    ToolboxItem(false),
    DesignTimeVisible(false),
    DefaultProperty(nameof(Text))
    ]
    public class StatusBarPanel : Component, ISupportInitialize
    {
        private const int DEFAULTWIDTH = 100;
        private const int DEFAULTMINWIDTH = 10;
        private const int PANELTEXTINSET = 3;
        private const int PANELGAP = 2;

        private string? _text = string.Empty;
        private string _name = string.Empty;
        private string? _toolTipText = string.Empty;
        private Icon? _icon;

        private HorizontalAlignment _alignment = HorizontalAlignment.Left;
        private StatusBarPanelBorderStyle _borderStyle = StatusBarPanelBorderStyle.Sunken;
        private StatusBarPanelStyle _style = StatusBarPanelStyle.Text;

        // these are package scope so the parent can get at them.
        //
        private StatusBar? _parent;
        private int _width = DEFAULTWIDTH;
        private int _right;
        private int _minWidth = DEFAULTMINWIDTH;
        private int _index;
        private StatusBarPanelAutoSize _autoSize = StatusBarPanelAutoSize.None;

        private bool _initializing;

        private object? _userData;

        /// <summary>
        ///  Initializes a new default instance of the <see cref='StatusBarPanel'/> class.
        /// </summary>
        public StatusBarPanel()
        {
        }

        /// <summary>
        ///  Gets or sets the <see cref='Alignment'/>
        ///  property.
        /// </summary>
        [
        SRCategory(nameof(SR.CatAppearance)),
        DefaultValue(HorizontalAlignment.Left),
        Localizable(true),
        SRDescription(nameof(SR.StatusBarPanelAlignmentDescr))
        ]
        public HorizontalAlignment Alignment
        {
            get => _alignment;
            set
            {
                //valid values are 0x0 to 0x2
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)HorizontalAlignment.Left, (int)HorizontalAlignment.Center))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(HorizontalAlignment));
                }
                if (_alignment != value)
                {
                    _alignment = value;
                    Realize();
                }
            }
        }

        /// <summary>
        ///  Gets or sets the <see cref='AutoSize'/>
        ///  property.
        /// </summary>
        [
        SRCategory(nameof(SR.CatAppearance)),
        DefaultValue(StatusBarPanelAutoSize.None),
        RefreshProperties(RefreshProperties.All),
        SRDescription(nameof(SR.StatusBarPanelAutoSizeDescr))
        ]
        public StatusBarPanelAutoSize AutoSize
        {
            get => _autoSize;
            set
            {
                //valid values are 0x1 to 0x3
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)StatusBarPanelAutoSize.None, (int)StatusBarPanelAutoSize.Contents))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(StatusBarPanelAutoSize));
                }
                if (_autoSize != value)
                {
                    _autoSize = value;
                    UpdateSize();
                }
            }
        }

        /// <summary>
        ///  Gets or sets the <see cref='BorderStyle'/> property.
        /// </summary>
        [SRCategory(nameof(SR.CatAppearance))]
        [DefaultValue(StatusBarPanelBorderStyle.Sunken)]
        [DispId(PInvoke.DISPID_BORDERSTYLE)]
        [SRDescription(nameof(SR.StatusBarPanelBorderStyleDescr))]
        public StatusBarPanelBorderStyle BorderStyle
        {
            get => _borderStyle;
            set
            {
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)StatusBarPanelBorderStyle.None, (int)StatusBarPanelBorderStyle.Sunken))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(StatusBarPanelBorderStyle));
                }

                if (_borderStyle != value)
                {
                    _borderStyle = value;
                    Realize();
                    if (Created)
                    {
                        _parent.Invalidate();
                    }
                }
            }
        }

        [MemberNotNullWhen(true, nameof(_parent))]
        private bool Created => _parent is not null && _parent.ArePanelsRealized();

        /// <summary>
        ///  Gets or sets the <see cref='Icon'/>
        ///  property.
        /// </summary>
        [
        SRCategory(nameof(SR.CatAppearance)),
        DefaultValue(null),
        Localizable(true),
        SRDescription(nameof(SR.StatusBarPanelIconDescr))
        ]
        public Icon? Icon
        {
            // unfortunately we have no way of getting the icon from the control.
            get => _icon;
            set
            {
                if (value is null)
                {
                    _icon = null;
                }
                else
                {
                    Size smallIconSize = SystemInformation.SmallIconSize;
                    if (value.Height > smallIconSize.Height || value.Width > smallIconSize.Width)
                    {
                        _icon = new Icon(value, smallIconSize);
                    }
                    else
                    {
                        _icon = value;
                    }
                }

                if (Created)
                {
                    IntPtr handle = (_icon is null) ? IntPtr.Zero : _icon.Handle;
                    PInvoke.SendMessage(_parent, PInvoke.SB_SETICON, (WPARAM)GetIndex(), handle);
                }
                UpdateSize();
                if (Created)
                {
                    _parent.Invalidate();
                }
            }
        }

        /// <summary>
        ///  Expose index internally
        /// </summary>
        internal int Index
        {
            get => _index;
            set => _index = value;
        }
        /// <summary>
        ///  Gets or sets the minimum width the <see cref='StatusBarPanel'/> can be within the <see cref='StatusBar'/>
        ///  control.
        /// </summary>
        [
        SRCategory(nameof(SR.CatBehavior)),
        DefaultValue(DEFAULTMINWIDTH),
        Localizable(true),
        RefreshProperties(RefreshProperties.All),
        SRDescription(nameof(SR.StatusBarPanelMinWidthDescr))
        ]
        public int MinWidth
        {
            get => _minWidth;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(SR.InvalidLowBoundArgumentEx, nameof(MinWidth), value, 0));
                }

                if (value != _minWidth)
                {
                    _minWidth = value;

                    UpdateSize();
                    if (_minWidth > Width)
                    {
                        Width = value;
                    }
                }
            }
        }

        /// <summary>
        ///  Gets or sets the name of the panel.
        /// </summary>
        [
            SRCategory(nameof(SR.CatAppearance)),
            Localizable(true),
            SRDescription(nameof(SR.StatusBarPanelNameDescr))
            ]
        public string Name
        {
            get => WindowsFormsUtils.GetComponentName(this, _name);
            set
            {
                _name = value;
                if (Site is not null)
                {
                    Site.Name = _name;
                }
            }
        }

        /// <summary>
        ///  Represents the <see cref='StatusBar'/>
        ///  control which hosts the
        ///  panel.
        /// </summary>
        [Browsable(false)]
        public StatusBar? Parent
        {
            get => _parent;
            // Expose a direct setter for parent internally
            internal set => _parent = value;
        }

        /// <summary>
        ///  Expose right internally
        /// </summary>
        internal int Right
        {
            get => _right;
            set => _right = value;
        }

        /// <summary>
        ///  Gets or sets the style of the panel.
        /// </summary>
        [
        SRCategory(nameof(SR.CatAppearance)),
        DefaultValue(StatusBarPanelStyle.Text),
        SRDescription(nameof(SR.StatusBarPanelStyleDescr))
        ]
        public StatusBarPanelStyle Style
        {
            get => _style;
            set
            {
                //valid values are 0x1 to 0x2
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)StatusBarPanelStyle.Text, (int)StatusBarPanelStyle.OwnerDraw))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(StatusBarPanelStyle));
                }
                if (_style != value)
                {
                    _style = value;
                    Realize();
                    if (Created)
                    {
                        _parent.Invalidate();
                    }
                }
            }
        }

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
            get => _userData;
            set => _userData = value;
        }

        /// <summary>
        ///  Gets or sets the text of the panel.
        /// </summary>
        [
        SRCategory(nameof(SR.CatAppearance)),
        Localizable(true),
        DefaultValue(""),
        SRDescription(nameof(SR.StatusBarPanelTextDescr))
        ]
        public string Text
        {
            get => _text ?? string.Empty;
            set
            {
                value ??= string.Empty;

                if (!Text.Equals(value))
                {
                    if (value.Length == 0)
                    {
                        _text = null;
                    }
                    else
                    {
                        _text = value;
                    }
                    Realize();
                    UpdateSize();
                }
            }
        }

        /// <summary>
        ///  Gets
        ///  or sets the panel's tool tip text.
        /// </summary>
        [
        SRCategory(nameof(SR.CatAppearance)),
        Localizable(true),
        DefaultValue(""),
        SRDescription(nameof(SR.StatusBarPanelToolTipTextDescr))
        ]
        public string ToolTipText
        {
            get => _toolTipText ?? string.Empty;
            set
            {
                value ??= string.Empty;

                if (!ToolTipText.Equals(value))
                {
                    if (value.Length == 0)
                    {
                        _toolTipText = null;
                    }
                    else
                    {
                        _toolTipText = value;
                    }

                    if (Created)
                    {
                        _parent.UpdateTooltip(this);
                    }
                }
            }
        }

        /// <summary>
        ///  Gets or sets the width of the <see cref='StatusBarPanel'/> within the <see cref='StatusBar'/>
        ///  control.
        /// </summary>
        [
        Localizable(true),
        SRCategory(nameof(SR.CatAppearance)),
        DefaultValue(DEFAULTWIDTH),
        SRDescription(nameof(SR.StatusBarPanelWidthDescr))
        ]
        public int Width
        {
            get => _width;
            set
            {
                if (!_initializing && value < _minWidth)
                {
                    throw new ArgumentOutOfRangeException(nameof(Width), SR.WidthGreaterThanMinWidth);
                }

                _width = value;
                UpdateSize();
            }
        }

        /// <summary>
        ///  Handles tasks required when the control is being initialized.
        /// </summary>
        public void BeginInit() => _initializing = true;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_parent is not null)
                {
                    int index = GetIndex();
                    if (index != -1)
                    {
                        _parent.Panels.RemoveAt(index);
                    }
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///  Called when initialization of the control is complete.
        /// </summary>
        public void EndInit()
        {
            _initializing = false;

            if (Width < MinWidth)
            {
                Width = MinWidth;
            }
        }

        /// <summary>
        ///  Gets the width of the contents of the panel
        /// </summary>
        internal int GetContentsWidth(bool newPanel)
        {
            string text;
            if (newPanel)
            {
                if (_text is null)
                {
                    text = string.Empty;
                }
                else
                {
                    text = _text;
                }
            }
            else
            {
                text = Text;
            }

            Size sz;
            using (Graphics g = _parent!.CreateGraphics/*Internal*/())
            {
                sz = Size.Ceiling(g.MeasureString(text, _parent.Font));
                //sz = TextRenderer.MeasureText(text, _parent.Font);
            }
            if (_icon is not null)
            {
                sz.Width += _icon.Size.Width + 5;
            }

            int width = sz.Width + SystemInformation.BorderSize.Width * 2 + PANELTEXTINSET * 2 + PANELGAP;
            return Math.Max(width, _minWidth);
        }

        /// <summary>
        ///  Returns the index of the panel by making the parent control search
        ///  for it within its list.
        /// </summary>
        private int GetIndex() => _index;

        /// <summary>
        ///  Sets all the properties for this panel.
        /// </summary>
        internal void Realize()
        {
            if (Created)
            {
                string sendText;
                uint border = 0;

                string text = _text ?? string.Empty;

                HorizontalAlignment align = _alignment;
                // Translate the alignment for Rtl apps
                //
                if (!_parent.RightToLeftLayout && _parent.RightToLeft == RightToLeft.Yes)
                {
                    switch (align)
                    {
                        case HorizontalAlignment.Left:
                            align = HorizontalAlignment.Right;
                            break;
                        case HorizontalAlignment.Right:
                            align = HorizontalAlignment.Left;
                            break;
                    }
                }

                switch (align)
                {
                    case HorizontalAlignment.Center:
                        sendText = "\t" + text;
                        break;
                    case HorizontalAlignment.Right:
                        sendText = "\t\t" + text;
                        break;
                    default:
                        sendText = text;
                        break;
                }
                switch (_borderStyle)
                {
                    case StatusBarPanelBorderStyle.None:
                        border |= PInvoke.SBT_NOBORDERS;
                        break;
                    case StatusBarPanelBorderStyle.Sunken:
                        break;
                    case StatusBarPanelBorderStyle.Raised:
                        border |= PInvoke.SBT_POPOUT;
                        break;
                }
                switch (_style)
                {
                    case StatusBarPanelStyle.Text:
                        break;
                    case StatusBarPanelStyle.OwnerDraw:
                        border |= PInvoke.SBT_OWNERDRAW;
                        break;
                }

                int wparam = GetIndex() | (int)border;
                if (_parent.RightToLeft == RightToLeft.Yes)
                {
                    wparam |= (int)PInvoke.SBT_RTLREADING;
                }

                int result = (int)PInvoke.SendMessage(_parent, PInvoke.SB_SETTEXT, (WPARAM)wparam, sendText);

                if (result == 0)
                {
                    throw new InvalidOperationException(SR.UnableToSetPanelText);
                }

                if (_icon is not null && _style != StatusBarPanelStyle.OwnerDraw)
                {
                    PInvoke.SendMessage(_parent, PInvoke.SB_SETICON, (WPARAM)GetIndex(), _icon.Handle);
                }
                else
                {
                    PInvoke.SendMessage(_parent, PInvoke.SB_SETICON, (WPARAM)GetIndex(), 0);
                }

                if (_style == StatusBarPanelStyle.OwnerDraw)
                {
                    RECT rect = new RECT();
                    result = (int)PInvoke.SendMessage(_parent, PInvoke.SB_GETRECT, (WPARAM)GetIndex(), ref rect);

                    if (result != 0)
                    {
                        _parent.Invalidate(Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom));
                    }
                }
            }
        }

        private void UpdateSize()
        {
            if (_autoSize == StatusBarPanelAutoSize.Contents)
            {
                ApplyContentSizing();
            }
            else
            {
                if (Created)
                {
                    _parent.DirtyLayout();
                    _parent.PerformLayout();
                }
            }
        }

        private void ApplyContentSizing()
        {
            if (_autoSize == StatusBarPanelAutoSize.Contents &&
                _parent is not null)
            {
                int newWidth = GetContentsWidth(false);
                if (newWidth != Width)
                {
                    Width = newWidth;
                    if (Created)
                    {
                        _parent.DirtyLayout();
                        _parent.PerformLayout();
                    }
                }
            }
        }

        /// <summary>
        ///  Retrieves a string that contains information about the
        ///  panel.
        /// </summary>
        public override string ToString()
            => $"{nameof(StatusBarPanel)}: {{{Text}}}";
    }
}
