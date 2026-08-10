using System;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.Civil.DatabaseServices;

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
        public double MinRadiusNoSuperelevation { get; set; }
    }

    public class ProfileDesignParameters
    {
        public int DesignSpeed { get; set; }
        public string TerrainType { get; set; } = "Đồng bằng"; // Đồng bằng, Đồi, Núi
        public double MaxGrade { get; set; } // % Độ dốc dọc tối đa (i_max)
        public double MinGrade { get; set; } = 0.5; // % Độ dốc dọc tối thiểu (i_min)
        public double MinConvexRadiusNormal { get; set; } // R_loi thông thường (m)
        public double MinConvexRadiusLimit { get; set; } // R_loi giới hạn (m)
        public double MinConcaveRadiusNormal { get; set; } // R_lom thông thường (m)
        public double MinConcaveRadiusLimit { get; set; } // R_lom giới hạn (m)
        public double MinVerticalCurveLength { get; set; } // Chiều dài đường cong đứng tối thiểu (m)
        public double MinGradeTangentLength { get; set; } // Chiều dài đoạn dốc tối thiểu (m)
        public double AlgebraicGradeDiffThreshold { get; set; } = 1.0; // % Hiệu dốc đại số ngưỡng bắt buộc cắm cong đứng
    }

    public interface IDesignStandard
    {
        string StandardName { get; }
        int[] SupportedSpeeds { get; }
        DesignParameters GetParameters(int speed);
        ProfileDesignParameters GetProfileParameters(int speed, string terrain = "Đồng bằng");
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
                case 20: p.MinRadiusNormal = 30; p.MinRadiusLimit = 15; p.MinCurveLength = 20; p.MinTransitionCurveLength = 20; p.MinRadiusNoSuperelevation = 150; break;
                case 30: p.MinRadiusNormal = 60; p.MinRadiusLimit = 30; p.MinCurveLength = 30; p.MinTransitionCurveLength = 25; p.MinRadiusNoSuperelevation = 350; break;
                case 40: p.MinRadiusNormal = 125; p.MinRadiusLimit = 60; p.MinCurveLength = 40; p.MinTransitionCurveLength = 35; p.MinRadiusNoSuperelevation = 600; break;
                case 60: p.MinRadiusNormal = 250; p.MinRadiusLimit = 125; p.MinCurveLength = 60; p.MinTransitionCurveLength = 50; p.MinRadiusNoSuperelevation = 1500; break;
                case 80: p.MinRadiusNormal = 500; p.MinRadiusLimit = 250; p.MinCurveLength = 80; p.MinTransitionCurveLength = 70; p.MinRadiusNoSuperelevation = 2500; break;
                case 100: p.MinRadiusNormal = 1000; p.MinRadiusLimit = 400; p.MinCurveLength = 100; p.MinTransitionCurveLength = 85; p.MinRadiusNoSuperelevation = 4000; break;
                case 120: p.MinRadiusNormal = 1500; p.MinRadiusLimit = 600; p.MinCurveLength = 120; p.MinTransitionCurveLength = 100; p.MinRadiusNoSuperelevation = 5000; break;
                default: p.MinRadiusNormal = 250; p.MinRadiusLimit = 125; p.MinCurveLength = 60; p.MinTransitionCurveLength = 50; p.MinRadiusNoSuperelevation = 1500; break;
            }
            return p;
        }

        /// <summary>
        /// Giá trị theo TCVN 4054:2005:
        /// - Bảng 15: Độ dốc dọc tối đa theo cấp đường và địa hình
        /// - Bảng 17: Chiều dài tối thiểu đoạn đổi dốc
        /// - Bảng 19: Bán kính đường cong đứng tối thiểu (thông thường & giới hạn)
        /// - Mục 5.8.1: Ngưỡng Δi bắt buộc cong đứng (V≥60: 1%, V<60: 2%)
        /// </summary>
        public ProfileDesignParameters GetProfileParameters(int speed, string terrain = "Đồng bằng")
        {
            // Ngưỡng Δi: V≥60 → 1.0%, V<60 → 2.0% (TCVN 4054:2005 mục 5.8.1)
            double threshold = speed >= 60 ? 1.0 : 2.0;
            var p = new ProfileDesignParameters { DesignSpeed = speed, TerrainType = terrain, MinGrade = 0.5, AlgebraicGradeDiffThreshold = threshold };
            bool isMountain = terrain.Contains("Núi");
            bool isHilly = terrain.Contains("Đồi");

            switch (speed)
            {
                case 120: // Cấp I
                    p.MaxGrade = isMountain || isHilly ? 4.0 : 3.0;
                    p.MinConvexRadiusNormal = 17000; p.MinConvexRadiusLimit = 11000; // Bảng 19
                    p.MinConcaveRadiusNormal = 6000;  p.MinConcaveRadiusLimit = 4000;  // Bảng 19
                    p.MinVerticalCurveLength = 100; p.MinGradeTangentLength = 300;    // Bảng 17
                    break;
                case 100: // Cấp II
                    p.MaxGrade = isMountain || isHilly ? 5.0 : 4.0;
                    p.MinConvexRadiusNormal = 10000; p.MinConvexRadiusLimit = 6000; // Bảng 19
                    p.MinConcaveRadiusNormal = 5000;  p.MinConcaveRadiusLimit = 3000; // Bảng 19
                    p.MinVerticalCurveLength = 85; p.MinGradeTangentLength = 250;   // Bảng 17
                    break;
                case 80: // Cấp III
                    p.MaxGrade = isMountain ? 7.0 : (isHilly ? 6.0 : 5.0);
                    p.MinConvexRadiusNormal = 5000; p.MinConvexRadiusLimit = 4000; // Bảng 19
                    p.MinConcaveRadiusNormal = 3000; p.MinConcaveRadiusLimit = 2000; // Bảng 19
                    p.MinVerticalCurveLength = 70; p.MinGradeTangentLength = 150;   // Bảng 17
                    break;
                case 60: // Cấp IV
                    p.MaxGrade = isMountain ? 8.0 : (isHilly ? 7.0 : 6.0);
                    p.MinConvexRadiusNormal = 4000; p.MinConvexRadiusLimit = 2500; // Bảng 19
                    p.MinConcaveRadiusNormal = 1500; p.MinConcaveRadiusLimit = 1000; // Bảng 19
                    p.MinVerticalCurveLength = 50; p.MinGradeTangentLength = 100;   // Bảng 17
                    break;
                case 40: // Cấp V
                    p.MaxGrade = isMountain ? 9.0 : (isHilly ? 8.0 : 7.0);
                    p.MinConvexRadiusNormal = 1000; p.MinConvexRadiusLimit = 700;  // Bảng 19
                    p.MinConcaveRadiusNormal = 700;  p.MinConcaveRadiusLimit = 450;  // Bảng 19
                    p.MinVerticalCurveLength = 35; p.MinGradeTangentLength = 70;    // Bảng 17
                    break;
                case 30: // Cấp VI
                    p.MaxGrade = isMountain ? 10.0 : (isHilly ? 9.0 : 8.0);
                    p.MinConvexRadiusNormal = 600;  p.MinConvexRadiusLimit = 400;  // Bảng 19
                    p.MinConcaveRadiusNormal = 400;  p.MinConcaveRadiusLimit = 250;  // Bảng 19
                    p.MinVerticalCurveLength = 25; p.MinGradeTangentLength = 60;    // Bảng 17
                    break;
                case 20: // Ngoại suy
                default:
                    p.MaxGrade = isMountain ? 11.0 : (isHilly ? 10.0 : 9.0);
                    p.MinConvexRadiusNormal = 200;  p.MinConvexRadiusLimit = 200;  // Bảng 19
                    p.MinConcaveRadiusNormal = 200;  p.MinConcaveRadiusLimit = 100;  // Bảng 19
                    p.MinVerticalCurveLength = 20; p.MinGradeTangentLength = 50;    // Bảng 17
                    break;
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
                case 30: p.MinRadiusNormal = 60; p.MinRadiusLimit = 30; p.MinCurveLength = 30; p.MinTransitionCurveLength = 25; p.MinRadiusNoSuperelevation = 350; break;
                case 40: p.MinRadiusNormal = 125; p.MinRadiusLimit = 60; p.MinCurveLength = 40; p.MinTransitionCurveLength = 35; p.MinRadiusNoSuperelevation = 600; break;
                case 50: p.MinRadiusNormal = 150; p.MinRadiusLimit = 100; p.MinCurveLength = 50; p.MinTransitionCurveLength = 40; p.MinRadiusNoSuperelevation = 900; break;
                case 60: p.MinRadiusNormal = 250; p.MinRadiusLimit = 125; p.MinCurveLength = 60; p.MinTransitionCurveLength = 50; p.MinRadiusNoSuperelevation = 1500; break;
                case 80: p.MinRadiusNormal = 500; p.MinRadiusLimit = 250; p.MinCurveLength = 80; p.MinTransitionCurveLength = 70; p.MinRadiusNoSuperelevation = 2500; break;
                case 100: p.MinRadiusNormal = 1000; p.MinRadiusLimit = 400; p.MinCurveLength = 100; p.MinTransitionCurveLength = 85; p.MinRadiusNoSuperelevation = 4000; break;
                default: p.MinRadiusNormal = 250; p.MinRadiusLimit = 125; p.MinCurveLength = 60; p.MinTransitionCurveLength = 50; p.MinRadiusNoSuperelevation = 1500; break;
            }
            return p;
        }

        /// <summary>
        /// Giá trị theo TCVN 13592:2022 Bảng 23/25/26.
        /// Ngưỡng Δi: V≥60 → 1%, V<60 → 2% (Mục 8.8.1)
        /// Tham khảo tương tự TCVN 4054:2005 khi không có giá trị riêng.
        /// </summary>
        public ProfileDesignParameters GetProfileParameters(int speed, string terrain = "Đồng bằng")
        {
            double threshold = speed >= 60 ? 1.0 : 2.0;
            var p = new ProfileDesignParameters { DesignSpeed = speed, TerrainType = terrain, MinGrade = 0.5, AlgebraicGradeDiffThreshold = threshold };

            switch (speed)
            {
                case 100:
                    p.MaxGrade = 4.0;
                    p.MinConvexRadiusNormal = 10000; p.MinConvexRadiusLimit = 6000;
                    p.MinConcaveRadiusNormal = 5000;  p.MinConcaveRadiusLimit = 3000;
                    p.MinVerticalCurveLength = 85; p.MinGradeTangentLength = 250;
                    break;
                case 80:
                    p.MaxGrade = 5.0;
                    p.MinConvexRadiusNormal = 5000; p.MinConvexRadiusLimit = 4000;
                    p.MinConcaveRadiusNormal = 3000; p.MinConcaveRadiusLimit = 2000;
                    p.MinVerticalCurveLength = 70; p.MinGradeTangentLength = 150;
                    break;
                case 60:
                    p.MaxGrade = 6.0;
                    p.MinConvexRadiusNormal = 4000; p.MinConvexRadiusLimit = 2500;
                    p.MinConcaveRadiusNormal = 1500; p.MinConcaveRadiusLimit = 1000;
                    p.MinVerticalCurveLength = 50; p.MinGradeTangentLength = 100;
                    break;
                case 50:
                    p.MaxGrade = 7.0;
                    p.MinConvexRadiusNormal = 2500; p.MinConvexRadiusLimit = 1500;
                    p.MinConcaveRadiusNormal = 1000; p.MinConcaveRadiusLimit = 700;
                    p.MinVerticalCurveLength = 40; p.MinGradeTangentLength = 80;
                    break;
                case 40:
                    p.MaxGrade = 8.0;
                    p.MinConvexRadiusNormal = 1000; p.MinConvexRadiusLimit = 700;
                    p.MinConcaveRadiusNormal = 700;  p.MinConcaveRadiusLimit = 450;
                    p.MinVerticalCurveLength = 35; p.MinGradeTangentLength = 70;
                    break;
                case 30:
                default:
                    p.MaxGrade = 9.0;
                    p.MinConvexRadiusNormal = 600;  p.MinConvexRadiusLimit = 400;
                    p.MinConcaveRadiusNormal = 400;  p.MinConcaveRadiusLimit = 250;
                    p.MinVerticalCurveLength = 25; p.MinGradeTangentLength = 60;
                    break;
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
            p.MaxStraightLength = 4000; // TCVN 5729:2012 Mục 7.2: không nên thiết kế các đoạn tuyến thẳng trên đường cao tốc dài quá 4 km (4000 m)
            p.MinStraightLengthSameDirection = 6 * speed;
            p.MinStraightLengthReverseDirection = 2 * speed; 
            p.MaxSuperelevation = 8.0;

            switch (speed)
            {
                case 60: p.MinRadiusNormal = 400; p.MinRadiusLimit = 200; p.MinCurveLength = 100; p.MinTransitionCurveLength = 50; p.MinRadiusNoSuperelevation = 1500; break;
                case 80: p.MinRadiusNormal = 700; p.MinRadiusLimit = 400; p.MinCurveLength = 140; p.MinTransitionCurveLength = 70; p.MinRadiusNoSuperelevation = 2500; break;
                case 100: p.MinRadiusNormal = 1200; p.MinRadiusLimit = 700; p.MinCurveLength = 170; p.MinTransitionCurveLength = 85; p.MinRadiusNoSuperelevation = 4000; break;
                case 120: p.MinRadiusNormal = 2000; p.MinRadiusLimit = 1000; p.MinCurveLength = 200; p.MinTransitionCurveLength = 100; p.MinRadiusNoSuperelevation = 5000; break;
                default: p.MinRadiusNormal = 700; p.MinRadiusLimit = 400; p.MinCurveLength = 140; p.MinTransitionCurveLength = 70; p.MinRadiusNoSuperelevation = 2500; break;
            }
            return p;
        }

        /// <summary>
        /// Giá trị theo TCVN 5729:2012:
        /// - Bảng 4: Độ dốc dọc tối đa (lên dốc / xuống dốc)
        /// - Bảng 6: Bán kính đường cong đứng tối thiểu (thông thường & giới hạn)
        /// - Mọi chỗ đổi dốc đều phải bố trí ĐCĐ → ngưỡng Δi = 0.5%
        /// </summary>
        public ProfileDesignParameters GetProfileParameters(int speed, string terrain = "Đồng bằng")
        {
            // Đường cao tốc: mọi chỗ đổi dốc đều phải bố trí ĐCĐ (TCVN 5729:2012 mục 7.4)
            var p = new ProfileDesignParameters { DesignSpeed = speed, TerrainType = terrain, MinGrade = 0.5, AlgebraicGradeDiffThreshold = 0.5 };

            switch (speed)
            {
                case 120: // Cấp 120
                    p.MaxGrade = 4.0; // Bảng 4: lên dốc 4%, xuống dốc 5%
                    p.MinConvexRadiusNormal = 17000; p.MinConvexRadiusLimit = 12000; // Bảng 6
                    p.MinConcaveRadiusNormal = 6000;  p.MinConcaveRadiusLimit = 5000;  // Bảng 6
                    p.MinVerticalCurveLength = 100; p.MinGradeTangentLength = 300;
                    break;
                case 100: // Cấp 100
                    p.MaxGrade = 5.0; // Bảng 4: lên dốc 5%, xuống dốc 5.5%
                    p.MinConvexRadiusNormal = 10000; p.MinConvexRadiusLimit = 6000; // Bảng 6
                    p.MinConcaveRadiusNormal = 4500;  p.MinConcaveRadiusLimit = 3000; // Bảng 6
                    p.MinVerticalCurveLength = 85; p.MinGradeTangentLength = 250;
                    break;
                case 80: // Cấp 80
                    p.MaxGrade = 6.0; // Bảng 4: lên dốc 6%, xuống dốc 6%
                    p.MinConvexRadiusNormal = 4500; p.MinConvexRadiusLimit = 3000; // Bảng 6
                    p.MinConcaveRadiusNormal = 3000; p.MinConcaveRadiusLimit = 2000; // Bảng 6
                    p.MinVerticalCurveLength = 70; p.MinGradeTangentLength = 200;
                    break;
                case 60: // Cấp 60
                default:
                    p.MaxGrade = 6.0; // Bảng 4: lên dốc 6%, xuống dốc 6%
                    p.MinConvexRadiusNormal = 2000; p.MinConvexRadiusLimit = 1500; // Bảng 6
                    p.MinConcaveRadiusNormal = 2000; p.MinConcaveRadiusLimit = 1000; // Bảng 6
                    p.MinVerticalCurveLength = 50; p.MinGradeTangentLength = 150;
                    break;
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
                case 15: p.MinRadiusNormal = 15; p.MinRadiusLimit = 15; p.MinCurveLength = 15; p.MinTransitionCurveLength = 0; p.MinRadiusNoSuperelevation = 100; break;
                case 20: p.MinRadiusNormal = 30; p.MinRadiusLimit = 15; p.MinCurveLength = 20; p.MinTransitionCurveLength = 0; p.MinRadiusNoSuperelevation = 150; break;
                case 30: p.MinRadiusNormal = 60; p.MinRadiusLimit = 30; p.MinCurveLength = 30; p.MinTransitionCurveLength = 20; p.MinRadiusNoSuperelevation = 350; break;
                case 40: p.MinRadiusNormal = 125; p.MinRadiusLimit = 60; p.MinCurveLength = 40; p.MinTransitionCurveLength = 35; p.MinRadiusNoSuperelevation = 600; break;
                default: p.MinRadiusNormal = 30; p.MinRadiusLimit = 15; p.MinCurveLength = 20; p.MinTransitionCurveLength = 0; p.MinRadiusNoSuperelevation = 150; break;
            }
            return p;
        }

        /// <summary>
        /// Giá trị theo TCVN 10380:2014 Bảng 9.
        /// Ngưỡng Δi: V<60 → 2.0% (tham chiếu TCVN 4054:2005 mục 5.8.1)
        /// </summary>
        public ProfileDesignParameters GetProfileParameters(int speed, string terrain = "Đồng bằng")
        {
            // Đường GTNT: tất cả V < 60 → ngưỡng Δi = 2.0%
            var p = new ProfileDesignParameters { DesignSpeed = speed, TerrainType = terrain, MinGrade = 0.5, AlgebraicGradeDiffThreshold = 2.0 };
            bool isMountain = terrain.Contains("Núi") || terrain.Contains("Đồi");

            switch (speed)
            {
                case 40:
                    p.MaxGrade = isMountain ? 10.0 : 8.0;
                    p.MinConvexRadiusNormal = 1000; p.MinConvexRadiusLimit = 700;  // Tham chiếu TCVN 4054 V=40
                    p.MinConcaveRadiusNormal = 700;  p.MinConcaveRadiusLimit = 450;
                    p.MinVerticalCurveLength = 30; p.MinGradeTangentLength = 70;
                    break;
                case 30:
                    p.MaxGrade = isMountain ? 11.0 : 9.0;
                    p.MinConvexRadiusNormal = 600;  p.MinConvexRadiusLimit = 400;  // Tham chiếu TCVN 4054 V=30
                    p.MinConcaveRadiusNormal = 400;  p.MinConcaveRadiusLimit = 250;
                    p.MinVerticalCurveLength = 25; p.MinGradeTangentLength = 60;
                    break;
                case 20:
                    p.MaxGrade = isMountain ? 13.0 : 10.0;
                    p.MinConvexRadiusNormal = 200;  p.MinConvexRadiusLimit = 200;  // Tham chiếu TCVN 4054 V=20
                    p.MinConcaveRadiusNormal = 200;  p.MinConcaveRadiusLimit = 100;
                    p.MinVerticalCurveLength = 20; p.MinGradeTangentLength = 50;
                    break;
                case 15:
                default:
                    p.MaxGrade = isMountain ? 15.0 : 11.0;
                    p.MinConvexRadiusNormal = 100;  p.MinConvexRadiusLimit = 100;  // Ngoại suy
                    p.MinConcaveRadiusNormal = 100;  p.MinConcaveRadiusLimit = 50;
                    p.MinVerticalCurveLength = 15; p.MinGradeTangentLength = 40;
                    break;
            }
            return p;
        }
    }

    // --- Bổ sung Tiêu chuẩn Mặt Cắt Ngang Đường ---
    public class CrossSectionInputParameters
    {
        public string StandardName { get; set; } = "TCVN 4054:2005";
        public string RoadType { get; set; } = "Đường Cấp III";
        public int DesignSpeed { get; set; } = 60;

        public int LanesCount { get; set; } = 2;
        public double LaneWidth { get; set; } = 3.5;
        public double MedianWidth { get; set; } = 0.0; // Dải phân cách giữa
        public double SafetyStripWidth { get; set; } = 0.0; // Dải an toàn
        public double HardShoulderWidth { get; set; } = 1.5; // Lề gia cố (Đường ô tô)
        public double SoftShoulderWidth { get; set; } = 0.5; // Lề đất (Đường ô tô)
        public double SidewalkWidth { get; set; } = 0.0; // Vỉa hè / Dải đi bộ
        public double GreeneryWidth { get; set; } = 0.0; // Dải cây xanh
        public double BikeLaneWidth { get; set; } = 0.0; // Dải xe thô sơ

        public double RoadwayCrossSlope { get; set; } = 2.0; // % Độ dốc ngang mặt đường
        public double ShoulderCrossSlope { get; set; } = 4.0; // % Độ dốc lề đường
        public double SidewalkCrossSlope { get; set; } = 2.0; // % Độ dốc vỉa hè
        public double TargetRightOfWay { get; set; } = 12.0; // Lộ giới quy hoạch / Chỉ giới đường đỏ (m)

        public bool IsUrbanRoad => StandardName.Contains("13592");

        public double TotalCarriagewayWidth => LanesCount * LaneWidth;
        public double TotalShoulderWidth => 2 * (HardShoulderWidth + SoftShoulderWidth);
        public double TotalProposedWidth => TotalCarriagewayWidth + MedianWidth + (2 * SafetyStripWidth) + TotalShoulderWidth + (2 * SidewalkWidth) + (2 * GreeneryWidth) + (2 * BikeLaneWidth);
    }

    public class CrossSectionStandardReq
    {
        public string StandardName { get; set; }
        public string RoadType { get; set; }
        public int DesignSpeed { get; set; }
        public int MinLanesCount { get; set; } = 2;
        public double MinLaneWidth { get; set; } = 3.5;
        public double StandardLaneWidth { get; set; } = 3.5;
        public double MinShoulderWidth { get; set; } = 1.5; // Tổng lề (Cho đường ô tô)
        public double MinHardShoulderWidth { get; set; } = 1.0; // Lề gia cố
        public double MinSafetyStripWidth { get; set; } = 0.5; // Dải an toàn (bắt buộc cho Vtk >= 50-60km/h)
        public double RecommendedRightOfWay { get; set; } = 35.0; // Bề rộng lộ giới quy hoạch gợi ý (m)
        public double MinMedianWidth { get; set; } = 0.0;
        public double MinSidewalkWidth { get; set; } = 0.0;
        public double MinRoadwaySlope { get; set; } = 1.5;
        public double MaxRoadwaySlope { get; set; } = 2.5;
        public double MinShoulderSlope { get; set; } = 2.0;
        public double MaxShoulderSlope { get; set; } = 5.0;
        public string Description { get; set; } = "";
    }

    public enum CheckStatus
    {
        Pass,
        Warning,
        Fail
    }

    public class CrossSectionCheckItem
    {
        public string ElementName { get; set; }
        public string ProposedValue { get; set; }
        public string StandardRequirement { get; set; }
        public CheckStatus Status { get; set; }
        public string Note { get; set; }
    }

    public static class CrossSectionEvaluator
    {
        public static List<string> GetRoadTypes(string standardName)
        {
            if (standardName.Contains("4054"))
            {
                return new List<string> { "Đường Cao Tốc", "Đường Cấp I", "Đường Cấp II", "Đường Cấp III", "Đường Cấp IV", "Đường Cấp V", "Đường Cấp VI" };
            }
            else if (standardName.Contains("13592"))
            {
                return new List<string> { "Đường Cao Tốc Đô Thị", "Đường Trục Chính Đô Thị", "Đường Chính Đô Thị", "Đường Liên Khu Vực", "Đường Khu Vực", "Đường Phân Khu Vực", "Đường Nội Bộ" };
            }
            else if (standardName.Contains("10380"))
            {
                return new List<string> { "Đường GTNT Cấp A", "Đường GTNT Cấp B", "Đường GTNT Cấp C", "Đường GTNT Cấp D" };
            }
            else
            {
                return new List<string> { "Đường Cao Tốc", "Đường Cấp I", "Đường Cấp II", "Đường Cấp III" };
            }
        }

        public static List<int> GetSupportedSpeeds(string standardName, string roadType)
        {
            if (standardName.Contains("4054"))
            {
                switch (roadType)
                {
                    case "Đường Cao Tốc": return new List<int> { 120, 100, 80 };
                    case "Đường Cấp I": return new List<int> { 120, 100 };
                    case "Đường Cấp II": return new List<int> { 100, 80 };
                    case "Đường Cấp III": return new List<int> { 80, 60 };
                    case "Đường Cấp IV": return new List<int> { 60, 40 };
                    case "Đường Cấp V": return new List<int> { 40, 30 };
                    case "Đường Cấp VI": return new List<int> { 30, 20 };
                    default: return new List<int> { 60, 80 };
                }
            }
            else if (standardName.Contains("13592"))
            {
                switch (roadType)
                {
                    case "Đường Cao Tốc Đô Thị": return new List<int> { 100, 80 };
                    case "Đường Trục Chính Đô Thị": return new List<int> { 80, 60 };
                    case "Đường Chính Đô Thị": return new List<int> { 60, 50 };
                    case "Đường Liên Khu Vực": return new List<int> { 80, 60 };
                    case "Đường Khu Vực": return new List<int> { 60, 50, 40 };
                    case "Đường Phân Khu Vực": return new List<int> { 50, 40 };
                    case "Đường Nội Bộ": return new List<int> { 40, 30 };
                    default: return new List<int> { 60, 50 };
                }
            }
            else if (standardName.Contains("10380"))
            {
                switch (roadType)
                {
                    case "Đường GTNT Cấp A": return new List<int> { 40, 30 };
                    case "Đường GTNT Cấp B": return new List<int> { 30, 20 };
                    case "Đường GTNT Cấp C": return new List<int> { 20, 15 };
                    case "Đường GTNT Cấp D": return new List<int> { 15 };
                    default: return new List<int> { 30, 20 };
                }
            }
            return new List<int> { 60, 80, 100 };
        }

        public static CrossSectionStandardReq GetRequirement(string standardName, string roadType, int speed)
        {
            var req = new CrossSectionStandardReq
            {
                StandardName = standardName,
                RoadType = roadType,
                DesignSpeed = speed
            };

            if (standardName.Contains("4054")) // TCVN 4054:2005 Đường Ô tô
            {
                switch (roadType)
                {
                    case "Đường Cao Tốc":
                        req.MinLanesCount = 4; req.MinLaneWidth = 3.75; req.StandardLaneWidth = 3.75;
                        req.MinShoulderWidth = 2.5; req.MinHardShoulderWidth = 2.5; req.MinMedianWidth = 1.5; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.75; req.RecommendedRightOfWay = 60.0;
                        req.Description = "Đường cao tốc ngoài đô thị (TCVN 4054:2005 / TCVN 5729:2012)";
                        break;
                    case "Đường Cấp I":
                        req.MinLanesCount = 4; req.MinLaneWidth = 3.75; req.StandardLaneWidth = 3.75;
                        req.MinShoulderWidth = 3.0; req.MinHardShoulderWidth = 2.5; req.MinMedianWidth = 2.0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.75; req.RecommendedRightOfWay = 50.0;
                        req.Description = "Đường cấp I (TCVN 4054:2005)";
                        break;
                    case "Đường Cấp II":
                        req.MinLanesCount = 2; req.MinLaneWidth = 3.75; req.StandardLaneWidth = 3.75;
                        req.MinShoulderWidth = 2.5; req.MinHardShoulderWidth = 2.0; req.MinMedianWidth = 0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.50; req.RecommendedRightOfWay = 40.0;
                        req.Description = "Đường cấp II (TCVN 4054:2005)";
                        break;
                    case "Đường Cấp III":
                        req.MinLanesCount = 2; req.MinLaneWidth = 3.5; req.StandardLaneWidth = 3.5;
                        req.MinShoulderWidth = 2.0; req.MinHardShoulderWidth = 1.5; req.MinMedianWidth = 0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = speed >= 60 ? 0.50 : 0.25; req.RecommendedRightOfWay = 30.0;
                        req.Description = "Đường cấp III (TCVN 4054:2005)";
                        break;
                    case "Đường Cấp IV":
                        req.MinLanesCount = 2; req.MinLaneWidth = 3.0; req.StandardLaneWidth = 3.5;
                        req.MinShoulderWidth = 1.5; req.MinHardShoulderWidth = 1.0; req.MinMedianWidth = 0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.25; req.RecommendedRightOfWay = 20.0;
                        req.Description = "Đường cấp IV (TCVN 4054:2005)";
                        break;
                    case "Đường Cấp V":
                        req.MinLanesCount = 1; req.MinLaneWidth = 3.5; req.StandardLaneWidth = 3.5;
                        req.MinShoulderWidth = 1.0; req.MinHardShoulderWidth = 0.5; req.MinMedianWidth = 0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.0; req.RecommendedRightOfWay = 15.0;
                        req.Description = "Đường cấp V (TCVN 4054:2005)";
                        break;
                    case "Đường Cấp VI":
                    default:
                        req.MinLanesCount = 1; req.MinLaneWidth = 3.0; req.StandardLaneWidth = 3.5;
                        req.MinShoulderWidth = 0.5; req.MinHardShoulderWidth = 0.0; req.MinMedianWidth = 0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.0; req.RecommendedRightOfWay = 12.0;
                        req.Description = "Đường cấp VI (TCVN 4054:2005)";
                        break;
                }
            }
            else if (standardName.Contains("13592")) // TCVN 13592:2022 Đường Đô thị
            {
                switch (roadType)
                {
                    case "Đường Cao Tốc Đô Thị":
                        req.MinLanesCount = 4; req.MinLaneWidth = 3.75; req.StandardLaneWidth = 3.75;
                        req.MinShoulderWidth = 2.0; req.MinHardShoulderWidth = 1.5; req.MinMedianWidth = 1.5; req.MinSidewalkWidth = 3.0;
                        req.MinSafetyStripWidth = 0.75; req.RecommendedRightOfWay = 60.0;
                        req.Description = "Đường cao tốc đô thị (TCVN 13592:2022)";
                        break;
                    case "Đường Trục Chính Đô Thị":
                        req.MinLanesCount = 4; req.MinLaneWidth = 3.75; req.StandardLaneWidth = 3.75;
                        req.MinShoulderWidth = 0.0; req.MinHardShoulderWidth = 0.0; req.MinMedianWidth = 2.0; req.MinSidewalkWidth = 4.5;
                        req.MinSafetyStripWidth = 0.50; req.RecommendedRightOfWay = 50.0;
                        req.Description = "Đường trục chính đô thị (TCVN 13592:2022)";
                        break;
                    case "Đường Chính Đô Thị":
                        req.MinLanesCount = 4; req.MinLaneWidth = 3.5; req.StandardLaneWidth = 3.75;
                        req.MinShoulderWidth = 0.0; req.MinHardShoulderWidth = 0.0; req.MinMedianWidth = 1.5; req.MinSidewalkWidth = 3.0;
                        req.MinSafetyStripWidth = 0.50; req.RecommendedRightOfWay = 40.0;
                        req.Description = "Đường chính đô thị (TCVN 13592:2022)";
                        break;
                    case "Đường Liên Khu Vực":
                        req.MinLanesCount = 4; req.MinLaneWidth = 3.5; req.StandardLaneWidth = 3.75;
                        req.MinShoulderWidth = 0.0; req.MinHardShoulderWidth = 0.0; req.MinMedianWidth = 1.5; req.MinSidewalkWidth = 3.0;
                        req.MinSafetyStripWidth = 0.50; req.RecommendedRightOfWay = 35.0;
                        req.Description = "Đường liên khu vực (TCVN 13592:2022)";
                        break;
                    case "Đường Khu Vực":
                        req.MinLanesCount = 2; req.MinLaneWidth = 3.5; req.StandardLaneWidth = 3.5;
                        req.MinShoulderWidth = 0.0; req.MinHardShoulderWidth = 0.0; req.MinMedianWidth = 0; req.MinSidewalkWidth = 3.0;
                        req.MinSafetyStripWidth = speed >= 50 ? 0.25 : 0.0; req.RecommendedRightOfWay = 25.0;
                        req.Description = "Đường khu vực (TCVN 13592:2022)";
                        break;
                    case "Đường Phân Khu Vực":
                        req.MinLanesCount = 2; req.MinLaneWidth = 3.25; req.StandardLaneWidth = 3.5;
                        req.MinShoulderWidth = 0.0; req.MinHardShoulderWidth = 0.0; req.MinMedianWidth = 0; req.MinSidewalkWidth = 2.0;
                        req.MinSafetyStripWidth = 0.0; req.RecommendedRightOfWay = 17.5;
                        req.Description = "Đường phân khu vực (TCVN 13592:2022)";
                        break;
                    case "Đường Nội Bộ":
                    default:
                        req.MinLanesCount = 2; req.MinLaneWidth = 3.0; req.StandardLaneWidth = 3.25;
                        req.MinShoulderWidth = 0.0; req.MinHardShoulderWidth = 0.0; req.MinMedianWidth = 0; req.MinSidewalkWidth = 1.5;
                        req.MinSafetyStripWidth = 0.0; req.RecommendedRightOfWay = 12.0;
                        req.Description = "Đường nội bộ (TCVN 13592:2022)";
                        break;
                }
            }
            else if (standardName.Contains("10380")) // TCVN 10380:2014 Đường GTNT
            {
                switch (roadType)
                {
                    case "Đường GTNT Cấp A":
                        req.MinLanesCount = 1; req.MinLaneWidth = 3.5; req.StandardLaneWidth = 3.5;
                        req.MinShoulderWidth = 1.0; req.MinHardShoulderWidth = 0.5; req.MinMedianWidth = 0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.25; req.RecommendedRightOfWay = 15.0;
                        req.Description = "Đường giao thông nông thôn Cấp A (TCVN 10380:2014)";
                        break;
                    case "Đường GTNT Cấp B":
                        req.MinLanesCount = 1; req.MinLaneWidth = 3.0; req.StandardLaneWidth = 3.5;
                        req.MinShoulderWidth = 0.75; req.MinHardShoulderWidth = 0.25; req.MinMedianWidth = 0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.0; req.RecommendedRightOfWay = 12.0;
                        req.Description = "Đường giao thông nông thôn Cấp B (TCVN 10380:2014)";
                        break;
                    case "Đường GTNT Cấp C":
                        req.MinLanesCount = 1; req.MinLaneWidth = 3.0; req.StandardLaneWidth = 3.0;
                        req.MinShoulderWidth = 0.5; req.MinHardShoulderWidth = 0.0; req.MinMedianWidth = 0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.0; req.RecommendedRightOfWay = 10.0;
                        req.Description = "Đường giao thông nông thôn Cấp C (TCVN 10380:2014)";
                        break;
                    case "Đường GTNT Cấp D":
                    default:
                        req.MinLanesCount = 1; req.MinLaneWidth = 1.5; req.StandardLaneWidth = 3.0;
                        req.MinShoulderWidth = 0.5; req.MinHardShoulderWidth = 0.0; req.MinMedianWidth = 0; req.MinSidewalkWidth = 0;
                        req.MinSafetyStripWidth = 0.0; req.RecommendedRightOfWay = 8.0;
                        req.Description = "Đường giao thông nông thôn Cấp D (TCVN 10380:2014)";
                        break;
                }
            }

            return req;
        }

        public static List<CrossSectionCheckItem> Evaluate(CrossSectionInputParameters p)
        {
            var results = new List<CrossSectionCheckItem>();
            var req = GetRequirement(p.StandardName, p.RoadType, p.DesignSpeed);

            // 1. Số làn xe
            var itemLanes = new CrossSectionCheckItem
            {
                ElementName = "Số làn xe",
                ProposedValue = $"{p.LanesCount} làn",
                StandardRequirement = $"≥ {req.MinLanesCount} làn"
            };
            if (p.LanesCount >= req.MinLanesCount)
            {
                itemLanes.Status = CheckStatus.Pass;
                itemLanes.Note = "Thỏa mãn số làn xe tối thiểu";
            }
            else
            {
                itemLanes.Status = CheckStatus.Fail;
                itemLanes.Note = $"Thiếu {req.MinLanesCount - p.LanesCount} làn xe so với tiêu chuẩn";
            }
            results.Add(itemLanes);

            // 2. Bề rộng 1 làn xe
            var itemLaneWidth = new CrossSectionCheckItem
            {
                ElementName = "Bề rộng 1 làn xe (Blan)",
                ProposedValue = $"{p.LaneWidth:F2} m",
                StandardRequirement = $"≥ {req.MinLaneWidth:F2} m (Chuẩn {req.StandardLaneWidth:F2}m)"
            };
            if (p.LaneWidth >= req.MinLaneWidth)
            {
                itemLaneWidth.Status = CheckStatus.Pass;
                itemLaneWidth.Note = p.LaneWidth >= req.StandardLaneWidth ? "Đạt bề rộng làn tiêu chuẩn" : "Đạt mức tối thiểu cho phép";
            }
            else
            {
                itemLaneWidth.Status = CheckStatus.Fail;
                itemLaneWidth.Note = $"Thiếu {(req.MinLaneWidth - p.LaneWidth):F2}m so với mức tối thiểu quy định";
            }
            results.Add(itemLaneWidth);

            // 3. Tổng bề rộng phần xe chạy
            var itemCarriageway = new CrossSectionCheckItem
            {
                ElementName = "Phần xe chạy (Bxc)",
                ProposedValue = $"{p.TotalCarriagewayWidth:F2} m",
                StandardRequirement = $"≥ {(req.MinLanesCount * req.MinLaneWidth):F2} m",
                Status = p.TotalCarriagewayWidth >= (req.MinLanesCount * req.MinLaneWidth) ? CheckStatus.Pass : CheckStatus.Fail,
                Note = p.TotalCarriagewayWidth >= (req.MinLanesCount * req.MinLaneWidth) ? "Đạt tổng bề rộng xe chạy" : "Chưa đủ bề rộng mặt xe chạy"
            };
            results.Add(itemCarriageway);

            // Dải an toàn (Bdat)
            if (req.MinSafetyStripWidth > 0 || p.SafetyStripWidth > 0)
            {
                var itemSafety = new CrossSectionCheckItem
                {
                    ElementName = "Dải an toàn (Bdat)",
                    ProposedValue = $"{p.SafetyStripWidth:F2} m (Mỗi bên)",
                    StandardRequirement = req.MinSafetyStripWidth > 0 ? $"≥ {req.MinSafetyStripWidth:F2} m (cho Vtk={p.DesignSpeed}km/h)" : "Tùy chọn (0m)"
                };
                if (p.SafetyStripWidth >= req.MinSafetyStripWidth)
                {
                    itemSafety.Status = CheckStatus.Pass;
                    itemSafety.Note = "Dải an toàn đạt yêu cầu theo vận tốc thiết kế";
                }
                else
                {
                    itemSafety.Status = CheckStatus.Fail;
                    itemSafety.Note = $"Dải an toàn thiếu {(req.MinSafetyStripWidth - p.SafetyStripWidth):F2}m cho vận tốc Vtk={p.DesignSpeed}km/h";
                }
                results.Add(itemSafety);
            }

            // 4. Dải phân cách giữa
            if (req.MinMedianWidth > 0 || p.MedianWidth > 0)
            {
                var itemMedian = new CrossSectionCheckItem
                {
                    ElementName = "Dải phân cách giữa (Bdpc)",
                    ProposedValue = $"{p.MedianWidth:F2} m",
                    StandardRequirement = req.MinMedianWidth > 0 ? $"≥ {req.MinMedianWidth:F2} m" : "Tùy chọn (0m)"
                };
                if (p.MedianWidth >= req.MinMedianWidth)
                {
                    itemMedian.Status = CheckStatus.Pass;
                    itemMedian.Note = "Đạt bề rộng dải phân cách giữa";
                }
                else
                {
                    itemMedian.Status = CheckStatus.Fail;
                    itemMedian.Note = $"Dải phân cách giữa thiếu {(req.MinMedianWidth - p.MedianWidth):F2}m";
                }
                results.Add(itemMedian);
            }

            // 5. Lề đường / Cấu tạo Hè phố (Blg)
            if (p.IsUrbanRoad && p.RoadType != "Đường Cao Tốc Đô Thị")
            {
                var itemShoulder = new CrossSectionCheckItem
                {
                    ElementName = "Lề đường / Hè phố (Blg)",
                    ProposedValue = p.HardShoulderWidth > 0 ? $"{p.HardShoulderWidth:F2} m (Mỗi bên)" : "Thay bằng Hè phố Bvh (Không dùng lề đất)",
                    StandardRequirement = "Không bố trí lề đất (TCVN 13592: Bố trí Hè phố Bvh)",
                    Status = CheckStatus.Pass,
                    Note = p.HardShoulderWidth > 0 ? $"Bố trí dải an toàn/lề gia cố rộng {p.HardShoulderWidth:F2}m sát hè phố" : "Thỏa mãn cấu tạo Hè phố đô thị theo TCVN 13592:2022"
                };
                results.Add(itemShoulder);
            }
            else
            {
                if (req.MinShoulderWidth > 0 || p.TotalShoulderWidth > 0)
                {
                    double proposedHardShoulder = p.HardShoulderWidth;
                    var itemShoulder = new CrossSectionCheckItem
                    {
                        ElementName = "Bề rộng lề gia cố (Blgc)",
                        ProposedValue = $"{proposedHardShoulder:F2} m (Mỗi bên)",
                        StandardRequirement = $"≥ {req.MinHardShoulderWidth:F2} m"
                    };
                    if (proposedHardShoulder >= req.MinHardShoulderWidth)
                    {
                        itemShoulder.Status = CheckStatus.Pass;
                        itemShoulder.Note = "Lề gia cố đạt yêu cầu";
                    }
                    else
                    {
                        itemShoulder.Status = CheckStatus.Fail;
                        itemShoulder.Note = $"Lề gia cố thiếu {(req.MinHardShoulderWidth - proposedHardShoulder):F2}m";
                    }
                    results.Add(itemShoulder);
                }
            }

            // 6. Vỉa hè / Dải đi bộ (cho đường đô thị)
            if (req.MinSidewalkWidth > 0 || p.SidewalkWidth > 0)
            {
                var itemSidewalk = new CrossSectionCheckItem
                {
                    ElementName = "Vỉa hè / Dải đi bộ (Bvh)",
                    ProposedValue = $"{p.SidewalkWidth:F2} m (Mỗi bên)",
                    StandardRequirement = req.MinSidewalkWidth > 0 ? $"≥ {req.MinSidewalkWidth:F2} m" : "Không bắt buộc"
                };
                if (p.SidewalkWidth >= req.MinSidewalkWidth)
                {
                    itemSidewalk.Status = CheckStatus.Pass;
                    itemSidewalk.Note = "Vỉa hè đạt quy chuẩn đô thị";
                }
                else
                {
                    itemSidewalk.Status = CheckStatus.Fail;
                    itemSidewalk.Note = $"Vỉa hè thiếu {(req.MinSidewalkWidth - p.SidewalkWidth):F2}m so với quy chuẩn";
                }
                results.Add(itemSidewalk);
            }

            // 7. Độ dốc ngang mặt đường
            var itemRoadSlope = new CrossSectionCheckItem
            {
                ElementName = "Độ dốc ngang mặt đường (im)",
                ProposedValue = $"{p.RoadwayCrossSlope:F1} %",
                StandardRequirement = $"{req.MinRoadwaySlope:F1}% - {req.MaxRoadwaySlope:F1}%"
            };
            if (p.RoadwayCrossSlope >= req.MinRoadwaySlope && p.RoadwayCrossSlope <= req.MaxRoadwaySlope)
            {
                itemRoadSlope.Status = CheckStatus.Pass;
                itemRoadSlope.Note = "Độ dốc thoát nước đạt tiêu chuẩn";
            }
            else
            {
                itemRoadSlope.Status = CheckStatus.Warning;
                itemRoadSlope.Note = "Nằm ngoài dải dốc tiêu chuẩn thông thường (1.5% - 2.5%)";
            }
            results.Add(itemRoadSlope);

            // 8. Độ dốc lề đường
            var itemShoulderSlope = new CrossSectionCheckItem
            {
                ElementName = "Độ dốc ngang lề đường (il)",
                ProposedValue = $"{p.ShoulderCrossSlope:F1} %",
                StandardRequirement = $"{req.MinShoulderSlope:F1}% - {req.MaxShoulderSlope:F1}%"
            };
            if (p.ShoulderCrossSlope >= req.MinShoulderSlope && p.ShoulderCrossSlope <= req.MaxShoulderSlope)
            {
                itemShoulderSlope.Status = CheckStatus.Pass;
                itemShoulderSlope.Note = "Độ dốc lề thoát nước tốt";
            }
            else
            {
                itemShoulderSlope.Status = CheckStatus.Warning;
                itemShoulderSlope.Note = "Cần chú ý kết cấu lề để thoát nước";
            }
            results.Add(itemShoulderSlope);

            // 9. So sánh với Chỉ giới đường đỏ / Lộ giới quy hoạch
            double totalWidth = p.TotalProposedWidth;
            var itemROW = new CrossSectionCheckItem
            {
                ElementName = "Tổng bề rộng MC & Lộ giới (Bcgdd)",
                ProposedValue = $"{totalWidth:F2} m",
                StandardRequirement = p.TargetRightOfWay > 0 ? $"≤ {p.TargetRightOfWay:F2} m (Lộ giới quy hoạch)" : "Chưa nhập lộ giới"
            };
            if (p.TargetRightOfWay <= 0)
            {
                itemROW.Status = CheckStatus.Warning;
                itemROW.Note = "Chưa thiết lập lộ giới quy hoạch để so sánh";
            }
            else if (totalWidth <= p.TargetRightOfWay)
            {
                itemROW.Status = CheckStatus.Pass;
                itemROW.Note = $"Đạt nằm trong lộ giới quy hoạch (Dư {(p.TargetRightOfWay - totalWidth):F2}m)";
            }
            else
            {
                itemROW.Status = CheckStatus.Fail;
                itemROW.Note = $"Vượt quá lộ giới quy hoạch {(totalWidth - p.TargetRightOfWay):F2}m";
            }
            results.Add(itemROW);

            return results;
        }
    }

    public class ProfileCheckItem
    {
        public int Index { get; set; }
        public double Station { get; set; }
        public double Elevation { get; set; }
        public string ItemName { get; set; } = "";
        public string ProposedValue { get; set; } = "";
        public string StandardRequirement { get; set; } = "";
        public CheckStatus Status { get; set; }
        public string Note { get; set; } = "";
    }

    public static class ProfileEvaluator
    {
        public static List<ProfileCheckItem> Evaluate(Profile profile, ProfileDesignParameters req)
        {
            var results = new List<ProfileCheckItem>();
            if (profile == null || profile.PVIs == null || profile.PVIs.Count < 2)
            {
                return results;
            }

            int stt = 1;
            var pvis = profile.PVIs;
            int count = pvis.Count;

            // Pre-scan: Thu thập chiều dài cong đứng tại mỗi PVI để tính tangent length chính xác
            double[] curveLengthAtPVI = new double[count];
            for (int k = 0; k < count; k++)
            {
                curveLengthAtPVI[k] = 0;
                try
                {
                    var pviK = pvis[k];
                    double sK = pviK.RawStation;

                    // Scan Entities
                    if (profile.Entities != null)
                    {
                        foreach (dynamic ent in profile.Entities)
                        {
                            try
                            {
                                string entTN = ent.GetType().Name;
                                if (entTN.Contains("Tangent")) continue;
                                double st0 = Convert.ToDouble(ent.StartStation);
                                double st1 = Convert.ToDouble(ent.EndStation);
                                if (sK >= st0 - 1.0 && sK <= st1 + 1.0)
                                {
                                    try { curveLengthAtPVI[k] = Convert.ToDouble(ent.Length); } catch { }
                                    break;
                                }
                            }
                            catch { }
                        }
                    }

                    // Fallback: Reflection on PVI
                    if (curveLengthAtPVI[k] <= 0)
                    {
                        Type pt = pviK.GetType();
                        PropertyInfo pl = pt.GetProperty("Length") ?? pt.GetProperty("CurveLength");
                        if (pl != null)
                        {
                            try { curveLengthAtPVI[k] = Convert.ToDouble(pl.GetValue(pviK, null)); } catch { }
                        }
                        if (curveLengthAtPVI[k] <= 0)
                        {
                            PropertyInfo pl1 = pt.GetProperty("Length1");
                            PropertyInfo pl2 = pt.GetProperty("Length2");
                            if (pl1 != null && pl2 != null)
                            {
                                try
                                {
                                    curveLengthAtPVI[k] = Convert.ToDouble(pl1.GetValue(pviK, null))
                                                        + Convert.ToDouble(pl2.GetValue(pviK, null));
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }

            // 1. Kiểm tra dốc dọc & chiều dài đoạn dốc giữa các PVI liên tiếp
            for (int i = 0; i < count - 1; i++)
            {
                var pvi1 = pvis[i];
                var pvi2 = pvis[i + 1];

                double s1 = pvi1.RawStation;
                double z1 = pvi1.Elevation;
                double s2 = pvi2.RawStation;
                double z2 = pvi2.Elevation;

                double lenPVI = s2 - s1;
                if (lenPVI <= 0.001) continue;

                // Chiều dài đoạn dốc theo TCVN 5729:2012 & TCVN 4054:2005 (Chú thích Bảng 5):
                // L_dốc = 1/4 * L_v1 + L_thẳng + 1/4 * L_v2
                // Với L_thẳng = lenPVI - 1/2 * L_v1 - 1/2 * L_v2
                // => L_dốc = lenPVI - 1/4 * L_v1 - 1/4 * L_v2
                double lenTangent = lenPVI - (curveLengthAtPVI[i] / 2.0) - (curveLengthAtPVI[i + 1] / 2.0); // Đoạn dốc thẳng thuần túy
                if (lenTangent < 0) lenTangent = 0;

                double lenDocTCVN = lenPVI - (curveLengthAtPVI[i] / 4.0) - (curveLengthAtPVI[i + 1] / 4.0); // Chiều dài dốc theo TCVN
                if (lenDocTCVN < 0) lenDocTCVN = 0;

                double slope = (z2 - z1) / lenPVI * 100.0;
                double absSlope = Math.Abs(slope);

                // Check 1.1: Độ dốc lớn nhất (i_max)
                var itemSlopeMax = new ProfileCheckItem
                {
                    Index = stt++,
                    Station = s1,
                    Elevation = z1,
                    ItemName = $"Đoạn dốc Km{(s1 / 1000.0):F3} - Km{(s2 / 1000.0):F3} (i%)",
                    ProposedValue = $"{slope:+0.00;-0.00;0.00} % (L_dốc={lenDocTCVN:F2}m)",
                    StandardRequirement = $"|i| ≤ {req.MaxGrade:F1} % (Vtk={req.DesignSpeed}km/h, {req.TerrainType})"
                };

                if (absSlope > req.MaxGrade + 0.001)
                {
                    itemSlopeMax.Status = CheckStatus.Fail;
                    itemSlopeMax.Note = $"VI PHẠM: Độ dốc |i| = {absSlope:F2}% vượt dốc tối đa {req.MaxGrade:F1}% cho phép!";
                }
                else
                {
                    itemSlopeMax.Status = CheckStatus.Pass;
                    itemSlopeMax.Note = "Thỏa mãn độ dốc dọc tối đa";
                }
                results.Add(itemSlopeMax);

                // Check 1.2: Độ dốc tối thiểu thoát nước (i_min)
                if (absSlope < req.MinGrade - 0.001)
                {
                    results.Add(new ProfileCheckItem
                    {
                        Index = stt++,
                        Station = s1,
                        Elevation = z1,
                        ItemName = $"Thoát nước đoạn dốc Km{(s1 / 1000.0):F3}",
                        ProposedValue = $"{slope:+0.00;-0.00;0.00} %",
                        StandardRequirement = $"|i| ≥ {req.MinGrade:F1} % (Thoát nước rãnh)",
                        Status = CheckStatus.Warning,
                        Note = $"CẢNH BÁO: Độ dốc |i| = {absSlope:F2}% nhỏ hơn dốc tối thiểu {req.MinGrade:F1}% dễ gây đọng nước rãnh."
                    });
                }

                // Check 1.3: Chiều dài đoạn dốc tối thiểu (L_doc_min)
                // Sử dụng lenDocTCVN = 1/4 L_v1 + L_thẳng + 1/4 L_v2 theo TCVN 5729:2012 (Chú thích Bảng 5)
                var itemLen = new ProfileCheckItem
                {
                    Index = stt++,
                    Station = s1,
                    Elevation = z1,
                    ItemName = $"Chiều dài đoạn dốc L_dốc (Km{(s1 / 1000.0):F3})",
                    ProposedValue = $"{lenDocTCVN:F2} m (L_thẳng={lenTangent:F2}m)",
                    StandardRequirement = $"≥ {req.MinGradeTangentLength:F1} m (TCVN 5729:2012)"
                };

                if (lenDocTCVN < req.MinGradeTangentLength - 0.001)
                {
                    itemLen.Status = CheckStatus.Fail;
                    itemLen.Note = $"VI PHẠM: Chiều dài dốc L_dốc = {lenDocTCVN:F2}m nhỏ hơn chiều dài tối thiểu {req.MinGradeTangentLength:F1}m quy định.";
                }
                else
                {
                    itemLen.Status = CheckStatus.Pass;
                    itemLen.Note = "Đoạn dốc đủ chiều dài êm thuận theo TCVN";
                }
                results.Add(itemLen);
            }

            // 2. Kiểm tra Đổi dốc và Đường cong đứng tại các PVI trung gian
            for (int i = 1; i < count - 1; i++)
            {
                var pviPrev = pvis[i - 1];
                var pviCurr = pvis[i];
                var pviNext = pvis[i + 1];

                double sPrev = pviPrev.RawStation;
                double zPrev = pviPrev.Elevation;
                double sCurr = pviCurr.RawStation;
                double zCurr = pviCurr.Elevation;
                double sNext = pviNext.RawStation;
                double zNext = pviNext.Elevation;

                double len1 = sCurr - sPrev;
                double len2 = sNext - sCurr;
                if (len1 <= 0.001 || len2 <= 0.001) continue;

                double slope1 = (zCurr - zPrev) / len1 * 100.0; // i_in
                double slope2 = (zNext - zCurr) / len2 * 100.0; // i_out
                double deltaI = slope2 - slope1; // Δi
                double absDeltaI = Math.Abs(deltaI);

                // Kiểm tra xem PVI có đường cong đứng không
                bool hasCurve = false;
                double curveLength = 0;
                double radius = 0;
                bool isConvex = deltaI < 0; // i_in > i_out -> lồi, i_in < i_out -> lõm

                // Check 1: Lấy thông tin đường cong từ profile.Entities (nếu có)
                try
                {
                    if (profile != null && profile.Entities != null)
                    {
                        foreach (dynamic ent in profile.Entities)
                        {
                            try
                            {
                                string entTypeName = ent.GetType().Name;
                                bool isTangentEnt = entTypeName.Contains("Tangent");

                                double startSt = Convert.ToDouble(ent.StartStation);
                                double endSt = Convert.ToDouble(ent.EndStation);

                                if (!isTangentEnt && sCurr >= startSt - 1.0 && sCurr <= endSt + 1.0)
                                {
                                    hasCurve = true;
                                    try { curveLength = Convert.ToDouble(ent.Length); } catch { }
                                    try { radius = Convert.ToDouble(ent.Radius); } catch { }
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                try
                {
                    Type pviType = pviCurr.GetType();
                    string typeName = pviType.Name;

                    bool isTangent = true;

                    // 1. Try reading IsTangent property
                    PropertyInfo propIsTangent = pviType.GetProperty("IsTangent");
                    if (propIsTangent != null)
                    {
                        try { isTangent = Convert.ToBoolean(propIsTangent.GetValue(pviCurr, null)); } catch { }
                    }

                    // 2. Try reading CurveType property
                    PropertyInfo propCurveType = pviType.GetProperty("CurveType");
                    if (propCurveType != null)
                    {
                        try
                        {
                            var ctVal = propCurveType.GetValue(pviCurr, null);
                            if (ctVal != null && !ctVal.ToString().Equals("None", StringComparison.OrdinalIgnoreCase))
                            {
                                isTangent = false;
                            }
                        }
                        catch { }
                    }

                    // 3. Try reading Length / CurveLength / Length1+Length2
                    double len = 0;
                    PropertyInfo propLen = pviType.GetProperty("Length") ?? pviType.GetProperty("CurveLength");
                    if (propLen != null)
                    {
                        try { len = Convert.ToDouble(propLen.GetValue(pviCurr, null)); } catch { }
                    }

                    if (len <= 0)
                    {
                        PropertyInfo propL1 = pviType.GetProperty("Length1");
                        PropertyInfo propL2 = pviType.GetProperty("Length2");
                        if (propL1 != null && propL2 != null)
                        {
                            try
                            {
                                double l1 = Convert.ToDouble(propL1.GetValue(pviCurr, null));
                                double l2 = Convert.ToDouble(propL2.GetValue(pviCurr, null));
                                len = l1 + l2;
                            }
                            catch { }
                        }
                    }

                    // 4. Try reading Radius
                    double rad = 0;
                    PropertyInfo propRad = pviType.GetProperty("Radius");
                    if (propRad != null)
                    {
                        try { rad = Convert.ToDouble(propRad.GetValue(pviCurr, null)); } catch { }
                    }

                    // 5. Try reading K factor
                    PropertyInfo propK = pviType.GetProperty("K");
                    double kVal = 0;
                    if (propK != null)
                    {
                        try { kVal = Convert.ToDouble(propK.GetValue(pviCurr, null)); } catch { }
                    }

                    // Decision
                    if (!isTangent || len > 0 || rad > 0 || kVal > 0 || typeName.Contains("Parabola") || typeName.Contains("Circular") || typeName.Contains("Curve"))
                    {
                        hasCurve = true;
                        // Chỉ ghi đè nếu giá trị mới tốt hơn (> 0), tránh xóa kết quả từ Method 1
                        if (len > 0) curveLength = len;
                        if (rad > 0) radius = rad;

                        if (radius <= 0 && absDeltaI > 0.0001)
                        {
                            if (curveLength > 0)
                            {
                                radius = (curveLength * 100.0) / absDeltaI;
                            }
                            else if (kVal > 0)
                            {
                                radius = kVal * 100.0;
                                curveLength = (radius * absDeltaI) / 100.0;
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback
                }

                // Check 2.1: Bắt buộc cắm cong đứng khi Δi ≥ Threshold
                if (absDeltaI >= req.AlgebraicGradeDiffThreshold - 0.001 && !hasCurve)
                {
                    results.Add(new ProfileCheckItem
                    {
                        Index = stt++,
                        Station = sCurr,
                        Elevation = zCurr,
                        ItemName = $"PVI tại Km{(sCurr / 1000.0):F3}",
                        ProposedValue = $"Δi = {absDeltaI:F2}% (Không có đường cong đứng)",
                        StandardRequirement = $"Bắt buộc bố trí ĐCĐ khi |Δi| ≥ {req.AlgebraicGradeDiffThreshold:F1}%",
                        Status = CheckStatus.Fail,
                        Note = $"VI PHẠM: Đổi dốc |Δi| = {absDeltaI:F2}% ≥ {req.AlgebraicGradeDiffThreshold:F1}% nhưng chưa bố trí đường cong đứng!"
                    });
                }

                // Check 2.2: Kiểm tra bán kính & chiều dài đường cong đứng nếu có
                if (hasCurve)
                {
                    string curveTypeStr = isConvex ? "Cong đứng LỒI" : "Cong đứng LÕM";
                    double minRNormal = isConvex ? req.MinConvexRadiusNormal : req.MinConcaveRadiusNormal;
                    double minRLimit = isConvex ? req.MinConvexRadiusLimit : req.MinConcaveRadiusLimit;

                    var itemRadius = new ProfileCheckItem
                    {
                        Index = stt++,
                        Station = sCurr,
                        Elevation = zCurr,
                        ItemName = $"{curveTypeStr} PVI Km{(sCurr / 1000.0):F3} (Bán kính R)",
                        ProposedValue = $"R = {radius:F0} m (L={curveLength:F2}m, Δi={absDeltaI:F2}%)",
                        StandardRequirement = $"R_chuẩn ≥ {minRNormal:F0}m (R_GH ≥ {minRLimit:F0}m)"
                    };

                    if (radius < minRLimit - 0.1)
                    {
                        itemRadius.Status = CheckStatus.Fail;
                        itemRadius.Note = $"VI PHẠM: Bán kính {curveTypeStr.ToLower()} R = {radius:F0}m nhỏ hơn bán kính giới hạn R_GH = {minRLimit:F0}m!";
                    }
                    else if (radius < minRNormal - 0.1)
                    {
                        itemRadius.Status = CheckStatus.Warning;
                        itemRadius.Note = $"CẢNH BÁO: Bán kính R = {radius:F0}m đạt mức giới hạn ({minRLimit:F0}m), khuyến cáo nâng lên R_chuẩn ({minRNormal:F0}m).";
                    }
                    else
                    {
                        itemRadius.Status = CheckStatus.Pass;
                        itemRadius.Note = "Bán kính đường cong đứng đạt tiêu chuẩn";
                    }
                    results.Add(itemRadius);

                    // Check chiều dài đường cong đứng (L_v)
                    var itemCurveLen = new ProfileCheckItem
                    {
                        Index = stt++,
                        Station = sCurr,
                        Elevation = zCurr,
                        ItemName = $"{curveTypeStr} PVI Km{(sCurr / 1000.0):F3} (Chiều dài L)",
                        ProposedValue = $"L = {curveLength:F2} m",
                        StandardRequirement = $"≥ {req.MinVerticalCurveLength:F1} m"
                    };

                    if (curveLength < req.MinVerticalCurveLength - 0.001)
                    {
                        itemCurveLen.Status = CheckStatus.Fail;
                        itemCurveLen.Note = $"VI PHẠM: Chiều dài đường cong đứng L = {curveLength:F2}m nhỏ hơn chiều dài tối thiểu {req.MinVerticalCurveLength:F1}m quy định.";
                    }
                    else
                    {
                        itemCurveLen.Status = CheckStatus.Pass;
                        itemCurveLen.Note = "Chiều dài đường cong đứng thỏa mãn";
                    }
                    results.Add(itemCurveLen);
                }
            }

            return results;
        }
    }
}
