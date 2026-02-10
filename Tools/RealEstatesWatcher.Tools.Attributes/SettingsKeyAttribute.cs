namespace RealEstatesWatcher.Tools.Attributes;

/// <summary>
/// Specifies the settings key name that is associated with a property.
/// </summary>
/// <remarks>
/// Apply this attribute to properties to indicate which settings key they map to
/// in a configuration or settings provider.
/// </remarks>
/// <param name="name">The name of the settings key.</param>
[AttributeUsage(AttributeTargets.Property)]
public class SettingsKeyAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the settings key name specified for the attributed property.
    /// </summary>
    public string Name { get; } = name;
}