#if WINFORMS_NAMESPACE
namespace System.Windows.Forms
#else
namespace WinFormsLegacyControls
#endif
{
    partial class ToolBar
    {
        // Control.cs
        // BeginUpdateInternal, EndUpdateInternal はサブクラスからのみ使用されているため、安全に使える。

        private short _updateCount;

        private void BeginUpdateInternal()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            if (_updateCount == 0)
            {
                PInvoke.SendMessage(this, PInvoke.WM_SETREDRAW, (WPARAM)(BOOL)false);
            }

            _updateCount++;
        }

        private bool EndUpdateInternal()
        {
            return EndUpdateInternal(true);
        }

        private bool EndUpdateInternal(bool invalidate)
        {
            if (_updateCount > 0)
            {
                Debug.Assert(IsHandleCreated, "Handle should be created by now");
                _updateCount--;
                if (_updateCount == 0)
                {
                    PInvoke.SendMessage(this, PInvoke.WM_SETREDRAW, (WPARAM)(BOOL)true);
                    if (invalidate)
                    {
                        Invalidate();
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
