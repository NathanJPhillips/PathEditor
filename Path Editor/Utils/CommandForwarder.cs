using System.Diagnostics;
using System.Windows.Input;

namespace NobleTech.Products.PathEditor.Utils;

internal class CommandForwarder<TViewModel>(Func<TViewModel, ICommand> getCommand)
{
    public void ExecuteCommand(object dataContext, ExecutedRoutedEventArgs e)
    {
        if (dataContext is not TViewModel viewModel)
        {
            Debug.WriteLine($"CommandForwarder: DataContext is not of type {typeof(TViewModel).Name}, silently ignoring command.");
            return;
        }
        ICommand command = getCommand(viewModel);
        if (command?.CanExecute(e.Parameter) == true)
            command.Execute(e.Parameter);
    }

    public void CanExecuteCommand(object dataContext, CanExecuteRoutedEventArgs e)
    {
        if (dataContext is not TViewModel viewModel)
        {
            Debug.WriteLine($"CommandForwarder: DataContext is not of type {typeof(TViewModel).Name}, silently ignoring command.");
            return;
        }
        e.CanExecute = getCommand(viewModel).CanExecute(e.Parameter);
    }
}
