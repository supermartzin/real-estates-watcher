using System.Reflection;

namespace RealEstatesWatcher.Tools.Attributes;

public static class Attributes
{
    public static string GetSettingsKey<TSettings>(string propertyName)
        => typeof(TSettings)
               .GetProperty(propertyName)
               ?.GetCustomAttribute<SettingsKeyAttribute>()?.Name
           ?? throw new ArgumentException(
               $"Provided Property name '{propertyName}' of {nameof(TSettings)} does not have a {nameof(SettingsKeyAttribute)} set.");

    public static string GetSettingsSectionKey<TSettings>()
        => typeof(TSettings).GetCustomAttribute<SettingsSectionKeyAttribute>()?.Name
           ?? throw new ArgumentException(
               $"Specified '{nameof(TSettings)} does not have a {nameof(SettingsSectionKeyAttribute)} set.");
}