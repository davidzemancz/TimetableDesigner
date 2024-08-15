namespace TimetableDesigner
{
    partial class FormMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.tsMain = new System.Windows.Forms.ToolStrip();
            this.tsbPrintTest1 = new System.Windows.Forms.ToolStripButton();
            this.tsbZoomIn = new System.Windows.Forms.ToolStripButton();
            this.tsbZoomOut = new System.Windows.Forms.ToolStripButton();
            this.tsbAddTextField = new System.Windows.Forms.ToolStripButton();
            this.reportDesigner1 = new TimetableDesigner.ReportDesigner();
            this.tsbSavePdf = new System.Windows.Forms.ToolStripButton();
            this.tsMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // tsMain
            // 
            this.tsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbPrintTest1,
            this.tsbZoomIn,
            this.tsbZoomOut,
            this.tsbAddTextField,
            this.tsbSavePdf});
            this.tsMain.Location = new System.Drawing.Point(0, 0);
            this.tsMain.Name = "tsMain";
            this.tsMain.Size = new System.Drawing.Size(1206, 25);
            this.tsMain.TabIndex = 0;
            this.tsMain.Text = "toolStrip1";
            this.tsMain.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tsMain_ItemClicked);
            // 
            // tsbPrintTest1
            // 
            this.tsbPrintTest1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbPrintTest1.Image = ((System.Drawing.Image)(resources.GetObject("tsbPrintTest1.Image")));
            this.tsbPrintTest1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbPrintTest1.Name = "tsbPrintTest1";
            this.tsbPrintTest1.Size = new System.Drawing.Size(67, 22);
            this.tsbPrintTest1.Text = "Print test 1";
            // 
            // tsbZoomIn
            // 
            this.tsbZoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbZoomIn.Image = ((System.Drawing.Image)(resources.GetObject("tsbZoomIn.Image")));
            this.tsbZoomIn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbZoomIn.Name = "tsbZoomIn";
            this.tsbZoomIn.Size = new System.Drawing.Size(23, 22);
            this.tsbZoomIn.Text = "+";
            // 
            // tsbZoomOut
            // 
            this.tsbZoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbZoomOut.Image = ((System.Drawing.Image)(resources.GetObject("tsbZoomOut.Image")));
            this.tsbZoomOut.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbZoomOut.Name = "tsbZoomOut";
            this.tsbZoomOut.Size = new System.Drawing.Size(23, 22);
            this.tsbZoomOut.Text = "-";
            // 
            // tsbAddTextField
            // 
            this.tsbAddTextField.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAddTextField.Image = ((System.Drawing.Image)(resources.GetObject("tsbAddTextField.Image")));
            this.tsbAddTextField.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbAddTextField.Name = "tsbAddTextField";
            this.tsbAddTextField.Size = new System.Drawing.Size(82, 22);
            this.tsbAddTextField.Text = "Add text field";
            // 
            // reportDesigner1
            // 
            this.reportDesigner1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportDesigner1.Location = new System.Drawing.Point(0, 25);
            this.reportDesigner1.Name = "reportDesigner1";
            this.reportDesigner1.Scale = 0.5F;
            this.reportDesigner1.Size = new System.Drawing.Size(1206, 865);
            this.reportDesigner1.TabIndex = 1;
            // 
            // tsbSavePdf
            // 
            this.tsbSavePdf.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSavePdf.Image = ((System.Drawing.Image)(resources.GetObject("tsbSavePdf.Image")));
            this.tsbSavePdf.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSavePdf.Name = "tsbSavePdf";
            this.tsbSavePdf.Size = new System.Drawing.Size(56, 22);
            this.tsbSavePdf.Text = "Save pdf";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1206, 890);
            this.Controls.Add(this.reportDesigner1);
            this.Controls.Add(this.tsMain);
            this.Name = "FormMain";
            this.Text = "Form1";
            this.tsMain.ResumeLayout(false);
            this.tsMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip tsMain;
        private System.Windows.Forms.ToolStripButton tsbPrintTest1;
        private ReportDesigner reportDesigner1;
        private System.Windows.Forms.ToolStripButton tsbZoomIn;
        private System.Windows.Forms.ToolStripButton tsbZoomOut;
        private System.Windows.Forms.ToolStripButton tsbAddTextField;
        private System.Windows.Forms.ToolStripButton tsbSavePdf;
    }
}

