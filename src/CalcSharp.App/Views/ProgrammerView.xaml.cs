using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CalcSharp.Core.Programmer;

namespace CalcSharp.App.Views;

public partial class ProgrammerView : UserControl, IModeView
{
    private static readonly Brush ActiveBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xC2, 0xFF));
    private static readonly Brush BitOnBrush = new SolidColorBrush(Color.FromRgb(0x3B, 0x3B, 0x3B));
    private static readonly Brush BitOffBrush = new SolidColorBrush(Color.FromRgb(0x2B, 0x2B, 0x2B));

    private readonly ProgrammerCalculator _calculator = new();
    private readonly Button[] _bitButtons = new Button[64];
    private readonly List<Button> _hexKeys = [];

    public ProgrammerView()
    {
        InitializeComponent();

        foreach (var button in Keypad.Children.OfType<Button>())
        {
            var tag = (string)button.Tag;
            if (tag.Length == 1 && tag[0] is >= 'A' and <= 'F' && tag != "C")
            {
                _hexKeys.Add(button);
            }
        }

        // The hex digit C is tagged "C_" to distinguish it from the Clear key.
        _hexKeys.AddRange(Keypad.Children.OfType<Button>().Where(b => (string)b.Tag == "C_"));

        foreach (var button in Keypad.Children.OfType<Button>()
                     .Concat(MemoryRow.Children.OfType<Button>()))
        {
            button.Click += (_, _) => Press((string)button.Tag);
        }

        foreach (var button in ((Grid)RadixPanel.Parent).Children.OfType<Grid>().SelectMany(g => g.Children.OfType<Button>()))
        {
            button.Click += (_, _) => Press((string)button.Tag);
        }

        BuildBitPanel();
        Refresh();
    }

    private void BuildBitPanel()
    {
        // four rows of sixteen bits, most significant first
        for (var row = 0; row < 4; row++)
        {
            var grid = new UniformGrid { Rows = 1, Columns = 16 };
            for (var col = 0; col < 16; col++)
            {
                var index = 63 - (row * 16 + col);
                var b = new Button
                {
                    Content = "0",
                    FontSize = 11,
                    Margin = new Thickness(col % 4 == 3 ? 1 : 0.5, 1, col % 4 == 3 ? 4 : 0.5, 1),
                    Padding = new Thickness(0),
                    Height = 22,
                    Background = BitOffBrush,
                    Tag = "bit:" + index,
                    ToolTip = index.ToString(),
                };
                b.Click += (_, _) => Press((string)b.Tag);
                _bitButtons[index] = b;
                grid.Children.Add(b);
            }

            BitPanel.Children.Add(grid);
        }
    }

    private void ShowMenu(UIElement target, IEnumerable<(string Label, string Key)> items, int columns)
    {
        MenuGrid.Children.Clear();
        MenuGrid.Columns = columns;
        foreach (var (label, key) in items)
        {
            var b = new Button { Content = label, Tag = key, FontSize = 14, Height = 34, Background = (Brush)FindResource("OperatorBrush") };
            b.Click += (_, _) =>
            {
                MenuPopup.IsOpen = false;
                Press(key);
            };
            MenuGrid.Children.Add(b);
        }

        MenuPopup.PlacementTarget = target;
        MenuPopup.IsOpen = true;
    }

    private void Press(string key)
    {
        if (key.StartsWith("radix:", StringComparison.Ordinal))
        {
            _calculator.SetRadix(Enum.Parse<Radix>(key[6..]));
        }
        else if (key.StartsWith("bit:", StringComparison.Ordinal))
        {
            _calculator.ToggleBit(int.Parse(key[4..]));
        }
        else if (key.StartsWith("shift:", StringComparison.Ordinal))
        {
            _calculator.ShiftMode = Enum.Parse<ShiftMode>(key[6..]);
        }
        else
        {
            switch (key)
            {
                case "+": _calculator.Operator(ProgrammerOp.Add); break;
                case "−" or "-": _calculator.Operator(ProgrammerOp.Subtract); break;
                case "×" or "*": _calculator.Operator(ProgrammerOp.Multiply); break;
                case "÷" or "/": _calculator.Operator(ProgrammerOp.Divide); break;
                case "Mod": _calculator.Operator(ProgrammerOp.Mod); break;
                case "AND": _calculator.Operator(ProgrammerOp.And); break;
                case "OR": _calculator.Operator(ProgrammerOp.Or); break;
                case "XOR": _calculator.Operator(ProgrammerOp.Xor); break;
                case "NAND": _calculator.Operator(ProgrammerOp.Nand); break;
                case "NOR": _calculator.Operator(ProgrammerOp.Nor); break;
                case "NOT": _calculator.Not(); break;
                case "Lsh": _calculator.Operator(ProgrammerOp.Lsh); break;
                case "Rsh": _calculator.Operator(ProgrammerOp.Rsh); break;
                case "RoL": _calculator.Operator(ProgrammerOp.RoL); break;
                case "RoR": _calculator.Operator(ProgrammerOp.RoR); break;
                case "RoLC": _calculator.Operator(ProgrammerOp.RoLC); break;
                case "RoRC": _calculator.Operator(ProgrammerOp.RoRC); break;
                case "(": _calculator.OpenParenthesis(); break;
                case ")": _calculator.CloseParenthesis(); break;
                case "=": _calculator.Evaluate(); break;
                case "±": _calculator.Negate(); break;
                case "⌫": _calculator.Backspace(); break;
                case "CE": _calculator.ClearEntry(); break;
                case "C": _calculator.Clear(); break;
                case "MC": _calculator.MemoryClear(); break;
                case "MR": _calculator.MemoryRecall(); break;
                case "MS": _calculator.MemoryStore(); break;
                case "M+": _calculator.MemoryAdd(); break;
                case "M-": _calculator.MemorySubtract(); break;
                case "word": _calculator.CycleWordSize(); break;
                case "bits": BitPanel.Visibility = BitPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; break;
                case "bitwise":
                    ShowMenu(BitwiseButton,
                        [("AND", "AND"), ("OR", "OR"), ("NOT", "NOT"), ("NAND", "NAND"), ("NOR", "NOR"), ("XOR", "XOR")], 3);
                    break;
                case "shiftmenu":
                    ShowMenu(ShiftButton,
                        [("Arithmetic", "shift:Arithmetic"), ("Logical", "shift:Logical"),
                         ("Rotate", "shift:RotateCircular"), ("Rotate carry", "shift:RotateThroughCarry")], 2);
                    break;
                case "C_":
                    _calculator.Digit(12);
                    break;
                default:
                    if (key.Length == 1 && Uri.IsHexDigit(key[0]))
                    {
                        _calculator.Digit(Convert.ToInt32(key, 16));
                    }

                    break;
            }
        }

        Refresh();
    }

    private void Refresh()
    {
        DisplayText.Text = _calculator.Display;
        ExpressionText.Text = _calculator.Expression;
        MemoryIndicator.Text = _calculator.HasMemory ? "M" : "";
        HexText.Text = _calculator.Hex;
        DecText.Text = _calculator.Dec;
        OctText.Text = _calculator.Oct;
        BinText.Text = _calculator.Bin;
        WordButton.Content = _calculator.WordSize.ToString().ToUpperInvariant();

        HexButton.Foreground = _calculator.Radix == Radix.Hex ? ActiveBrush : Brushes.White;
        DecButton.Foreground = _calculator.Radix == Radix.Dec ? ActiveBrush : Brushes.White;
        OctButton.Foreground = _calculator.Radix == Radix.Oct ? ActiveBrush : Brushes.White;
        BinButton.Foreground = _calculator.Radix == Radix.Bin ? ActiveBrush : Brushes.White;

        foreach (var child in Keypad.Children.OfType<Button>())
        {
            var tag = (string)child.Tag;
            if (tag == "C_" || (tag.Length == 1 && tag[0] is >= 'A' and <= 'F' && tag != "C"))
            {
                child.IsEnabled = _calculator.Radix == Radix.Hex;
            }
            else if (tag.Length == 1 && char.IsAsciiDigit(tag[0]))
            {
                child.IsEnabled = _calculator.IsDigitEnabled(tag[0] - '0');
            }
        }

        var rotate = _calculator.ShiftMode is ShiftMode.RotateCircular or ShiftMode.RotateThroughCarry;
        var carry = _calculator.ShiftMode == ShiftMode.RotateThroughCarry;
        LshButton.Content = rotate ? (carry ? "RoL" + "C" : "RoL") : "Lsh";
        LshButton.Tag = rotate ? (carry ? "RoLC" : "RoL") : "Lsh";
        RshButton.Content = rotate ? (carry ? "RoRC" : "RoR") : "Rsh";
        RshButton.Tag = rotate ? (carry ? "RoRC" : "RoR") : "Rsh";

        for (var i = 0; i < 64; i++)
        {
            var on = _calculator.GetBit(i);
            var b = _bitButtons[i];
            b.Content = on ? "1" : "0";
            b.Background = on ? BitOnBrush : BitOffBrush;
            b.IsEnabled = i < _calculator.Bits;
        }
    }

    public bool HandleTextInput(TextCompositionEventArgs e)
    {
        var text = e.Text;
        string? key = text switch
        {
            "+" => "+",
            "-" => "−",
            "*" => "×",
            "/" => "÷",
            "=" => "=",
            "%" => "Mod",
            "&" => "AND",
            "|" => "OR",
            "^" => "XOR",
            "~" => "NOT",
            "<" => "Lsh",
            ">" => "Rsh",
            "(" => "(",
            ")" => ")",
            "j" or "J" => "RoL",
            "k" or "K" => "RoR",
            _ => null,
        };

        if (key is null && text.Length == 1 && Uri.IsHexDigit(text[0]))
        {
            var digit = Convert.ToInt32(text, 16);
            if (_calculator.IsDigitEnabled(digit))
            {
                _calculator.Digit(digit);
                Refresh();
                return true;
            }

            return false;
        }

        if (key is null)
        {
            return false;
        }

        Press(key);
        return true;
    }

    public bool HandleKeyDown(KeyEventArgs e)
    {
        string? key;
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            key = e.Key switch
            {
                Key.M => "MS",
                Key.R => "MR",
                Key.P => "M+",
                Key.Q => "M-",
                Key.L => "MC",
                _ => null,
            };
        }
        else
        {
            key = e.Key switch
            {
                Key.Enter => "=",
                Key.Back => "⌫",
                Key.Escape => "C",
                Key.Delete => "CE",
                Key.F9 => "±",
                Key.F5 => "radix:Hex",
                Key.F6 => "radix:Dec",
                Key.F7 => "radix:Oct",
                Key.F8 => "radix:Bin",
                _ => null,
            };
        }

        if (key is null)
        {
            return false;
        }

        Press(key);
        return true;
    }
}
