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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.tsMain = new System.Windows.Forms.ToolStrip();
            this.tsbZoomIn = new System.Windows.Forms.ToolStripButton();
            this.tsbZoomOut = new System.Windows.Forms.ToolStripButton();
            this.tsbAddTextField = new System.Windows.Forms.ToolStripButton();
            this.tsbSavePdf = new System.Windows.Forms.ToolStripButton();
            this.tsbSnapping = new System.Windows.Forms.ToolStripButton();
            this.reportDesigner1 = new TimetableDesigner();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.tsMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // tsMain
            // 
            this.tsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbZoomIn,
            this.tsbZoomOut,
            this.tsbAddTextField,
            this.tsbSnapping,
            this.tsbSavePdf});
            this.tsMain.Location = new System.Drawing.Point(0, 0);
            this.tsMain.Name = "tsMain";
            this.tsMain.Size = new System.Drawing.Size(1206, 25);
            this.tsMain.TabIndex = 0;
            this.tsMain.Text = "toolStrip1";
            this.tsMain.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tsMain_ItemClicked);
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
            // tsbSavePdf
            // 
            this.tsbSavePdf.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSavePdf.Image = ((System.Drawing.Image)(resources.GetObject("tsbSavePdf.Image")));
            this.tsbSavePdf.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSavePdf.Name = "tsbSavePdf";
            this.tsbSavePdf.Size = new System.Drawing.Size(73, 22);
            this.tsbSavePdf.Text = "Save to .pdf";
            // 
            // tsbSnapping
            // 
            this.tsbSnapping.Checked = true;
            this.tsbSnapping.CheckOnClick = true;
            this.tsbSnapping.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsbSnapping.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSnapping.Image = ((System.Drawing.Image)(resources.GetObject("tsbSnapping.Image")));
            this.tsbSnapping.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSnapping.Name = "tsbSnapping";
            this.tsbSnapping.Size = new System.Drawing.Size(61, 22);
            this.tsbSnapping.Text = "Snapping";
            // 
            // reportDesigner1
            // 
            this.reportDesigner1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportDesigner1.Location = new System.Drawing.Point(0, 25);
            this.reportDesigner1.Name = "reportDesigner1";
            this.reportDesigner1.ScalingFactor = 0.5F;
            this.reportDesigner1.Size = new System.Drawing.Size(1206, 865);
            this.reportDesigner1.SnappingEnabled = true;
            this.reportDesigner1.TabIndex = 1;
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
        private TimetableDesigner reportDesigner1;
        private System.Windows.Forms.ToolStripButton tsbZoomIn;
        private System.Windows.Forms.ToolStripButton tsbZoomOut;
        private System.Windows.Forms.ToolStripButton tsbAddTextField;
        private System.Windows.Forms.ToolStripButton tsbSavePdf;
        private System.Windows.Forms.ToolStripButton tsbSnapping;
        private System.Windows.Forms.FontDialog fontDialog1;
    }
}

