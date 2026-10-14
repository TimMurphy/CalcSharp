using System.Windows.Controls;
using System.Windows.Input;
using CalcSharp.Core;

namespace CalcSharp.App.Views;

public partial class StandardView : UserControl, IModeView
{
    private readonly StandardCalculator _calculator = new();

    public StandardView()
    {
        InitializeComponent();
        foreach (var button in Keypad.Children.OfType<Button>().Concat(MemoryRow.Children.OfType<Button>()))
        {
            button.Click += (_, _) => Press((string)button.Tag);
        }
    }

    private void Press(string key)
    {
        switch (key)
        {
            case "+": _calculator.Operator(Operation.Add); break;
            case "−" or "-": _calculator.Operator(Operation.Subtract); break;
            case "×" or "*": _calculator.Operator(Operation.Multiply); break;
            case "÷" or "/": _calculator.Operator(Operation.Divide); break;
            case "=": _calculator.Evaluate(); break;
            case ".": _calculator.DecimalPoint(); break;
            case "±": _calculator.Negate(); break;
            case "%": _calculator.Percent(); break;
            case "√": _calculator.SquareRoot(); break;
            case "x²": _calculator.Square(); break;
            case "1/x": _calculator.Reciprocal(); break;
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

        DisplayText.Text = _calculator.Display;
        ExpressionText.Text = _calculator.Expression;
        MemoryIndicator.Text = _calculator.HasMemory ? "M" : "";
    }

    public bool HandleTextInput(TextCompositionEventArgs e)
    {
        if (e.Text == "@")
        {
            Press("√");
            return true;
        }

        if (e.Text is "+" or "-" or "*" or "/" or "." or "%" or "=" ||
            (e.Text.Length == 1 && char.IsAsciiDigit(e.Text[0])))
        {
            Press(e.Text);
            return true;
        }

        return false;
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
