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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormReportDesigner));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.addElemSection = new System.Windows.Forms.ToolStripButton();
            this.addTable = new System.Windows.Forms.ToolStripButton();
            this.addElem = new System.Windows.Forms.ToolStripButton();
            this.zoomIn = new System.Windows.Forms.ToolStripButton();
            this.zoomOut = new System.Windows.Forms.ToolStripButton();
            this.tsCbxPaper = new System.Windows.Forms.ToolStripComboBox();
            this.reportDesigner1 = new TimetableDesignerApp.ReportDesigner();
            this.tsbShowGrid = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addElemSection,
            this.addTable,
            this.addElem,
            this.zoomIn,
            this.zoomOut,
            this.tsbShowGrid,
            this.tsCbxPaper});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1500, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStrip1_ItemClicked);
            // 
            // addElemSection
            // 
            this.addElemSection.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.addElemSection.Image = ((System.Drawing.Image)(resources.GetObject("addElemSection.Image")));
            this.addElemSection.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.addElemSection.Name = "addElemSection";
            this.addElemSection.Size = new System.Drawing.Size(120, 22);
            this.addElemSection.Text = "Add element section";
            // 
            // addTable
            // 
            this.addTable.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.addTable.Image = ((System.Drawing.Image)(resources.GetObject("addTable.Image")));
            this.addTable.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.addTable.Name = "addTable";
            this.addTable.Size = new System.Drawing.Size(103, 22);
            this.addTable.Text = "Add table section";
            // 
            // addElem
            // 
            this.addElem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.addElem.Image = ((System.Drawing.Image)(resources.GetObject("addElem.Image")));
            this.addElem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.addElem.Name = "addElem";
            this.addElem.Size = new System.Drawing.Size(79, 22);
            this.addElem.Text = "Add element";
            // 
            // zoomIn
            // 
            this.zoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.zoomIn.Image = ((System.Drawing.Image)(resources.GetObject("zoomIn.Image")));
            this.zoomIn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.zoomIn.Name = "zoomIn";
            this.zoomIn.Size = new System.Drawing.Size(56, 22);
            this.zoomIn.Text = "Zoom in";
            // 
            // zoomOut
            // 
            this.zoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.zoomOut.Image = ((System.Drawing.Image)(resources.GetObject("zoomOut.Image")));
            this.zoomOut.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.zoomOut.Name = "zoomOut";
            this.zoomOut.Size = new System.Drawing.Size(64, 22);
            this.zoomOut.Text = "Zoom out";
            // 
            // tsCbxPaper
            // 
            this.tsCbxPaper.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tsCbxPaper.Name = "tsCbxPaper";
            this.tsCbxPaper.Size = new System.Drawing.Size(121, 25);
            this.tsCbxPaper.SelectedIndexChanged += new System.EventHandler(this.tsCbxPaper_SelectedIndexChanged);
            // 
            // reportDesigner1
            // 
            this.reportDesigner1.AutoScroll = true;
            this.reportDesigner1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.reportDesigner1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportDesigner1.Location = new System.Drawing.Point(0, 25);
            this.reportDesigner1.Name = "reportDesigner1";
            this.reportDesigner1.PaperMargin = new System.Windows.Forms.Padding(10);
            this.reportDesigner1.PaperSize = TimetableDesignerApp.PaperSize.A4;
            this.reportDesigner1.SelectedElement = null;
            this.reportDesigner1.SelectedSection = null;
            this.reportDesigner1.ShowGrid = false;
            this.reportDesigner1.Size = new System.Drawing.Size(1500, 748);
            this.reportDesigner1.TabIndex = 0;
            this.reportDesigner1.ZoomFactor = 1F;
            // 
            // tsbShowGrid
            // 
            this.tsbShowGrid.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbShowGrid.Image = ((System.Drawing.Image)(resources.GetObject("tsbShowGrid.Image")));
            this.tsbShowGrid.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbShowGrid.Name = "tsbShowGrid";
            this.tsbShowGrid.Size = new System.Drawing.Size(64, 22);
            this.tsbShowGrid.Text = "Show grid";
            // 
            // FormReportDesigner
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1500, 773);
            this.Controls.Add(this.reportDesigner1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "FormReportDesigner";
            this.Text = "FormReportDesigner";
            this.Load += new System.EventHandler(this.FormReportDesigner_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ReportDesigner reportDesigner1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton addElemSection;
        private System.Windows.Forms.ToolStripButton addTable;
        private System.Windows.Forms.ToolStripButton addElem;
        private System.Windows.Forms.ToolStripButton zoomIn;
        private System.Windows.Forms.ToolStripButton zoomOut;
        private System.Windows.Forms.ToolStripComboBox tsCbxPaper;
        private System.Windows.Forms.ToolStripButton tsbShowGrid;
    }
}