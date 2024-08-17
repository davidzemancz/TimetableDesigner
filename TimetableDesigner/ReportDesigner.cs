using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design.Behavior;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;

namespace TimetableDesigner
{
    public class ReportDesigner : Panel
    {
        public float Scale { get; set; } = 0.5f;
        private List<ReportDesignerTextField> textFields = new List<ReportDesignerTextField>();
        private ReportDesignerTextField selectedTextField;
        private PointF lastMousePosition;
        private bool isResizing = false;
        private const int RESIZE_HANDLE_SIZE = 6;

        private const float SNAP_THRESHOLD = 5f; // Threshold for snapping in paper space
        private List<SnapLine> snapLines = new List<SnapLine>();

        // A4 paper dimensions in pixels at 100% scale
        private const float A4_WIDTH = 827;
        private const float A4_HEIGHT = 1169;

        private TextBox editTextBox;
        private ContextMenuStrip contextMenu;

        public ReportDesigner()
        {
            this.DoubleBuffered = true;

            editTextBox = new TextBox
            {
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = this.Font
            };
            editTextBox.KeyDown += EditTextBox_KeyDown;
            this.Controls.Add(editTextBox);

            contextMenu = new ContextMenuStrip();
            var duplicateItem = new ToolStripMenuItem("Duplicate");
            duplicateItem.Click += DuplicateMenuItem_Click;
            contextMenu.Items.Add(duplicateItem);

            this.ContextMenuStrip = contextMenu;
        }

        private void DuplicateMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedTextField != null)
            {
                var newTextField = new ReportDesignerTextField
                {
                    Text = selectedTextField.Text,
                    Location = new PointF(selectedTextField.Location.X + 20, selectedTextField.Location.Y + 20),
                    Size = selectedTextField.Size
                };

                textFields.Add(newTextField);
                selectedTextField = newTextField;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            float paperWidth = A4_WIDTH * Scale;
            float paperHeight = A4_HEIGHT * Scale;
            float x = (this.Width - paperWidth) / 2;
            float y = (this.Height - paperHeight) / 2;

            // Draw A4 paper
            e.Graphics.FillRectangle(Brushes.White, x, y, paperWidth, paperHeight);
            e.Graphics.DrawRectangle(Pens.Black, x, y, paperWidth, paperHeight);

            // Draw text fields
            foreach (var textField in textFields)
            {
                PointF fieldLocation = PaperToControl(textField.Location);
                SizeF fieldSize = new SizeF(textField.Size.Width * Scale, textField.Size.Height * Scale);

                RectangleF rect = new RectangleF(fieldLocation, fieldSize);
                if (textField == selectedTextField)
                {
                    e.Graphics.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(50, 0, 120, 215)), rect);
                    e.Graphics.DrawRectangle(new Pen(System.Drawing.Color.FromArgb(0, 120, 215), 2f), rect.X, rect.Y, rect.Width, rect.Height);
                }
                else
                {
                    e.Graphics.DrawRectangle(Pens.Blue, rect.X, rect.Y, rect.Width, rect.Height);
                }

                // Draw wrapped text
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Near;
                    sf.LineAlignment = StringAlignment.Near;
                    sf.Trimming = StringTrimming.Word;
                    sf.FormatFlags = StringFormatFlags.LineLimit;

                    e.Graphics.DrawString(textField.Text, this.Font, Brushes.Black, rect, sf);
                }

                // Draw resize handle
                e.Graphics.FillRectangle(Brushes.Blue,
                    fieldLocation.X + fieldSize.Width - RESIZE_HANDLE_SIZE,
                    fieldLocation.Y + fieldSize.Height - RESIZE_HANDLE_SIZE,
                    RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE);
            }

            // Draw snap lines
            using (Pen snapPen = new Pen(Color.Red, 1))
            {
                snapPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                foreach (var snapLine in snapLines)
                {
                    if (snapLine.IsVertical)
                    {
                        x = PaperToControl(new PointF(snapLine.Position, 0)).X;
                        e.Graphics.DrawLine(snapPen, x, 0, x, this.Height);
                    }
                    else
                    {
                        y = PaperToControl(new PointF(0, snapLine.Position)).Y;
                        e.Graphics.DrawLine(snapPen, 0, y, this.Width, y);
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            PointF paperPosition = ControlToPaper(e.Location);
            selectedTextField = null;
            isResizing = false;

            var textFieldsReversed = textFields.Reverse<ReportDesignerTextField>();
            foreach (var textField in textFieldsReversed)
            {
                RectangleF fieldRect = new RectangleF(textField.Location, textField.Size);
                if (fieldRect.Contains(paperPosition))
                {
                    selectedTextField = textField;

                    // Check if mouse is over resize handle
                    RectangleF resizeHandle = new RectangleF(
                        fieldRect.Right - RESIZE_HANDLE_SIZE / Scale,
                        fieldRect.Bottom - RESIZE_HANDLE_SIZE / Scale,
                        RESIZE_HANDLE_SIZE / Scale,
                        RESIZE_HANDLE_SIZE / Scale);

                    if (resizeHandle.Contains(paperPosition))
                    {
                        isResizing = true;
                    }
                    break;
                }
            }

            lastMousePosition = paperPosition;

            if (e.Button == MouseButtons.Right && selectedTextField != null)
            {
                contextMenu.Show(this, e.Location);
            }

            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (selectedTextField != null && e.Button == MouseButtons.Left)
            {
                PointF paperPosition = ControlToPaper(e.Location);
                float deltaX = paperPosition.X - lastMousePosition.X;
                float deltaY = paperPosition.Y - lastMousePosition.Y;

                if (isResizing)
                {
                    // Resize the text field
                    selectedTextField.Size = new SizeF(
                        Math.Max(10, selectedTextField.Size.Width + deltaX),
                        Math.Max(10, selectedTextField.Size.Height + deltaY)
                    );
                }
                else
                {
                    PointF newLocation = new PointF(
                        selectedTextField.Location.X + deltaX,
                        selectedTextField.Location.Y + deltaY
                    );

                    // Check if the text field is not moved outside the paper
                    PointF newPaperLocation = ControlToPaper(newLocation);
                    if (newLocation.X < 0) newLocation.X = 0;
                    if (newLocation.Y < 0) newLocation.Y = 0;
                    if (newLocation.X + selectedTextField.Size.Width > A4_WIDTH) newLocation.X = A4_WIDTH - selectedTextField.Size.Width;
                    if (newLocation.Y + selectedTextField.Size.Height > A4_HEIGHT) newLocation.Y = A4_HEIGHT - selectedTextField.Size.Height;

                    // Apply snapping
                    newLocation = ApplySnapping(newLocation);

                    // Move the text field
                    selectedTextField.Location = newLocation;
                }

                lastMousePosition = paperPosition;
                this.Invalidate();
            }
        }

        private PointF ApplySnapping(PointF newLocation)
        {
            snapLines.Clear();
            float snapX = newLocation.X;
            float snapY = newLocation.Y;

            foreach (var field in textFields.Where(f => f != selectedTextField))
            {
                // Snap to left edge
                if (Math.Abs(newLocation.X - field.Location.X) < SNAP_THRESHOLD)
                {
                    snapX = field.Location.X;
                    snapLines.Add(new SnapLine { IsVertical = true, Position = snapX });
                }
                // Snap to right edge
                else if (Math.Abs((newLocation.X + selectedTextField.Size.Width) - (field.Location.X + field.Size.Width)) < SNAP_THRESHOLD)
                {
                    snapX = field.Location.X + field.Size.Width - selectedTextField.Size.Width;
                    snapLines.Add(new SnapLine { IsVertical = true, Position = field.Location.X + field.Size.Width });
                }

                // Snap to top edge
                if (Math.Abs(newLocation.Y - field.Location.Y) < SNAP_THRESHOLD)
                {
                    snapY = field.Location.Y;
                    snapLines.Add(new SnapLine { IsVertical = false, Position = snapY });
                }
                // Snap to bottom edge
                else if (Math.Abs((newLocation.Y + selectedTextField.Size.Height) - (field.Location.Y + field.Size.Height)) < SNAP_THRESHOLD)
                {
                    snapY = field.Location.Y + field.Size.Height - selectedTextField.Size.Height;
                    snapLines.Add(new SnapLine { IsVertical = false, Position = field.Location.Y + field.Size.Height });
                }
            }

            return new PointF(snapX, snapY);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            snapLines.Clear();
            isResizing = false;

            Invalidate();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            PointF paperPosition = ControlToPaper(e.Location);

            foreach (var textField in textFields)
            {
                RectangleF fieldRect = new RectangleF(textField.Location, textField.Size);
                if (fieldRect.Contains(paperPosition))
                {
                    StartEditingTextField(textField);
                    break;
                }
            }
        }

        private void StartEditingTextField(ReportDesignerTextField textField)
        {
            PointF fieldLocation = PaperToControl(textField.Location);
            SizeF fieldSize = new SizeF(textField.Size.Width * Scale, textField.Size.Height * Scale);

            editTextBox.Location = Point.Round(fieldLocation);
            editTextBox.Size = Size.Round(fieldSize);
            editTextBox.Text = textField.Text;
            editTextBox.Tag = textField;
            editTextBox.Multiline = true;
            editTextBox.Visible = true;
            editTextBox.Focus();
        }

        private void EditTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Control)
            {
                FinishEditing();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                // Ensure user wants to cancel editing
                if (MessageBox.Show("Are you sure you want to cancel editing?", "Cancel editing", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    CancelEditing();
                }
            }
        }

        private void CancelEditing()
        {
            editTextBox.Visible = false;
            editTextBox.Tag = null;
            this.Invalidate();
        }

        private void FinishEditing()
        {
            if (editTextBox.Tag is ReportDesignerTextField textField)
            {
                textField.Text = editTextBox.Text;
            }
            editTextBox.Visible = false;
            editTextBox.Tag = null;
            this.Invalidate();
        }

        public void AddTextField(string text, PointF location, SizeF size)
        {
            textFields.Add(new ReportDesignerTextField
            {
                Text = text,
                Location = location,
                Size = size
            });
            this.Invalidate();
        }

        private PointF PaperToControl(PointF paperLocation)
        {
            float paperWidth = A4_WIDTH * Scale;
            float paperHeight = A4_HEIGHT * Scale;
            float x = (this.Width - paperWidth) / 2;
            float y = (this.Height - paperHeight) / 2;

            return new PointF(
                x + paperLocation.X * Scale,
                y + paperLocation.Y * Scale
            );
        }

        private PointF ControlToPaper(PointF controlLocation)
        {
            float paperWidth = A4_WIDTH * Scale;
            float paperHeight = A4_HEIGHT * Scale;
            float x = (this.Width - paperWidth) / 2;
            float y = (this.Height - paperHeight) / 2;

            return new PointF(
                (controlLocation.X - x) / Scale,
                (controlLocation.Y - y) / Scale
            );
        }

        public void ExportToPdf(string filePath)
        {
            using (PdfWriter writer = new PdfWriter(filePath))
            using (PdfDocument pdf = new PdfDocument(writer))
            {
                Document document = new Document(pdf, PageSize.A4);

                float pdfWidth = PageSize.A4.GetWidth();
                float pdfHeight = PageSize.A4.GetHeight();

                // Set page background to white
                document.SetBackgroundColor(ColorConstants.WHITE);

                // Set default font
                PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                document.SetFont(font);

                // Add text fields to the PDF
                foreach (var textField in textFields)
                {
                    // Convert coordinates and size from paper space to PDF space
                    float x = ConvertToPdfSpace(textField.Location.X, A4_WIDTH, pdfWidth);
                    float y = pdfHeight - ConvertToPdfSpace(textField.Location.Y + textField.Size.Height, A4_HEIGHT, pdfHeight);
                    float width = ConvertToPdfSpace(textField.Size.Width, A4_WIDTH, pdfWidth);
                    float height = ConvertToPdfSpace(textField.Size.Height, A4_HEIGHT, pdfHeight);

                    Paragraph p = new Paragraph(textField.Text)
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetFixedPosition(x, y, width)
                        .SetWidth(width)
                        .SetHeight(height); // Add a border for visibility

                    document.Add(p);
                }

                // Close the document
                document.Close();
            }
        }

        private float ConvertToPdfSpace(float value, float originalDimension, float pdfDimension)
        {
            return (value / originalDimension) * pdfDimension;
        }
    }

    public class ReportDesignerTextField
    {
        public string Text { get; set; }
        public PointF Location { get; set; }
        public SizeF Size { get; set; }
    }

    public class SnapLine
    {
        public bool IsVertical { get; set; }
        public float Position { get; set; }
    }
}