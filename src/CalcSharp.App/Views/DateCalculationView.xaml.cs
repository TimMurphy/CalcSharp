using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CalcSharp.Core.DateCalculation;

namespace CalcSharp.App.Views;

public partial class DateCalculationView : UserControl, IModeView
{
    public DateCalculationView()
    {
        InitializeComponent();
        var today = DateTime.Today;
        DiffFrom.SelectedDate = today;
        DiffTo.SelectedDate = today;
        AddFrom.SelectedDate = today;
        foreach (var (combo, max) in new[] { (YearsCombo, 999), (MonthsCombo, 11), (DaysCombo, 999) })
        {
            combo.ItemsSource = Enumerable.Range(0, max + 1).ToList();
            combo.SelectedIndex = 0;
        }

        Update();
    }

    public bool HandleKeyDown(KeyEventArgs e) => false;

    public bool HandleTextInput(TextCompositionEventArgs e) => false;

    private void OnChanged(object sender, RoutedEventArgs e) => Update();

    private void Update()
    {
        if (!IsInitialized || DaysCombo is null || DaysCombo.SelectedItem is null)
        {
            return;
        }

        var diffMode = ModeCombo.SelectedIndex == 0;
        DiffPanel.Visibility = diffMode ? Visibility.Visible : Visibility.Collapsed;
        AddPanel.Visibility = diffMode ? Visibility.Collapsed : Visibility.Visible;

        if (diffMode)
        {
            var from = DiffFrom.SelectedDate is { } f ? DateOnly.FromDateTime(f) : DateOnly.FromDateTime(DateTime.Today);
            var to = DiffTo.SelectedDate is { } t ? DateOnly.FromDateTime(t) : DateOnly.FromDateTime(DateTime.Today);
            var diff = DateCalculator.Difference(from, to);
            DiffResult.Text = diff.Text;
            DiffDays.Text = diff.DaysText ?? "";
        }
        else
        {
            var from = AddFrom.SelectedDate is { } f ? DateOnly.FromDateTime(f) : DateOnly.FromDateTime(DateTime.Today);
            var result = DateCalculator.Offset(
                from,
                (int)YearsCombo.SelectedItem,
                (int)MonthsCombo.SelectedItem,
                (int)DaysCombo.SelectedItem,
                SubtractOption.IsChecked == true);
            DateResult.Text = result.Text;
        }
    }
}
