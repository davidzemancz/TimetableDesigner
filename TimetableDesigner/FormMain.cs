using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Drawing.Layout;
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
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

      

        #region Events

        private void FormMain_Load(object sender, EventArgs e)
        {
            reportDesigner1.ScaleFactor = 0.65f;
            tsTxbMargin.Text = reportDesigner1.PaperMargin.Left.ToString();
        }

        private void tsActions_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == tsbZoomIn)
            {
                reportDesigner1.ScaleFactor += 0.1f;
                reportDesigner1.Invalidate();
            }
            else if (e.ClickedItem == tsbZoomOut)
            {
                reportDesigner1.ScaleFactor -= 0.1f;
                reportDesigner1.Invalidate();
            }
            else if (e.ClickedItem == tsbSavePdf)
            {
                // Show save file dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PDF files (*.pdf)|*.pdf";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.FileName = "Timetable.pdf";
                var dr = saveFileDialog.ShowDialog();
                if (dr != DialogResult.OK) return;
                string outputPath = saveFileDialog.FileName;

                // Export to PDF
                reportDesigner1.ExportToPdf(outputPath);

                // Ask user if they want to open the file
                if (MessageBox.Show("The timetable has been saved to " + outputPath + ". Do you want to open it now?", "Timetable Designer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(outputPath);
                }

            }
            else if (e.ClickedItem == tsbSnapping)
            {
                reportDesigner1.SnappingEnabled = !tsbSnapping.Checked;
            }
            else if (e.ClickedItem == tsbScalingFont)
            {
                reportDesigner1.ScaleFontWhileResizing = !tsbScalingFont.Checked;
            }
        }

        private void tsTxbMargin_TextChanged(object sender, EventArgs e)
        {
            int.TryParse(tsTxbMargin.Text, out int margin);
            reportDesigner1.PaperMargin = new Padding(margin);
            reportDesigner1.Invalidate();
        }

        private void tsElements_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == tsbAddTextField)
            {
                FontDialog fontDialog = new FontDialog();
                fontDialog.Font = new Font("Arial", 12); // Povolene fonty viz https://docs.pdfsharp.net/PDFsharp/Topics/Fonts/Font-Resolving.html
                fontDialog.ShowColor = true;
                var dr = fontDialog.ShowDialog();
                if (dr != DialogResult.OK) return;
                reportDesigner1.AddTextField("textField", new Point(0, 0), new Size(100, 30), fontDialog.Font, fontDialog.Color);
            }
            else if(e.ClickedItem == tsBtnAddRect)
            {
                ColorDialog colorDialog = new ColorDialog();
                var dr = colorDialog.ShowDialog();
                if (dr != DialogResult.OK) return;
                reportDesigner1.AddRectangle(new Point(0, 0), new Size(100, 50), Color.Empty, colorDialog.Color, 1);
            }
            else if(e.ClickedItem == tsBtnAddFilledRect)
            {
                ColorDialog colorDialog = new ColorDialog();
                var dr = colorDialog.ShowDialog();
                if (dr != DialogResult.OK) return;
                reportDesigner1.AddRectangle(new Point(0, 0), new Size(100, 50), colorDialog.Color, Color.Empty, 0);
            }
            else if(e.ClickedItem == tsBtnAddLine)
            {
                reportDesigner1.AddLine(new Point(0, 0), new Point(100, 100), Color.Black, 1);
            }
        }

        #endregion


    }
}
