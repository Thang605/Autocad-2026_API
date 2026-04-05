// (C) Copyright 2024 by T27
// Lệnh in hàng loạt Model space - Tham khảo từ LISP INMODEL_TNC3D
// Cập nhật: Quét chọn block cùng tên, nhóm theo Y, sắp xếp trái→phải, trên→dưới
//
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;

// Alias để tránh xung đột namespace
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsTextBox = System.Windows.Forms.TextBox;
using WinFormsButton = System.Windows.Forms.Button;
using WinFormsComboBox = System.Windows.Forms.ComboBox;
using DrawingFont = System.Drawing.Font;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.AT_InModel_HangLoat_Commands))]

namespace Civil3DCsharp
{
    /// <summary>
    /// Class lưu thông tin điểm in (1 nhóm block cùng Y)
    /// </summary>
    public class PrintPointInfo
    {
        public int Index { get; set; }
        public Point3d StartPoint { get; set; }
        public int Quantity { get; set; }

        public PrintPointInfo(int index, Point3d startPoint, int quantity)
        {
            Index = index;
            StartPoint = startPoint;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// Thông tin một nhóm block cùng Y (một hàng in)
    /// </summary>
    public class PrintRowInfo
    {
        public double YValue { get; set; }
        public List<Point3d> BlockPositions { get; set; } = new();
        public int RowIndex { get; set; }

        /// <summary>
        /// Số lượng block trong hàng = số bản in
        /// </summary>
        public int Count => BlockPositions.Count;
    }

    /// <summary>
    /// Form cấu hình in hàng loạt
    /// </summary>
    public class BatchPlotSettingsForm : Form
    {
        // Properties trả về kết quả
        public List<PrintRowInfo> PrintRows { get; private set; } = new();
        public string SelectedPrinter { get; private set; } = "PDF reDirect v2";
        public string SelectedPaperSize { get; private set; } = "A3";
        public double FrameWidth { get; private set; } = 84.0;
        public double FrameHeight { get; private set; } = 59.4;
        public string SelectedScale { get; private set; } = "5:1";
        public string SelectedCtb { get; private set; } = "monochrome.ctb";
        public bool IsLandscape { get; private set; } = true;
        public string BlockName { get; set; } = "";
        public List<ObjectId> SelectedBlockIds { get; set; } = new();

        // Controls
        private DataGridView dgvPrintPoints = null!;
        private WinFormsComboBox cmbPrinter = null!;
        private WinFormsComboBox cmbPaperSize = null!;
        private WinFormsTextBox txtFrameWidth = null!;
        private WinFormsTextBox txtFrameHeight = null!;
        private WinFormsComboBox cmbScale = null!;
        private WinFormsComboBox cmbCtb = null!;
        private RadioButton rbLandscape = null!;
        private RadioButton rbPortrait = null!;
        private WinFormsButton btnSelectBlocks = null!;
        private WinFormsButton btnPickBlock = null!;
        private WinFormsTextBox txtBlockName = null!;
        private WinFormsLabel lblBlockCount = null!;
        private WinFormsButton btnOK = null!;
        private WinFormsButton btnCancel = null!;
        private WinFormsLabel lblTotalPrints = null!;

        // Editor reference for point picking
        private Editor _editor;

        // Static để lưu giá trị giữa các phiên
        private static string _lastPrinter = "PDF reDirect v2";
        private static string _lastPaperSize = "A3";
        private static double _lastFrameWidth = 84.0;
        private static double _lastFrameHeight = 59.4;
        private static string _lastScale = "5:1";
        private static string _lastCtb = "monochrome.ctb";
        private static bool _lastIsLandscape = true;
        public static string LastBlockName { get; set; } = "";

        // Paper sizes cho từng loại máy in
        private static readonly Dictionary<string, string[]> PrinterPaperSizes = new()
        {
            { "PDF reDirect v2", new[] { "A0", "A1", "A2", "A3", "A4", "A5", "Letter", "Legal", "Tabloid" } },
            { "Microsoft Print to PDF", new[] { "A3", "A4", "Letter", "Legal", "Tabloid" } },
            { "AutoCAD PDF (General Documentation).pc3", new[] { 
                "ISO full bleed A0 (1189.00 x 841.00 MM)",
                "ISO full bleed A1 (841.00 x 594.00 MM)", 
                "ISO full bleed A2 (594.00 x 420.00 MM)",
                "ISO full bleed A3 (420.00 x 297.00 MM)",
                "ISO full bleed A4 (297.00 x 210.00 MM)"
            }}
        };

        public BatchPlotSettingsForm(Editor editor, string blockName)
        {
            _editor = editor;
            BlockName = blockName;
            InitializeComponent();
            LoadDefaultValues();
        }

        private void InitializeComponent()
        {
            this.Text = "🖨️ In Hàng Loạt Model - AT_InModel_HangLoat";
            this.Size = new Size(750, 680);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            int y = 15;

            // ========== Group: Chọn Block ==========
            var grpBlock = new GroupBox
            {
                Text = "📦 Chọn Block khung in",
                Location = new Point(15, y),
                Size = new Size(705, 85),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            var lblBlockNameLabel = new WinFormsLabel
            {
                Text = "Tên Block:",
                Location = new Point(10, 28),
                Size = new Size(75, 23),
                ForeColor = Color.Black
            };

            txtBlockName = new WinFormsTextBox
            {
                Location = new Point(90, 25),
                Size = new Size(180, 25),
                Text = BlockName,
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            btnPickBlock = new WinFormsButton
            {
                Text = "🔄 Đổi Block",
                Location = new Point(280, 23),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPickBlock.FlatAppearance.BorderSize = 0;
            btnPickBlock.Click += BtnPickBlock_Click;

            btnSelectBlocks = new WinFormsButton
            {
                Text = "📍 Quét chọn Block",
                Location = new Point(390, 23),
                Size = new Size(140, 28),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSelectBlocks.FlatAppearance.BorderSize = 0;
            btnSelectBlocks.Click += BtnSelectBlocks_Click;

            lblBlockCount = new WinFormsLabel
            {
                Text = "Đã chọn: 0 block",
                Location = new Point(540, 28),
                Size = new Size(150, 23),
                ForeColor = Color.FromArgb(0, 120, 215),
                Font = new DrawingFont("Segoe UI", 9, FontStyle.Bold)
            };

            var lblBlockNote = new WinFormsLabel
            {
                Text = "💡 Chọn Block mẫu (🔄) → Quét chọn tất cả block cùng tên (📍). Điểm đặt block = góc trái dưới khung in.",
                Location = new Point(10, 55),
                Size = new Size(680, 20),
                ForeColor = Color.Gray,
                Font = new DrawingFont("Segoe UI", 8f)
            };

            grpBlock.Controls.Add(lblBlockNameLabel);
            grpBlock.Controls.Add(txtBlockName);
            grpBlock.Controls.Add(btnPickBlock);
            grpBlock.Controls.Add(btnSelectBlocks);
            grpBlock.Controls.Add(lblBlockCount);
            grpBlock.Controls.Add(lblBlockNote);

            y += 95;

            // ========== Group: Danh sách điểm in (nhóm theo Y) ==========
            var grpPoints = new GroupBox
            {
                Text = "📍 Danh sách điểm in (nhóm theo hàng Y)",
                Location = new Point(15, y),
                Size = new Size(705, 200),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            dgvPrintPoints = new DataGridView
            {
                Location = new Point(10, 25),
                Size = new Size(580, 160),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            dgvPrintPoints.Columns.Add("RowIdx", "Hàng");
            dgvPrintPoints.Columns.Add("YValue", "Tọa độ Y");
            dgvPrintPoints.Columns.Add("Count", "Số bản");
            dgvPrintPoints.Columns.Add("XRange", "Phạm vi X");
            dgvPrintPoints.Columns["RowIdx"].Width = 55;
            dgvPrintPoints.Columns["YValue"].Width = 120;
            dgvPrintPoints.Columns["Count"].Width = 80;
            dgvPrintPoints.Columns["XRange"].Width = 310;

            lblTotalPrints = new WinFormsLabel
            {
                Text = "Tổng:\n0 bản\n0 hàng",
                Location = new Point(600, 25),
                Size = new Size(95, 80),
                Font = new DrawingFont("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                TextAlign = ContentAlignment.TopLeft
            };

            grpPoints.Controls.Add(dgvPrintPoints);
            grpPoints.Controls.Add(lblTotalPrints);

            y += 210;

            // ========== Group: Cài đặt in ==========
            var grpSettings = new GroupBox
            {
                Text = "⚙️ Cài đặt in",
                Location = new Point(15, y),
                Size = new Size(350, 290),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            int settingY = 30;
            int labelX = 15;
            int controlX = 130;
            int controlWidth = 200;

            // Máy in
            var lblPrinter = new WinFormsLabel
            {
                Text = "Máy in:",
                Location = new Point(labelX, settingY + 3),
                Size = new Size(100, 23),
                ForeColor = Color.Black
            };
            cmbPrinter = new WinFormsComboBox
            {
                Location = new Point(controlX, settingY),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPrinter.Items.AddRange(new object[] {
                "PDF reDirect v2",
                "Microsoft Print to PDF",
                "AutoCAD PDF (General Documentation).pc3"
            });
            cmbPrinter.SelectedIndexChanged += CmbPrinter_SelectedIndexChanged;
            grpSettings.Controls.Add(lblPrinter);
            grpSettings.Controls.Add(cmbPrinter);

            settingY += 40;

            // Kích thước giấy
            var lblPaperSize = new WinFormsLabel
            {
                Text = "Kích thước giấy:",
                Location = new Point(labelX, settingY + 3),
                Size = new Size(110, 23),
                ForeColor = Color.Black
            };
            cmbPaperSize = new WinFormsComboBox
            {
                Location = new Point(controlX, settingY),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            grpSettings.Controls.Add(lblPaperSize);
            grpSettings.Controls.Add(cmbPaperSize);

            settingY += 40;

            // Tỷ lệ
            var lblScale = new WinFormsLabel
            {
                Text = "Tỷ lệ in:",
                Location = new Point(labelX, settingY + 3),
                Size = new Size(100, 23),
                ForeColor = Color.Black
            };
            cmbScale = new WinFormsComboBox
            {
                Location = new Point(controlX, settingY),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbScale.Items.AddRange(new object[] {
                "1:1", "2:1", "5:1", "10:1", "20:1", "50:1", "100:1",
                "1:2", "1:5", "1:10", "1:20", "1:50", "1:100", "1:200", "1:500", "1:1000"
            });
            grpSettings.Controls.Add(lblScale);
            grpSettings.Controls.Add(cmbScale);

            settingY += 40;

            // CTB file
            var lblCtb = new WinFormsLabel
            {
                Text = "CTB file:",
                Location = new Point(labelX, settingY + 3),
                Size = new Size(100, 23),
                ForeColor = Color.Black
            };
            cmbCtb = new WinFormsComboBox
            {
                Location = new Point(controlX, settingY),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCtb.Items.AddRange(new object[] {
                "monochrome.ctb",
                "acad.ctb",
                "grayscale.ctb",
                "Screening 100%.ctb",
                "Screening 75%.ctb",
                "Screening 50%.ctb",
                "Screening 25%.ctb"
            });
            grpSettings.Controls.Add(lblCtb);
            grpSettings.Controls.Add(cmbCtb);

            settingY += 40;

            // Hướng giấy
            var lblOrientation = new WinFormsLabel
            {
                Text = "Hướng giấy:",
                Location = new Point(labelX, settingY + 3),
                Size = new Size(100, 23),
                ForeColor = Color.Black
            };
            rbLandscape = new RadioButton
            {
                Text = "Ngang",
                Location = new Point(controlX, settingY),
                Size = new Size(70, 23),
                Checked = true
            };
            rbPortrait = new RadioButton
            {
                Text = "Dọc",
                Location = new Point(controlX + 80, settingY),
                Size = new Size(60, 23)
            };
            grpSettings.Controls.Add(lblOrientation);
            grpSettings.Controls.Add(rbLandscape);
            grpSettings.Controls.Add(rbPortrait);

            // ========== Group: Kích thước khung ==========
            var grpFrame = new GroupBox
            {
                Text = "📐 Kích thước khung in",
                Location = new Point(380, y),
                Size = new Size(340, 290),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            int frameY = 30;
            int frameLabelX = 15;
            int frameControlX = 150;
            int frameControlWidth = 120;

            // Chiều rộng khung
            var lblFrameWidth = new WinFormsLabel
            {
                Text = "Chiều rộng (mm):",
                Location = new Point(frameLabelX, frameY + 3),
                Size = new Size(130, 23),
                ForeColor = Color.Black
            };
            txtFrameWidth = new WinFormsTextBox
            {
                Location = new Point(frameControlX, frameY),
                Size = new Size(frameControlWidth, 25),
                Text = "84"
            };
            grpFrame.Controls.Add(lblFrameWidth);
            grpFrame.Controls.Add(txtFrameWidth);

            frameY += 40;

            // Chiều cao khung
            var lblFrameHeight = new WinFormsLabel
            {
                Text = "Chiều cao (mm):",
                Location = new Point(frameLabelX, frameY + 3),
                Size = new Size(130, 23),
                ForeColor = Color.Black
            };
            txtFrameHeight = new WinFormsTextBox
            {
                Location = new Point(frameControlX, frameY),
                Size = new Size(frameControlWidth, 25),
                Text = "59.4"
            };
            grpFrame.Controls.Add(lblFrameHeight);
            grpFrame.Controls.Add(txtFrameHeight);

            frameY += 50;

            // Ghi chú
            var lblNote = new WinFormsLabel
            {
                Text = "💡 Ghi chú:\n" +
                       "- Điểm đặt block = góc trái dưới\n" +
                       "- Nhóm cùng Y (dung sai 0.5)\n" +
                       "- In: Trái→Phải, Trên→Dưới\n" +
                       "- Kích thước khung = vùng in\n" +
                       "  quanh mỗi điểm đặt block",
                Location = new Point(frameLabelX, frameY),
                Size = new Size(300, 120),
                ForeColor = Color.Gray,
                Font = new DrawingFont("Segoe UI", 8.5f)
            };
            grpFrame.Controls.Add(lblNote);

            y += 300;

            // ========== Buttons OK/Cancel ==========
            btnOK = new WinFormsButton
            {
                Text = "✅ In",
                Location = new Point(280, y),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new DrawingFont("Segoe UI", 10, FontStyle.Bold)
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;

            btnCancel = new WinFormsButton
            {
                Text = "❌ Hủy",
                Location = new Point(400, y),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new DrawingFont("Segoe UI", 10, FontStyle.Bold)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Add controls
            this.Controls.Add(grpBlock);
            this.Controls.Add(grpPoints);
            this.Controls.Add(grpSettings);
            this.Controls.Add(grpFrame);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void CmbPrinter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdatePaperSizeList();
        }

        private void UpdatePaperSizeList()
        {
            string selectedPrinter = cmbPrinter.SelectedItem?.ToString() ?? "PDF reDirect v2";
            cmbPaperSize.Items.Clear();

            if (PrinterPaperSizes.TryGetValue(selectedPrinter, out string[]? paperSizes))
            {
                cmbPaperSize.Items.AddRange(paperSizes);
            }
            else
            {
                cmbPaperSize.Items.AddRange(new object[] { "A0", "A1", "A2", "A3", "A4" });
            }

            int a3Index = -1;
            for (int i = 0; i < cmbPaperSize.Items.Count; i++)
            {
                if (cmbPaperSize.Items[i]?.ToString()?.Contains("A3") == true)
                {
                    a3Index = i;
                    break;
                }
            }
            
            if (a3Index >= 0)
                cmbPaperSize.SelectedIndex = a3Index;
            else if (cmbPaperSize.Items.Count > 0)
                cmbPaperSize.SelectedIndex = 0;
        }

        private void LoadDefaultValues()
        {
            bool printerFound = false;
            for (int i = 0; i < cmbPrinter.Items.Count; i++)
            {
                if (cmbPrinter.Items[i]?.ToString() == _lastPrinter)
                {
                    cmbPrinter.SelectedIndex = i;
                    printerFound = true;
                    break;
                }
            }
            
            if (!printerFound && cmbPrinter.Items.Count > 0)
            {
                cmbPrinter.SelectedIndex = 0;
                _lastPrinter = cmbPrinter.Items[0]?.ToString() ?? "PDF reDirect v2";
            }
            
            UpdatePaperSizeList();
            SelectComboItemExact(cmbPaperSize, _lastPaperSize);
            
            txtFrameWidth.Text = _lastFrameWidth.ToString("F1");
            txtFrameHeight.Text = _lastFrameHeight.ToString("F1");
            SelectComboItemExact(cmbScale, _lastScale);
            SelectComboItemExact(cmbCtb, _lastCtb);
            rbLandscape.Checked = _lastIsLandscape;
            rbPortrait.Checked = !_lastIsLandscape;
        }

        private void SelectComboItemExact(WinFormsComboBox combo, string value)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i]?.ToString() == value)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            
            for (int i = 0; i < combo.Items.Count; i++)
            {
                string? item = combo.Items[i]?.ToString();
                if (item != null && item.Contains(value))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            
            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        /// <summary>
        /// Pick block mẫu để lấy tên
        /// </summary>
        private void BtnPickBlock_Click(object? sender, EventArgs e)
        {
            this.Hide();

            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                PromptEntityOptions peo = new PromptEntityOptions("\n📍 Chọn Block mẫu (khung in):");
                peo.SetRejectMessage("\n⚠️ Vui lòng chọn Block!");
                peo.AddAllowedClass(typeof(BlockReference), true);

                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status == PromptStatus.OK)
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockReference blkRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blkRef != null)
                        {
                            BlockTableRecord btr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                            if (btr != null)
                            {
                                BlockName = btr.Name;
                                txtBlockName.Text = BlockName;
                                LastBlockName = BlockName;
                                SelectedBlockIds.Clear();
                                PrintRows.Clear();
                                dgvPrintPoints.Rows.Clear();
                                lblBlockCount.Text = "Đã chọn: 0 block";
                                UpdateTotalPrints();
                                ed.WriteMessage($"\n✅ Đã chọn Block mẫu: {BlockName}");
                            }
                        }
                        tr.Commit();
                    }
                }
            }
            catch (System.Exception ex)
            {
                _editor.WriteMessage($"\n❌ Lỗi: {ex.Message}");
            }
            finally
            {
                this.Show();
                this.BringToFront();
                this.Focus();
            }
        }

        /// <summary>
        /// Quét chọn các block cùng tên, nhóm theo Y, sắp xếp trái→phải
        /// </summary>
        private void BtnSelectBlocks_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BlockName))
            {
                MessageBox.Show("Vui lòng chọn Block mẫu trước (nút 🔄 Đổi Block)!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.Hide();

            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                // Tạo filter để chọn block theo tên (giống lệnh 18)
                TypedValue[] filterList = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, BlockName)
                };
                SelectionFilter filter = new SelectionFilter(filterList);

                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = $"\n📍 Quét chọn các Block '{BlockName}' cần in:";
                pso.AllowDuplicates = false;

                PromptSelectionResult psr = ed.GetSelection(pso, filter);

                if (psr.Status == PromptStatus.OK)
                {
                    SelectionSet ss = psr.Value;
                    SelectedBlockIds.Clear();

                    // Thu thập vị trí các block
                    var blockPositions = new List<Point3d>();

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (SelectedObject so in ss)
                        {
                            if (so != null)
                            {
                                SelectedBlockIds.Add(so.ObjectId);
                                BlockReference blkRef = tr.GetObject(so.ObjectId, OpenMode.ForRead) as BlockReference;
                                if (blkRef != null)
                                {
                                    blockPositions.Add(blkRef.Position);
                                }
                            }
                        }
                        tr.Commit();
                    }

                    // Nhóm theo Y (dung sai 0.5) - giống lệnh 18
                    double yTolerance = 0.5;
                    var yGroups = blockPositions
                        .GroupBy(p => Math.Round(p.Y / yTolerance) * yTolerance)
                        .OrderByDescending(g => g.Key)  // Trên→Dưới (Y cao trước)
                        .ToList();

                    // Tạo PrintRows
                    PrintRows.Clear();
                    dgvPrintPoints.Rows.Clear();
                    int rowIdx = 1;

                    foreach (var group in yGroups)
                    {
                        var sortedPositions = group.OrderBy(p => p.X).ToList(); // Trái→Phải

                        var row = new PrintRowInfo
                        {
                            YValue = group.Key,
                            BlockPositions = sortedPositions,
                            RowIndex = rowIdx
                        };
                        PrintRows.Add(row);

                        // Hiển thị trên DataGridView
                        double minX = sortedPositions.First().X;
                        double maxX = sortedPositions.Last().X;
                        string xRange = sortedPositions.Count == 1
                            ? $"X = {minX:F1}"
                            : $"X: {minX:F1} → {maxX:F1}";

                        dgvPrintPoints.Rows.Add(rowIdx, group.Key.ToString("F1"), sortedPositions.Count, xRange);
                        rowIdx++;
                    }

                    UpdateTotalPrints();
                    lblBlockCount.Text = $"Đã chọn: {SelectedBlockIds.Count} block";
                    ed.WriteMessage($"\n✅ Đã chọn {SelectedBlockIds.Count} block '{BlockName}' → {PrintRows.Count} hàng");
                }
            }
            catch (System.Exception ex)
            {
                _editor.WriteMessage($"\n❌ Lỗi: {ex.Message}");
            }
            finally
            {
                this.Show();
                this.BringToFront();
                this.Focus();
            }
        }

        private void UpdateTotalPrints()
        {
            int total = PrintRows.Sum(r => r.Count);
            lblTotalPrints.Text = $"Tổng:\n{total} bản\n{PrintRows.Count} hàng";
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Validate
            if (PrintRows.Count == 0)
            {
                MessageBox.Show("Vui lòng quét chọn các block trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(txtFrameWidth.Text, out double frameWidth) || frameWidth <= 0)
            {
                MessageBox.Show("Chiều rộng khung phải là số dương!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtFrameWidth.Focus();
                return;
            }

            if (!double.TryParse(txtFrameHeight.Text, out double frameHeight) || frameHeight <= 0)
            {
                MessageBox.Show("Chiều cao khung phải là số dương!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtFrameHeight.Focus();
                return;
            }

            // Save values
            SelectedPrinter = cmbPrinter.SelectedItem?.ToString() ?? "PDF reDirect v2";
            SelectedPaperSize = cmbPaperSize.SelectedItem?.ToString() ?? "A3";
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            SelectedScale = cmbScale.SelectedItem?.ToString() ?? "5:1";
            SelectedCtb = cmbCtb.SelectedItem?.ToString() ?? "monochrome.ctb";
            IsLandscape = rbLandscape.Checked;

            // Save for next session
            _lastPrinter = SelectedPrinter;
            _lastPaperSize = SelectedPaperSize;
            _lastFrameWidth = FrameWidth;
            _lastFrameHeight = FrameHeight;
            _lastScale = SelectedScale;
            _lastCtb = SelectedCtb;
            _lastIsLandscape = IsLandscape;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    /// <summary>
    /// Class chứa lệnh in hàng loạt
    /// </summary>
    public class AT_InModel_HangLoat_Commands
    {
        [CommandMethod("AT_InModel_HangLoat")]
        public static void AT_InModel_HangLoat()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== 🖨️ IN HÀNG LOẠT MODEL - AT_InModel_HangLoat ===");

                // Kiểm tra xem đang ở Model space không
                if (db.TileMode == false)
                {
                    ed.WriteMessage("\n⚠️ Bạn đang ở Paper space. Vui lòng chuyển sang Model space.");
                    ed.WriteMessage("\n   Gõ lệnh MODEL hoặc nhấn phím Tab để chuyển.");
                    return;
                }

                // Bước 1: Chọn block mẫu (nếu chưa có)
                string blockName = BatchPlotSettingsForm.LastBlockName ?? "";

                if (string.IsNullOrEmpty(blockName))
                {
                    // Pick block mẫu
                    PromptEntityOptions peo = new PromptEntityOptions("\n📍 Chọn Block khung in mẫu:");
                    peo.SetRejectMessage("\n⚠️ Vui lòng chọn Block!");
                    peo.AddAllowedClass(typeof(BlockReference), true);

                    PromptEntityResult per = ed.GetEntity(peo);

                    if (per.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n❌ Đã hủy lệnh.");
                        return;
                    }

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockReference blkRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blkRef != null)
                        {
                            BlockTableRecord btr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                            if (btr != null)
                            {
                                blockName = btr.Name;
                                BatchPlotSettingsForm.LastBlockName = blockName;
                            }
                        }
                        tr.Commit();
                    }
                }

                ed.WriteMessage($"\n📦 Block mẫu: {blockName}");

                // Bước 2: Hiển thị form
                List<PrintRowInfo> printRows;
                string printer, paperSize, scale, ctb, orientation;
                double frameWidth, frameHeight;

                using (var form = new BatchPlotSettingsForm(ed, blockName))
                {
                    if (form.ShowDialog() != DialogResult.OK)
                    {
                        ed.WriteMessage("\n❌ Đã hủy lệnh.");
                        return;
                    }

                    // Lấy thông tin từ form
                    printRows = new List<PrintRowInfo>(form.PrintRows);
                    printer = form.SelectedPrinter;
                    paperSize = form.SelectedPaperSize;
                    frameWidth = form.FrameWidth;
                    frameHeight = form.FrameHeight;
                    scale = form.SelectedScale;
                    ctb = form.SelectedCtb;
                    orientation = form.IsLandscape ? "Landscape" : "Portrait";
                }

                int totalPrints = printRows.Sum(r => r.Count);

                ed.WriteMessage($"\n\n📋 Thông tin in:");
                ed.WriteMessage($"\n   - Máy in: {printer}");
                ed.WriteMessage($"\n   - Kích thước giấy: {paperSize}");
                ed.WriteMessage($"\n   - Kích thước khung: {frameWidth} x {frameHeight} mm");
                ed.WriteMessage($"\n   - Tỷ lệ: {scale}");
                ed.WriteMessage($"\n   - CTB: {ctb}");
                ed.WriteMessage($"\n   - Hướng: {orientation}");
                ed.WriteMessage($"\n   - Số hàng: {printRows.Count}");
                ed.WriteMessage($"\n   - Tổng số bản in: {totalPrints}");

                // Bước 3: Xây dựng lệnh in LISP
                // Thứ tự: Trái→Phải trong mỗi hàng, Trên→Dưới giữa các hàng
                var allCommands = new System.Text.StringBuilder();
                int printedCount = 0;

                allCommands.AppendLine("(progn ");

                foreach (var row in printRows)
                {
                    ed.WriteMessage($"\n\n📍 Hàng {row.RowIndex} (Y ≈ {row.YValue:F1}) - {row.Count} bản:");

                    foreach (var pos in row.BlockPositions)
                    {
                        // Điểm đặt block = góc trái dưới
                        double p3X = pos.X;
                        double p3Y = pos.Y;
                        double p4X = p3X + frameWidth;
                        double p4Y = p3Y + frameHeight;

                        AppendPlotCommand(allCommands,
                            printer, paperSize, orientation,
                            p3X, p3Y, p4X, p4Y,
                            scale, ctb);

                        printedCount++;
                        ed.WriteMessage($"\n   📄 Bản {printedCount}: ({p3X:F0},{p3Y:F0}) → ({p4X:F0},{p4Y:F0})");
                    }
                }

                allCommands.AppendLine("(princ)) ");

                // Gửi tất cả lệnh một lần
                ed.WriteMessage($"\n\n🚀 Đang gửi {printedCount} lệnh in...");
                doc.SendStringToExecute(allCommands.ToString(), true, false, false);

                ed.WriteMessage($"\n\n🎉 Đã gửi lệnh in cho {printedCount} bản vẽ!");
                ed.WriteMessage("\n💡 Vui lòng đợi quá trình in hoàn tất.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi: {ex.Message}");
                ed.WriteMessage($"\n   Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Thêm lệnh -PLOT vào StringBuilder - sử dụng LISP (command ...) syntax
        /// Giống hệt cách LISP gốc hoạt động
        /// </summary>
        private static void AppendPlotCommand(System.Text.StringBuilder sb,
            string printer, string paperSize, string orientation,
            double p3X, double p3Y, double p4X, double p4Y,
            string scale, string ctb)
        {
            sb.Append("(command \"-PLOT\" ");
            sb.Append("\"Y\" ");           // Detailed plot configuration? Yes
            sb.Append("\"\" ");            // Layout name (empty = current/Model)
            sb.Append($"\"{printer}\" ");  // Printer/plotter name
            sb.Append($"\"{paperSize}\" ");// Paper size
            sb.Append("\"Millimeters\" "); // Drawing units
            sb.Append($"\"{orientation}\" ");// Landscape/Portrait
            sb.Append("\"No\" ");          // Plot upside-down? No
            sb.Append("\"Window\" ");      // Plot area: Window
            sb.Append($"(list {p3X:F4} {p3Y:F4}) ");  // First corner as LISP list
            sb.Append($"(list {p4X:F4} {p4Y:F4}) ");  // Second corner as LISP list
            sb.Append($"\"{scale}\" ");    // Plot scale
            sb.Append("\"Center\" ");      // Plot offset: Center
            sb.Append("\"Yes\" ");         // Plot with plot styles? Yes
            sb.Append($"\"{ctb}\" ");      // Plot style table (CTB)
            sb.Append("\"Yes\" ");         // Plot with lineweights? Yes
            sb.Append("\"As displayed\" ");// Shade plot: As displayed
            sb.Append("\"No\" ");          // Save changes to layout? No
            sb.Append("\"No\" ");          // Plot to file? No
            sb.Append("\"Yes\"");          // Proceed with plot? Yes
            sb.AppendLine(") ");           // Close LISP command
        }
    }
}
