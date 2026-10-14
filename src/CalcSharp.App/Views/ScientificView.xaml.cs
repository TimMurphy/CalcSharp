using System.Windows.Controls;
using System.Windows.Input;
using CalcSharp.Core.Scientific;

namespace CalcSharp.App.Views;

public partial class ScientificView : UserControl, IModeView
{
    private readonly ScientificCalculator _calculator = new();
    private readonly Dictionary<string, Button> _buttons = new();

    public ScientificView()
    {
        InitializeComponent();
        foreach (var button in Keypad.Children.OfType<Button>()
                     .Concat(MemoryRow.Children.OfType<Button>())
                     .Concat(ModeRow.Children.OfType<Button>())
                     .Concat(TrigRow.Children.OfType<Button>()))
        {
            var tag = (string)button.Tag;
            _buttons[tag] = button;
            button.Click += (_, _) => Press(tag);
        }

        Refresh();
    }

    private void Press(string key)
    {
        var inv = _calculator.Inverse;
        switch (key)
        {
            case "+": _calculator.Operator(ScientificOperator.Add); break;
            case "−" or "-": _calculator.Operator(ScientificOperator.Subtract); break;
            case "×" or "*": _calculator.Operator(ScientificOperator.Multiply); break;
            case "÷" or "/": _calculator.Operator(ScientificOperator.Divide); break;
            case "mod": _calculator.Operator(ScientificOperator.Mod); break;
            case "xy":
                _calculator.Operator(inv ? ScientificOperator.Root : ScientificOperator.Power);
                break;
            case "log" when inv: _calculator.Operator(ScientificOperator.LogBase); break;
            case "=": _calculator.Evaluate(); break;
            case ".": _calculator.DecimalPoint(); break;
            case "(": _calculator.OpenParenthesis(); break;
            case ")": _calculator.CloseParenthesis(); break;
            case "±": _calculator.Negate(); break;
            case "x2":
                if (inv) _calculator.Cube(); else _calculator.Square();
                break;
            case "cube": _calculator.Cube(); break;
            case "sqrt":
                if (inv) _calculator.CubeRoot(); else _calculator.SquareRoot();
                break;
            case "10x":
                if (inv) _calculator.TwoPower(); else _calculator.TenPower();
                break;
            case "log": _calculator.Log(); break;
            case "ln":
                if (inv) _calculator.EPower(); else _calculator.Ln();
                break;
            case "n!": _calculator.Factorial(); break;
            case "1/x": _calculator.Reciprocal(); break;
            case "abs": _calculator.Abs(); break;
            case "exp": _calculator.Exp(); break;
            case "pi": _calculator.Pi(); break;
            case "e": _calculator.E(); break;
            case "sin": _calculator.Trig(TrigFunction.Sin); break;
            case "cos": _calculator.Trig(TrigFunction.Cos); break;
            case "tan": _calculator.Trig(TrigFunction.Tan); break;
            case "sec": _calculator.Trig(TrigFunction.Sec); break;
            case "csc": _calculator.Trig(TrigFunction.Csc); break;
            case "cot": _calculator.Trig(TrigFunction.Cot); break;
            case "dms": _calculator.Dms(); break;
            case "deg": _calculator.Degrees(); break;
            case "2nd": _calculator.ToggleInverse(); break;
            case "hyp": _calculator.ToggleHyperbolic(); break;
            case "angle": _calculator.CycleAngleMode(); break;
            case "DEG": _calculator.AngleMode = AngleMode.Degrees; break;
            case "RAD": _calculator.AngleMode = AngleMode.Radians; break;
            case "GRAD": _calculator.AngleMode = AngleMode.Gradians; break;
            case "fe": _calculator.ToggleFixedExponent(); break;
            case "⌫": _calculator.Backspace(); break;
            case "CE": _calculator.ClearEntry(); break;
            case "C": _calculator.Clear(); break;
            case "MC": _calculator.MemoryClear(); break;
            case "MR": _calculator.MemoryRecall(); break;
            case "MS": _calculator.MemoryStore(); break;
            case "M+": _calculator.MemoryAdd(); break;
            case "M-": _calculator.MemorySubtract(); break;
            default:
                if (key.Length == 1 && char.IsAsciiDigit(key[0]))
                {
                    _calculator.Digit(key[0] - '0');
                }

                break;
        }

        Refresh();
    }

    private void Refresh()
    {
        DisplayText.Text = _calculator.Display;
        var parens = _calculator.OpenParenCount > 0 ? $"   ({_calculator.OpenParenCount})" : "";
        ExpressionText.Text = _calculator.Expression + parens;
        MemoryIndicator.Text = _calculator.HasMemory ? "M" : "";

        var inv = _calculator.Inverse;
        var hyp = _calculator.Hyperbolic;
        SetLabel("x2", inv ? "x³" : "x²");
        SetLabel("sqrt", inv ? "³√x" : "²√x");
        SetLabel("xy", inv ? "ʸ√x" : "xʸ");
        SetLabel("10x", inv ? "2ˣ" : "10ˣ");
        SetLabel("log", inv ? "logᵧx" : "log");
        SetLabel("ln", inv ? "eˣ" : "ln");
        SetLabel("angle", _calculator.AngleMode switch
        {
            AngleMode.Degrees => "DEG",
            AngleMode.Radians => "RAD",
            _ => "GRAD",
        });
        foreach (var name in new[] { "sin", "cos", "tan", "sec", "csc", "cot" })
        {
            SetLabel(name, name + (inv ? "⁻¹" : "") + (hyp ? "h" : ""));
        }

        Highlight("2nd", inv);
        Highlight("hyp", hyp);
        Highlight("fe", _calculator.FixedExponent);
    }

    private void SetLabel(string tag, string text) => _buttons[tag].Content = text;

    private void Highlight(string tag, bool on) =>
        _buttons[tag].Foreground = on
            ? (System.Windows.Media.Brush)FindResource("EqualsBrush")
            : System.Windows.Media.Brushes.White;

    public bool HandleTextInput(TextCompositionEventArgs e)
    {
        var key = e.Text switch
        {
            "@" => "sqrt",
            "!" => "n!",
            "^" => "xy",
            "%" => "mod",
            "|" => "abs",
            "#" => "cube",
            "p" or "P" => "pi",
            "+" or "-" or "*" or "/" or "." or "=" or "(" or ")" => e.Text,
            "n" or "N" => "ln",
            "l" or "L" => "log",
            "s" => "sin",
            "o" => "cos",
            "t" => "tan",
            "q" or "Q" => "x2",
            "y" or "Y" => "xy",
            "m" or "M" => "dms",
            "x" or "X" => "exp",
            "v" or "V" => "fe",
            "d" or "D" => "angle",
            "h" or "H" => "hyp",
            "j" or "J" => "2nd",
            _ => null,
        };

        if (key is null && e.Text.Length == 1 && char.IsAsciiDigit(e.Text[0]))
        {
            key = e.Text;
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
        var key = Keyboard.Modifiers == ModifierKeys.Control
            ? e.Key switch
            {
                Key.M => "MS",
                Key.R => "MR",
                Key.P => "M+",
                Key.Q => "M-",
                Key.L => "MC",
                _ => null,
            }
            : e.Key switch
            {
                Key.Enter => "=",
                Key.Back => "⌫",
                Key.Escape => "C",
                Key.Delete => "CE",
                Key.R => "1/x",
                Key.F9 => "±",
                Key.F3 => "DEG",
                Key.F4 => "RAD",
                Key.F5 => "GRAD",
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
