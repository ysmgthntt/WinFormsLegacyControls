using System.Windows.Forms;

internal class ImageListIndexer
{
    private string _key = string.Empty;
    private int _index = -1;
    private bool _useIntegerIndex = true;
    private ImageList? _imageList;

    public virtual ImageList? ImageList
    {
        get => _imageList;
        set => _imageList = value;
    }

    public virtual string Key
    {
        get => _key;
        set
        {
            _index = -1;
            _key = (value ?? string.Empty);
            _useIntegerIndex = false;
        }
    }

    public virtual int Index
    {
        get => _index;
        set
        {
            _key = string.Empty;
            _index = value;
            _useIntegerIndex = true;
        }

    }

    public virtual int ActualIndex
    {
        get
        {
            if (_useIntegerIndex)
            {
                return Index;
            }
            else if (ImageList is not null)
            {
                return ImageList.Images.IndexOfKey(Key);
            }

            return -1;
        }
    }
}
