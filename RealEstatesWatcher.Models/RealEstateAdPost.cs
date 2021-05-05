using System;

namespace RealEstatesWatcher.Models
{
    public class RealEstateAdPost
    {
        public string AdsPortalName { get; }

        public string Title { get; }

        public string Text { get; }

        public decimal Price { get; }

        public Currency Currency { get; }

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
                                string address,
                                Uri webUrl,
                                decimal floorArea = decimal.Zero,
                                Uri? imageUrl = default,
                                DateTime? publishTime = default)
        {
            AdsPortalName = adsPortalName ?? throw new ArgumentNullException(nameof(adsPortalName));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            WebUrl = webUrl ?? throw new ArgumentNullException(nameof(webUrl));
            Currency = currency;
            Price = price >= decimal.Zero ? price : throw new ArgumentOutOfRangeException(nameof(price));

            FloorArea = floorArea;
            ImageUrl = imageUrl;
            PublishTime = publishTime;
        }

        public override string ToString() => $"[{AdsPortalName}]: {Title} | {Currency} {Price}";

        protected bool Equals(RealEstateAdPost other) => WebUrl.Equals(other.WebUrl);

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            
            return obj.GetType() == GetType() && Equals((RealEstateAdPost) obj);
        }

        public override int GetHashCode() => WebUrl.GetHashCode();
    }
}
