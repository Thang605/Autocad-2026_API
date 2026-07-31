using System;
using System.Collections.Generic;

namespace Civil3DCsharp
{
    public class DesignParameters
    {
        public int DesignSpeed { get; set; }
        public double MinRadiusNormal { get; set; }
        public double MinRadiusLimit { get; set; }
        public double MinCurveLength { get; set; }
        public double MinTransitionCurveLength { get; set; }
        public double MaxStraightLength { get; set; }
        public double MinStraightLengthSameDirection { get; set; }
        public double MinStraightLengthReverseDirection { get; set; }
        public double MaxSuperelevation { get; set; }
    }

    public interface IDesignStandard
    {
        string StandardName { get; }
        int[] SupportedSpeeds { get; }
        DesignParameters GetParameters(int speed);
    }

    public static class StandardFactory
    {
        private static List<IDesignStandard> _standards;

        static StandardFactory()
        {
            _standards = new List<IDesignStandard>
            {
                new TCVN4054_2005(),
                new TCVN13592_2022(),
                new TCVN5729_2012(),
                new TCVN10380_2014()
            };
        }

        public static List<IDesignStandard> GetAllStandards() => _standards;
    }

    // TCVN 4054:2005 - Đường Ô Tô
    public class TCVN4054_2005 : IDesignStandard
    {
        public string StandardName => "TCVN 4054:2005 (Đường Ô Tô)";
        public int[] SupportedSpeeds => new[] { 20, 30, 40, 60, 80, 100, 120 };

        public DesignParameters GetParameters(int speed)
        {
            var p = new DesignParameters { DesignSpeed = speed };
            p.MaxStraightLength = 20 * speed;
            p.MinStraightLengthSameDirection = 6 * speed;
            p.MinStraightLengthReverseDirection = 2 * speed; 
            p.MaxSuperelevation = 8.0;

            switch (speed)
            {
                case 20: p.MinRadiusNormal = 30; p.MinRadiusLimit = 15; p.MinCurveLength = 20; p.MinTransitionCurveLength = 20; break;
                case 30: p.MinRadiusNormal = 60; p.MinRadiusLimit = 30; p.MinCurveLength = 30; p.MinTransitionCurveLength = 25; break;
                case 40: p.MinRadiusNormal = 125; p.MinRadiusLimit = 60; p.MinCurveLength = 40; p.MinTransitionCurveLength = 35; break;
                case 60: p.MinRadiusNormal = 250; p.MinRadiusLimit = 125; p.MinCurveLength = 60; p.MinTransitionCurveLength = 50; break;
                case 80: p.MinRadiusNormal = 500; p.MinRadiusLimit = 250; p.MinCurveLength = 80; p.MinTransitionCurveLength = 70; break;
                case 100: p.MinRadiusNormal = 1000; p.MinRadiusLimit = 400; p.MinCurveLength = 100; p.MinTransitionCurveLength = 85; break;
                case 120: p.MinRadiusNormal = 1500; p.MinRadiusLimit = 600; p.MinCurveLength = 120; p.MinTransitionCurveLength = 100; break;
                default: p.MinRadiusNormal = 250; p.MinRadiusLimit = 125; p.MinCurveLength = 60; p.MinTransitionCurveLength = 50; break;
            }
            return p;
        }
    }

    // TCVN 13592:2022 - Đường Đô thị
    public class TCVN13592_2022 : IDesignStandard
    {
        public string StandardName => "TCVN 13592:2022 (Đường Đô thị)";
        public int[] SupportedSpeeds => new[] { 30, 40, 50, 60, 80, 100 };

        public DesignParameters GetParameters(int speed)
        {
            var p = new DesignParameters { DesignSpeed = speed };
            p.MaxStraightLength = 20 * speed;
            p.MinStraightLengthSameDirection = 6 * speed;
            p.MinStraightLengthReverseDirection = 2 * speed; 
            p.MaxSuperelevation = 8.0;

            switch (speed)
            {
                case 30: p.MinRadiusNormal = 60; p.MinRadiusLimit = 30; p.MinCurveLength = 30; p.MinTransitionCurveLength = 25; break;
                case 40: p.MinRadiusNormal = 125; p.MinRadiusLimit = 60; p.MinCurveLength = 40; p.MinTransitionCurveLength = 35; break;
                case 50: p.MinRadiusNormal = 150; p.MinRadiusLimit = 100; p.MinCurveLength = 50; p.MinTransitionCurveLength = 40; break;
                case 60: p.MinRadiusNormal = 250; p.MinRadiusLimit = 125; p.MinCurveLength = 60; p.MinTransitionCurveLength = 50; break;
                case 80: p.MinRadiusNormal = 500; p.MinRadiusLimit = 250; p.MinCurveLength = 80; p.MinTransitionCurveLength = 70; break;
                case 100: p.MinRadiusNormal = 1000; p.MinRadiusLimit = 400; p.MinCurveLength = 100; p.MinTransitionCurveLength = 85; break;
                default: p.MinRadiusNormal = 250; p.MinRadiusLimit = 125; p.MinCurveLength = 60; p.MinTransitionCurveLength = 50; break;
            }
            return p;
        }
    }

    // TCVN 5729:2012 - Đường Cao Tốc
    public class TCVN5729_2012 : IDesignStandard
    {
        public string StandardName => "TCVN 5729:2012 (Đường Cao Tốc)";
        public int[] SupportedSpeeds => new[] { 60, 80, 100, 120 };

        public DesignParameters GetParameters(int speed)
        {
            var p = new DesignParameters { DesignSpeed = speed };
            p.MaxStraightLength = 20 * speed;
            p.MinStraightLengthSameDirection = 6 * speed;
            p.MinStraightLengthReverseDirection = 2 * speed; 
            p.MaxSuperelevation = 8.0;

            switch (speed)
            {
                case 60: p.MinRadiusNormal = 400; p.MinRadiusLimit = 200; p.MinCurveLength = 100; p.MinTransitionCurveLength = 50; break;
                case 80: p.MinRadiusNormal = 700; p.MinRadiusLimit = 400; p.MinCurveLength = 140; p.MinTransitionCurveLength = 70; break;
                case 100: p.MinRadiusNormal = 1200; p.MinRadiusLimit = 700; p.MinCurveLength = 170; p.MinTransitionCurveLength = 85; break;
                case 120: p.MinRadiusNormal = 2000; p.MinRadiusLimit = 1000; p.MinCurveLength = 200; p.MinTransitionCurveLength = 100; break;
                default: p.MinRadiusNormal = 700; p.MinRadiusLimit = 400; p.MinCurveLength = 140; p.MinTransitionCurveLength = 70; break;
            }
            return p;
        }
    }

    // TCVN 10380:2014 - Đường Giao thông nông thôn
    public class TCVN10380_2014 : IDesignStandard
    {
        public string StandardName => "TCVN 10380:2014 (Đường GTNT)";
        public int[] SupportedSpeeds => new[] { 15, 20, 30, 40 };

        public DesignParameters GetParameters(int speed)
        {
            var p = new DesignParameters { DesignSpeed = speed };
            p.MaxStraightLength = 20 * speed;
            p.MinStraightLengthSameDirection = 0; // GTNT thuong khong quy dinh khat khe doan chem
            p.MinStraightLengthReverseDirection = 0; 
            p.MaxSuperelevation = 6.0; // GTNT max thuong la 6%

            switch (speed)
            {
                case 15: p.MinRadiusNormal = 15; p.MinRadiusLimit = 15; p.MinCurveLength = 15; p.MinTransitionCurveLength = 0; break;
                case 20: p.MinRadiusNormal = 30; p.MinRadiusLimit = 15; p.MinCurveLength = 20; p.MinTransitionCurveLength = 0; break;
                case 30: p.MinRadiusNormal = 60; p.MinRadiusLimit = 30; p.MinCurveLength = 30; p.MinTransitionCurveLength = 20; break;
                case 40: p.MinRadiusNormal = 125; p.MinRadiusLimit = 60; p.MinCurveLength = 40; p.MinTransitionCurveLength = 30; break;
                default: p.MinRadiusNormal = 30; p.MinRadiusLimit = 15; p.MinCurveLength = 20; p.MinTransitionCurveLength = 0; break;
            }
            return p;
        }
    }
}
