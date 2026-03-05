using AuraNews.ViewModels;
using Microsoft.Maui.Controls;
using System;

namespace AuraNews.Views;

public partial class FeedPage : ContentPage
{
    private bool _isFirstLoad = true;

    public FeedPage(FeedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnNewArticlesButtonClicked(object sender, EventArgs e)
    {
        // Immer ganz nach oben scrollen!
        FeedCollectionView?.ScrollTo(0, position: ScrollToPosition.Start, animate: true);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var viewModel = BindingContext as FeedViewModel;

        if (_isFirstLoad)
        {
            _isFirstLoad = false;
        }

        // Frische Artikel werden lautlos im Hintergrund geladen
        _ = viewModel?.LoadFeedAsync();
    }

    private void OnFeedScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
    }
}