using RealEstatesWatcher.Tools.Attributes;

namespace RealEstatesWatcher.Tests;

public class AttributesTests
{
    [Fact]
    public void GetSettingsSectionKey_ReturnsConfiguredName() =>
        Assert.Equal("sample", Attributes.GetSettingsSectionKey<AnnotatedSettings>());

    [Fact]
    public void GetSettingsKey_ReturnsConfiguredPropertyName() =>
        Assert.Equal("value_key", Attributes.GetSettingsKey<AnnotatedSettings>(nameof(AnnotatedSettings.Value)));

    [Theory]
    [InlineData(nameof(AnnotatedSettings.Unannotated))]
    [InlineData("Missing")]
    public void GetSettingsKey_RejectsMissingMetadata(string propertyName) =>
        Assert.Throws<ArgumentException>(() => Attributes.GetSettingsKey<AnnotatedSettings>(propertyName));

    [Fact]
    public void GetSettingsSectionKey_RejectsUnannotatedTypes() =>
        Assert.Throws<ArgumentException>(() => Attributes.GetSettingsSectionKey<UnannotatedSettings>());

    [SettingsSectionKey("sample")]
    private sealed class AnnotatedSettings
    {
        [SettingsKey("value_key")]
        public string? Value { get; init; }

        public string? Unannotated { get; init; }
    }

    private sealed class UnannotatedSettings;
}
