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
    /// <summary>
    /// A custom panel for designing reports with text fields and alignment features.
    /// </summary>
    public class TimetableDesigner : Panel
    {
        #region Constants

        // A4 paper dimensions in pixels at 100% scale
        private const float A4_WIDTH = 827;
        private const float A4_HEIGHT = 1169;
        private const int RESIZE_HANDLE_SIZE = 6;
        private const float SNAP_THRESHOLD = 5f; // Threshold for snapping in paper space

        #endregion

        #region Fields and Properties

        public float Scale { get; set; } = 0.5f;
        private List<TimetableDesignerTextField> textFields = new List<TimetableDesignerTextField>();
        private TimetableDesignerTextField selectedTextField;
        private PointF lastMousePosition;
        private bool isResizing = false;
        private List<TimetableDesignerSnapLine> snapLines = new List<TimetableDesignerSnapLine>();
        private TextBox editTextBox;
        private ContextMenuStrip contextMenu;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the TimetableDesigner class.
        /// </summary>
        public TimetableDesigner()
        {
            this.DoubleBuffered = true;

            // Initialize edit text box
            editTextBox = new TextBox
            {
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = this.Font
            };
            editTextBox.KeyDown += EditTextBox_KeyDown;
            this.Controls.Add(editTextBox);

            // Initialize context menu
            contextMenu = new ContextMenuStrip();
            var duplicateItem = new ToolStripMenuItem("Duplicate");
            duplicateItem.Click += DuplicateMenuItem_Click;
            contextMenu.Items.Add(duplicateItem);

            this.ContextMenuStrip = contextMenu;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Paint event of the TimetableDesigner.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawPaper(e.Graphics);
            DrawTextFields(e.Graphics);
            DrawSnapLines(e.Graphics);
        }

        /// <summary>
        /// Handles the MouseDown event of the TimetableDesigner.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            PointF paperPosition = ControlToPaper(e.Location);
            selectedTextField = null;
            isResizing = false;

            SelectTextField(paperPosition);

            lastMousePosition = paperPosition;

            if (e.Button == MouseButtons.Right && selectedTextField != null)
            {
                contextMenu.Show(this, e.Location);
            }

            Invalidate();
        }

        /// <summary>
        /// Handles the MouseMove event of the TimetableDesigner.
        /// </summary>
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
                    ResizeTextField(deltaX, deltaY);
                }
                else
                {
                    MoveTextField(deltaX, deltaY);
                }

                lastMousePosition = paperPosition;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Handles the MouseUp event of the TimetableDesigner.
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            snapLines.Clear();
            isResizing = false;

            Invalidate();
        }

        /// <summary>
        /// Handles the MouseDoubleClick event of the TimetableDesigner.
        /// </summary>
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

        #endregion

        #region Drawing Methods

        /// <summary>
        /// Draws the A4 paper on the panel.
        /// </summary>
        private void DrawPaper(Graphics g)
        {
            float paperWidth = A4_WIDTH * Scale;
            float paperHeight = A4_HEIGHT * Scale;
            float x = (this.Width - paperWidth) / 2;
            float y = (this.Height - paperHeight) / 2;

            g.FillRectangle(Brushes.White, x, y, paperWidth, paperHeight);
            g.DrawRectangle(Pens.Black, x, y, paperWidth, paperHeight);
        }

        /// <summary>
        /// Draws all text fields on the panel.
        /// </summary>
        private void DrawTextFields(Graphics g)
        {
            foreach (var textField in textFields)
            {
                PointF fieldLocation = PaperToControl(textField.Location);
                SizeF fieldSize = new SizeF(textField.Size.Width * Scale, textField.Size.Height * Scale);

                RectangleF rect = new RectangleF(fieldLocation, fieldSize);
                DrawTextField(g, textField, rect);
            }
        }

        /// <summary>
        /// Draws a single text field on the panel.
        /// </summary>
        private void DrawTextField(Graphics g, TimetableDesignerTextField textField, RectangleF rect)
        {
            if (textField == selectedTextField)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 0, 120, 215)), rect);
                g.DrawRectangle(new Pen(Color.FromArgb(0, 120, 215), 2f), rect.X, rect.Y, rect.Width, rect.Height);
            }
            else
            {
                g.DrawRectangle(Pens.Blue, rect.X, rect.Y, rect.Width, rect.Height);
            }

            DrawWrappedText(g, textField.Text, rect);
            DrawResizeHandle(g, rect);
        }

        /// <summary>
        /// Draws wrapped text within a rectangle.
        /// </summary>
        private void DrawWrappedText(Graphics g, string text, RectangleF rect)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
                sf.Trimming = StringTrimming.Word;
                sf.FormatFlags = StringFormatFlags.LineLimit;

                g.DrawString(text, this.Font, Brushes.Black, rect, sf);
            }
        }

        /// <summary>
        /// Draws the resize handle for a text field.
        /// </summary>
        private void DrawResizeHandle(Graphics g, RectangleF rect)
        {
            g.FillRectangle(Brushes.Blue,
                rect.Right - RESIZE_HANDLE_SIZE,
                rect.Bottom - RESIZE_HANDLE_SIZE,
                RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE);
        }

        /// <summary>
        /// Draws snap lines on the panel.
        /// </summary>
        private void DrawSnapLines(Graphics g)
        {
            using (Pen snapPen = new Pen(Color.Red, 1))
            {
                snapPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                foreach (var snapLine in snapLines)
                {
                    if (snapLine.IsVertical)
                    {
                        float x = PaperToControl(new PointF(snapLine.Position, 0)).X;
                        g.DrawLine(snapPen, x, 0, x, this.Height);
                    }
                    else
                    {
                        float y = PaperToControl(new PointF(0, snapLine.Position)).Y;
                        g.DrawLine(snapPen, 0, y, this.Width, y);
                    }
                }
            }
        }

        #endregion

        #region Text Field Manipulation

        /// <summary>
        /// Selects a text field based on the given paper position.
        /// </summary>
        private void SelectTextField(PointF paperPosition)
        {
            var textFieldsReversed = textFields.Reverse<TimetableDesignerTextField>();
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
        }

        /// <summary>
        /// Resizes the selected text field.
        /// </summary>
        private void ResizeTextField(float deltaX, float deltaY)
        {
            selectedTextField.Size = new SizeF(
                Math.Max(10, selectedTextField.Size.Width + deltaX),
                Math.Max(10, selectedTextField.Size.Height + deltaY)
            );
        }

        /// <summary>
        /// Moves the selected text field.
        /// </summary>
        private void MoveTextField(float deltaX, float deltaY)
        {
            PointF newLocation = new PointF(
                selectedTextField.Location.X + deltaX,
                selectedTextField.Location.Y + deltaY
            );

            // Check if the text field is not moved outside the paper
            if (newLocation.X < 0) newLocation.X = 0;
            if (newLocation.Y < 0) newLocation.Y = 0;
            if (newLocation.X + selectedTextField.Size.Width > A4_WIDTH) newLocation.X = A4_WIDTH - selectedTextField.Size.Width;
            if (newLocation.Y + selectedTextField.Size.Height > A4_HEIGHT) newLocation.Y = A4_HEIGHT - selectedTextField.Size.Height;

            // Apply snapping
            newLocation = ApplySnapping(newLocation);

            // Move the text field
            selectedTextField.Location = newLocation;
        }

        /// <summary>
        /// Applies snapping to the given location.
        /// </summary>
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
                    snapLines.Add(new TimetableDesignerSnapLine { IsVertical = true, Position = snapX });
                }
                // Snap to right edge
                else if (Math.Abs((newLocation.X + selectedTextField.Size.Width) - (field.Location.X + field.Size.Width)) < SNAP_THRESHOLD)
                {
                    snapX = field.Location.X + field.Size.Width - selectedTextField.Size.Width;
                    snapLines.Add(new TimetableDesignerSnapLine { IsVertical = true, Position = field.Location.X + field.Size.Width });
                }

                // Snap to top edge
                if (Math.Abs(newLocation.Y - field.Location.Y) < SNAP_THRESHOLD)
                {
                    snapY = field.Location.Y;
                    snapLines.Add(new TimetableDesignerSnapLine { IsVertical = false, Position = snapY });
                }
                // Snap to bottom edge
                else if (Math.Abs((newLocation.Y + selectedTextField.Size.Height) - (field.Location.Y + field.Size.Height)) < SNAP_THRESHOLD)
                {
                    snapY = field.Location.Y + field.Size.Height - selectedTextField.Size.Height;
                    snapLines.Add(new TimetableDesignerSnapLine { IsVertical = false, Position = field.Location.Y + field.Size.Height });
                }
            }

            return new PointF(snapX, snapY);
        }

        /// <summary>
        /// Starts editing the given text field.
        /// </summary>
        private void StartEditingTextField(TimetableDesignerTextField textField)
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

        /// <summary>
        /// Handles the KeyDown event of the edit text box.
        /// </summary>
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

        /// <summary>
        /// Cancels the current text field editing.
        /// </summary>
        private void CancelEditing()
        {
            editTextBox.Visible = false;
            editTextBox.Tag = null;
            this.Invalidate();
        }

        /// <summary>
        /// Finishes the current text field editing
        /// </summary>
        private void FinishEditing()
        {
            if (editTextBox.Tag is TimetableDesignerTextField textField)
            {
                textField.Text = editTextBox.Text;
            }
            editTextBox.Visible = false;
            editTextBox.Tag = null;
            this.Invalidate();
        }

        /// <summary>
        /// Adds a new text field to the timetable.
        /// </summary>
        public void AddTextField(string text, PointF location, SizeF size)
        {
            textFields.Add(new TimetableDesignerTextField
            {
                Text = text,
                Location = location,
                Size = size
            });
            this.Invalidate();
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Converts paper coordinates to control coordinates.
        /// </summary>
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

        /// <summary>
        /// Converts control coordinates to paper coordinates.
        /// </summary>
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

        #endregion

        #region PDF Export

        /// <summary>
        /// Exports the current timetable design to a PDF file.
        /// </summary>
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
                    AddTextFieldToPdf(document, textField, font, pdfWidth, pdfHeight);
                }

                // Close the document
                document.Close();
            }
        }

        /// <summary>
        /// Adds a single text field to the PDF document.
        /// </summary>
        private void AddTextFieldToPdf(Document document, TimetableDesignerTextField textField, PdfFont font, float pdfWidth, float pdfHeight)
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
                .SetHeight(height);

            document.Add(p);
        }

        /// <summary>
        /// Converts a value from paper space to PDF space.
        /// </summary>
        private float ConvertToPdfSpace(float value, float originalDimension, float pdfDimension)
        {
            return (value / originalDimension) * pdfDimension;
        }

        #endregion

        #region Context Menu Handlers

        /// <summary>
        /// Handles the Click event of the Duplicate menu item.
        /// </summary>
        private void DuplicateMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedTextField != null)
            {
                var newTextField = new TimetableDesignerTextField
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

        #endregion
    }

    /// <summary>
    /// Represents a text field in the timetable designer.
    /// </summary>
    public class TimetableDesignerTextField
    {
        public string Text { get; set; }
        public PointF Location { get; set; }
        public SizeF Size { get; set; }
    }

    /// <summary>
    /// Represents a snap line for alignment in the timetable designer.
    /// </summary>
    public class TimetableDesignerSnapLine
    {
        public bool IsVertical { get; set; }
        public float Position { get; set; }
    }
}