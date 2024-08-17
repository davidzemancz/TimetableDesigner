using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TimetableDesigner
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        #region Methods

        private void PrintTest1()
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

            // Initialize PDF writer
            using (var writer = new PdfWriter(outputPath))
            {
                // Initialize PDF document
                using (var pdf = new PdfDocument(writer))
                {
                    // Initialize document
                    using (var document = new Document(pdf))
                    {
                        // Add title
                        document.Add(new Paragraph("Bus Timetable")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(20)
                            .SetBold());

                        // Create table
                        Table table = new Table(new float[] { 1, 2, 2, 2 }).UseAllAvailableWidth();

                        // Add table headers
                        string[] headers = { "Route", "Departure", "Arrival", "Days" };
                        foreach (string header in headers)
                        {
                            table.AddHeaderCell(new Cell().Add(new Paragraph(header).SetBold()));
                        }

                        // Add sample data
                        AddRow(table, "101", "08:00", "09:30", "Mon-Fri");
                        AddRow(table, "101", "10:00", "11:30", "Mon-Fri");
                        AddRow(table, "102", "09:00", "10:15", "Sat-Sun");
                        AddRow(table, "103", "11:00", "12:45", "Daily");

                        // Add the table to the document
                        document.Add(table);
                    }
                }
            }

            // Ask user if they want to open the file

            if (MessageBox.Show("The timetable has been saved to " + outputPath + ". Do you want to open it now?", "Timetable Designer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(outputPath);
            }
        }

        private void AddRow(Table table, string route, string departure, string arrival, string days)
        {
            table.AddCell(new Cell().Add(new Paragraph(route)));
            table.AddCell(new Cell().Add(new Paragraph(departure)));
            table.AddCell(new Cell().Add(new Paragraph(arrival)));
            table.AddCell(new Cell().Add(new Paragraph(days)));
        }

        #endregion

        #region Events

        private void FormMain_Load(object sender, EventArgs e)
        {
            reportDesigner1.ScalingFactor = 0.5f;
        }

        private void tsMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == tsbZoomIn)
            {
                reportDesigner1.ScalingFactor += 0.1f;
                reportDesigner1.Invalidate();
            }
            else if (e.ClickedItem == tsbZoomOut)
            {
                reportDesigner1.ScalingFactor -= 0.1f;
                reportDesigner1.Invalidate();
            }
            else if (e.ClickedItem == tsbAddTextField)
            {
                FontDialog fontDialog = new FontDialog();
                fontDialog.Font = Font;
                fontDialog.ShowColor = true;
                var dr = fontDialog.ShowDialog();
                if(dr != DialogResult.OK) return;
                reportDesigner1.AddTextField("textField", new Point(0, 0), new Size(100, 30), fontDialog.Font, fontDialog.Color);
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
        }


        #endregion

        
    }
}
