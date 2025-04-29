namespace NobleTech.Products.PathEditor.ViewModels;

internal class MessageBoxViewModel
{
    public required string Title { get; set; }

    public required string Message { get; set; }

    public Buttons ButtonsToShow { get; set; } = Buttons.OK;

    public Buttons DefaultButton { get; set; } = Buttons.OK;

    public Buttons? SelectedButton { get; set; }

    public Images Image { get; set; } = Images.None;

    public enum Buttons
    {
        OK = 1,
        Yes = 2,
        No = 4,
        Cancel = 8,
    }

    // Specifies the icon that is displayed by a message box.
    public enum Images
    {
        // The message box contains no symbols.
        None,
        // The message box contains a symbol consisting of white X in a circle with a red background.
        Error,
        // The message box contains a symbol consisting of an exclamation point in a triangle with
        // a yellow background.
        Warning,
        // The message box contains a symbol consisting of a lowercase letter i in a circle.
        Information,
    }
}
