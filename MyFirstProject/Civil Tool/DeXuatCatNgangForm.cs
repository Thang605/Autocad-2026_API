using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Civil3DCsharp
{
    public partial class DeXuatCatNgangForm : Form
    {
        public bool RequestDrawCadTable { get; private set; } = false;
        public CrossSectionInputParameters CurrentInputParameters { get; private set; } = new CrossSectionInputParameters();
        public List<CrossSectionCheckItem> CurrentEvaluationResults { get; private set; } = new List<CrossSectionCheckItem>();

        public DeXuatCatNgangForm()
        {
            InitializeComponent();
            InitStandardDropdowns();
            UpdateEvaluationUI();
        }

        private void InitStandardDropdowns()
        {
            cboStandard.Items.Clear();
            cboStandard.Items.Add("TCVN 4054:2005 (Đường Ô Tô)");
            cboStandard.Items.Add("TCVN 13592:2022 (Đường Đô Thị)");
            cboStandard.Items.Add("TCVN 10380:2014 (Đường GTNT)");
            cboStandard.SelectedIndex = 0;
        }

        private void OnStandardChanged(object sender, EventArgs e)
        {
            string selectedStandard = cboStandard.SelectedItem?.ToString() ?? "";
            var roadTypes = CrossSectionEvaluator.GetRoadTypes(selectedStandard);

            cboRoadType.Items.Clear();
            foreach (var rt in roadTypes)
            {
                cboRoadType.Items.Add(rt);
            }

            if (cboRoadType.Items.Count > 0)
            {
                cboRoadType.SelectedIndex = 3 < cboRoadType.Items.Count ? 3 : 0;
            }

            lblHardShoulder.Text = "Lề gia cố Blgc:";
            lblSoftShoulder.Text = "Lề đất Bld (m/bên):";

            if (selectedStandard.Contains("13592"))
            {
                string selectedType = cboRoadType.SelectedItem?.ToString() ?? "";
                if (selectedType == "Đường Cao Tốc Đô Thị")
                {
                    numHardShoulder.Enabled = true;
                    numSoftShoulder.Enabled = false;
                    numSoftShoulder.Value = 0.00m;
                    if (numHardShoulder.Value == 0) numHardShoulder.Value = 1.50m;
                }
                else
                {
                    numHardShoulder.Value = 0.00m;
                    numSoftShoulder.Value = 0.00m;
                    numHardShoulder.Enabled = false;
                    numSoftShoulder.Enabled = false;
                }

                if (numSidewalk.Value == 0) numSidewalk.Value = 3.00m;
            }
            else
            {
                numHardShoulder.Enabled = true;
                numSoftShoulder.Enabled = true;

                if (numHardShoulder.Value == 0) numHardShoulder.Value = 1.50m;
                if (numSoftShoulder.Value == 0) numSoftShoulder.Value = 0.50m;
                if (numSidewalk.Value > 0 && selectedStandard.Contains("4054")) numSidewalk.Value = 0.00m;
            }

            UpdateEvaluationUI();
        }

        private void OnRoadTypeChanged(object sender, EventArgs e)
        {
            string selectedStandard = cboStandard.SelectedItem?.ToString() ?? "";
            string selectedType = cboRoadType.SelectedItem?.ToString() ?? "";

            var speeds = CrossSectionEvaluator.GetSupportedSpeeds(selectedStandard, selectedType);
            cboDesignSpeed.Items.Clear();
            foreach (var spd in speeds)
            {
                cboDesignSpeed.Items.Add($"{spd} km/h");
            }

            if (cboDesignSpeed.Items.Count > 0)
            {
                cboDesignSpeed.SelectedIndex = cboDesignSpeed.Items.Count - 1;
            }

            var req = CrossSectionEvaluator.GetRequirement(selectedStandard, selectedType, GetSelectedSpeed());
            numLanesCount.Value = Math.Max(numLanesCount.Minimum, Math.Min(numLanesCount.Maximum, (decimal)req.MinLanesCount));
            numLaneWidth.Value = Math.Max(numLaneWidth.Minimum, Math.Min(numLaneWidth.Maximum, (decimal)req.MinLaneWidth));
            numMedianWidth.Value = Math.Max(numMedianWidth.Minimum, Math.Min(numMedianWidth.Maximum, (decimal)req.MinMedianWidth));
            numSafetyStrip.Value = Math.Max(numSafetyStrip.Minimum, Math.Min(numSafetyStrip.Maximum, (decimal)req.MinSafetyStripWidth));
            numTargetROW.Value = Math.Max(numTargetROW.Minimum, Math.Min(numTargetROW.Maximum, (decimal)req.RecommendedRightOfWay));

            if (selectedStandard.Contains("13592"))
            {
                if (selectedType == "Đường Cao Tốc Đô Thị")
                {
                    numHardShoulder.Enabled = true;
                    numSoftShoulder.Enabled = false;
                    numSoftShoulder.Value = 0.00m;
                    numHardShoulder.Value = Math.Max(numHardShoulder.Minimum, Math.Min(numHardShoulder.Maximum, (decimal)req.MinHardShoulderWidth));
                }
                else
                {
                    numHardShoulder.Value = 0.00m;
                    numSoftShoulder.Value = 0.00m;
                    numHardShoulder.Enabled = false;
                    numSoftShoulder.Enabled = false;
                }

                if (req.MinSidewalkWidth > 0)
                {
                    numSidewalk.Value = Math.Max(numSidewalk.Minimum, Math.Min(numSidewalk.Maximum, (decimal)req.MinSidewalkWidth));
                }
            }
            else
            {
                numHardShoulder.Enabled = true;
                numSoftShoulder.Enabled = true;
                numHardShoulder.Value = Math.Max(numHardShoulder.Minimum, Math.Min(numHardShoulder.Maximum, (decimal)req.MinHardShoulderWidth));
            }

            UpdateEvaluationUI();
        }

        private void OnStandardOrTypeChanged(object sender, EventArgs e)
        {
            UpdateEvaluationUI();
        }

        private void OnInputChanged(object sender, EventArgs e)
        {
            UpdateEvaluationUI();
        }

        private int GetSelectedSpeed()
        {
            if (cboDesignSpeed.SelectedItem == null) return 60;
            string spdStr = cboDesignSpeed.SelectedItem.ToString().Replace("km/h", "").Trim();
            int spd;
            return int.TryParse(spdStr, out spd) ? spd : 60;
        }

        private CrossSectionInputParameters CollectInputParameters()
        {
            return new CrossSectionInputParameters
            {
                StandardName = cboStandard.SelectedItem?.ToString() ?? "TCVN 4054:2005",
                RoadType = cboRoadType.SelectedItem?.ToString() ?? "Đường Cấp III",
                DesignSpeed = GetSelectedSpeed(),

                LanesCount = (int)numLanesCount.Value,
                LaneWidth = (double)numLaneWidth.Value,
                MedianWidth = (double)numMedianWidth.Value,
                SafetyStripWidth = (double)numSafetyStrip.Value,

                HardShoulderWidth = (double)numHardShoulder.Value,
                SoftShoulderWidth = (double)numSoftShoulder.Value,

                SidewalkWidth = (double)numSidewalk.Value,
                GreeneryWidth = (double)numGreenery.Value,
                BikeLaneWidth = (double)numBikeLane.Value,

                RoadwayCrossSlope = (double)numRoadSlope.Value,
                ShoulderCrossSlope = (double)numShoulderSlope.Value,
                SidewalkCrossSlope = (double)numRoadSlope.Value,
                TargetRightOfWay = (double)numTargetROW.Value
            };
        }

        private void UpdateEvaluationUI()
        {
            CurrentInputParameters = CollectInputParameters();
            CurrentEvaluationResults = CrossSectionEvaluator.Evaluate(CurrentInputParameters);

            lblCarriagewayWidth.Text = $"Mặt xe chạy (Bxc): {CurrentInputParameters.TotalCarriagewayWidth:F2} m";
            lblTotalProposedWidth.Text = $"Tổng bề rộng MC: {CurrentInputParameters.TotalProposedWidth:F2} m";

            picPreview?.Invalidate();

            dgvEvaluationResults.Rows.Clear();
            int passCount = 0;
            int failCount = 0;
            int warnCount = 0;

            for (int i = 0; i < CurrentEvaluationResults.Count; i++)
            {
                var item = CurrentEvaluationResults[i];
                int rowIndex = dgvEvaluationResults.Rows.Add(
                    (i + 1).ToString(),
                    item.ElementName,
                    item.ProposedValue,
                    item.StandardRequirement,
                    GetStatusText(item.Status),
                    item.Note
                );

                DataGridViewRow row = dgvEvaluationResults.Rows[rowIndex];
                FormatRowByStatus(row, item.Status);

                if (item.Status == CheckStatus.Pass) passCount++;
                else if (item.Status == CheckStatus.Fail) failCount++;
                else warnCount++;
            }

            if (failCount == 0 && warnCount == 0)
            {
                lblStatusSummary.Text = $"TRẠNG THÁI: ĐẠT CHUẨN TOÀN BỘ ({passCount}/{CurrentEvaluationResults.Count} TIÊU CHÍ)";
                lblStatusSummary.ForeColor = Color.DarkGreen;
            }
            else if (failCount > 0)
            {
                lblStatusSummary.Text = $"CẢNH BÁO: CÓ {failCount} TIÊU CHÍ KHÔNG ĐẠT TIÊU CHUẨN!";
                lblStatusSummary.ForeColor = Color.DarkRed;
            }
            else
            {
                lblStatusSummary.Text = $"LƯU Ý: CÓ {warnCount} TIÊU CHÍ CẦN CHÚ Ý";
                lblStatusSummary.ForeColor = Color.DarkOrange;
            }
        }

        private string GetStatusText(CheckStatus status)
        {
            switch (status)
            {
                case CheckStatus.Pass: return "ĐẠT";
                case CheckStatus.Warning: return "CẢNH BÁO";
                case CheckStatus.Fail: return "KHÔNG ĐẠT";
                default: return "ĐẠT";
            }
        }

        private void FormatRowByStatus(DataGridViewRow row, CheckStatus status)
        {
            var statusCell = row.Cells["colStatus"];
            switch (status)
            {
                case CheckStatus.Pass:
                    statusCell.Style.BackColor = Color.FromArgb(220, 245, 220);
                    statusCell.Style.ForeColor = Color.DarkGreen;
                    statusCell.Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    break;
                case CheckStatus.Warning:
                    statusCell.Style.BackColor = Color.FromArgb(255, 245, 200);
                    statusCell.Style.ForeColor = Color.DarkOrange;
                    statusCell.Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    break;
                case CheckStatus.Fail:
                    statusCell.Style.BackColor = Color.FromArgb(255, 220, 220);
                    statusCell.Style.ForeColor = Color.DarkRed;
                    statusCell.Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    break;
            }
        }

        private void PicPreview_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int w = picPreview.Width;
            int h = picPreview.Height;
            if (w <= 0 || h <= 0) return;

            g.Clear(Color.FromArgb(248, 249, 250));

            var p = CurrentInputParameters;
            if (p == null) return;

            double totalProposed = p.TotalProposedWidth;
            double maxViewWidth = Math.Max(totalProposed, p.TargetRightOfWay > 0 ? p.TargetRightOfWay * 1.1 : totalProposed * 1.1);
            if (maxViewWidth <= 0) maxViewWidth = 10;

            int marginX = 65;
            int drawWidth = w - 2 * marginX;
            double scale = drawWidth / maxViewWidth; // Pixels per meter

            float centerX = w / 2.0f;
            float baseY = h / 2.0f + 15;
            float roadHeight = 20;

            // Compute exact continuous X coordinates from Centerline (0) going outward
            // 1. Median (Dải phân cách giữa)
            double currentXLeft = 0;
            double currentXRight = 0;
            if (p.MedianWidth > 0)
            {
                currentXLeft = -p.MedianWidth / 2.0;
                currentXRight = p.MedianWidth / 2.0;
            }

            // 2. Inner Safety Strip (Dải an toàn sát dải phân cách giữa)
            double leftSafetyStart = currentXLeft - p.SafetyStripWidth;
            double rightSafetyEnd = currentXRight + p.SafetyStripWidth;

            // 3. Carriageway (Mặt xe chạy / Các làn xe)
            double leftCarriagewayWidth = (p.LanesCount / 2.0) * p.LaneWidth;
            double rightCarriagewayWidth = (p.LanesCount / 2.0) * p.LaneWidth;

            double leftLanesStart = leftSafetyStart - leftCarriagewayWidth;
            double rightLanesEnd = rightSafetyEnd + rightCarriagewayWidth;

            // 4. Bike Lane (Dải xe thô sơ)
            double leftBikeStart = leftLanesStart - p.BikeLaneWidth;
            double rightBikeEnd = rightLanesEnd + p.BikeLaneWidth;

            // 5. Hard Shoulder (Lề gia cố)
            double leftHardStart = leftBikeStart - p.HardShoulderWidth;
            double rightHardEnd = rightBikeEnd + p.HardShoulderWidth;

            // 6. Soft Shoulder (Lề đất)
            double leftSoftStart = leftHardStart - p.SoftShoulderWidth;
            double rightSoftEnd = rightHardEnd + p.SoftShoulderWidth;

            // 7. Greenery (Dải cây xanh)
            double leftGreenStart = leftSoftStart - p.GreeneryWidth;
            double rightGreenEnd = rightSoftEnd + p.GreeneryWidth;

            // 8. Sidewalk (Vỉa hè / Dải đi bộ)
            double leftSidewalkStart = leftGreenStart - p.SidewalkWidth;
            double rightSidewalkEnd = rightGreenEnd + p.SidewalkWidth;

            // Draw Right-of-Way (Lộ giới / Chỉ giới đường đỏ) lines
            if (p.TargetRightOfWay > 0)
            {
                float rowLeftPx = centerX + (float)(-p.TargetRightOfWay / 2.0 * scale);
                float rowRightPx = centerX + (float)(p.TargetRightOfWay / 2.0 * scale);

                using (Pen redDashPen = new Pen(Color.Red, 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    g.DrawLine(redDashPen, rowLeftPx, 22, rowLeftPx, h - 35);
                    g.DrawLine(redDashPen, rowRightPx, 22, rowRightPx, h - 35);
                }

                using (Font fBold = new Font("Segoe UI", 8F, FontStyle.Bold))
                using (Brush redBrush = new SolidBrush(Color.DarkRed))
                {
                    g.DrawString("CGĐĐ", fBold, redBrush, rowLeftPx - 15, 5);
                    g.DrawString("CGĐĐ", fBold, redBrush, rowRightPx - 15, 5);
                }
            }

            // Helper draw segment block
            Action<double, double, Color, string, string, float> drawSegmentOffset = (xStart, xEnd, color, title, valText, yOffset) =>
            {
                if (Math.Abs(xEnd - xStart) <= 0.001) return;
                float x1 = centerX + (float)(xStart * scale);
                float x2 = centerX + (float)(xEnd * scale);
                float sw = Math.Abs(x2 - x1);

                using (Brush b = new SolidBrush(color))
                using (Pen pBorder = new Pen(Color.FromArgb(60, 60, 60), 1f))
                {
                    g.FillRectangle(b, Math.Min(x1, x2), baseY - roadHeight / 2 + yOffset, sw, roadHeight);
                    g.DrawRectangle(pBorder, Math.Min(x1, x2), baseY - roadHeight / 2 + yOffset, sw, roadHeight);
                }

                float midX = (x1 + x2) / 2.0f;
                using (Pen dimPen = new Pen(Color.DimGray, 1f))
                using (Font fFont = new Font("Segoe UI", 7.5F, FontStyle.Regular))
                using (Brush textBrush = new SolidBrush(Color.Black))
                {
                    float dimY = baseY + 20;
                    g.DrawLine(dimPen, x1, baseY + 10, x1, dimY + 4);
                    g.DrawLine(dimPen, x2, baseY + 10, x2, dimY + 4);
                    g.DrawLine(dimPen, x1, dimY, x2, dimY);

                    if (sw > 14)
                    {
                        SizeF sz = g.MeasureString(valText, fFont);
                        g.DrawString(valText, fFont, textBrush, midX - sz.Width / 2, dimY + 2);
                    }
                }
            };

            Action<double, double, Color, string, string> drawSegment = (xStart, xEnd, color, title, valText) =>
            {
                drawSegmentOffset(xStart, xEnd, color, title, valText, 0);
            };

            // 1. Sidewalk (Vỉa hè - Nâng nhẹ chiều cao cho đường đô thị)
            float sidewalkYOffset = p.IsUrbanRoad ? -6f : 0f;
            drawSegmentOffset(leftSidewalkStart, leftGreenStart, Color.FromArgb(189, 195, 199), "Vỉa hè", $"{p.SidewalkWidth:F1}m", sidewalkYOffset);
            drawSegmentOffset(rightGreenEnd, rightSidewalkEnd, Color.FromArgb(189, 195, 199), "Vỉa hè", $"{p.SidewalkWidth:F1}m", sidewalkYOffset);

            // 2. Greenery (Dải cây xanh)
            drawSegment(leftGreenStart, leftSoftStart, Color.FromArgb(46, 204, 113), "Cây xanh", $"{p.GreeneryWidth:F1}m");
            drawSegment(rightSoftEnd, rightGreenEnd, Color.FromArgb(46, 204, 113), "Cây xanh", $"{p.GreeneryWidth:F1}m");

            // 3. Soft Shoulder (Lề đất - Cho đường ngoài đô thị)
            drawSegment(leftSoftStart, leftHardStart, Color.FromArgb(211, 84, 0), "Lề đất", $"{p.SoftShoulderWidth:F1}m");
            drawSegment(rightHardEnd, rightSoftEnd, Color.FromArgb(211, 84, 0), "Lề đất", $"{p.SoftShoulderWidth:F1}m");

            // 4. Hard Shoulder (Lề gia cố)
            drawSegment(leftHardStart, leftBikeStart, Color.FromArgb(127, 140, 141), "Lề gia cố", $"{p.HardShoulderWidth:F1}m");
            drawSegment(rightBikeEnd, rightHardEnd, Color.FromArgb(127, 140, 141), "Lề gia cố", $"{p.HardShoulderWidth:F1}m");

            // 5. Bike Lane (Dải xe thô sơ)
            drawSegment(leftBikeStart, leftLanesStart, Color.FromArgb(230, 126, 34), "Thô sơ", $"{p.BikeLaneWidth:F1}m");
            drawSegment(rightLanesEnd, rightBikeEnd, Color.FromArgb(230, 126, 34), "Thô sơ", $"{p.BikeLaneWidth:F1}m");

            // 6. Carriageway (Phần xe chạy / Lòng đường)
            drawSegment(leftLanesStart, leftSafetyStart, Color.FromArgb(52, 73, 94), "Phần xe chạy", $"{leftCarriagewayWidth:F2}m");
            drawSegment(rightSafetyEnd, rightLanesEnd, Color.FromArgb(52, 73, 94), "Phần xe chạy", $"{rightCarriagewayWidth:F2}m");

            // Draw dashed lane lines inside Carriageway
            using (Pen lanePen = new Pen(Color.White, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                int sideLanes = Math.Max(1, p.LanesCount / 2);
                double singleLaneW = p.LaneWidth;
                for (int l = 1; l < sideLanes; l++)
                {
                    float lxLeft = centerX + (float)((leftSafetyStart - l * singleLaneW) * scale);
                    float lxRight = centerX + (float)((rightSafetyEnd + l * singleLaneW) * scale);
                    g.DrawLine(lanePen, lxLeft, baseY - roadHeight / 2 + 2, lxLeft, baseY + roadHeight / 2 - 2);
                    g.DrawLine(lanePen, lxRight, baseY - roadHeight / 2 + 2, lxRight, baseY + roadHeight / 2 - 2);
                }
            }

            // 7. Inner Safety Strip (Dải an toàn SÁT dải phân cách giữa)
            if (p.SafetyStripWidth > 0)
            {
                drawSegment(leftSafetyStart, currentXLeft, Color.FromArgb(44, 62, 80), "An toàn", $"{p.SafetyStripWidth:F2}m");
                drawSegment(currentXRight, rightSafetyEnd, Color.FromArgb(44, 62, 80), "An toàn", $"{p.SafetyStripWidth:F2}m");
            }

            // 8. Median (Dải phân cách giữa)
            if (p.MedianWidth > 0)
            {
                drawSegment(currentXLeft, currentXRight, Color.FromArgb(39, 174, 96), "Dải phân cách", $"{p.MedianWidth:F2}m");
            }

            // Draw Centerline symbol (TIM TUYẾN CL)
            using (Pen clPen = new Pen(Color.Crimson, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot })
            {
                g.DrawLine(clPen, centerX, 20, centerX, h - 30);
            }
            using (Font fCenter = new Font("Segoe UI", 8F, FontStyle.Bold))
            using (Brush clBrush = new SolidBrush(Color.Crimson))
            {
                g.DrawString("TIM TUYẾN (CL)", fCenter, clBrush, centerX - 38, h - 22);
            }

            // Draw Total Width Dimension Line at Top
            float totalLeftPx = centerX + (float)(leftSidewalkStart * scale);
            float totalRightPx = centerX + (float)(rightSidewalkEnd * scale);
            using (Pen topDimPen = new Pen(Color.DarkBlue, 1.5f))
            using (Font fTop = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (Brush topBrush = new SolidBrush(Color.DarkBlue))
            {
                float topY = 25;
                g.DrawLine(topDimPen, totalLeftPx, topY - 5, totalLeftPx, topY + 5);
                g.DrawLine(topDimPen, totalRightPx, topY - 5, totalRightPx, topY + 5);
                g.DrawLine(topDimPen, totalLeftPx, topY, totalRightPx, topY);

                string totalStr = $"TỔNG BỀ RỘNG MẶT CẮT NGANG (Btổng) = {totalProposed:F2} m";
                SizeF szTop = g.MeasureString(totalStr, fTop);
                g.DrawString(totalStr, fTop, topBrush, centerX - szTop.Width / 2, topY - 18);
            }
        }

        private void BtnEvaluate_Click(object sender, EventArgs e)
        {
            UpdateEvaluationUI();
            MessageBox.Show("Đã cập nhật sơ đồ và kiểm tra tiêu chuẩn mặt cắt ngang thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDrawCadTable_Click(object sender, EventArgs e)
        {
            RequestDrawCadTable = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "CSV File (*.csv)|*.csv|Text File (*.txt)|*.txt",
                    FileName = $"BaoCao_DeXuat_MatCatNgang_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("BẢNG ĐỀ XUẤT MẶT CẮT NGANG ĐƯỜNG VÀ KIỂM TRA TIÊU CHUẨN QUY CHUẨN");
                    sb.AppendLine($"Bộ tiêu chuẩn:,{CurrentInputParameters.StandardName}");
                    sb.AppendLine($"Cấp đường:,{CurrentInputParameters.RoadType}");
                    sb.AppendLine($"Vận tốc thiết kế:,{CurrentInputParameters.DesignSpeed} km/h");
                    sb.AppendLine($"Phân loại mặt cắt:,{(CurrentInputParameters.IsUrbanRoad ? "Đường đô thị (Lòng đường + Bó vỉa + Vỉa hè)" : "Đường ô tô ngoài đô thị (Mặt đường + Lề gia cố + Lề đất)")}");
                    sb.AppendLine($"Mặt xe chạy (Bxc):,{CurrentInputParameters.TotalCarriagewayWidth:F2} m");
                    sb.AppendLine($"Tổng bề rộng MC:,{CurrentInputParameters.TotalProposedWidth:F2} m");
                    sb.AppendLine($"Lộ giới quy hoạch:,{CurrentInputParameters.TargetRightOfWay:F2} m");
                    sb.AppendLine();
                    sb.AppendLine("STT,Yếu tố Cắt ngang,Giá trị Đề xuất,Yêu cầu Tiêu chuẩn,Đánh giá,Ghi chú");

                    for (int i = 0; i < CurrentEvaluationResults.Count; i++)
                    {
                        var item = CurrentEvaluationResults[i];
                        sb.AppendLine($"{i + 1},\"{item.ElementName}\",\"{item.ProposedValue}\",\"{item.StandardRequirement}\",\"{GetStatusText(item.Status)}\",\"{item.Note}\"");
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Xuất báo cáo kết quả thành công ra file:\n{sfd.FileName}", "Xuất báo cáo thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất file báo cáo: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
