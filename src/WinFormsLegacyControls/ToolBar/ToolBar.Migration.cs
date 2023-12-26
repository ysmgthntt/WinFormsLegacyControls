using System.Reflection;

namespace System.Windows.Forms
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

        // internal methods

        private const int STATE_DISPOSING = 0x00001000;

        private static MethodInfo? _miSetState;

        private void SetState(int flag, bool value)
        {
            if (_miSetState is null)
                _miSetState = typeof(Control).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);

            _miSetState!.Invoke(this, [flag, value]);
        }
    }
}
