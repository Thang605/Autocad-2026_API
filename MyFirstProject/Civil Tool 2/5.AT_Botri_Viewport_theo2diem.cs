// (C) Copyright 2024 by T27
// Lệnh bố trí viewport lên layout với 2 điểm chọn trong Model space
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;

using MyFirstProject.Extensions;

// Alias để tránh xung đột namespace
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.AT_Botri_Viewport_theo2diem))]

namespace Civil3DCsharp
{
    /// <summary>
    /// Class chứa lệnh bố trí viewport theo 2 điểm chọn trong Model space
    /// </summary>
    public class AT_Botri_Viewport_theo2diem
    {
        /// <summary>
        /// Lệnh chính: Bố trí viewport lên layout dựa trên 2 điểm chọn trong Model space
        /// User chọn 2 điểm định nghĩa vùng hiển thị (bounding box) trong Model space
        /// Sau đó chọn điểm đặt viewport trong Layout
        /// </summary>
        [CommandMethod("AT_BoTri_ViewPort_Theo2Diem")]
        public static void BoTri_ViewPort_Theo2Diem()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== BỐ TRÍ VIEWPORT THEO 2 ĐIỂM ===");

                // Kiểm tra xem đang ở Model space không
                if (db.TileMode == false)
                {
                    ed.WriteMessage("\n⚠️ Bạn đang ở Paper space. Vui lòng chuyển sang Model space để chọn vùng hiển thị.");
                    ed.WriteMessage("\n   Gõ lệnh MODEL hoặc nhấn phím Tab để chuyển.");
                    return;
                }

                // Bước 1: Chọn điểm thứ nhất trong Model space
                PromptPointOptions ppo1 = new("\n Chọn điểm góc thứ nhất trong Model:");
                ppo1.AllowNone = false;
                PromptPointResult ppr1 = ed.GetPoint(ppo1);
                
                if (ppr1.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n❌ Không có điểm nào được chọn. Lệnh đã hủy.");
                    return;
                }
                Point3d point1 = ppr1.Value;

                // Bước 2: Chọn điểm thứ hai trong Model space
                PromptPointOptions ppo2 = new("\n Chọn điểm góc thứ hai trong Model:");
                ppo2.AllowNone = false;
                ppo2.BasePoint = point1;
                ppo2.UseBasePoint = true;
                PromptPointResult ppr2 = ed.GetPoint(ppo2);
                
                if (ppr2.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n❌ Không có điểm nào được chọn. Lệnh đã hủy.");
                    return;
                }
                Point3d point2 = ppr2.Value;

                // Tính toán bounding box từ 2 điểm
                double minX = Math.Min(point1.X, point2.X);
                double maxX = Math.Max(point1.X, point2.X);
                double minY = Math.Min(point1.Y, point2.Y);
                double maxY = Math.Max(point1.Y, point2.Y);
                
                double modelWidth = maxX - minX;
                double modelHeight = maxY - minY;
                
                // Tâm của vùng hiển thị trong Model space
                Point3d modelCenter = new Point3d(
                    (minX + maxX) / 2,
                    (minY + maxY) / 2,
                    0);

                ed.WriteMessage($"\n📐 Vùng hiển thị: {modelWidth:F2} x {modelHeight:F2}");
                ed.WriteMessage($"\n   Tâm: ({modelCenter.X:F2}, {modelCenter.Y:F2})");

                if (modelWidth <= 0 || modelHeight <= 0)
                {
                    ed.WriteMessage("\n❌ Vùng chọn không hợp lệ. Hai điểm phải khác nhau.");
                    return;
                }

                // Bước 3: Lấy danh sách tỉ lệ từ bản vẽ và hiển thị form
                List<ScaleInfo> drawingScales = GetDrawingScales(db);
                
                if (drawingScales.Count == 0)
                {
                    ed.WriteMessage("\n⚠️ Không tìm thấy tỉ lệ nào trong bản vẽ.");
                    return;
                }

                // Hiển thị form chọn tỉ lệ
                ScaleInfo? selectedScale;
                using (var form = new ViewportScale2PointForm(drawingScales))
                {
                    if (form.ShowDialog() != DialogResult.OK)
                    {
                        ed.WriteMessage("\n❌ Đã hủy lệnh.");
                        return;
                    }
                    selectedScale = form.SelectedScale;
                }

                if (selectedScale == null)
                {
                    ed.WriteMessage("\n❌ Không chọn được tỉ lệ. Lệnh đã hủy.");
                    return;
                }

                double customScale = selectedScale.ScaleValue;
                ed.WriteMessage($"\n✅ Tỉ lệ đã chọn: {selectedScale.Name}");

                // Bước 4: Chuyển sang Layout
                ed.WriteMessage("\n\n📋 Chuyển sang Layout để đặt viewport...");
                
                LayoutManager layoutMgr = LayoutManager.Current;
                if (layoutMgr.CurrentLayout == "Model")
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                        foreach (DBDictionaryEntry entry in layoutDict)
                        {
                            if (entry.Key != "Model")
                            {
                                layoutMgr.CurrentLayout = entry.Key;
                                ed.WriteMessage($"\n📋 Đã chuyển sang Layout: {entry.Key}");
                                break;
                            }
                        }
                        tr.Commit();
                    }
                }

                doc.SendStringToExecute("REGEN ", false, false, false);
                System.Threading.Thread.Sleep(200);

                // Bước 5: Cho user chọn điểm đặt viewport trong Layout (tâm viewport)
                PromptPointOptions ppo = new("\n Chọn điểm đặt viewport trong Layout (tâm viewport):");
                ppo.AllowNone = false;
                PromptPointResult ppr = ed.GetPoint(ppo);
                
                if (ppr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n❌ Không có điểm nào được chọn. Lệnh đã hủy.");
                    return;
                }
                Point3d insertPoint = ppr.Value;

                // Bước 6: Tạo viewport
                CreateViewportFrom2Points(db, ed, modelCenter, modelWidth, modelHeight, insertPoint, customScale);

                ed.WriteMessage("\n\n✅ Đã tạo viewport thành công!");
                ed.WriteMessage("\n   💡 Mẹo: Bạn có thể dùng lệnh VPCLIP để điều chỉnh boundary của viewport.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi: {ex.Message}");
                ed.WriteMessage($"\n   Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả tỉ lệ có sẵn trong bản vẽ
        /// </summary>
        private static List<ScaleInfo> GetDrawingScales(Database db)
        {
            List<ScaleInfo> scales = new();
            
            try
            {
                ObjectContextManager ocm = db.ObjectContextManager;
                if (ocm != null)
                {
                    ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                    if (occ != null)
                    {
                        foreach (ObjectContext oc in occ)
                        {
                            if (oc is AnnotationScale annoScale)
                            {
                                // Bỏ qua tỉ lệ từ file Xref
                                if (annoScale.Name.Contains("_XREF", StringComparison.OrdinalIgnoreCase))
                                    continue;
                                
                                scales.Add(new ScaleInfo
                                {
                                    Name = annoScale.Name,
                                    PaperUnits = annoScale.PaperUnits,
                                    DrawingUnits = annoScale.DrawingUnits
                                });
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n⚠️ Lỗi khi lấy danh sách tỉ lệ: {ex.Message}");
            }

            // Sắp xếp theo DrawingUnits
            scales = scales.OrderBy(s => s.DrawingUnits).ToList();
            
            return scales;
        }

        /// <summary>
        /// Tạo viewport từ 2 điểm đã chọn trong Model space
        /// </summary>
        private static void CreateViewportFrom2Points(Database db, Editor ed,
            Point3d modelCenter, double modelWidth, double modelHeight, 
            Point3d paperInsertPoint, double customScale)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayoutManager layoutMgr = LayoutManager.Current;
                    Layout layout = (Layout)tr.GetObject(layoutMgr.GetLayoutId(layoutMgr.CurrentLayout), OpenMode.ForRead);
                    BlockTableRecord paperSpace = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite);

                    // Tính toán kích thước viewport trên Paper space
                    double paperWidth = modelWidth * customScale;
                    double paperHeight = modelHeight * customScale;

                    // Tạo viewport mới
                    Viewport viewport = new()
                    {
                        CenterPoint = paperInsertPoint,
                        Width = paperWidth,
                        Height = paperHeight,
                        CustomScale = customScale,
                        ViewCenter = new Point2d(modelCenter.X, modelCenter.Y)
                    };

                    ObjectId viewportId = paperSpace.AppendEntity(viewport);
                    tr.AddNewlyCreatedDBObject(viewport, true);
                    viewport.On = true;
                    viewport.Locked = true;

                    ed.WriteMessage($"\n📐 Viewport đã được tạo:");
                    ed.WriteMessage($"\n   - View Center: ({modelCenter.X:F2}, {modelCenter.Y:F2})");
                    ed.WriteMessage($"\n   - Kích thước Paper: {paperWidth:F2} x {paperHeight:F2}");
                    ed.WriteMessage($"\n   - Tỉ lệ: 1:{1.0 / customScale:F0}");

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n❌ Lỗi khi tạo viewport: {ex.Message}");
                    tr.Abort();
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Form chọn tỉ lệ cho lệnh AT_BoTri_ViewPort_Theo2Diem
    /// </summary>
    public class ViewportScale2PointForm : Form
    {
        // Properties để trả về kết quả
        public ScaleInfo? SelectedScale { get; private set; }

        // Controls
        private ComboBox cmbScale = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;

        private List<ScaleInfo> _scales;

        public ViewportScale2PointForm(List<ScaleInfo> scales)
        {
            _scales = scales;
            InitializeComponent();
            LoadScales();
        }

        private void InitializeComponent()
        {
            this.Text = "Bố Trí Viewport Theo 2 Điểm";
            this.Size = new Size(380, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            int y = 20;
            int labelWidth = 100;
            int controlX = 120;
            int controlWidth = 220;

            // Label Tỉ lệ
            var lblScale = new Label
            {
                Text = "Tỉ lệ viewport:",
                Location = new Point(20, y + 3),
                Size = new Size(labelWidth, 23),
                AutoSize = false
            };

            // ComboBox Tỉ lệ
            cmbScale = new ComboBox
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            y += 50;

            // Buttons
            btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(140, y),
                Size = new Size(90, 30)
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Location = new Point(250, y),
                Size = new Size(90, 30)
            };

            // Add controls
            this.Controls.AddRange(new Control[]
            {
                lblScale, cmbScale,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void LoadScales()
        {
            cmbScale.Items.Clear();
            foreach (var scale in _scales)
            {
                cmbScale.Items.Add(scale);
            }

            // Chọn mặc định 1:100 hoặc tương đương
            int defaultIndex = _scales.FindIndex(s => s.Name == "1:100");
            if (defaultIndex < 0) defaultIndex = _scales.FindIndex(s => s.DrawingUnits == 100);
            if (defaultIndex < 0) defaultIndex = 0;

            if (cmbScale.Items.Count > 0)
            {
                cmbScale.SelectedIndex = defaultIndex;
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            SelectedScale = cmbScale.SelectedItem as ScaleInfo;
        }
    }
}
