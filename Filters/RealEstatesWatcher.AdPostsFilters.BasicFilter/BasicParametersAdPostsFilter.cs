using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdPostsFilters.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsFilters.BasicFilter;

public class BasicParametersAdPostsFilter(BasicParametersAdPostsFilterSettings settings,
                                          ILogger<BasicParametersAdPostsFilter>? logger = default) : IRealEstateAdPostsFilter
{
    private readonly ILogger<BasicParametersAdPostsFilter>? _logger = logger;
    private readonly BasicParametersAdPostsFilterSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public IEnumerable<RealEstateAdPost> Filter(IEnumerable<RealEstateAdPost> adPosts)
    {
        ArgumentNullException.ThrowIfNull(adPosts);

        return adPosts.Where(post =>
        {
            // min price filter
            if (_settings.MinPrice is not null && post.Price != decimal.Zero && post.Price < _settings.MinPrice)
                return false;
            // max price filter
            if (_settings.MaxPrice is not null && post.Price != decimal.Zero && post.Price > _settings.MaxPrice)
                return false;
            // layouts filter
            if (_settings.Layouts.Count > 0 && post.Layout != Layout.NotSpecified &&
                !_settings.Layouts.Contains(post.Layout))
                return false;
            // min floor area filter
            if (_settings.MinFloorArea is not null && post.FloorArea != decimal.Zero &&
                post.FloorArea < _settings.MinFloorArea)
                return false;
            // max floor area filter
            if (_settings.MaxFloorArea is not null && post.FloorArea != decimal.Zero &&
                post.FloorArea > _settings.MaxFloorArea)
                return false;

            return true;
        });
    }
}