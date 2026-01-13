using RealEstatesWatcher.Models;

using System.Globalization;
using System.Text;
using System.Web;

namespace RealEstatesWatcher.AdPostsHandlers.Base.Html;

public abstract class HtmlBasedAdPostsHandlerBase
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
            postHtmlBuilder = postHtmlBuilder.Replace("{$address-links-display}", "inline-block")
                               .Replace("{$address-encoded}", HttpUtility.UrlEncode(post.Address));
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$address-links-display}", "none");
        }

        // layout
        postHtmlBuilder = postHtmlBuilder.Replace("{$layout}", post.Layout is not Layout.NotSpecified ? post.Layout.ToDisplayString() : "-");

        // floor area
        postHtmlBuilder = post.FloorArea is not null and not decimal.Zero ? postHtmlBuilder.Replace("{$floor-area}", post.FloorArea + " m²") : postHtmlBuilder.Replace("{$floor-area}", "-");

        // image
        if (post.ImageUrl is not null)
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$img-link}", post.ImageUrl.AbsoluteUri)
                               .Replace("{$img-display}", "block");
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$img-link}", string.Empty)
                               .Replace("{$img-display}", "none");
        }

        // price
        if (post.Price is not decimal.Zero)
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$price}", post.Price.ToString("N", new NumberFormatInfo { NumberGroupSeparator = " " }))
                               .Replace("{$currency}", post.Currency.ToString())
                               .Replace("{$price-display}", "block")
                               .Replace("{$price-comment-display}", "none")
                               .Replace("{$price-comment}", string.Empty);
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$price-comment}", post.PriceComment ?? "-")
                               .Replace("{$price-comment-display}", "block")
                               .Replace("{$price-display}", "none")
                               .Replace("{$price}", string.Empty)
                               .Replace("{$currency}", string.Empty);
        }

        // additional fees
        if (post.AdditionalFees is not null and not decimal.Zero)
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$additional-fees}", post.AdditionalFees.Value.ToString("N", new NumberFormatInfo { NumberGroupSeparator = " " }))
                               .Replace("{$additional-fees-display}", "inline-block");
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$additional-fees}", decimal.Zero.ToString("N"))
                               .Replace("{$additional-fees-display}", "none");
        }

        // text
        if (!string.IsNullOrEmpty(post.Text))
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$text}", post.Text)
                               .Replace("{$text-display}", "table");
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$text}", string.Empty)
                               .Replace("{$text-display}", "none");
        }

        return postHtmlBuilder.ToString();
    }

    protected virtual string CreateFullHtmlPage(IEnumerable<RealEstateAdPost> posts, string titleHtmlElement)
    {
        return CommonHtmlTemplateElements
            .FullPage
            .Replace("<maintitle/>", titleHtmlElement)
            .Replace("<posts/>", string.Join(Environment.NewLine, posts.Select(CreateHtmlPostElement)));
    }
}