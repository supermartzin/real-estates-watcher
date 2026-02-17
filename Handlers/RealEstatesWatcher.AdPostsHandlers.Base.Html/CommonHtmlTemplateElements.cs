namespace RealEstatesWatcher.AdPostsHandlers.Base.Html;

public static class CommonHtmlTemplateElements
{
    public const string FullPage = """
                                   <!DOCTYPE html>
                                   <html lang="en">
                                   <head>
                                       <meta charset="utf-8">
                                       <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                       <title>Real Estate Advertisements</title>
                                       <link rel="preconnect" href="https://fonts.googleapis.com">
                                       <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
                                       <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
                                   </head>
                                   <body style="margin: 0; padding: 0; background-color: #f0f2f5; font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #1a1a2e; line-height: 1.5;">
                                       <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color: #f0f2f5;">
                                           <tr>
                                               <td align="center" style="padding: 24px 16px;">
                                                   <div style="max-width: 640px; margin: 0 auto; text-align: left;">
                                                       <maintitle/>
                                                       <posts/>
                                                   </div>
                                               </td>
                                           </tr>
                                       </table>
                                   </body>
                                   </html>
                                   """;

    public const string TitleNewPosts = """
                                        <h1 style="font-size: 1.75rem; font-weight: 700; color: #1a1a2e; margin-bottom: 20px; padding-bottom: 12px;">🏠 <span>New Real Estate Offer</span></h1>
                                        """;

    public const string TitleInitialPosts = """
                                            <h1 style="font-size: 1.75rem; font-weight: 700; color: #1a1a2e; margin-bottom: 20px; padding-bottom: 12px;">🏠 <span>Current Real Estate Offer</span></h1>
                                            """;

    public const string Post = """
                               <div style="margin-bottom: 20px; background: #ffffff; border-radius: 12px; border: 1px solid #d1d5db; overflow: hidden;">
                                   <div style="width: 100%; max-height: 280px; overflow: hidden; display: {$img-display};">
                                       <img src="{$img-link}" style="width: 100%; height: 280px; object-fit: cover; display: block;" alt="Ad main visual presentation"/>
                                   </div>
                                   <div style="padding: 18px 22px;">
                                           <a href="{$post-link}" style="text-decoration: none;">
                                               <h3 style="margin: 0 0 10px 0; font-size: 1.1rem; font-weight: 600; color: #6366f1; line-height: 1.35;">{$title}</h3>
                                           </a>
                                           <div style="display: {$price-display}; margin-bottom: 12px;">
                                               <span style="font-size: 1.25rem; font-weight: 700; color: #047857;">{$price} {$currency}</span>
                                               <span style="display: {$additional-fees-display}; font-size: 0.85rem; color: #6b7280; margin-left: 4px;">+ {$additional-fees} {$currency}</span>
                                           </div>
                                           <div style="display: {$price-comment-display}; margin-bottom: 12px;">
                                               <span style="font-size: 1.1rem; font-weight: 600; color: #047857;">{$price-comment}</span>
                                           </div>
                                           <table style="border-collapse: collapse; font-size: 0.875rem; color: #374151; width: 100%;">
                                               <tr>
                                                   <td style="padding: 3px 6px 3px 0; color: #6b7280; white-space: nowrap;">Server</td>
                                                   <td style="padding: 3px 0; font-weight: 500;">{$portal-name}</td>
                                               </tr>
                                               <tr>
                                                   <td style="padding: 3px 6px 3px 0; color: #6b7280; white-space: nowrap; vertical-align: middle;">Adresa</td>
                                                   <td style="padding: 3px 0; font-weight: 500; white-space: nowrap;">
                                                       <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="display: inline-table; vertical-align: middle;">
                                                           <tr>
                                                               <td valign="middle" style="padding: 0; font-weight: 500;">{$address}</td>
                                                               <td valign="middle" style="padding: 0 0 0 8px; display: {$address-links-display};"><a target="_blank" rel="noopener noreferrer" href="https://www.google.com/maps/search/?api=1&query={$address-encoded}" style="text-decoration: none;"><img alt="Google's map logo" title="Otvoriť v Google Mapách" src="https://upload.wikimedia.org/wikipedia/commons/thumb/a/aa/Google_Maps_icon_%282020%29.svg/32px-Google_Maps_icon_%282020%29.svg.png?20200218211225" style="height: 16px; width: auto; display: block;" /></a></td>
                                                               <td valign="middle" style="padding: 0 0 0 8px; display: {$address-links-display};"><a target="_blank" rel="noopener noreferrer" href="https://mapy.com/fnc/v1/search?query={$address-encoded}" style="text-decoration: none;"><img alt="Icon of Mapy.com" title="Otvoriť na Mapy.com" src="https://mapy.com/img/favicon/common/plain/favicon-32x32.png" style="height: 16px; width: auto; display: block;" /></a></td>
                                                               <td valign="middle" style="padding: 0 0 0 8px; display: {$address-links-display};"><a target="_blank" rel="noopener noreferrer" href="https://maps.apple.com/?q={$address-encoded}" style="text-decoration: none;"><img alt="Icon of Apple Maps" title="Otvoriť v Apple Mapách" src="https://maps.apple.com/static/maps-app-web-client/images/maps-app-icon-120x120.png" style="height: 16px; width: auto; display: block;" /></a></td>
                                                           </tr>
                                                       </table>
                                                   </td>
                                               </tr>
                                               <tr>
                                                   <td style="padding: 3px 6px 3px 0; color: #6b7280; white-space: nowrap;">Výmera</td>
                                                   <td style="padding: 3px 0; font-weight: 500;">{$floor-area}</td>
                                               </tr>
                                               <tr>
                                                   <td style="padding: 3px 6px 3px 0; color: #6b7280; white-space: nowrap;">Dispozícia</td>
                                                   <td style="padding: 3px 0; font-weight: 500;">{$layout}</td>
                                               </tr>
                                           </table>
                                           <p style="margin: 10px 0 0 0; padding-top: 10px; border-top: 1px solid #e5e7eb; font-size: 0.85rem; color: #4b5563; text-align: justify; line-height: 1.5; display: {$text-display};">{$text}</p>
                                       </div>
                               </div>
                               """;
}