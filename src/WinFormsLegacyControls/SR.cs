using System.Resources;
using System.Windows.Forms;

internal static class SR
{
    private static ResourceManager? s_resourceManager;

    internal static string GetResourceString(string value)
    {
        s_resourceManager ??= new ResourceManager("System.SR", typeof(Control).Assembly);
        return s_resourceManager.GetString(value) ?? value;
    }

    //
    internal static string CatAppearance => GetResourceString(nameof(CatAppearance));
    internal static string CatBehavior => GetResourceString(nameof(CatBehavior));
    internal static string CatData => GetResourceString(nameof(CatData));
    internal static string CatMouse => GetResourceString(nameof(CatMouse));
    internal static string CatPropertyChanged => GetResourceString(nameof(CatPropertyChanged));
    internal static string CatWindowStyle => GetResourceString(nameof(CatWindowStyle));
    //
    internal static string ControlTagDescr => nameof(ControlTagDescr);
    internal static string InvalidArgument => nameof(InvalidArgument);
    internal static string InvalidLowBoundArgumentEx => nameof(InvalidLowBoundArgumentEx);
    internal static string ObjectHasParent => nameof(ObjectHasParent);
    internal static string StatusBarAddFailed => nameof(StatusBarAddFailed);
    internal static string StatusBarBadStatusBarPanel => nameof(StatusBarBadStatusBarPanel);
    internal static string StatusBarDrawItem => nameof(StatusBarDrawItem);
    internal static string StatusBarOnPanelClickDescr => nameof(StatusBarOnPanelClickDescr);
    internal static string StatusBarPanelAlignmentDescr => nameof(StatusBarPanelAlignmentDescr);
    internal static string StatusBarPanelAutoSizeDescr => nameof(StatusBarPanelAutoSizeDescr);
    internal static string StatusBarPanelBorderStyleDescr => nameof(StatusBarPanelBorderStyleDescr);
    internal static string StatusBarPanelIconDescr => nameof(StatusBarPanelIconDescr);
    internal static string StatusBarPanelMinWidthDescr => nameof(StatusBarPanelMinWidthDescr);
    internal static string StatusBarPanelNameDescr => nameof(StatusBarPanelNameDescr);
    internal static string StatusBarPanelsDescr => nameof(StatusBarPanelsDescr);
    internal static string StatusBarPanelStyleDescr => nameof(StatusBarPanelStyleDescr);
    internal static string StatusBarPanelTextDescr => nameof(StatusBarPanelTextDescr);
    internal static string StatusBarPanelToolTipTextDescr => nameof(StatusBarPanelToolTipTextDescr);
    internal static string StatusBarPanelWidthDescr => nameof(StatusBarPanelWidthDescr);
    internal static string StatusBarShowPanelsDescr => nameof(StatusBarShowPanelsDescr);
    internal static string StatusBarSizingGripDescr => nameof(StatusBarSizingGripDescr);
    internal static string UnableToSetPanelText => nameof(UnableToSetPanelText);
    internal static string WidthGreaterThanMinWidth => nameof(WidthGreaterThanMinWidth);
    //
    internal static string ControlOnAutoSizeChangedDescr => nameof(ControlOnAutoSizeChangedDescr);
    internal static string ToolBarAppearanceDescr => nameof(ToolBarAppearanceDescr);
    internal static string ToolBarAutoSizeDescr => nameof(ToolBarAutoSizeDescr);
    internal static string ToolBarBadToolBarButton => nameof(ToolBarBadToolBarButton);
    internal static string ToolBarBorderStyleDescr => nameof(ToolBarBorderStyleDescr);
    internal static string ToolBarButtonClickDescr => nameof(ToolBarButtonClickDescr);
    internal static string ToolBarButtonDropDownDescr => nameof(ToolBarButtonDropDownDescr);
    internal static string ToolBarButtonsDescr => nameof(ToolBarButtonsDescr);
    internal static string ToolBarButtonSizeDescr => nameof(ToolBarButtonSizeDescr);
    internal static string ToolBarButtonEnabledDescr => nameof(ToolBarButtonEnabledDescr);
    internal static string ToolBarButtonImageIndexDescr => nameof(ToolBarButtonImageIndexDescr);
    internal static string ToolBarButtonInvalidDropDownMenuType => nameof(ToolBarButtonInvalidDropDownMenuType);
    internal static string ToolBarButtonMenuDescr => nameof(ToolBarButtonMenuDescr);
    internal static string ToolBarButtonNotFound => nameof(ToolBarButtonNotFound);
    internal static string ToolBarButtonPartialPushDescr => nameof(ToolBarButtonPartialPushDescr);
    internal static string ToolBarButtonPushedDescr => nameof(ToolBarButtonPushedDescr);
    internal static string ToolBarButtonStyleDescr => nameof(ToolBarButtonStyleDescr);
    internal static string ToolBarButtonTextDescr => nameof(ToolBarButtonTextDescr);
    internal static string ToolBarButtonToolTipTextDescr => nameof(ToolBarButtonToolTipTextDescr);
    internal static string ToolBarButtonVisibleDescr => nameof(ToolBarButtonVisibleDescr);
    internal static string ToolBarDividerDescr => nameof(ToolBarDividerDescr);
    internal static string ToolBarDropDownArrowsDescr => nameof(ToolBarDropDownArrowsDescr);
    internal static string ToolBarImageListDescr => nameof(ToolBarImageListDescr);
    internal static string ToolBarImageSizeDescr => nameof(ToolBarImageSizeDescr);
    internal static string ToolBarShowToolTipsDescr => nameof(ToolBarShowToolTipsDescr);
    internal static string ToolBarTextAlignDescr => nameof(ToolBarTextAlignDescr);
    internal static string ToolBarWrappableDescr => nameof(ToolBarWrappableDescr);
    //
    internal static string CommandIdNotAllocated => nameof(CommandIdNotAllocated);
    internal static string ContextMenuCollapseDescr => nameof(ContextMenuCollapseDescr);
    internal static string ContextMenuInvalidParent => nameof(ContextMenuInvalidParent);
    internal static string ContextMenuSourceControlDescr => nameof(ContextMenuSourceControlDescr);
    internal static string ControlHandleDescr => nameof(ControlHandleDescr);
    internal static string drawItemEventDescr => nameof(drawItemEventDescr);
    internal static string FindKeyMayNotBeEmptyOrNull => nameof(FindKeyMayNotBeEmptyOrNull);
    internal static string MainMenuCollapseDescr => nameof(MainMenuCollapseDescr);
    internal static string MDIMenuMoreWindows => GetResourceString(nameof(MDIMenuMoreWindows));
    internal static string measureItemEventDescr => nameof(measureItemEventDescr);
    internal static string MenuBadMenuItem => nameof(MenuBadMenuItem);
    internal static string MenuIsParentDescr => nameof(MenuIsParentDescr);
    internal static string MenuItemAlreadyExists => nameof(MenuItemAlreadyExists);
    internal static string MenuItemCheckedDescr => nameof(MenuItemCheckedDescr);
    internal static string MenuItemDefaultDescr => nameof(MenuItemDefaultDescr);
    internal static string MenuItemEnabledDescr => nameof(MenuItemEnabledDescr);
    internal static string MenuItemInvalidCheckProperty => nameof(MenuItemInvalidCheckProperty);
    internal static string MenuItemMDIListDescr => nameof(MenuItemMDIListDescr);
    internal static string MenuItemMergeOrderDescr => nameof(MenuItemMergeOrderDescr);
    internal static string MenuItemMergeTypeDescr => nameof(MenuItemMergeTypeDescr);
    internal static string MenuItemOnClickDescr => nameof(MenuItemOnClickDescr);
    internal static string MenuItemOnInitDescr => nameof(MenuItemOnInitDescr);
    internal static string MenuItemOnSelectDescr => nameof(MenuItemOnSelectDescr);
    internal static string MenuItemOwnerDrawDescr => nameof(MenuItemOwnerDrawDescr);
    internal static string MenuItemRadioCheckDescr => nameof(MenuItemRadioCheckDescr);
    internal static string MenuItemShortCutDescr => nameof(MenuItemShortCutDescr);
    internal static string MenuItemShowShortCutDescr => nameof(MenuItemShowShortCutDescr);
    internal static string MenuItemTextDescr => nameof(MenuItemTextDescr);
    internal static string MenuItemVisibleDescr => nameof(MenuItemVisibleDescr);
    internal static string MenuMDIListItemDescr => nameof(MenuMDIListItemDescr);
    internal static string MenuMenuItemsDescr => nameof(MenuMenuItemsDescr);
    internal static string MenuMergeWithSelf => nameof(MenuMergeWithSelf);
    internal static string MenuRightToLeftDescr => nameof(MenuRightToLeftDescr);
    internal static string TooManyResumeUpdateMenuHandles => nameof(TooManyResumeUpdateMenuHandles);
}
