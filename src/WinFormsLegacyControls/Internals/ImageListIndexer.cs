using System.Windows.Forms;

internal class ImageListIndexer
{
    private string key = string.Empty;
    private int index = -1;
    private bool useIntegerIndex = true;
    private ImageList imageList = null;

    public virtual ImageList ImageList
    {
        get { return imageList; }
        set { imageList = value; }
    }

    public virtual string Key
    {
        get { return key; }
        set
        {
            index = -1;
            key = (value ?? string.Empty);
            useIntegerIndex = false;
        }
    }

    public virtual int Index
    {
        get { return index; }
        set
        {
            key = string.Empty;
            index = value;
            useIntegerIndex = true;
        }

    }

    public virtual int ActualIndex
    {
        get
        {
            if (useIntegerIndex)
            {
                return Index;
            }
            else if (ImageList != null)
            {
                return ImageList.Images.IndexOfKey(Key);
            }

            return -1;
        }
    }
}
