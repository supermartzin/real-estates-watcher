using RealEstatesWatcher.Models;

using System.Globalization;
using System.Text;
using System.Web;

namespace RealEstatesWatcher.AdPostsHandlers.Base.Html;

public abstract class HtmlBasedAdPostsHandlerBase(NumberFormatInfo numberFormat)
{
    protected virtual string CreateHtmlPostElement(RealEstateAdPost post)
    {
        var postHtmlBuilder = new StringBuilder(CommonHtmlTemplateElements.Post)
                                    .Replace("{$title}", post.Title)
                                    .Replace("{$portal-name}", post.AdsPortalName)
                                    .Replace("{$post-link}", post.WebUrl.AbsoluteUri)
                                    .Replace("{$address}", post.Address);

        // address links
        if (!string.IsNullOrEmpty(post.Address))
        {
            postHtmlBuilder.Replace("{$address-links-display}", "inline-block")
                           .Replace("{$address-encoded}", HttpUtility.UrlEncode(post.Address));
        }
        else
        {
            postHtmlBuilder.Replace("{$address-links-display}", "none");
        }

        // layout
        postHtmlBuilder.Replace("{$layout}", post.Layout is not Layout.NotSpecified ? post.Layout.ToDisplayString() : "-");

        // floor area
        postHtmlBuilder.Replace("{$floor-area}", post.FloorArea is not null and not decimal.Zero ? post.FloorArea + " m²" : "-");

        // image
        if (post.ImageUrl is not null)
        {
            postHtmlBuilder.Replace("{$img-link}", post.ImageUrl.AbsoluteUri)
                           .Replace("{$img-display}", "block");
        }
        else
        {
            postHtmlBuilder.Replace("{$img-link}", string.Empty)
                           .Replace("{$img-display}", "none");
        }

        // price
        if (post.Price is not decimal.Zero)
        {
            postHtmlBuilder.Replace("{$price}", post.Price.ToString("N", numberFormat))
                           .Replace("{$currency}", post.Currency.ToString())
                           .Replace("{$price-display}", "block")
                           .Replace("{$price-comment-display}", "none")
                           .Replace("{$price-comment}", string.Empty);
        }
        else
        {
            postHtmlBuilder.Replace("{$price-comment}", post.PriceComment ?? "-")
                           .Replace("{$price-comment-display}", "block")
                           .Replace("{$price-display}", "none")
                           .Replace("{$price}", string.Empty)
                           .Replace("{$currency}", string.Empty);
        }

        // additional fees
        if (post.AdditionalFees is not null and not decimal.Zero)
        {
            postHtmlBuilder.Replace("{$additional-fees}", post.AdditionalFees.Value.ToString("N", numberFormat))
                           .Replace("{$additional-fees-display}", "inline-block");
        }
        else
        {
            postHtmlBuilder.Replace("{$additional-fees}", decimal.Zero.ToString("N"))
                           .Replace("{$additional-fees-display}", "none");
        }

        // text
        postHtmlBuilder.Replace("{$text}", post.Text)
                       .Replace("{$text-display}", !string.IsNullOrEmpty(post.Text) ? "table" : "none");

        return postHtmlBuilder.ToString();
    }

    protected virtual string CreateFullHtmlPage(IEnumerable<RealEstateAdPost> posts, string titleHtmlElement) =>
        CommonHtmlTemplateElements.FullPage
            .Replace("<maintitle/>", titleHtmlElement)
            .Replace("<posts/>", string.Join(Environment.NewLine, posts.Select(CreateHtmlPostElement)));
}