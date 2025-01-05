namespace RealEstatesWatcher.Models;

public sealed class RealEstateAdPost : IEquatable<RealEstateAdPost>
{
    private readonly decimal _price;

    public required string AdsPortalName { get; init; }

    public required string Title { get; init; }

    public required string Text { get; init; }

    public required decimal Price
    {
        get => _price;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, decimal.Zero);

            _price = value;
        }
    }

    public required string Address { get; init; }

    public required Uri WebUrl { get; init; }

    public required Currency Currency { get; init; }

    public required Layout Layout { get; init; }

    public decimal? FloorArea { get; init; } = decimal.Zero;

    public decimal? AdditionalFees { get; init; } = decimal.Zero;

    public string? PriceComment { get; init; }

    public Uri? ImageUrl { get; init; }

    public DateTime? PublishTime { get; init; }

    public override string ToString() => $"[{AdsPortalName}]: {Title} | {Currency} {Price} | {Layout.ToDisplayString()} | URL: {WebUrl.ToString()}";

    public bool Equals(RealEstateAdPost? other) => WebUrl.GetLeftPart(UriPartial.Path)
        .Equals(other?.WebUrl.GetLeftPart(UriPartial.Path));

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
           
        return obj.GetType() == GetType() && Equals((RealEstateAdPost) obj);
    }

    public override int GetHashCode() => WebUrl.GetLeftPart(UriPartial.Path).GetHashCode();
}