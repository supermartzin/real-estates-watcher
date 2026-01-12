namespace RealEstatesWatcher.AdPostsHandlers.Templates;

public static class CommonHtmlTemplateElements
{
    public const string FullPage = """
                                   <!DOCTYPE html>
                                   <html lang="en">
                                   <head>
                                       <meta charset="utf-8">
                                       <title>Real Estate Advertisements</title>
                                   </head>
                                   <body style="max-width: 800px; margin:10px auto;">
                                       <maintitle/>
                                       <posts/>
                                   </body>
                                   </html>
                                   """;

    public const string TitleNewPosts = """
                                        <h1>🏦 <span style="color: #4f4f4f; font-style: italic;">NEW Real estate offer</span></h1>
                                        """;

    public const string TitleInitialPosts = """
                                            <h1>🏦 <span style="color: #4f4f4f; font-style: italic;"> Current Real estate offer</span></h1>
                                            """;

    public const string Post = """
                               <div style="padding: 10px; background: #ededed; min-height: 200px;">
                                   <div style="float: left; margin-right: 1em; width: 30%; height: 180px; display: {$img-display};">
                                       <img src="{$img-link}" style="height: 100%; width: 100%; object-fit: cover;" alt="Ad main visual presentation"/>
                                   </div>
                                   <a href="{$post-link}">
                                       <h3 style="margin: 0.2em; margin-top: 0;">{$title}</h3>
                                   </a>
                                   <span style="font-size: medium; color: #4f4f4f; display: {$price-display};">
                                       <strong>{$price}</strong> {$currency}
                                       <span style="display: {$additional-fees-display};"> + {$additional-fees} {$currency}</span><br/>
                                   </span>
                                   <span style="font-size: medium; color: #4f4f4f; display: {$price-comment-display};">
                                       <strong>{$price-comment}</strong><br/>
                                   </span>
                                   <span>
                                       <strong>Server:</strong> {$portal-name}<br/>
                                       <strong>Adresa:</strong> {$address} 
                                       <span style="display: {$address-links-display};">
                                           <a target="_blank" rel="noopener noreferrer" href="https://www.google.com/maps/search/?api=1&query={$address-encoded}"><img alt="Google's map logo" title="Otvoriť v Google Mapách" src="https://upload.wikimedia.org/wikipedia/commons/thumb/a/aa/Google_Maps_icon_%282020%29.svg/32px-Google_Maps_icon_%282020%29.svg.png?20200218211225" style="margin-left: 6px; max-height: 14px; width: auto" /></a>
                                           <a target="_blank" rel="noopener noreferrer" href="https://mapy.com/fnc/v1/search?query={$address-encoded}"><img alt="Icon of Mapy.com" title="Otvoriť na Mapy.com" src="https://mapy.com/img/favicon/common/plain/favicon-32x32.png" style="max-height: 14px; width: auto"></a>
                                           <a target="_blank" rel="noopener noreferrer" href="https://maps.apple.com/?q={$address-encoded}"><img alt="Icon of Apple Maps" title="Otvoriť v Apple Mapách" src="https://maps.apple.com/static/maps-app-web-client/images/maps-app-icon-120x120.png" style="max-height: 14px; width: auto"></a>
                                       </span><br/>
                                       <strong>Výmera:</strong> {$floor-area}<br/>
                                       <strong>Dispozícia:</strong> {$layout}<br/>
                                   </span>
                                   <p style="margin: 0.2em; font-size: small; text-align: justify; display: {$text-display};">{$text}</p>
                               </div>
                               """;
}