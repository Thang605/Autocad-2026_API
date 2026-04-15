using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

// Aliases để tránh xung đột WPF vs WinForms
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfButton = System.Windows.Controls.Button;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfGrid = System.Windows.Controls.Grid;
using WpfBorder = System.Windows.Controls.Border;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfColumnDefinition = System.Windows.Controls.ColumnDefinition;
using WpfRowDefinition = System.Windows.Controls.RowDefinition;
using WpfColor = System.Windows.Media.Color;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfCursors = System.Windows.Input.Cursors;
using WpfBrushes = System.Windows.Media.Brushes;

namespace MyFirstProject
{
    /// <summary>
    /// WPF UserControl — giao diện Tool Palette dark theme
    /// Hiển thị danh sách lệnh phân nhóm theo Category, có tìm kiếm
    /// </summary>
    public class ToolPaletteControl : WpfUserControl
    {
        // ═══════════════════════════════════════════════════════════════
        //  THEME COLORS (AutoCAD Dark)
        // ═══════════════════════════════════════════════════════════════

        static readonly SolidColorBrush BgMain = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x1E, 0x1E, 0x1E)));
        static readonly SolidColorBrush BgSearch = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x30, 0x30, 0x34)));
        static readonly SolidColorBrush BgInput = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x3C, 0x3C, 0x3C)));
        static readonly SolidColorBrush BgHeader = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x28, 0x28, 0x2C)));
        static readonly SolidColorBrush BgHeaderHover = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x33, 0x33, 0x38)));
        static readonly SolidColorBrush BgItem = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x1E, 0x1E, 0x1E)));
        static readonly SolidColorBrush BgItemHover = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x2D, 0x42, 0x5A)));
        static readonly SolidColorBrush BgItemClick = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x00, 0x6A, 0xB5)));
        static readonly SolidColorBrush FgText = Freeze(new SolidColorBrush(WpfColor.FromRgb(0xDC, 0xDC, 0xDC)));
        static readonly SolidColorBrush FgDim = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x80, 0x80, 0x80)));
        static readonly SolidColorBrush FgCmd = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x6A, 0x99, 0xBB)));
        static readonly SolidColorBrush FgCount = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x56, 0x9C, 0xD6)));
        static readonly SolidColorBrush AccentBar = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x00, 0x7A, 0xCC)));
        static readonly SolidColorBrush BorderLine = Freeze(new SolidColorBrush(WpfColor.FromRgb(0x3F, 0x3F, 0x46)));

        static SolidColorBrush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

        // ═══════════════════════════════════════════════════════════════
        //  STATE
        // ═══════════════════════════════════════════════════════════════

        private WpfTextBox _searchBox;
        private WpfStackPanel _categoriesPanel;
        private WpfTextBlock _statusText;
        private List<PaletteCommandInfo> _allCommands = new();
        private readonly Action<string> _executeCommand;
        private readonly Dictionary<string, bool> _expandStates = new();

        // ═══════════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════

        public ToolPaletteControl(Action<string> executeCommand)
        {
            _executeCommand = executeCommand;
            BuildUI();
        }

        // ═══════════════════════════════════════════════════════════════
        //  BUILD UI
        // ═══════════════════════════════════════════════════════════════

        private void BuildUI()
        {
            Background = BgMain;
            FontFamily = new WpfFontFamily("Segoe UI");

            var mainGrid = new WpfGrid();
            mainGrid.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });      // Search
            mainGrid.RowDefinitions.Add(new WpfRowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });      // Status + buttons

            // ─────────── SEARCH BAR ───────────
            var searchBorder = new WpfBorder
            {
                Background = BgSearch,
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = BorderLine,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var searchGrid = new WpfGrid();
            searchGrid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });

            // Search icon
            var searchIcon = new WpfTextBlock
            {
                Text = "🔍",
                FontSize = 12,
                Foreground = FgDim,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            WpfGrid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            // Search textbox
            _searchBox = new WpfTextBox
            {
                Background = BgInput,
                Foreground = FgText,
                CaretBrush = FgText,
                BorderBrush = BorderLine,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 3, 6, 3),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            // Placeholder text via GotFocus/LostFocus
            _searchBox.Tag = "placeholder";
            _searchBox.Text = "Tìm kiếm lệnh...";
            _searchBox.Foreground = FgDim;
            _searchBox.GotFocus += (s, e) =>
            {
                if (_searchBox.Tag as string == "placeholder")
                {
                    _searchBox.Text = "";
                    _searchBox.Foreground = FgText;
                    _searchBox.Tag = null;
                }
            };
            _searchBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(_searchBox.Text))
                {
                    _searchBox.Tag = "placeholder";
                    _searchBox.Text = "Tìm kiếm lệnh...";
                    _searchBox.Foreground = FgDim;
                }
            };
            _searchBox.TextChanged += (s, e) =>
            {
                if (_searchBox.Tag as string != "placeholder")
                    FilterCommands(_searchBox.Text);
            };
            WpfGrid.SetColumn(_searchBox, 1);
            searchGrid.Children.Add(_searchBox);

            // Clear button
            var clearBtn = CreateIconButton("✕", 22, () =>
            {
                _searchBox.Tag = null;
                _searchBox.Text = "";
                _searchBox.Foreground = FgText;
                _searchBox.Focus();
                FilterCommands("");
            });
            clearBtn.Margin = new Thickness(4, 0, 0, 0);
            WpfGrid.SetColumn(clearBtn, 2);
            searchGrid.Children.Add(clearBtn);

            searchBorder.Child = searchGrid;
            WpfGrid.SetRow(searchBorder, 0);
            mainGrid.Children.Add(searchBorder);

            // ─────────── COMMANDS LIST ───────────
            var scrollViewer = new WpfScrollViewer
            {
                VerticalScrollBarVisibility = WpfScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = WpfScrollBarVisibility.Disabled,
                Background = BgMain,
                Focusable = false
            };

            _categoriesPanel = new WpfStackPanel();
            scrollViewer.Content = _categoriesPanel;

            WpfGrid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // ─────────── STATUS BAR ───────────
            var statusGrid = new WpfGrid();
            statusGrid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statusGrid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
            statusGrid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });

            _statusText = new WpfTextBlock
            {
                Foreground = FgDim,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0)
            };
            WpfGrid.SetColumn(_statusText, 0);
            statusGrid.Children.Add(_statusText);

            // Expand All
            var expandAllBtn = CreateIconButton("▼", 22, () =>
            {
                foreach (var k in _expandStates.Keys.ToList()) _expandStates[k] = true;
                FilterCommands(GetSearchText());
            });
            expandAllBtn.ToolTip = "Mở tất cả";
            WpfGrid.SetColumn(expandAllBtn, 1);
            statusGrid.Children.Add(expandAllBtn);

            // Collapse All
            var collapseAllBtn = CreateIconButton("▶", 22, () =>
            {
                foreach (var k in _expandStates.Keys.ToList()) _expandStates[k] = false;
                FilterCommands(GetSearchText());
            });
            collapseAllBtn.ToolTip = "Thu gọn tất cả";
            collapseAllBtn.Margin = new Thickness(2, 0, 0, 0);
            WpfGrid.SetColumn(collapseAllBtn, 2);
            statusGrid.Children.Add(collapseAllBtn);

            var statusBorder = new WpfBorder
            {
                Background = BgSearch,
                BorderBrush = BorderLine,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(8, 4, 6, 4),
                Child = statusGrid
            };
            WpfGrid.SetRow(statusBorder, 2);
            mainGrid.Children.Add(statusBorder);

            Content = mainGrid;
        }

        // ═══════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Nạp danh sách lệnh và render UI
        /// </summary>
        public void LoadCommands(List<PaletteCommandInfo> commands)
        {
            _allCommands = commands ?? new();

            // Khởi tạo expand state cho categories mới
            foreach (var cat in _allCommands.Select(c => c.Category).Distinct())
            {
                if (!_expandStates.ContainsKey(cat))
                    _expandStates[cat] = true;
            }

            FilterCommands(GetSearchText());
        }

        // ═══════════════════════════════════════════════════════════════
        //  FILTER & RENDER
        // ═══════════════════════════════════════════════════════════════

        private void FilterCommands(string searchText)
        {
            _categoriesPanel.Children.Clear();

            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _allCommands
                : _allCommands.Where(c =>
                    c.Label.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Command.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            bool isSearching = !string.IsNullOrWhiteSpace(searchText);

            var grouped = filtered.GroupBy(c => c.Category).OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var section = CreateCategorySection(group.Key, group.ToList(), isSearching);
                _categoriesPanel.Children.Add(section);
            }

            // Status
            UpdateStatus(filtered.Count);
        }

        // ═══════════════════════════════════════════════════════════════
        //  CATEGORY SECTION (custom collapsible)
        // ═══════════════════════════════════════════════════════════════

        private FrameworkElement CreateCategorySection(string category, List<PaletteCommandInfo> commands, bool forceExpand)
        {
            var container = new WpfStackPanel();

            // Determine expand state
            bool isExpanded = forceExpand || (_expandStates.ContainsKey(category) && _expandStates[category]);

            // ─── Header ───
            var headerGrid = new WpfGrid();
            headerGrid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });

            // Arrow
            var arrow = new WpfTextBlock
            {
                Text = isExpanded ? "▼" : "▶",
                FontSize = 9,
                Foreground = FgDim,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 6, 0),
                Width = 12
            };
            WpfGrid.SetColumn(arrow, 0);
            headerGrid.Children.Add(arrow);

            // Category name
            var catName = new WpfTextBlock
            {
                Text = category,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = FgText,
                VerticalAlignment = VerticalAlignment.Center
            };
            WpfGrid.SetColumn(catName, 1);
            headerGrid.Children.Add(catName);

            // Count badge
            var countText = new WpfTextBlock
            {
                Text = commands.Count.ToString(),
                FontSize = 10,
                Foreground = FgCount,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            WpfGrid.SetColumn(countText, 2);
            headerGrid.Children.Add(countText);

            var headerBorder = new WpfBorder
            {
                Background = BgHeader,
                Padding = new Thickness(0, 5, 0, 5),
                BorderBrush = BorderLine,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Cursor = WpfCursors.Hand,
                Child = headerGrid
            };

            // Left accent bar
            headerBorder.BorderBrush = BorderLine;

            // Items panel
            var itemsPanel = new WpfStackPanel
            {
                Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed
            };

            foreach (var cmd in commands)
            {
                itemsPanel.Children.Add(CreateCommandItem(cmd));
            }

            // Header hover
            headerBorder.MouseEnter += (s, e) => headerBorder.Background = BgHeaderHover;
            headerBorder.MouseLeave += (s, e) => headerBorder.Background = BgHeader;

            // Click to toggle
            headerBorder.MouseLeftButtonDown += (s, e) =>
            {
                bool newState = itemsPanel.Visibility != Visibility.Visible;
                itemsPanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                arrow.Text = newState ? "▼" : "▶";
                _expandStates[category] = newState;
                e.Handled = true;
            };

            container.Children.Add(headerBorder);
            container.Children.Add(itemsPanel);
            return container;
        }

        // ═══════════════════════════════════════════════════════════════
        //  COMMAND ITEM
        // ═══════════════════════════════════════════════════════════════

        private WpfBorder CreateCommandItem(PaletteCommandInfo cmd)
        {
            var grid = new WpfGrid();
            grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });

            // Label
            var label = new WpfTextBlock
            {
                Text = cmd.Label,
                FontSize = 12,
                Foreground = FgText,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            WpfGrid.SetColumn(label, 0);
            grid.Children.Add(label);

            // Command name (monospace, dim)
            var cmdText = new WpfTextBlock
            {
                Text = cmd.Command,
                FontSize = 10,
                FontFamily = new WpfFontFamily("Consolas"),
                Foreground = FgCmd,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            WpfGrid.SetColumn(cmdText, 1);
            grid.Children.Add(cmdText);

            var border = new WpfBorder
            {
                Background = BgItem,
                Padding = new Thickness(20, 5, 8, 5),
                BorderBrush = new SolidColorBrush(WpfColor.FromArgb(20, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Cursor = WpfCursors.Hand,
                Child = grid
            };

            // Tooltip
            if (!string.IsNullOrEmpty(cmd.Description))
            {
                var tt = new WpfTextBlock
                {
                    MaxWidth = 280,
                    TextWrapping = TextWrapping.Wrap
                };
                tt.Inlines.Add(new System.Windows.Documents.Run(cmd.Description + "\n") { Foreground = FgText });
                tt.Inlines.Add(new System.Windows.Documents.Run("Lệnh: " + cmd.Command) { Foreground = FgCmd, FontFamily = new WpfFontFamily("Consolas"), FontSize = 11 });
                border.ToolTip = tt;
            }
            else
            {
                border.ToolTip = "Lệnh: " + cmd.Command;
            }

            // Hover effect
            border.MouseEnter += (s, e) =>
            {
                border.Background = BgItemHover;
            };
            border.MouseLeave += (s, e) =>
            {
                border.Background = BgItem;
            };

            // Click → execute command
            border.MouseLeftButtonDown += (s, e) =>
            {
                border.Background = BgItemClick;
                e.Handled = true;
            };
            border.MouseLeftButtonUp += (s, e) =>
            {
                border.Background = BgItemHover;
                _executeCommand?.Invoke(cmd.Command);
                e.Handled = true;
            };

            return border;
        }

        // ═══════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════

        private string GetSearchText()
        {
            if (_searchBox == null) return "";
            if (_searchBox.Tag as string == "placeholder") return "";
            return _searchBox.Text ?? "";
        }

        private void UpdateStatus(int visibleCount)
        {
            int total = _allCommands.Count;
            if (visibleCount != total)
                _statusText.Text = $"🔍 {visibleCount}/{total} lệnh";
            else
                _statusText.Text = $"📋 {total} lệnh";
        }

        private WpfButton CreateIconButton(string icon, double size, Action onClick)
        {
            var btn = new WpfButton
            {
                Content = icon,
                Width = size,
                Height = size,
                FontSize = 10,
                Padding = new Thickness(0),
                Background = WpfBrushes.Transparent,
                Foreground = FgDim,
                BorderThickness = new Thickness(0),
                Cursor = WpfCursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            btn.Click += (s, e) => onClick();

            btn.MouseEnter += (s, e) => btn.Foreground = FgText;
            btn.MouseLeave += (s, e) => btn.Foreground = FgDim;

            return btn;
        }
    }
}
