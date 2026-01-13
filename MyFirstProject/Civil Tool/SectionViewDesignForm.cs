using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using MyFirstProject.Extensions;

namespace MyFirstProject.Civil_Tool
{
    public partial class SectionViewDesignForm : Form
    {
        // Static fields to remember last used values
        private static double _lastWeedingTop = 0.7;
        private static double _lastWeedingTN = 1.0;
        private static bool _lastAddElevationBands = true;
        private static bool _lastAddDistanceBands = true;
        private static bool _lastImportBandSet = false;
        private static string _lastBandSetStyleName = "(None)";
        private static bool _lastCreateMaterialList = true;
        private static string _lastMaterialListName = "Bảng đào đắp";
        private static bool _lastCreateVolumeTable = true;
        private static string _lastTablePosition = "TopLeft";
        private static bool _lastAutoConfigureSources = true;
        private static string _lastLayoutTemplatePath = "Z:/Z.FORM MAU LAM VIEC/1. BIM/2.MAU C3D/2.THU VIEN C3D/2.LAYOUT C3D/LAYOUT CIVIL 3D.dwt";
        private static string _lastLayoutName = "A3-TN-1-200";
        private static string _lastSectionViewStyleName = "TCVN Road Section 1-1000";
        private static string _lastPlotStyleName = "A3 SECTION FIT ALL";

        // Form controls
        private GroupBox groupBoxAlignment;
        private System.Windows.Forms.Label lblAlignment;
        private TextBox txtAlignmentName;
        private System.Windows.Forms.Button btnSelectAlignment;

        private GroupBox groupBoxPlacement;
        private System.Windows.Forms.Label lblPlacementPoint;
        private TextBox txtPlacementPoint;
        private System.Windows.Forms.Button btnSelectPlacementPoint;
        private System.Windows.Forms.Label lblLayoutTemplate;
        private ComboBox cmbLayoutTemplate;
        private System.Windows.Forms.Label lblLayoutName;
        private ComboBox cmbLayoutName;

        private GroupBox groupBoxSectionSources;
        private DataGridView dgvSectionSources;
        private System.Windows.Forms.Button btnRefreshSources;
        private CheckBox chkAutoConfigureSources;

        private GroupBox groupBoxStyles;
        private System.Windows.Forms.Label lblSectionViewStyle;
        private ComboBox cmbSectionViewStyle;
        private System.Windows.Forms.Label lblPlotStyle;
        private ComboBox cmbPlotStyle;

        private GroupBox groupBoxMaterial;
        private CheckBox chkCreateMaterialList;
        private System.Windows.Forms.Label lblMaterialListName;
        private TextBox txtMaterialListName;

        private GroupBox groupBoxBands;
        private CheckBox chkAddElevationBands;
        private CheckBox chkAddDistanceBands;
        private CheckBox chkImportBandSet;
        private System.Windows.Forms.Label lblBandSetStyle;
        private ComboBox cmbBandSetStyle;
        private System.Windows.Forms.Label lblWeedingTop;
        private NumericUpDown nudWeedingTop;
        private System.Windows.Forms.Label lblWeedingTN;
        private NumericUpDown nudWeedingTN;

        private GroupBox groupBoxTable;
        private CheckBox chkCreateVolumeTable;
        private System.Windows.Forms.Label lblTablePosition;
        private ComboBox cmbTablePosition;

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        // Properties to store user selections
        public ObjectId AlignmentId { get; private set; } = ObjectId.Null;
        public Point3d PlacementPoint { get; private set; } = Point3d.Origin;
        public string LayoutTemplatePath { get; private set; } = "Z:/Z.FORM MAU LAM VIEC/1. BIM/2.MAU C3D/2.THU VIEN C3D/2.LAYOUT C3D/LAYOUT CIVIL 3D.dwt";
        public string LayoutName { get; private set; } = "A3-TN-1-200";
        public List<SectionSourceConfig> SectionSources { get; private set; } = new();
        public ObjectId SectionViewStyleId { get; private set; } = ObjectId.Null;
        public ObjectId PlotStyleId { get; private set; } = ObjectId.Null;
        public bool CreateMaterialList { get; private set; } = true;
        public string MaterialListName { get; private set; } = "Bảng đào đắp";

        public bool AddElevationBands { get; private set; } = true;
        public bool AddDistanceBands { get; private set; } = true;
        public bool ImportBandSet { get; private set; } = false;
        public ObjectId BandSetStyleId { get; private set; } = ObjectId.Null;
        public double WeedingTop { get; private set; } = 0.7;
        public double WeedingTN { get; private set; } = 1.0;

        public bool CreateVolumeTable { get; private set; } = true;
        public string TablePosition { get; private set; } = "TopLeft";
        public bool DialogResultOK { get; private set; } = false;

        public SectionViewDesignForm()
        {
            InitializeComponent();
            LoadStylesAndTemplates();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            // Form setup
            this.Text = "Thiết lập tham số vẽ trắc ngang thiết kế";
            this.Size = new Size(800, 700);  // Increased height to ensure all controls are visible
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int yPos = 10;
            int groupHeight = 0;

            // Alignment group
            this.groupBoxAlignment = new GroupBox();
            this.groupBoxAlignment.Text = "1. Chọn tim đường";
            this.groupBoxAlignment.Location = new System.Drawing.Point(10, yPos);
            this.groupBoxAlignment.Size = new Size(760, 80);

            this.lblAlignment = new System.Windows.Forms.Label();
            this.lblAlignment.Text = "Tim đường:";
            this.lblAlignment.Location = new System.Drawing.Point(15, 25);
            this.lblAlignment.Size = new Size(80, 23);

            this.txtAlignmentName = new TextBox();
            this.txtAlignmentName.Location = new System.Drawing.Point(100, 25);
            this.txtAlignmentName.Size = new Size(500, 23);
            this.txtAlignmentName.ReadOnly = true;

            this.btnSelectAlignment = new System.Windows.Forms.Button();
            this.btnSelectAlignment.Text = "Chọn";
            this.btnSelectAlignment.Location = new System.Drawing.Point(610, 25);
            this.btnSelectAlignment.Size = new Size(80, 25);
            this.btnSelectAlignment.Click += BtnSelectAlignment_Click;

            this.groupBoxAlignment.Controls.AddRange(new Control[] { lblAlignment, txtAlignmentName, btnSelectAlignment });
            this.Controls.Add(groupBoxAlignment);

            yPos += 90;

            // Placement group
            this.groupBoxPlacement = new GroupBox();
            this.groupBoxPlacement.Text = "2. Vị trí đặt và Layout";
            this.groupBoxPlacement.Location = new System.Drawing.Point(10, yPos);
            this.groupBoxPlacement.Size = new Size(760, 120);

            this.lblPlacementPoint = new System.Windows.Forms.Label();
            this.lblPlacementPoint.Text = "Điểm đặt:";
            this.lblPlacementPoint.Location = new System.Drawing.Point(15, 25);
            this.lblPlacementPoint.Size = new Size(80, 23);

            this.txtPlacementPoint = new TextBox();
            this.txtPlacementPoint.Location = new System.Drawing.Point(100, 25);
            this.txtPlacementPoint.Size = new Size(500, 23);
            this.txtPlacementPoint.ReadOnly = true;

            this.btnSelectPlacementPoint = new System.Windows.Forms.Button();
            this.btnSelectPlacementPoint.Text = "Chọn";
            this.btnSelectPlacementPoint.Location = new System.Drawing.Point(610, 25);
            this.btnSelectPlacementPoint.Size = new Size(80, 25);
            this.btnSelectPlacementPoint.Click += BtnSelectPlacementPoint_Click;

            this.lblLayoutTemplate = new System.Windows.Forms.Label();
            this.lblLayoutTemplate.Text = "Layout Template:";
            this.lblLayoutTemplate.Location = new System.Drawing.Point(15, 55);
            this.lblLayoutTemplate.Size = new Size(120, 23);

            this.cmbLayoutTemplate = new ComboBox();
            this.cmbLayoutTemplate.Location = new System.Drawing.Point(140, 55);
            this.cmbLayoutTemplate.Size = new Size(600, 23);
            this.cmbLayoutTemplate.DropDownStyle = ComboBoxStyle.DropDown;

            this.lblLayoutName = new System.Windows.Forms.Label();
            this.lblLayoutName.Text = "Layout Name:";
            this.lblLayoutName.Location = new System.Drawing.Point(15, 85);
            this.lblLayoutName.Size = new Size(120, 23);

            this.cmbLayoutName = new ComboBox();
            this.cmbLayoutName.Location = new System.Drawing.Point(140, 85);
            this.cmbLayoutName.Size = new Size(600, 23);
            this.cmbLayoutName.DropDownStyle = ComboBoxStyle.DropDownList;

            this.groupBoxPlacement.Controls.AddRange(new Control[] { lblPlacementPoint, txtPlacementPoint, btnSelectPlacementPoint, lblLayoutTemplate, cmbLayoutTemplate, lblLayoutName, cmbLayoutName });
            this.Controls.Add(groupBoxPlacement);

            yPos += 130;

            // Section Sources group
            this.groupBoxSectionSources = new GroupBox();
            this.groupBoxSectionSources.Text = "3. Nguồn dữ liệu trắc ngang";
            this.groupBoxSectionSources.Location = new System.Drawing.Point(10, yPos);
            this.groupBoxSectionSources.Size = new Size(760, 160); // Reduced height to save space

            this.chkAutoConfigureSources = new CheckBox();
            this.chkAutoConfigureSources.Text = "Tự động cấu hình nguồn dữ liệu theo tên";
            this.chkAutoConfigureSources.Location = new System.Drawing.Point(15, 25);
            this.chkAutoConfigureSources.Size = new Size(300, 23);
            this.chkAutoConfigureSources.Checked = true;

            this.btnRefreshSources = new System.Windows.Forms.Button();
            this.btnRefreshSources.Text = "Làm mới";
            this.btnRefreshSources.Location = new System.Drawing.Point(650, 25);
            this.btnRefreshSources.Size = new Size(80, 25);
            this.btnRefreshSources.Click += BtnRefreshSources_Click;

            this.dgvSectionSources = new DataGridView();
            this.dgvSectionSources.Location = new System.Drawing.Point(15, 55);
            this.dgvSectionSources.Size = new Size(730, 95); // Reduced height
            this.dgvSectionSources.AllowUserToAddRows = false;
            this.dgvSectionSources.AllowUserToDeleteRows = false;
            this.dgvSectionSources.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvSectionSources.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvSectionSources.EditMode = DataGridViewEditMode.EditOnEnter;

            // Add DataError event handler to prevent error dialogs
            this.dgvSectionSources.DataError += (sender, e) =>
            {
                A.Ed.WriteMessage($"\nDataGridView data error: {e.Exception?.Message ?? "Unknown error"}");
                e.ThrowException = false;
            };

            // Add event handler for better ComboBox handling
            this.dgvSectionSources.CellFormatting += (sender, e) =>
            {
                try
                {
                    if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
                    {
                        var column = dgvSectionSources.Columns[e.ColumnIndex];
                        if (column.Name == "Style" && e.Value is StyleItem styleItem)
                        {
                            e.Value = styleItem.Name;
                            e.FormattingApplied = true;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\nError in CellFormatting: {ex.Message}");
                }
            };

            // Add columns to DataGridView
            var colUse = new DataGridViewCheckBoxColumn();
            colUse.Name = "Use";
            colUse.HeaderText = "Sử dụng";
            colUse.Width = 80;
            this.dgvSectionSources.Columns.Add(colUse);

            var colSourceType = new DataGridViewTextBoxColumn();
            colSourceType.Name = "SourceType";
            colSourceType.HeaderText = "Loại nguồn";
            colSourceType.ReadOnly = true;
            colSourceType.Width = 120;
            this.dgvSectionSources.Columns.Add(colSourceType);

            var colSourceName = new DataGridViewTextBoxColumn();
            colSourceName.Name = "SourceName";
            colSourceName.HeaderText = "Tên nguồn";
            colSourceName.ReadOnly = true;
            colSourceName.FillWeight = 200;
            this.dgvSectionSources.Columns.Add(colSourceName);

            var colStyle = new DataGridViewComboBoxColumn();
            colStyle.Name = "Style";
            colStyle.HeaderText = "Style";
            colStyle.Width = 200;
            colStyle.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            colStyle.FlatStyle = FlatStyle.Standard;
            // Thiết lập để có thể edit và chọn từ dropdown
            colStyle.DropDownWidth = 250;
            this.dgvSectionSources.Columns.Add(colStyle);

            this.groupBoxSectionSources.Controls.AddRange(new Control[] { chkAutoConfigureSources, btnRefreshSources, dgvSectionSources });
            this.Controls.Add(groupBoxSectionSources);

            yPos += 170;

            // Styles group
            this.groupBoxStyles = new GroupBox();
            this.groupBoxStyles.Text = "4. Styles";
            this.groupBoxStyles.Location = new System.Drawing.Point(10, yPos);
            this.groupBoxStyles.Size = new Size(760, 90);

            this.lblSectionViewStyle = new System.Windows.Forms.Label();
            this.lblSectionViewStyle.Text = "Section View Style:";
            this.lblSectionViewStyle.Location = new System.Drawing.Point(15, 25);
            this.lblSectionViewStyle.Size = new Size(150, 23);

            this.cmbSectionViewStyle = new ComboBox();
            this.cmbSectionViewStyle.Location = new System.Drawing.Point(170, 25);
            this.cmbSectionViewStyle.Size = new Size(300, 23);
            this.cmbSectionViewStyle.DropDownStyle = ComboBoxStyle.DropDownList;

            this.lblPlotStyle = new System.Windows.Forms.Label();
            this.lblPlotStyle.Text = "Plot Style:";
            this.lblPlotStyle.Location = new System.Drawing.Point(15, 55);
            this.lblPlotStyle.Size = new Size(150, 23);

            this.cmbPlotStyle = new ComboBox();
            this.cmbPlotStyle.Location = new System.Drawing.Point(170, 55);
            this.cmbPlotStyle.Size = new Size(300, 23);
            this.cmbPlotStyle.DropDownStyle = ComboBoxStyle.DropDownList;

            this.groupBoxStyles.Controls.AddRange(new Control[] { lblSectionViewStyle, cmbSectionViewStyle, lblPlotStyle, cmbPlotStyle });
            this.Controls.Add(groupBoxStyles);

            yPos += 100;

            // Options groups side by side
            int leftGroupWidth = 375;
            int rightGroupWidth = 375;

            // Material group (left) - This is the "Tạo material list" section
            this.groupBoxMaterial = new GroupBox();
            this.groupBoxMaterial.Text = "5. Vật liệu";
            this.groupBoxMaterial.Location = new System.Drawing.Point(10, yPos);
            this.groupBoxMaterial.Size = new Size(leftGroupWidth, 90);
            this.groupBoxMaterial.BackColor = Color.LightYellow; // Highlight for visibility

            this.chkCreateMaterialList = new CheckBox();
            this.chkCreateMaterialList.Text = "Tạo material list";
            this.chkCreateMaterialList.Location = new System.Drawing.Point(15, 25);
            this.chkCreateMaterialList.Size = new Size(200, 23);
            this.chkCreateMaterialList.Checked = true;
            this.chkCreateMaterialList.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, FontStyle.Bold);

            this.lblMaterialListName = new System.Windows.Forms.Label();
            this.lblMaterialListName.Text = "Tên material list:";
            this.lblMaterialListName.Location = new System.Drawing.Point(15, 55);
            this.lblMaterialListName.Size = new Size(120, 23);

            this.txtMaterialListName = new TextBox();
            this.txtMaterialListName.Location = new System.Drawing.Point(140, 55);
            this.txtMaterialListName.Size = new Size(220, 23);
            this.txtMaterialListName.Text = "Bảng đào đắp";

            this.groupBoxMaterial.Controls.AddRange(new Control[] { chkCreateMaterialList, lblMaterialListName, txtMaterialListName });
            this.Controls.Add(groupBoxMaterial);

            // Table group (right)
            this.groupBoxTable = new GroupBox();
            this.groupBoxTable.Text = "6. Bảng khối lượng";
            this.groupBoxTable.Location = new System.Drawing.Point(395, yPos);
            this.groupBoxTable.Size = new Size(rightGroupWidth, 90);

            this.chkCreateVolumeTable = new CheckBox();
            this.chkCreateVolumeTable.Text = "Tạo bảng khối lượng";
            this.chkCreateVolumeTable.Location = new System.Drawing.Point(15, 25);
            this.chkCreateVolumeTable.Size = new Size(200, 23);
            this.chkCreateVolumeTable.Checked = true;

            this.lblTablePosition = new System.Windows.Forms.Label();
            this.lblTablePosition.Text = "Vị trí bảng:";
            this.lblTablePosition.Location = new System.Drawing.Point(15, 55);
            this.lblTablePosition.Size = new Size(80, 23);

            this.cmbTablePosition = new ComboBox();
            this.cmbTablePosition.Location = new System.Drawing.Point(100, 55);
            this.cmbTablePosition.Size = new Size(150, 23);
            this.cmbTablePosition.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbTablePosition.Items.AddRange(new string[] { "TopLeft", "TopRight", "BottomLeft", "BottomRight" });
            this.cmbTablePosition.SelectedIndex = 0;

            this.groupBoxTable.Controls.AddRange(new Control[] { chkCreateVolumeTable, lblTablePosition, cmbTablePosition });
            this.Controls.Add(groupBoxTable);

            yPos += 100;

            // Bands group
            this.groupBoxBands = new GroupBox();
            this.groupBoxBands.Text = "7. Bands & Labels";
            this.groupBoxBands.Location = new System.Drawing.Point(10, yPos);
            this.groupBoxBands.Size = new Size(760, 130); // Increased height for weeding controls

            this.chkAddElevationBands = new CheckBox();
            this.chkAddElevationBands.Text = "Thêm elevation bands";
            this.chkAddElevationBands.Location = new System.Drawing.Point(15, 25);
            this.chkAddElevationBands.Size = new Size(200, 23);
            this.chkAddElevationBands.Checked = true;

            this.chkAddDistanceBands = new CheckBox();
            this.chkAddDistanceBands.Text = "Thêm distance bands";
            this.chkAddDistanceBands.Location = new System.Drawing.Point(230, 25);
            this.chkAddDistanceBands.Size = new Size(200, 23);
            this.chkAddDistanceBands.Checked = true;

            // Weeding Distance controls
            this.lblWeedingTop = new System.Windows.Forms.Label();
            this.lblWeedingTop.Text = "Weeding TOP:";
            this.lblWeedingTop.Location = new System.Drawing.Point(460, 25);
            this.lblWeedingTop.Size = new Size(100, 23);

            this.nudWeedingTop = new NumericUpDown();
            this.nudWeedingTop.Location = new System.Drawing.Point(560, 25);
            this.nudWeedingTop.Size = new Size(70, 23);
            this.nudWeedingTop.Minimum = 0.1m;
            this.nudWeedingTop.Maximum = 10m;
            this.nudWeedingTop.Value = 0.7m;
            this.nudWeedingTop.DecimalPlaces = 1;
            this.nudWeedingTop.Increment = 0.1m;

            this.lblWeedingTN = new System.Windows.Forms.Label();
            this.lblWeedingTN.Text = "Weeding TN:";
            this.lblWeedingTN.Location = new System.Drawing.Point(640, 25);
            this.lblWeedingTN.Size = new Size(80, 23);

            this.nudWeedingTN = new NumericUpDown();
            this.nudWeedingTN.Location = new System.Drawing.Point(720, 25);
            this.nudWeedingTN.Size = new Size(70, 23);
            this.nudWeedingTN.Minimum = 0.1m;
            this.nudWeedingTN.Maximum = 10m;
            this.nudWeedingTN.Value = 1.0m;
            this.nudWeedingTN.DecimalPlaces = 1;
            this.nudWeedingTN.Increment = 0.1m;

            // Band Set import
            this.chkImportBandSet = new CheckBox();
            this.chkImportBandSet.Text = "Import Band Set";
            this.chkImportBandSet.Location = new System.Drawing.Point(15, 55);
            this.chkImportBandSet.Size = new Size(150, 23);
            this.chkImportBandSet.Checked = false;
            this.chkImportBandSet.CheckedChanged += ChkImportBandSet_CheckedChanged;

            this.lblBandSetStyle = new System.Windows.Forms.Label();
            this.lblBandSetStyle.Text = "Band Set:";
            this.lblBandSetStyle.Location = new System.Drawing.Point(180, 55);
            this.lblBandSetStyle.Size = new Size(120, 23);
            this.lblBandSetStyle.Enabled = false;

            this.cmbBandSetStyle = new ComboBox();
            this.cmbBandSetStyle.Location = new System.Drawing.Point(310, 55);
            this.cmbBandSetStyle.Size = new Size(300, 23);
            this.cmbBandSetStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbBandSetStyle.Enabled = false;
            this.cmbBandSetStyle.SelectedIndexChanged += CmbBandSetStyle_SelectedIndexChanged;

            // Weeding info label
            var lblWeedingInfo = new System.Windows.Forms.Label();
            lblWeedingInfo.Text = "💡 Weeding Distance: Khoảng cách tối thiểu giữa các labels/bands (đơn vị: m)";
            lblWeedingInfo.Location = new System.Drawing.Point(15, 85);
            lblWeedingInfo.Size = new Size(600, 23);
            lblWeedingInfo.ForeColor = Color.Gray;

            this.groupBoxBands.Controls.AddRange(new Control[] {
                chkAddElevationBands, chkAddDistanceBands,
                lblWeedingTop, nudWeedingTop, lblWeedingTN, nudWeedingTN,
                chkImportBandSet, lblBandSetStyle, cmbBandSetStyle,
                lblWeedingInfo });
            this.Controls.Add(groupBoxBands);

            yPos += 140;

            // Add info label about corridor surface creation
            var lblCorridorInfo = new System.Windows.Forms.Label();
            lblCorridorInfo.Text = "💡 Để tạo corridor surfaces, sử dụng lệnh riêng biệt: CTSV_TaoCorridorSurface";
            lblCorridorInfo.Location = new System.Drawing.Point(10, yPos);
            lblCorridorInfo.Size = new Size(760, 25);
            lblCorridorInfo.ForeColor = Color.DarkOrange;
            lblCorridorInfo.Font = new System.Drawing.Font(lblCorridorInfo.Font, FontStyle.Bold);
            lblCorridorInfo.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblCorridorInfo);

            yPos += 35;

            // OK/Cancel buttons
            this.btnOK = new System.Windows.Forms.Button();
            this.btnOK.Text = "OK";
            this.btnOK.Location = new System.Drawing.Point(600, yPos);
            this.btnOK.Size = new Size(80, 35);
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, FontStyle.Bold);
            this.btnOK.Click += BtnOK_Click;

            this.btnCancel = new System.Windows.Forms.Button();
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new System.Drawing.Point(690, yPos);
            this.btnCancel.Size = new Size(80, 35);
            this.btnCancel.Click += (sender, e) =>
            {
                DialogResultOK = false;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] { btnOK, btnCancel });

            // Adjust form height
            this.Height = yPos + 80;
        }

        private void LoadStylesAndTemplates()
        {
            try
            {
                // Load layout template path (default)
                cmbLayoutTemplate.Items.Add("Z:/Z.FORM MAU LAM VIEC/1. BIM/2.MAU C3D/2.THU VIEN C3D/2.LAYOUT C3D/LAYOUT CIVIL 3D.dwt");
                cmbLayoutTemplate.Text = LayoutTemplatePath;

                // Load layout names
                cmbLayoutName.Items.AddRange(new string[] { "A3-TN-1-200", "A3-TN-1-500", "A3-TN-1-1000", "A4-TN-1-200" });
                cmbLayoutName.SelectedIndex = 0;

                // Load styles from document
                LoadDocumentStyles();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải styles: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadDocumentStyles()
        {
            try
            {
                // Use separate transaction for loading styles
                using (var tr = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // Load Section View Styles - ưu tiên "TCVN Road Section 1-1000"
                        var sectionViewStyles = A.Cdoc.Styles.SectionViewStyles;
                        StyleItem? priorityItem = null;

                        foreach (ObjectId styleId in sectionViewStyles)
                        {
                            if (tr.GetObject(styleId, OpenMode.ForWrite) is Autodesk.Civil.DatabaseServices.Styles.SectionViewStyle style)
                            {
                                var styleItem = new StyleItem(style.Name, styleId);
                                cmbSectionViewStyle.Items.Add(styleItem);

                                // Check for priority style
                                if (style.Name == "TCVN Road Section 1-1000")
                                {
                                    priorityItem = styleItem;
                                }
                            }
                        }

                        // Set priority or first item
                        if (priorityItem != null)
                        {
                            cmbSectionViewStyle.SelectedItem = priorityItem;
                            SectionViewStyleId = priorityItem.StyleId;
                        }
                        else if (cmbSectionViewStyle.Items.Count > 0)
                        {
                            cmbSectionViewStyle.SelectedIndex = 0;
                        }

                        // Load Group Plot Styles - ưu tiên "A3 SECTION FIT ALL"
                        var plotStyles = A.Cdoc.Styles.GroupPlotStyles;
                        StyleItem? priorityPlotItem = null;

                        foreach (ObjectId styleId in plotStyles)
                        {
                            if (tr.GetObject(styleId, OpenMode.ForWrite) is GroupPlotStyle style)
                            {
                                var styleItem = new StyleItem(style.Name, styleId);
                                cmbPlotStyle.Items.Add(styleItem);

                                // Check for priority style
                                if (style.Name == "A3 SECTION FIT ALL")
                                {
                                    priorityPlotItem = styleItem;
                                }
                            }
                        }

                        // Set priority or first item
                        if (priorityPlotItem != null)
                        {
                            cmbPlotStyle.SelectedItem = priorityPlotItem;
                            PlotStyleId = priorityPlotItem.StyleId;
                        }
                        else if (cmbPlotStyle.Items.Count > 0)
                        {
                            cmbPlotStyle.SelectedIndex = 0;
                        }

                        // Load Section Styles for DataGridView
                        LoadSectionStyles(tr);

                        // Load Band Set Styles for section views
                        LoadBandSetStyles(tr);

                        tr.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        tr.Abort();
                        throw new System.Exception($"Lỗi trong transaction load styles: {ex.Message}", ex);
                    }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi tải document styles: {ex.Message}");
                MessageBox.Show($"Không thể tải styles từ document: {ex.Message}\nVui lòng kiểm tra document hiện tại có các styles cần thiết.",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadBandSetStyles(Transaction tr)
        {
            try
            {
                // Load available Band Sets for section views
                cmbBandSetStyle.Items.Clear();

                // Add default option
                var defaultItem = new StyleItem("(None)", ObjectId.Null);
                cmbBandSetStyle.Items.Add(defaultItem);
                cmbBandSetStyle.SelectedItem = defaultItem;

                int bandSetCount = 0;

                // Try Method 1: SectionViewBandSetStyles
                try
                {
                    var bandSetStyles = A.Cdoc.Styles.SectionViewBandSetStyles;
                    foreach (ObjectId bandSetId in bandSetStyles)
                    {
                        try
                        {
                            var bandSet = tr.GetObject(bandSetId, OpenMode.ForRead);
                            var nameProperty = bandSet.GetType().GetProperty("Name");
                            if (nameProperty != null)
                            {
                                string bandSetName = nameProperty.GetValue(bandSet)?.ToString() ?? "Unknown";
                                cmbBandSetStyle.Items.Add(new StyleItem($"[BandSet] {bandSetName}", bandSetId));
                                bandSetCount++;
                            }
                        }
                        catch { }
                    }
                    A.Ed.WriteMessage($"\n[1] SectionViewBandSetStyles: {bandSetCount}");
                }
                catch (System.Exception ex) { A.Ed.WriteMessage($"\n[1] Failed: {ex.Message}"); }



                // Try Method 3: Section Data Band Styles
                try
                {
                    var dataStyles = A.Cdoc.Styles.BandStyles.SectionViewSectionDataBandStyles;
                    int dataCount = 0;
                    foreach (ObjectId styleId in dataStyles)
                    {
                        try
                        {
                            var entity = tr.GetObject(styleId, OpenMode.ForRead);
                            var nameProperty = entity.GetType().GetProperty("Name");
                            if (nameProperty != null)
                            {
                                string styleName = nameProperty.GetValue(entity)?.ToString() ?? "Unknown";
                                cmbBandSetStyle.Items.Add(new StyleItem($"[Data] {styleName}", styleId));
                                dataCount++;
                            }
                        }
                        catch { }
                    }
                    A.Ed.WriteMessage($"\n[3] SectionViewSectionDataBandStyles: {dataCount}");
                    bandSetCount += dataCount;
                }
                catch (System.Exception ex) { A.Ed.WriteMessage($"\n[3] Failed: {ex.Message}"); }

                // Try Method 4: Profile View Band Set Styles (for reference/comparison)
                try
                {
                    var profileBandStyles = A.Cdoc.Styles.ProfileViewBandSetStyles;
                    int profCount = 0;
                    foreach (ObjectId styleId in profileBandStyles)
                    {
                        try
                        {
                            var entity = tr.GetObject(styleId, OpenMode.ForRead);
                            var nameProperty = entity.GetType().GetProperty("Name");
                            if (nameProperty != null)
                            {
                                string styleName = nameProperty.GetValue(entity)?.ToString() ?? "Unknown";
                                // Chỉ hiển thị để tham khảo, không thêm vào combobox vì đây là cho ProfileView
                                profCount++;
                            }
                        }
                        catch { }
                    }
                    A.Ed.WriteMessage($"\n[4] ProfileViewBandSetStyles (tham khảo): {profCount}");
                }
                catch (System.Exception ex) { A.Ed.WriteMessage($"\n[4] Failed: {ex.Message}"); }

                A.Ed.WriteMessage($"\nTổng cộng: {cmbBandSetStyle.Items.Count - 1} Band styles.");

                // Nếu không có band styles, thông báo cho user
                if (bandSetCount == 0)
                {
                    A.Ed.WriteMessage($"\n⚠️ Không tìm thấy Band Styles trong document.");
                    A.Ed.WriteMessage($"\n💡 Sử dụng các tùy chọn 'Thêm elevation bands' và 'Thêm distance bands' thay thế.");
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi tải Band Sets: {ex.Message}");
            }
        }

        private void LoadSectionStyles(Transaction tr)
        {
            var sectionStyleColumn = dgvSectionSources.Columns["Style"] as DataGridViewComboBoxColumn;
            if (sectionStyleColumn != null)
            {
                sectionStyleColumn.Items.Clear();

                // Add a default "No Style" option
                sectionStyleColumn.Items.Add(new StyleItem("(No Style)", ObjectId.Null));

                // Load Section Styles theo loại nguồn
                try
                {
                    var sectionStyles = A.Cdoc.Styles.SectionStyles;
                    foreach (ObjectId styleId in sectionStyles)
                    {
                        if (tr.GetObject(styleId, OpenMode.ForRead) is SectionStyle style)
                        {
                            sectionStyleColumn.Items.Add(new StyleItem($"[Section] {style.Name}", styleId));
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\nKhông thể load Section Styles: {ex.Message}");
                }

                // Load Code Set Styles for corridors theo loại nguồn
                try
                {
                    var codeSetStyles = A.Cdoc.Styles.CodeSetStyles;
                    foreach (ObjectId styleId in codeSetStyles)
                    {
                        if (tr.GetObject(styleId, OpenMode.ForRead) is CodeSetStyle style)
                        {
                            sectionStyleColumn.Items.Add(new StyleItem($"[CodeSet] {style.Name}", styleId));
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\nKhông thể load Code Set Styles: {ex.Message}");
                }

                // Thiết lập thuộc tính để ComboBox hoạt động đúng
                sectionStyleColumn.ValueType = typeof(StyleItem);
                sectionStyleColumn.DataSource = null; // Đảm bảo không sử dụng DataSource
                sectionStyleColumn.DisplayMember = "Name";
                sectionStyleColumn.ValueMember = "StyleId";
            }
        }

        private void BtnSelectAlignment_Click(object sender, EventArgs e)
        {
            try
            {
                this.Hide();

                // Use separate transaction for alignment selection
                ObjectId alignmentId = ObjectId.Null;
                string alignmentName = "";

                try
                {
                    alignmentId = UserInput.GAlignmentId("\nChọn tim đường để vẽ trắc ngang:");

                    if (alignmentId != ObjectId.Null)
                    {
                        using (var tr = A.Db.TransactionManager.StartTransaction())
                        {
                            if (tr.GetObject(alignmentId, OpenMode.ForWrite) is Alignment alignment)
                            {
                                AlignmentId = alignmentId;
                                alignmentName = alignment.Name ?? "Unnamed Alignment";
                            }
                            tr.Commit();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Lỗi khi chọn alignment: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                this.Show();

                // Update UI after showing form
                if (alignmentId != ObjectId.Null)
                {
                    txtAlignmentName.Text = alignmentName;
                    // Auto-refresh section sources when alignment is selected
                    RefreshSectionSources();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi chọn alignment: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Show();
            }
        }

        private void BtnSelectPlacementPoint_Click(object sender, EventArgs e)
        {
            try
            {
                this.Hide();
                Point3d point = UserInput.GPoint("\nChọn vị trí điểm để đặt trắc ngang:");

                if (point != Point3d.Origin)
                {
                    PlacementPoint = point;
                    txtPlacementPoint.Text = $"X: {point.X:F3}, Y: {point.Y:F3}, Z: {point.Z:F3}";
                }
                this.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi chọn điểm đặt: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Show();
            }
        }

        private void BtnRefreshSources_Click(object sender, EventArgs e)
        {
            RefreshSectionSources();
        }

        private void RefreshSectionSources()
        {
            dgvSectionSources.Rows.Clear();

            if (AlignmentId == ObjectId.Null)
            {
                MessageBox.Show("Vui lòng chọn alignment trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var tr = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        var alignment = tr.GetObject(AlignmentId, OpenMode.ForWrite) as Alignment;
                        if (alignment?.GetSampleLineGroupIds().Count > 0)
                        {
                            var sampleLineGroupId = alignment.GetSampleLineGroupIds()[0];
                            var sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
                            var sectionSources = sampleLineGroup?.GetSectionSources();

                            if (sectionSources != null)
                            {
                                foreach (SectionSource sectionSource in sectionSources)
                                {
                                    var config = new SectionSourceConfig
                                    {
                                        SourceId = sectionSource.SourceId,
                                        SourceType = sectionSource.SourceType.ToString(),
                                        SourceName = GetSourceName(sectionSource.SourceId, tr),
                                        UseSource = ShouldUseSourceByDefault(sectionSource, tr, alignment.Name),
                                        StyleId = GetDefaultStyleForSource(sectionSource, tr, alignment.Name)
                                    };

                                    AddSectionSourceToGrid(config);
                                }
                            }
                        }
                        tr.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        tr.Abort();
                        throw new System.Exception($"Lỗi trong transaction refresh sources: {ex.Message}", ex);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi làm mới section sources: {ex.Message}\nVui lòng kiểm tra alignment có sample line group.",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetSourceName(ObjectId sourceId, Transaction tr)
        {
            try
            {
                var entity = tr.GetObject(sourceId, OpenMode.ForWrite);
                if (entity is TinSurface surface)
                    return surface.Name ?? "Unnamed Surface";
                if (entity is Corridor corridor)
                    return corridor.Name ?? "Unnamed Corridor";
                return "Unknown Source";
            }
            catch
            {
                return "Error Reading Source";
            }
        }

        private bool ShouldUseSourceByDefault(SectionSource sectionSource, Transaction tr, string alignmentName)
        {
            if (!chkAutoConfigureSources.Checked)
                return true;

            try
            {
                string sourceName = GetSourceName(sectionSource.SourceId, tr);

                if (sectionSource.SourceType == SectionSourceType.TinSurface)
                {
                    // Case-insensitive comparison for surface names
                    return sourceName.Contains("EG", StringComparison.OrdinalIgnoreCase) ||
                           sourceName.Contains("TN", StringComparison.OrdinalIgnoreCase) ||
                           sourceName.Contains("top", StringComparison.OrdinalIgnoreCase) ||
                           sourceName.Contains("datum", StringComparison.OrdinalIgnoreCase);
                }
                if (sectionSource.SourceType == SectionSourceType.Corridor)
                {
                    return sourceName.Contains(alignmentName, StringComparison.OrdinalIgnoreCase);
                }
                if (sectionSource.SourceType == SectionSourceType.CorridorSurface)
                {
                    // Only select CorridorSurface that belongs to the selected alignment
                    // e.g., for alignment "D5", only select "Corridor_D5_Top" or "Corridor_D5_Datum"
                    // NOT "Corridor_N8-L_Datum" or "Corridor_N7-L_Top"
                    bool belongsToAlignment = sourceName.Contains(alignmentName, StringComparison.OrdinalIgnoreCase);
                    bool isTopOrDatum = sourceName.Contains("top", StringComparison.OrdinalIgnoreCase) ||
                                        sourceName.Contains("datum", StringComparison.OrdinalIgnoreCase);
                    return belongsToAlignment && isTopOrDatum;
                }
                return sectionSource.SourceType == SectionSourceType.Material;
            }
            catch
            {
                return true;
            }
        }

        private ObjectId GetDefaultStyleForSource(SectionSource sectionSource, Transaction tr, string alignmentName)
        {
            try
            {
                string sourceName = GetSourceName(sectionSource.SourceId, tr);

                if (sectionSource.SourceType == SectionSourceType.TinSurface)
                {
                    if (sourceName.Contains("EG") || sourceName.Contains("TN"))
                    {
                        try { return A.Cdoc.Styles.SectionStyles["1.TN Ground"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                    if (sourceName.Contains("top"))
                    {
                        try { return A.Cdoc.Styles.SectionStyles["2.Top Ground"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                    if (sourceName.Contains("datum"))
                    {
                        try { return A.Cdoc.Styles.SectionStyles["3.Datum Ground"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                }
                else if (sectionSource.SourceType == SectionSourceType.Corridor)
                {
                    // Priority 1: All Codes 1-1000 style for corridor sources
                    ObjectId allCodesStyleId = GetAllCodesStyleForSectionSource();
                    if (allCodesStyleId != ObjectId.Null)
                    {
                        A.Ed.WriteMessage($"\n  ✅ Corridor sử dụng style: All Codes 1-1000");
                        return allCodesStyleId;
                    }

                    // Fallback for corridor
                    if (sourceName.Contains(alignmentName))
                    {
                        try { return A.Cdoc.Styles.CodeSetStyles["1. All Codes 1-1000"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                }
                else if (sectionSource.SourceType == SectionSourceType.CorridorSurface)
                {
                    // For corridor surfaces, use Section Styles instead of CodeSet
                    if (sourceName.Contains("top") || sourceName.Contains("Top"))
                    {
                        try
                        {
                            A.Ed.WriteMessage($"\n  ✅ CorridorSurface (Top) sử dụng style: 2.Top Ground");
                            return A.Cdoc.Styles.SectionStyles["2.Top Ground"];
                        }
                        catch { /* Style not found, continue to fallback */ }
                    }
                    if (sourceName.Contains("datum") || sourceName.Contains("Datum"))
                    {
                        try
                        {
                            A.Ed.WriteMessage($"\n  ✅ CorridorSurface (Datum) sử dụng style: 3.Datum Ground");
                            return A.Cdoc.Styles.SectionStyles["3.Datum Ground"];
                        }
                        catch { /* Style not found, continue to fallback */ }
                    }

                    // Default fallback for other corridor surfaces - try Top Ground first
                    try
                    {
                        A.Ed.WriteMessage($"\n  ✅ CorridorSurface (Default) sử dụng style: 2.Top Ground");
                        return A.Cdoc.Styles.SectionStyles["2.Top Ground"];
                    }
                    catch
                    {
                        try
                        {
                            A.Ed.WriteMessage($"\n  ✅ CorridorSurface (Fallback) sử dụng style: 3.Datum Ground");
                            return A.Cdoc.Styles.SectionStyles["3.Datum Ground"];
                        }
                        catch { /* Style not found, continue to fallback */ }
                    }
                }
                else if (sectionSource.SourceType == SectionSourceType.Material)
                {
                    // Default section style for material - sử dụng section style thông thường
                    try { return A.Cdoc.Styles.SectionStyles["Standard"]; }
                    catch { /* Style not found, continue to fallback */ }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi lấy default style: {ex.Message}");
            }

            // Return ObjectId.Null as fallback - will use "No Style" option
            return ObjectId.Null;
        }

        // Helper method to get All Codes style for section sources
        private ObjectId GetAllCodesStyleForSectionSource()
        {
            try
            {
                // Try CodeSet styles first (preferred for corridor sources)
                var codeSetStyles = A.Cdoc.Styles.CodeSetStyles;

                // Priority style names for All Codes
                string[] allCodesStyleNames = {
                    "1. All Codes 1-1000",
                    "All Codes 1-1000",
                    "1.All Codes 1-1000",
                    "All Codes",
                    "1. All Codes",
                    "ALL CODES 1-1000"
                };

                foreach (string styleName in allCodesStyleNames)
                {
                    if (codeSetStyles.Contains(styleName))
                    {
                        A.Ed.WriteMessage($"\n  ✅ Tìm thấy CodeSet style: {styleName}");
                        return codeSetStyles[styleName];
                    }
                }

                A.Ed.WriteMessage($"\n  ⚠️ Không tìm thấy All Codes 1-1000 style trong CodeSet");
                return ObjectId.Null;
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi tìm All Codes style: {ex.Message}");
                return ObjectId.Null;
            }
        }

        private void AddSectionSourceToGrid(SectionSourceConfig config)
        {
            try
            {
                int rowIndex = dgvSectionSources.Rows.Add();
                var row = dgvSectionSources.Rows[rowIndex];

                row.Cells["Use"].Value = config.UseSource;
                row.Cells["SourceType"].Value = config.SourceType;
                row.Cells["SourceName"].Value = config.SourceName;

                // Set style - tìm và gán StyleItem thích hợp
                var styleColumn = dgvSectionSources.Columns["Style"] as DataGridViewComboBoxColumn;
                if (styleColumn != null)
                {
                    StyleItem? selectedStyleItem = null;

                    if (config.StyleId != ObjectId.Null && config.StyleId.IsValid)
                    {
                        // Tìm StyleItem có StyleId tương ứng
                        foreach (StyleItem styleItem in styleColumn.Items)
                        {
                            if (styleItem.StyleId == config.StyleId)
                            {
                                selectedStyleItem = styleItem;
                                break;
                            }
                        }
                    }

                    // Nếu không tìm thấy style tương ứng, chọn "(No Style)"
                    if (selectedStyleItem == null && styleColumn.Items.Count > 0)
                    {
                        selectedStyleItem = styleColumn.Items[0] as StyleItem; // "(No Style)" item
                    }

                    // Gán giá trị cho cell
                    if (selectedStyleItem != null)
                    {
                        row.Cells["Style"].Value = selectedStyleItem;
                    }
                }

                row.Tag = config;
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi thêm section source vào grid: {ex.Message}");
                // Add row without style if there's an error
                try
                {
                    int rowIndex = dgvSectionSources.Rows.Add();
                    var row = dgvSectionSources.Rows[rowIndex];
                    row.Cells["Use"].Value = config.UseSource;
                    row.Cells["SourceType"].Value = config.SourceType;
                    row.Cells["SourceName"].Value = config.SourceName;

                    // Set default "(No Style)" if available
                    var styleColumn = dgvSectionSources.Columns["Style"] as DataGridViewComboBoxColumn;
                    if (styleColumn?.Items.Count > 0)
                    {
                        row.Cells["Style"].Value = styleColumn.Items[0];
                    }

                    row.Tag = config;
                }
                catch
                {
                    // If even basic row adding fails, just log and continue
                    A.Ed.WriteMessage($"\nKhông thể thêm row cho source: {ex.Message}");
                }
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (AlignmentId == ObjectId.Null)
                {
                    MessageBox.Show("Vui lòng chọn alignment.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (PlacementPoint == Point3d.Origin)
                {
                    MessageBox.Show("Vui lòng chọn điểm đặt trắc ngang.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get user inputs
                LayoutTemplatePath = cmbLayoutTemplate.Text;
                LayoutName = cmbLayoutName.SelectedItem?.ToString() ?? "A3-TN-1-200";

                if (cmbSectionViewStyle.SelectedItem is StyleItem sectionViewStyle)
                    SectionViewStyleId = sectionViewStyle.StyleId;

                if (cmbPlotStyle.SelectedItem is StyleItem plotStyle)
                    PlotStyleId = plotStyle.StyleId;

                CreateMaterialList = chkCreateMaterialList.Checked;
                MaterialListName = txtMaterialListName.Text;

                CreateVolumeTable = chkCreateVolumeTable.Checked;
                TablePosition = cmbTablePosition.SelectedItem?.ToString() ?? "TopLeft";

                AddElevationBands = chkAddElevationBands.Checked;
                AddDistanceBands = chkAddDistanceBands.Checked;
                ImportBandSet = chkImportBandSet.Checked;

                // Get selected band set style
                if (cmbBandSetStyle.SelectedItem is StyleItem bandSetStyle)
                    BandSetStyleId = bandSetStyle.StyleId;

                // Get section sources
                SectionSources.Clear();
                foreach (DataGridViewRow row in dgvSectionSources.Rows)
                {
                    if (row.Tag is SectionSourceConfig config)
                    {
                        config.UseSource = Convert.ToBoolean(row.Cells["Use"].Value ?? false);
                        if (row.Cells["Style"].Value is StyleItem selectedStyle)
                        {
                            config.StyleId = selectedStyle.StyleId;
                        }
                        SectionSources.Add(config);
                    }
                }

                // Get weeding values
                WeedingTop = (double)nudWeedingTop.Value;
                WeedingTN = (double)nudWeedingTN.Value;

                // Save values for next time
                SaveLastUsedValues();

                DialogResultOK = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChkImportBandSet_CheckedChanged(object sender, EventArgs e)
        {
            bool useBandSet = chkImportBandSet.Checked;

            // Enable/disable band set controls
            lblBandSetStyle.Enabled = useBandSet;
            cmbBandSetStyle.Enabled = useBandSet;

            // Disable individual band controls when using band set
            chkAddElevationBands.Enabled = !useBandSet;
            chkAddDistanceBands.Enabled = !useBandSet;

            if (useBandSet)
            {
                // Uncheck individual bands when using band set
                chkAddElevationBands.Checked = false;
                chkAddDistanceBands.Checked = false;
                A.Ed.WriteMessage("\nSử dụng Band Set - các band riêng lẻ sẽ bị vô hiệu hóa.");
            }
            else
            {
                // Re-enable individual bands
                chkAddElevationBands.Checked = true;
                chkAddDistanceBands.Checked = true;
                A.Ed.WriteMessage("\nSử dụng bands riêng lẻ.");
            }
        }

        private void RestoreLastUsedValues()
        {
            try
            {
                // Restore weeding values
                nudWeedingTop.Value = (decimal)_lastWeedingTop;
                nudWeedingTN.Value = (decimal)_lastWeedingTN;

                // Restore band options
                chkAddElevationBands.Checked = _lastAddElevationBands;
                chkAddDistanceBands.Checked = _lastAddDistanceBands;
                chkImportBandSet.Checked = _lastImportBandSet;

                // Restore band set style selection
                if (!string.IsNullOrEmpty(_lastBandSetStyleName))
                {
                    for (int i = 0; i < cmbBandSetStyle.Items.Count; i++)
                    {
                        if (cmbBandSetStyle.Items[i] is StyleItem item && item.Name == _lastBandSetStyleName)
                        {
                            cmbBandSetStyle.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Restore material list options
                chkCreateMaterialList.Checked = _lastCreateMaterialList;
                txtMaterialListName.Text = _lastMaterialListName;

                // Restore volume table options
                chkCreateVolumeTable.Checked = _lastCreateVolumeTable;
                int tableIdx = cmbTablePosition.Items.IndexOf(_lastTablePosition);
                if (tableIdx >= 0) cmbTablePosition.SelectedIndex = tableIdx;

                // Restore auto configure sources
                chkAutoConfigureSources.Checked = _lastAutoConfigureSources;

                // Restore layout template and name
                cmbLayoutTemplate.Text = _lastLayoutTemplatePath;
                int layoutIdx = cmbLayoutName.Items.IndexOf(_lastLayoutName);
                if (layoutIdx >= 0) cmbLayoutName.SelectedIndex = layoutIdx;

                // Restore section view style
                if (!string.IsNullOrEmpty(_lastSectionViewStyleName))
                {
                    for (int i = 0; i < cmbSectionViewStyle.Items.Count; i++)
                    {
                        if (cmbSectionViewStyle.Items[i] is StyleItem item && item.Name == _lastSectionViewStyleName)
                        {
                            cmbSectionViewStyle.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Restore plot style
                if (!string.IsNullOrEmpty(_lastPlotStyleName))
                {
                    for (int i = 0; i < cmbPlotStyle.Items.Count; i++)
                    {
                        if (cmbPlotStyle.Items[i] is StyleItem item && item.Name == _lastPlotStyleName)
                        {
                            cmbPlotStyle.SelectedIndex = i;
                            break;
                        }
                    }
                }

                A.Ed.WriteMessage("\n\u0110\u00e3 kh\u00f4i ph\u1ee5c c\u00e1c th\u00f4ng s\u1ed1 t\u1eeb l\u1ea7n s\u1eed d\u1ee5ng tr\u01b0\u1edbc.");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nL\u1ed7i kh\u00f4i ph\u1ee5c gi\u00e1 tr\u1ecb: {ex.Message}");
            }
        }

        private void SaveLastUsedValues()
        {
            try
            {
                // Save weeding values
                _lastWeedingTop = (double)nudWeedingTop.Value;
                _lastWeedingTN = (double)nudWeedingTN.Value;

                // Save band options
                _lastAddElevationBands = chkAddElevationBands.Checked;
                _lastAddDistanceBands = chkAddDistanceBands.Checked;
                _lastImportBandSet = chkImportBandSet.Checked;

                // Save band set style name
                if (cmbBandSetStyle.SelectedItem is StyleItem bandStyle)
                    _lastBandSetStyleName = bandStyle.Name;

                // Save material list options
                _lastCreateMaterialList = chkCreateMaterialList.Checked;
                _lastMaterialListName = txtMaterialListName.Text;

                // Save volume table options
                _lastCreateVolumeTable = chkCreateVolumeTable.Checked;
                _lastTablePosition = cmbTablePosition.SelectedItem?.ToString() ?? "TopLeft";

                // Save auto configure sources
                _lastAutoConfigureSources = chkAutoConfigureSources.Checked;

                // Save layout template and name
                _lastLayoutTemplatePath = cmbLayoutTemplate.Text;
                _lastLayoutName = cmbLayoutName.SelectedItem?.ToString() ?? "A3-TN-1-200";

                // Save section view style name
                if (cmbSectionViewStyle.SelectedItem is StyleItem sectionStyle)
                    _lastSectionViewStyleName = sectionStyle.Name;

                // Save plot style name
                if (cmbPlotStyle.SelectedItem is StyleItem plotStyle)
                    _lastPlotStyleName = plotStyle.Name;

                A.Ed.WriteMessage("\n\u0110\u00e3 l\u01b0u c\u00e1c th\u00f4ng s\u1ed1 cho l\u1ea7n s\u1eed d\u1ee5ng ti\u1ebfp theo.");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nL\u1ed7i l\u01b0u gi\u00e1 tr\u1ecb: {ex.Message}");
            }
        }

        private void CmbBandSetStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbBandSetStyle.SelectedItem is StyleItem selectedStyle)
            {
                BandSetStyleId = selectedStyle.StyleId;
                A.Ed.WriteMessage($"\nĐã chọn Band Set Style: {selectedStyle.Name}");
            }
        }
    }

    // Helper classes
    public class SectionSourceConfig
    {
        public ObjectId SourceId { get; set; } = ObjectId.Null;
        public string SourceType { get; set; } = "";
        public string SourceName { get; set; } = "";
        public bool UseSource { get; set; } = true;
        public ObjectId StyleId { get; set; } = ObjectId.Null;
    }

    public class StyleItem
    {
        public string Name { get; set; }
        public ObjectId StyleId { get; set; }

        public StyleItem(string name, ObjectId styleId)
        {
            Name = name;
            StyleId = styleId;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is StyleItem other)
            {
                return StyleId == other.StyleId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return StyleId.GetHashCode();
        }
    }
}
