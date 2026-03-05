using AuraNews.ViewModels;

namespace AuraNews.Views;

public partial class DiscoverPage : ContentPage
{
    public DiscoverPage(DiscoverViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}