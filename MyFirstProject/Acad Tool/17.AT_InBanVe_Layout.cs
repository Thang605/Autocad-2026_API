// (C) Copyright 2024 by T27
// Lệnh in bản vẽ trong Layout - In các bản vẽ A3 đặt cạnh nhau
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
using Autodesk.AutoCAD.PlottingServices;

// Alias để tránh xung đột namespace
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsTextBox = System.Windows.Forms.TextBox;
using WinFormsButton = System.Windows.Forms.Button;
using WinFormsComboBox = System.Windows.Forms.ComboBox;
using DrawingFont = System.Drawing.Font;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.AT_InBanVe_Layout_Commands))]

namespace Civil3DCsharp
{
    /// <summary>
    /// Enum cho thứ tự sắp xếp khi in
    /// </summary>
    public enum LayoutSortOrder
    {
        Normal,         // Giữ nguyên thứ tự tìm thấy
        LeftToRight,    // Từ trái sang phải
        TopToBottom     // Từ trên xuống dưới
    }

    /// <summary>
    /// Class lưu thông tin vùng in trong Layout
    /// </summary>
    public class LayoutPrintArea
    {
        public int Index { get; set; }
        public ObjectId ObjectId { get; set; }
        public Point3d MinPoint { get; set; }
        public Point3d MaxPoint { get; set; }
        public string Name { get; set; } = "";

        public LayoutPrintArea(int index, ObjectId objId, Point3d minPt, Point3d maxPt, string name = "")
        {
            Index = index;
            ObjectId = objId;
            MinPoint = minPt;
            MaxPoint = maxPt;
            Name = name;
        }
    }

    /// <summary>
    /// Form cấu hình in trong Layout
    /// </summary>
    public class LayoutPrintForm : Form
    {
        // Properties trả về kết quả
        public string SelectedPrinter { get; private set; } = "PDF reDirect v2";
        public string SelectedPaperSize { get; private set; } = "A3";
        public string SelectedPlotStyle { get; private set; } = "monochrome.ctb";
        public bool UseBlock { get; private set; } = true;
        public string BlockName { get; private set; } = "";
        public LayoutSortOrder SortOrder { get; private set; } = LayoutSortOrder.LeftToRight;
        public bool CenterPlot { get; private set; } = true;
        public double OffsetX { get; private set; } = 0;
        public double OffsetY { get; private set; } = 0;
        public List<LayoutPrintArea> PrintAreas { get; private set; } = new();
        public double PaperWidth { get; private set; } = 420;  // mm
        public double PaperHeight { get; private set; } = 297; // mm
        public double PrintWidth { get; private set; } = 0;    // 0 = dùng extents block
        public double PrintHeight { get; private set; } = 0;   // 0 = dùng extents block

        // Controls
        private WinFormsComboBox cmbPrinter = null!;
        private WinFormsComboBox cmbPaperSize = null!;
        private WinFormsComboBox cmbPlotStyle = null!;
        private WinFormsTextBox txtPaperWidth = null!;
        private WinFormsTextBox txtPaperHeight = null!;
        private RadioButton rbBlock = null!;
        private WinFormsTextBox txtBlockName = null!;
        private WinFormsButton btnPick = null!;
        private WinFormsButton btnSelect = null!;
        private WinFormsLabel lblBlockCount = null!;
        private WinFormsTextBox txtPrintWidth = null!;
        private WinFormsTextBox txtPrintHeight = null!;
        private RadioButton rbNormal = null!;
        private RadioButton rbLeftToRight = null!;
        private RadioButton rbTopToBottom = null!;

        private WinFormsButton btnPrint = null!;
        private WinFormsButton btnPreview = null!;
        private WinFormsButton btnCancel = null!;
        private WinFormsLabel lblStatus = null!;

        // List các block đã chọn
        private List<ObjectId> _selectedBlockIds = new();

        // Editor reference for picking
        private Editor _editor;

        // Static để lưu giá trị giữa các phiên
        private static string _lastPrinter = "PDF reDirect v2";
        private static string _lastPaperSize = "A3";
        private static string _lastPlotStyle = "monochrome.ctb";
        private static bool _lastUseBlock = true;
        private static string _lastBlockName = "";
        private static LayoutSortOrder _lastSortOrder = LayoutSortOrder.LeftToRight;
        private static bool _lastCenterPlot = true;

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

        // Kích thước giấy chuẩn (mm)
        private static readonly Dictionary<string, (double Width, double Height)> PaperDimensions = new()
        {
            { "A0", (1189, 841) },
            { "A1", (841, 594) },
            { "A2", (594, 420) },
            { "A3", (420, 297) },
            { "A4", (297, 210) },
            { "A5", (210, 148) },
            { "Letter", (279.4, 215.9) },
            { "Legal", (355.6, 215.9) },
            { "Tabloid", (431.8, 279.4) }
        };

        public LayoutPrintForm(Editor editor)
        {
            _editor = editor;
            InitializeComponent();
            LoadDefaultValues();
        }

        private void InitializeComponent()
        {
            this.Text = "In bản vẽ theo block";
            this.Size = new Size(340, 405);  // Giảm height vì bỏ Plot To File và Plot Offset
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = SystemColors.Control;

            int y = 10;
            int labelX = 10;
            int controlX = 85;
            int controlWidth = 150;

            // ========== Group: Setting ==========
            var grpSetting = new GroupBox
            {
                Text = "Setting",
                Location = new Point(10, y),
                Size = new Size(305, 130)  // Tăng height cho Paper W/H
            };

            int settingY = 20;

            // Printer
            var lblPrinter = new WinFormsLabel
            {
                Text = "Printer",
                Location = new Point(labelX, settingY + 3),
                Size = new Size(60, 20)
            };
            cmbPrinter = new WinFormsComboBox
            {
                Location = new Point(controlX, settingY),
                Size = new Size(controlWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPrinter.Items.AddRange(new object[] {
                "PDF reDirect v2",
                "Microsoft Print to PDF",
                "AutoCAD PDF (General Documentation).pc3"
            });
            cmbPrinter.SelectedIndexChanged += CmbPrinter_SelectedIndexChanged;
            grpSetting.Controls.Add(lblPrinter);
            grpSetting.Controls.Add(cmbPrinter);

            settingY += 26;

            // Paper Size
            var lblPaperSize = new WinFormsLabel
            {
                Text = "Paper Size",
                Location = new Point(labelX, settingY + 3),
                Size = new Size(70, 20)
            };
            cmbPaperSize = new WinFormsComboBox
            {
                Location = new Point(controlX, settingY),
                Size = new Size(controlWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            grpSetting.Controls.Add(lblPaperSize);
            grpSetting.Controls.Add(cmbPaperSize);

            settingY += 26;

            // Plot Style
            var lblPlotStyle = new WinFormsLabel
            {
                Text = "Plot Style",
                Location = new Point(labelX, settingY + 3),
                Size = new Size(65, 20)
            };
            cmbPlotStyle = new WinFormsComboBox
            {
                Location = new Point(controlX, settingY),
                Size = new Size(controlWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPlotStyle.Items.AddRange(new object[] {
                "monochrome.ctb",  // Mặc định theo LISP
                "acad.ctb",
                "grayscale.ctb",
                "Screening 100%.ctb",
                "Screening 75%.ctb",
                "Screening 50%.ctb",
                "acad.stb"
            });
            grpSetting.Controls.Add(lblPlotStyle);
            grpSetting.Controls.Add(cmbPlotStyle);

            settingY += 26;

            // Paper Width/Height
            var lblPaperWH = new WinFormsLabel
            {
                Text = "Paper W×H",
                Location = new Point(labelX, settingY + 3),
                Size = new Size(70, 20)
            };
            txtPaperWidth = new WinFormsTextBox
            {
                Location = new Point(controlX, settingY),
                Size = new Size(50, 23),
                Text = "420"
            };
            var lblPaperX = new WinFormsLabel
            {
                Text = "×",
                Location = new Point(138, settingY + 3),
                Size = new Size(15, 20)
            };
            txtPaperHeight = new WinFormsTextBox
            {
                Location = new Point(155, settingY),
                Size = new Size(50, 23),
                Text = "297"
            };
            var lblPaperMM = new WinFormsLabel
            {
                Text = "mm",
                Location = new Point(208, settingY + 3),
                Size = new Size(30, 20)
            };
            grpSetting.Controls.Add(lblPaperWH);
            grpSetting.Controls.Add(txtPaperWidth);
            grpSetting.Controls.Add(lblPaperX);
            grpSetting.Controls.Add(txtPaperHeight);
            grpSetting.Controls.Add(lblPaperMM);

            this.Controls.Add(grpSetting);

            y += 140;

            // ========== Group: Print Method ==========
            var grpPrintMethod = new GroupBox
            {
                Text = "Print Method",
                Location = new Point(10, y),
                Size = new Size(305, 115)  // Tăng height cho Print W/H và Count
            };

            int methodY = 18;

            rbBlock = new RadioButton
            {
                Text = "Block",
                Location = new Point(labelX, methodY),
                Size = new Size(55, 20),
                Checked = true
            };
            rbBlock.CheckedChanged += RbBlock_CheckedChanged;

            txtBlockName = new WinFormsTextBox
            {
                Location = new Point(70, methodY - 2),
                Size = new Size(95, 23)
            };

            btnPick = new WinFormsButton
            {
                Text = "Pick",
                Location = new Point(170, methodY - 2),
                Size = new Size(40, 23)
            };
            btnPick.Click += BtnPick_Click;

            btnSelect = new WinFormsButton
            {
                Text = "Select",
                Location = new Point(215, methodY - 2),
                Size = new Size(50, 23)
            };
            btnSelect.Click += BtnSelect_Click;

            grpPrintMethod.Controls.Add(rbBlock);
            grpPrintMethod.Controls.Add(txtBlockName);
            grpPrintMethod.Controls.Add(btnPick);
            grpPrintMethod.Controls.Add(btnSelect);

            methodY += 25;

            // Block Count Label
            lblBlockCount = new WinFormsLabel
            {
                Text = "Số bản in: 0",
                Location = new Point(labelX, methodY + 2),
                Size = new Size(100, 20),
                ForeColor = Color.Green,
                Font = new DrawingFont(this.Font, FontStyle.Bold)
            };
            grpPrintMethod.Controls.Add(lblBlockCount);

            // Print Width/Height
            var lblPrintWH = new WinFormsLabel
            {
                Text = "Print W×H",
                Location = new Point(105, methodY + 2),
                Size = new Size(60, 20)
            };
            txtPrintWidth = new WinFormsTextBox
            {
                Location = new Point(165, methodY),
                Size = new Size(40, 23),
                Text = ""
            };
            var lblPrintX = new WinFormsLabel
            {
                Text = "×",
                Location = new Point(207, methodY + 2),
                Size = new Size(12, 20)
            };
            txtPrintHeight = new WinFormsTextBox
            {
                Location = new Point(220, methodY),
                Size = new Size(40, 23),
                Text = ""
            };
            var lblPrintMM = new WinFormsLabel
            {
                Text = "mm",
                Location = new Point(262, methodY + 2),
                Size = new Size(25, 20)
            };
            grpPrintMethod.Controls.Add(lblPrintWH);
            grpPrintMethod.Controls.Add(txtPrintWidth);
            grpPrintMethod.Controls.Add(lblPrintX);
            grpPrintMethod.Controls.Add(txtPrintHeight);
            grpPrintMethod.Controls.Add(lblPrintMM);

            this.Controls.Add(grpPrintMethod);

            y += 125;

            // ========== Group: Sort ==========
            var grpSort = new GroupBox
            {
                Text = "Sort",
                Location = new Point(10, y),
                Size = new Size(305, 45)
            };

            rbNormal = new RadioButton
            {
                Text = "Normal",
                Location = new Point(labelX, 18),
                Size = new Size(65, 20)
            };

            rbLeftToRight = new RadioButton
            {
                Text = "Left->Right",
                Location = new Point(80, 18),
                Size = new Size(90, 20),
                Checked = true
            };

            rbTopToBottom = new RadioButton
            {
                Text = "Top->Bottom",
                Location = new Point(175, 18),
                Size = new Size(100, 20)
            };

            grpSort.Controls.Add(rbNormal);
            grpSort.Controls.Add(rbLeftToRight);
            grpSort.Controls.Add(rbTopToBottom);

            this.Controls.Add(grpSort);

            y += 55;



            // ========== Buttons ==========
            btnPrint = new WinFormsButton
            {
                Text = "Print",
                Location = new Point(20, y),
                Size = new Size(75, 28)
            };
            btnPrint.Click += BtnPrint_Click;

            btnPreview = new WinFormsButton
            {
                Text = "Preview",
                Location = new Point(110, y),
                Size = new Size(75, 28)
            };
            btnPreview.Click += BtnPreview_Click;

            btnCancel = new WinFormsButton
            {
                Text = "Cancel",
                Location = new Point(200, y),
                Size = new Size(75, 28)
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(btnPrint);
            this.Controls.Add(btnPreview);
            this.Controls.Add(btnCancel);

            y += 35;

            // Status label
            lblStatus = new WinFormsLabel
            {
                Text = "nguyentuyen86@gmail.com",
                Location = new Point(10, y),
                Size = new Size(280, 20),
                ForeColor = Color.Blue
            };
            this.Controls.Add(lblStatus);

            this.AcceptButton = btnPrint;
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

            // Chọn A3 nếu có, hoặc item đầu tiên
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

            // Cập nhật Paper Width/Height
            UpdatePaperDimensions();

            // Đăng ký event sau khi load xong
            cmbPaperSize.SelectedIndexChanged -= CmbPaperSize_SelectedIndexChanged;
            cmbPaperSize.SelectedIndexChanged += CmbPaperSize_SelectedIndexChanged;
        }

        private void CmbPaperSize_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdatePaperDimensions();
        }

        private void UpdatePaperDimensions()
        {
            string selectedSize = cmbPaperSize.SelectedItem?.ToString() ?? "A3";

            // Tìm kích thước tương ứng
            foreach (var kvp in PaperDimensions)
            {
                if (selectedSize.Contains(kvp.Key))
                {
                    txtPaperWidth.Text = kvp.Value.Width.ToString();
                    txtPaperHeight.Text = kvp.Value.Height.ToString();
                    return;
                }
            }

            // Mặc định A3
            txtPaperWidth.Text = "420";
            txtPaperHeight.Text = "297";
        }

        private void LoadDefaultValues()
        {
            // Load printer
            SelectComboItem(cmbPrinter, _lastPrinter);
            UpdatePaperSizeList();
            SelectComboItem(cmbPaperSize, _lastPaperSize);
            SelectComboItem(cmbPlotStyle, _lastPlotStyle);

            // Print method - chỉ còn Block mode
            rbBlock.Checked = true;
            txtBlockName.Text = _lastBlockName;

            // Sort order
            rbNormal.Checked = _lastSortOrder == LayoutSortOrder.Normal;
            rbLeftToRight.Checked = _lastSortOrder == LayoutSortOrder.LeftToRight;
            rbTopToBottom.Checked = _lastSortOrder == LayoutSortOrder.TopToBottom;

            // Center

        }

        private void SelectComboItem(WinFormsComboBox combo, string value)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i]?.ToString() == value)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private void RbBlock_CheckedChanged(object? sender, EventArgs e)
        {
            bool useBlock = rbBlock.Checked;
            txtBlockName.Enabled = useBlock;
            btnPick.Enabled = useBlock;
            btnSelect.Enabled = useBlock;
        }



        private void BtnPick_Click(object? sender, EventArgs e)
        {
            this.Hide();

            try
            {
                PromptEntityOptions peo = new PromptEntityOptions("\n📍 Chọn Block để làm khung in:");
                peo.SetRejectMessage("\n⚠️ Vui lòng chọn Block!");
                peo.AddAllowedClass(typeof(BlockReference), true);

                PromptEntityResult per = _editor.GetEntity(peo);

                if (per.Status == PromptStatus.OK)
                {
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    Database db = doc.Database;

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockReference blkRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blkRef != null)
                        {
                            BlockTableRecord btr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                            if (btr != null)
                            {
                                txtBlockName.Text = btr.Name;
                                _editor.WriteMessage($"\n✅ Đã chọn Block: {btr.Name}");
                                // Reset block count khi pick block mới
                                _selectedBlockIds.Clear();
                                lblBlockCount.Text = "Số bản in: 0";
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

        private void BtnSelect_Click(object? sender, EventArgs e)
        {
            string blockName = txtBlockName.Text.Trim();
            if (string.IsNullOrEmpty(blockName))
            {
                MessageBox.Show("Vui lòng nhập tên Block hoặc Pick block trước!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.Hide();

            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                // Tạo filter để chọn block theo tên
                TypedValue[] filterList = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, blockName)
                };
                SelectionFilter filter = new SelectionFilter(filterList);

                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = $"\n📍 Quét chọn các Block '{blockName}' để in (hoặc Enter để chọn tất cả):";
                pso.AllowDuplicates = false;

                PromptSelectionResult psr = ed.GetSelection(pso, filter);

                if (psr.Status == PromptStatus.OK)
                {
                    SelectionSet ss = psr.Value;
                    _selectedBlockIds.Clear();

                    foreach (SelectedObject so in ss)
                    {
                        if (so != null)
                        {
                            _selectedBlockIds.Add(so.ObjectId);
                        }
                    }

                    lblBlockCount.Text = $"Số bản in: {_selectedBlockIds.Count}";
                    ed.WriteMessage($"\n✅ Đã chọn {_selectedBlockIds.Count} block '{blockName}'");
                }
                else if (psr.Status == PromptStatus.Error)
                {
                    // Không tìm thấy block nào
                    ed.WriteMessage($"\n⚠️ Không tìm thấy block '{blockName}' trong selection");
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

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            SaveValues();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnPreview_Click(object? sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            SaveValues();
            this.DialogResult = DialogResult.Yes; // Use Yes for Preview
            this.Close();
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtBlockName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên Block hoặc chọn từ bản vẽ!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBlockName.Focus();
                return false;
            }



            return true;
        }

        private void SaveValues()
        {
            SelectedPrinter = cmbPrinter.SelectedItem?.ToString() ?? "PDF reDirect v2";
            SelectedPaperSize = cmbPaperSize.SelectedItem?.ToString() ?? "A3";
            SelectedPlotStyle = cmbPlotStyle.SelectedItem?.ToString() ?? "monochrome.ctb";
            UseBlock = true;  // Luôn dùng Block mode
            BlockName = txtBlockName.Text.Trim();

            if (rbNormal.Checked) SortOrder = LayoutSortOrder.Normal;
            else if (rbLeftToRight.Checked) SortOrder = LayoutSortOrder.LeftToRight;
            else SortOrder = LayoutSortOrder.TopToBottom;

            CenterPlot = true;
            OffsetX = 0;
            OffsetY = 0;

            // Paper và Print dimensions
            PaperWidth = double.TryParse(txtPaperWidth.Text, out double pw) ? pw : 420;
            PaperHeight = double.TryParse(txtPaperHeight.Text, out double ph) ? ph : 297;
            PrintWidth = double.TryParse(txtPrintWidth.Text, out double prw) ? prw : 0;
            PrintHeight = double.TryParse(txtPrintHeight.Text, out double prh) ? prh : 0;

            // Chuyển selected blocks thành PrintAreas
            if (UseBlock && _selectedBlockIds.Count > 0)
            {
                PrintAreas = ConvertSelectedBlocksToPrintAreas(_selectedBlockIds, SortOrder);
            }

            // Save for next session
            _lastPrinter = SelectedPrinter;
            _lastPaperSize = SelectedPaperSize;
            _lastPlotStyle = SelectedPlotStyle;
            _lastUseBlock = UseBlock;
            _lastBlockName = BlockName;
            _lastSortOrder = SortOrder;
            _lastCenterPlot = CenterPlot;
        }

        private List<LayoutPrintArea> ConvertSelectedBlocksToPrintAreas(List<ObjectId> blockIds, LayoutSortOrder sortOrder)
        {
            var areas = new List<LayoutPrintArea>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                int index = 0;
                foreach (ObjectId objId in blockIds)
                {
                    try
                    {
                        BlockReference blkRef = tr.GetObject(objId, OpenMode.ForRead) as BlockReference;
                        if (blkRef != null)
                        {
                            index++;
                            Extents3d ext = blkRef.GeometricExtents;
                            BlockTableRecord btr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                            string name = btr?.Name ?? "";
                            areas.Add(new LayoutPrintArea(index, objId, ext.MinPoint, ext.MaxPoint, name));
                        }
                    }
                    catch { /* Skip invalid blocks */ }
                }
                tr.Commit();
            }

            // Sắp xếp theo thứ tự
            switch (sortOrder)
            {
                case LayoutSortOrder.LeftToRight:
                    areas = areas.OrderBy(a => a.MinPoint.X).ThenByDescending(a => a.MinPoint.Y).ToList();
                    break;
                case LayoutSortOrder.TopToBottom:
                    areas = areas.OrderByDescending(a => a.MinPoint.Y).ThenBy(a => a.MinPoint.X).ToList();
                    break;
            }

            // Đánh số lại sau khi sắp xếp
            for (int i = 0; i < areas.Count; i++)
            {
                areas[i].Index = i + 1;
            }

            return areas;
        }
    }

    /// <summary>
    /// Class chứa lệnh in trong Layout
    /// </summary>
    public class AT_InBanVe_Layout_Commands
    {
        [CommandMethod("AT_InBanVe_Layout")]
        public static void AT_InBanVe_Layout()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== 🖨️ IN BẢN VẼ LAYOUT - AT_InBanVe_Layout ===");

                // Kiểm tra xem đang ở Paper space không
                if (db.TileMode == true)
                {
                    ed.WriteMessage("\n⚠️ Bạn đang ở Model space. Vui lòng chuyển sang Layout (Paper space).");
                    ed.WriteMessage("\n   Nhấp vào tab Layout hoặc gõ lệnh LAYOUT.");
                    return;
                }

                // Hiển thị form
                string printer, paperSize, plotStyle, blockName;
                bool centerPlot;
                double offsetX, offsetY;
                LayoutSortOrder sortOrder;

                using (var form = new LayoutPrintForm(ed))
                {
                    var result = form.ShowDialog();

                    if (result == DialogResult.Cancel)
                    {
                        ed.WriteMessage("\n❌ Đã hủy lệnh.");
                        return;
                    }

                    bool isPreview = (result == DialogResult.Yes);

                    printer = form.SelectedPrinter;
                    paperSize = form.SelectedPaperSize;
                    plotStyle = form.SelectedPlotStyle;
                    blockName = form.BlockName;
                    sortOrder = form.SortOrder;
                    centerPlot = form.CenterPlot;
                    offsetX = form.OffsetX;
                    offsetY = form.OffsetY;

                    ed.WriteMessage($"\n\n📋 Thông tin in:");
                    ed.WriteMessage($"\n   - Máy in: {printer}");
                    ed.WriteMessage($"\n   - Kích thước giấy: {paperSize} ({form.PaperWidth}x{form.PaperHeight} mm)");
                    ed.WriteMessage($"\n   - Plot Style: {plotStyle}");
                    ed.WriteMessage($"\n   - Block: {blockName}");
                    ed.WriteMessage($"\n   - Sắp xếp: {sortOrder}");
                    ed.WriteMessage($"\n   - {(isPreview ? "CHẾ ĐỘ PREVIEW" : "CHẾ ĐỘ IN")}");

                    // Lấy các vùng in - ưu tiên từ form (đã chọn), nếu không thì tìm kiếm tự động
                    List<LayoutPrintArea> printAreas;
                    if (form.PrintAreas.Count > 0)
                    {
                        // User đã chọn blocks bằng nút Select
                        printAreas = form.PrintAreas;
                        ed.WriteMessage($"\n📍 Sử dụng {printAreas.Count} block đã chọn.");
                    }
                    else
                    {
                        // Tìm kiếm tự động trong Layout
                        printAreas = FindPrintAreas(db, ed, blockName, sortOrder);
                    }

                    if (printAreas.Count == 0)
                    {
                        ed.WriteMessage($"\n⚠️ Không tìm thấy Block '{blockName}'!");
                        ed.WriteMessage($"\n   Hãy sử dụng nút 'Select' để quét chọn các block cần in.");
                        return;
                    }

                    ed.WriteMessage($"\n\n📍 Tìm thấy {printAreas.Count} vùng in:");
                    foreach (var area in printAreas)
                    {
                        ed.WriteMessage($"\n   {area.Index}. ({area.MinPoint.X:F2}, {area.MinPoint.Y:F2}) -> ({area.MaxPoint.X:F2}, {area.MaxPoint.Y:F2})");
                    }

                    // Thực hiện in
                    if (isPreview)
                    {
                        // Chỉ preview vùng đầu tiên
                        PreviewPlot(ed, printAreas[0], printer, paperSize, plotStyle, centerPlot, offsetX, offsetY);
                    }
                    else
                    {
                        // In tất cả các vùng
                        PlotAllAreas(doc, ed, printAreas, printer, paperSize, plotStyle, centerPlot, offsetX, offsetY);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi: {ex.Message}");
                ed.WriteMessage($"\n   Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Tìm tất cả vùng in trong Layout hiện hành (Block mode)
        /// </summary>
        private static List<LayoutPrintArea> FindPrintAreas(Database db, Editor ed,
            string blockName, LayoutSortOrder sortOrder)
        {
            var areas = new List<LayoutPrintArea>();
            int index = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Lấy Layout hiện hành
                LayoutManager layoutMgr = LayoutManager.Current;
                string currentLayoutName = layoutMgr.CurrentLayout;

                DBDictionary layoutDict = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                Layout currentLayout = tr.GetObject(layoutDict.GetAt(currentLayoutName), OpenMode.ForRead) as Layout;
                BlockTableRecord paperSpace = tr.GetObject(currentLayout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in paperSpace)
                {
                    Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // Tìm block references
                    if (ent is BlockReference blkRef)
                    {
                        BlockTableRecord btr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        if (btr != null && string.Equals(btr.Name, blockName, StringComparison.OrdinalIgnoreCase))
                        {
                            index++;
                            Extents3d ext = blkRef.GeometricExtents;
                            areas.Add(new LayoutPrintArea(index, objId, ext.MinPoint, ext.MaxPoint, btr.Name));
                        }
                    }
                }

                tr.Commit();
            }

            // Sắp xếp theo thứ tự
            switch (sortOrder)
            {
                case LayoutSortOrder.LeftToRight:
                    areas = areas.OrderBy(a => a.MinPoint.X).ThenBy(a => -a.MinPoint.Y).ToList();
                    break;
                case LayoutSortOrder.TopToBottom:
                    areas = areas.OrderByDescending(a => a.MinPoint.Y).ThenBy(a => a.MinPoint.X).ToList();
                    break;
                    // Normal: giữ nguyên thứ tự
            }

            // Đánh số lại sau khi sắp xếp
            for (int i = 0; i < areas.Count; i++)
            {
                areas[i].Index = i + 1;
            }

            return areas;
        }

        /// <summary>
        /// Preview một vùng in
        /// </summary>
        private static void PreviewPlot(Editor ed, LayoutPrintArea area,
            string printer, string paperSize, string plotStyle,
            bool centerPlot, double offsetX, double offsetY)
        {
            ed.WriteMessage($"\n\n🔍 Đang preview vùng in {area.Index}...");

            // Sử dụng lệnh PREVIEW của AutoCAD
            Document doc = Application.DocumentManager.MdiActiveDocument;

            string offsetCmd = centerPlot ? "Center" : $"{offsetX},{offsetY}";

            var cmd = new System.Text.StringBuilder();
            cmd.Append($"(command \"-PLOT\" ");
            cmd.Append("\"Y\" ");  // Detailed plot
            cmd.Append("\"\" ");   // Current layout
            cmd.Append($"\"{printer}\" ");
            cmd.Append($"\"{paperSize}\" ");
            cmd.Append("\"Millimeters\" ");
            cmd.Append("\"Landscape\" ");
            cmd.Append("\"No\" ");  // Not upside down
            cmd.Append("\"Window\" ");
            cmd.Append($"(list {area.MinPoint.X:F4} {area.MinPoint.Y:F4}) ");
            cmd.Append($"(list {area.MaxPoint.X:F4} {area.MaxPoint.Y:F4}) ");
            cmd.Append("\"Fit\" ");  // Scale to fit
            cmd.Append($"\"{offsetCmd}\" ");
            cmd.Append("\"Yes\" ");  // Plot with plot styles
            cmd.Append($"\"{plotStyle}\" ");
            cmd.Append("\"Yes\" ");  // Plot with lineweights
            cmd.Append("\"As displayed\" ");
            cmd.Append("\"No\" ");   // Don't save changes
            cmd.Append("\"No\" ");   // Don't plot to file
            cmd.Append("\"No\" ");   // Don't proceed - just preview
            cmd.AppendLine("(princ))");

            doc.SendStringToExecute(cmd.ToString(), true, false, false);

            ed.WriteMessage("\n💡 Sử dụng Preview để xem trước kết quả in.");
        }

        /// <summary>
        /// In tất cả các vùng sử dụng LISP command
        /// </summary>
        private static void PlotAllAreas(Document doc, Editor ed, List<LayoutPrintArea> areas,
            string printer, string paperSize, string plotStyle,
            bool centerPlot, double offsetX, double offsetY)
        {
            ed.WriteMessage($"\n\n🚀 Đang chuẩn bị in {areas.Count} bản vẽ...");

            string offsetCmd = centerPlot ? "Center" : $"{offsetX},{offsetY}";

            // Xây dựng tất cả lệnh in thành một chuỗi LISP
            var allCommands = new System.Text.StringBuilder();
            allCommands.AppendLine("(progn ");

            foreach (var area in areas)
            {
                ed.WriteMessage($"\n📄 Chuẩn bị vùng in {area.Index}/{areas.Count}: ({area.MinPoint.X:F0},{area.MinPoint.Y:F0}) -> ({area.MaxPoint.X:F0},{area.MaxPoint.Y:F0})");

                // Thêm lệnh -PLOT cho vùng này
                AppendLayoutPlotCommand(allCommands,
                    printer, paperSize,
                    area.MinPoint.X, area.MinPoint.Y,
                    area.MaxPoint.X, area.MaxPoint.Y,
                    plotStyle, offsetCmd);
            }

            allCommands.AppendLine("(princ)) ");

            // Gửi tất cả lệnh một lần
            ed.WriteMessage($"\n\n🚀 Đang gửi {areas.Count} lệnh in đến máy in...");
            doc.SendStringToExecute(allCommands.ToString(), true, false, false);

            ed.WriteMessage($"\n\n🎉 Đã gửi lệnh in cho {areas.Count} bản vẽ!");
            ed.WriteMessage("\n💡 Vui lòng đợi quá trình in hoàn tất.");
        }

        /// <summary>
        /// Thêm lệnh -PLOT cho Layout vào StringBuilder
        /// Tham khảo từ LISP: INLAYOUT_FIX
        /// </summary>
        private static void AppendLayoutPlotCommand(System.Text.StringBuilder sb,
            string printer, string paperSize,
            double p1X, double p1Y, double p2X, double p2Y,
            string plotStyle, string offsetCmd)
        {
            // Sử dụng LISP (command ...) syntax cho Layout
            sb.Append("(command \"-PLOT\" ");
            sb.Append("\"Y\" ");                    // Detailed plot configuration? Yes
            sb.Append("\"\" ");                     // Layout name - rỗng = current layout
            sb.Append($"\"{printer}\" ");          // Printer/plotter name
            sb.Append($"\"{paperSize}\" ");        // Paper size
            sb.Append("\"Millimeters\" ");         // Drawing units
            sb.Append("\"Landscape\" ");           // Orientation
            sb.Append("\"No\" ");                  // Plot upside-down? No
            sb.Append("\"Window\" ");              // Plot area: Window
            sb.Append($"(list {p1X:F4} {p1Y:F4}) ");  // First corner as LISP list
            sb.Append($"(list {p2X:F4} {p2Y:F4}) ");  // Second corner as LISP list
            sb.Append("\"1\" ");                   // Plot scale: 1:1
            sb.Append($"\"{offsetCmd}\" ");        // Plot offset: Center or X,Y
            sb.Append("\"Yes\" ");                 // Plot with plot styles? Yes
            sb.Append($"\"{plotStyle}\" ");        // Plot style table (monochrome.ctb)
            sb.Append("\"Yes\" ");                 // 15. Plot with lineweights? Yes
            sb.Append("\"Yes\" ");                 // 16. Scale lineweights with plot scale? Yes
            sb.Append("\"No\" ");                  // 17. Plot paper space first? No
            sb.Append("\"No\" ");                  // 18. Hide paperspace objects? No
            sb.Append("\"No\" ");                  // 19. Write to file? No
            sb.Append("\"No\" ");                  // 20. Save changes to page setup? No
            sb.Append("\"Yes\"");                  // 21. Proceed with plot? Yes
            sb.AppendLine(") ");                   // Close LISP command
        }
    }
}
