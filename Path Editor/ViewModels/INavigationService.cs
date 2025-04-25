namespace NobleTech.Products.PathEditor.ViewModels;

internal interface INavigationService
{
    void ShowWindow(string windowName, object viewModel);
    bool? ShowDialog(string windowName, object viewModel);
    bool? DialogResult { get; set; }
    void Close();
}
