﻿// Licensed to the .NET Foundation under one or more agreements.
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
    /// <summary>
    ///  Represents a Windows toolbar.
    /// </summary>
    //[ComVisible(true)]
    //[ClassInterface(ClassInterfaceType.AutoDispatch)]
    [DefaultEvent(nameof(ButtonClick))]
    //[Designer("System.Windows.Forms.Design.ToolBarDesigner, " + AssemblyRef.SystemDesign)]
    [DefaultProperty(nameof(Buttons))]
    public partial class ToolBar : Control
    {
        private readonly ToolBarButtonCollection _buttonsCollection;

        /// <summary>
        ///  The size of a button in the ToolBar
        /// </summary>
        internal Size _buttonSize = Size.Empty;

        /// <summary>
        ///  This is used by our autoSizing support.
        /// </summary>
        private int _requestedSize;
        /// <summary>
        ///  This represents the width of the drop down arrow we have if the
        ///  DropDownArrows property is true.  this value is used by the ToolBarButton
        ///  objects to compute their size
        /// </summary>
        internal const int DDARROW_WIDTH = 15;

        /// <summary>
        ///  Indicates what our appearance will be.  This will either be normal
        ///  or flat.
        /// </summary>
        private ToolBarAppearance _appearance = ToolBarAppearance.Normal;

        /// <summary>
        ///  Indicates whether or not we have a border
        /// </summary>
        private BorderStyle _borderStyle = BorderStyle.None;

        /// <summary>
        ///  The array of buttons we're working with.
        /// </summary>
        private readonly List<ToolBarButton> _buttons = new();

        /// <summary>
        ///  Indicates if text captions should go underneath images in buttons or
        ///  to the right of them
        /// </summary>
        private ToolBarTextAlign _textAlign = ToolBarTextAlign.Underneath;

        /// <summary>
        ///  The ImageList object that contains the main images for our control.
        /// </summary>
        private ImageList? _imageList;

        /// <summary>
        ///  The maximum width of buttons currently being displayed.  This is needed
        ///  by our autoSizing code.  If this value is -1, it needs to be recomputed.
        /// </summary>
        private int _maxWidth = -1;
        private int _hotItem = -1;

        // Track the current scale factor so we can scale our buttons
        private float _currentScaleDX = 1.0F;
        private float _currentScaleDY = 1.0F;

#pragma warning disable IDE0055 // Fix formatting
        private const int TOOLBARSTATE_wrappable        = 0x00000001;
        private const int TOOLBARSTATE_dropDownArrows   = 0x00000002;
        private const int TOOLBARSTATE_divider          = 0x00000004;
        private const int TOOLBARSTATE_showToolTips     = 0x00000008;
        private const int TOOLBARSTATE_autoSize         = 0x00000010;
        private const int TOOLBARSTATE_rtllayout        = 0x00000020;
#pragma warning restore IDE0055

        // PERF: take all the bools and put them into a state variable
        private BitVector32 _toolBarState; // see TOOLBARSTATE_ consts above

        // event handlers
        //
        private static readonly object s_buttonClickEvent = new();
        private static readonly object s_buttonDropDownEvent = new();

        /// <summary>
        ///  Initializes a new instance of the <see cref='ToolBar'/> class.
        /// </summary>
        public ToolBar()
        {
            // Set this BEFORE calling any other methods so that these defaults will be propagated
            _toolBarState = new BitVector32(TOOLBARSTATE_autoSize |
                                            TOOLBARSTATE_showToolTips |
                                            TOOLBARSTATE_divider |
                                            TOOLBARSTATE_dropDownArrows |
                                            TOOLBARSTATE_wrappable);

            SetStyle(ControlStyles.UserPaint, false);
            SetStyle(ControlStyles.FixedHeight, AutoSize);
            SetStyle(ControlStyles.FixedWidth, false);
            TabStop = false;
            Dock = DockStyle.Top;
            _buttonsCollection = new ToolBarButtonCollection(this);
        }

        /// <summary>
        ///  Gets or sets the appearance of the toolbar
        ///  control and its buttons.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [DefaultValue(ToolBarAppearance.Normal)]
        [Localizable(true)]
        [SRDescription(nameof(SR.ToolBarAppearanceDescr))]
        public ToolBarAppearance Appearance
        {
            get => _appearance;
            set
            {
                //valid values are 0x0 to 0x1
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)ToolBarAppearance.Normal, (int)ToolBarAppearance.Flat))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ToolBarAppearance));
                }

                if (value != _appearance)
                {
                    _appearance = value;
                    RecreateHandle();
                }
            }
        }

        /// <summary>
        ///  Indicates whether the toolbar
        ///  adjusts its size automatically based on the size of the buttons and the
        ///  dock style.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [DefaultValue(true)]
        [Localizable(true)]
        [SRDescription(nameof(SR.ToolBarAutoSizeDescr))]
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override bool AutoSize
        {
            get => _toolBarState[TOOLBARSTATE_autoSize];
            set
            {
                // Note that we intentionally do not call base.  Toolbars size themselves by
                // overriding SetBoundsCore (old RTM code).  We let CommonProperties.GetAutoSize
                // continue to return false to keep our LayoutEngines from messing with TextBoxes.
                // This is done for backwards compatibility since the new AutoSize behavior differs.
                if (AutoSize != value)
                {
                    _toolBarState[TOOLBARSTATE_autoSize] = value;
                    if (Dock is DockStyle.Left or DockStyle.Right)
                    {
                        SetStyle(ControlStyles.FixedWidth, AutoSize);
                        SetStyle(ControlStyles.FixedHeight, false);
                    }
                    else
                    {
                        SetStyle(ControlStyles.FixedHeight, AutoSize);
                        SetStyle(ControlStyles.FixedWidth, false);
                    }
                    AdjustSize(Dock);
                    OnAutoSizeChanged(EventArgs.Empty);
                }
            }
        }

        [SRCategory(nameof(SR.CatPropertyChanged))]
        [SRDescription(nameof(SR.ControlOnAutoSizeChangedDescr))]
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public new event EventHandler AutoSizeChanged
        {
            add => base.AutoSizeChanged += value;
            remove => base.AutoSizeChanged -= value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Color BackColor
        {
            get => base.BackColor;
            set => base.BackColor = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler BackColorChanged
        {
            add => base.BackColorChanged += value;
            remove => base.BackColorChanged -= value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Image? BackgroundImage
        {
            get => base.BackgroundImage;
            set => base.BackgroundImage = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler BackgroundImageChanged
        {
            add => base.BackgroundImageChanged += value;
            remove => base.BackgroundImageChanged -= value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override ImageLayout BackgroundImageLayout
        {
            get => base.BackgroundImageLayout;
            set => base.BackgroundImageLayout = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler BackgroundImageLayoutChanged
        {
            add => base.BackgroundImageLayoutChanged += value;
            remove => base.BackgroundImageLayoutChanged -= value;
        }

        /// <summary>
        ///  Gets or sets
        ///  the border style of the toolbar control.
        /// </summary>
        [SRCategory(nameof(SR.CatAppearance))]
        [DefaultValue(BorderStyle.None)]
        [DispId(PInvoke.DISPID_BORDERSTYLE)]
        [SRDescription(nameof(SR.ToolBarBorderStyleDescr))]
        public BorderStyle BorderStyle
        {
            get => _borderStyle;
            set
            {
                //valid values are 0x0 to 0x2
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)BorderStyle.None, (int)BorderStyle.Fixed3D))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(BorderStyle));
                }

                if (_borderStyle != value)
                {
                    _borderStyle = value;

                    //UpdateStyles();
                    RecreateHandle();   // Looks like we need to recreate the handle to avoid painting glitches
                }
            }
        }

        /// <summary>
        ///  A collection of <see cref='ToolBarButton'/> controls assigned to the
        ///  toolbar control. The property is read-only.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Localizable(true)]
        [SRDescription(nameof(SR.ToolBarButtonsDescr))]
        [MergableProperty(false)]
        public ToolBarButtonCollection Buttons => _buttonsCollection;

        /// <summary>
        ///  Gets or sets
        ///  the size of the buttons on the toolbar control.
        /// </summary>
        [SRCategory(nameof(SR.CatAppearance))]
        [RefreshProperties(RefreshProperties.All)]
        [Localizable(true)]
        [SRDescription(nameof(SR.ToolBarButtonSizeDescr))]
        public Size ButtonSize
        {
            get
            {
                if (_buttonSize.IsEmpty)
                {

                    // Obtain the current buttonsize of the first button from the winctl control
                    //
                    if (IsHandleCreated && _buttons.Count > 0)
                    {
                        LRESULT result = PInvoke.SendMessage(this, PInvoke.TB_GETBUTTONSIZE);
                        if (result != 0)
                            return new Size(result.LOWORD, result.HIWORD);
                    }
                    if (TextAlign == ToolBarTextAlign.Underneath)
                    {
                        return new Size(39, 36);    // Default button size
                    }
                    else
                    {
                        return new Size(23, 22);    // Default button size
                    }
                }
                else
                {
                    return _buttonSize;
                }
            }

            set
            {

                if (value.Width < 0 || value.Height < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(SR.InvalidArgument, nameof(ButtonSize), value));
                }

                if (_buttonSize != value)
                {
                    _buttonSize = value;
                    _maxWidth = -1; // Force recompute of maxWidth
                    RecreateHandle();
                    AdjustSize(Dock);
                }
            }
        }

        /// <summary>
        ///  Returns the parameters needed to create the handle.  Inheriting classes
        ///  can override this to provide extra functionality.  They should not,
        ///  however, forget to get base.CreateParams first to get the struct
        ///  filled up with the basic info.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassName = PInvoke.TOOLBARCLASSNAME;

                // windows forms has it's own docking code.
                //
                cp.Style |= PInvoke.CCS_NOPARENTALIGN | PInvoke.CCS_NORESIZE;
                // | NativeMethods.WS_CHILD was commented out since setTopLevel should be able to work.

                if (!Divider)
                {
                    cp.Style |= PInvoke.CCS_NODIVIDER;
                }

                if (Wrappable)
                {
                    cp.Style |= (int)PInvoke.TBSTYLE_WRAPABLE;
                }

                if (ShowToolTips && !DesignMode)
                {
                    cp.Style |= (int)PInvoke.TBSTYLE_TOOLTIPS;
                }

                cp.ExStyle &= (~(int)WINDOW_EX_STYLE.WS_EX_CLIENTEDGE);
                cp.Style &= (~(int)WINDOW_STYLE.WS_BORDER);
                switch (_borderStyle)
                {
                    case BorderStyle.Fixed3D:
                        cp.ExStyle |= (int)WINDOW_EX_STYLE.WS_EX_CLIENTEDGE;
                        break;
                    case BorderStyle.FixedSingle:
                        cp.Style |= (int)WINDOW_STYLE.WS_BORDER;
                        break;
                }

                switch (_appearance)
                {
                    case ToolBarAppearance.Normal:
                        break;
                    case ToolBarAppearance.Flat:
                        cp.Style |= (int)PInvoke.TBSTYLE_FLAT;
                        break;
                }

                switch (_textAlign)
                {
                    case ToolBarTextAlign.Underneath:
                        break;
                    case ToolBarTextAlign.Right:
                        cp.Style |= (int)PInvoke.TBSTYLE_LIST;
                        break;
                }

                // [spec]
                if (RightToLeftLayout && (cp.ExStyle & (int)WINDOW_EX_STYLE.WS_EX_RTLREADING) == (int)WINDOW_EX_STYLE.WS_EX_RTLREADING)
                {
                    cp.ExStyle |= (int)WINDOW_EX_STYLE.WS_EX_LAYOUTRTL;
                    cp.ExStyle &= ~(int)(WINDOW_EX_STYLE.WS_EX_RTLREADING | WINDOW_EX_STYLE.WS_EX_RIGHT | WINDOW_EX_STYLE.WS_EX_LEFTSCROLLBAR);
                }

                return cp;
            }
        }

        protected override ImeMode DefaultImeMode => ImeMode.Disable;

        /// <summary>
        ///  Deriving classes can override this to configure a default size for their control.
        ///  This is more efficient than setting the size in the control's constructor.
        /// </summary>
        protected override Size DefaultSize => new Size(100, 22);

        /// <summary>
        ///  Gets or sets a value indicating
        ///  whether the toolbar displays a divider.
        /// </summary>
        [SRCategory(nameof(SR.CatAppearance))]
        [DefaultValue(true)]
        [SRDescription(nameof(SR.ToolBarDividerDescr))]
        public bool Divider
        {
            get => _toolBarState[TOOLBARSTATE_divider];
            set
            {
                if (Divider != value)
                {

                    _toolBarState[TOOLBARSTATE_divider] = value;
                    RecreateHandle();
                }
            }
        }

        /// <summary>
        ///  Sets the way in which this ToolBar is docked to its parent. We need to
        ///  override this to ensure autoSizing works correctly
        /// </summary>
        [Localizable(true)]
        [DefaultValue(DockStyle.Top)]
        public override DockStyle Dock
        {
            get => base.Dock;
            set
            {
                //valid values are 0x0 to 0x5
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)DockStyle.None, (int)DockStyle.Fill))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(DockStyle));
                }

                if (Dock != value)
                {
                    if (value is DockStyle.Left or DockStyle.Right)
                    {
                        SetStyle(ControlStyles.FixedWidth, AutoSize);
                        SetStyle(ControlStyles.FixedHeight, false);
                    }
                    else
                    {
                        SetStyle(ControlStyles.FixedHeight, AutoSize);
                        SetStyle(ControlStyles.FixedWidth, false);
                    }
                    AdjustSize(value);
                    base.Dock = value;
                }
            }
        }

        /// <summary>
        ///  This property is overridden and hidden from statement completion
        ///  on controls that are based on Win32 Native Controls.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override bool DoubleBuffered
        {
            get => base.DoubleBuffered;
            set => base.DoubleBuffered = value;
        }

        /// <summary>
        ///  Gets or sets a value indicating whether drop-down buttons on a
        ///  toolbar display down arrows.
        /// </summary>
        [DefaultValue(false)]
        [SRCategory(nameof(SR.CatAppearance))]
        [Localizable(true)]
        [SRDescription(nameof(SR.ToolBarDropDownArrowsDescr))]
        public bool DropDownArrows
        {
            get => _toolBarState[TOOLBARSTATE_dropDownArrows];
            set
            {

                if (DropDownArrows != value)
                {
                    _toolBarState[TOOLBARSTATE_dropDownArrows] = value;
                    RecreateHandle();
                }
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Color ForeColor
        {
            get => base.ForeColor;
            set => base.ForeColor = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler ForeColorChanged
        {
            add => base.ForeColorChanged += value;
            remove => base.ForeColorChanged -= value;
        }

        /// <summary>
        ///  Gets or sets the collection of images available to the toolbar button
        ///  controls.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [DefaultValue(null)]
        [SRDescription(nameof(SR.ToolBarImageListDescr))]
        public ImageList? ImageList
        {
            get => _imageList;
            set
            {
                if (value != _imageList)
                {
                    EventHandler recreateHandler = new EventHandler(ImageListRecreateHandle);
                    EventHandler disposedHandler = new EventHandler(DetachImageList);

                    if (_imageList is not null)
                    {
                        _imageList.Disposed -= disposedHandler;
                        _imageList.RecreateHandle -= recreateHandler;
                    }

                    _imageList = value;

                    if (value is not null)
                    {
                        value.Disposed += disposedHandler;
                        value.RecreateHandle += recreateHandler;
                    }

                    if (IsHandleCreated)
                    {
                        RecreateHandle();
                    }
                }
            }
        }

        /// <summary>
        ///  Gets the size of the images in the image list assigned to the
        ///  toolbar.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [SRDescription(nameof(SR.ToolBarImageSizeDescr))]
        public Size ImageSize => _imageList?.ImageSize ?? new Size(0, 0);

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new ImeMode ImeMode
        {
            get => base.ImeMode;
            set => base.ImeMode = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler ImeModeChanged
        {
            add => base.ImeModeChanged += value;
            remove => base.ImeModeChanged -= value;
        }

        /// <summary>
        ///  The preferred height for this ToolBar control.  This is
        ///  used by the AutoSizing code.
        /// </summary>
        private int PreferredHeight
        {
            get
            {
                int height;

                if (_buttons.Count == 0 || !IsHandleCreated)
                {
                    height = ButtonSize.Height;
                }
                else
                {
                    // get the first visible button and get it's height
                    //
                    RECT rect = new RECT();
                    int firstVisible;

                    for (firstVisible = 0; firstVisible < _buttons.Count; firstVisible++)
                    {
                        if (_buttons[firstVisible].Visible)
                        {
                            break;
                        }
                    }
                    if (firstVisible == _buttons.Count)
                    {
                        firstVisible = 0;
                    }

                    unsafe
                    {
                        PInvoke.SendMessage(this, PInvoke.TB_GETRECT, (WPARAM)firstVisible, &rect);
                    }

                    // height is the button's height plus some extra goo
                    //
                    height = rect.Height;
                    Debug.Assert(height == rect.bottom - rect.top);
                }

                // if the ToolBar is wrappable, and there is more than one row, make
                // sure the height is correctly adjusted
                //
                if (Wrappable && IsHandleCreated)
                {
                    height *= (int)PInvoke.SendMessage(this, PInvoke.TB_GETROWS);
                }

                height = (height > 0) ? height : 1;

                switch (_borderStyle)
                {
                    case BorderStyle.FixedSingle:
                        height += SystemInformation.BorderSize.Height;
                        break;
                    case BorderStyle.Fixed3D:
                        height += SystemInformation.Border3DSize.Height;
                        break;
                }

                if (Divider)
                {
                    height += 2;
                }

                height += 4;

                return height;
            }

        }

        /// <summary>
        ///  The preferred width for this ToolBar control.  This is
        ///  used by AutoSizing code.
        ///  NOTE!!!!!!!!! This function assumes it's only going to get called
        ///  if the control is docked left or right [ie, it really
        ///  just returns a max width]
        /// </summary>
        private int PreferredWidth
        {
            get
            {
                int width;

                // fortunately, we compute this value sometimes, so we can just
                // use it if we have it.
                //
                if (_maxWidth == -1)
                {
                    // don't have it, have to recompute
                    //
                    if (!IsHandleCreated || _buttons.Count == 0)
                    {
                        _maxWidth = ButtonSize.Width;
                    }
                    else
                    {

                        RECT rect = new RECT();

                        for (int x = 0; x < _buttons.Count; x++)
                        {
                            unsafe
                            {
                                // ? 0 -> x ?
                                PInvoke.SendMessage(this, PInvoke.TB_GETRECT, 0, &rect);
                            }
                            if (rect.Width > _maxWidth)
                            {
                                _maxWidth = rect.Width;
                                Debug.Assert(_maxWidth == rect.right - rect.left);
                            }
                        }
                    }
                }

                width = _maxWidth;

                if (_borderStyle != BorderStyle.None)
                {
                    width += SystemInformation.BorderSize.Height * 4 + 3;
                }

                return width;
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override RightToLeft RightToLeft
        {
            get => base.RightToLeft;
            set => base.RightToLeft = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler RightToLeftChanged
        {
            add => base.RightToLeftChanged += value;
            remove => base.RightToLeftChanged -= value;
        }

        // [spec]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool RightToLeftLayout
        {
            get => _toolBarState[TOOLBARSTATE_rtllayout];
            set
            {
                if (RightToLeftLayout != value)
                {
                    _toolBarState[TOOLBARSTATE_rtllayout] = value;
                    if (RightToLeft == RightToLeft.Yes)
                    {
                        RecreateHandle();
                    }
                }
            }
        }

        /// <summary>
        ///  We need to track the current scale factor so that we can tell the
        ///  unmanaged control how to scale its buttons.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void ScaleCore(float dx, float dy)
        {
            _currentScaleDX = dx;
            _currentScaleDY = dy;
            base.ScaleCore(dx, dy);
            UpdateButtons();
        }

        /// <summary>
        ///  We need to track the current scale factor so that we can tell the
        ///  unmanaged control how to scale its buttons.
        /// </summary>
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            _currentScaleDX = factor.Width;
            _currentScaleDY = factor.Height;
            base.ScaleControl(factor, specified);
        }

        /// <summary>
        ///  Gets or sets a value indicating whether the toolbar displays a
        ///  tool tip for each button.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [DefaultValue(false)]
        [Localizable(true)]
        [SRDescription(nameof(SR.ToolBarShowToolTipsDescr))]
        public bool ShowToolTips
        {
            get => _toolBarState[TOOLBARSTATE_showToolTips];
            set
            {
                if (ShowToolTips != value)
                {

                    _toolBarState[TOOLBARSTATE_showToolTips] = value;
                    RecreateHandle();
                }
            }
        }

        [DefaultValue(false)]
        public new bool TabStop
        {
            get => base.TabStop;
            set => base.TabStop = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [AllowNull]
        public override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler TextChanged
        {
            add => base.TextChanged += value;
            remove => base.TextChanged -= value;
        }

        /// <summary>
        ///  Gets or sets the alignment of text in relation to each
        ///  image displayed on
        ///  the toolbar button controls.
        /// </summary>
        [SRCategory(nameof(SR.CatAppearance))]
        [DefaultValue(ToolBarTextAlign.Underneath)]
        [Localizable(true)]
        [SRDescription(nameof(SR.ToolBarTextAlignDescr))]
        public ToolBarTextAlign TextAlign
        {
            get => _textAlign;
            set
            {
                //valid values are 0x0 to 0x1
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)ToolBarTextAlign.Underneath, (int)ToolBarTextAlign.Right))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ToolBarTextAlign));
                }

                if (_textAlign == value)
                {
                    return;
                }

                _textAlign = value;
                RecreateHandle();
            }
        }

        /// <summary>
        ///  Gets
        ///  or sets a value
        ///  indicating whether the toolbar buttons wrap to the next line if the
        ///  toolbar becomes too small to display all the buttons
        ///  on the same line.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [DefaultValue(true)]
        [Localizable(true)]
        [SRDescription(nameof(SR.ToolBarWrappableDescr))]
        public bool Wrappable
        {
            get => _toolBarState[TOOLBARSTATE_wrappable];
            set
            {
                if (Wrappable != value)
                {
                    _toolBarState[TOOLBARSTATE_wrappable] = value;
                    RecreateHandle();
                }
            }
        }

        /// <summary>
        ///  Occurs when a <see cref='ToolBarButton'/> on the <see cref='ToolBar'/> is clicked.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [SRDescription(nameof(SR.ToolBarButtonClickDescr))]
        public event ToolBarButtonClickEventHandler ButtonClick
        {
            add => Events.AddHandler(s_buttonClickEvent, value);
            remove => Events.RemoveHandler(s_buttonClickEvent, value);
        }

        /// <summary>
        ///  Occurs when a drop-down style <see cref='ToolBarButton'/> or its down arrow is clicked.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [SRDescription(nameof(SR.ToolBarButtonDropDownDescr))]
        public event ToolBarButtonClickEventHandler ButtonDropDown
        {
            add => Events.AddHandler(s_buttonDropDownEvent, value);
            remove => Events.RemoveHandler(s_buttonDropDownEvent, value);
        }

        /// <summary>
        ///  ToolBar Onpaint.
        /// </summary>
        /// <hideinheritance/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event PaintEventHandler Paint
        {
            add => base.Paint += value;
            remove => base.Paint -= value;
        }

        /// <summary>
        ///  Adjusts the height or width of the ToolBar to make sure we have enough
        ///  room to show the buttons.
        /// </summary>
        // we pass in a value for dock rather than calling Dock ourselves
        // because we can't change Dock until the size has been properly adjusted.
        private void AdjustSize(DockStyle dock)
        {
            int saveSize = _requestedSize;
            try
            {
                if (dock is DockStyle.Left or DockStyle.Right)
                {
                    Width = AutoSize ? PreferredWidth : saveSize;
                }
                else
                {
                    Height = AutoSize ? PreferredHeight : saveSize;
                }
            }
            finally
            {
                _requestedSize = saveSize;
            }
        }

        /// <summary>
        ///  This routine lets us change a bunch of things about the toolbar without
        ///  having each operation wait for the paint to complete.  This must be
        ///  matched up with a call to endUpdate().
        /// </summary>
        private void BeginUpdate() => BeginUpdateInternal();

        protected override void CreateHandle()
        {
            if (!RecreatingHandle)
            {
                //using ThemingScope scope = new(Application.UseVisualStyles);
                unsafe
                {
                    PInvoke.InitCommonControlsEx(new INITCOMMONCONTROLSEX
                    {
                        dwSize = (uint)sizeof(INITCOMMONCONTROLSEX),
                        dwICC = INITCOMMONCONTROLSEX_ICC.ICC_BAR_CLASSES
                    });
                }
            }
            base.CreateHandle();
        }

        /// <summary>
        ///  Resets the imageList to null.  We wire this method up to the imageList's
        ///  Dispose event, so that we don't hang onto an imageList that's gone away.
        /// </summary>
        private void DetachImageList(object? sender, EventArgs e) => ImageList = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //
#if LOCK
                lock (this)
#endif
                {
                    if (_imageList is not null)
                    {
                        _imageList.Disposed -= new EventHandler(DetachImageList);
                        _imageList = null;
                    }

                    if (_buttons.Count > 0)
                    {
                        for (int i = 0; i < _buttons.Count; i++)
                        {
                            ToolBarButton b = _buttons[i];
                            // Since ToolBar is being disposed, there is no need to do TB_DELETEBUTTON
                            // from ToolBar.RemoveAt
                            b._parent = null;
                            b._stringIndex = -1;
                            b.Dispose();
                        }
                        // from ToolBarButtonCollection.Clear
                        _buttons.Clear();
                    }
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        ///  This routine lets us change a bunch of things about the toolbar without
        ///  having each operation wait for the paint to complete.  This must be
        ///  matched up with a call to beginUpdate().
        /// </summary>
        private void EndUpdate() => EndUpdateInternal();

        /// <summary>
        ///  Forces the button sizes based on various different things.  The default
        ///  ToolBar button sizing rules are pretty primitive and this tends to be
        ///  a little better, and lets us actually show things like DropDown Arrows
        ///  for ToolBars
        /// </summary>
        private void ForceButtonWidths()
        {
            if (_buttons.Count > 0 && _buttonSize.IsEmpty && IsHandleCreated)
            {

                // force ourselves to re-compute this each time
                //
                _maxWidth = -1;

                for (int x = 0; x < _buttons.Count; x++)
                {

                    TBBUTTONINFOW tbbi = default;
                    tbbi.cx = (ushort)_buttons[x].Width;

                    if (tbbi.cx > _maxWidth)
                    {
                        _maxWidth = tbbi.cx;
                    }

                    tbbi.dwMask = TBBUTTONINFOW_MASK.TBIF_SIZE;
                    unsafe
                    {
                        tbbi.cbSize = (uint)sizeof(TBBUTTONINFOW);
                        PInvoke.SendMessage(this, PInvoke.TB_SETBUTTONINFO, (WPARAM)x, &tbbi);
                    }
                }
            }
        }

        private void ImageListRecreateHandle(object? sender, EventArgs e)
        {
            if (IsHandleCreated)
            {
                RecreateHandle();
            }
        }

        private void Insert(int index, ToolBarButton button)
        {
            button._parent = this;
            _buttons.Insert(index, button);
        }

        /// <summary>
        ///  Inserts a button at a given location on the toolbar control.
        /// </summary>
        private void InsertButton(int index, ToolBarButton value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (index < 0 || index > _buttons.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
            }

            // insert the button into our local array, and then into the
            // real windows ToolBar control
            //
            Insert(index, value);
            /* Unnecessary because RecreateHandle is called in UpdateButtons
            if (IsHandleCreated)
            {
                NativeMethods.TBBUTTON tbbutton = new NativeMethods.TBBUTTON();
                value.GetTBBUTTON(index, ref tbbutton);
                PInvoke.SendMessage(this, PInvoke.TB_INSERTBUTTON, (WPARAM)index, ref tbbutton);
            }
            */
            UpdateButtons();
        }

        /// <summary>
        ///  Adds a button to the ToolBar
        /// </summary>
        private int InternalAddButton(ToolBarButton button)
        {
            ArgumentNullException.ThrowIfNull(button);

            int index = _buttons.Count;
            Insert(index, button);
            return index;
        }

        /// <summary>
        ///  Changes the data for a button in the ToolBar, and then does the appropriate
        ///  work to update the ToolBar control.
        /// </summary>
        internal void InternalSetButton(int index, ToolBarButton value, bool recreate, bool updateText)
        {
            // tragically, there doesn't appear to be a way to remove the
            // string for the button if it has one, so we just have to leave
            // it in there.
            //
            ToolBarButton oldButton = _buttons[index];
            oldButton._parent = null;
            oldButton._stringIndex = -1;
            _buttons[index] = value;
            value._parent = this;

            if (IsHandleCreated)
            {
                if (recreate)
                {
                    UpdateButtons();
                }
                else
                {
                    // Not required when recreating
                    value.SetButtonInfo(updateText, index);

                    // after doing anything with the comctl ToolBar control, this
                    // appears to be a good idea.
                    //
                    PInvoke.SendMessage(this, PInvoke.TB_AUTOSIZE);

                    ForceButtonWidths();
                    Invalidate();
                }
            }
        }

        /// <summary>
        ///  Raises the <see cref='ButtonClick'/>
        ///  event.
        /// </summary>
        protected virtual void OnButtonClick(ToolBarButtonClickEventArgs e)
        {
            ((ToolBarButtonClickEventHandler?)Events[s_buttonClickEvent])?.Invoke(this, e);

            // [Command]
            if (e.Button.Command is { } command)
            {
                object? commandParameter = e.Button.CommandParameter;
                if (command.CanExecute(commandParameter))
                {
                    command.Execute(commandParameter);
                }
            }
        }

        /// <summary>
        ///  Raises the <see cref='ButtonDropDown'/>
        ///  event.
        /// </summary>
        protected virtual void OnButtonDropDown(ToolBarButtonClickEventArgs e)
            => ((ToolBarButtonClickEventHandler?)Events[s_buttonDropDownEvent])?.Invoke(this, e);

        /// <summary>
        ///  Overridden from the control class so we can add all the buttons
        ///  and do whatever work needs to be done.
        ///  Don't forget to call base.OnHandleCreated.
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (_toolTip is not null)
            {
                nint handle = WinFormsLegacyControls.Migration.ToolTipAccessors.GetHandle(_toolTip);
                PInvoke.SendMessage(this, PInvoke.TB_SETTOOLTIPS, (WPARAM)handle, 0);
                GC.KeepAlive(_toolTip);
            }

            // we have to set the button struct size, because they don't.
            //
            unsafe
            {
                PInvoke.SendMessage(this, PInvoke.TB_BUTTONSTRUCTSIZE, (WPARAM)sizeof(NativeMethods.TBBUTTON));
            }

            // set up some extra goo
            //
            if (DropDownArrows)
            {
                PInvoke.SendMessage(this, PInvoke.TB_SETEXTENDEDSTYLE, 0, (LPARAM)PInvoke.TBSTYLE_EX_DRAWDDARROWS);
            }

            // if we have an imagelist, add it in now.
            //
            if (_imageList is not null)
            {
                PInvoke.SendMessage(this, PInvoke.TB_SETIMAGELIST, 0, _imageList.Handle);
            }

            RealizeButtons();

            // Force a repaint, as occasionally the ToolBar border does not paint properly
            // (comctl ToolBar is flaky)
            //
            BeginUpdate();
            try
            {
                Size size = Size;
                Size = new Size(size.Width + 1, size.Height);
                Size = size;
            }
            finally
            {
                EndUpdate();
            }
        }

        /// <summary>
        ///  The control is being resized. Make sure the width/height are correct.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Wrappable)
            {
                AdjustSize(Dock);
            }
        }

        /// <summary>
        ///  Overridden to ensure that the buttons and the control resize properly
        ///  whenever the font changes.
        /// </summary>
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (IsHandleCreated)
            {
                if (!_buttonSize.IsEmpty)
                {
                    SendToolbarButtonSizeMessage();
                }
                else
                {
                    AdjustSize(Dock);
                    ForceButtonWidths();
                }
            }
        }

        /// <summary>
        ///  Sets all the button data into the ToolBar control
        /// </summary>
        private void RealizeButtons()
        {
            int count = _buttons.Count;
            if (count > 0)
            {
                try
                {
                    BeginUpdate();
                    //  go and add in all the strings for all of our buttons
                    //
                    for (int x = 0; x < count; x++)
                    {
                        ToolBarButton button = _buttons[x];
                        if (button.Text.Length > 0)
                        {
                            string addString = button.Text + '\0'.ToString();
                            button._stringIndex = PInvoke.SendMessage(this, PInvoke.TB_ADDSTRING, 0, addString);
                        }
                        else
                        {
                            button._stringIndex = -1;
                        }
                    }

                    // insert the buttons and set their parent pointers
                    //
                    unsafe
                    {
                        NativeMethods.TBBUTTON* ptbbuttons = stackalloc NativeMethods.TBBUTTON[count];
#if DEBUG
                        Span<NativeMethods.TBBUTTON> span = new(ptbbuttons, count);
#endif

                        for (int x = 0; x < count; x++)
                        {
                            ToolBarButton button = _buttons[x];
                            button.GetTBBUTTON(x, ref ptbbuttons[x]);
                            button._parent = this;
                        }

                        PInvoke.SendMessage(this, PInvoke.TB_ADDBUTTONS, (WPARAM)count, ptbbuttons);
                    }

                    // after doing anything with the comctl ToolBar control, this
                    // appears to be a good idea.
                    //
                    PInvoke.SendMessage(this, PInvoke.TB_AUTOSIZE);

                    // The win32 ToolBar control is somewhat unpredictable here. We
                    // have to set the button size after we've created all the
                    // buttons.  Otherwise, we need to manually set the width of all
                    // the buttons so they look reasonable
                    //
                    if (!_buttonSize.IsEmpty)
                    {
                        SendToolbarButtonSizeMessage();
                    }
                    else
                    {
                        ForceButtonWidths();
                    }
                    AdjustSize(Dock);
                }
                finally
                {
                    EndUpdate();
                }
            }
        }

        private void RemoveAt(int index)
        {
            ToolBarButton oldButton = _buttons[index];
            oldButton._parent = null;
            oldButton._stringIndex = -1;

            _buttons.RemoveAt(index);
        }

        /// <summary>
        ///  Resets the toolbar buttons to the minimum
        ///  size.
        /// </summary>
        private void ResetButtonSize()
        {
            _buttonSize = Size.Empty;
            RecreateHandle();
        }

        ///  Sends a TB_SETBUTTONSIZE message to the unmanaged control, with size arguments properly scaled.
        private void SendToolbarButtonSizeMessage()
            => PInvoke.SendMessage(this, PInvoke.TB_SETBUTTONSIZE, 0, LPARAM.MAKELPARAM((int)(_buttonSize.Width * _currentScaleDX), (int)(_buttonSize.Height * _currentScaleDY)));

        /// <summary>
        ///  Overrides Control.setBoundsCore to enforce AutoSize.
        /// </summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            int originalHeight = height;
            int originalWidth = width;

            base.SetBoundsCore(x, y, width, height, specified);

            if (Dock is DockStyle.Left or DockStyle.Right)
            {
                if ((specified & BoundsSpecified.Width) != BoundsSpecified.None)
                {
                    _requestedSize = width;
                }

                if (AutoSize)
                {
                    width = PreferredWidth;
                }

                if (width != originalWidth && Dock == DockStyle.Right)
                {
                    int deltaWidth = originalWidth - width;
                    x += deltaWidth;
                }

            }
            else
            {
                if ((specified & BoundsSpecified.Height) != BoundsSpecified.None)
                {
                    _requestedSize = height;
                }

                if (AutoSize)
                {
                    height = PreferredHeight;
                }

                if (height != originalHeight && Dock == DockStyle.Bottom)
                {
                    int deltaHeight = originalHeight - height;
                    y += deltaHeight;
                }

            }

            base.SetBoundsCore(x, y, width, height, specified);
        }

        /// <summary>
        ///  Determines if the <see cref='ButtonSize'/> property needs to be persisted.
        /// </summary>
        private bool ShouldSerializeButtonSize() => !_buttonSize.IsEmpty;

        /*
        /// <summary>
        ///  Called by ToolTip to poke in that Tooltip into this ComCtl so that the Native ChildToolTip is not exposed.
        /// </summary>
        internal void SetToolTip(ToolTip toolTip)
        {
            UnsafeNativeMethods.SendMessage(new HandleRef(this, Handle), NativeMethods.TB_SETTOOLTIPS, new HandleRef(toolTip, toolTip.Handle), 0);

        }
        */
        private ToolTip? _toolTip;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetToolTip(ToolTip? t)
        {
            _toolTip = t;
            if (IsHandleCreated)
                RecreateHandle();
        }

        /// <summary>
        ///  Returns a string representation for this control.
        /// </summary>
        public override string ToString()
            => $"{base.ToString()}, Buttons.Count: {_buttons.Count}" + (_buttons.Count > 0 ? ", Buttons[0]: " + _buttons[0].ToString() : "");

        /// <summary>
        ///  Updates all the information in the ToolBar.  Tragically, the win32
        ///  control is pretty flakey, and the only real choice here is to recreate
        ///  the handle and re-realize all the buttons.
        /// </summary>
        private void UpdateButtons()
        {
            if (IsHandleCreated)
            {
                RecreateHandle();
            }
        }

        private ContextMenu? _toolBarButtonContextMenu;

        /// <summary>
        ///  The button clicked was a dropdown button.  If it has a menu specified,
        ///  show it now.  Otherwise, fire an onButtonDropDown event.
        /// </summary>
        private unsafe void WmNotifyDropDown(ref Message m)
        {
            int iItem;
            NativeMethods.NMTOOLBAR* nmTB = (NativeMethods.NMTOOLBAR*)m.LParam;
            iItem = nmTB->iItem;
            ToolBarButton tbb = _buttons[iItem];
            /*
            if (tbb is null)
            {
                throw new InvalidOperationException(SR.ToolBarButtonNotFound);
            }
            */

            OnButtonDropDown(new ToolBarButtonClickEventArgs(tbb));

            if (tbb.DropDownMenuInternal is { } contextMenu)
            {
                RECT rc = new RECT();

                PInvoke.SendMessage(this, PInvoke.TB_GETRECT, (WPARAM)iItem, &rc);

                _toolBarButtonContextMenu = contextMenu;
                contextMenu.Show(this, new Point(rc.left, rc.bottom));

                /* MainMenu?
                else
                {
                    menu.GetMainMenu()?.ProcessInitMenuPopup(menu.Handle);

                    PInvoke.MapWindowPoints(hwndFrom, HWND.Null, ref rc);

                    TPMPARAMS tpm;
                    unsafe
                    {
                        tpm = new TPMPARAMS
                        {
                            cbSize = (uint)sizeof(TPMPARAMS),
                            rcExclude = rc
                        };
                    }

                    IntPtr createHandle = menu.Handle;
                    BOOL result = PInvoke.TrackPopupMenuEx(menu,
                        TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN |
                        TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON |
                        TRACK_POPUP_MENU_FLAGS.TPM_VERTICAL,
                        rc.left, rc.bottom, this, ref tpm);
                    Debug.Assert(result);
                }
                */
            }
        }

        // readonly にすると動作しない。
        private ToolTipBuffer _toolTipBuffer;

        private unsafe void WmNotifyNeedText(ref Message m)
        {
            NMTTDISPINFOW* ttt = (NMTTDISPINFOW*)m.LParam;
            int commandID = (int)ttt->hdr.idFrom;
            ToolBarButton tbb = _buttons[commandID];
            _toolTipBuffer.SetText(tbb?.ToolTipText);
            ttt->lpszText = (char*)_toolTipBuffer.Buffer;

            ttt->hinst = HINSTANCE.Null;

            // RightToLeft reading order
            //
            if (RightToLeft == RightToLeft.Yes)
            {
                ttt->uFlags |= TOOLTIP_FLAGS.TTF_RTLREADING;
            }
        }

        // Track the currently hot item since the user might be using the tab and
        // arrow keys to navigate the toolbar and if that's the case, we'll need to know where to re-
        // position the tooltip window when the underlying toolbar control attempts to display it.
        private void WmNotifyHotItemChange(ref Message m)
        {
            // Should we set the hot item?
            unsafe
            {
                NMTBHOTITEM* nmTbHotItem = (NMTBHOTITEM*)m.LParam;
                if (NMTBHOTITEM_FLAGS.HICF_ENTERING == (nmTbHotItem->dwFlags & NMTBHOTITEM_FLAGS.HICF_ENTERING))
                {
                    _hotItem = nmTbHotItem->idNew;
                }
                else if (NMTBHOTITEM_FLAGS.HICF_LEAVING == (nmTbHotItem->dwFlags & NMTBHOTITEM_FLAGS.HICF_LEAVING))
                {
                    _hotItem = -1;
                }
                else if (NMTBHOTITEM_FLAGS.HICF_MOUSE == (nmTbHotItem->dwFlags & NMTBHOTITEM_FLAGS.HICF_MOUSE))
                {
                    _hotItem = nmTbHotItem->idNew;
                }
                else if (NMTBHOTITEM_FLAGS.HICF_ARROWKEYS == (nmTbHotItem->dwFlags & NMTBHOTITEM_FLAGS.HICF_ARROWKEYS))
                {
                    _hotItem = nmTbHotItem->idNew;
                }
                else if (NMTBHOTITEM_FLAGS.HICF_ACCELERATOR == (nmTbHotItem->dwFlags & NMTBHOTITEM_FLAGS.HICF_ACCELERATOR))
                {
                    _hotItem = nmTbHotItem->idNew;
                }
                else if (NMTBHOTITEM_FLAGS.HICF_DUPACCEL == (nmTbHotItem->dwFlags & NMTBHOTITEM_FLAGS.HICF_DUPACCEL))
                {
                    _hotItem = nmTbHotItem->idNew;
                }
                else if (NMTBHOTITEM_FLAGS.HICF_RESELECT == (nmTbHotItem->dwFlags & NMTBHOTITEM_FLAGS.HICF_RESELECT))
                {
                    _hotItem = nmTbHotItem->idNew;
                }
                else if (NMTBHOTITEM_FLAGS.HICF_LMOUSE == (nmTbHotItem->dwFlags & NMTBHOTITEM_FLAGS.HICF_LMOUSE))
                {
                    _hotItem = nmTbHotItem->idNew;
                }
                else if (NMTBHOTITEM_FLAGS.HICF_TOGGLEDROPDOWN == (nmTbHotItem->dwFlags & NMTBHOTITEM_FLAGS.HICF_TOGGLEDROPDOWN))
                {
                    _hotItem = nmTbHotItem->idNew;
                }
            }
        }

        private void WmReflectCommand(ref Message m)
        {
            int id = PARAM.LOWORD(m.WParam);
            ToolBarButton tbb = _buttons[id];

            //if (tbb is not null)
            {
                ToolBarButtonClickEventArgs e = new ToolBarButtonClickEventArgs(tbb);
                OnButtonClick(e);
            }

            base.WndProc(ref m);

            ResetMouseEventArgs();
        }

        protected override unsafe void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                case PInvoke.WM_COMMAND + MessageId.WM_REFLECT:
                    WmReflectCommand(ref m);
                    break;

                case PInvoke.WM_NOTIFY:
                case PInvoke.WM_NOTIFY + MessageId.WM_REFLECT:
                    NMHDR* note = (NMHDR*)m.LParam;
                    switch (note->code)
                    {
                        case PInvoke.TTN_NEEDTEXT:
                            WmNotifyNeedText(ref m);
                            m.Result = 1;
                            return;
                        case PInvoke.TTN_SHOW:
                            // Prevent the tooltip from displaying in the upper left corner of the
                            // desktop when the control is nowhere near that location.
                            WINDOWPLACEMENT wndPlacement = default;
                            int nRet = PInvoke.GetWindowPlacement(note->hwndFrom, &wndPlacement);

                            // Is this tooltip going to be positioned in the upper left corner of the display,
                            // but nowhere near the toolbar button?
                            if (wndPlacement.rcNormalPosition.left == 0 &&
                                wndPlacement.rcNormalPosition.top == 0 &&
                                _hotItem != -1)
                            {

                                // Assume that we're going to vertically center the tooltip on the right edge of the current
                                // hot item.

                                // Where is the right edge of the current hot item?
                                int buttonRight = 0;
                                for (int idx = 0; idx <= _hotItem; idx++)
                                {
                                    // How wide is the item at this index? (It could be a separator, and therefore a different width.)
                                    buttonRight += _buttonsCollection[idx].GetButtonWidth();
                                }

                                // Where can we place this tooltip so that it will be completely visible on the current display?
                                int tooltipWidth = wndPlacement.rcNormalPosition.Width;
                                int tooltipHeight = wndPlacement.rcNormalPosition.Height;
                                Debug.Assert(tooltipWidth == wndPlacement.rcNormalPosition.right - wndPlacement.rcNormalPosition.left);
                                Debug.Assert(tooltipHeight == wndPlacement.rcNormalPosition.bottom - wndPlacement.rcNormalPosition.top);

                                // We'll need screen coordinates of this position for setting the tooltip's position
                                int x = Location.X + buttonRight + 1;
                                int y = Location.Y + (ButtonSize.Height / 2);
                                var leftTop = new Point(x, y);
                                PInvoke.ClientToScreen(this, &leftTop);

                                // Will the tooltip bleed off the top?
                                if (leftTop.Y < SystemInformation.WorkingArea.Y)
                                {
                                    // Reposition the tooltip to be displayed below the button
                                    leftTop.Y += (ButtonSize.Height / 2) + 1;
                                }

                                // Will the tooltip bleed off the bottom?
                                if (leftTop.Y + tooltipHeight > SystemInformation.WorkingArea.Height)
                                {
                                    // Reposition the tooltip to be displayed above the button
                                    leftTop.Y -= ((ButtonSize.Height / 2) + tooltipHeight + 1);
                                }

                                // Will the tooltip bleed off the right edge?
                                if (leftTop.X + tooltipWidth > SystemInformation.WorkingArea.Right)
                                {
                                    // Move the tooltip far enough left that it will display in the working area
                                    leftTop.X -= (ButtonSize.Width + tooltipWidth + 2);
                                }

                                PInvoke.SetWindowPos(note->hwndFrom, HWND.Null, leftTop.X, leftTop.Y, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                                m.Result = 1;
                                return;
                            }
                            break;

                        case PInvoke.TBN_HOTITEMCHANGE:
                            WmNotifyHotItemChange(ref m);
                            break;

                        case PInvoke.TBN_QUERYINSERT:
                            m.Result = 1;
                            break;

                        case PInvoke.TBN_DROPDOWN:
                            WmNotifyDropDown(ref m);
                            break;
                    }
                    base.WndProc(ref m);    // add
                    break;

                // Control WndProc for ToolBarButton.DropDownMenu
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
                    DefWndProc(ref m);
                    break;

                // [spec]
                case PInvoke.WM_MENUCHAR:
                    if (_toolBarButtonContextMenu is not null)
                        _toolBarButtonContextMenu.WmMenuChar(ref m);
                    else
                        base.WndProc(ref m);
                    break;

                // [spec]
                case PInvoke.WM_INITMENUPOPUP:
                    if (_toolBarButtonContextMenu is not null)
                        _toolBarButtonContextMenu.ProcessInitMenuPopup(m.WParam);
                    else
                        base.WndProc(ref m);
                    break;

                // [spec]
                case PInvoke.WM_EXITMENULOOP:
                    if (_toolBarButtonContextMenu is { } contextMenu)
                    {
                        _toolBarButtonContextMenu = null;
                        if (m.WParam != 0)
                            contextMenu.RaiseCollapse();
                        DefWndProc(ref m);
                    }
                    else
                    {
                        base.WndProc(ref m);
                    }
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
            // ? WmReflectCommand だと 2 回呼ばれる。
            //base.WndProc(ref m);
        }
    }
}
