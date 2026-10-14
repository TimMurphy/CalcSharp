using System.Windows;
using System.Windows.Controls;
using CalcSharp.App.Views;

namespace CalcSharp.App;

/// <summary>A selectable entry in the navigation menu.</summary>
public sealed record ModeInfo(string Group, string Title, Func<UIElement> CreateView);

/// <summary>
/// The navigation menu. Views are discovered by convention so modes can be added independently:
/// <c>CalcSharp.App.Views.&lt;Name&gt;View</c> with a parameterless constructor, or for converters
/// <c>CalcSharp.App.Views.ConverterView</c> with a constructor taking the category name.
/// A mode whose view does not exist yet shows a placeholder.
/// </summary>
public static class ModeCatalog
{
    private static readonly string[] Converters =
    [
        "Currency", "Volume", "Length", "Weight and mass", "Temperature", "Energy", "Area",
        "Speed", "Time", "Power", "Data", "Pressure", "Angle",
    ];

    public static IReadOnlyList<ModeInfo> Modes { get; } = Build();

    private static List<ModeInfo> Build()
    {
        var modes = new List<ModeInfo>
        {
            new("Calculator", "Standard", () => new StandardView()),
            new("Calculator", "Scientific", () => Create("ScientificView", "Scientific")),
            new("Calculator", "Graphing", () => Create("GraphingView", "Graphing")),
            new("Calculator", "Programmer", () => Create("ProgrammerView", "Programmer")),
            new("Calculator", "Date calculation", () => Create("DateCalculationView", "Date calculation")),
        };

        foreach (var category in Converters)
        {
            modes.Add(new ModeInfo("Converter", category, () => CreateConverter(category)));
        }

        return modes;
    }

    private static UIElement Create(string typeName, string title)
    {
        var type = Type.GetType($"CalcSharp.App.Views.{typeName}");
        return type is null ? Placeholder(title) : (UIElement)Activator.CreateInstance(type)!;
    }

    private static UIElement CreateConverter(string category)
    {
        var type = Type.GetType("CalcSharp.App.Views.ConverterView");
        return type is null ? Placeholder(category) : (UIElement)Activator.CreateInstance(type, category)!;
    }

    private static UIElement Placeholder(string title) => new TextBlock
    {
        Text = $"{title} is not implemented yet.",
        Foreground = System.Windows.Media.Brushes.Gray,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };
}
