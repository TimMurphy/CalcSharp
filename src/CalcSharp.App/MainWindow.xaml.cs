using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CalcSharp.App.Views;

namespace CalcSharp.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var view = new ListCollectionView(ModeCatalog.Modes.ToList());
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ModeInfo.Group)));
        NavList.ItemsSource = view;
        NavList.SelectedIndex = 0;
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e) =>
        NavPanel.Visibility = NavPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    private void NavList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ModeInfo mode)
        {
            return;
        }

        Host.Content = mode.CreateView();
        TitleText.Text = mode.Title;
        NavPanel.Visibility = Visibility.Collapsed;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Host.Content is IModeView view && view.HandleKeyDown(e))
        {
            e.Handled = true;
        }
    }

    private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (Host.Content is IModeView view && view.HandleTextInput(e))
        {
            e.Handled = true;
        }
    }
}
