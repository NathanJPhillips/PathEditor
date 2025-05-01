namespace NobleTech.Products.PathEditor.ViewModels;

internal interface INavigationService
{
    void ShowWindow(NavigationDestinations destination, object viewModel);
    void ReplaceWindow(NavigationDestinations destination, object viewModel);
    bool? ShowDialog(NavigationDestinations destination, object viewModel);
    bool? DialogResult { get; set; }
    void Close();
}
