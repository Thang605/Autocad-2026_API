using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject
{
    public class DieuChinhDuongEGForm : Form
    {
        // ===== Public properties for results =====
        public ObjectId SelectedTargetSurfaceId { get; private set; } = ObjectId.Null;
        public bool ProcessAdjacentSections { get; private set; } = true;

        // ===== Private fields =====
        private List<SurfaceItem> surfaceItems = new();
        private ObjectId sourceSurfaceId = ObjectId.Null;

        // ===== UI Controls =====
        private WinFormsLabel lblTitle = null!;
        private GroupBox grpInfo = null!;
        private WinFormsLabel lblAlignment = null!;
        private WinFormsLabel lblAlignmentValue = null!;
        private WinFormsLabel lblSampleLine = null!;
        private WinFormsLabel lblSampleLineValue = null!;
        private WinFormsLabel lblStation = null!;
        private WinFormsLabel lblStationValue = null!;
        private WinFormsLabel lblSourceSurface = null!;
        private WinFormsLabel lblSourceSurfaceValue = null!;
        private WinFormsLabel lblPrevStation = null!;
        private WinFormsLabel lblPrevStationValue = null!;
        private WinFormsLabel lblNextStation = null!;
        private WinFormsLabel lblNextStationValue = null!;

        private GroupBox grpSettings = null!;
        private WinFormsLabel lblTargetSurface = null!;
        private ComboBox cboTargetSurface = null!;
        private CheckBox chkProcessAdjacent = null!;
        private WinFormsLabel lblAdjacentNote = null!;

        private Panel panelButtons = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;

        private class SurfaceItem
        {
            public string Name { get; set; } = "";
            public ObjectId Id { get; set; }
            public override string ToString() => Name;
        }

        public DieuChinhDuongEGForm()
        {
            InitializeComponent();
            LoadSurfaces();
        }

        /// <summary>
        /// Set detected context information to display on the form
        /// </summary>
        public void SetContextInfo(
            string alignmentName,
            string sampleLineName,
            double station,
            string sourceSurfaceName,
            ObjectId sourceSurfaceId,
            double? previousStation,
            double? nextStation)
        {
            lblAlignmentValue.Text = alignmentName;
            lblSampleLineValue.Text = sampleLineName;
            lblStationValue.Text = $"{station:F3}";
            lblSourceSurfaceValue.Text = string.IsNullOrEmpty(sourceSurfaceName) ? "(không xác định)" : sourceSurfaceName;
            this.sourceSurfaceId = sourceSurfaceId;

            lblPrevStationValue.Text = previousStation.HasValue ? $"{previousStation.Value:F3}" : "(không có)";
            lblNextStationValue.Text = nextStation.HasValue ? $"{nextStation.Value:F3}" : "(không có)";

            // Color code station availability
            lblPrevStationValue.ForeColor = previousStation.HasValue ? Color.DarkGreen : Color.Gray;
            lblNextStationValue.ForeColor = nextStation.HasValue ? Color.DarkGreen : Color.Gray;

            // Auto-select source surface in dropdown if available
            if (sourceSurfaceId != ObjectId.Null)
            {
                for (int i = 0; i < cboTargetSurface.Items.Count; i++)
                {
                    if (((SurfaceItem)cboTargetSurface.Items[i]).Id == sourceSurfaceId)
                    {
                        cboTargetSurface.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void LoadSurfaces()
        {
            surfaceItems.Clear();
            cboTargetSurface.Items.Clear();

            try
            {
                var civilDoc = CivilApplication.ActiveDocument;
                var surfaceIds = civilDoc.GetSurfaceIds();

                using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                    .MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (ObjectId surfId in surfaceIds)
                    {
                        try
                        {
                            if (tr.GetObject(surfId, OpenMode.ForRead) is CivSurface tinSurf)
                            {
                                var item = new SurfaceItem { Name = tinSurf.Name, Id = surfId };
                                surfaceItems.Add(item);
                                cboTargetSurface.Items.Add(item);
                            }
                        }
                        catch { continue; }
                    }
                    tr.Commit();
                }

                if (cboTargetSurface.Items.Count > 0)
                    cboTargetSurface.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách surface: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (cboTargetSurface.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn surface đích.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedTargetSurfaceId = ((SurfaceItem)cboTargetSurface.SelectedItem).Id;
            ProcessAdjacentSections = chkProcessAdjacent.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void InitializeComponent()
        {
            // ===== Form settings =====
            this.Text = "Điều chỉnh đường EG";
            this.ClientSize = new Size(500, 430);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new WinFormsFont("Segoe UI", 9F);
            this.BackColor = Color.White;

            int leftMargin = 16;
            int rightMargin = 16;
            int contentWidth = this.ClientSize.Width - leftMargin - rightMargin;

            // ===== Title =====
            lblTitle = new WinFormsLabel
            {
                Text = "⚙ Điều chỉnh đường EG (Existing Ground)",
                Font = new WinFormsFont("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 80, 160),
                Location = new WinFormsPoint(leftMargin, 12),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // ===== Info GroupBox =====
            grpInfo = new GroupBox
            {
                Text = "Thông tin cọc đã chọn",
                Font = new WinFormsFont("Segoe UI", 9F, FontStyle.Bold),
                Location = new WinFormsPoint(leftMargin, 46),
                Size = new Size(contentWidth, 180)
            };
            this.Controls.Add(grpInfo);

            int infoLabelX = 14;
            int infoValueX = 140;
            int infoY = 24;
            int infoRowHeight = 24;
            WinFormsFont infoFont = new WinFormsFont("Segoe UI", 9F, FontStyle.Regular);
            WinFormsFont infoValueFont = new WinFormsFont("Segoe UI", 9F, FontStyle.Bold);

            // Alignment
            lblAlignment = new WinFormsLabel { Text = "Tuyến (Alignment):", Location = new WinFormsPoint(infoLabelX, infoY), AutoSize = true, Font = infoFont };
            lblAlignmentValue = new WinFormsLabel { Text = "—", Location = new WinFormsPoint(infoValueX, infoY), AutoSize = true, Font = infoValueFont, ForeColor = Color.FromArgb(0, 100, 180) };
            grpInfo.Controls.AddRange(new Control[] { lblAlignment, lblAlignmentValue });
            infoY += infoRowHeight;

            // Sample Line
            lblSampleLine = new WinFormsLabel { Text = "Sample Line:", Location = new WinFormsPoint(infoLabelX, infoY), AutoSize = true, Font = infoFont };
            lblSampleLineValue = new WinFormsLabel { Text = "—", Location = new WinFormsPoint(infoValueX, infoY), AutoSize = true, Font = infoValueFont, ForeColor = Color.FromArgb(0, 100, 180) };
            grpInfo.Controls.AddRange(new Control[] { lblSampleLine, lblSampleLineValue });
            infoY += infoRowHeight;

            // Station
            lblStation = new WinFormsLabel { Text = "Station:", Location = new WinFormsPoint(infoLabelX, infoY), AutoSize = true, Font = infoFont };
            lblStationValue = new WinFormsLabel { Text = "—", Location = new WinFormsPoint(infoValueX, infoY), AutoSize = true, Font = infoValueFont, ForeColor = Color.FromArgb(0, 100, 180) };
            grpInfo.Controls.AddRange(new Control[] { lblStation, lblStationValue });
            infoY += infoRowHeight;

            // Source Surface
            lblSourceSurface = new WinFormsLabel { Text = "Surface nguồn:", Location = new WinFormsPoint(infoLabelX, infoY), AutoSize = true, Font = infoFont };
            lblSourceSurfaceValue = new WinFormsLabel { Text = "—", Location = new WinFormsPoint(infoValueX, infoY), AutoSize = true, Font = infoValueFont, ForeColor = Color.FromArgb(0, 130, 60) };
            grpInfo.Controls.AddRange(new Control[] { lblSourceSurface, lblSourceSurfaceValue });
            infoY += infoRowHeight + 4;

            // Separator line
            var separator = new WinFormsLabel { BorderStyle = BorderStyle.Fixed3D, Location = new WinFormsPoint(infoLabelX, infoY), Size = new Size(contentWidth - 30, 2) };
            grpInfo.Controls.Add(separator);
            infoY += 8;

            // Previous station
            lblPrevStation = new WinFormsLabel { Text = "Station trước:", Location = new WinFormsPoint(infoLabelX, infoY), AutoSize = true, Font = infoFont };
            lblPrevStationValue = new WinFormsLabel { Text = "—", Location = new WinFormsPoint(infoValueX, infoY), AutoSize = true, Font = infoFont };
            grpInfo.Controls.AddRange(new Control[] { lblPrevStation, lblPrevStationValue });
            infoY += infoRowHeight;

            // Next station
            lblNextStation = new WinFormsLabel { Text = "Station sau:", Location = new WinFormsPoint(infoLabelX, infoY), AutoSize = true, Font = infoFont };
            lblNextStationValue = new WinFormsLabel { Text = "—", Location = new WinFormsPoint(infoValueX, infoY), AutoSize = true, Font = infoFont };
            grpInfo.Controls.AddRange(new Control[] { lblNextStation, lblNextStationValue });

            // ===== Settings GroupBox =====
            grpSettings = new GroupBox
            {
                Text = "Thiết lập",
                Font = new WinFormsFont("Segoe UI", 9F, FontStyle.Bold),
                Location = new WinFormsPoint(leftMargin, 236),
                Size = new Size(contentWidth, 130)
            };
            this.Controls.Add(grpSettings);

            WinFormsFont settingsFont = new WinFormsFont("Segoe UI", 9F, FontStyle.Regular);

            lblTargetSurface = new WinFormsLabel
            {
                Text = "Surface đích (thêm breakline vào):",
                Location = new WinFormsPoint(14, 26),
                AutoSize = true,
                Font = settingsFont
            };
            grpSettings.Controls.Add(lblTargetSurface);

            cboTargetSurface = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new WinFormsPoint(14, 48),
                Size = new Size(contentWidth - 30, 24),
                Font = settingsFont
            };
            grpSettings.Controls.Add(cboTargetSurface);

            chkProcessAdjacent = new CheckBox
            {
                Text = "Xử lý section lân cận (trước && sau) thành Feature Line",
                Location = new WinFormsPoint(14, 82),
                AutoSize = true,
                Checked = true,
                Font = settingsFont
            };
            grpSettings.Controls.Add(chkProcessAdjacent);

            lblAdjacentNote = new WinFormsLabel
            {
                Text = "Tự động tạo Feature Line từ section trước và sau để surface nội suy mượt hơn.",
                Location = new WinFormsPoint(32, 103),
                Size = new Size(contentWidth - 50, 18),
                ForeColor = Color.Gray,
                Font = new WinFormsFont("Segoe UI", 8F, FontStyle.Italic)
            };
            grpSettings.Controls.Add(lblAdjacentNote);

            // ===== Button panel =====
            panelButtons = new Panel
            {
                Location = new WinFormsPoint(0, this.ClientSize.Height - 50),
                Size = new Size(this.ClientSize.Width, 50),
                BackColor = Color.FromArgb(245, 245, 248)
            };
            this.Controls.Add(panelButtons);

            // Separator above buttons
            var btnSeparator = new WinFormsLabel
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new WinFormsPoint(0, 0),
                Size = new Size(this.ClientSize.Width, 2)
            };
            panelButtons.Controls.Add(btnSeparator);

            btnOK = new Button
            {
                Text = "Thực hiện",
                Size = new Size(100, 32),
                Location = new WinFormsPoint(this.ClientSize.Width - 230, 10),
                Font = new WinFormsFont("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 120, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;
            panelButtons.Controls.Add(btnOK);

            btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(90, 32),
                Location = new WinFormsPoint(this.ClientSize.Width - 120, 10),
                Font = new WinFormsFont("Segoe UI", 9F),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnCancel.Click += BtnCancel_Click;
            panelButtons.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}
