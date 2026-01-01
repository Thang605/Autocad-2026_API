// (C) Copyright 2024 by T27
// Form hiển thị hướng dẫn sử dụng các lệnh với tabs phân loại

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Civil3DCsharp.HelpSystem
{
    /// <summary>
    /// Form hiển thị danh sách lệnh với các tab phân loại
    /// </summary>
    public class HelpForm : Form
    {
        private TabControl tabControl;
        private TextBox txtSearch;
        private RichTextBox rtbDetails;
        private SplitContainer splitContainer;
        private Label lblStatus;
        private Button btnCopy;
        private Button btnClose;

        private Dictionary<string, List<CommandInfo>> _groupedCommands;
        private CommandInfo _selectedCommand;

        public HelpForm()
        {
            InitializeComponent();
            LoadCommands();
        }

        private void InitializeComponent()
        {
            this.Text = "T27 Tools - Huong Dan Su Dung Lenh";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 500);
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.FromArgb(245, 245, 250);

            // ========== BOTTOM PANEL: Status + Buttons ==========
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(240, 240, 245),
                Padding = new Padding(10)
            };

            lblStatus = new Label
            {
                Text = "San sang",
                AutoSize = true,
                Location = new Point(15, 15),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            btnCopy = new Button
            {
                Text = "Copy ten lenh",
                Size = new Size(130, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCopy.Click += BtnCopy_Click;

            btnClose = new Button
            {
                Text = "Dong",
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 80, 80),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => this.Close();

            bottomPanel.Controls.Add(lblStatus);
            bottomPanel.Controls.Add(btnCopy);
            bottomPanel.Controls.Add(btnClose);
            this.Controls.Add(bottomPanel);

            // ========== LEFT PANEL: Search + Tabs ==========
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 600,
                BackColor = Color.White
            };

            // Search panel
            Panel searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10, 10, 10, 5),
                BackColor = Color.FromArgb(70, 130, 180)
            };

            Label lblSearch = new Label
            {
                Text = "Tim kiem:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 15)
            };

            txtSearch = new TextBox
            {
                Location = new Point(100, 12),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10F)
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            searchPanel.Controls.Add(lblSearch);
            searchPanel.Controls.Add(txtSearch);

            // Tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                Padding = new Point(10, 5)
            };
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            leftPanel.Controls.Add(tabControl);
            leftPanel.Controls.Add(searchPanel);

            // ========== RIGHT PANEL: Details ==========
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 250, 255)
            };

            // Title panel for details
            Panel titlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(50, 50, 50),
                Padding = new Padding(10, 8, 10, 8)
            };

            Label lblDetailsTitle = new Label
            {
                Text = "CHI TIET LENH",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            titlePanel.Controls.Add(lblDetailsTitle);

            // Details rich text box
            rtbDetails = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 255),
                Font = new Font("Consolas", 10F),
                BorderStyle = BorderStyle.None
            };
            rtbDetails.LinkClicked += RtbDetails_LinkClicked;

            // QUAN TRỌNG: Thêm rtbDetails trước, sau đó thêm titlePanel
            rightPanel.Controls.Add(rtbDetails);
            rightPanel.Controls.Add(titlePanel);

            // QUAN TRỌNG: Thêm rightPanel TRƯỚC leftPanel để Dock.Fill hoạt động đúng
            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);

            // Resize handler for buttons
            this.Resize += (s, e) =>
            {
                btnClose.Location = new Point(this.ClientSize.Width - 105, 10);
                btnCopy.Location = new Point(this.ClientSize.Width - 245, 10);
            };

            // Initialize button positions
            this.Load += (s, e) =>
            {
                btnClose.Location = new Point(this.ClientSize.Width - 105, 10);
                btnCopy.Location = new Point(this.ClientSize.Width - 245, 10);
            };
        }

        private void LoadCommands()
        {
            try
            {
                // Get all commands and group by category
                var allCommands = HelpSystem.GetAllCommands().ToList();

                // Group commands
                _groupedCommands = allCommands
                    .GroupBy(c => GetMainCategory(c.Category))
                    .OrderBy(g => GetCategoryOrder(g.Key))
                    .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Name).ToList());

                // Create tabs
                CreateTabs();

                // Update status
                lblStatus.Text = $"📊 Tổng cộng: {allCommands.Count} lệnh | {_groupedCommands.Count} nhóm";
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

            // Extract main category (before the dash)
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
            TabPage tabAll = new TabPage("📋 Tất cả");
            tabAll.Tag = "ALL";
            ListView lvAll = CreateListView();
            foreach (var group in _groupedCommands)
            {
                foreach (var cmd in group.Value)
                {
                    AddCommandToListView(lvAll, cmd);
                }
            }
            tabAll.Controls.Add(lvAll);
            tabControl.TabPages.Add(tabAll);

            // Tabs for each category
            foreach (var group in _groupedCommands)
            {
                string icon = GetCategoryIcon(group.Key);
                TabPage tab = new TabPage($"{icon} {group.Key} ({group.Value.Count})");
                tab.Tag = group.Key;

                ListView lv = CreateListView();
                foreach (var cmd in group.Value)
                {
                    AddCommandToListView(lv, cmd);
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
                BackColor = Color.White
            };

            lv.Columns.Add("Tên lệnh", 200);
            lv.Columns.Add("Nhóm", 150);
            lv.Columns.Add("Mô tả", 400);

            lv.SelectedIndexChanged += ListView_SelectedIndexChanged;
            lv.DoubleClick += ListView_DoubleClick;

            return lv;
        }

        private void AddCommandToListView(ListView lv, CommandInfo cmd)
        {
            ListViewItem item = new ListViewItem(cmd.Name);
            item.SubItems.Add(cmd.Category ?? "");
            item.SubItems.Add(cmd.Description ?? "");
            item.Tag = cmd;

            // Alternate row colors
            if (lv.Items.Count % 2 == 1)
            {
                item.BackColor = Color.FromArgb(248, 248, 255);
            }

            lv.Items.Add(item);
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView lv = sender as ListView;
            if (lv?.SelectedItems.Count > 0)
            {
                _selectedCommand = lv.SelectedItems[0].Tag as CommandInfo;
                DisplayCommandDetails(_selectedCommand);
            }
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (_selectedCommand != null)
            {
                // Copy command name to clipboard
                Clipboard.SetText(_selectedCommand.Name);
                lblStatus.Text = $"✅ Đã copy lệnh '{_selectedCommand.Name}' vào clipboard!";
            }
        }

        private void DisplayCommandDetails(CommandInfo cmd)
        {
            if (cmd == null) return;

            rtbDetails.Clear();

            // Title
            AppendText($"═══════════════════════════════════════════════════\n", Color.FromArgb(70, 130, 180), true);
            AppendText($"  {cmd.Name}\n", Color.FromArgb(0, 100, 180), true, 14);
            AppendText($"═══════════════════════════════════════════════════\n\n", Color.FromArgb(70, 130, 180), true);

            // Category
            AppendText("📁 NHÓM: ", Color.FromArgb(100, 100, 100), true);
            AppendText($"{cmd.Category ?? "Chưa phân loại"}\n\n", Color.Black, false);

            // Description
            AppendText("📝 MÔ TẢ:\n", Color.FromArgb(0, 100, 0), true);
            AppendText($"   {cmd.Description ?? "Chưa có mô tả"}\n\n", Color.Black, false);

            // Usage
            AppendText("⌨️ CÚ PHÁP:\n", Color.FromArgb(0, 100, 0), true);
            AppendText($"   Command: ", Color.Gray, false);
            AppendText($"{cmd.Usage ?? cmd.Name}\n\n", Color.FromArgb(180, 0, 0), true);

            // Steps
            if (cmd.Steps != null && cmd.Steps.Length > 0)
            {
                AppendText("📋 CÁC BƯỚC THỰC HIỆN:\n", Color.FromArgb(0, 100, 0), true);
                foreach (var step in cmd.Steps)
                {
                    AppendText($"   {step}\n", Color.Black, false);
                }
                AppendText("\n", Color.Black, false);
            }

            // Examples
            if (cmd.Examples != null && cmd.Examples.Length > 0)
            {
                AppendText("💡 VÍ DỤ:\n", Color.FromArgb(0, 100, 0), true);
                foreach (var ex in cmd.Examples)
                {
                    AppendText($"   {ex}\n", Color.FromArgb(100, 100, 100), false);
                }
                AppendText("\n", Color.Black, false);
            }

            // Notes
            if (cmd.Notes != null && cmd.Notes.Length > 0)
            {
                AppendText("⚠️ LƯU Ý:\n", Color.FromArgb(180, 100, 0), true);
                foreach (var note in cmd.Notes)
                {
                    AppendText($"   {note}\n", Color.FromArgb(150, 100, 50), false);
                }
            }

            // Video Link
            if (!string.IsNullOrEmpty(cmd.VideoLink))
            {
                AppendText("\n🎥 VIDEO HƯỚNG DẪN:\n", Color.Red, true);

                // Add link
                int start = rtbDetails.TextLength;
                rtbDetails.AppendText($"   {cmd.VideoLink}\n");
                rtbDetails.Select(start, cmd.VideoLink.Length + 4); // +4 for indentation
                rtbDetails.SelectionColor = Color.Blue;
                rtbDetails.SelectionFont = new Font("Consolas", 10, FontStyle.Underline);

                // Enable link clicking
                // Enable link clicking
                // Event handler is now attached in InitializeComponent
            }

            rtbDetails.SelectionStart = 0;
            rtbDetails.ScrollToCaret();
        }

        private void AppendText(string text, Color color, bool bold, int fontSize = 10)
        {
            int start = rtbDetails.TextLength;
            rtbDetails.AppendText(text);
            rtbDetails.Select(start, text.Length);
            rtbDetails.SelectionColor = color;
            rtbDetails.SelectionFont = new Font("Consolas", fontSize, bold ? FontStyle.Bold : FontStyle.Regular);
            rtbDetails.SelectionLength = 0;
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(keyword))
            {
                // Restore original
                CreateTabs();
                return;
            }

            // Filter commands
            var filtered = HelpSystem.GetAllCommands()
                .Where(c =>
                    (c.Name?.ToLower().Contains(keyword) == true) ||
                    (c.Description?.ToLower().Contains(keyword) == true) ||
                    (c.Category?.ToLower().Contains(keyword) == true))
                .OrderBy(c => c.Name)
                .ToList();

            // Update first tab with filtered results
            if (tabControl.TabPages.Count > 0)
            {
                TabPage firstTab = tabControl.TabPages[0];
                firstTab.Text = $"🔍 Kết quả ({filtered.Count})";

                ListView lv = firstTab.Controls[0] as ListView;
                if (lv != null)
                {
                    lv.Items.Clear();
                    foreach (var cmd in filtered)
                    {
                        AddCommandToListView(lv, cmd);
                    }
                }

                tabControl.SelectedIndex = 0;
            }

            lblStatus.Text = $"🔍 Tìm thấy {filtered.Count} lệnh với từ khóa '{keyword}'";
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Clear search when changing tabs (if not searching)
            if (!string.IsNullOrEmpty(txtSearch.Text))
            {
                // Keep search active
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (_selectedCommand != null)
            {
                Clipboard.SetText(_selectedCommand.Name);
                lblStatus.Text = $"✅ Đã copy lệnh '{_selectedCommand.Name}' vào clipboard!";
            }
            else
            {
                lblStatus.Text = "⚠️ Vui lòng chọn một lệnh trước!";
            }
        }

        private void RtbDetails_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.LinkText,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở link: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
