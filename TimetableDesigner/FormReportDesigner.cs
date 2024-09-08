using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == addElemSection)
            {
                reportDesigner1.AddSection(new RDElementSection());
            }
            else if(e.ClickedItem == addTable)
            {
                reportDesigner1.AddSection(new RDTableSection());
            }
            else if(e.ClickedItem == addElem && reportDesigner1.SelectedSection is RDElementSection elemSection)
            {
                reportDesigner1.AddElement(new RDTextElement(elemSection, "Cus bus", Font, Color.Black));
            }
            else if(e.ClickedItem == zoomIn)
            {
                reportDesigner1.ZoomFactor += 0.1f;
            }
            else if (e.ClickedItem == zoomOut)
            {
                reportDesigner1.ZoomFactor -= 0.1f;
            }
            
        }
    }
}
