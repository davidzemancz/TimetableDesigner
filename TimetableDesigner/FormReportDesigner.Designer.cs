namespace TimetableDesignerApp
{
    partial class FormReportDesigner
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.reportDesigner1 = new TimetableDesignerApp.ReportDesigner();
            this.SuspendLayout();
            // 
            // reportDesigner1
            // 
            this.reportDesigner1.AutoScroll = true;
            this.reportDesigner1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.reportDesigner1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportDesigner1.Location = new System.Drawing.Point(0, 0);
            this.reportDesigner1.Name = "reportDesigner1";
            this.reportDesigner1.PaperSize = TimetableDesignerApp.PaperSize.A4;
            this.reportDesigner1.Size = new System.Drawing.Size(1500, 773);
            this.reportDesigner1.TabIndex = 0;
            this.reportDesigner1.ZoomFactor = 1F;
            // 
            // FormReportDesigner
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1500, 773);
            this.Controls.Add(this.reportDesigner1);
            this.Name = "FormReportDesigner";
            this.Text = "FormReportDesigner";
            this.ResumeLayout(false);

        }

        #endregion

        private ReportDesigner reportDesigner1;
    }
}