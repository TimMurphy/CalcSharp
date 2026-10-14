using System.Globalization;

namespace CalcSharp.Core.Graphing;

public readonly record struct GraphPoint(double X, double Y);

/// <summary>Visible rectangle of the graph plane with zoom/pan math.</summary>
public sealed class Viewport
{
    public const double DefaultHalfWidth = 10;

    public double XMin { get; private set; } = -DefaultHalfWidth;
    public double XMax { get; private set; } = DefaultHalfWidth;
    public double YMin { get; private set; } = -DefaultHalfWidth;
    public double YMax { get; private set; } = DefaultHalfWidth;

    public double Width => XMax - XMin;
    public double Height => YMax - YMin;

    public void Set(double xMin, double xMax, double yMin, double yMax)
    {
        if (!(xMax > xMin) || !(yMax > yMin)) throw new ArgumentException("Max must exceed min.");
        XMin = xMin; XMax = xMax; YMin = yMin; YMax = yMax;
    }

    public void Reset() => Set(-DefaultHalfWidth, DefaultHalfWidth, -DefaultHalfWidth, DefaultHalfWidth);

    /// <summary>Zooms about a graph-space point; factor &gt; 1 zooms in.</summary>
    public void ZoomAt(double factor, double cx, double cy)
    {
        if (factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        double w = Width / factor, h = Height / factor;
        if (w < 1e-9 || h < 1e-9 || w > 1e9 || h > 1e9) return;
        double fx = (cx - XMin) / Width, fy = (cy - YMin) / Height;
        XMin = cx - fx * w; XMax = XMin + w;
        YMin = cy - fy * h; YMax = YMin + h;
    }

    public void Zoom(double factor) => ZoomAt(factor, (XMin + XMax) / 2, (YMin + YMax) / 2);

    /// <summary>Pans by a pixel delta (dragging right moves the graph right).</summary>
    public void PanPixels(double dx, double dy, double pixelWidth, double pixelHeight)
    {
        double gx = dx / pixelWidth * Width, gy = dy / pixelHeight * Height;
        XMin -= gx; XMax -= gx;
        YMin += gy; YMax += gy;
    }

    public double ToScreenX(double x, double pixelWidth) => (x - XMin) / Width * pixelWidth;
    public double ToScreenY(double y, double pixelHeight) => (YMax - y) / Height * pixelHeight;
    public double FromScreenX(double px, double pixelWidth) => XMin + px / pixelWidth * Width;
    public double FromScreenY(double py, double pixelHeight) => YMax - py / pixelHeight * Height;
}

public static class AxisTicks
{
    /// <summary>Returns the "nice" step (1, 2 or 5 x 10^n) giving about <paramref name="target"/> ticks.</summary>
    public static double NiceStep(double range, int target = 10)
    {
        double raw = range / Math.Max(1, target);
        double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double n = raw / mag;
        double nice = n < 1.5 ? 1 : n < 3.5 ? 2 : n < 7.5 ? 5 : 10;
        return nice * mag;
    }

    /// <summary>Tick values within [min, max].</summary>
    public static IReadOnlyList<double> Compute(double min, double max, int target = 10)
    {
        double step = NiceStep(max - min, target);
        var list = new List<double>();
        double k = Math.Ceiling(min / step - 1e-9);
        for (; k * step <= max + step * 1e-9; k++)
        {
            list.Add(k == 0 ? 0 : Math.Round(k * step, 12));
        }
        return list;
    }

    /// <summary>Formats a tick label without floating-point noise.</summary>
    public static string Format(double v) =>
        Math.Abs(v) >= 1e6 || (v != 0 && Math.Abs(v) < 1e-4)
            ? v.ToString("0.###E+0", CultureInfo.InvariantCulture)
            : Math.Round(v, 10).ToString("0.##########", CultureInfo.InvariantCulture);
}

/// <summary>Line colours offered for equations, named as in the app's resources.</summary>
public static class EquationPalette
{
    public static readonly IReadOnlyList<(string Name, string Hex)> Colours =
    [
        ("Colour 1", "#4CC2FF"),
        ("Colour 2", "#F7630C"),
        ("Colour 3", "#16C60C"),
        ("Colour 4", "#B146C2"),
        ("Plum light", "#C239B3"),
        ("Charcoal", "#7A7574"),
        ("Dark green", "#10893E"),
        ("Mint green", "#00B7C3"),
        ("Navy blue", "#0063B1"),
        ("Orange bright", "#FF8C00"),
        ("Yellow gold", "#FFB900"),
    ];

    public static (string Name, string Hex) For(int index) =>
        Colours[((index % Colours.Count) + Colours.Count) % Colours.Count];
}
