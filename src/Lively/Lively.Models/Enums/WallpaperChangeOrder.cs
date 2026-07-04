namespace Lively.Models.Enums;

/// <summary>
/// Defines the order in which wallpapers are cycled during auto-change.
/// </summary>
public enum WallpaperChangeOrder
{
    /// <summary>
    /// Cycle wallpapers in the order they appear in the library.
    /// </summary>
    sequential,
    /// <summary>
    /// Shuffle wallpapers randomly, avoiding repeats within each round.
    /// </summary>
    shuffle
}
