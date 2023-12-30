namespace WinFormsLegacyControls
{
    internal enum MenuChangeKind
    {
        CHANGE_ITEMS = 0,       // item(s) added or removed
        CHANGE_VISIBLE = 1,     // item(s) hidden or shown
        CHANGE_MDI = 2,         // mdi item changed
        CHANGE_MERGE = 3,       // mergeType or mergeOrder changed
        CHANGE_ITEMADDED = 4,   // mergeType or mergeOrder changed
    }
}
