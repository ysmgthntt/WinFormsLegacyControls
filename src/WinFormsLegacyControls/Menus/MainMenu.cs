// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    /// <summary>
    ///  Represents a menu structure for a form.
    /// </summary>
    //[ToolboxItemFilter("System.Windows.Forms.MainMenu")]
    public partial class MainMenu : Menu
    {
        internal Form? _form;
        internal Form? _ownerForm;  // this is the form that created this menu, and is the only form allowed to dispose it.
        private RightToLeft _rightToLeft = RightToLeft.Inherit;
        private static readonly object s_collapseEvent = new();

#if DEBUG
        internal string? _debugText;
#endif

        /// <summary>
        ///  Creates a new MainMenu control.
        /// </summary>
        public MainMenu()
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref='MainMenu'/> class with the specified container.
        /// </summary>
        public MainMenu(IContainer container)
        {
            ArgumentNullException.ThrowIfNull(container);

            container.Add(this);
        }

        /// <summary>
        ///  Creates a new MainMenu control with the given items to start
        ///  with.
        /// </summary>
        public MainMenu(MenuItem[] items)
            : base(items)
        {
        }

        [SRDescription(nameof(SR.MainMenuCollapseDescr))]
        public event EventHandler Collapse
        {
            add => Events.AddHandler(s_collapseEvent, value);
            remove => Events.RemoveHandler(s_collapseEvent, value);
        }

        /// <summary>
        ///  This is used for international applications where the language
        ///  is written from RightToLeft. When this property is true,
        ///  text alignment and reading order will be from right to left.
        /// </summary>
        // Add an AmbientValue attribute so that the Reset context menu becomes available in the Property Grid.
        [Localizable(true)]
        [AmbientValue(RightToLeft.Inherit)]
        [SRDescription(nameof(SR.MenuRightToLeftDescr))]
        public virtual RightToLeft RightToLeft
        {
            get
            {
                if (DesignMode) // add
                    return _rightToLeft;

                if (RightToLeft.Inherit == _rightToLeft)
                {
                    if (_form is not null)
                    {
                        return _form.RightToLeft;
                    }
                    else
                    {
                        return RightToLeft.Inherit;
                    }
                }
                else
                {
                    return _rightToLeft;
                }
            }
            set
            {

                //valid values are 0x0 to 0x2
                if (!ClientUtils.IsEnumValid(value, (int)value, (int)RightToLeft.No, (int)RightToLeft.Inherit))
                {
                    throw new InvalidEnumArgumentException(nameof(RightToLeft), (int)value, typeof(RightToLeft));
                }
                if (_rightToLeft != value)
                {
                    _rightToLeft = value;
                    UpdateRtl((value == RightToLeft.Yes));
                    // [fixed]
                    if (_form is not null)
                        PInvoke.DrawMenuBar(_form);
                }

            }
        }

        internal override bool RenderIsRightToLeft
            => (RightToLeft == RightToLeft.Yes && (_form is null || !_form.IsMirrored));

        /// <summary>
        ///  Creates a new MainMenu object which is a dupliate of this one.
        /// </summary>
        public virtual MainMenu CloneMenu()
        {
            MainMenu newMenu = new MainMenu();
            newMenu.CloneMenu(this);
            return newMenu;
        }

        protected override IntPtr CreateMenuHandle()
            => PInvoke.CreateMenu();

        /// <summary>
        ///  Clears out this MainMenu object and discards all of it's resources.
        ///  If the menu is parented in a form, it is disconnected from that as
        ///  well.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_form is not null && (_ownerForm is null || _form == _ownerForm))
                {
                    if (_form.TryGetMainMenuSupportFormNativeWindow(out var window))
                        window.Menu = null;
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///  Indicates which form in which we are currently residing [if any]
        /// </summary>
        public Form? GetForm() => _form;

        /*
        internal Form GetFormUnsafe()
        {
            return form;
        }
        */

        internal override void ItemsChanged(MenuChangeKind change)
        {
            base.ItemsChanged(change);
            _form?.MenuChanged(change, this);
        }

        internal void ItemsChanged(MenuChangeKind change, Menu menu)
            => _form?.MenuChanged(change, menu);

        /// <summary>
        ///  Fires the collapse event
        /// </summary>
        protected internal virtual void OnCollapse(EventArgs e)
            => ((EventHandler?)Events[s_collapseEvent])?.Invoke(this, e);

        /// <summary>
        ///  Returns true if the RightToLeft should be persisted in code gen.
        /// </summary>
        private bool ShouldSerializeRightToLeft() => RightToLeft.Inherit != _rightToLeft;

        /*
        /// <summary>
        ///  Returns a string representation for this control.
        /// </summary>
        public override string ToString()
        {
            // Removing GetForm information
            return base.ToString();
        }
        */
    }
}
