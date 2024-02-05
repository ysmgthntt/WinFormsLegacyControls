// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    partial class MenuItem
    {
        private class MdiListUserData
        {
            public virtual void OnClick(EventArgs e)
            {
            }
        }

        private sealed class MdiListFormData : MdiListUserData
        {
            private readonly MenuItem _parent;
            private readonly int _boundIndex;

            public MdiListFormData(MenuItem parentItem, int boundFormIndex)
            {
                _boundIndex = boundFormIndex;
                _parent = parentItem;
            }

            public override void OnClick(EventArgs e)
            {
                if (_boundIndex != -1)
                {
                    Form[] forms = _parent.FindMdiForms(out _);
                    Debug.Assert(forms is not null, "Didn't get a list of the MDI Forms.");

                    if (forms is not null && forms.Length > _boundIndex)
                    {
                        Form boundForm = forms[_boundIndex];
                        boundForm.Activate();
                        if (boundForm.ActiveControl is not null && !boundForm.ActiveControl.Focused)
                        {
                            boundForm.ActiveControl.Focus();
                        }
                    }
                }
            }
        }

        private sealed class MdiListMoreWindowsData : MdiListUserData
        {
            private readonly MenuItem _parent;

            public MdiListMoreWindowsData(MenuItem parent)
            {
                _parent = parent;
            }

            public override void OnClick(EventArgs e)
            {
                Form[] forms = _parent.FindMdiForms(out var active);
                Debug.Assert(forms is not null, "Didn't get a list of the MDI Forms.");
                //Form active = _parent.GetMainMenu()./*GetFormUnsafe()*/form.ActiveMdiChild;
                Debug.Assert(active is not null, "Didn't get the active MDI child");
                if (forms is { Length: > 0 } && active is not null)
                {
                    Type? t = Type.GetType("System.Windows.Forms.MdiWindowDialog, System.Windows.Forms");
                    if (t is not null)
                    {
                        MethodInfo? miSetItems = t.GetMethod("SetItems");
                        if (miSetItems is not null)
                        {
                            PropertyInfo? piActiveChildForm = t.GetProperty("ActiveChildForm");
                            if (piActiveChildForm is not null)
                            {
                                using Form dialog = (Form)Activator.CreateInstance(t)!;
                                miSetItems.Invoke(dialog, [active, forms]);
                                if (dialog.ShowDialog() == DialogResult.OK)
                                {
                                    active = (Form)piActiveChildForm.GetValue(dialog)!;
                                    goto activate;
                                }
                                return;
                            }
                        }
                    }

                    using (var dialog = new MdiWindowDialog())
                    {
                        dialog.SetItems(active, forms);
                        DialogResult result = dialog.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            active = dialog.ActiveChildForm!;
                        }
                        else
                        {
                            return;
                        }
                    }

                activate:
                    active.Activate();
                    if (active.ActiveControl is not null && !active.ActiveControl.Focused)
                    {
                        active.ActiveControl.Focus();
                    }
                }
            }
        }
    }
}
