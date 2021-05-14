using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.UI.Console
{
    public class FileAdPostsHandler : IRealEstateAdPostsHandler
    {
        private static class HtmlTemplates
        {
            public const string FullPage = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
</head>
<body style=""max-width: 800px; margin:10px auto;"">
    {$posts}
</body>
</html>";

            public const string Post = "<div style=\"padding: 10px; background: #ededed; min-height: 170px;\">\r\n    <div style=\"float: left; margin-right: 1em; width: 30%; height: 150px; display: {$img-display};\">\r\n        <img src=\"{$img-link}\" style=\"height: 100%; width: 100%; object-fit: cover;\" />\r\n    </div>\r\n    <a href=\"{$post-link}\">\r\n        <h3 style=\"margin-block-end: 0.2em; margin-block-start: 0em;\">{$title}</h3>\r\n    </a>\r\n    <span style=\"font-size: medium; color: grey; display: {$price-display};\">\r\n        <b>{$price}</b> {$currency}<br/>\r\n    </span>\r\n    <span style=\"font-size: medium; color: grey; display: {$price-comment-display};\">\r\n        <b>{$price-comment}</b><br/>\r\n    </span>\r\n    <span>\r\n        <b>Adresa:</b> {$address}<br/>\r\n        <b>Výmera:</b> {$floor-area}<br/>\r\n    </span>\r\n    <p style=\"margin-block-start: 0.2em; margin-block-end: 0em; font-size: small; display: {$text-display};\">{$text}</p>\r\n</div>";
        } 

        private static readonly string FilePath =  Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + Path.DirectorySeparatorChar + "ads.html";

        public Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost)
        {
            // skip
            return Task.CompletedTask;
        }

        public Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts)
        {
            // skip
            return Task.CompletedTask;
        }

        public async Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts)
        {
            var posts = adPosts.Select(post =>
            {
                var postHtml = HtmlTemplates.Post.Replace("{$title}", post.Title)
                                                       .Replace("{$post-link}", post.WebUrl.AbsoluteUri)
                                                       .Replace("{$address}", post.Address)
                                                       .Replace("{$floor-area}", post.FloorArea + " m²");

                // image
                if (post.ImageUrl is not null)
                {
                    postHtml = postHtml.Replace("{$img-link}", post.ImageUrl.AbsoluteUri)
                                       .Replace("{$img-display}", "block");
                }
                else
                {
                    postHtml = postHtml.Replace("{$img-display}", "none");
                }
                // price
                if (post.Price != decimal.Zero)
                {
                    postHtml = postHtml.Replace("{$price}", post.Price.ToString("N", new NumberFormatInfo { NumberGroupSeparator = " " }))
                                       .Replace("{$currency}", post.Currency.ToString())
                                       .Replace("{$price-display}", "block")
                                       .Replace("{$price-comment-display}", "none");
                }
                else
                {
                    postHtml = postHtml.Replace("{$price-comment}", post.PriceComment ?? "-")
                                       .Replace("{$price-display}", "none")
                                       .Replace("{$price-comment-display}", "block");
                }
                // text
                if (!string.IsNullOrEmpty(post.Text))
                {
                    postHtml = postHtml.Replace("{$text}", post.Text)
                                       .Replace("{$text-display}", "block");
                }
                else
                {
                    postHtml = postHtml.Replace("{$text-display}", "none");
                }
                        
                return postHtml;
            });

            var page = HtmlTemplates.FullPage.Replace("{$posts}", string.Join(Environment.NewLine, posts));

            await File.WriteAllTextAsync(FilePath, page).ConfigureAwait(false);
        }
    }
}