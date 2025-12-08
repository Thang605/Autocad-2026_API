using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Acad = Autodesk.AutoCAD.ApplicationServices;

[assembly: CommandClass(typeof(Civil3DCsharp.CT_TaoDuong_ConnectedAlignment_NutGiao_Commands))]

namespace Civil3DCsharp
{
    public class CT_TaoDuong_ConnectedAlignment_NutGiao_Commands
    {
        // Static variables to remember last input values
        private static double _lastRadius = 15.0;
        private static bool _lastIs3Way = false; // false = 4-way, true = 3-way
        private static double _lastOverlap = 2.0; // Connection overlap (optional)

        [CommandMethod("CT_TaoDuong_ConnectedAlignment_NutGiao")]
        public static void CT_TaoDuong_ConnectedAlignment_NutGiao()
        {
            using Transaction tr = Acad.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
            try
            {
                var doc = Acad.Application.DocumentManager.MdiActiveDocument;
                var ed = doc.Editor;
                var civilDoc = CivilApplication.ActiveDocument;

                // TODO: Implement selection logic for alignments
                // For now, we will just show the form with placeholder names to ensure it builds and runs.

                using (var form = new ConnectedAlignmentForm())
                {
                    form.SetValues("Alignment 1 (Test)", "Alignment 2 (Test)", _lastRadius, _lastIs3Way);
                    var res = Application.ShowModalDialog(form);
                    if (res == DialogResult.OK)
                    {
                        _lastRadius = form.Radius;
                        _lastIs3Way = form.Is3Way;
                        ed.WriteMessage($"\nSelected Radius: {_lastRadius}, 3-Way: {_lastIs3Way}");
                        // Implement creation logic here
                    }
                }

                tr.Commit();
            }
            catch (System.Exception ex)
            {
                Acad.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nError: " + ex.Message);
            }
        }
    }

    public class ConnectedAlignmentForm : Form
    {
        public double Radius { get; private set; }
        public bool Is3Way { get; private set; }

        private System.Windows.Forms.Label lblAlign1;
        private System.Windows.Forms.TextBox txtAlign1;
        private System.Windows.Forms.Label lblAlign2;
        private System.Windows.Forms.TextBox txtAlign2;
        private System.Windows.Forms.Label lblRadius;
        private System.Windows.Forms.TextBox txtRadius;
        private System.Windows.Forms.GroupBox grpType;
        private System.Windows.Forms.RadioButton rdb3Way;
        private System.Windows.Forms.RadioButton rdb4Way;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        public ConnectedAlignmentForm()
        {
            InitializeComponent();
        }

        public void SetValues(string align1Name, string align2Name, double lastRadius, bool lastIs3Way)
        {
            txtAlign1.Text = align1Name;
            txtAlign2.Text = align2Name;
            txtRadius.Text = lastRadius.ToString();
            if (lastIs3Way) rdb3Way.Checked = true;
            else rdb4Way.Checked = true;
        }

        private void InitializeComponent()
        {
            this.lblAlign1 = new System.Windows.Forms.Label();
            this.txtAlign1 = new System.Windows.Forms.TextBox();
            this.lblAlign2 = new System.Windows.Forms.Label();
            this.txtAlign2 = new System.Windows.Forms.TextBox();
            this.lblRadius = new System.Windows.Forms.Label();
            this.txtRadius = new System.Windows.Forms.TextBox();
            this.grpType = new System.Windows.Forms.GroupBox();
            this.rdb3Way = new System.Windows.Forms.RadioButton();
            this.rdb4Way = new System.Windows.Forms.RadioButton();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // Form
            this.Text = "Tạo Connected Alignment tại Nút Giao";
            this.Size = new System.Drawing.Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Align 1
            this.lblAlign1.Text = "Tuyến đường 1:";
            this.lblAlign1.Location = new System.Drawing.Point(20, 20);
            this.lblAlign1.Size = new System.Drawing.Size(100, 23);

            this.txtAlign1.Location = new System.Drawing.Point(130, 17);
            this.txtAlign1.Size = new System.Drawing.Size(230, 23);
            this.txtAlign1.ReadOnly = true;

            // Align 2
            this.lblAlign2.Text = "Tuyến đường 2:";
            this.lblAlign2.Location = new System.Drawing.Point(20, 50);
            this.lblAlign2.Size = new System.Drawing.Size(100, 23);

            this.txtAlign2.Location = new System.Drawing.Point(130, 47);
            this.txtAlign2.Size = new System.Drawing.Size(230, 23);
            this.txtAlign2.ReadOnly = true;

            // Radius
            this.lblRadius.Text = "Bán kính (m):";
            this.lblRadius.Location = new System.Drawing.Point(20, 90);
            this.lblRadius.Size = new System.Drawing.Size(100, 23);

            this.txtRadius.Location = new System.Drawing.Point(130, 87);
            this.txtRadius.Size = new System.Drawing.Size(100, 23);

            // Group Type
            this.grpType.Text = "Loại nút giao";
            this.grpType.Location = new System.Drawing.Point(20, 130);
            this.grpType.Size = new System.Drawing.Size(340, 100);

            this.rdb3Way.Text = "Ngã 3 (T-Intersection)";
            this.rdb3Way.Location = new System.Drawing.Point(20, 30);
            this.rdb3Way.Size = new System.Drawing.Size(200, 23);

            this.rdb4Way.Text = "Ngã 4 (Cross Intersection)";
            this.rdb4Way.Location = new System.Drawing.Point(20, 60);
            this.rdb4Way.Size = new System.Drawing.Size(200, 23);
            this.rdb4Way.Checked = true;

            // Buttons
            this.btnOK.Text = "Tạo";
            this.btnOK.Location = new System.Drawing.Point(200, 250);
            this.btnOK.Size = new System.Drawing.Size(75, 30);
            this.btnOK.Click += new EventHandler(this.btnOK_Click);

            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new System.Drawing.Point(285, 250);
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);

            // Add controls
            this.Controls.Add(this.lblAlign1);
            this.Controls.Add(this.txtAlign1);
            this.Controls.Add(this.lblAlign2);
            this.Controls.Add(this.txtAlign2);
            this.Controls.Add(this.lblRadius);
            this.Controls.Add(this.txtRadius);
            this.Controls.Add(this.grpType);
            this.grpType.Controls.Add(this.rdb3Way);
            this.grpType.Controls.Add(this.rdb4Way);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);

            this.ResumeLayout(false);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (double.TryParse(txtRadius.Text, out double r))
            {
                Radius = r;
                Is3Way = rdb3Way.Checked;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Vui lòng nhập bán kính hợp lệ (số).");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
