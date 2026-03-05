using AuraNews.ViewModels;
using Microsoft.Maui.Controls;

namespace AuraNews.Views;

public partial class ReaderPage : ContentPage
{
    public ReaderPage(ReaderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
