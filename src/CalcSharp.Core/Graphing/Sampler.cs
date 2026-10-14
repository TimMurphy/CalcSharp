namespace CalcSharp.Core.Graphing;

/// <summary>Turns equations into polyline segments in graph coordinates for a viewport.</summary>
public static class Sampler
{
    /// <summary>Samples an equation. Each returned list is one continuous polyline.</summary>
    public static List<List<GraphPoint>> Sample(GraphEquation eq, Viewport vp, int samples = 800)
    {
        samples = Math.Max(2, samples);
        return eq.Kind switch
        {
            EquationKind.YOfX => SampleCurve(eq.Evaluate, vp.XMin, vp.XMax, vp.Height, samples, swap: false),
            EquationKind.XOfY => SampleCurve(eq.Evaluate, vp.YMin, vp.YMax, vp.Width, samples, swap: true),
            _ => SampleImplicit(eq, vp, Math.Clamp(samples / 4, 2, 200)),
        };
    }

    private static List<List<GraphPoint>> SampleCurve(Func<double, double> f, double t0, double t1,
        double crossRange, int samples, bool swap)
    {
        var result = new List<List<GraphPoint>>();
        List<GraphPoint>? cur = null;
        double? prev = null;
        for (int i = 0; i <= samples; i++)
        {
            double t = t0 + (t1 - t0) * i / samples;
            double v = f(t);
            bool bad = double.IsNaN(v) || double.IsInfinity(v);
            if (!bad && prev is double p)
            {
                // Asymptote: sign flips with both sides far outside the view, or an implausibly large jump.
                bool flip = Math.Sign(v) != Math.Sign(p) && Math.Abs(v) > crossRange && Math.Abs(p) > crossRange;
                if (flip || Math.Abs(v - p) > crossRange * 8) bad = true;
            }
            if (bad)
            {
                cur = null;
                prev = null;
                continue;
            }
            if (cur is null)
            {
                cur = [];
                result.Add(cur);
            }
            cur.Add(swap ? new GraphPoint(v, t) : new GraphPoint(t, v));
            prev = v;
        }
        return result.Where(l => l.Count >= 2).ToList();
    }

    // Marching squares: each crossing cell yields a 2-point polyline.
    private static List<List<GraphPoint>> SampleImplicit(GraphEquation eq, Viewport vp, int n)
    {
        var vals = new double[n + 1, n + 1];
        double dx = vp.Width / n, dy = vp.Height / n;
        for (int i = 0; i <= n; i++)
            for (int j = 0; j <= n; j++)
                vals[i, j] = eq.Residual(vp.XMin + i * dx, vp.YMin + j * dy);

        var segs = new List<List<GraphPoint>>();
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                double a = vals[i, j], b = vals[i + 1, j], c = vals[i + 1, j + 1], d = vals[i, j + 1];
                if (double.IsNaN(a + b + c + d) || double.IsInfinity(a + b + c + d)) continue;
                double x0 = vp.XMin + i * dx, y0 = vp.YMin + j * dy;
                var pts = new List<GraphPoint>();
                void Edge(double v1, double v2, double ax, double ay, double bx, double by)
                {
                    if ((v1 < 0) != (v2 < 0))
                    {
                        double t = v1 / (v1 - v2);
                        pts.Add(new GraphPoint(ax + (bx - ax) * t, ay + (by - ay) * t));
                    }
                }
                Edge(a, b, x0, y0, x0 + dx, y0);
                Edge(b, c, x0 + dx, y0, x0 + dx, y0 + dy);
                Edge(d, c, x0, y0 + dy, x0 + dx, y0 + dy);
                Edge(a, d, x0, y0, x0, y0 + dy);
                for (int k = 0; k + 1 < pts.Count; k += 2) segs.Add([pts[k], pts[k + 1]]);
            }
        }
        return segs;
    }
}
