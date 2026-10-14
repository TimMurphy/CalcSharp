using CalcSharp.Core.Graphing;
using Xunit;

namespace CalcSharp.Core.Tests.Graphing;

public class GraphingTests
{
    private static double Y(string eq, double x) => GraphEquation.Parse(eq).Evaluate(x);

    [Theory]
    [InlineData("x+1", 2, 3)]
    [InlineData("x-1", 2, 1)]
    [InlineData("2*x", 3, 6)]
    [InlineData("x÷2", 3, 1.5)]
    [InlineData("x×x", 3, 9)]
    [InlineData("x^2", 3, 9)]
    [InlineData("2^x", 3, 8)]
    [InlineData("2x", 4, 8)]
    [InlineData("2x^2", 3, 18)]
    [InlineData("(x+1)(x-1)", 3, 8)]
    [InlineData("-x^2", 3, -9)]
    [InlineData("2^-x", 1, 0.5)]
    [InlineData("2^3^2", 0, 512)]
    [InlineData("1/2x", 4, 2)]
    [InlineData("x²", 5, 25)]
    [InlineData("x+pi", 0, Math.PI)]
    [InlineData("e^x", 1, Math.E)]
    [InlineData("2pi", 0, 2 * Math.PI)]
    public void Arithmetic(string eq, double x, double expected)
    {
        string text = eq.Contains('x') ? eq : eq + "+0x";
        Assert.Equal(expected, Y(text, x), 9);
    }

    [Theory]
    [InlineData("sqrt(x)", 16, 4)]
    [InlineData("abs(x)", -3, 3)]
    [InlineData("sin(x)", Math.PI / 2, 1)]
    [InlineData("cos(x)", 0, 1)]
    [InlineData("tan(x)", Math.PI / 4, 1)]
    [InlineData("asin(x)", 1, Math.PI / 2)]
    [InlineData("acos(x)", 1, 0)]
    [InlineData("atan(x)", 1, Math.PI / 4)]
    [InlineData("sinh(x)", 0, 0)]
    [InlineData("cosh(x)", 0, 1)]
    [InlineData("tanh(x)", 0, 0)]
    [InlineData("ln(x)", Math.E, 1)]
    [InlineData("log(x)", 1000, 3)]
    [InlineData("exp(x)", 1, Math.E)]
    [InlineData("floor(x)", 2.7, 2)]
    [InlineData("ceil(x)", 2.1, 3)]
    [InlineData("round(x)", 2.5, 3)]
    [InlineData("min(x,2)", 5, 2)]
    [InlineData("max(x,2)", 5, 5)]
    [InlineData("sinx", Math.PI / 2, 1)]
    [InlineData("sin x", Math.PI / 2, 1)]
    [InlineData("√x", 9, 3)]
    public void Functions(string eq, double x, double expected) =>
        Assert.Equal(expected, Y(eq, x), 9);

    [Fact] public void SqrtOfNegativeIsNaN() => Assert.True(double.IsNaN(Y("sqrt(x)", -1)));
    [Fact] public void LnOfZeroIsNaN() => Assert.True(double.IsNaN(Y("ln(x)", 0)));
    [Fact] public void DivideByZeroIsNaN() => Assert.True(double.IsNaN(Y("1/x", 0)));
    [Fact] public void CubeRootOfNegative() => Assert.Equal(-2, Y("x^(1/3)", -8), 6);
    [Fact] public void ImplicitMultiplicationOfVariables() =>
        Assert.Equal(EquationKind.Implicit, GraphEquation.Parse("xy=1").Kind);

    [Fact] public void YEqualsIsYOfX() => Assert.Equal(EquationKind.YOfX, GraphEquation.Parse("y=x^2").Kind);
    [Fact] public void ReversedYEquals() => Assert.Equal(4, GraphEquation.Parse("x^2=y").Evaluate(2));
    [Fact] public void XEqualsIsXOfY() => Assert.Equal(EquationKind.XOfY, GraphEquation.Parse("x=3").Kind);
    [Fact] public void CircleIsImplicit() => Assert.Equal(EquationKind.Implicit, GraphEquation.Parse("x^2+y^2=25").Kind);
    [Fact] public void CircleResidual() => Assert.Equal(0, GraphEquation.Parse("x^2+y^2=25").Residual(3, 4), 9);

    [Theory]
    [InlineData("", GraphException.EmptyExpression)]
    [InlineData("   ", GraphException.EmptyExpression)]
    [InlineData("2+3", GraphException.NoVariable)]
    [InlineData("y=", GraphException.EqualWithoutEquation)]
    [InlineData("=x", GraphException.EqualWithoutEquation)]
    [InlineData("y=x=2", GraphException.EqualWithoutEquation)]
    [InlineData("1=2", GraphException.NoSolution)]
    [InlineData("(x+1", GraphException.MissingClose)]
    [InlineData("x+1)", GraphException.MissingOpen)]
    [InlineData("x+", GraphException.InvalidSyntax)]
    [InlineData("x$", GraphException.InvalidSyntax)]
    public void ErrorMessages(string text, string message)
    {
        Assert.False(GraphEquation.TryParse(text, out var eq, out var err));
        Assert.Null(eq);
        Assert.Equal(message, err);
    }

    [Fact] public void ParseThrowsGraphException() =>
        Assert.Throws<GraphException>(() => GraphEquation.Parse(""));

    [Fact]
    public void ViewportDefault()
    {
        var v = new Viewport();
        Assert.Equal(-10, v.XMin);
        Assert.Equal(10, v.YMax);
    }

    [Fact]
    public void ZoomInHalvesWidthAboutCentre()
    {
        var v = new Viewport();
        v.Zoom(2);
        Assert.Equal(-5, v.XMin, 9);
        Assert.Equal(5, v.XMax, 9);
    }

    [Fact]
    public void ZoomAtKeepsPointFixed()
    {
        var v = new Viewport();
        v.ZoomAt(2, 5, 5);
        Assert.Equal(-2.5, v.XMin, 9);
        Assert.Equal(7.5, v.XMax, 9);
    }

    [Fact]
    public void ZoomOutDoublesHeight()
    {
        var v = new Viewport();
        v.Zoom(0.5);
        Assert.Equal(40, v.Height, 9);
    }

    [Fact]
    public void PanRightMovesViewLeft()
    {
        var v = new Viewport();
        v.PanPixels(100, 0, 400, 400);
        Assert.Equal(-15, v.XMin, 9);
    }

    [Fact]
    public void PanDownMovesViewUp()
    {
        var v = new Viewport();
        v.PanPixels(0, 100, 400, 400);
        Assert.Equal(-5, v.YMin, 9);
    }

    [Fact]
    public void ResetRestoresDefault()
    {
        var v = new Viewport();
        v.Zoom(4);
        v.PanPixels(30, 30, 100, 100);
        v.Reset();
        Assert.Equal((-10.0, 10.0, -10.0, 10.0), (v.XMin, v.XMax, v.YMin, v.YMax));
    }

    [Fact]
    public void ScreenMappingRoundTrip()
    {
        var v = new Viewport();
        Assert.Equal(200, v.ToScreenX(0, 400), 9);
        Assert.Equal(200, v.ToScreenY(0, 400), 9);
        Assert.Equal(0, v.ToScreenY(10, 400), 9);
        Assert.Equal(-10, v.FromScreenX(0, 400), 9);
        Assert.Equal(10, v.FromScreenY(0, 400), 9);
    }

    [Fact]
    public void SetRejectsInvertedRange() =>
        Assert.Throws<ArgumentException>(() => new Viewport().Set(1, 0, 0, 1));

    [Theory]
    [InlineData(20, 10, 2)]
    [InlineData(10, 10, 1)]
    [InlineData(100, 10, 10)]
    [InlineData(1, 10, 0.1)]
    [InlineData(50, 10, 5)]
    public void NiceStep(double range, int target, double expected) =>
        Assert.Equal(expected, AxisTicks.NiceStep(range, target), 9);

    [Fact]
    public void TicksIncludeZeroAndStayInRange()
    {
        var t = AxisTicks.Compute(-10, 10, 10);
        Assert.Contains(0, t);
        Assert.All(t, v => Assert.InRange(v, -10, 10));
        Assert.Equal(11, t.Count);
    }

    [Fact]
    public void TickFormat()
    {
        Assert.Equal("0.5", AxisTicks.Format(0.5));
        Assert.Equal("-3", AxisTicks.Format(-3));
        Assert.Equal("0.3", AxisTicks.Format(0.1 + 0.2));
    }

    [Fact]
    public void LinearSampleIsSingleSegment()
    {
        var segs = Sampler.Sample(GraphEquation.Parse("x"), new Viewport(), 100);
        Assert.Single(segs);
        Assert.Equal(101, segs[0].Count);
    }

    [Fact]
    public void SqrtBreaksAtNegativeDomain()
    {
        var segs = Sampler.Sample(GraphEquation.Parse("sqrt(x)"), new Viewport(), 100);
        Assert.Single(segs);
        Assert.True(segs[0][0].X >= 0);
    }

    [Fact]
    public void ReciprocalBreaksAtAsymptote()
    {
        var segs = Sampler.Sample(GraphEquation.Parse("1/x"), new Viewport(), 201);
        Assert.Equal(2, segs.Count);
    }

    [Fact]
    public void TanProducesManySegments()
    {
        var segs = Sampler.Sample(GraphEquation.Parse("tan(x)"), new Viewport(), 800);
        Assert.True(segs.Count >= 6);
    }

    [Fact]
    public void VerticalLineSwapsCoordinates()
    {
        var segs = Sampler.Sample(GraphEquation.Parse("x=3"), new Viewport(), 50);
        Assert.Single(segs);
        Assert.All(segs[0], p => Assert.Equal(3, p.X, 9));
    }

    [Fact]
    public void CircleSamplesNearRadius()
    {
        var segs = Sampler.Sample(GraphEquation.Parse("x^2+y^2=25"), new Viewport(), 800);
        Assert.NotEmpty(segs);
        Assert.All(segs.SelectMany(s => s), p => Assert.InRange(Math.Sqrt(p.X * p.X + p.Y * p.Y), 4.5, 5.5));
    }

    [Fact]
    public void PaletteWraps()
    {
        Assert.Equal("Colour 1", EquationPalette.For(0).Name);
        Assert.Equal(EquationPalette.For(0), EquationPalette.For(EquationPalette.Colours.Count));
    }
}
