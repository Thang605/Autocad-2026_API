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
using Autodesk.Civil.ApplicationServices;

namespace MyFirstProject
{
    public partial class SurfaceReferenceForm : Form
    {
        public enum ApplicationMode
        {
            SelectedObjects,
            EntireNetwork
        }

        public ApplicationMode SelectedMode { get; private set; }
        public ObjectId SelectedSurfaceId { get; private set; }
        public ObjectId SelectedNetworkId { get; private set; }

        private List<NetworkInfo> networks;

        public class NetworkInfo
        {
            public string Name { get; set; } = "";
            public ObjectId Id { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public SurfaceReferenceForm()
        {
            InitializeComponent();
            networks = new List<NetworkInfo>();
            SelectedSurfaceId = ObjectId.Null;
            SelectedNetworkId = ObjectId.Null;
            LoadNetworks();
            
            // Set default mode
            radioSelectedObjects.Checked = true;
            UpdateUI();
        }

        private void LoadNetworks()
        {
            networks.Clear();
            comboBoxNetworks.Items.Clear();

            try
            {
                var civilDoc = CivilApplication.ActiveDocument;
                var networks = civilDoc.GetPipeNetworkIds();

                using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (ObjectId networkId in networks)
                    {
                        var network = tr.GetObject(networkId, OpenMode.ForRead) as Network;
                        if (network != null)
                        {
                            var networkInfo = new NetworkInfo
                            {
                                Name = network.Name,
                                Id = network.Id
                            };
                            this.networks.Add(networkInfo);
                        }
                    }
                    tr.Commit();
                }

                foreach (var item in this.networks)
                {
                    comboBoxNetworks.Items.Add(item);
                }

                if (comboBoxNetworks.Items.Count > 0)
                {
                    comboBoxNetworks.SelectedIndex = 0;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách mạng lưới: {ex.Message}", "Lỗi", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SetSurfaceInfo(ObjectId surfaceId, string surfaceName)
        {
            SelectedSurfaceId = surfaceId;
            lblSurfaceInfo.Text = $"Mặt phẳng được chọn: {surfaceName}";
        }

        private void radioSelectedObjects_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void radioEntireNetwork_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            bool isNetworkMode = radioEntireNetwork.Checked;
            
            groupBoxNetwork.Enabled = isNetworkMode;
            comboBoxNetworks.Enabled = isNetworkMode;
            btnRefreshNetworks.Enabled = isNetworkMode;
            
            if (isNetworkMode)
            {
                lblInstructions.Text = "Chế độ: Áp dụng cho toàn bộ đối tượng trong mạng lưới được chọn.";
            }
            else
            {
                lblInstructions.Text = "Chế độ: Áp dụng cho các đối tượng được chọn thủ công.";
            }
        }

        private void btnRefreshNetworks_Click(object sender, EventArgs e)
        {
            LoadNetworks();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (SelectedSurfaceId == ObjectId.Null)
            {
                MessageBox.Show("Vui lòng chọn mặt phẳng trước khi tiếp tục.", "Thông báo", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (radioEntireNetwork.Checked)
            {
                if (comboBoxNetworks.SelectedItem == null)
                {
                    MessageBox.Show("Vui lòng chọn một mạng lưới.", "Thông báo", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedNetwork = (NetworkInfo)comboBoxNetworks.SelectedItem;
                SelectedNetworkId = selectedNetwork.Id;
                SelectedMode = ApplicationMode.EntireNetwork;
            }
            else
            {
                SelectedMode = ApplicationMode.SelectedObjects;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
