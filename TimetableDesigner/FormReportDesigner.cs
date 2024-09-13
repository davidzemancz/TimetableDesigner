using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TimetableDesignerApp
{
    public partial class FormReportDesigner : Form
    {
        public FormReportDesigner()
        {
            InitializeComponent();
        }

        private void FormReportDesigner_Load(object sender, EventArgs e)
        {
            foreach (ReportDesigner.PaperSizes paperSize in Enum.GetValues(typeof(ReportDesigner.PaperSizes)))
            {
                tsCbxPaper.Items.Add(paperSize);
            }
            tsCbxPaper.SelectedItem = reportDesigner1.PaperSize;
        }

        private void tsCbxPaper_SelectedIndexChanged(object sender, EventArgs e)
        {
            var paperSize = (ReportDesigner.PaperSizes)tsCbxPaper.SelectedItem;
            reportDesigner1.PaperSize = paperSize;
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == addElemSection)
            {
                reportDesigner1.AddSection(new RDElementSection());
            }
            else if (e.ClickedItem == addTable)
            {
                reportDesigner1.AddSection(new RDTableSection());
            }
            else if (e.ClickedItem == addElem && reportDesigner1.SelectedSection is RDElementSection elemSection)
            {
                reportDesigner1.AddElement(new RDTextElement(elemSection, "Cus bus", Font, Color.Black) { AutoScaleFont = true });
            }
            else if (e.ClickedItem == zoomIn)
            {
                reportDesigner1.ZoomFactor += 0.1f;
            }
            else if (e.ClickedItem == zoomOut)
            {
                reportDesigner1.ZoomFactor -= 0.1f;
            }
            else if (e.ClickedItem == tsbShowGrid)
            {
                reportDesigner1.ShowGrid = !reportDesigner1.ShowGrid;
            }
            else if (e.ClickedItem == tsbDrawSnaplines)
            {
                reportDesigner1.ShowSnapLines = !reportDesigner1.ShowSnapLines;
            }
            else if (e.ClickedItem == tsbUseSnapLines)
            {
                reportDesigner1.UseSnapLines = !reportDesigner1.UseSnapLines;
            }


        }

       
    }
}
