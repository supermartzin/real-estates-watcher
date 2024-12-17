namespace RealEstatesWatcher.Models;

public class RealEstateAdPost(string adsPortalName,
                              string title,
                              string text,
                              decimal price,
                              Currency currency,
                              Layout layout,
                              string address,
                              Uri webUrl,
                              decimal additionalFees = decimal.Zero,
                              decimal floorArea = decimal.Zero,
                              string? priceComment = null,
                              Uri? imageUrl = null,
                              DateTime? publishTime = null)
{
    public string AdsPortalName { get; } = adsPortalName ?? throw new ArgumentNullException(nameof(adsPortalName));

    public string Title { get; } = title ?? throw new ArgumentNullException(nameof(title));

    public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));

    public decimal Price { get; } = price >= decimal.Zero ? price : throw new ArgumentOutOfRangeException(nameof(price));

    public decimal AdditionalFees { get; } = additionalFees;

    public string? PriceComment { get; } = priceComment;

    public Currency Currency { get; } = currency;

    public Layout Layout { get; } = layout;

    public string Address { get; } = address ?? throw new ArgumentNullException(nameof(address));

    public decimal FloorArea { get; } = floorArea;

    public Uri WebUrl { get; } = webUrl ?? throw new ArgumentNullException(nameof(webUrl));

    public Uri? ImageUrl { get; } = imageUrl;

    public DateTime? PublishTime { get; } = publishTime;

    public override string ToString() => $"[{AdsPortalName}]: {Title} | {Currency} {Price} | {Layout.ToDisplayString()} | URL: {WebUrl.ToString()}";

    protected bool Equals(RealEstateAdPost other) => WebUrl.GetLeftPart(UriPartial.Path)
        .Equals(other.WebUrl.GetLeftPart(UriPartial.Path));

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