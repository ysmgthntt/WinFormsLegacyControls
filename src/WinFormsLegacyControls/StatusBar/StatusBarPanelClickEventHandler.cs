﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    /// <summary>
    ///  Represents a method that will handle the <see cref='StatusBar.OnPanelClick'/>
    ///  event of a <see cref='StatusBar'/>.
    /// </summary>
    public delegate void StatusBarPanelClickEventHandler(object sender, StatusBarPanelClickEventArgs e);
}
