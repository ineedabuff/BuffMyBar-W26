namespace BuffBar.Core;

/// <summary>
/// Palette d'un thème (themes\&lt;nom&gt;.json). Si <see cref="FollowWindows"/> est
/// vrai, les couleurs sont ignorées et la barre suit le thème de Windows.
/// </summary>
public sealed class ThemePalette
{
    public bool FollowWindows { get; set; } = false;

    public string BarBackground { get; set; } = "#000000";
    public string ModuleBackground { get; set; } = "#000000";
    public string ModuleBorder { get; set; } = "#3A3A3A";
    public string HoverBackground { get; set; } = "#1E1E1E";
    public string HoverBorder { get; set; } = "#555555";
    public string PrimaryText { get; set; } = "#FFFFFF";
    public string SubtleText { get; set; } = "#C8C8C8";
    public string Accent { get; set; } = "#DDFF24";
}
