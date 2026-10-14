using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CalcSharp.Core.Converters;

namespace CalcSharp.App.Views;

public partial class ConverterView : UserControl, IModeView
{
    private static readonly Brush ActiveBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly Brush InactiveBrush = new SolidColorBrush(Color.FromRgb(0xC5, 0xC5, 0xC5));

    private readonly UnitConverter _converter;
    private bool _loading = true;

    public ConverterView(string category)
    {
        InitializeComponent();
        _converter = new UnitConverter(category);

        foreach (var unit in _converter.Category.Units)
        {
            FromUnit.Items.Add(unit.Name);
            ToUnit.Items.Add(unit.Name);
        }

        FromUnit.SelectedIndex = _converter.Category.DefaultFrom;
        ToUnit.SelectedIndex = _converter.Category.DefaultTo;
        _loading = false;

        foreach (var button in Keypad.Children.OfType<Button>())
        {
            button.Click += (_, _) => Press((string)button.Tag);
        }

        if (_converter.StatusText is { } status)
        {
            StatusText.Text = status;
            StatusText.Visibility = System.Windows.Visibility.Visible;
        }

        Refresh();
    }

    private void Press(string key)
    {
        switch (key)
        {
            case "": return;
            case "CE" or "C": _converter.Clear(); break;
            case "⌫": _converter.Backspace(); break;
            case "±": _converter.Negate(); break;
            case ".": _converter.DecimalPoint(); break;
            default:
                if (key.Length == 1 && char.IsAsciiDigit(key[0]))
                {
                    _converter.Digit(key[0] - '0');
                }

                break;
        }

        Refresh();
    }

    private void Refresh()
    {
        FromText.Text = _converter.FromDisplay;
        ToText.Text = _converter.ToDisplay;
        FromText.Foreground = _converter.ActiveField == 0 ? ActiveBrush : InactiveBrush;
        ToText.Foreground = _converter.ActiveField == 1 ? ActiveBrush : InactiveBrush;
    }

    private void FromPanel_Click(object sender, MouseButtonEventArgs e)
    {
        _converter.SetActiveField(0);
        Refresh();
    }

    private void ToPanel_Click(object sender, MouseButtonEventArgs e)
    {
        _converter.SetActiveField(1);
        Refresh();
    }

    private void FromUnit_Changed(object sender, SelectionChangedEventArgs e) => UnitChanged(0, FromUnit);

    private void ToUnit_Changed(object sender, SelectionChangedEventArgs e) => UnitChanged(1, ToUnit);

    private void UnitChanged(int field, ComboBox box)
    {
        if (_loading || box.SelectedIndex < 0)
        {
            return;
        }

        _converter.SetUnit(field, _converter.Category.Units[box.SelectedIndex]);
        Refresh();
    }

    public bool HandleTextInput(TextCompositionEventArgs e)
    {
        if (e.Text.Length != 1)
        {
            return false;
        }

        if (char.IsAsciiDigit(e.Text[0]) || e.Text == ".")
        {
            Press(e.Text);
            return true;
        }

        return false;
    }

    public bool HandleKeyDown(KeyEventArgs e)
    {
        var key = e.Key switch
        {
            Key.Back => "⌫",
            Key.Escape => "CE",
            Key.Delete => "CE",
            Key.F9 => "±",
            _ => null,
        };

        if (key is null)
        {
            return false;
        }

        Press(key);
        return true;
    }
}
