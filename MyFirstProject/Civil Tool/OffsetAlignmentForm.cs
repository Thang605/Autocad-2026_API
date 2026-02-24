using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject
{
    public class OffsetAlignmentForm : Form
    {
        // Parameter Persistence (Static)
        private static double _lastOffsetWidth = 10.0;
        private static bool _lastBothSides = false;
        private static ObjectId _lastStyleId = ObjectId.Null;
        private static ObjectId _lastParentAlignmentId = ObjectId.Null;

        // UI Controls
        private WinFormsLabel lblParentAlignment = null!;
        private Button btnPickAlignment = null!;
        private TextBox txtOffsetWidth = null!;
        private Button btnPickOffset = null!;
        private TextBox txtStartStation = null!;
        private Button btnPickStartStation = null!;
        private TextBox txtEndStation = null!;
        private Button btnPickEndStation = null!;
        private ComboBox cmbStyles = null!;
        private TextBox txtName = null!;
        private CheckBox chkBothSides = null!;
        private Button btnCreate = null!;
        private Button btnCancel = null!;

        // Data & State
        public ObjectId ParentAlignmentId { get; private set; } = ObjectId.Null;
        public double OffsetWidth => double.TryParse(txtOffsetWidth.Text, out double val) ? val : 10.0;
        public double StartStation => double.TryParse(txtStartStation.Text, out double val) ? val : 0;
        public double EndStation => double.TryParse(txtEndStation.Text, out double val) ? val : 0;
        public bool BothSides => chkBothSides.Checked;
        public ObjectId SelectedStyleId => cmbStyles.SelectedItem is StyleItem item ? item.Id : ObjectId.Null;
        public string NewAlignmentName => txtName.Text;

        private AlignmentServiceHelper _alignmentService;
        private UserInputHelper _uiHelper;

        public OffsetAlignmentForm()
        {
            _alignmentService = new AlignmentServiceHelper();
            _uiHelper = new UserInputHelper();
            InitializeComponent();
            PopulateStyles();
            LoadPersistedParameters();
        }

        private void InitializeComponent()
        {
            this.Text = "Tạo Offset Alignment";
            this.Size = new Size(400, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            int startX = 20;
            int startY = 20;
            int labelWidth = 120;
            int controlWidth = 180;
            int buttonWidth = 50;
            int spacing = 35;

            // Alignment
            new WinFormsLabel { Text = "Alignment gốc:", Location = new WinFormsPoint(startX, startY), Size = new Size(labelWidth, 20) }.Parent = this;
            lblParentAlignment = new WinFormsLabel { Text = "(Chưa chọn)", Location = new WinFormsPoint(startX + labelWidth, startY), Size = new Size(controlWidth, 20), BorderStyle = BorderStyle.Fixed3D };
            lblParentAlignment.Parent = this;
            btnPickAlignment = new Button { Text = "...", Location = new WinFormsPoint(startX + labelWidth + controlWidth + 5, startY - 2), Size = new Size(buttonWidth, 25) };
            btnPickAlignment.Click += BtnPickAlignment_Click;
            btnPickAlignment.Parent = this;

            // Name
            startY += spacing;
            new WinFormsLabel { Text = "Tên Alignment mới:", Location = new WinFormsPoint(startX, startY), Size = new Size(labelWidth, 20) }.Parent = this;
            txtName = new TextBox { Location = new WinFormsPoint(startX + labelWidth, startY), Size = new Size(controlWidth + buttonWidth + 5, 23) };
            txtName.Parent = this;

            // Offset Width
            startY += spacing;
            new WinFormsLabel { Text = "Bề rộng Offset:", Location = new WinFormsPoint(startX, startY), Size = new Size(labelWidth, 20) }.Parent = this;
            txtOffsetWidth = new TextBox { Text = "10.0", Location = new WinFormsPoint(startX + labelWidth, startY), Size = new Size(controlWidth, 23) };
            txtOffsetWidth.Parent = this;
            btnPickOffset = new Button { Text = "Pick", Location = new WinFormsPoint(startX + labelWidth + controlWidth + 5, startY - 2), Size = new Size(buttonWidth, 25) };
            btnPickOffset.Click += BtnPickOffset_Click;
            btnPickOffset.Parent = this;

            // Both Sides Option
            startY += spacing;
            chkBothSides = new CheckBox
            {
                Text = "Tạo cả 2 bên (Offset đối xứng)",
                Location = new WinFormsPoint(startX + labelWidth, startY),
                Size = new Size(200, 20)
            };
            chkBothSides.Parent = this;

            // Start Station
            startY += spacing;
            new WinFormsLabel { Text = "Lý trình đầu:", Location = new WinFormsPoint(startX, startY), Size = new Size(labelWidth, 20) }.Parent = this;
            txtStartStation = new TextBox { Location = new WinFormsPoint(startX + labelWidth, startY), Size = new Size(controlWidth, 23) };
            txtStartStation.Parent = this;
            btnPickStartStation = new Button { Text = "Pick", Location = new WinFormsPoint(startX + labelWidth + controlWidth + 5, startY - 2), Size = new Size(buttonWidth, 25) };
            btnPickStartStation.Click += (s, e) => PickStation(txtStartStation);
            btnPickStartStation.Parent = this;

            // End Station
            startY += spacing;
            new WinFormsLabel { Text = "Lý trình cuối:", Location = new WinFormsPoint(startX, startY), Size = new Size(labelWidth, 20) }.Parent = this;
            txtEndStation = new TextBox { Location = new WinFormsPoint(startX + labelWidth, startY), Size = new Size(controlWidth, 23) };
            txtEndStation.Parent = this;
            btnPickEndStation = new Button { Text = "Pick", Location = new WinFormsPoint(startX + labelWidth + controlWidth + 5, startY - 2), Size = new Size(buttonWidth, 25) };
            btnPickEndStation.Click += (s, e) => PickStation(txtEndStation);
            btnPickEndStation.Parent = this;

            // Styles
            startY += spacing;
            new WinFormsLabel { Text = "Alignment Style:", Location = new WinFormsPoint(startX, startY), Size = new Size(labelWidth, 20) }.Parent = this;
            cmbStyles = new ComboBox { Location = new WinFormsPoint(startX + labelWidth, startY), Size = new Size(controlWidth + buttonWidth + 5, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStyles.Parent = this;

            // Action Buttons
            startY += spacing + 30;
            btnCreate = new Button { Text = "Tạo", Location = new WinFormsPoint(startX + 120, startY), Size = new Size(100, 35) };
            btnCreate.Click += BtnCreate_Click;
            btnCreate.Parent = this;

            btnCancel = new Button { Text = "Hủy", Location = new WinFormsPoint(startX + 230, startY), Size = new Size(100, 35), DialogResult = DialogResult.Cancel };
            btnCancel.Parent = this;

            this.AcceptButton = btnCreate;
            this.CancelButton = btnCancel;
        }

        private void PopulateStyles()
        {
            var styles = _alignmentService.GetAllAlignmentStyles();
            int lastIdx = -1;
            for (int i = 0; i < styles.Count; i++)
            {
                var item = new StyleItem(styles[i].Id, styles[i].Name);
                cmbStyles.Items.Add(item);
                if (styles[i].Id == _lastStyleId) lastIdx = i;
            }

            if (lastIdx != -1) cmbStyles.SelectedIndex = lastIdx;
            else if (cmbStyles.Items.Count > 0) cmbStyles.SelectedIndex = 0;
        }

        private void LoadPersistedParameters()
        {
            txtOffsetWidth.Text = _lastOffsetWidth.ToString("F3");
            chkBothSides.Checked = _lastBothSides;

            if (_lastParentAlignmentId != ObjectId.Null && !_lastParentAlignmentId.IsErased)
            {
                SetParentAlignment(_lastParentAlignmentId);
            }
        }

        private void SavePersistedParameters()
        {
            if (double.TryParse(txtOffsetWidth.Text, out double width)) _lastOffsetWidth = width;
            _lastBothSides = chkBothSides.Checked;
            if (cmbStyles.SelectedItem is StyleItem item) _lastStyleId = item.Id;
            _lastParentAlignmentId = ParentAlignmentId;
        }

        private void SetParentAlignment(ObjectId id)
        {
            try
            {
                using var tr = id.Database.TransactionManager.StartTransaction();
                var align = tr.GetObject(id, OpenMode.ForRead) as Alignment;
                if (align != null)
                {
                    ParentAlignmentId = id;
                    lblParentAlignment.Text = align.Name;
                    txtName.Text = $"{align.Name}_Offset";
                    txtStartStation.Text = align.StartingStation.ToString("F3");
                    txtEndStation.Text = align.EndingStation.ToString("F3");
                }
            }
            catch { /* Handle potential issues with stale ObjectIds */ }
        }

        private void BtnPickAlignment_Click(object? sender, EventArgs e)
        {
            this.Hide();
            ObjectId id = _uiHelper.GetAlignmentId("\nChọn Alignment gốc: ");
            if (id != ObjectId.Null)
            {
                SetParentAlignment(id);
            }
            this.Show();
        }

        private void BtnPickOffset_Click(object? sender, EventArgs e)
        {
            if (ParentAlignmentId == ObjectId.Null)
            {
                MessageBox.Show("Vui lòng chọn Alignment gốc trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.Hide();
            using var tr = ParentAlignmentId.Database.TransactionManager.StartTransaction();
            var align = (Alignment)tr.GetObject(ParentAlignmentId, OpenMode.ForRead);

            var pt = _uiHelper.GetPoint("\nChọn điểm để lấy khoảng cách offset: ");
            double station = 0, offset = 0;
            align.StationOffset(pt.X, pt.Y, ref station, ref offset);
            txtOffsetWidth.Text = Math.Abs(offset).ToString("F3");
            this.Show();
        }

        private void PickStation(TextBox target)
        {
            if (ParentAlignmentId == ObjectId.Null)
            {
                MessageBox.Show("Vui lòng chọn Alignment gốc trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.Hide();
            using var tr = ParentAlignmentId.Database.TransactionManager.StartTransaction();
            var align = (Alignment)tr.GetObject(ParentAlignmentId, OpenMode.ForRead);

            var pt = _uiHelper.GetPoint("\nChọn điểm trên Alignment để lấy lý trình: ");
            double station = 0, offset = 0;
            align.StationOffset(pt.X, pt.Y, ref station, ref offset);
            target.Text = station.ToString("F3");
            this.Show();
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            if (ParentAlignmentId == ObjectId.Null)
            {
                MessageBox.Show("Vui lòng chọn Alignment gốc.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên cho Alignment mới.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!double.TryParse(txtStartStation.Text, out double start) || !double.TryParse(txtEndStation.Text, out double end))
            {
                MessageBox.Show("Lý trình không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (end <= start)
            {
                MessageBox.Show("Lý trình cuối phải lớn hơn lý trình đầu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SavePersistedParameters();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private class StyleItem
        {
            public ObjectId Id { get; }
            public string Name { get; }
            public StyleItem(ObjectId id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }
    }
}
