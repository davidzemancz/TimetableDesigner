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
            timetableDesigner.ScaleFactor = 0.65f;
            tsTxbMargin.Text = timetableDesigner.PaperMargin.Left.ToString();

            // Paper sizes
            foreach (var paperSize in Enum.GetValues(typeof(TimetableDesigner.PaperSizes)))
            {
                tsCbPaperSize.Items.Add(paperSize);
            }
            tsCbPaperSize.SelectedItem = TimetableDesigner.PaperSizes.A4;
        }

        private void tsActions_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == tsbZoomIn)
            {
                timetableDesigner.ScaleFactor += 0.1f;
                timetableDesigner.Invalidate();
            }
            else if (e.ClickedItem == tsbZoomOut)
            {
                timetableDesigner.ScaleFactor -= 0.1f;
                timetableDesigner.Invalidate();
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
                timetableDesigner.ExportToPdf(outputPath);

                // Ask user if they want to open the file
                if (MessageBox.Show("The timetable has been saved to " + outputPath + ". Do you want to open it now?", "Timetable Designer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(outputPath);
                }

            }
            else if (e.ClickedItem == tsbSnapping)
            {
                timetableDesigner.SnappingEnabled = !tsbSnapping.Checked;
            }
            else if (e.ClickedItem == tsbScalingFont)
            {
                timetableDesigner.ScaleFontWhileResizing = !tsbScalingFont.Checked;
            }
        }

        private void tsTxbMargin_TextChanged(object sender, EventArgs e)
        {
            int.TryParse(tsTxbMargin.Text, out int margin);
            timetableDesigner.PaperMargin = new Padding(margin);
            timetableDesigner.Invalidate();
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
                timetableDesigner.AddTextField("textField", new Point(0, 0), new Size(100, 30), fontDialog.Font, fontDialog.Color);
            }
            else if(e.ClickedItem == tsBtnAddRect)
            {
                ColorDialog colorDialog = new ColorDialog();
                var dr = colorDialog.ShowDialog();
                if (dr != DialogResult.OK) return;
                timetableDesigner.AddRectangle(new Point(0, 0), new Size(100, 50), Color.Empty, colorDialog.Color, 1);
            }
            else if(e.ClickedItem == tsBtnAddFilledRect)
            {
                ColorDialog colorDialog = new ColorDialog();
                var dr = colorDialog.ShowDialog();
                if (dr != DialogResult.OK) return;
                timetableDesigner.AddRectangle(new Point(0, 0), new Size(100, 50), colorDialog.Color, Color.Empty, 0);
            }
            else if(e.ClickedItem == tsBtnAddLine)
            {
                timetableDesigner.AddLine(new Point(0, 0), new Point(100, 100), Color.Black, 1);
            }
        }

        private void tsCbPaperSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            timetableDesigner.PaperSize = (TimetableDesigner.PaperSizes)tsCbPaperSize.SelectedItem;
        }

        #endregion


    }
}
