using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class UndoViewModel : ObservableObject
{
    private readonly Stack<UndoableAction> doneActions = [];
    private readonly Stack<UndoableAction> undoneActions = [];

    public class UndoableAction(string name, Action redo, Action undo)
    {
        public string Name { get; } = name;
        public Action Redo { get; } = redo;
        public Action Undo { get; } = undo;
    }

    public string? NextUndoName => doneActions.Count == 0 ? null : doneActions.Peek().Name;

    public string? NextRedoName => undoneActions.Count == 0 ? null : undoneActions.Peek().Name;

    /// <summary>
    /// Starts a new action, which prevents redoing the last undone action.
    /// </summary>
    public void StartToDo()
    {
        undoneActions.Clear();
    }

    public void Do(string name, Action redo, Action undo)
    {
        StartToDo();
        UndoableAction action = new(name, redo, undo);
        action.Redo();
        doneActions.Push(action);
        OnStacksChanged();
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (doneActions.Count == 0)
            return;
        UndoableAction action = doneActions.Pop();
        action.Undo();
        undoneActions.Push(action);
        OnStacksChanged();
    }
    private bool CanUndo() => doneActions.Count != 0;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (undoneActions.Count == 0)
            return;
        UndoableAction action = undoneActions.Pop();
        action.Redo();
        doneActions.Push(action);
        OnStacksChanged();
    }
    private bool CanRedo() => undoneActions.Count != 0;

    private void OnStacksChanged()
    {
        OnPropertyChanged(nameof(NextUndoName));
        OnPropertyChanged(nameof(NextRedoName));
        UndoCommand?.NotifyCanExecuteChanged();
        RedoCommand?.NotifyCanExecuteChanged();
    }
}
