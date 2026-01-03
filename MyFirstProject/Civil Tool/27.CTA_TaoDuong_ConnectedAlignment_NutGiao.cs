using System;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.CTA_TaoDuong_ConnectedAlignment_NutGiao_Commands))]

namespace Civil3DCsharp
{
    /// <summary>
    /// Lệnh tạo Connected Alignment tại nút giao
    /// Hỗ trợ tạo nhiều connected alignment cho ngã 3 (2 đường) hoặc ngã 4 (4 đường)
    /// </summary>
    public class CTA_TaoDuong_ConnectedAlignment_NutGiao_Commands
    {
        private static int _lastIntersectionType = 4; // 3 = ngã 3, 4 = ngã 4

        /// <summary>
        /// Lệnh chính - Tạo nhiều Connected Alignment cho nút giao
        /// </summary>
        [CommandMethod("CTA_TaoDuong_ConnectedAlignment_NutGiao")]
        public static void CTA_TaoDuong_ConnectedAlignment_NutGiao()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n");
                ed.WriteMessage("\n╔══════════════════════════════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║           TẠO CONNECTED ALIGNMENT TẠI NÚT GIAO                               ║");
                ed.WriteMessage("\n╚══════════════════════════════════════════════════════════════════════════════╝");

                // Hỏi loại nút giao
                PromptKeywordOptions pkoType = new PromptKeywordOptions("\nChọn loại nút giao [Nga3/Nga4]: ", "Nga3 Nga4");
                pkoType.Keywords.Default = _lastIntersectionType == 3 ? "Nga3" : "Nga4";
                pkoType.AllowNone = true;

                PromptResult prType = ed.GetKeywords(pkoType);
                if (prType.Status != PromptStatus.OK && prType.Status != PromptStatus.None)
                {
                    ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                int numConnections;
                if (prType.Status == PromptStatus.None || prType.StringResult == "Nga4")
                {
                    numConnections = 4;
                    _lastIntersectionType = 4;
                    ed.WriteMessage("\n→ Ngã 4: Sẽ tạo 4 connected alignment (4 góc rẽ)");
                }
                else
                {
                    numConnections = 2;
                    _lastIntersectionType = 3;
                    ed.WriteMessage("\n→ Ngã 3: Sẽ tạo 2 connected alignment (2 góc rẽ)");
                }

                // Tạo từng connected alignment
                for (int i = 1; i <= numConnections; i++)
                {
                    ed.WriteMessage("\n");
                    ed.WriteMessage($"\n┌──────────────────────────────────────────────────────────────────────────────┐");
                    ed.WriteMessage($"\n│  TẠO CONNECTED ALIGNMENT {i}/{numConnections}                                              │");
                    ed.WriteMessage($"\n├──────────────────────────────────────────────────────────────────────────────┤");
                    ed.WriteMessage($"\n│  Bước 1: Chọn Alignment thứ nhất (From Alignment)                            │");
                    ed.WriteMessage($"\n│  Bước 2: Pick điểm bắt đầu trên Alignment 1                                  │");
                    ed.WriteMessage($"\n│  Bước 3: Chọn Alignment thứ hai (To Alignment)                               │");
                    ed.WriteMessage($"\n│  Bước 4: Pick điểm kết thúc trên Alignment 2                                 │");
                    ed.WriteMessage($"\n│  Bước 5: Nhập thông số (Radius, Spiral, v.v.)                                │");
                    ed.WriteMessage($"\n└──────────────────────────────────────────────────────────────────────────────┘");
                    ed.WriteMessage("\n");

                    // Gọi lệnh gốc của Civil 3D
                    doc.SendStringToExecute("_AeccCreateAlignmentConnected ", true, false, true);

                    // Nếu còn đường tiếp theo, hỏi người dùng có muốn tiếp tục không
                    if (i < numConnections)
                    {
                        // Sử dụng SendStringToExecute với synchronous để đợi lệnh hoàn thành
                        // Sau đó hỏi người dùng
                        string script = $"(progn (alert \"Đã tạo connected alignment {i}/{numConnections}.\\nNhấn OK để tiếp tục tạo đường tiếp theo.\")) ";
                        doc.SendStringToExecute(script, true, false, true);
                    }
                }

                // Thông báo hoàn thành
                string finalScript = $"(alert \"Hoàn thành! Đã tạo {numConnections} connected alignment cho nút giao.\") ";
                doc.SendStringToExecute(finalScript, true, false, true);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Lệnh tắt cho ngã 4 - Tạo 4 connected alignment
        /// </summary>
        [CommandMethod("CTA_ConnectedAlignment_Nga4")]
        public static void CTA_ConnectedAlignment_Nga4()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n");
            ed.WriteMessage("\n╔══════════════════════════════════════════════════════════════════════════════╗");
            ed.WriteMessage("\n║     TẠO 4 CONNECTED ALIGNMENT CHO NGÃ 4                                      ║");
            ed.WriteMessage("\n╚══════════════════════════════════════════════════════════════════════════════╝");
            ed.WriteMessage("\n→ Sẽ gọi lệnh _AeccCreateAlignmentConnected 4 lần liên tiếp");

            for (int i = 1; i <= 4; i++)
            {
                ed.WriteMessage($"\n\n═══ TẠO CONNECTED ALIGNMENT {i}/4 ═══");
                doc.SendStringToExecute("_AeccCreateAlignmentConnected ", true, false, true);

                if (i < 4)
                {
                    string script = $"(alert \"Đã tạo connected alignment {i}/4.\\nNhấn OK để tiếp tục tạo đường tiếp theo.\") ";
                    doc.SendStringToExecute(script, true, false, true);
                }
            }

            doc.SendStringToExecute("(alert \"Hoàn thành! Đã tạo 4 connected alignment cho ngã 4.\") ", true, false, true);
        }

        /// <summary>
        /// Lệnh tắt cho ngã 3 - Tạo 2 connected alignment
        /// </summary>
        [CommandMethod("CTA_ConnectedAlignment_Nga3")]
        public static void CTA_ConnectedAlignment_Nga3()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n");
            ed.WriteMessage("\n╔══════════════════════════════════════════════════════════════════════════════╗");
            ed.WriteMessage("\n║     TẠO 2 CONNECTED ALIGNMENT CHO NGÃ 3                                      ║");
            ed.WriteMessage("\n╚══════════════════════════════════════════════════════════════════════════════╝");
            ed.WriteMessage("\n→ Sẽ gọi lệnh _AeccCreateAlignmentConnected 2 lần liên tiếp");

            for (int i = 1; i <= 2; i++)
            {
                ed.WriteMessage($"\n\n═══ TẠO CONNECTED ALIGNMENT {i}/2 ═══");
                doc.SendStringToExecute("_AeccCreateAlignmentConnected ", true, false, true);

                if (i < 2)
                {
                    string script = $"(alert \"Đã tạo connected alignment {i}/2.\\nNhấn OK để tiếp tục tạo đường tiếp theo.\") ";
                    doc.SendStringToExecute(script, true, false, true);
                }
            }

            doc.SendStringToExecute("(alert \"Hoàn thành! Đã tạo 2 connected alignment cho ngã 3.\") ", true, false, true);
        }

        /// <summary>
        /// Lệnh gọi trực tiếp 1 lần _AeccCreateAlignmentConnected
        /// </summary>
        [CommandMethod("CTA_ConnectedAlignment")]
        public static void CTA_ConnectedAlignment()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("_AeccCreateAlignmentConnected ", true, false, false);
        }

        /// <summary>
        /// Hiển thị hướng dẫn sử dụng
        /// </summary>
        [CommandMethod("CTA_ConnectedAlignment_Help")]
        public static void ShowHelp()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n");
            ed.WriteMessage("\n╔══════════════════════════════════════════════════════════════════════════════╗");
            ed.WriteMessage("\n║           HƯỚNG DẪN TẠO CONNECTED ALIGNMENT TẠI NÚT GIAO                     ║");
            ed.WriteMessage("\n╠══════════════════════════════════════════════════════════════════════════════╣");
            ed.WriteMessage("\n║  CÁC LỆNH AVAILABLE:                                                         ║");
            ed.WriteMessage("\n║  • CTA_TaoDuong_ConnectedAlignment_NutGiao - Chọn ngã 3 hoặc ngã 4          ║");
            ed.WriteMessage("\n║  • CTA_ConnectedAlignment_Nga4             - Tạo 4 đường cho ngã 4          ║");
            ed.WriteMessage("\n║  • CTA_ConnectedAlignment_Nga3             - Tạo 2 đường cho ngã 3          ║");
            ed.WriteMessage("\n║  • CTA_ConnectedAlignment                  - Tạo 1 đường (gọi lệnh gốc)     ║");
            ed.WriteMessage("\n╠══════════════════════════════════════════════════════════════════════════════╣");
            ed.WriteMessage("\n║  CÁC BƯỚC CHO MỖI CONNECTED ALIGNMENT:                                       ║");
            ed.WriteMessage("\n║  1. Chọn Alignment thứ nhất (From Alignment)                                 ║");
            ed.WriteMessage("\n║  2. Pick điểm trên Alignment 1 để xác định vị trí bắt đầu                   ║");
            ed.WriteMessage("\n║  3. Chọn Alignment thứ hai (To Alignment)                                    ║");
            ed.WriteMessage("\n║  4. Pick điểm trên Alignment 2 để xác định vị trí kết thúc                  ║");
            ed.WriteMessage("\n║  5. Nhập các thông số: Radius, Spiral, v.v.                                  ║");
            ed.WriteMessage("\n║  6. Civil 3D sẽ tự động tạo alignment nối giữa 2 alignment                  ║");
            ed.WriteMessage("\n╠══════════════════════════════════════════════════════════════════════════════╣");
            ed.WriteMessage("\n║  VÍ DỤ NGÃ 4:                                                                ║");
            ed.WriteMessage("\n║  ┌─────X─────┐                                                               ║");
            ed.WriteMessage("\n║  │     │     │   X = điểm giao                                               ║");
            ed.WriteMessage("\n║  │  1  │  2  │   1,2,3,4 = 4 connected alignment cần tạo                     ║");
            ed.WriteMessage("\n║  ├─────┼─────┤                                                               ║");
            ed.WriteMessage("\n║  │  3  │  4  │                                                               ║");
            ed.WriteMessage("\n║  └─────┴─────┘                                                               ║");
            ed.WriteMessage("\n╚══════════════════════════════════════════════════════════════════════════════╝");
            ed.WriteMessage("\n");
        }
    }
}
