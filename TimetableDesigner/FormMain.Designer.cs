namespace TimetableDesignerApp
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
            this.tsActions = new System.Windows.Forms.ToolStrip();
            this.tsbZoomIn = new System.Windows.Forms.ToolStripButton();
            this.tsbZoomOut = new System.Windows.Forms.ToolStripButton();
            this.tsbScalingFont = new System.Windows.Forms.ToolStripButton();
            this.tsbSnapping = new System.Windows.Forms.ToolStripButton();
            this.tsbSavePdf = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tsTxbMargin = new System.Windows.Forms.ToolStripTextBox();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.reportDesigner1 = new TimetableDesignerApp.TimetableDesigner();
            this.tsElements = new System.Windows.Forms.ToolStrip();
            this.tsbAddTextField = new System.Windows.Forms.ToolStripButton();
            this.tsActions.SuspendLayout();
            this.reportDesigner1.SuspendLayout();
            this.tsElements.SuspendLayout();
            this.SuspendLayout();
            // 
            // tsActions
            // 
            this.tsActions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbZoomIn,
            this.tsbZoomOut,
            this.tsbScalingFont,
            this.tsbSnapping,
            this.tsbSavePdf,
            this.toolStripLabel1,
            this.tsTxbMargin});
            this.tsActions.Location = new System.Drawing.Point(0, 0);
            this.tsActions.Name = "tsActions";
            this.tsActions.Size = new System.Drawing.Size(1206, 25);
            this.tsActions.TabIndex = 0;
            this.tsActions.Text = "toolStrip1";
            this.tsActions.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tsActions_ItemClicked);
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
            // tsbScalingFont
            // 
            this.tsbScalingFont.CheckOnClick = true;
            this.tsbScalingFont.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbScalingFont.Image = ((System.Drawing.Image)(resources.GetObject("tsbScalingFont.Image")));
            this.tsbScalingFont.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbScalingFont.Name = "tsbScalingFont";
            this.tsbScalingFont.Size = new System.Drawing.Size(137, 22);
            this.tsbScalingFont.Text = "Scale font while resizing";
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
            // tsbSavePdf
            // 
            this.tsbSavePdf.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSavePdf.Image = ((System.Drawing.Image)(resources.GetObject("tsbSavePdf.Image")));
            this.tsbSavePdf.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSavePdf.Name = "tsbSavePdf";
            this.tsbSavePdf.Size = new System.Drawing.Size(73, 22);
            this.tsbSavePdf.Text = "Save to .pdf";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(81, 22);
            this.toolStripLabel1.Text = "Margin: (mm)";
            // 
            // tsTxbMargin
            // 
            this.tsTxbMargin.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.tsTxbMargin.Name = "tsTxbMargin";
            this.tsTxbMargin.Size = new System.Drawing.Size(50, 25);
            this.tsTxbMargin.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tsTxbMargin.TextChanged += new System.EventHandler(this.tsTxbMargin_TextChanged);
            // 
            // reportDesigner1
            // 
            this.reportDesigner1.Controls.Add(this.tsElements);
            this.reportDesigner1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportDesigner1.Location = new System.Drawing.Point(0, 25);
            this.reportDesigner1.Name = "reportDesigner1";
            this.reportDesigner1.PaperMargin = new System.Windows.Forms.Padding(10);
            this.reportDesigner1.ScaleFactor = 0.5F;
            this.reportDesigner1.ScaleFontWhileResizing = false;
            this.reportDesigner1.Size = new System.Drawing.Size(1206, 865);
            this.reportDesigner1.SnappingEnabled = true;
            this.reportDesigner1.TabIndex = 1;
            // 
            // tsElements
            // 
            this.tsElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbAddTextField});
            this.tsElements.Location = new System.Drawing.Point(0, 0);
            this.tsElements.Name = "tsElements";
            this.tsElements.Size = new System.Drawing.Size(1206, 25);
            this.tsElements.TabIndex = 1;
            this.tsElements.Text = "toolStrip1";
            this.tsElements.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tsElements_ItemClicked);
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
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1206, 890);
            this.Controls.Add(this.reportDesigner1);
            this.Controls.Add(this.tsActions);
            this.Name = "FormMain";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.tsActions.ResumeLayout(false);
            this.tsActions.PerformLayout();
            this.reportDesigner1.ResumeLayout(false);
            this.reportDesigner1.PerformLayout();
            this.tsElements.ResumeLayout(false);
            this.tsElements.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip tsActions;
        private TimetableDesigner reportDesigner1;
        private System.Windows.Forms.ToolStripButton tsbZoomIn;
        private System.Windows.Forms.ToolStripButton tsbZoomOut;
        private System.Windows.Forms.ToolStripButton tsbSavePdf;
        private System.Windows.Forms.ToolStripButton tsbSnapping;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.ToolStripButton tsbScalingFont;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox tsTxbMargin;
        private System.Windows.Forms.ToolStrip tsElements;
        private System.Windows.Forms.ToolStripButton tsbAddTextField;
    }
}

