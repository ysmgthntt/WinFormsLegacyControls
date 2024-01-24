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
    [
    ComVisible(true),
    ClassInterface(ClassInterfaceType.AutoDispatch),
    DefaultEvent(nameof(PanelClick)),
    DefaultProperty(nameof(Text)),
    //Designer("System.Windows.Forms.Design.StatusBarDesigner, " + AssemblyRef.SystemDesign),
    ]
    public class StatusBar : Control
    {
        private int sizeGripWidth = 0;
        private const int SIMPLE_INDEX = 0xFF;

        private static readonly object EVENT_PANELCLICK = new object();
        private static readonly object EVENT_SBDRAWITEM = new object();

        private bool showPanels;
        private bool layoutDirty;
        private int panelsRealized;
        private bool sizeGrip = true;
        private string? simpleText;
        private Point lastClick = new Point(0, 0);
        private readonly List<StatusBarPanel> _panels = new();
        private StatusBarPanelCollection? panelsCollection;
        private ControlToolTip? tooltips;

        private ToolTip? mainToolTip = null;
        //private bool toolTipSet = false;
        private bool _rightToLeftLayout;

        /// <summary>
        ///  Initializes a new default instance of the <see cref='StatusBar'/> class.
        /// </summary>
        public StatusBar()
        : base()
        {
            base.SetStyle(ControlStyles.UserPaint | ControlStyles.Selectable, false);

            Dock = DockStyle.Bottom;
            TabStop = false;
        }

        private static VisualStyleRenderer? renderer = null;

        /// <summary>
        ///  A VisualStyleRenderer we can use to get information about the current UI theme
        /// </summary>
        private static VisualStyleRenderer? VisualStyleRenderer
        {
            get
            {
                if (VisualStyleRenderer.IsSupported)
                {
                    renderer ??= new VisualStyleRenderer(VisualStyleElement.ToolBar.Button.Normal);
                }
                else
                {
                    renderer = null;
                }
                return renderer;
            }
        }

        private int SizeGripWidth
        {
            get
            {
                if (sizeGripWidth == 0)
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
                        sizeGripWidth = elementSize.Width;

                        // ...plus gripper width
                        thisElement = VisualStyleElement.Status.Gripper.Normal;
                        vsRenderer.SetParameters(thisElement);
                        elementSize = vsRenderer.GetPartSize(graphics, ThemeSizeType.True);
                        sizeGripWidth += elementSize.Width;

                        // Either GetPartSize could have returned a width of zero, so make sure we have a reasonable number:
                        sizeGripWidth = Math.Max(sizeGripWidth, 16);
                    }
                    else
                    {
                        sizeGripWidth = 16;
                    }
                }
                return sizeGripWidth;
            }
        }

        /// <summary>
        ///  The background color of this control. This is an ambient property and will
        ///  always return a non-null value.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Color BackColor
        {
            get
            {
                // not supported, always return CONTROL
                return SystemColors.Control;
            }
            set
            {
                // no op, not supported.
            }
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler BackColorChanged
        {
            add => base.BackColorChanged += value;
            remove => base.BackColorChanged -= value;
        }

        /// <summary>
        ///  Gets or sets the image rendered on the background of the
        ///  <see cref='StatusBar'/>
        ///  control.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Image? BackgroundImage
        {
            get
            {
                return base.BackgroundImage;
            }
            set
            {
                base.BackgroundImage = value;
            }
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler BackgroundImageChanged
        {
            add => base.BackgroundImageChanged += value;
            remove => base.BackgroundImageChanged -= value;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override ImageLayout BackgroundImageLayout
        {
            get
            {
                return base.BackgroundImageLayout;
            }
            set
            {
                base.BackgroundImageLayout = value;
            }
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler BackgroundImageLayoutChanged
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

                if (sizeGrip)
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

        protected override ImeMode DefaultImeMode
        {
            get
            {
                return ImeMode.Disable;
            }
        }

        /// <summary>
        ///  Deriving classes can override this to configure a default size for their control.
        ///  This is more efficient than setting the size in the control's constructor.
        /// </summary>
        protected override Size DefaultSize
        {
            get
            {
                return new Size(100, 22);
            }
        }

        /// <summary>
        ///  This property is overridden and hidden from statement completion
        ///  on controls that are based on Win32 Native Controls.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override bool DoubleBuffered
        {
            get
            {
                return base.DoubleBuffered;
            }
            set
            {
                base.DoubleBuffered = value;
            }
        }

        /// <summary>
        ///  Gets or sets the docking behavior of the <see cref='StatusBar'/> control.
        /// </summary>
        [
        Localizable(true),
        DefaultValue(DockStyle.Bottom)
        ]
        public override DockStyle Dock
        {
            get
            {
                return base.Dock;
            }
            set
            {
                base.Dock = value;
            }
        }

        /// <summary>
        ///  Gets or sets the font the <see cref='StatusBar'/>
        ///  control will use to display
        ///  information.
        /// </summary>
        [
        Localizable(true)
        ]
        [AllowNull]
        public override Font Font
        {
            get { return base.Font; }
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
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Color ForeColor
        {
            get
            {
                return base.ForeColor;
            }
            set
            {
                base.ForeColor = value;
            }
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler ForeColorChanged
        {
            add => base.ForeColorChanged += value;
            remove => base.ForeColorChanged -= value;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public ImeMode ImeMode
        {
            get
            {
                return base.ImeMode;
            }
            set
            {
                base.ImeMode = value;
            }
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
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
        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        SRDescription(nameof(SR.StatusBarPanelsDescr)),
        Localizable(true),
        SRCategory(nameof(SR.CatAppearance)),
        MergableProperty(false)
        ]
        public StatusBarPanelCollection Panels
            => panelsCollection ??= new StatusBarPanelCollection(this);

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
        [
        Localizable(true)
        ]
        [AllowNull]
        public override string Text
        {
            get
            {
                if (simpleText is null)
                {
                    return "";
                }
                else
                {
                    return simpleText;
                }
            }
            set
            {
                SetSimpleText(value);
                if (simpleText != value)
                {
                    simpleText = value;
                    OnTextChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether panels should be shown.
        /// </summary>
        [
        SRCategory(nameof(SR.CatBehavior)),
        DefaultValue(false),
        SRDescription(nameof(SR.StatusBarShowPanelsDescr))
        ]
        public bool ShowPanels
        {
            get
            {
                return showPanels;
            }
            set
            {
                if (showPanels != value)
                {
                    showPanels = value;

                    layoutDirty = true;
                    if (IsHandleCreated)
                    {
                        PInvoke.SendMessage(this, PInvoke.SB_SIMPLE, (WPARAM)(BOOL)(!showPanels));

                        if (showPanels)
                        {
                            PerformLayout();
                            RealizePanels();
                        }
                        else if (tooltips is not null)
                        {
                            for (int i = 0; i < _panels.Count; i++)
                            {
                                tooltips.SetTool(_panels[i], null);
                            }
                        }

                        SetSimpleText(simpleText);
                    }
                }
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether a sizing grip
        ///  will be rendered on the corner of the <see cref='StatusBar'/>
        ///  control.
        /// </summary>
        [
        SRCategory(nameof(SR.CatAppearance)),
        DefaultValue(true),
        SRDescription(nameof(SR.StatusBarSizingGripDescr))
        ]
        public bool SizingGrip
        {
            get
            {
                return sizeGrip;
            }
            set
            {
                if (value != sizeGrip)
                {
                    sizeGrip = value;
                    RecreateHandle();
                }
            }
        }

        /// <summary>
        ///  Gets or sets a value indicating whether the user will be able to tab to the
        ///  <see cref='StatusBar'/> .
        /// </summary>
        [DefaultValue(false)]
        new public bool TabStop
        {
            get
            {
                return base.TabStop;
            }
            set
            {
                base.TabStop = value;
            }
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
        [SRCategory(nameof(SR.CatBehavior)), SRDescription(nameof(SR.StatusBarDrawItem))]
        public event StatusBarDrawItemEventHandler DrawItem
        {
            add => Events.AddHandler(EVENT_SBDRAWITEM, value);
            remove => Events.RemoveHandler(EVENT_SBDRAWITEM, value);
        }

        /// <summary>
        ///  Occurs when a panel on the status bar is clicked.
        /// </summary>
        [SRCategory(nameof(SR.CatMouse)), SRDescription(nameof(SR.StatusBarOnPanelClickDescr))]
        public event StatusBarPanelClickEventHandler PanelClick
        {
            add => Events.AddHandler(EVENT_PANELCLICK, value);
            remove => Events.RemoveHandler(EVENT_PANELCLICK, value);
        }

        /// <summary>
        ///  StatusBar Onpaint.
        /// </summary>
        /// <hideinheritance/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event PaintEventHandler Paint
        {
            add => base.Paint += value;
            remove => base.Paint -= value;
        }

        /// <summary>
        ///  Tells whether the panels have been realized.
        /// </summary>
        internal bool ArePanelsRealized()
        {
            return showPanels && IsHandleCreated;
        }

        internal void DirtyLayout()
        {
            layoutDirty = true;
        }

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
                Span<int> offsets = stackalloc int[1];
                offsets[0] = sz.Width;
                if (sizeGrip)
                {
                    offsets[0] -= SizeGripWidth;
                }
                PInvoke.SendMessage(this, PInvoke.SB_SETPARTS, 1, ref offsets[0]);
                PInvoke.SendMessage(this, PInvoke.SB_SETICON, 0, 0);

                return;
            }

            int[] offsets2 = new int[length];
            int currentOffset = 0;
            for (int i = 0; i < length; i++)
            {
                StatusBarPanel panel = _panels[i];
                currentOffset += panel.Width;
                offsets2[i] = currentOffset;
                panel.Right = offsets2[i];
            }

            fixed (int* pOffsets = offsets2)
            {
                PInvoke.SendMessage(this, PInvoke.SB_SETPARTS, (WPARAM)length, (LPARAM)pOffsets);
            }

            // Tooltip setup...
            for (int i = 0; i < length; i++)
            {
                StatusBarPanel panel = _panels[i];
                UpdateTooltip(panel);
            }

            layoutDirty = false;
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
                if (panelsCollection is not null)
                {
                    StatusBarPanel[] panelCopy = new StatusBarPanel[panelsCollection.Count];
                    ((ICollection)panelsCollection).CopyTo(panelCopy, 0);
                    panelsCollection.Clear();

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
                layoutDirty = true;
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
                tooltips = new ControlToolTip(this);
            }

            if (!showPanels)
            {
                PInvoke.SendMessage(this, PInvoke.SB_SIMPLE, (WPARAM)1);
                SetSimpleText(simpleText);
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
            if (tooltips is not null)
            {
                tooltips.Dispose();
                tooltips = null;
            }
        }

        /// <summary>
        ///  Raises the <see cref='OnMouseDown'/> event.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            lastClick.X = e.X;
            lastClick.Y = e.Y;
            base.OnMouseDown(e);
        }

        /// <summary>
        ///  Raises the <see cref='PanelClick'/> event.
        /// </summary>
        protected virtual void OnPanelClick(StatusBarPanelClickEventArgs e)
        {
            ((StatusBarPanelClickEventHandler?)Events[EVENT_PANELCLICK])?.Invoke(this, e);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (showPanels)
            {
                LayoutPanels();
                if (IsHandleCreated && panelsRealized != _panels.Count)
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
            int old = panelsRealized;

            panelsRealized = 0;

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
                    panelsRealized++;
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
                sbp.ParentInternal = null;
            }

            _panels.Clear();
            if (showPanels == true)
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
            if (!showPanels && IsHandleCreated)
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
                if (sizeGrip)
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

            if (changed || layoutDirty)
            {
                ApplyPanelWidths();
            }
        }

        /// <summary>
        ///  Raises the <see cref='OnDrawItem'/>
        ///  event.
        /// </summary>
        protected virtual void OnDrawItem(StatusBarDrawItemEventArgs sbdievent)
        {
            ((StatusBarDrawItemEventHandler?)Events[EVENT_SBDRAWITEM])?.Invoke(this, sbdievent);
        }

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
        {
            string s = base.ToString();
            if (Panels is not null)
            {
                s += ", Panels.Count: " + Panels.Count.ToString(CultureInfo.CurrentCulture);
                if (Panels.Count > 0)
                {
                    s += ", Panels[0]: " + Panels[0].ToString();
                }
            }
            return s;
        }

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
            mainToolTip = t;
            if (IsHandleCreated)
                RecreateHandle();
        }

        internal void UpdateTooltip(StatusBarPanel panel)
        {
            if (tooltips is null)
            {
                if (IsHandleCreated && !DesignMode)
                {
                    //This shouldn't happen: tooltips should've already been set.  The best we can
                    //do here is reset it.
                    tooltips = new ControlToolTip(this);
                }
                else
                {
                    return;
                }
            }

            if (panel.Parent == this && panel.ToolTipText.Length > 0)
            {
                int border = SystemInformation.Border3DSize.Width;
                ControlToolTip.Tool t = tooltips.GetTool(panel) ?? new();
                t.text = panel.ToolTipText;
                t.rect = new Rectangle(panel.Right - panel.Width + border, 0, panel.Width - border, Height);
                tooltips.SetTool(panel, t);
            }
            else
            {
                tooltips.SetTool(panel, null);
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

            if (RightToLeftLayout && /*RightToLeft == RightToLeft.Yes*/PInvoke.GetLayout(dis->hDC) == (nint)DC_LAYOUT.LAYOUT_RTL)
            {
                // RTL to LTR Coordinate transformation for GDI+
                int left = Width - dis->rcItem.right;
                RECT rect = new RECT(left, dis->rcItem.top, left + dis->rcItem.Width, dis->rcItem.bottom);
                uint oldLayout = PInvoke.SetLayout(dis->hDC, DC_LAYOUT.LAYOUT_BITMAPORIENTATIONPRESERVED);
                try
                {
                    using Graphics g = Graphics.FromHdcInternal(dis->hDC);
                    OnDrawItem(new StatusBarDrawItemEventArgs(g, Font, rect, (int)dis->itemID, DrawItemState.None, panel, ForeColor, BackColor));
                }
                finally
                {
                    PInvoke.SetLayout(dis->hDC, (DC_LAYOUT)oldLayout);
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
            if (!showPanels)
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
                if (lastClick.X < currentOffset)
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

                Point pt = lastClick;
                StatusBarPanel panel = _panels[index];

                StatusBarPanelClickEventArgs sbpce = new StatusBarPanelClickEventArgs(panel,
                                                                                      button, clicks, pt.X, pt.Y);
                OnPanelClick(sbpce);
            }
        }

        private void WmNCHitTest(ref Message m)
        {
            int x = PARAM.SignedLOWORD(m.LParam);
            Rectangle bounds = Bounds;
            bool callSuper = true;

            // The default implementation of the statusbar
            // will let you size the form when it is docked on the bottom,
            // but when it is anywhere else, the statusbar will be resized.
            // to prevent that we provide a little bit a sanity to only
            // allow resizing, when it would resize the form.
            if (x > bounds.X + bounds.Width - SizeGripWidth)
            {
                if (Parent/*Internal*/ is Form form)
                {
                    FormBorderStyle bs = form.FormBorderStyle;

                    if (bs != FormBorderStyle.Sizable
                        && bs != FormBorderStyle.SizableToolWindow)
                    {
                        callSuper = false;
                    }

                    if (!form.TopLevel
                        || Dock != DockStyle.Bottom)
                    {
                        callSuper = false;
                    }

                    if (callSuper)
                    {
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
                    }
                }
                else
                {
                    callSuper = false;
                }
            }

            if (callSuper)
            {
                base.WndProc(ref m);
            }
            else
            {
                m.Result = (nint)PInvoke.HTCLIENT;
            }
        }

        /// <summary>
        ///  Base wndProc. All messages are sent to wndProc after getting filtered through
        ///  the preProcessMessage function. Inheriting controls should call base.wndProc
        ///  for any messages that they don't handle.
        /// </summary>
        protected unsafe override void WndProc(ref Message m)
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

        /// <summary>
        ///  The collection of StatusBarPanels that the StatusBar manages.
        ///  event.
        /// </summary>
        [ListBindable(false)]
        public class StatusBarPanelCollection : IList
        {
            private readonly StatusBar owner;

            // A caching mechanism for key accessor
            // We use an index here rather than control so that we don't have lifetime
            // issues by holding on to extra references.
            private int lastAccessedIndex = -1;

            /// <summary>
            ///  Constructor for the StatusBarPanelCollection class
            /// </summary>
            public StatusBarPanelCollection(StatusBar owner)
            {
                ArgumentNullException.ThrowIfNull(owner);
                this.owner = owner;
            }

            /// <summary>
            ///  This method will return an individual StatusBarPanel with the appropriate index.
            /// </summary>
            public virtual StatusBarPanel this[int index]
            {
                get => owner._panels[index];
                set
                {
                    ArgumentNullException.ThrowIfNull(value);

                    owner.layoutDirty = true;

                    if (value.Parent is not null)
                    {
                        throw new ArgumentException(SR.ObjectHasParent, nameof(value));
                    }

                    int length = owner._panels.Count;

                    if (index < 0 || index >= length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                    }

                    StatusBarPanel oldPanel = owner._panels[index];
                    oldPanel.ParentInternal = null;
                    value.ParentInternal = owner;
                    if (value.AutoSize == StatusBarPanelAutoSize.Contents)
                    {
                        value.Width = value.GetContentsWidth(true);
                    }
                    owner._panels[index] = value;
                    value.Index = index;

                    if (owner.ArePanelsRealized())
                    {
                        owner.PerformLayout();
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
            public int Count => owner._panels.Count;

            object ICollection.SyncRoot => this;

            bool ICollection.IsSynchronized => false;

            bool IList.IsFixedSize => false;

            public bool IsReadOnly => false;

            /// <summary>
            ///  Adds a StatusBarPanel to the collection.
            /// </summary>
            public virtual StatusBarPanel Add(string text)
            {
                StatusBarPanel panel = new StatusBarPanel
                {
                    Text = text
                };
                Add(panel);
                return panel;
            }

            /// <summary>
            ///  Adds a StatusBarPanel to the collection.
            /// </summary>
            public virtual int Add(StatusBarPanel value)
            {
                int index = owner._panels.Count;
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

            /// <summary>
            ///  Inserts a StatusBarPanel in the collection.
            /// </summary>
            public virtual void Insert(int index, StatusBarPanel value)
            {
                //check for the value not to be null
                ArgumentNullException.ThrowIfNull(value);
                //end check

                owner.layoutDirty = true;
                if (value.Parent != owner && value.Parent is not null)
                {
                    throw new ArgumentException(SR.ObjectHasParent, nameof(value));
                }

                int length = owner._panels.Count;

                if (index < 0 || index > length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, string.Format(SR.InvalidArgument, nameof(index), index));
                }

                value.ParentInternal = owner;

                switch (value.AutoSize)
                {
                    case StatusBarPanelAutoSize.None:
                    case StatusBarPanelAutoSize.Spring:
                        break;
                    case StatusBarPanelAutoSize.Contents:
                        value.Width = value.GetContentsWidth(true);
                        break;
                }

                owner._panels.Insert(index, value);
                owner.UpdatePanelIndex();

                owner.ForcePanelUpdate();
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
                owner.RemoveAllPanelsWithoutUpdate();
                owner.PerformLayout();
            }

            /// <summary>
            ///  Removes an individual StatusBarPanel in the collection.
            /// </summary>
            public virtual void Remove(StatusBarPanel value)
            {
                //check for the value not to be null
                ArgumentNullException.ThrowIfNull(value);
                //end check

                if (value.Parent != owner)
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
                StatusBarPanel panel = owner._panels[index];

                owner._panels.RemoveAt(index);
                panel.ParentInternal = null;

                // this will cause the panels tooltip to be removed since it's no longer a child
                // of this StatusBar.
                owner.UpdateTooltip(panel);

                // We must reindex the panels after a removal...
                owner.UpdatePanelIndex();
                owner.ForcePanelUpdate();
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
                => ((ICollection)owner._panels).CopyTo(dest, index);

            /// <summary>
            ///  Returns the Enumerator for this collection.
            /// </summary>
            public IEnumerator GetEnumerator()
                => owner._panels.GetEnumerator();
        }

        /// <summary>
        ///  This is a tooltip control that provides tips for a single
        ///  control. Each "tool" region is defined by a rectangle and
        ///  the string that should be displayed. This implementation
        ///  is based on System.Windows.Forms.ToolTip, but this control
        ///  is lighter weight and provides less functionality... however
        ///  this control binds to rectangular regions, instead of
        ///  full controls.
        /// </summary>
        private sealed class ControlToolTip : IWin32Window
        {
            public sealed class Tool
            {
                public Rectangle rect = Rectangle.Empty;
                public string? text;
                internal IntPtr id = new IntPtr(-1);
            }

            private readonly Hashtable tools = new Hashtable();
            private ToolTipNativeWindow? window;
            private readonly StatusBar parent;
            private int nextId = 0;

            /// <summary>
            ///  Creates a new ControlToolTip.
            /// </summary>
            public ControlToolTip(StatusBar parent)
            {
                this.parent = parent;
            }

            /// <summary>
            ///  Returns the createParams to create the window.
            /// </summary>
            private CreateParams CreateParams
            {
                get
                {
                    unsafe
                    {
                        PInvoke.InitCommonControlsEx(new INITCOMMONCONTROLSEX
                        {
                            dwSize = (uint)sizeof(INITCOMMONCONTROLSEX),
                            dwICC = INITCOMMONCONTROLSEX_ICC.ICC_TAB_CLASSES
                        });
                    }

                    var cp = new CreateParams
                    {
                        Parent = IntPtr.Zero,
                        ClassName = PInvoke.TOOLTIPS_CLASS
                    };
                    cp.Style |= (int)PInvoke.TTS_ALWAYSTIP;
                    cp.ExStyle = 0;
                    cp.Caption = null;
                    return cp;
                }
            }

            public IntPtr Handle
            {
                get
                {
                    if (!IsHandleCreated)
                    {
                        CreateHandle();
                    }
                    return window.Handle;
                }
            }

            [MemberNotNullWhen(true, nameof(window))]
            private bool IsHandleCreated
                => window is not null && window.Handle != IntPtr.Zero;

            private void AssignId(Tool tool)
            {
                tool.id = (IntPtr)nextId;
                nextId++;
            }

            /// <summary>
            ///  Sets the tool for the specified key. Keep in mind
            ///  that as soon as setTool is called, the handle for
            ///  the ControlToolTip is created, and the handle for
            ///  the parent control is also created. If the parent
            ///  handle is recreated in the future, all tools must
            ///  be re-added. The old tool for the specified key
            ///  will be removed. Passing null in for the
            ///  tool parameter will result in the tool
            ///  region being removed.
            /// </summary>
            public void SetTool(object key, Tool? tool)
            {
                bool remove = false;
                bool add = false;
                bool update = false;

                Tool? toRemove = null;
                if (tools.ContainsKey(key))
                {
                    toRemove = (Tool?)tools[key];
                }

                if (toRemove is not null)
                {
                    remove = true;
                }
                if (tool is not null)
                {
                    add = true;
                }
                if (tool is not null && toRemove is not null
                    && tool.id == toRemove.id)
                {
                    update = true;
                }

                if (update)
                {
                    UpdateTool(tool!);
                }
                else
                {
                    if (remove)
                    {
                        RemoveTool(toRemove!);
                    }
                    if (add)
                    {
                        AddTool(tool!);
                    }
                }

                if (tool is not null)
                {
                    tools[key] = tool;
                }
                else
                {
                    tools.Remove(key);
                }
            }

            /// <summary>
            ///  Returns the tool associated with the specified key,
            ///  or null if there is no area.
            /// </summary>
            public Tool? GetTool(object key)
            {
                return (Tool?)tools[key];
            }

            // [fixed]
            // 指定した StatusBarPanel の ToolTipText が変更、削除できない。
            // TTM_SETTOOLINFOW, TTM_DELTOOLW も TTM_ADDTOOLW した Handle に送信する必要がある。
            private LRESULT SendMessage(ToolInfoWrapper<Control> info, uint message)
            {
                ToolTip? t = parent.mainToolTip;
                HandleRef handle;
                if (t is not null)
                    handle = new HandleRef(t, WinFormsLegacyControls.Migration.ToolTipAccessors.GetHandle(t));
                else
                    handle = new HandleRef(this, Handle);
                return info.SendMessage(handle, message);
            }

            private void AddTool(Tool tool)
            {
                if (tool is not null && tool.text is not null && tool.text.Length > 0)
                {
                    ToolInfoWrapper<Control> info = GetTOOLINFO(tool);
                    if (SendMessage(info, PInvoke.TTM_ADDTOOLW) == 0)
                    {
                        throw new InvalidOperationException(SR.StatusBarAddFailed);
                    }
                }
            }

            private void RemoveTool(Tool tool)
            {
                if (tool is not null && tool.text is not null && tool.text.Length > 0 && (int)tool.id >= 0)
                {
                    ToolInfoWrapper<Control> info = GetMinTOOLINFO(tool);
                    SendMessage(info, PInvoke.TTM_DELTOOLW);
                }
            }

            private void UpdateTool(Tool tool)
            {
                if (tool is not null && tool.text is not null && tool.text.Length > 0 && (int)tool.id >= 0)
                {
                    ToolInfoWrapper<Control> info = GetTOOLINFO(tool);
                    SendMessage(info, PInvoke.TTM_SETTOOLINFOW);
                }
            }

            /// <summary>
            ///  Creates the handle for the control.
            /// </summary>
            [MemberNotNull(nameof(window))]
            private void CreateHandle()
            {
                window ??= new();
                window.CreateHandle(CreateParams);
                PInvoke.SetWindowPos(
                    this,
                    HWND.HWND_TOPMOST,
                    0, 0, 0, 0,
                    SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

                // Setting the max width has the added benefit of enabling multiline tool tips
                PInvoke.SendMessage(this, PInvoke.TTM_SETMAXTIPWIDTH, 0, (LPARAM)SystemInformation.MaxWindowTrackSize.Width);
            }

            /// <summary>
            ///  Destroys the handle for this control.
            /// </summary>
            private void DestroyHandle()
            {
                if (IsHandleCreated)
                {
                    window.DestroyHandle();
                    tools.Clear();
                }
            }

            /// <summary>
            ///  Disposes of the component.  Call dispose when the component is no longer needed.
            ///  This method removes the component from its container (if the component has a site)
            ///  and triggers the dispose event.
            /// </summary>
            public void Dispose()
            {
                DestroyHandle();
            }

            /// <summary>
            ///  Returns a new instance of the TOOLINFO_T structure with the minimum
            ///  required data to uniquely identify a region. This is used primarily
            ///  for delete operations. NOTE: This cannot force the creation of a handle.
            /// </summary>
            private ToolInfoWrapper<Control> GetMinTOOLINFO(Tool tool)
            {
                if ((int)tool.id < 0)
                {
                    AssignId(tool);
                }

                return new ToolInfoWrapper<Control>(
                    parent,
                    // [fixed]
                    // https://github.com/dotnet/winforms/pull/1612/files#diff-8d43c48c6ec7a62bb08bf0bb5f4669378adcba9417e33f554f9ff8fdab508aef
                    //id: parent is StatusBar sb ? sb.Handle : tool.id);
                    //id: parent is StatusBar sb && sb.mainToolTip is not null ? sb.Handle : tool.id);
                    // 指定した StatusBarPanel の ToolTipText が変更、削除できない。
                    // TTM_SETTOOLINFOW, TTM_DELTOOLW は TTTOOLINFOW.uId を使用して対象を識別する。
                    id: tool.id);
            }

            /// <summary>
            ///  Returns a detailed TOOLINFO_T structure that represents the specified
            ///  region. NOTE: This may force the creation of a handle.
            /// </summary>
            private ToolInfoWrapper<Control> GetTOOLINFO(Tool tool)
            {
                ToolInfoWrapper<Control> ti = GetMinTOOLINFO(tool);
                ti.Info.uFlags |= TOOLTIP_FLAGS.TTF_TRANSPARENT | TOOLTIP_FLAGS.TTF_SUBCLASS;

                // RightToLeft reading order
                Control richParent = parent;
                if (richParent is not null && richParent.RightToLeft == RightToLeft.Yes)
                {
                    ti.Info.uFlags |= TOOLTIP_FLAGS.TTF_RTLREADING;
                }

                ti.Text = tool.text;
                ti.Info.rect = tool.rect;
                return ti;
            }

            ~ControlToolTip()
            {
                DestroyHandle();
            }

            private sealed class ToolTipNativeWindow : NativeWindow
            {
                protected override void WndProc(ref Message m)
                {
                    switch ((uint)m.Msg)
                    {
                        case PInvoke.WM_SETFOCUS:
                            return;
                        default:
                            base.WndProc(ref m);
                            break;
                    }
                }
            }
        }
    }
}
