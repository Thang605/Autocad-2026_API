using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Civil3DCsharp;

namespace MyFirstProject.Menu_form
{
    public class StandardComparisonForm : Form
    {
        private DataGridView dgv;

        public StandardComparisonForm(int speed, List<IDesignStandard> standards)
        {
            this.Text = $"Bảng so sánh thông số Tiêu chuẩn thiết kế (V_tk = {speed} km/h)";
            this.Size = new Size(900, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowIcon = false;
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            dgv = new DataGridView();
            dgv.Dock = DockStyle.Fill;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;

            this.Controls.Add(dgv);

            LoadData(speed, standards);
        }

        private void LoadData(int speed, List<IDesignStandard> standards)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Thông số thiết kế hình học", typeof(string));

            foreach (var std in standards)
            {
                dt.Columns.Add(std.StandardName, typeof(string));
            }

            string[] parameters = new string[]
            {
                "Bán kính đường cong nằm tối thiểu - Giới hạn (m)",
                "Bán kính đường cong nằm tối thiểu - Thông thường (m)",
                "Chiều dài đường cong nằm tối thiểu (m)",
                "Chiều dài đường cong chuyển tiếp tối thiểu (m)",
                "Chiều dài đoạn thẳng tối đa (m)",
                "Chiều dài đoạn chêm (Cùng chiều) tối thiểu (m)",
                "Chiều dài đoạn chêm (Ngược chiều) tối thiểu (m)",
                "Độ dốc siêu cao tối đa Isc_max (%)"
            };

            for (int i = 0; i < parameters.Length; i++)
            {
                DataRow row = dt.NewRow();
                row[0] = parameters[i];
                
                int colIdx = 1;
                foreach (var std in standards)
                {
                    if (std.SupportedSpeeds.Contains(speed))
                    {
                        var p = std.GetParameters(speed);
                        switch (i)
                        {
                            case 0: row[colIdx] = p.MinRadiusLimit.ToString(); break;
                            case 1: row[colIdx] = p.MinRadiusNormal.ToString(); break;
                            case 2: row[colIdx] = p.MinCurveLength.ToString(); break;
                            case 3: row[colIdx] = p.MinTransitionCurveLength.ToString(); break;
                            case 4: row[colIdx] = p.MaxStraightLength.ToString(); break;
                            case 5: row[colIdx] = p.MinStraightLengthSameDirection.ToString(); break;
                            case 6: row[colIdx] = p.MinStraightLengthReverseDirection.ToString(); break;
                            case 7: row[colIdx] = p.MaxSuperelevation.ToString("F1") + " %"; break;
                        }
                    }
                    else
                    {
                        row[colIdx] = "- (Không hỗ trợ)";
                    }
                    colIdx++;
                }
                dt.Rows.Add(row);
            }

            dgv.DataSource = dt;
            
            // Format column widths
            dgv.Columns[0].FillWeight = 40; // parameter name gets 40% width
            for(int i = 1; i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].FillWeight = 60f / (dgv.Columns.Count - 1);
                dgv.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
    }
}
