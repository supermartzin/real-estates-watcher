using RealEstatesWatcher.Models;
using RealEstatesWatcher.Tools.Attributes;

namespace RealEstatesWatcher.AdPostsFilters.BasicFilter;

[SettingsSectionKey("basic")]
public record BasicParametersAdPostsFilterSettings
{
    [SettingsKey("price_min")]
    public decimal? MinPrice { get; init; }

    [SettingsKey("price_max")]
    public decimal? MaxPrice { get; init; }

    [SettingsKey("layouts")]
    public ISet<Layout> Layouts { get; init; } = new HashSet<Layout>();

    [SettingsKey("floor_area_min")]
    public decimal? MinFloorArea { get; init; }

    [SettingsKey("floor_area_max")]
    public decimal? MaxFloorArea { get; init; }
}