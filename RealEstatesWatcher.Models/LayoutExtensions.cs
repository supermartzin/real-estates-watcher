namespace RealEstatesWatcher.Models
{
    public static class LayoutExtensions
    {
        public static string ToDisplayString(this Layout layout) => layout switch
        {
            Layout.OnePlusOne   => "1+1",
            Layout.OnePlusKk    => "1+kk",
            Layout.TwoPlusOne   => "2+1",
            Layout.TwoPlusKk    => "2+kk",
            Layout.ThreePlusOne => "3+1",
            Layout.ThreePlusKk  => "3+kk",
            Layout.FourPlusOne  => "4+1",
            Layout.FourPlusKk   => "4+kk",
            Layout.FivePlusOne  => "5+1",
            Layout.FivePlusKk   => "5+kk",
            Layout.NotSpecified => "Not Specified",
            _                   => "Not Specified"
        };

        public static Layout ToLayout(string layoutValue) => layoutValue switch
        {
            "1+1"  => Layout.OnePlusOne,
            "1+kk" => Layout.OnePlusKk,
            "2+1"  => Layout.TwoPlusOne,
            "2+kk" => Layout.TwoPlusKk,
            "3+1"  => Layout.ThreePlusOne,
            "3+kk" => Layout.ThreePlusKk,
            "4+1"  => Layout.FourPlusOne,
            "4+kk" => Layout.FourPlusKk,
            "5+1"  => Layout.FivePlusOne,
            "5+kk" => Layout.FivePlusKk,
            _      => Layout.NotSpecified
        };
    }
}