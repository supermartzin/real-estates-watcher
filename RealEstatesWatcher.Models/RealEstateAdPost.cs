using System;

namespace RealEstatesWatcher.Models
{
    public class RealEstateAdPost
    {
        public string AdsPortalName { get; }

        public string Title { get; }

        public string Text { get; }

        public decimal Price { get; }

        public string? PriceComment { get; }

        public Currency Currency { get; }

        public Layout Layout { get; }

        public string Address { get; }

        public decimal FloorArea { get; }

        public Uri WebUrl { get; }

        public Uri? ImageUrl { get; }

        public DateTime? PublishTime { get; }

        public RealEstateAdPost(string adsPortalName,
                                string title,
                                string text,
                                decimal price,
                                Currency currency,
                                Layout layout,
                                string address,
                                Uri webUrl,
                                decimal floorArea = decimal.Zero,
                                string? priceComment = default,
                                Uri? imageUrl = default,
                                DateTime? publishTime = default)
        {
            AdsPortalName = adsPortalName ?? throw new ArgumentNullException(nameof(adsPortalName));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            WebUrl = webUrl ?? throw new ArgumentNullException(nameof(webUrl));
            Currency = currency;
            Layout = layout;
            Price = price >= decimal.Zero ? price : throw new ArgumentOutOfRangeException(nameof(price));

            FloorArea = floorArea;
            PriceComment = priceComment;
            ImageUrl = imageUrl;
            PublishTime = publishTime;
        }

        public override string ToString() => $"[{AdsPortalName}]: {Title} | {Currency} {Price} | {Layout.ToDisplayString()}";

        protected bool Equals(RealEstateAdPost other) => WebUrl.GetLeftPart(UriPartial.Path)
                                                               .Equals(other.WebUrl
                                                                            .GetLeftPart(UriPartial.Path));

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            
            return obj.GetType() == GetType() && Equals((RealEstateAdPost) obj);
        }

        public override int GetHashCode() => WebUrl.GetLeftPart(UriPartial.Path).GetHashCode();
    }
}
