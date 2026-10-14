using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CalcSharp.Core.Graphing;

namespace CalcSharp.App.Views;

public partial class GraphingView : UserControl, IModeView
{
    private sealed class Row
    {
        public required TextBox Input;
        public required TextBlock Error;
        public required Button Toggle;
        public required Border Dot;
        public bool Visible = true;
        public GraphEquation? Equation;
        public required Brush Colour;
    }

    private readonly Viewport _viewport = new();
    private readonly List<Row> _rows = [];
    private bool _dragging;
    private Point _lastDrag;

    public GraphingView()
    {
        InitializeComponent();
        AddRow("");
    }

    public bool HandleKeyDown(KeyEventArgs e) => false;

    public bool HandleTextInput(TextCompositionEventArgs e) => false;

    private void AddRow(string text)
    {
        var (name, hex) = EquationPalette.For(_rows.Count);
        var brush = (Brush)new BrushConverter().ConvertFromString(hex)!;
        var dot = new Border
        {
            Width = 16, Height = 16, CornerRadius = new CornerRadius(8), Background = brush,
        };
        var toggle = new Button
        {
            Content = dot, Width = 32, Height = 32, Background = Brushes.Transparent,
            ToolTip = "Hide equation", Margin = new Thickness(0, 0, 4, 0),
        };
        System.Windows.Automation.AutomationProperties.SetName(toggle, name);
        var input = new TextBox
        {
            Text = text, FontSize = 16, Background = new SolidColorBrush(Color.FromRgb(0x2B, 0x2B, 0x2B)),
            Foreground = Brushes.White, BorderBrush = brush, BorderThickness = new Thickness(0, 0, 0, 2),
            Padding = new Thickness(4), VerticalContentAlignment = VerticalAlignment.Center,
            CaretBrush = Brushes.White,
        };
        var remove = new Button
        {
            Content = "✕", Width = 28, Height = 32, FontSize = 12, Background = Brushes.Transparent,
            ToolTip = "Remove equation",
        };
        var error = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x99, 0xA4)), TextWrapping = TextWrapping.Wrap,
            FontSize = 12, Margin = new Thickness(40, 0, 4, 4), Visibility = Visibility.Collapsed,
        };

        var row = new Row { Input = input, Error = error, Toggle = toggle, Dot = dot, Colour = brush };
        DockPanel.SetDock(toggle, Dock.Left);
        DockPanel.SetDock(remove, Dock.Right);
        var line = new DockPanel { Margin = new Thickness(4, 2, 4, 2) };
        line.Children.Add(toggle);
        line.Children.Add(remove);
        line.Children.Add(input);
        var host = new StackPanel();
        host.Children.Add(line);
        host.Children.Add(error);

        input.TextChanged += (_, _) => { Reparse(row); Redraw(); };
        toggle.Click += (_, _) =>
        {
            row.Visible = !row.Visible;
            dot.Opacity = row.Visible ? 1 : 0.3;
            toggle.ToolTip = row.Visible ? "Hide equation" : "Show equation";
            Redraw();
        };
        remove.Click += (_, _) =>
        {
            _rows.Remove(row);
            EquationList.Children.Remove(host);
            if (_rows.Count == 0) AddRow("");
            Redraw();
        };

        _rows.Add(row);
        EquationList.Children.Add(host);
        Reparse(row);
    }

    private static void Reparse(Row row)
    {
        string text = row.Input.Text;
        row.Equation = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            row.Error.Visibility = Visibility.Collapsed;
            return;
        }
        if (GraphEquation.TryParse(text, out var eq, out var err))
        {
            row.Equation = eq;
            row.Error.Visibility = Visibility.Collapsed;
        }
        else
        {
            row.Error.Text = err;
            row.Error.Visibility = Visibility.Visible;
        }
    }

    private void AddEquation_Click(object sender, RoutedEventArgs e)
    {
        AddRow("");
        _rows[^1].Input.Focus();
    }

    // ---- Rendering -------------------------------------------------------

    private void Redraw()
    {
        double w = GraphCanvas.ActualWidth, h = GraphCanvas.ActualHeight;
        if (w < 2 || h < 2) return;
        GraphCanvas.Children.Clear();

        var gridBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
        var axisBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
        var labelBrush = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));

        var xs = AxisTicks.Compute(_viewport.XMin, _viewport.XMax, Math.Max(2, (int)(w / 80)));
        var ys = AxisTicks.Compute(_viewport.YMin, _viewport.YMax, Math.Max(2, (int)(h / 60)));
        double axisX = Math.Clamp(_viewport.ToScreenX(0, w), 0, w);
        double axisY = Math.Clamp(_viewport.ToScreenY(0, h), 0, h);

        foreach (double tx in xs)
        {
            double px = _viewport.ToScreenX(tx, w);
            AddLine(px, 0, px, h, gridBrush, 1);
            if (tx != 0) AddLabel(AxisTicks.Format(tx), px + 2, Math.Min(axisY + 2, h - 18), labelBrush);
        }
        foreach (double ty in ys)
        {
            double py = _viewport.ToScreenY(ty, h);
            AddLine(0, py, w, py, gridBrush, 1);
            if (ty != 0) AddLabel(AxisTicks.Format(ty), Math.Min(axisX + 4, w - 40), py + 2, labelBrush);
        }
        AddLine(0, axisY, w, axisY, axisBrush, 1.5);
        AddLine(axisX, 0, axisX, h, axisBrush, 1.5);

        int samples = Math.Max(100, (int)w);
        foreach (var row in _rows)
        {
            if (!row.Visible || row.Equation is null) continue;
            foreach (var seg in Sampler.Sample(row.Equation, _viewport, samples))
            {
                var poly = new Polyline
                {
                    Stroke = row.Colour, StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round,
                    IsHitTestVisible = false,
                };
                foreach (var p in seg)
                {
                    poly.Points.Add(new Point(_viewport.ToScreenX(p.X, w), _viewport.ToScreenY(p.Y, h)));
                }
                GraphCanvas.Children.Add(poly);
            }
        }
    }

    private void AddLine(double x1, double y1, double x2, double y2, Brush b, double t) =>
        GraphCanvas.Children.Add(new Line
        {
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = b, StrokeThickness = t, IsHitTestVisible = false,
        });

    private void AddLabel(string text, double x, double y, Brush b)
    {
        var tb = new TextBlock { Text = text, Foreground = b, FontSize = 11, IsHitTestVisible = false };
        Canvas.SetLeft(tb, x);
        Canvas.SetTop(tb, y);
        GraphCanvas.Children.Add(tb);
    }

    // ---- Interaction -----------------------------------------------------

    private void GraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void GraphCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var p = e.GetPosition(GraphCanvas);
        double w = GraphCanvas.ActualWidth, h = GraphCanvas.ActualHeight;
        double factor = e.Delta > 0 ? 1.25 : 0.8;
        _viewport.ZoomAt(factor, _viewport.FromScreenX(p.X, w), _viewport.FromScreenY(p.Y, h));
        Redraw();
        e.Handled = true;
    }

    private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _lastDrag = e.GetPosition(GraphCanvas);
        GraphCanvas.CaptureMouse();
        GraphCanvas.Cursor = Cursors.SizeAll;
    }

    private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        GraphCanvas.ReleaseMouseCapture();
        GraphCanvas.Cursor = Cursors.Arrow;
    }

    private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var p = e.GetPosition(GraphCanvas);
        _viewport.PanPixels(p.X - _lastDrag.X, p.Y - _lastDrag.Y, GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);
        _lastDrag = p;
        Redraw();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) { _viewport.Zoom(1.25); Redraw(); }

    private void ZoomOut_Click(object sender, RoutedEventArgs e) { _viewport.Zoom(0.8); Redraw(); }

    private void ResetView_Click(object sender, RoutedEventArgs e) { _viewport.Reset(); Redraw(); }
}
