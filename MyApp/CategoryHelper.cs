using Avalonia.Media;
using System.Collections.Generic;

namespace MyApp;

public static class CategoryHelper
{
    public record CategoryMeta(string Emoji, IBrush Color);

    public static readonly Dictionary<string, CategoryMeta> Categories = new()
    {
        ["Housing"]       = new("🏠", new SolidColorBrush(Color.Parse("#4e79a7"))),
        ["Food"]          = new("🍔", new SolidColorBrush(Color.Parse("#f28e2b"))),
        ["Transport"]     = new("🚗", new SolidColorBrush(Color.Parse("#e15759"))),
        ["Health"]        = new("❤️", new SolidColorBrush(Color.Parse("#76b7b2"))),
        ["Entertainment"] = new("🎬", new SolidColorBrush(Color.Parse("#59a14f"))),
        ["Subscriptions"] = new("📱", new SolidColorBrush(Color.Parse("#edc948"))),
        ["Other"]         = new("📦", new SolidColorBrush(Color.Parse("#b07aa1"))),
    };

    public static string GetEmoji(string category) =>
        Categories.TryGetValue(category, out var meta) ? meta.Emoji : "📦";

    public static IBrush GetColor(string category) =>
        Categories.TryGetValue(category, out var meta) ? meta.Color : Brushes.Gray;
}