using System.Windows.Input;

namespace CalcSharp.App.Views;

/// <summary>
/// Implemented by every calculator/converter view so the main window can forward keyboard input.
/// </summary>
public interface IModeView
{
    /// <summary>Handles a key press. Returns true if the key was consumed.</summary>
    bool HandleKeyDown(KeyEventArgs e);

    /// <summary>Handles typed text. Returns true if the text was consumed.</summary>
    bool HandleTextInput(TextCompositionEventArgs e);
}
