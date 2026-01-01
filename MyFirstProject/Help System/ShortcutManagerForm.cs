// (C) Copyright 2024 by T27
// Form quản lý lệnh tắt - cho phép gán shortcuts và xuất file LSP

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Civil3DCsharp.HelpSystem
{
    /// <summary>
    /// Form quản lý và gán lệnh tắt cho các lệnh AutoCAD/Civil3D
    /// </summary>
    public class ShortcutManagerForm : Form
    {
        private TabControl tabControl;
        private TextBox txtSearch;
        private Label lblStatus;
        private Button btnLoadConfig;
        private Button btnSaveConfig;
        private Button btnExportLsp;
        private Button btnReset;
        private Button btnClose;
        private RichTextBox rtbPreview;

        private ShortcutConfig _config;
        private Dictionary<string, List<CommandShortcutInfo>> _groupedShortcuts;
        private string _lastConfigPath;

        public ShortcutManagerForm()
        {
            InitializeComponent();
            LoadCommands();
        }

        private void InitializeComponent()
        {
            this.Text = "T27 Tools - Quan Ly Lenh Tat (Shortcut Manager)";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(900, 600);
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.FromArgb(245, 245, 250);

            // ========== TOP PANEL: Search + Load ==========
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(50, 50, 60),
                Padding = new Padding(10)
            };

            Label lblTitle = new Label
            {
                Text = "🔧 QUAN LY LENH TAT",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(15, 18)
            };

            Label lblSearch = new Label
            {
                Text = "Tim kiem:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(250, 20)
            };

            txtSearch = new TextBox
            {
                Location = new Point(330, 17),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10F)
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            btnLoadConfig = new Button
            {
                Text = "📂 Load cau hinh cu",
                Size = new Size(160, 32),
                Location = new Point(600, 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnLoadConfig.Click += BtnLoadConfig_Click;

            topPanel.Controls.AddRange(new Control[] { lblTitle, lblSearch, txtSearch, btnLoadConfig });
            this.Controls.Add(topPanel);

            // ========== BOTTOM PANEL: Buttons + Status ==========
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(240, 240, 245),
                Padding = new Padding(10)
            };

            lblStatus = new Label
            {
                Text = "San sang",
                AutoSize = true,
                Location = new Point(15, 18),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            btnSaveConfig = new Button
            {
                Text = "💾 Luu cau hinh JSON",
                Size = new Size(160, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 150, 80),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSaveConfig.Click += BtnSaveConfig_Click;

            btnExportLsp = new Button
            {
                Text = "📄 Xuat file LSP",
                Size = new Size(140, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 100, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnExportLsp.Click += BtnExportLsp_Click;

            btnReset = new Button
            {
                Text = "🔄 Reset",
                Size = new Size(90, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(150, 80, 80),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnReset.Click += BtnReset_Click;

            btnClose = new Button
            {
                Text = "Dong",
                Size = new Size(90, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => this.Close();

            bottomPanel.Controls.AddRange(new Control[] { lblStatus, btnSaveConfig, btnExportLsp, btnReset, btnClose });
            this.Controls.Add(bottomPanel);

            // ========== RIGHT PANEL: Preview ==========
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 350,
                BackColor = Color.FromArgb(40, 40, 50),
                Padding = new Padding(10)
            };

            Label lblPreview = new Label
            {
                Text = "📋 XEM TRUOC FILE LSP",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 30
            };

            rtbPreview = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 40),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.None
            };

            rightPanel.Controls.Add(rtbPreview);
            rightPanel.Controls.Add(lblPreview);
            this.Controls.Add(rightPanel);

            // ========== CENTER: Tab Control ==========
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                Padding = new Point(10, 5)
            };

            this.Controls.Add(tabControl);

            // Resize handler
            this.Resize += (s, e) => PositionButtons();
            this.Load += (s, e) => PositionButtons();
        }

        private void PositionButtons()
        {
            int y = 10;
            btnClose.Location = new Point(this.ClientSize.Width - 360 - btnClose.Width, y);
            btnReset.Location = new Point(btnClose.Left - btnReset.Width - 10, y);
            btnExportLsp.Location = new Point(btnReset.Left - btnExportLsp.Width - 10, y);
            btnSaveConfig.Location = new Point(btnExportLsp.Left - btnSaveConfig.Width - 10, y);
        }

        private void LoadCommands()
        {
            try
            {
                _config = new ShortcutConfig();
                var allCommands = HelpSystem.GetAllCommands().ToList();
                _config.MergeWithNewCommands(allCommands);

                // Group by category
                _groupedShortcuts = _config.Shortcuts
                    .GroupBy(s => GetMainCategory(s.Category))
                    .OrderBy(g => GetCategoryOrder(g.Key))
                    .ToDictionary(g => g.Key, g => g.OrderBy(c => c.OriginalCommand).ToList());

                CreateTabs();
                UpdateStatus();
                UpdatePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách lệnh: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetMainCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return "Khác";

            if (category.Contains(" - "))
                return category.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();

            return category;
        }

        private int GetCategoryOrder(string category)
        {
            switch (category.ToUpper())
            {
                case "CAD": return 1;
                case "CIVIL": return 2;
                case "MENU": return 3;
                case "HELP": return 4;
                default: return 99;
            }
        }

        private void CreateTabs()
        {
            tabControl.TabPages.Clear();

            // Tab "Tất cả"
            TabPage tabAll = new TabPage("📋 Tat ca");
            tabAll.Tag = "ALL";
            ListView lvAll = CreateListView();
            foreach (var shortcut in _config.Shortcuts.OrderBy(s => s.OriginalCommand))
            {
                AddShortcutToListView(lvAll, shortcut);
            }
            tabAll.Controls.Add(lvAll);
            tabControl.TabPages.Add(tabAll);

            // Tabs for each category
            foreach (var group in _groupedShortcuts)
            {
                string icon = GetCategoryIcon(group.Key);
                TabPage tab = new TabPage($"{icon} {group.Key} ({group.Value.Count})");
                tab.Tag = group.Key;

                ListView lv = CreateListView();
                foreach (var shortcut in group.Value)
                {
                    AddShortcutToListView(lv, shortcut);
                }
                tab.Controls.Add(lv);
                tabControl.TabPages.Add(tab);
            }
        }

        private string GetCategoryIcon(string category)
        {
            switch (category.ToUpper())
            {
                case "CAD": return "📐";
                case "CIVIL": return "🛤️";
                case "MENU": return "📁";
                case "HELP": return "❓";
                default: return "📌";
            }
        }

        private ListView CreateListView()
        {
            ListView lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.White,
                LabelEdit = false
            };

            lv.Columns.Add("Tên lệnh gốc", 250);
            lv.Columns.Add("Lệnh tắt", 100);
            lv.Columns.Add("Nhóm", 150);
            lv.Columns.Add("Mô tả", 300);

            // Double-click to edit shortcut
            lv.DoubleClick += ListView_DoubleClick;

            return lv;
        }

        private void AddShortcutToListView(ListView lv, CommandShortcutInfo shortcut)
        {
            ListViewItem item = new ListViewItem(shortcut.OriginalCommand);
            item.SubItems.Add(shortcut.Shortcut ?? "");
            item.SubItems.Add(shortcut.Category ?? "");
            item.SubItems.Add(shortcut.Description ?? "");
            item.Tag = shortcut;

            // Highlight if has shortcut
            if (!string.IsNullOrWhiteSpace(shortcut.Shortcut))
            {
                item.BackColor = Color.FromArgb(230, 255, 230);
            }
            else if (lv.Items.Count % 2 == 1)
            {
                item.BackColor = Color.FromArgb(248, 248, 255);
            }

            lv.Items.Add(item);
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            ListView lv = sender as ListView;
            if (lv?.SelectedItems.Count > 0)
            {
                var item = lv.SelectedItems[0];
                var shortcut = item.Tag as CommandShortcutInfo;
                if (shortcut != null)
                {
                    EditShortcut(shortcut, item);
                }
            }
        }

        private void EditShortcut(CommandShortcutInfo shortcut, ListViewItem item)
        {
            using (var form = new Form())
            {
                form.Text = "Đặt lệnh tắt";
                form.Size = new Size(400, 180);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lblCommand = new Label
                {
                    Text = $"Lệnh: {shortcut.OriginalCommand}",
                    Location = new Point(20, 20),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };

                var lblShortcut = new Label
                {
                    Text = "Lệnh tắt:",
                    Location = new Point(20, 60),
                    AutoSize = true
                };

                var txtShortcut = new TextBox
                {
                    Text = shortcut.Shortcut ?? "",
                    Location = new Point(100, 57),
                    Size = new Size(150, 25),
                    Font = new Font("Segoe UI", 10F),
                    CharacterCasing = CharacterCasing.Upper
                };

                var btnOK = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(100, 100),
                    Size = new Size(80, 30)
                };

                var btnCancel = new Button
                {
                    Text = "Hủy",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(190, 100),
                    Size = new Size(80, 30)
                };

                form.Controls.AddRange(new Control[] { lblCommand, lblShortcut, txtShortcut, btnOK, btnCancel });
                form.AcceptButton = btnOK;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    string newShortcut = txtShortcut.Text.Trim().ToUpper();

                    // Validate shortcut
                    if (!string.IsNullOrEmpty(newShortcut) && !IsValidShortcut(newShortcut))
                    {
                        MessageBox.Show("Lệnh tắt chỉ được chứa chữ cái, số và dấu gạch dưới!",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Check duplicate
                    if (!string.IsNullOrEmpty(newShortcut))
                    {
                        var duplicate = _config.Shortcuts.FirstOrDefault(s =>
                            s.OriginalCommand != shortcut.OriginalCommand &&
                            s.Shortcut?.ToUpper() == newShortcut);

                        if (duplicate != null)
                        {
                            var result = MessageBox.Show(
                                $"Lệnh tắt '{newShortcut}' đã được gán cho '{duplicate.OriginalCommand}'.\nBạn có muốn thay thế không?",
                                "Lệnh tắt trùng",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (result == DialogResult.Yes)
                            {
                                duplicate.Shortcut = "";
                            }
                            else
                            {
                                return;
                            }
                        }
                    }

                    shortcut.Shortcut = newShortcut;
                    item.SubItems[1].Text = newShortcut;
                    item.BackColor = string.IsNullOrEmpty(newShortcut) ? Color.White : Color.FromArgb(230, 255, 230);

                    UpdateStatus();
                    UpdatePreview();
                    RefreshAllListViews();
                }
            }
        }

        private bool IsValidShortcut(string shortcut)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(shortcut, @"^[A-Z0-9_]+$");
        }

        private void RefreshAllListViews()
        {
            foreach (TabPage tab in tabControl.TabPages)
            {
                if (tab.Controls[0] is ListView lv)
                {
                    foreach (ListViewItem item in lv.Items)
                    {
                        var shortcut = item.Tag as CommandShortcutInfo;
                        if (shortcut != null)
                        {
                            item.SubItems[1].Text = shortcut.Shortcut ?? "";
                            item.BackColor = string.IsNullOrWhiteSpace(shortcut.Shortcut)
                                ? (lv.Items.IndexOf(item) % 2 == 1 ? Color.FromArgb(248, 248, 255) : Color.White)
                                : Color.FromArgb(230, 255, 230);
                        }
                    }
                }
            }
        }

        private void UpdateStatus()
        {
            int total = _config.Shortcuts.Count;
            int assigned = _config.GetAssignedShortcuts().Count;
            var duplicates = _config.FindDuplicateShortcuts();

            string status = $"📊 Đã gán: {assigned}/{total} lệnh";

            if (duplicates.Count > 0)
            {
                status += $" | ⚠️ {duplicates.Count} lệnh tắt bị trùng";
                lblStatus.ForeColor = Color.FromArgb(180, 80, 0);
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(80, 80, 80);
            }

            lblStatus.Text = status;
        }

        private void UpdatePreview()
        {
            var assignedShortcuts = _config.GetAssignedShortcuts();
            rtbPreview.Text = LspGenerator.GeneratePreview(assignedShortcuts, 30);
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim().ToLower();

            if (tabControl.TabPages.Count > 0 && tabControl.TabPages[0].Controls[0] is ListView lvAll)
            {
                lvAll.Items.Clear();

                var filtered = string.IsNullOrEmpty(keyword)
                    ? _config.Shortcuts
                    : _config.Shortcuts.Where(s =>
                        (s.OriginalCommand?.ToLower().Contains(keyword) == true) ||
                        (s.Shortcut?.ToLower().Contains(keyword) == true) ||
                        (s.Description?.ToLower().Contains(keyword) == true));

                foreach (var shortcut in filtered.OrderBy(s => s.OriginalCommand))
                {
                    AddShortcutToListView(lvAll, shortcut);
                }

                tabControl.SelectedIndex = 0;
                tabControl.TabPages[0].Text = string.IsNullOrEmpty(keyword)
                    ? "📋 Tat ca"
                    : $"🔍 Ket qua ({lvAll.Items.Count})";
            }
        }

        private void BtnLoadConfig_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Chọn file cấu hình lệnh tắt";
                dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                dialog.DefaultExt = "json";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var loadedConfig = ShortcutConfig.LoadFromFile(dialog.FileName);
                        if (loadedConfig != null)
                        {
                            // Merge với lệnh hiện tại
                            var allCommands = HelpSystem.GetAllCommands().ToList();
                            loadedConfig.MergeWithNewCommands(allCommands);
                            _config = loadedConfig;
                            _lastConfigPath = dialog.FileName;

                            // Rebuild UI
                            _groupedShortcuts = _config.Shortcuts
                                .GroupBy(s => GetMainCategory(s.Category))
                                .OrderBy(g => GetCategoryOrder(g.Key))
                                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.OriginalCommand).ToList());

                            CreateTabs();
                            UpdateStatus();
                            UpdatePreview();

                            MessageBox.Show($"Đã load cấu hình từ:\n{dialog.FileName}\n\nĐã merge với {allCommands.Count} lệnh hiện có.",
                                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Không thể đọc file cấu hình!", "Lỗi",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi load file: {ex.Message}", "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnSaveConfig_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Lưu file cấu hình lệnh tắt";
                dialog.Filter = "JSON files (*.json)|*.json";
                dialog.DefaultExt = "json";
                dialog.FileName = "t27_shortcuts_config.json";

                if (!string.IsNullOrEmpty(_lastConfigPath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(_lastConfigPath);
                    dialog.FileName = Path.GetFileName(_lastConfigPath);
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _config.SaveToFile(dialog.FileName);
                        _lastConfigPath = dialog.FileName;

                        MessageBox.Show($"Đã lưu cấu hình vào:\n{dialog.FileName}",
                            "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi lưu file: {ex.Message}", "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnExportLsp_Click(object sender, EventArgs e)
        {
            var assignedShortcuts = _config.GetAssignedShortcuts();

            if (assignedShortcuts.Count == 0)
            {
                MessageBox.Show("Chưa có lệnh tắt nào được gán!\nVui lòng double-click vào lệnh để đặt lệnh tắt.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check duplicates
            var duplicates = _config.FindDuplicateShortcuts();
            if (duplicates.Count > 0)
            {
                var dupList = string.Join("\n", duplicates.Select(d =>
                    $"  • {d.Key}: {string.Join(", ", d.Value)}"));

                var result = MessageBox.Show(
                    $"Có {duplicates.Count} lệnh tắt bị trùng:\n{dupList}\n\nBạn có muốn tiếp tục xuất file không?",
                    "Cảnh báo trùng lệnh",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Xuất file AutoLISP";
                dialog.Filter = "LISP files (*.lsp)|*.lsp";
                dialog.DefaultExt = "lsp";
                dialog.FileName = "t27_shortcuts.lsp";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        LspGenerator.GenerateLspFile(assignedShortcuts, dialog.FileName);

                        MessageBox.Show(
                            $"Đã xuất {assignedShortcuts.Count} lệnh tắt vào:\n{dialog.FileName}\n\n" +
                            $"Để sử dụng, load file vào AutoCAD:\n(load \"{dialog.FileName.Replace("\\", "/")}\")",
                            "Thành công",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xuất file: {ex.Message}", "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc muốn xóa TẤT CẢ lệnh tắt đã gán?\nThao tác này không thể hoàn tác!",
                "Xác nhận Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                foreach (var shortcut in _config.Shortcuts)
                {
                    shortcut.Shortcut = "";
                }

                RefreshAllListViews();
                UpdateStatus();
                UpdatePreview();

                MessageBox.Show("Đã xóa tất cả lệnh tắt!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
