using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NobleTech.Products.PathEditor.ViewModels;

/// <summary>
/// ViewModel for managing undo and redo actions.
/// </summary>
/// <remarks>
/// This class provides functionality to manage undo and redo operations using stacks.
/// It tracks the actions performed and undone, allowing users to revert or reapply changes.
/// </remarks>
internal partial class UndoViewModel : ObservableObject
{
    private readonly Stack<UndoableAction> doneActions = [];
    private readonly Stack<UndoableAction> undoneActions = [];

    /// <summary>
    /// Represents an action that can be undone and redone.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    /// <param name="redo">The action to execute for redoing.</param>
    /// <param name="undo">The action to execute for undoing.</param>
    private class UndoableAction(string name, Action redo, Action undo)
    {
        /// <summary>
        /// The name of the action.
        /// </summary>
        public string Name { get; } = name;
        /// <summary>
        /// The action to execute for doing/redoing.
        /// </summary>
        public Action Redo { get; } = redo;
        /// <summary>
        /// The action to execute for undoing.
        /// </summary>
        public Action Undo { get; } = undo;
    }

    /// <summary>
    /// Gets the name of the next action that can be undone.
    /// </summary>
    /// <value>
    /// The name of the next undoable action, or <c>null</c> if no actions are available to undo.
    /// </value>
    public string? NextUndoName => doneActions.Count == 0 ? null : doneActions.Peek().Name;

    /// <summary>
    /// Gets the name of the next action that can be redone.
    /// </summary>
    /// <value>
    /// The name of the next redoable action, or <c>null</c> if no actions are available to redo.
    /// </value>
    public string? NextRedoName => undoneActions.Count == 0 ? null : undoneActions.Peek().Name;

    /// <summary>
    /// Executes a new action, adding it to the undo stack and clearing the redo stack.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    /// <param name="redo">The action to execute for redoing.</param>
    /// <param name="undo">The action to execute for undoing.</param>
    public void Do(string name, Action redo, Action undo)
    {
        undoneActions.Clear();
        UndoableAction action = new(name, redo, undo);
        action.Redo();
        doneActions.Push(action);
        OnStacksChanged();
    }

    /// <summary>
    /// Undoes the last action, moving it to the redo stack.
    /// </summary>
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
    /// <summary>
    /// Determines whether an undo operation can be performed.
    /// </summary>
    /// <returns>Whether there are actions to undo.</returns>
    private bool CanUndo() => doneActions.Count != 0;

    /// <summary>
    /// Redoes the last undone action, moving it back to the undo stack.
    /// </summary>
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
    /// <summary>
    /// Determines whether a redo operation can be performed.
    /// </summary>
    /// <returns>Whether there are actions to redo.</returns>
    private bool CanRedo() => undoneActions.Count != 0;

    /// <summary>
    /// Notifies changes in the undo and redo stacks, updating related properties and commands.
    /// </summary>
    private void OnStacksChanged()
    {
        OnPropertyChanged(nameof(NextUndoName));
        OnPropertyChanged(nameof(NextRedoName));
        UndoCommand?.NotifyCanExecuteChanged();
        RedoCommand?.NotifyCanExecuteChanged();
    }
}
