using System.ComponentModel;
using System.Windows.Input;

namespace WinFormsLegacyControls;

partial class ToolBarButton
{
    private ICommand? _command;
    private object? _commandParameter;

    /// <summary>
    ///  Gets or sets the <see cref="ICommand"/> whose <see cref="ICommand.Execute(object?)"/>
    ///  method will be called when the <see cref="ToolBar.ButtonClick"/> event gets invoked.
    /// </summary>
    [Bindable(true)]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRCategory(nameof(SR.CatData))]
    [SRDescription(nameof(SR.CommandComponentCommandDescr))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ICommand? Command
    {
        get => _command;
        set
        {
            if (!object.Equals(_command, value))
            {
                if (_command is not null)
                {
                    _command.CanExecuteChanged -= OnCanExecuteChanged;
                }
                _command = value;
                if (_command is not null)
                {
                    _command.CanExecuteChanged += OnCanExecuteChanged;
                    Enabled = _command.CanExecute(_commandParameter);
                }
            }
        }
    }

    /// <summary>
    ///  Gets or sets the parameter that is passed to the <see cref="ICommand"/>
    ///  which is assigned to the <see cref="Command"/> property.
    /// </summary>
    [Bindable(true)]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRCategory(nameof(SR.CatData))]
    [SRDescription(nameof(SR.CommandComponentCommandParameterDescr))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public object? CommandParameter
    {
        get => _commandParameter;
        set
        {
            if (!object.Equals(_commandParameter, value))
            {
                _commandParameter = value;
                if (_command is not null)
                {
                    Enabled = _command.CanExecute(_commandParameter);
                }
            }
        }
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e)
    {
        Enabled = _command!.CanExecute(_commandParameter);
    }
}
