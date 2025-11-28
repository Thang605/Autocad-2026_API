using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using MyFirstProject.Extensions;

namespace MyFirstProject.Civil_Tool
{
    public partial class CorridorSurfaceForm : Form
    {
        // Form controls
        private GroupBox groupBoxCorridor;
        private System.Windows.Forms.Label lblCorridor;
        private ListBox lstCorridors;
        private System.Windows.Forms.Button btnSelectCorridors;
        private System.Windows.Forms.Button btnSelectAllCorridors;
        private System.Windows.Forms.Button btnClearSelection;
        
        private GroupBox groupBoxCorridorSurface;
        private CheckBox chkCreateTopSurface;
        private CheckBox chkCreateDatumSurface;
        private System.Windows.Forms.Label lblTopSurfaceName;
        private TextBox txtTopSurfaceName;
        private System.Windows.Forms.Label lblDatumSurfaceName;
        private TextBox txtDatumSurfaceName;
        private System.Windows.Forms.Label lblTopSurfaceStyle;
        private ComboBox cmbTopSurfaceStyle;
        private System.Windows.Forms.Label lblDatumSurfaceStyle;
        private ComboBox cmbDatumSurfaceStyle;
        
        private GroupBox groupBoxOptions;
        private CheckBox chkAddToSectionSources;
        private CheckBox chkRebuildCorridor;
        
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        // Properties to store user selections
        public List<ObjectId> CorridorIds { get; private set; } = new List<ObjectId>();
        public List<string> CorridorNames { get; private set; } = new List<string>();
        public bool CreateTopSurface { get; private set; } = true;
        public bool CreateDatumSurface { get; private set; } = true;
        public string TopSurfaceName { get; private set; } = "Top";
        public string DatumSurfaceName { get; private set; } = "Datum";
        public ObjectId TopSurfaceStyleId { get; private set; } = ObjectId.Null;
        public ObjectId DatumSurfaceStyleId { get; private set; } = ObjectId.Null;
        public bool AddToSectionSources { get; private set; } = true;
        public bool RebuildCorridor { get; private set; } = true;
        public bool DialogResultOK { get; private set; } = false;

        public CorridorSurfaceForm()
        {
            InitializeComponent();
            LoadStyles();
            LoadAvailableCorridors();
        }

        private void InitializeComponent()
        {
            // Form setup
            this.Text = "Tạo Corridor Surface cho nhiều Corridor";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int yPos = 10;

            // Corridor group (updated for multiple selection)
            this.groupBoxCorridor = new GroupBox();
            this.groupBoxCorridor.Text = "1. Chọn corridors";
            this.groupBoxCorridor.Location = new System.Drawing.Point(10, yPos);
            this.groupBoxCorridor.Size = new Size(660, 140);

            this.lblCorridor = new System.Windows.Forms.Label();
            this.lblCorridor.Text = "Danh sách corridors:";
            this.lblCorridor.Location = new System.Drawing.Point(15, 25);
            this.lblCorridor.Size = new Size(120, 23);

            this.lstCorridors = new ListBox();
            this.lstCorridors.Location = new System.Drawing.Point(15, 50);
            this.lstCorridors.Size = new Size(550, 80);
            this.lstCorridors.SelectionMode = SelectionMode.MultiExtended;
            this.lstCorridors.SelectedIndexChanged += LstCorridors_SelectedIndexChanged;

            this.btnSelectCorridors = new System.Windows.Forms.Button();
            this.btnSelectCorridors.Text = "Chọn";
            this.btnSelectCorridors.Location = new System.Drawing.Point(575, 50);
            this.btnSelectCorridors.Size = new Size(70, 25);
            this.btnSelectCorridors.Click += BtnSelectCorridors_Click;

            this.btnSelectAllCorridors = new System.Windows.Forms.Button();
            this.btnSelectAllCorridors.Text = "Chọn tất cả";
            this.btnSelectAllCorridors.Location = new System.Drawing.Point(575, 80);
            this.btnSelectAllCorridors.Size = new Size(70, 25);
            this.btnSelectAllCorridors.Click += BtnSelectAllCorridors_Click;

            this.btnClearSelection = new System.Windows.Forms.Button();
            this.btnClearSelection.Text = "Xóa chọn";
            this.btnClearSelection.Location = new System.Drawing.Point(575, 110);
            this.btnClearSelection.Size = new Size(70, 25);
            this.btnClearSelection.Click += BtnClearSelection_Click;

            this.groupBoxCorridor.Controls.AddRange(new Control[] { 
                lblCorridor, lstCorridors, btnSelectCorridors, btnSelectAllCorridors, btnClearSelection });
            this.Controls.Add(groupBoxCorridor);

            yPos += 150;

            // Corridor Surface group
            this.groupBoxCorridorSurface = new GroupBox();
            this.groupBoxCorridorSurface.Text = "2. Cấu hình Corridor Surface";
            this.groupBoxCorridorSurface.Location = new System.Drawing.Point(10, yPos);
            this.groupBoxCorridorSurface.Size = new Size(660, 150);

            // Top Surface
            this.chkCreateTopSurface = new CheckBox();
            this.chkCreateTopSurface.Text = "Tạo Top Surface";
            this.chkCreateTopSurface.Location = new System.Drawing.Point(15, 25);
            this.chkCreateTopSurface.Size = new Size(140, 23);
            this.chkCreateTopSurface.Checked = true;

            this.lblTopSurfaceName = new System.Windows.Forms.Label();
            this.lblTopSurfaceName.Text = "Tên:";
            this.lblTopSurfaceName.Location = new System.Drawing.Point(160, 25);
            this.lblTopSurfaceName.Size = new Size(40, 23);

            this.txtTopSurfaceName = new TextBox();
            this.txtTopSurfaceName.Location = new System.Drawing.Point(200, 25);
            this.txtTopSurfaceName.Size = new Size(100, 23);
            this.txtTopSurfaceName.Text = "Top";

            this.lblTopSurfaceStyle = new System.Windows.Forms.Label();
            this.lblTopSurfaceStyle.Text = "Style:";
            this.lblTopSurfaceStyle.Location = new System.Drawing.Point(310, 25);
            this.lblTopSurfaceStyle.Size = new Size(40, 23);

            this.cmbTopSurfaceStyle = new ComboBox();
            this.cmbTopSurfaceStyle.Location = new System.Drawing.Point(355, 25);
            this.cmbTopSurfaceStyle.Size = new Size(280, 23);
            this.cmbTopSurfaceStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbTopSurfaceStyle.SelectedIndexChanged += CmbTopSurfaceStyle_SelectedIndexChanged;

            // Datum Surface
            this.chkCreateDatumSurface = new CheckBox();
            this.chkCreateDatumSurface.Text = "Tạo Datum Surface";
            this.chkCreateDatumSurface.Location = new System.Drawing.Point(15, 55);
            this.chkCreateDatumSurface.Size = new Size(140, 23);
            this.chkCreateDatumSurface.Checked = true;

            this.lblDatumSurfaceName = new System.Windows.Forms.Label();
            this.lblDatumSurfaceName.Text = "Tên:";
            this.lblDatumSurfaceName.Location = new System.Drawing.Point(160, 55);
            this.lblDatumSurfaceName.Size = new Size(40, 23);

            this.txtDatumSurfaceName = new TextBox();
            this.txtDatumSurfaceName.Location = new System.Drawing.Point(200, 55);
            this.txtDatumSurfaceName.Size = new Size(100, 23);
            this.txtDatumSurfaceName.Text = "Datum";

            this.lblDatumSurfaceStyle = new System.Windows.Forms.Label();
            this.lblDatumSurfaceStyle.Text = "Style:";
            this.lblDatumSurfaceStyle.Location = new System.Drawing.Point(310, 55);
            this.lblDatumSurfaceStyle.Size = new Size(40, 23);

            this.cmbDatumSurfaceStyle = new ComboBox();
            this.cmbDatumSurfaceStyle.Location = new System.Drawing.Point(355, 55);
            this.cmbDatumSurfaceStyle.Size = new Size(280, 23);
            this.cmbDatumSurfaceStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbDatumSurfaceStyle.SelectedIndexChanged += CmbDatumSurfaceStyle_SelectedIndexChanged;

            // Info label (updated text)
            var lblInfo = new System.Windows.Forms.Label();
            lblInfo.Text = "Tên surface sẽ theo định dạng: [CorridorName]-L_Top và [CorridorName]-L_Datum";
            lblInfo.Location = new System.Drawing.Point(15, 85);
            lblInfo.Size = new Size(630, 23);
            lblInfo.ForeColor = Color.DarkBlue;
            lblInfo.Font = new System.Drawing.Font(lblInfo.Font, FontStyle.Italic);

            // Configuration note
            var lblConfig = new System.Windows.Forms.Label();
            lblConfig.Text = "Lưu ý: Sẽ áp dụng cùng cấu hình cho tất cả corridors được chọn";
            lblConfig.Location = new System.Drawing.Point(15, 110);
            lblConfig.Size = new Size(630, 23);
            lblConfig.ForeColor = Color.DarkGreen;
            lblConfig.Font = new System.Drawing.Font(lblConfig.Font, FontStyle.Italic);

            this.groupBoxCorridorSurface.Controls.AddRange(new Control[] { 
                chkCreateTopSurface, chkCreateDatumSurface, 
                lblTopSurfaceName, txtTopSurfaceName, lblTopSurfaceStyle, cmbTopSurfaceStyle,
                lblDatumSurfaceName, txtDatumSurfaceName, lblDatumSurfaceStyle, cmbDatumSurfaceStyle,
                lblInfo, lblConfig });
            this.Controls.Add(groupBoxCorridorSurface);

            yPos += 160;

            // Options group
            this.groupBoxOptions = new GroupBox();
            this.groupBoxOptions.Text = "3. Tùy chọn";
            this.groupBoxOptions.Location = new System.Drawing.Point(10, yPos);
            this.groupBoxOptions.Size = new Size(660, 80);

            this.chkAddToSectionSources = new CheckBox();
            this.chkAddToSectionSources.Text = "Tự động thêm vào Section Sources";
            this.chkAddToSectionSources.Location = new System.Drawing.Point(15, 25);
            this.chkAddToSectionSources.Size = new Size(250, 23);
            this.chkAddToSectionSources.Checked = true;

            this.chkRebuildCorridor = new CheckBox();
            this.chkRebuildCorridor.Text = "Rebuild corridors sau khi tạo";
            this.chkRebuildCorridor.Location = new System.Drawing.Point(15, 50);
            this.chkRebuildCorridor.Size = new Size(250, 23);
            this.chkRebuildCorridor.Checked = true;

            this.groupBoxOptions.Controls.AddRange(new Control[] { chkAddToSectionSources, chkRebuildCorridor });
            this.Controls.Add(groupBoxOptions);

            yPos += 90;

            // OK/Cancel buttons
            this.btnOK = new System.Windows.Forms.Button();
            this.btnOK.Text = "Tạo Surface";
            this.btnOK.Location = new System.Drawing.Point(500, yPos);
            this.btnOK.Size = new Size(80, 35);
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, FontStyle.Bold);
            this.btnOK.Click += BtnOK_Click;

            this.btnCancel = new System.Windows.Forms.Button();
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new System.Drawing.Point(590, yPos);
            this.btnCancel.Size = new Size(80, 35);
            this.btnCancel.Click += (sender, e) => { 
                DialogResultOK = false;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] { btnOK, btnCancel });

            // Adjust form height
            this.Height = yPos + 80;
        }

        private void LoadAvailableCorridors()
        {
            try
            {
                lstCorridors.Items.Clear();
                
                using (var tr = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        foreach (ObjectId corridorId in A.Cdoc.CorridorCollection)
                        {
                            if (tr.GetObject(corridorId, OpenMode.ForRead) is Corridor corridor)
                            {
                                var corridorItem = new CorridorItem(corridor.Name ?? "Unnamed Corridor", corridorId);
                                lstCorridors.Items.Add(corridorItem);
                            }
                        }
                        
                        A.Ed.WriteMessage($"\nĐã tải {lstCorridors.Items.Count} corridors có sẵn trong document.");
                        
                        tr.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        tr.Abort();
                        throw new System.Exception($"Lỗi trong transaction load corridors: {ex.Message}", ex);
                    }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi tải corridors: {ex.Message}");
                MessageBox.Show($"Không thể tải danh sách corridors: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadStyles()
        {
            try
            {
                using (var tr = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // Load Surface Styles
                        var surfaceStyles = A.Cdoc.Styles.SurfaceStyles;
                        
                        // Clear existing items
                        cmbTopSurfaceStyle.Items.Clear();
                        cmbDatumSurfaceStyle.Items.Clear();
                        
                        // Add default option
                        var defaultItem = new StyleItem("(Default)", ObjectId.Null);
                        cmbTopSurfaceStyle.Items.Add(defaultItem);
                        cmbDatumSurfaceStyle.Items.Add(defaultItem);
                        
                        StyleItem? borderOnlyItem = null;
                        StyleItem? allCodesItem = null;  // New: for corridor sources
                        StyleItem? topPriorityItem = null;
                        StyleItem? datumPriorityItem = null;
                        
                        foreach (ObjectId styleId in surfaceStyles)
                        {
                            if (tr.GetObject(styleId, OpenMode.ForRead) is SurfaceStyle style)
                            {
                                var styleItem = new StyleItem(style.Name, styleId);
                                cmbTopSurfaceStyle.Items.Add(styleItem);
                                cmbDatumSurfaceStyle.Items.Add(styleItem);
                                
                                string styleName = style.Name.ToLower();
                                
                                // Priority 1: BORDER ONLY style for surfaces
                                if (borderOnlyItem == null && (
                                    styleName == "border only" || 
                                    styleName == "borderonly" || 
                                    styleName == "border" ||
                                    styleName == "borders only" ||
                                    styleName == "boundary only" ||
                                    styleName == "outline only"))
                                {
                                    borderOnlyItem = styleItem;
                                }
                                
                                // Priority 1.5: ALL CODES style for corridor sources
                                if (allCodesItem == null && (
                                    styleName == "all codes 1-1000" ||
                                    styleName == "all codes" ||
                                    styleName == "1. all codes 1-1000" ||
                                    styleName.Contains("all codes") && styleName.Contains("1000")))
                                {
                                    allCodesItem = styleItem;
                                }
                                
                                // Priority 2: Type-specific styles
                                if (topPriorityItem == null && (styleName.Contains("top") || styleName.Contains("road") || styleName.Contains("pave")))
                                {
                                    topPriorityItem = styleItem;
                                }
                                if (datumPriorityItem == null && (styleName.Contains("datum") || styleName.Contains("subgrade") || styleName.Contains("formation")))
                                {
                                    datumPriorityItem = styleItem;
                                }
                            }
                        }
                        
                        // Try to load CodeSet styles for corridor surfaces (preferred for corridor sources)
                        try
                        {
                            var codeSetStyles = A.Cdoc.Styles.CodeSetStyles;
                            foreach (ObjectId styleId in codeSetStyles)
                            {
                                if (tr.GetObject(styleId, OpenMode.ForRead) is CodeSetStyle style)
                                {
                                    var styleItem = new StyleItem($"[CodeSet] {style.Name}", styleId);
                                    cmbTopSurfaceStyle.Items.Add(styleItem);
                                    cmbDatumSurfaceStyle.Items.Add(styleItem);
                                    
                                    string styleName = style.Name.ToLower();
                                    
                                    // Priority for All Codes style in CodeSet
                                    if (allCodesItem == null && (
                                        styleName == "all codes 1-1000" ||
                                        styleName == "1. all codes 1-1000" ||
                                        styleName == "all codes" ||
                                        styleName.Contains("all codes") && styleName.Contains("1000")))
                                    {
                                        allCodesItem = styleItem;
                                        A.Ed.WriteMessage($"\n✅ Tìm thấy CodeSet style: {style.Name}");
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            A.Ed.WriteMessage($"\nKhông thể load CodeSet styles: {ex.Message}");
                        }
                        
                        // Set priority items for Top Surface
                        // BORDER ONLY style is now the primary choice for corridor surfaces
                        if (borderOnlyItem != null)
                        {
                            cmbTopSurfaceStyle.SelectedItem = borderOnlyItem;
                            TopSurfaceStyleId = borderOnlyItem.StyleId;
                            A.Ed.WriteMessage($"\n✅ Top Surface: Sử dụng style {borderOnlyItem.Name}");
                        }
                        else if (allCodesItem != null)
                        {
                            cmbTopSurfaceStyle.SelectedItem = allCodesItem;
                            TopSurfaceStyleId = allCodesItem.StyleId;
                            A.Ed.WriteMessage($"\n✅ Top Surface: Sử dụng corridor style (fallback) {allCodesItem.Name}");
                        }
                        else if (topPriorityItem != null)
                        {
                            cmbTopSurfaceStyle.SelectedItem = topPriorityItem;
                            TopSurfaceStyleId = topPriorityItem.StyleId;
                            A.Ed.WriteMessage($"\n⚠️ Top Surface: Sử dụng style {topPriorityItem.Name} (không tìm thấy BORDER ONLY hoặc All Codes)");
                        }
                        else
                        {
                            cmbTopSurfaceStyle.SelectedIndex = 0;
                            A.Ed.WriteMessage($"\n⚠️ Top Surface: Sử dụng style mặc định");
                        }
                        
                        // Set priority items for Datum Surface  
                        // BORDER ONLY style is now the primary choice for corridor surfaces
                        if (borderOnlyItem != null)
                        {
                            cmbDatumSurfaceStyle.SelectedItem = borderOnlyItem;
                            DatumSurfaceStyleId = borderOnlyItem.StyleId;
                            A.Ed.WriteMessage($"\n✅ Datum Surface: Sử dụng style {borderOnlyItem.Name}");
                        }
                        else if (allCodesItem != null)
                        {
                            cmbDatumSurfaceStyle.SelectedItem = allCodesItem;
                            DatumSurfaceStyleId = allCodesItem.StyleId;
                            A.Ed.WriteMessage($"\n✅ Datum Surface: Sử dụng corridor style (fallback) {allCodesItem.Name}");
                        }
                        else if (datumPriorityItem != null)
                        {
                            cmbDatumSurfaceStyle.SelectedItem = datumPriorityItem;
                            DatumSurfaceStyleId = datumPriorityItem.StyleId;
                            A.Ed.WriteMessage($"\n⚠️ Datum Surface: Sử dụng style {datumPriorityItem.Name} (không tìm thấy BORDER ONLY hoặc All Codes)");
                        }
                        else
                        {
                            cmbDatumSurfaceStyle.SelectedIndex = 0;
                            A.Ed.WriteMessage($"\n⚠️ Datum Surface: Sử dụng style mặc định");
                        }
                        
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
                A.Ed.WriteMessage($"\nLỗi khi tải surface styles: {ex.Message}");
            }
        }

        private void LstCorridors_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSelectedCorridors();
        }

        private void BtnSelectCorridors_Click(object sender, EventArgs e)
        {
            try
            {
                this.Hide();
                
                try
                {
                    ObjectId corridorId = UserInput.GCorridorId("\nChọn corridor để thêm vào danh sách:");
                    
                    if (corridorId != ObjectId.Null)
                    {
                        using (var tr = A.Db.TransactionManager.StartTransaction())
                        {
                            if (tr.GetObject(corridorId, OpenMode.ForRead) is Corridor corridor)
                            {
                                string corridorName = corridor.Name ?? "Unnamed Corridor";
                                
                                // Check if already exists
                                bool exists = false;
                                foreach (CorridorItem item in lstCorridors.Items)
                                {
                                    if (item.CorridorId == corridorId)
                                    {
                                        exists = true;
                                        break;
                                    }
                                }
                                
                                if (!exists)
                                {
                                    var corridorItem = new CorridorItem(corridorName, corridorId);
                                    lstCorridors.Items.Add(corridorItem);
                                    lstCorridors.SelectedItem = corridorItem;
                                }
                                else
                                {
                                    MessageBox.Show("Corridor này đã có trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            tr.Commit();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Lỗi khi chọn corridor: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                this.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi chọn corridor: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Show();
            }
        }

        private void BtnSelectAllCorridors_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstCorridors.Items.Count; i++)
            {
                lstCorridors.SetSelected(i, true);
            }
            UpdateSelectedCorridors();
        }

        private void BtnClearSelection_Click(object sender, EventArgs e)
        {
            lstCorridors.ClearSelected();
            UpdateSelectedCorridors();
        }

        private void UpdateSelectedCorridors()
        {
            CorridorIds.Clear();
            CorridorNames.Clear();
            
            foreach (CorridorItem selectedItem in lstCorridors.SelectedItems)
            {
                CorridorIds.Add(selectedItem.CorridorId);
                CorridorNames.Add(selectedItem.CorridorName);
            }
        }

        private void CmbTopSurfaceStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTopSurfaceStyle.SelectedItem is StyleItem selectedStyle)
            {
                TopSurfaceStyleId = selectedStyle.StyleId;
                A.Ed.WriteMessage($"\nĐã chọn Top Surface Style: {selectedStyle.Name}");
            }
        }

        private void CmbDatumSurfaceStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDatumSurfaceStyle.SelectedItem is StyleItem selectedStyle)
            {
                DatumSurfaceStyleId = selectedStyle.StyleId;
                A.Ed.WriteMessage($"\nĐã chọn Datum Surface Style: {selectedStyle.Name}");
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (CorridorIds.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một corridor.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!chkCreateTopSurface.Checked && !chkCreateDatumSurface.Checked)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một loại surface để tạo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get user inputs
                CreateTopSurface = chkCreateTopSurface.Checked;
                CreateDatumSurface = chkCreateDatumSurface.Checked;
                TopSurfaceName = txtTopSurfaceName.Text;
                DatumSurfaceName = txtDatumSurfaceName.Text;
                AddToSectionSources = chkAddToSectionSources.Checked;
                RebuildCorridor = chkRebuildCorridor.Checked;
                
                // Get selected styles
                if (cmbTopSurfaceStyle.SelectedItem is StyleItem topStyle)
                    TopSurfaceStyleId = topStyle.StyleId;
                if (cmbDatumSurfaceStyle.SelectedItem is StyleItem datumStyle)
                    DatumSurfaceStyleId = datumStyle.StyleId;

                DialogResultOK = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Helper class for corridor items in the list
    public class CorridorItem
    {
        public string CorridorName { get; set; }
        public ObjectId CorridorId { get; set; }

        public CorridorItem(string name, ObjectId id)
        {
            CorridorName = name;
            CorridorId = id;
        }

        public override string ToString()
        {
            return CorridorName;
        }
    }
}
