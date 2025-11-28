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
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.Settings;

namespace MyFirstProject
{
    public partial class PipeDiameterSelectionForm : Form
    {
        public ObjectId SelectedPipeSizeId { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        
        private List<PipeSizeInfo> pipeSizes;
        
        public class PipeSizeInfo
        {
            public string Name { get; set; } = "";
            public ObjectId Id { get; set; }
            public int Index { get; set; }
            public double Diameter { get; set; }
            
            public override string ToString()
            {
                return Name; // Just display the name since diameter property is not accessible
            }
        }

        public PipeDiameterSelectionForm()
        {
            InitializeComponent();
            pipeSizes = new List<PipeSizeInfo>();
        }

        public void SetCurrentPipeInfo(string currentSizeName, double currentDiameter, int pipeCount)
        {
            lblCurrentInfo.Text = $"Đường kính hiện tại: {currentSizeName} (Ø {currentDiameter:F0}mm)\nSố lượng ống cống: {pipeCount}";
        }

        public void LoadPipeSizes(PartFamily pipeFamily, Transaction transaction)
        {
            pipeSizes.Clear();
            listBoxPipeSizes.Items.Clear();
            
            for (int i = 0; i < pipeFamily.PartSizeCount; i++)
            {
                PartSize? partSize = transaction.GetObject(pipeFamily[i], OpenMode.ForRead) as PartSize;
                if (partSize != null)
                {
                    var pipeSizeInfo = new PipeSizeInfo
                    {
                        Name = partSize.Name,
                        Id = partSize.Id,
                        Index = i,
                        Diameter = 0 // We'll just use the name for display since diameter property is not available
                    };
                    
                    pipeSizes.Add(pipeSizeInfo);
                }
            }
            
            // Add items to list in order
            foreach (var item in pipeSizes)
            {
                listBoxPipeSizes.Items.Add(item);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (listBoxPipeSizes.SelectedItem != null)
            {
                var selectedPipeSize = (PipeSizeInfo)listBoxPipeSizes.SelectedItem;
                SelectedPipeSizeId = selectedPipeSize.Id;
                SelectedIndex = selectedPipeSize.Index;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một kích thước ống cống.", "Thông báo", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listBoxPipeSizes_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxPipeSizes.SelectedItem != null)
            {
                btnOK_Click(sender, e);
            }
        }
    }
}
