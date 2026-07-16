using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyFirstProject.Civil_Tool_2
{
    public partial class PhanChiaCorridorForm : Form
    {
        private Database _db;
        private Editor _ed;

        public ObjectId SelectedCorridorId { get; private set; }
        public List<double> SplitStations { get; private set; }
        public bool FormAccepted { get; private set; }

        public PhanChiaCorridorForm(Database db, Editor ed)
        {
            InitializeComponent();
            _db = db;
            _ed = ed;
            SplitStations = new List<double>();
            FormAccepted = false;
        }

        private void btnChonCorridor_Click(object sender, EventArgs e)
        {
            using (this.HideTemp())
            {
                var options = new PromptEntityOptions("\nChọn Corridor: ");
                options.SetRejectMessage("\nĐối tượng chọn không phải là Corridor!");
                options.AddAllowedClass(typeof(Corridor), true);

                var result = _ed.GetEntity(options);
                if (result.Status == PromptStatus.OK)
                {
                    SelectedCorridorId = result.ObjectId;
                    using (Transaction tr = _db.TransactionManager.StartTransaction())
                    {
                        var corridor = tr.GetObject(SelectedCorridorId, OpenMode.ForRead) as Corridor;
                        txtCorridorName.Text = corridor.Name;
                        tr.Commit();
                    }
                }
            }
        }

        private void btnPickPoints_Click(object sender, EventArgs e)
        {
            using (this.HideTemp())
            {
                // Chọn Profile View
                var pvo = new PromptEntityOptions("\nChọn Trắc dọc (Profile View): ");
                pvo.SetRejectMessage("\nĐối tượng chọn không phải là Profile View!");
                pvo.AddAllowedClass(typeof(ProfileView), true);

                var pvResult = _ed.GetEntity(pvo);
                if (pvResult.Status != PromptStatus.OK) return;

                ObjectId pvId = pvResult.ObjectId;

                // Chọn nhiều điểm trên Profile View
                _ed.WriteMessage("\nBắt đầu pick các điểm trên trắc dọc (Nhấn ESC hoặc Enter để kết thúc)...");
                
                using (Transaction tr = _db.TransactionManager.StartTransaction())
                {
                    var pv = tr.GetObject(pvId, OpenMode.ForRead) as ProfileView;

                    while (true)
                    {
                        var ppo = new PromptPointOptions("\nPick điểm chia trên trắc dọc (Enter để kết thúc): ");
                        ppo.AllowNone = true;
                        
                        var ptResult = _ed.GetPoint(ppo);
                        if (ptResult.Status == PromptStatus.Cancel || ptResult.Status == PromptStatus.None)
                            break;

                        Point3d pt = ptResult.Value;
                        double station = 0;
                        double elevation = 0;

                        try
                        {
                            pv.FindStationAndElevationAtXY(pt.X, pt.Y, ref station, ref elevation);
                            
                            // Thêm vào danh sách nếu chưa có
                            if (!SplitStations.Contains(station))
                            {
                                SplitStations.Add(station);
                                _ed.WriteMessage($"\nĐã nhận lý trình: {station:F3}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _ed.WriteMessage($"\nKhông thể tính lý trình từ điểm vừa chọn: {ex.Message}");
                        }
                    }
                    tr.Commit();
                }

                // Cập nhật lại listbox
                UpdateStationList();
            }
        }

        private void UpdateStationList()
        {
            lstStations.Items.Clear();
            SplitStations.Sort();
            foreach (double stn in SplitStations)
            {
                lstStations.Items.Add(stn.ToString("F3"));
            }
        }

        private void btnXoaLyTrinh_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstStations.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < SplitStations.Count)
            {
                SplitStations.RemoveAt(selectedIndex);
                UpdateStationList();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một lý trình trong danh sách để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnThucHien_Click(object sender, EventArgs e)
        {
            if (SelectedCorridorId.IsNull)
            {
                MessageBox.Show("Vui lòng chọn Corridor trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (SplitStations.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một điểm chia trên trắc dọc!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FormAccepted = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            FormAccepted = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    // Helper class to hide form temporarily
    public static class FormExtensions
    {
        public static IDisposable HideTemp(this Form form)
        {
            return new FormHider(form);
        }

        private class FormHider : IDisposable
        {
            private Form _form;
            public FormHider(Form form)
            {
                _form = form;
                _form.Hide();
            }

            public void Dispose()
            {
                _form.Show();
            }
        }
    }
}
