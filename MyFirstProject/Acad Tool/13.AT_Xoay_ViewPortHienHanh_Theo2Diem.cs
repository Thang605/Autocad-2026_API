// (C) Copyright 2024 by T27
// Lệnh xoay viewport hiện hành theo 2 điểm chọn
//
using System;
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
using DrawingFont = System.Drawing.Font;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.AT_Xoay_ViewPortHienHanh_Theo2Diem))]

namespace Civil3DCsharp
{
    /// <summary>
    /// Enum cho hướng xoay viewport
    /// </summary>
    public enum ViewportRotationDirection
    {
        Horizontal,  // Xoay để 2 điểm nằm ngang
        Vertical     // Xoay để 2 điểm nằm dọc
    }

    /// <summary>
    /// Class chứa lệnh xoay viewport hiện hành theo 2 điểm
    /// </summary>
    public class AT_Xoay_ViewPortHienHanh_Theo2Diem
    {
        /// <summary>
        /// Lệnh chính: Xoay viewport hiện hành theo 2 điểm chọn
        /// </summary>
        [CommandMethod("AT_Xoay_ViewPortHienHanh_Theo2Diem")]
        public static void Xoay_ViewPortHienHanh_Theo2Diem()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== XOAY VIEWPORT HIỆN HÀNH THEO 2 ĐIỂM ===");

                // Kiểm tra xem đang ở Paper space không
                if (db.TileMode == true)
                {
                    ed.WriteMessage("\n⚠️ Bạn đang ở Model space. Vui lòng chuyển sang Layout (Paper space) để sử dụng lệnh này.");
                    ed.WriteMessage("\n   Gõ lệnh tên Layout hoặc nhấn phím Tab để chuyển.");
                    return;
                }

                // Lấy viewport hiện hành
                ObjectId currentViewportId = GetCurrentLayoutViewportId(db);
                if (currentViewportId == ObjectId.Null)
                {
                    ed.WriteMessage("\n❌ Không tìm thấy viewport hiện hành. Vui lòng double-click vào viewport cần xoay.");
                    return;
                }

                // Hiển thị form chọn hướng xoay
                ViewportRotationDirection rotationDirection;
                using (var form = new ViewportRotationForm())
                {
                    if (form.ShowDialog() != DialogResult.OK)
                    {
                        ed.WriteMessage("\n❌ Đã hủy lệnh.");
                        return;
                    }
                    rotationDirection = form.RotationDirection;
                }

                ed.WriteMessage($"\n📏 Hướng xoay: {(rotationDirection == ViewportRotationDirection.Horizontal ? "Ngang (→)" : "Dọc (↑)")}");

                // Bước 1: Cho user chọn điểm thứ nhất
                PromptPointOptions ppo1 = new("\n Chọn điểm thứ nhất:");
                ppo1.AllowNone = false;
                PromptPointResult ppr1 = ed.GetPoint(ppo1);

                if (ppr1.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n❌ Không có điểm nào được chọn. Lệnh đã hủy.");
                    return;
                }
                Point3d point1 = ppr1.Value;

                // Bước 2: Cho user chọn điểm thứ hai
                PromptPointOptions ppo2 = new("\n Chọn điểm thứ hai:");
                ppo2.BasePoint = point1;
                ppo2.UseBasePoint = true;
                ppo2.AllowNone = false;
                PromptPointResult ppr2 = ed.GetPoint(ppo2);

                if (ppr2.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n❌ Không có điểm nào được chọn. Lệnh đã hủy.");
                    return;
                }
                Point3d point2 = ppr2.Value;

                // Tính góc giữa 2 điểm
                double dx = point2.X - point1.X;
                double dy = point2.Y - point1.Y;
                double angle = Math.Atan2(dy, dx); // Góc từ điểm 1 đến điểm 2 (radians)

                // Tính góc xoay viewport dựa trên hướng được chọn
                double twistAngle;
                if (rotationDirection == ViewportRotationDirection.Horizontal)
                {
                    // Xoay để 2 điểm nằm ngang (song song trục X)
                    // Góc twist = -góc của đường thẳng
                    twistAngle = -angle;
                }
                else
                {
                    // Xoay để 2 điểm nằm dọc (song song trục Y)
                    // Góc twist = -(góc của đường thẳng - 90°)
                    twistAngle = -(angle - Math.PI / 2);
                }

                // Normalize góc về khoảng -PI đến PI
                twistAngle = NormalizeAngle(twistAngle);

                // Áp dụng góc xoay cho viewport
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        Viewport viewport = (Viewport)tr.GetObject(currentViewportId, OpenMode.ForWrite);
                        
                        // Cập nhật góc twist của viewport
                        viewport.TwistAngle = twistAngle;

                        tr.Commit();

                        // Hiển thị kết quả
                        double angleDegrees = twistAngle * 180.0 / Math.PI;
                        ed.WriteMessage($"\n✅ Đã xoay viewport!");
                        ed.WriteMessage($"\n   📐 Góc đường thẳng: {(angle * 180.0 / Math.PI):F2}°");
                        ed.WriteMessage($"\n   🔄 Góc xoay viewport: {angleDegrees:F2}°");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n❌ Lỗi khi xoay viewport: {ex.Message}");
                        tr.Abort();
                    }
                }

                // Regenerate để cập nhật hiển thị
                doc.SendStringToExecute("REGEN ", false, false, false);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi: {ex.Message}");
                ed.WriteMessage($"\n   Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Lệnh phụ: Xoay viewport với góc nhập trực tiếp
        /// </summary>
        [CommandMethod("AT_Xoay_ViewPortHienHanh_TheoGoc")]
        public static void Xoay_ViewPortHienHanh_TheoGoc()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== XOAY VIEWPORT HIỆN HÀNH THEO GÓC ===");

                // Kiểm tra xem đang ở Paper space không
                if (db.TileMode == true)
                {
                    ed.WriteMessage("\n⚠️ Bạn đang ở Model space. Vui lòng chuyển sang Layout (Paper space).");
                    return;
                }

                // Lấy viewport hiện hành
                ObjectId currentViewportId = GetCurrentLayoutViewportId(db);
                if (currentViewportId == ObjectId.Null)
                {
                    ed.WriteMessage("\n❌ Không tìm thấy viewport hiện hành.");
                    return;
                }

                // Lấy góc xoay hiện tại
                double currentAngle = 0;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Viewport viewport = (Viewport)tr.GetObject(currentViewportId, OpenMode.ForRead);
                    currentAngle = viewport.TwistAngle * 180.0 / Math.PI;
                    tr.Commit();
                }

                // Nhập góc xoay
                PromptDoubleOptions pdo = new($"\n Nhập góc xoay (độ) [Góc hiện tại: {currentAngle:F2}°]:");
                pdo.DefaultValue = 0;
                pdo.AllowNegative = true;
                pdo.AllowZero = true;
                PromptDoubleResult pdr = ed.GetDouble(pdo);

                if (pdr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n❌ Đã hủy lệnh.");
                    return;
                }

                double angleDegrees = pdr.Value;
                double angleRadians = angleDegrees * Math.PI / 180.0;

                // Áp dụng góc xoay
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Viewport viewport = (Viewport)tr.GetObject(currentViewportId, OpenMode.ForWrite);
                    viewport.TwistAngle = angleRadians;
                    tr.Commit();

                    ed.WriteMessage($"\n✅ Đã xoay viewport {angleDegrees:F2}°");
                }

                doc.SendStringToExecute("REGEN ", false, false, false);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Lệnh phụ: Đặt lại góc xoay viewport về 0
        /// </summary>
        [CommandMethod("AT_Xoay_ViewPortHienHanh_Reset")]
        public static void Xoay_ViewPortHienHanh_Reset()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== ĐẶT LẠI GÓC XOAY VIEWPORT ===");

                if (db.TileMode == true)
                {
                    ed.WriteMessage("\n⚠️ Bạn đang ở Model space. Vui lòng chuyển sang Layout.");
                    return;
                }

                ObjectId currentViewportId = GetCurrentLayoutViewportId(db);
                if (currentViewportId == ObjectId.Null)
                {
                    ed.WriteMessage("\n❌ Không tìm thấy viewport hiện hành.");
                    return;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Viewport viewport = (Viewport)tr.GetObject(currentViewportId, OpenMode.ForWrite);
                    double oldAngle = viewport.TwistAngle * 180.0 / Math.PI;
                    viewport.TwistAngle = 0;
                    tr.Commit();

                    ed.WriteMessage($"\n✅ Đã đặt lại góc xoay từ {oldAngle:F2}° về 0°");
                }

                doc.SendStringToExecute("REGEN ", false, false, false);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy ObjectId của viewport hiện hành trong Layout
        /// </summary>
        private static ObjectId GetCurrentLayoutViewportId(Database db)
        {
            ObjectId viewportId = ObjectId.Null;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Lấy current viewport number từ system variable
                    short cvport = (short)Application.GetSystemVariable("CVPORT");

                    if (cvport > 1) // Viewport ID > 1 nghĩa là đang trong một viewport
                    {
                        // Lấy layout hiện tại
                        LayoutManager layoutMgr = LayoutManager.Current;
                        Layout layout = (Layout)tr.GetObject(
                            layoutMgr.GetLayoutId(layoutMgr.CurrentLayout), OpenMode.ForRead);
                        
                        // Duyệt qua các entity trong layout để tìm viewport
                        BlockTableRecord layoutBtr = (BlockTableRecord)tr.GetObject(
                            layout.BlockTableRecordId, OpenMode.ForRead);

                        foreach (ObjectId entId in layoutBtr)
                        {
                            Entity ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                            if (ent is Viewport vp && vp.Number == cvport)
                            {
                                viewportId = entId;
                                break;
                            }
                        }
                    }

                    tr.Commit();
                }
            }
            catch
            {
                // Ignore errors
            }

            return viewportId;
        }

        /// <summary>
        /// Chuẩn hóa góc về khoảng -PI đến PI
        /// </summary>
        private static double NormalizeAngle(double angle)
        {
            while (angle > Math.PI)
                angle -= 2 * Math.PI;
            while (angle < -Math.PI)
                angle += 2 * Math.PI;
            return angle;
        }
    }

    /// <summary>
    /// Form chọn hướng xoay viewport
    /// </summary>
    public class ViewportRotationForm : Form
    {
        // Properties để trả về kết quả
        public ViewportRotationDirection RotationDirection { get; private set; } = ViewportRotationDirection.Horizontal;

        // Controls
        private RadioButton rbHorizontal = null!;
        private RadioButton rbVertical = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Label lblInfo = null!;
        private PictureBox pictureBox = null!;

        public ViewportRotationForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Xoay Viewport Theo 2 Điểm";
            this.Size = new Size(380, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            int y = 15;

            // Tiêu đề
            var lblTitle = new Label
            {
                Text = "🔄 Chọn hướng xoay viewport",
                Location = new Point(20, y),
                Size = new Size(340, 25),
                Font = new DrawingFont("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 51, 51)
            };

            y += 35;

            // Thông tin
            lblInfo = new Label
            {
                Text = "Chọn hướng để xoay viewport sao cho đường thẳng\ngiữa 2 điểm chọn sẽ nằm theo hướng được chọn.",
                Location = new Point(20, y),
                Size = new Size(340, 40),
                ForeColor = Color.FromArgb(102, 102, 102)
            };

            y += 50;

            // Panel chứa radio buttons
            var panel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(340, 80),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // Radio button Ngang
            rbHorizontal = new RadioButton
            {
                Text = "📏 Ngang (Horizontal)",
                Location = new Point(20, 15),
                Size = new Size(300, 25),
                Checked = true,
                Font = new DrawingFont("Segoe UI", 10, FontStyle.Regular)
            };
            rbHorizontal.CheckedChanged += RadioButton_CheckedChanged;

            // Radio button Dọc
            rbVertical = new RadioButton
            {
                Text = "📐 Dọc (Vertical)",
                Location = new Point(20, 45),
                Size = new Size(300, 25),
                Font = new DrawingFont("Segoe UI", 10, FontStyle.Regular)
            };
            rbVertical.CheckedChanged += RadioButton_CheckedChanged;

            panel.Controls.Add(rbHorizontal);
            panel.Controls.Add(rbVertical);

            y += 95;

            // Buttons
            btnOK = new Button
            {
                Text = "✓ Xác nhận",
                Location = new Point(80, y),
                Size = new Size(100, 35),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "✕ Hủy",
                Location = new Point(200, y),
                Size = new Size(100, 35),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            // Add controls
            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                lblInfo,
                panel,
                btnOK,
                btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void RadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            // Có thể thêm preview hoặc mô tả ở đây
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            RotationDirection = rbHorizontal.Checked 
                ? ViewportRotationDirection.Horizontal 
                : ViewportRotationDirection.Vertical;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
