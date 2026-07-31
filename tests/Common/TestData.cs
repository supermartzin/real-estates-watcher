using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

internal static class TestData
{
    public static RealEstateAdPost CreatePost(
        string id = "1",
        decimal price = 2_500_000m,
        decimal? floorArea = 60m,
        Layout layout = Layout.TwoPlusKk,
        string title = "Apartment") => new()
    {
        AdsPortalName = "Test portal",
        Title = title,
        Text = "A bright apartment",
        Price = price,
        Address = "Prague 1",
        WebUrl = new Uri($"https://example.test/ads/{id}"),
        Currency = Currency.CZK,
        Layout = layout,
        FloorArea = floorArea,
        AdditionalFees = 3_500m,
        ImageUrl = new Uri($"https://images.example.test/{id}.jpg")
    };
}
