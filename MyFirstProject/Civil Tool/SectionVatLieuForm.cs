using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MyFirstProject.Civil_Tool
{
    public partial class SectionVatLieuForm : Form
    {
        public bool FormAccepted { get; private set; } = false;
        public string SelectedLinkCode { get; private set; } = "";
        public string PrefixText { get; private set; } = "";
        
        public SectionVatLieuForm(List<string> linkCodes)
        {
            InitializeComponent();
            
            if (linkCodes != null && linkCodes.Count > 0)
            {
                cbbLinkCodes.Items.AddRange(linkCodes.ToArray());
                cbbLinkCodes.SelectedIndex = 0;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SelectedLinkCode = cbbLinkCodes.SelectedItem?.ToString() ?? cbbLinkCodes.Text;
            PrefixText = txtPrefix.Text;

            if (string.IsNullOrWhiteSpace(SelectedLinkCode))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập mã Link Code.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FormAccepted = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
