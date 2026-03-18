using System.Reflection;

namespace mmex.net.winform.Controls;

/// <summary>
/// WinForms rendering helpers.
/// DataGridView.DoubleBuffered is protected — reflection is the only way to set it
/// without subclassing. MainForm and UserControls set their own DoubleBuffered/SetStyle.
/// </summary>
internal static class WinFormsExtensions
{
    private static readonly PropertyInfo? _dgvDoubleBuffered =
        typeof(DataGridView).GetProperty("DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>Enables double buffering on a DataGridView (eliminates resize flicker).</summary>
    internal static void EnableDoubleBuffering(this DataGridView grid) =>
        _dgvDoubleBuffered?.SetValue(grid, true);
}
