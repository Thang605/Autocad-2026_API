using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using MyFirstProject;
using MyFirstProject.Extensions;

// Explicit aliases for Windows Forms types to avoid conflicts with Civil 3D
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;
using WinFormsSize = System.Drawing.Size;
using WinFormsColor = System.Drawing.Color;
using WinFormsFontStyle = System.Drawing.FontStyle;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsButton = System.Windows.Forms.Button;
using WinFormsPanel = System.Windows.Forms.Panel;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form để cấu hình target cho từng subassembly trong corridor
    /// Cho phép người dùng thiết lập target group và target option cho từng subassembly
    /// </summary>
    public partial class SubassemblyTargetConfigForm : Form
    {
        private readonly SubassemblyTargetInfoCollection _targetInfoCollection;
        private readonly ObjectIdCollection _alignmentTargets;
        private readonly ObjectIdCollection _profileTargets;
        private readonly ObjectIdCollection _surfaceTargets;
        private readonly ObjectIdCollection _polylineTargets;
        private readonly List<TargetConnection> _targetConnections;

        private DataGridView? dgvTargets;
        private WinFormsButton? btnApply;
        private WinFormsButton? btnCancel;
        private WinFormsButton? btnAutoConfig;
        private WinFormsLabel? lblTitle;
        private WinFormsLabel? lblInstructions;
        private WinFormsPanel? panelTop;
        private WinFormsPanel? panelBottom;
        private GroupBox? gbTargetInfo;

        public bool ConfigurationApplied { get; private set; }
        public List<TargetConnection> TargetConnections => _targetConnections;

        public SubassemblyTargetConfigForm(
            SubassemblyTargetInfoCollection targetInfoCollection,
            ObjectIdCollection alignmentTargets,
            ObjectIdCollection profileTargets,
            ObjectIdCollection surfaceTargets,
            ObjectIdCollection polylineTargets)
        {
            _targetInfoCollection = targetInfoCollection ?? throw new ArgumentNullException(nameof(targetInfoCollection));
            _alignmentTargets = alignmentTargets ?? new ObjectIdCollection();
            _profileTargets = profileTargets ?? new ObjectIdCollection();
            _surfaceTargets = surfaceTargets ?? new ObjectIdCollection();
            _polylineTargets = polylineTargets ?? new ObjectIdCollection();
            _targetConnections = new List<TargetConnection>();

            InitializeComponent();
            LoadTargetData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form settings
            this.Text = "Cấu hình Target cho Subassemblies";
            this.Size = new WinFormsSize(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Top Panel
            panelTop = new WinFormsPanel
            {
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(10)
            };

            lblTitle = new WinFormsLabel
            {
                Text = "CẤU HÌNH TARGET CHO SUBASSEMBLIES",
                Font = new WinFormsFont("Segoe UI", 14, WinFormsFontStyle.Bold),
                ForeColor = WinFormsColor.FromArgb(0, 102, 204),
                AutoSize = true,
                Location = new WinFormsPoint(10, 10)
            };

            lblInstructions = new WinFormsLabel
            {
                Text = "Chọn Target Group và Target Option cho từng Subassembly.\nMỗi subassembly cần ít nhất 2 targets để hoạt động đúng.",
                Font = new WinFormsFont("Segoe UI", 9),
                AutoSize = true,
                Location = new WinFormsPoint(10, 45),
                ForeColor = WinFormsColor.DarkSlateGray
            };

            panelTop.Controls.Add(lblTitle);
            panelTop.Controls.Add(lblInstructions);

            // GroupBox for target info
            gbTargetInfo = new GroupBox
            {
                Text = "Thông tin Targets khả dụng",
                Location = new WinFormsPoint(10, 80),
                Size = new WinFormsSize(960, 35),
                Font = new WinFormsFont("Segoe UI", 9, WinFormsFontStyle.Bold)
            };

            WinFormsLabel lblTargetInfo = new WinFormsLabel
            {
                Text = $"Alignments: {_alignmentTargets.Count} | Profiles: {_profileTargets.Count} | Surfaces: {_surfaceTargets.Count} | Polylines: {_polylineTargets.Count}",
                AutoSize = true,
                Location = new WinFormsPoint(10, 15),
                Font = new WinFormsFont("Segoe UI", 9),
                ForeColor = WinFormsColor.DarkGreen
            };

            gbTargetInfo.Controls.Add(lblTargetInfo);
            panelTop.Controls.Add(gbTargetInfo);

            // DataGridView
            dgvTargets = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = true,
                BackgroundColor = WinFormsColor.White,
                BorderStyle = BorderStyle.Fixed3D,
                Font = new WinFormsFont("Segoe UI", 9)
            };

            // Configure columns
            dgvTargets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Index",
                HeaderText = "Chỉ số",
                ReadOnly = true,
                FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvTargets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SubassemblyName",
                HeaderText = "Tên Subassembly",
                ReadOnly = true,
                FillWeight = 25
            });

            dgvTargets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TargetType",
                HeaderText = "Loại Target",
                ReadOnly = true,
                FillWeight = 25
            });

            DataGridViewComboBoxColumn targetGroupColumn = new DataGridViewComboBoxColumn
            {
                Name = "TargetGroup",
                HeaderText = "Target Group",
                FillWeight = 20,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };

            dgvTargets.Columns.Add(targetGroupColumn);

            DataGridViewComboBoxColumn targetOptionColumn = new DataGridViewComboBoxColumn
            {
                Name = "TargetOption",
                HeaderText = "Tùy chọn Target",
                FillWeight = 20,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };

            dgvTargets.Columns.Add(targetOptionColumn);

            // Alternating row colors
            dgvTargets.AlternatingRowsDefaultCellStyle.BackColor = WinFormsColor.FromArgb(240, 248, 255);
            dgvTargets.RowsDefaultCellStyle.SelectionBackColor = WinFormsColor.FromArgb(51, 153, 255);
            dgvTargets.RowsDefaultCellStyle.SelectionForeColor = WinFormsColor.White;

            // Bottom Panel
            panelBottom = new WinFormsPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10)
            };

            btnAutoConfig = new WinFormsButton
            {
                Text = "Tự động cấu hình",
                Size = new WinFormsSize(150, 35),
                Location = new WinFormsPoint(10, 12),
                Font = new WinFormsFont("Segoe UI", 9, WinFormsFontStyle.Bold),
                BackColor = WinFormsColor.FromArgb(255, 193, 7),
                ForeColor = WinFormsColor.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAutoConfig.FlatAppearance.BorderSize = 0;
            btnAutoConfig.Click += BtnAutoConfig_Click;

            btnApply = new WinFormsButton
            {
                Text = "Áp dụng",
                Size = new WinFormsSize(120, 35),
                Location = new WinFormsPoint(750, 12),
                Font = new WinFormsFont("Segoe UI", 9, WinFormsFontStyle.Bold),
                BackColor = WinFormsColor.FromArgb(76, 175, 80),
                ForeColor = WinFormsColor.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApply_Click;

            btnCancel = new WinFormsButton
            {
                Text = "Hủy",
                Size = new WinFormsSize(120, 35),
                Location = new WinFormsPoint(880, 12),
                Font = new WinFormsFont("Segoe UI", 9, WinFormsFontStyle.Bold),
                BackColor = WinFormsColor.FromArgb(244, 67, 54),
                ForeColor = WinFormsColor.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += BtnCancel_Click;

            panelBottom.Controls.Add(btnAutoConfig);
            panelBottom.Controls.Add(btnApply);
            panelBottom.Controls.Add(btnCancel);

            // Add controls to form
            this.Controls.Add(dgvTargets);
            this.Controls.Add(panelTop);
            this.Controls.Add(panelBottom);

            this.ResumeLayout(false);
        }

        private void LoadTargetData()
        {
            if (dgvTargets == null) return;

            try
            {
                dgvTargets.Rows.Clear();

                using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                {
                    for (int i = 0; i < _targetInfoCollection.Count; i++)
                    {
                        var targetInfo = _targetInfoCollection[i];

                        // Get subassembly name
                        string subassemblyName = GetSubassemblyTargetName(targetInfo, i);

                        // Get target type description
                        string targetTypeDesc = GetTargetTypeDescription(targetInfo);

                        // Add row
                        int rowIndex = dgvTargets.Rows.Add();
                        DataGridViewRow row = dgvTargets.Rows[rowIndex];

                        row.Cells["Index"].Value = i + 1;
                        row.Cells["SubassemblyName"].Value = subassemblyName;
                        row.Cells["TargetType"].Value = targetTypeDesc;

                        // Setup Target Group ComboBox
                        DataGridViewComboBoxCell targetGroupCell = (DataGridViewComboBoxCell)row.Cells["TargetGroup"];
                        targetGroupCell.DataSource = GetTargetGroupItems();
                        targetGroupCell.DisplayMember = "DisplayName";
                        targetGroupCell.ValueMember = "GroupId";

                        // Auto-select appropriate target group based on type
                        int suggestedGroupId = SuggestTargetGroup(targetInfo);
                        targetGroupCell.Value = suggestedGroupId >= 0 ? suggestedGroupId : -1;

                        // Setup Target Option ComboBox
                        DataGridViewComboBoxCell targetOptionCell = (DataGridViewComboBoxCell)row.Cells["TargetOption"];
                        targetOptionCell.DataSource = GetTargetOptionItems();
                        targetOptionCell.DisplayMember = "DisplayName";
                        targetOptionCell.ValueMember = "OptionValue";
                        targetOptionCell.Value = (int)SubassemblyTargetToOption.Nearest; // Default

                        // Store original targetInfo reference
                        row.Tag = targetInfo;
                    }

                    tr.Commit();
                }

                A.Ed.WriteMessage($"\n✅ Đã tải {dgvTargets.Rows.Count} subassembly targets vào form cấu hình.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                A.Ed.WriteMessage($"\n❌ Lỗi LoadTargetData: {ex.Message}");
            }
        }

        private string GetSubassemblyTargetName(SubassemblyTargetInfo targetInfo, int index)
        {
            try
            {
                // Try to access SubassemblyName property directly
                string subassemblyName = targetInfo.SubassemblyName;
                if (!string.IsNullOrWhiteSpace(subassemblyName))
                {
                    return $"{subassemblyName} (Target_{index})";
                }
            }
            catch { }

            return $"SubassemblyTarget_{index}";
        }

        private string GetTargetTypeDescription(SubassemblyTargetInfo targetInfo)
        {
            try
            {
                string targetType = targetInfo.TargetType.ToString();
                int targetCount = targetInfo.TargetIds?.Count ?? 0;
                return $"{targetType} ({targetCount} đối tượng)";
            }
            catch
            {
                int targetCount = targetInfo.TargetIds?.Count ?? 0;
                return $"Unknown ({targetCount} đối tượng)";
            }
        }

        private int SuggestTargetGroup(SubassemblyTargetInfo targetInfo)
        {
            try
            {
                string targetType = targetInfo.TargetType.ToString();

                if (targetType.Contains("Elevation") && _profileTargets.Count > 0) return 1;
                if (targetType.Contains("Offset") && _alignmentTargets.Count > 0) return 0;
                if (targetType.Contains("Surface") && _surfaceTargets.Count > 0) return 2;
                if (_polylineTargets.Count > 0) return 3;
                if (_alignmentTargets.Count > 0) return 0;
            }
            catch { }

            return -1;
        }

        private List<TargetGroupItem> GetTargetGroupItems()
        {
            return new List<TargetGroupItem>
            {
                new TargetGroupItem { GroupId = -1, DisplayName = "⊘ Không gắn kết" },
                new TargetGroupItem { GroupId = 0, DisplayName = $"⬌ Alignments ({_alignmentTargets.Count})" },
                new TargetGroupItem { GroupId = 1, DisplayName = $"⬍ Profiles ({_profileTargets.Count})" },
                new TargetGroupItem { GroupId = 2, DisplayName = $"⛰ Surfaces ({_surfaceTargets.Count})" },
                new TargetGroupItem { GroupId = 3, DisplayName = $"⬚ Polylines/Other ({_polylineTargets.Count})" }
            };
        }

        private List<TargetOptionItem> GetTargetOptionItems()
        {
            return new List<TargetOptionItem>
            {
                new TargetOptionItem { OptionValue = (int)SubassemblyTargetToOption.Nearest, DisplayName = "Nearest (Gần nhất)" }
            };
        }

        private void BtnAutoConfig_Click(object? sender, EventArgs e)
        {
            if (dgvTargets == null) return;

            try
            {
                A.Ed.WriteMessage("\n=== Tự động cấu hình targets ===");

                foreach (DataGridViewRow row in dgvTargets.Rows)
                {
                    if (row.Tag is SubassemblyTargetInfo targetInfo)
                    {
                        int suggestedGroupId = SuggestTargetGroup(targetInfo);

                        DataGridViewComboBoxCell targetGroupCell = (DataGridViewComboBoxCell)row.Cells["TargetGroup"];
                        targetGroupCell.Value = suggestedGroupId;

                        DataGridViewComboBoxCell targetOptionCell = (DataGridViewComboBoxCell)row.Cells["TargetOption"];
                        targetOptionCell.Value = (int)SubassemblyTargetToOption.Nearest;

                        A.Ed.WriteMessage($"\nTarget {row.Index + 1}: GroupId={suggestedGroupId}");
                    }
                }

                dgvTargets.Refresh();
                MessageBox.Show("Đã tự động cấu hình targets dựa trên loại subassembly.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tự động cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                A.Ed.WriteMessage($"\n❌ Lỗi BtnAutoConfig_Click: {ex.Message}");
            }
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            if (dgvTargets == null) return;

            try
            {
                _targetConnections.Clear();
                A.Ed.WriteMessage("\n=== Áp dụng cấu hình Target Connections ===");

                foreach (DataGridViewRow row in dgvTargets.Rows)
                {
                    if (row.Tag is SubassemblyTargetInfo targetInfo)
                    {
                        DataGridViewComboBoxCell targetGroupCell = (DataGridViewComboBoxCell)row.Cells["TargetGroup"];
                        DataGridViewComboBoxCell targetOptionCell = (DataGridViewComboBoxCell)row.Cells["TargetOption"];

                        int targetGroupId = -1;
                        int targetOption = (int)SubassemblyTargetToOption.Nearest;

                        if (targetGroupCell.Value != null && int.TryParse(targetGroupCell.Value.ToString(), out int groupId))
                        {
                            targetGroupId = groupId;
                        }

                        if (targetOptionCell.Value != null && int.TryParse(targetOptionCell.Value.ToString(), out int option))
                        {
                            targetOption = option;
                        }

                        var connection = new TargetConnection
                        {
                            SubassemblyIndex = row.Index,
                            TargetInfo = targetInfo,
                            TargetGroupId = targetGroupId,
                            TargetOption = (SubassemblyTargetToOption)targetOption
                        };

                        _targetConnections.Add(connection);
                        A.Ed.WriteMessage($"\nTarget {row.Index}: GroupId={targetGroupId}, Option={connection.TargetOption}");
                    }
                }

                ConfigurationApplied = true;
                A.Ed.WriteMessage($"\n✅ Đã thu thập {_targetConnections.Count} target connections.");

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi áp dụng cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                A.Ed.WriteMessage($"\n❌ Lỗi BtnApply_Click: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            ConfigurationApplied = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Apply the target connections to the baseline region
        /// DEPRECATED: Use ApplyTargetConfigurationFromForm in the main command instead
        /// This method is kept for backward compatibility with test commands
        /// </summary>
        [Obsolete("This method should not be called directly. The configuration should be applied in the transaction context where BaselineRegion was created.")]
        public void ApplyTargetConnectionsToBaselineRegion(BaselineRegion baselineRegion)
        {
            if (_targetConnections.Count == 0)
            {
                A.Ed.WriteMessage("\n⚠️ Không có target connections để áp dụng.");
                return;
            }

            try
            {
                A.Ed.WriteMessage("\n=== [DEPRECATED METHOD] Áp dụng cấu hình Target Connections ===");
                A.Ed.WriteMessage($"\n⚠️ Cảnh báo: Method này sẽ bị xóa trong phiên bản tương lai.");
                A.Ed.WriteMessage($"\nSố lượng subassembly targets: {_targetInfoCollection.Count}");
                A.Ed.WriteMessage($"\nSố lượng connections cấu hình: {_targetConnections.Count}");

                // Get fresh targets from baseline region
                var targetInfoCollection = baselineRegion.GetTargets();
                int successCount = 0;

                for (int i = 0; i < _targetConnections.Count; i++)
                {
                    var connection = _targetConnections[i];

                    try
                    {
                        if (connection.SubassemblyIndex < 0 || connection.SubassemblyIndex >= targetInfoCollection.Count)
                        {
                            A.Ed.WriteMessage($"\n⚠️ Target index {connection.SubassemblyIndex} ngoài phạm vi.");
                            continue;
                        }

                        var targetInfo = targetInfoCollection[connection.SubassemblyIndex];
                        A.Ed.WriteMessage($"\n\nTarget {connection.SubassemblyIndex} ({GetSubassemblyTargetName(connection.TargetInfo, connection.SubassemblyIndex)}):");

                        // Get appropriate target collection
                        ObjectIdCollection? selectedTargets = GetTargetCollectionByGroupId(connection.TargetGroupId);

                        if (selectedTargets == null || selectedTargets.Count == 0)
                        {
                            A.Ed.WriteMessage($"\n  ⚠️ Không gắn kết (no targets khả dụng)");
                            continue;
                        }

                        string groupName = GetGroupNameByGroupId(connection.TargetGroupId);
                        A.Ed.WriteMessage($"\n  - Gắn kết với: {groupName} ({selectedTargets.Count} đối tượng)");

                        // Create NEW ObjectIdCollection instead of modifying existing one
                        ObjectIdCollection newTargetIds = new ObjectIdCollection();

                        // Add new targets
                        if (selectedTargets.Count >= 2)
                        {
                            newTargetIds.Add(selectedTargets[0]);
                            newTargetIds.Add(selectedTargets[1]);
                        }
                        else if (selectedTargets.Count == 1)
                        {
                            newTargetIds.Add(selectedTargets[0]);
                            newTargetIds.Add(selectedTargets[0]); // Duplicate
                        }

                        // Assign the NEW collection to TargetIds
                        targetInfo.TargetIds = newTargetIds;

                        // Set target option
                        targetInfo.TargetToOption = connection.TargetOption;
                        A.Ed.WriteMessage($"\n  - Tùy chọn: {connection.TargetOption}");

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        A.Ed.WriteMessage($"\n  ❌ Lỗi khi áp dụng target {connection.SubassemblyIndex}: {ex.Message}");
                    }
                }

                // Apply to baseline region
                A.Ed.WriteMessage($"\n\n--- Gọi SetTargets() trên baseline region ---");
                baselineRegion.SetTargets(targetInfoCollection);

                A.Ed.WriteMessage($"\n\n✅ Đã áp dụng cấu hình target cho {successCount}/{_targetConnections.Count} subassembly targets.");
            }
            catch (Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi khi áp dụng target connections: {ex.Message}");
                throw;
            }
        }

        private ObjectIdCollection? GetTargetCollectionByGroupId(int groupId)
        {
            return groupId switch
            {
                0 => _alignmentTargets,
                1 => _profileTargets,
                2 => _surfaceTargets,
                3 => _polylineTargets,
                _ => null
            };
        }

        private string GetGroupNameByGroupId(int groupId)
        {
            return groupId switch
            {
                0 => "Alignments",
                1 => "Profiles",
                2 => "Surfaces",
                3 => "Polylines",
                _ => "Không gắn kết"
            };
        }
    }

    /// <summary>
    /// Class to store target connection configuration
    /// </summary>
    public class TargetConnection
    {
        public int SubassemblyIndex { get; set; }
        public SubassemblyTargetInfo? TargetInfo { get; set; }
        public int TargetGroupId { get; set; }
        public SubassemblyTargetToOption TargetOption { get; set; }
    }

    /// <summary>
    /// Class for Target Group ComboBox items
    /// </summary>
    public class TargetGroupItem
    {
        public int GroupId { get; set; }
        public string? DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName ?? "";
        }
    }

    /// <summary>
    /// Class for Target Option ComboBox items
    /// </summary>
    public class TargetOptionItem
    {
        public int OptionValue { get; set; }
        public string? DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName ?? "";
        }
    }
}
