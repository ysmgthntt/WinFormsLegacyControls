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
    /// <summary>
    ///  Represents a Windows status bar control.
    /// </summary>
    //[ComVisible(true)]
    //[ClassInterface(ClassInterfaceType.AutoDispatch)]
    [DefaultEvent(nameof(PanelClick))]
    [DefaultProperty(nameof(Text))]
    //[Designer("System.Windows.Forms.Design.StatusBarDesigner, " + AssemblyRef.SystemDesign)]
    public partial class StatusBar : Control
    {
        private int _sizeGripWidth;
        private const int SIMPLE_INDEX = 0xFF;

#pragma warning disable IDE1006 // Naming styles
        private static readonly object EVENT_PANELCLICK = new object();
        private static readonly object EVENT_SBDRAWITEM = new object();
#pragma warning restore IDE1006 // Naming styles

        private bool _showPanels;
        private bool _layoutDirty;
        private int _panelsRealized;
        private bool _sizeGrip = true;
        private string? _simpleText;
        private Point _lastClick = new Point(0, 0);
        private readonly List<StatusBarPanel> _panels = new();
        private StatusBarPanelCollection? _panelsCollection;
        private ControlToolTip? _tooltips;

        private ToolTip? _mainToolTip;
        //private bool toolTipSet = false;
        private bool _rightToLeftLayout;

        /// <summary>
        ///  Initializes a new default instance of the <see cref='StatusBar'/> class.
        /// </summary>
        public StatusBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.Selectable, false);

            Dock = DockStyle.Bottom;
            TabStop = false;
        }

        private static VisualStyleRenderer? s_renderer;

        /// <summary>
        ///  A VisualStyleRenderer we can use to get information about the current UI theme
        /// </summary>
        private static VisualStyleRenderer? VisualStyleRenderer
        {
            get
            {
                if (VisualStyleRenderer.IsSupported)
                {
                    s_renderer ??= new VisualStyleRenderer(VisualStyleElement.ToolBar.Button.Normal);
                }
                else
                {
                    s_renderer = null;
                }
                return s_renderer;
            }
        }

        private int SizeGripWidth
        {
            get
            {
                if (_sizeGripWidth == 0)
                {
                    if (Application.RenderWithVisualStyles && VisualStyleRenderer is not null)
                    {
                        // Need to build up accurate gripper width to avoid cutting off other panes.
                        VisualStyleRenderer vsRenderer = VisualStyleRenderer;
                        VisualStyleElement thisElement;
                        Size elementSize;

                        using Graphics graphics = Graphics.FromHwndInternal(Handle);

                        // gripper pane width...
                        thisElement = VisualStyleElement.Status.GripperPane.Normal;
                        vsRenderer.SetParameters(thisElement);
                        elementSize = vsRenderer.GetPartSize(graphics, ThemeSizeType.True);
                        _sizeGripWidth = elementSize.Width;

                        // ...plus gripper width
                        thisElement = VisualStyleElement.Status.Gripper.Normal;
                        vsRenderer.SetParameters(thisElement);
                        elementSize = vsRenderer.GetPartSize(graphics, ThemeSizeType.True);
                        _sizeGripWidth += elementSize.Width;

                        // Either GetPartSize could have returned a width of zero, so make sure we have a reasonable number:
                        // [fixed] [DPI]
                        _sizeGripWidth = Math.Max(_sizeGripWidth, LogicalToDeviceUnits(16));
                    }
                    else
                    {
                        // [fixed] [DPI]
                        _sizeGripWidth = LogicalToDeviceUnits(/*16*/18);
                    }
                }
                return _sizeGripWidth;
            }
        }

        /// <summary>
        ///  The background color of this control. This is an ambient property and will
        ///  always return a non-null value.
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Color BackColor
        {
            // not supported, always return CONTROL
            get => SystemColors.Control;
            // no op, not supported.
            set { }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler BackColorChanged
        {
            add => base.BackColorChanged += value;
            remove => base.BackColorChanged -= value;
        }

        /// <summary>
        ///  Gets or sets the image rendered on the background of the
        ///  <see cref='StatusBar'/>
        ///  control.
        /// </summary>
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
        ///  Returns the CreateParams used to create the handle for this control.
        ///  Inheriting classes should call base.getCreateParams in the manor below:
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassName = PInvoke.STATUSCLASSNAME;

                if (_sizeGrip)
                {
                    cp.Style |= (int)PInvoke.SBARS_SIZEGRIP;
                }
                else
                {
                    cp.Style &= ~(int)PInvoke.SBARS_SIZEGRIP;
                }
                cp.Style |= (int)(PInvoke.CCS_NOPARENTALIGN | PInvoke.CCS_NORESIZE);

                if ((cp.ExStyle & (int)WINDOW_EX_STYLE.WS_EX_RTLREADING) == (int)WINDOW_EX_STYLE.WS_EX_RTLREADING)
                {
                    if (RightToLeftLayout)
                    {
                        // [spec]
                        cp.ExStyle |= (int)WINDOW_EX_STYLE.WS_EX_LAYOUTRTL;
                        cp.ExStyle &= ~(int)(WINDOW_EX_STYLE.WS_EX_RTLREADING | WINDOW_EX_STYLE.WS_EX_RIGHT | WINDOW_EX_STYLE.WS_EX_LEFTSCROLLBAR);
                    }
                    else
                    {
                        // [fixed]
                        cp.ExStyle &= ~(int)WINDOW_EX_STYLE.WS_EX_LEFTSCROLLBAR;
                    }
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
        ///  Gets or sets the docking behavior of the <see cref='StatusBar'/> control.
        /// </summary>
        [Localizable(true)]
        [DefaultValue(DockStyle.Bottom)]
        public override DockStyle Dock
        {
            get => base.Dock;
            set => base.Dock = value;
        }

        /// <summary>
        ///  Gets or sets the font the <see cref='StatusBar'/>
        ///  control will use to display
        ///  information.
        /// </summary>
        [Localizable(true)]
        [AllowNull]
        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                SetPanelContentsWidths(false);
            }
        }

        /// <summary>
        ///  Gets or sets
        ///  the forecolor for the control.
        /// </summary>
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
        ///  Gets the collection of <see cref='StatusBar'/>
        ///  panels contained within the
        ///  control.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [SRDescription(nameof(SR.StatusBarPanelsDescr))]
        [Localizable(true)]
        [SRCategory(nameof(SR.CatAppearance))]
        [MergableProperty(false)]
        public StatusBarPanelCollection Panels
            => _panelsCollection ??= new StatusBarPanelCollection(this);

        // [spec]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool RightToLeftLayout
        {
            get => _rightToLeftLayout;
            set
            {
                if (RightToLeftLayout != value)
                {
                    _rightToLeftLayout = value;
                    if (RightToLeft == RightToLeft.Yes)
                    {
                        RecreateHandle();
                    }
                }
            }
        }

        /// <summary>
        ///  The status bar text.
        /// </summary>
        [Localizable(true)]
        [AllowNull]
        public override string Text
        {
            get => _simpleText ?? string.Empty;
            set
            {
                SetSimpleText(value);
                if (_simpleText != value)
                {
                    _simpleText = value;
                    OnTextChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether panels should be shown.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [DefaultValue(false)]
        [SRDescription(nameof(SR.StatusBarShowPanelsDescr))]
        public bool ShowPanels
        {
            get => _showPanels;
            set
            {
                if (_showPanels != value)
                {
                    _showPanels = value;

                    _layoutDirty = true;
                    if (IsHandleCreated)
                    {
                        PInvoke.SendMessage(this, PInvoke.SB_SIMPLE, (WPARAM)(BOOL)(!_showPanels));

                        if (_showPanels)
                        {
                            PerformLayout();
                            RealizePanels();
                        }
                        else if (_tooltips is not null)
                        {
                            for (int i = 0; i < _panels.Count; i++)
                            {
                                _tooltips.SetTool(_panels[i], null);
                            }
                        }

                        SetSimpleText(_simpleText);
                    }
                }
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether a sizing grip
        ///  will be rendered on the corner of the <see cref='StatusBar'/>
        ///  control.
        /// </summary>
        [SRCategory(nameof(SR.CatAppearance))]
        [DefaultValue(true)]
        [SRDescription(nameof(SR.StatusBarSizingGripDescr))]
        public bool SizingGrip
        {
            get => _sizeGrip;
            set
            {
                if (value != _sizeGrip)
                {
                    _sizeGrip = value;
                    RecreateHandle();
                }
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether the user will be able to tab to the
        ///  <see cref='StatusBar'/> .
        /// </summary>
        [DefaultValue(false)]
        public new bool TabStop
        {
            get => base.TabStop;
            set => base.TabStop = value;
        }

        /*
        internal bool ToolTipSet
        {
            get
            {
                return toolTipSet;
            }
        }

        internal ToolTip MainToolTip
        {
            get
            {
                return mainToolTip;
            }
        }
        */

        /// <summary>
        ///  Occurs when a visual aspect of an owner-drawn status bar changes.
        /// </summary>
        [SRCategory(nameof(SR.CatBehavior))]
        [SRDescription(nameof(SR.StatusBarDrawItem))]
        public event StatusBarDrawItemEventHandler DrawItem
        {
            add => Events.AddHandler(EVENT_SBDRAWITEM, value);
            remove => Events.RemoveHandler(EVENT_SBDRAWITEM, value);
        }

        /// <summary>
        ///  Occurs when a panel on the status bar is clicked.
        /// </summary>
        [SRCategory(nameof(SR.CatMouse))]
        [SRDescription(nameof(SR.StatusBarOnPanelClickDescr))]
        public event StatusBarPanelClickEventHandler PanelClick
        {
            add => Events.AddHandler(EVENT_PANELCLICK, value);
            remove => Events.RemoveHandler(EVENT_PANELCLICK, value);
        }

        /// <summary>
        ///  StatusBar Onpaint.
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
        ///  Tells whether the panels have been realized.
        /// </summary>
        internal bool ArePanelsRealized() => _showPanels && IsHandleCreated;

        internal void DirtyLayout() => _layoutDirty = true;

        /// <summary>
        ///  Makes the panel according to the sizes in the panel list.
        /// </summary>
        private unsafe void ApplyPanelWidths()
        {
            // This forces handle creation every time any time the StatusBar
            // has to be re-laidout.
            if (!IsHandleCreated)
            {
                return;
            }

            int length = _panels.Count;

            if (length == 0)
            {
                Size sz = Size;
                int* offsets = stackalloc int[1];
                offsets[0] = sz.Width;
                if (_sizeGrip)
                {
                    offsets[0] -= SizeGripWidth;
                }
                PInvoke.SendMessage(this, PInvoke.SB_SETPARTS, 1, offsets);
                PInvoke.SendMessage(this, PInvoke.SB_SETICON, 0, 0);

                return;
            }

            int* offsets2 = stackalloc int[length];
            int currentOffset = 0;
            for (int i = 0; i < length; i++)
            {
                StatusBarPanel panel = _panels[i];
                currentOffset += panel.Width;
                offsets2[i] = currentOffset;
                panel.Right = offsets2[i];
            }

            PInvoke.SendMessage(this, PInvoke.SB_SETPARTS, (WPARAM)length, offsets2);

            // Tooltip setup...
            for (int i = 0; i < length; i++)
            {
                StatusBarPanel panel = _panels[i];
                UpdateTooltip(panel);
            }

            _layoutDirty = false;
        }

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
        ///  Disposes this control
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_panelsCollection is not null)
                {
                    StatusBarPanel[] panelCopy = _panels.ToArray();
                    _panelsCollection.Clear();

                    foreach (StatusBarPanel p in panelCopy)
                    {
                        p.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///  Forces the panels to be updated, location, repainting, etc.
        /// </summary>
        private void ForcePanelUpdate()
        {
            if (ArePanelsRealized())
            {
                _layoutDirty = true;
                SetPanelContentsWidths(true);
                PerformLayout();
                RealizePanels();
            }
        }

        /// <summary>
        ///  Raises the <see cref='Control.CreateHandle'/>
        ///  event.
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode)
            {
                _tooltips = new ControlToolTip(this);
            }

            if (!_showPanels)
            {
                PInvoke.SendMessage(this, PInvoke.SB_SIMPLE, (WPARAM)1);
                SetSimpleText(_simpleText);
            }
            else
            {
                ForcePanelUpdate();
            }
        }

        /// <summary>
        ///  Raises the <see cref='OnHandleDestroyed'/> event.
        /// </summary>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            if (_tooltips is not null)
            {
                _tooltips.Dispose();
                _tooltips = null;
            }
        }

        /// <summary>
        ///  Raises the <see cref='OnMouseDown'/> event.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            _lastClick.X = e.X;
            _lastClick.Y = e.Y;
            base.OnMouseDown(e);
        }

        /// <summary>
        ///  Raises the <see cref='PanelClick'/> event.
        /// </summary>
        protected virtual void OnPanelClick(StatusBarPanelClickEventArgs e)
            => ((StatusBarPanelClickEventHandler?)Events[EVENT_PANELCLICK])?.Invoke(this, e);

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (_showPanels)
            {
                LayoutPanels();
                if (IsHandleCreated && _panelsRealized != _panels.Count)
                {
                    RealizePanels();
                }
            }
            base.OnLayout(levent);
        }

        /// <summary>
        ///  This function sets up all the panel on the status bar according to
        ///  the internal this.panels List.
        /// </summary>
        private void RealizePanels()
        {
            int length = _panels.Count;
            int old = _panelsRealized;

            _panelsRealized = 0;

            if (length == 0)
            {
                PInvoke.SendMessage(this, PInvoke.SB_SETTEXT, 0, string.Empty);
            }

            int i;
            for (i = 0; i < length; i++)
            {
                StatusBarPanel panel = _panels[i];
                try
                {
                    panel.Realize();
                    _panelsRealized++;
                }
                catch
                {
                }
            }
            for (; i < old; i++)
            {
                PInvoke.SendMessage(this, PInvoke.SB_SETTEXT, 0, 0);
            }
        }

        /// <summary>
        ///  Remove the internal list of panels without updating the control.
        /// </summary>
        private void RemoveAllPanelsWithoutUpdate()
        {
            int size = _panels.Count;
            // remove the parent reference
            for (int i = 0; i < size; i++)
            {
                StatusBarPanel sbp = _panels[i];
                sbp.Parent = null;
            }

            _panels.Clear();
            if (_showPanels)
            {
                ApplyPanelWidths();
                ForcePanelUpdate();
            }
        }

        /// <summary>
        ///  Sets the widths of any panels that have the
        ///  StatusBarPanelAutoSize.CONTENTS property set.
        /// </summary>
        private void SetPanelContentsWidths(bool newPanels)
        {
            int size = _panels.Count;
            bool changed = false;
            for (int i = 0; i < size; i++)
            {
                StatusBarPanel sbp = _panels[i];
                if (sbp.AutoSize == StatusBarPanelAutoSize.Contents)
                {
                    int newWidth = sbp.GetContentsWidth(newPanels);
                    if (sbp.Width != newWidth)
                    {
                        sbp.Width = newWidth;
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                DirtyLayout();
                PerformLayout();
            }
        }

        private void SetSimpleText(string? simpleText)
        {
            if (!_showPanels && IsHandleCreated)
            {
                int wparam = SIMPLE_INDEX + (int)PInvoke.SBT_NOBORDERS;
                if (RightToLeft == RightToLeft.Yes)
                {
                    wparam |= (int)PInvoke.SBT_RTLREADING;
                }

                PInvoke.SendMessage(this, PInvoke.SB_SETTEXT, (WPARAM)wparam, simpleText);
            }
        }

        /// <summary>
        ///  Sizes the the panels appropriately.  It looks at the SPRING AutoSize
        ///  property.
        /// </summary>
        private void LayoutPanels()
        {
            int barPanelWidth = 0;
            int springNum = 0;
            StatusBarPanel?[] pArray = new StatusBarPanel[_panels.Count];
            bool changed = false;

            for (int i = 0; i < pArray.Length; i++)
            {
                StatusBarPanel panel = _panels[i];
                if (panel.AutoSize == StatusBarPanelAutoSize.Spring)
                {
                    pArray[springNum] = panel;
                    springNum++;
                }
                else
                {
                    barPanelWidth += panel.Width;
                }
            }

            if (springNum > 0)
            {
                Rectangle rect = Bounds;
                int springPanelsLeft = springNum;
                int leftoverWidth = rect.Width - barPanelWidth;
                if (_sizeGrip)
                {
                    leftoverWidth -= SizeGripWidth;
                }
                int copyOfLeftoverWidth = unchecked((int)0x80000000);
                while (springPanelsLeft > 0)
                {
                    int widthOfSpringPanel = (leftoverWidth) / springPanelsLeft;
                    if (leftoverWidth == copyOfLeftoverWidth)
                    {
                        break;
                    }

                    copyOfLeftoverWidth = leftoverWidth;

                    for (int i = 0; i < springNum; i++)
                    {
                        StatusBarPanel? panel = pArray[i];
                        if (panel is null)
                        {
                            continue;
                        }

                        if (widthOfSpringPanel < panel.MinWidth)
                        {
                            if (panel.Width != panel.MinWidth)
                            {
                                changed = true;
                            }
                            panel.Width = panel.MinWidth;
                            pArray[i] = null;
                            springPanelsLeft--;
                            leftoverWidth -= panel.MinWidth;
                        }
                        else
                        {
                            if (panel.Width != widthOfSpringPanel)
                            {
                                changed = true;
                            }
                            panel.Width = widthOfSpringPanel;
                        }
                    }
                }
            }

            if (changed || _layoutDirty)
            {
                ApplyPanelWidths();
            }
        }

        /// <summary>
        ///  Raises the <see cref='OnDrawItem'/>
        ///  event.
        /// </summary>
        protected virtual void OnDrawItem(StatusBarDrawItemEventArgs sbdievent)
            => ((StatusBarDrawItemEventHandler?)Events[EVENT_SBDRAWITEM])?.Invoke(this, sbdievent);

        /// <summary>
        ///  Raises the <see cref='OnResize'/> event.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            Invalidate();
            base.OnResize(e);
        }

        /// <summary>
        ///  Returns a string representation for this control.
        /// </summary>
        public override string ToString()
            => $"{base.ToString()}, Panels.Count: {Panels.Count}" + (Panels.Count > 0 ? ", Panels[0]: " + Panels[0].ToString() : "");

        // call this when System.Windows.forms.toolTip is Associated with Statusbar....
        /*
        internal void SetToolTip(ToolTip t)
        {
            mainToolTip = t;
            toolTipSet = true;
        }
        */
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetToolTip(ToolTip t)
        {
            _mainToolTip = t;
            if (IsHandleCreated)
                RecreateHandle();
        }

        internal void UpdateTooltip(StatusBarPanel panel)
        {
            if (_tooltips is null)
            {
                if (IsHandleCreated && !DesignMode)
                {
                    //This shouldn't happen: tooltips should've already been set.  The best we can
                    //do here is reset it.
                    _tooltips = new ControlToolTip(this);
                }
                else
                {
                    return;
                }
            }

            if (panel.Parent == this && panel.ToolTipText.Length > 0)
            {
                int border = SystemInformation.Border3DSize.Width;
                ControlToolTip.Tool t = _tooltips.GetTool(panel) ?? new();
                t.text = panel.ToolTipText;
                t.rect = new Rectangle(panel.Right - panel.Width + border, 0, panel.Width - border, Height);
                _tooltips.SetTool(panel, t);
            }
            else
            {
                _tooltips.SetTool(panel, null);
            }
        }

        private void UpdatePanelIndex()
        {
            int length = _panels.Count;
            for (int i = 0; i < length; i++)
            {
                _panels[i].Index = i;
            }
        }

        /// <summary>
        ///  Processes messages for ownerdraw panels.
        /// </summary>
        private unsafe void WmDrawItem(ref Message m)
        {
            DRAWITEMSTRUCT* dis = (DRAWITEMSTRUCT*)m.LParam;

            int length = _panels.Count;
            if (dis->itemID < 0 || dis->itemID >= length)
            {
                Debug.Fail("OwnerDraw item out of range");
            }

            // The itemState is not defined for a statusbar control
            StatusBarPanel panel = _panels[(int)dis->itemID];

            if (RightToLeftLayout && IsMirrored)
            {
                // [spec]
                // RTL to LTR Coordinate transformation for GDI+
                int left = Width - dis->rcItem.right;
                RECT rect = new RECT(left, dis->rcItem.top, left + dis->rcItem.Width, dis->rcItem.bottom);
                uint oldLayout = PInvoke.SetLayout(dis->hDC, (DC_LAYOUT)0);
                try
                {
                    using Graphics g = Graphics.FromHdcInternal(dis->hDC);
                    OnDrawItem(new StatusBarDrawItemEventArgs(g, Font, rect, (int)dis->itemID, DrawItemState.None, panel, ForeColor, BackColor));
                }
                finally
                {
                    _ = PInvoke.SetLayout(dis->hDC, (DC_LAYOUT)oldLayout);
                }
            }
            else
            {
                using Graphics g = Graphics.FromHdcInternal(dis->hDC);
                OnDrawItem(new StatusBarDrawItemEventArgs(g, Font, dis->rcItem, (int)dis->itemID, DrawItemState.None, panel, ForeColor, BackColor));
            }
        }

        private unsafe void WmNotifyNMClick(NMHDR* note)
        {
            if (!_showPanels)
            {
                return;
            }

            int size = _panels.Count;
            int currentOffset = 0;
            int index = -1;
            for (int i = 0; i < size; i++)
            {
                StatusBarPanel panel = _panels[i];
                currentOffset += panel.Width;
                if (_lastClick.X < currentOffset)
                {
                    // this is where the mouse was clicked.
                    index = i;
                    break;
                }
            }
            if (index != -1)
            {
                MouseButtons button = MouseButtons.Left;
                int clicks = 0;
                switch (note->code)
                {
                    case PInvoke.NM_CLICK:
                        button = MouseButtons.Left;
                        clicks = 1;
                        break;
                    case PInvoke.NM_RCLICK:
                        button = MouseButtons.Right;
                        clicks = 1;
                        break;
                    case PInvoke.NM_DBLCLK:
                        button = MouseButtons.Left;
                        clicks = 2;
                        break;
                    case PInvoke.NM_RDBLCLK:
                        button = MouseButtons.Right;
                        clicks = 2;
                        break;
                }

                Point pt = _lastClick;
                StatusBarPanel panel = _panels[index];

                StatusBarPanelClickEventArgs sbpce = new StatusBarPanelClickEventArgs(panel,
                                                                                      button, clicks, pt.X, pt.Y);
                OnPanelClick(sbpce);
            }
        }

        private void WmNCHitTest(ref Message m)
        {
            if (SizingGrip)
            {
                int x = PARAM.SignedLOWORD(m.LParam);
                Rectangle bounds = Bounds;

                // [fixed]
                // WM_NCHITTEST message has screen coordinates.
                x = PointToClient(new Point(x, bounds.Y)).X;

                // The default implementation of the statusbar
                // will let you size the form when it is docked on the bottom,
                // but when it is anywhere else, the statusbar will be resized.
                // to prevent that we provide a little bit a sanity to only
                // allow resizing, when it would resize the form.
                if (x > bounds.X + bounds.Width - SizeGripWidth)
                {
                    if (Parent/*Internal*/ is not Form form)
                    {
                        m.Result = (nint)PInvoke.HTCLIENT;
                        return;
                    }

                    /*
                    ControlCollection children = form.Controls;
                    int c = children.Count;
                    for (int i = 0; i < c; i++)
                    {
                        Control ctl = children[i];
                        if (ctl != this && ctl.Dock == DockStyle.Bottom)
                        {
                            if (ctl.Top > Top)
                            {
                                callSuper = false;
                                break;
                            }
                        }
                    }
                    */

                    // [fixed]
                    Size formClientSize = form.ClientSize;
                    if (bounds.Bottom < formClientSize.Height || bounds.Right < formClientSize.Width ||
                        !form.TopLevel || form is not { FormBorderStyle: FormBorderStyle.Sizable or FormBorderStyle.SizableToolWindow } ||
                        Dock != DockStyle.Bottom)
                    {
                        m.Result = (nint)PInvoke.HTCLIENT;
                        return;
                    }
                }
            }

            base.WndProc(ref m);
        }

        /// <summary>
        ///  Base wndProc. All messages are sent to wndProc after getting filtered through
        ///  the preProcessMessage function. Inheriting controls should call base.wndProc
        ///  for any messages that they don't handle.
        /// </summary>
        protected override unsafe void WndProc(ref Message m)
        {
            switch ((uint)m.Msg)
            {
                case PInvoke.WM_NCHITTEST:
                    WmNCHitTest(ref m);
                    break;
                case MessageId.WM_REFLECT | PInvoke.WM_DRAWITEM:
                    WmDrawItem(ref m);
                    break;
                case PInvoke.WM_NOTIFY:
                case PInvoke.WM_NOTIFY | MessageId.WM_REFLECT:
                    NMHDR* note = (NMHDR*)m.LParam;
                    switch (note->code)
                    {
                        case PInvoke.NM_CLICK:
                        case PInvoke.NM_RCLICK:
                        case PInvoke.NM_DBLCLK:
                        case PInvoke.NM_RDBLCLK:
                            WmNotifyNMClick(note);
                            break;
                        default:
                            base.WndProc(ref m);
                            break;
                    }
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }
}
