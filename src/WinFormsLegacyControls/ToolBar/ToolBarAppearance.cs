// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    /// <summary>
    ///  Specifies the type of toolbar to display.
    /// </summary>
    public enum ToolBarAppearance
    {
        /// <summary>
        ///  The toolbar and buttons appear as standard three dimensional controls.
        /// </summary>
        Normal = 0,

        /// <summary>
        ///  The toolbar and buttons appear flat, but the buttons change to three
        ///  dimensional as the mouse pointer moves over them.
        /// </summary>
        Flat = 1,
    }
}
