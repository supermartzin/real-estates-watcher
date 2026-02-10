namespace RealEstatesWatcher.Tools.Attributes;

/// <summary>
/// Specifies the configuration section key associated with a settings class.
/// Apply this attribute to a class that represents configuration settings to
/// indicate which configuration section (by key) should be bound to the class.
/// </summary>
/// <param name="name">The key of the configuration section associated with the settings class.</param>
[AttributeUsage(AttributeTargets.Class)]
public class SettingsSectionKeyAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the configuration section key specified by the attribute.
    /// This value is provided via the attribute's primary constructor and
    /// represents the name of the configuration section to bind to the settings class.
    /// </summary>
    public string Name { get; } = name;
}