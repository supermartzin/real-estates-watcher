using System;

namespace RealEstatesWatcher.Models
{
    public class RealEstateAdPost
    {
        public string Title { get; }

        public string Text { get; }

        public decimal Price { get; }

        public Currency Currency { get; }

        public string Address { get; }

        public decimal FloorArea { get; }

        public Uri WebUrl { get; }

        public Uri? ImageUrl { get; }

        public DateTime? PublishTime { get; }

        public RealEstateAdPost(string title,
                                string text,
                                decimal price,
                                Currency currency,
                                string address,
                                string webUrl,
                                decimal floorArea = decimal.Zero,
                                string? imageUrl = default,
                                DateTime? publishTime = default)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            WebUrl = webUrl is not null ? new Uri(webUrl) : throw new ArgumentNullException(nameof(webUrl));
            Currency = currency;
            Price = price >= decimal.Zero ? price : throw new ArgumentOutOfRangeException(nameof(price));

            FloorArea = floorArea;
            ImageUrl = imageUrl is not null ? new Uri(imageUrl) : default;
            PublishTime = publishTime;
        }
    }
}
