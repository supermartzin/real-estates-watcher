using System.Collections.Generic;

using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsFilters.BasicFilter
{
    public class BasicParametersAdPostsFilterSettings
    {
        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public ISet<Layout> Layouts { get; set; }

        public decimal? MinFloorArea { get; set; }

        public decimal? MaxFloorArea { get; set; }

        public BasicParametersAdPostsFilterSettings()
        {
            Layouts = new HashSet<Layout>();
        }
    }
}