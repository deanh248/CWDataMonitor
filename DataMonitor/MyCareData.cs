
using System;

namespace DataMonitor
{
    class MyCareData
    {
        public string packageName = "";
        public MyCarePackageType package = MyCarePackageType.Unknown;
        public double packageSize = -1;
        public double packageRemainingData = -1;
        public DateTime packageLastUsageTime = DateTime.MinValue;
        public string packageLastUsageTimeRaw = "";

        public static MyCarePackageType ParsePackage(string s)
        {
            switch (s)
            {
                case "Intro":
                    return MyCarePackageType.Intro;
                case "LitePro":
                    return MyCarePackageType.LitePro;
                case "PlusPro":
                    return MyCarePackageType.PlusPro;
                case "AdvantagePro":
                    return MyCarePackageType.AdvantagePro;
                case "PremiumPro":
                    return MyCarePackageType.PremiumPro;
                case "UltimatePro":
                    return MyCarePackageType.UltimatePro;

                // UNTESTED
                case "Wireless 245":
                    return MyCarePackageType.Wireless245;
                case "Wireless 495":
                    return MyCarePackageType.Wireless495;
                case "Wireless 795":
                    return MyCarePackageType.Wireless795;
                case "Wireless 1495":
                    return MyCarePackageType.Wireless1495;
                case "Wireless245":
                    return MyCarePackageType.Wireless245;
                case "Wireless495":
                    return MyCarePackageType.Wireless495;
                case "Wireless795":
                    return MyCarePackageType.Wireless795;
                case "Wireless1495":
                    return MyCarePackageType.Wireless1495;
                default:
                    return MyCarePackageType.Unknown;
            }
        }
    }

    enum MyCarePackageType
    {
        Unknown = 0,
        Intro = 8,
        LitePro = 35,
        PlusPro = 70,
        AdvantagePro = 145,
        PremiumPro = 230,
        UltimatePro = 500,
        Wireless245 = 10,
        Wireless495 = 25,
        Wireless795 = 50,
        Wireless1495 = 100,
    }
}
