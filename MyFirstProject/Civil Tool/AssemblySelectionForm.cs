using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using MyFirstProject.Extensions;

namespace MyFirstProject
{
    public partial class AssemblySelectionForm : Form
    {
        public ObjectId SelectedAssemblyId { get; private set; }
        public string SelectedAssemblyName { get; private set; } = "";
        
        private List<AssemblyInfo> assemblies;
        
        public class AssemblyInfo
        {
            public string Name { get; set; } = "";
            public ObjectId Id { get; set; }
            public string Description { get; set; } = "";
            
            public override string ToString()
            {
                return Name;
            }
        }

        // Form controls
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblInstructions;
        private System.Windows.Forms.Label lblDescription;
        private ListBox listBoxAssemblies;
        private TextBox txtDescription;
        private Button btnOK;
        private Button btnCancel;

        public AssemblySelectionForm()
        {
            InitializeComponent();
            assemblies = new List<AssemblyInfo>();
            LoadAssemblies();
        }

        private void InitializeComponent()
        {
            // Initialize controls
            lblTitle = new System.Windows.Forms.Label();
            lblInstructions = new System.Windows.Forms.Label();
            lblDescription = new System.Windows.Forms.Label();
            listBoxAssemblies = new ListBox();
            txtDescription = new TextBox();
            btnOK = new Button();
            btnCancel = new Button();

            SuspendLayout();

            // lblTitle
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.DarkBlue;
            lblTitle.Location = new System.Drawing.Point(30, 30);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(200, 32);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Chọn Assembly";

            // lblInstructions
            lblInstructions.AutoSize = true;
            lblInstructions.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblInstructions.Location = new System.Drawing.Point(30, 80);
            lblInstructions.Name = "lblInstructions";
            lblInstructions.Size = new Size(400, 25);
            lblInstructions.TabIndex = 1;
            lblInstructions.Text = "Chọn assembly để sử dụng cho corridor:";

            // listBoxAssemblies
            listBoxAssemblies.Font = new System.Drawing.Font("Segoe UI", 10F);
            listBoxAssemblies.FormattingEnabled = true;
            listBoxAssemblies.ItemHeight = 28;
            listBoxAssemblies.Location = new System.Drawing.Point(30, 120);
            listBoxAssemblies.Name = "listBoxAssemblies";
            listBoxAssemblies.Size = new Size(500, 280);
            listBoxAssemblies.TabIndex = 2;
            listBoxAssemblies.SelectedIndexChanged += listBoxAssemblies_SelectedIndexChanged;
            listBoxAssemblies.DoubleClick += listBoxAssemblies_DoubleClick;

            // lblDescription
            lblDescription.AutoSize = true;
            lblDescription.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold);
            lblDescription.Location = new System.Drawing.Point(30, 420);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(75, 25);
            lblDescription.TabIndex = 3;
            lblDescription.Text = "Mô tả:";

            // txtDescription
            txtDescription.Font = new System.Drawing.Font("Segoe UI", 9F);
            txtDescription.Location = new System.Drawing.Point(30, 450);
            txtDescription.Multiline = true;
            txtDescription.Name = "txtDescription";
            txtDescription.ReadOnly = true;
            txtDescription.ScrollBars = ScrollBars.Vertical;
            txtDescription.Size = new Size(500, 80);
            txtDescription.TabIndex = 4;
            txtDescription.Text = "Chọn assembly từ danh sách để xem thông tin chi tiết.";

            // btnOK
            btnOK.Font = new System.Drawing.Font("Segoe UI", 10F, FontStyle.Bold);
            btnOK.Location = new System.Drawing.Point(280, 560);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(120, 50);
            btnOK.TabIndex = 5;
            btnOK.Text = "Chọn";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;

            // btnCancel
            btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            btnCancel.Location = new System.Drawing.Point(410, 560);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(120, 50);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Hủy";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;

            // Form setup
            ClientSize = new Size(560, 640);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(txtDescription);
            Controls.Add(lblDescription);
            Controls.Add(listBoxAssemblies);
            Controls.Add(lblInstructions);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AssemblySelectionForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Chọn Assembly";

            ResumeLayout(false);
            PerformLayout();
        }

        private void LoadAssemblies()
        {
            assemblies.Clear();
            listBoxAssemblies.Items.Clear();

            try
            {
                using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId assemblyId in A.Cdoc.AssemblyCollection)
                    {
                        Assembly? assembly = tr.GetObject(assemblyId, OpenMode.ForRead) as Assembly;
                        if (assembly != null)
                        {
                            var assemblyInfo = new AssemblyInfo
                            {
                                Name = assembly.Name,
                                Id = assembly.Id,
                                Description = GetAssemblyDescription(assembly)
                            };
                            
                            assemblies.Add(assemblyInfo);
                        }
                    }
                    
                    tr.Commit();
                }

                // Sort assemblies by name
                assemblies = assemblies.OrderBy(a => a.Name).ToList();

                // Add to listbox
                foreach (var assemblyInfo in assemblies)
                {
                    listBoxAssemblies.Items.Add(assemblyInfo);
                }

                // Select first item if available
                if (assemblies.Count > 0)
                {
                    listBoxAssemblies.SelectedIndex = 0;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách assembly: {ex.Message}", "Lỗi", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetAssemblyDescription(Assembly assembly)
        {
            try
            {
                string description = $"Tên: {assembly.Name}\n";
                description += $"ObjectId: {assembly.Id}\n";
                
                // Try to get subassembly count
                try
                {
                    using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                    {
                        var subassemblies = assembly.Groups;
                        description += $"Số nhóm subassembly: {subassemblies.Count}\n";
                        
                        // List some subassemblies
                        int count = 0;
                        foreach (var group in subassemblies)
                        {
                            if (count < 3) // Limit to first 3
                            {
                                description += $"- Nhóm: {group.Name}\n";
                                count++;
                            }
                            else if (subassemblies.Count > 3)
                            {
                                description += $"... và {subassemblies.Count - 3} nhóm khác\n";
                                break;
                            }
                        }
                        
                        tr.Commit();
                    }
                }
                catch
                {
                    description += "Không thể lấy thông tin chi tiết về subassembly.\n";
                }
                
                return description;
            }
            catch
            {
                return "Không thể lấy thông tin về assembly này.";
            }
        }

        private void listBoxAssemblies_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxAssemblies.SelectedItem != null)
            {
                var selectedAssembly = (AssemblyInfo)listBoxAssemblies.SelectedItem;
                txtDescription.Text = selectedAssembly.Description;
            }
        }

        private void listBoxAssemblies_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxAssemblies.SelectedItem != null)
            {
                btnOK_Click(sender, e);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (listBoxAssemblies.SelectedItem != null)
            {
                var selectedAssembly = (AssemblyInfo)listBoxAssemblies.SelectedItem;
                SelectedAssemblyId = selectedAssembly.Id;
                SelectedAssemblyName = selectedAssembly.Name;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một assembly.", "Thông báo", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        public static (ObjectId assemblyId, string assemblyName) ShowAssemblySelectionDialog()
        {
            using (var form = new AssemblySelectionForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    return (form.SelectedAssemblyId, form.SelectedAssemblyName);
                }
                return (ObjectId.Null, "");
            }
        }
    }
}
