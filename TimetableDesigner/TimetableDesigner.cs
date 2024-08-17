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
using Rectangle = System.Drawing.Rectangle;

namespace TimetableDesigner
{
    /// <summary>
    /// A custom panel for designing reports with text fields and alignment features.
    /// </summary>
    public class TimetableDesigner : Panel
    {
        #region Constants

        // A4 paper dimensions in pixels at 100% scale
        
        private const float A4_WIDTH_MM = 210;
        private const float A4_HEIGHT_MM = 297;
        private const float MM_PER_INCH = 25.4f;
        private const float POINTS_PER_INCH = 72f;
        private const int RESIZE_HANDLE_SIZE = 6;
        private const float SNAP_THRESHOLD = 5f; // Threshold for snapping in paper space
        private const int RULER_SIZE = 20; // Width/Height of the rulers
        private const int TICK_SIZE = 5; // Size of tick marks on rulers
        private const int MAJOR_TICK_INTERVAL = 100; // Interval for major tick marks (in paper space)

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets whether snapping is enabled.
        /// </summary>
        public bool SnappingEnabled { get; set; } = true;

        /// <summary>
        /// Scale factor for the paper size.
        /// </summary>
        public float ScalingFactor { get; set; } = 0.5f;
        #endregion

        #region Private fields 

        private float a4WidthPixels;
        private float a4HeightPixels;
        private List<TimetableDesignerTextField> textFields = new List<TimetableDesignerTextField>();
        private TimetableDesignerTextField selectedTextField;
        private PointF lastMousePosition;
        private bool isResizing = false;
        private List<TimetableDesignerSnapLine> snapLines = new List<TimetableDesignerSnapLine>();
        private TextBox editTextBox;
        private ContextMenuStrip contextMenu;
        private Rectangle paperRect;
        private float dpiX, dpiY;

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

            dpiX = e.Graphics.DpiX;
            dpiY = e.Graphics.DpiY;
            a4WidthPixels = MillimetersToPixelsX(A4_WIDTH_MM);
            a4HeightPixels = MillimetersToPixelsY(A4_HEIGHT_MM);

            paperRect = GetPaperRectangle();

            DrawPaper(e.Graphics);
            DrawRulers(e.Graphics);
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
            float paperWidth = a4WidthPixels * ScalingFactor;
            float paperHeight = a4HeightPixels * ScalingFactor;
            float x = (this.Width - paperWidth) / 2;
            float y = (this.Height - paperHeight) / 2;

            g.FillRectangle(Brushes.White, x, y, paperWidth, paperHeight);
            g.DrawRectangle(Pens.Black, x, y, paperWidth, paperHeight);
        }

        /// <summary>
        /// Draws rulers on the border of the paper 
        /// </summary>
        /// <param name="g"></param>
        private void DrawRulers(Graphics g)
        {
            // Draw ruler backgrounds
            g.FillRectangle(SystemBrushes.Control, paperRect.Left, paperRect.Top - RULER_SIZE, paperRect.Width, RULER_SIZE); // Top ruler
            g.FillRectangle(SystemBrushes.Control, paperRect.Left - RULER_SIZE, paperRect.Top, RULER_SIZE, paperRect.Height); // Left ruler

            // Mark size
            SizeF markSize = g.MeasureString("100", Font);

            // Draw tick marks and numbers
            for (int mm = 0; mm <= A4_WIDTH_MM; mm++)
            {
                float x = MillimetersToPixelsX(mm) * ScalingFactor + paperRect.Left;
                int tickHeight = mm % MAJOR_TICK_INTERVAL == 0 ? TICK_SIZE * 2 : TICK_SIZE;
                g.DrawLine(Pens.Black, x, paperRect.Top - tickHeight, x, paperRect.Top);

                if (mm % MAJOR_TICK_INTERVAL == 0)
                {
                    g.DrawString(mm.ToString(), this.Font, Brushes.Black, x - 10, paperRect.Top - RULER_SIZE);
                }
            }

            for (int mm = 0; mm <= A4_HEIGHT_MM; mm++)
            {
                float y = MillimetersToPixelsY(mm) * ScalingFactor + paperRect.Top;
                int tickWidth = mm % MAJOR_TICK_INTERVAL == 0 ? TICK_SIZE * 2 : TICK_SIZE;
                g.DrawLine(Pens.Black, paperRect.Left - tickWidth, y, paperRect.Left, y);

                if (mm % MAJOR_TICK_INTERVAL == 0)
                {
                    g.DrawString(mm.ToString(), this.Font, Brushes.Black, paperRect.Left - markSize.Width - 4, y - 10);
                }
            }

            // Draw position indicators for selected text field
            if (selectedTextField != null)
            {
                DrawRulerPositionIndicators(g);
            }
        }

        /// <summary>
        /// Draw position indicators for selected text field
        /// </summary>
        /// <param name="g"></param>
        private void DrawRulerPositionIndicators(Graphics g)
        {
            PointF fieldLocation = PaperToControl(selectedTextField.Location);

            // Draw X position indicator
            g.FillPolygon(Brushes.Red, new Point[] {
                new Point((int)fieldLocation.X - 5, paperRect.Top - RULER_SIZE),
                new Point((int)fieldLocation.X + 5, paperRect.Top - RULER_SIZE),
                new Point((int)fieldLocation.X, paperRect.Top - RULER_SIZE + 5)
            });

            // Draw Y position indicator
            g.FillPolygon(Brushes.Red, new Point[] {
                new Point(paperRect.Left - RULER_SIZE, (int)fieldLocation.Y - 5),
                new Point(paperRect.Left - RULER_SIZE, (int)fieldLocation.Y + 5),
                new Point(paperRect.Left - RULER_SIZE + 5, (int)fieldLocation.Y)
            });

            SizeF markSize = g.MeasureString("100 mm", Font);

            // Draw position values in millimeters
            float xMm = PixelsToMillimetersX(selectedTextField.Location.X);
            float yMm = PixelsToMillimetersY(selectedTextField.Location.Y);
            g.DrawString($"{xMm:F0} mm", this.Font, Brushes.Black, fieldLocation.X - (markSize.Width / 2), paperRect.Top - RULER_SIZE - markSize.Height);
            g.DrawString($"{yMm:F0} mm", this.Font, Brushes.Black, paperRect.Left - RULER_SIZE - markSize.Width, fieldLocation.Y - (markSize.Height / 2));
        }

        /// <summary>
        /// Draws all text fields on the panel.
        /// </summary>
        private void DrawTextFields(Graphics g)
        {
            foreach (var textField in textFields)
            {
                PointF fieldLocation = PaperToControl(textField.Location);
                SizeF fieldSize = new SizeF(textField.Size.Width * ScalingFactor, textField.Size.Height * ScalingFactor);

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
            if(!SnappingEnabled) return;

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
                        fieldRect.Right - RESIZE_HANDLE_SIZE / ScalingFactor,
                        fieldRect.Bottom - RESIZE_HANDLE_SIZE / ScalingFactor,
                        RESIZE_HANDLE_SIZE / ScalingFactor,
                        RESIZE_HANDLE_SIZE / ScalingFactor);

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
            if (newLocation.X + selectedTextField.Size.Width > a4WidthPixels) newLocation.X = a4WidthPixels - selectedTextField.Size.Width;
            if (newLocation.Y + selectedTextField.Size.Height > a4HeightPixels) newLocation.Y = a4HeightPixels - selectedTextField.Size.Height;

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
            if(!SnappingEnabled) return newLocation;

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
            SizeF fieldSize = new SizeF(textField.Size.Width * ScalingFactor, textField.Size.Height * ScalingFactor);

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

        #region Unit Conversion

        private float MillimetersToPixelsX(float mm)
        {
            return mm * dpiX / MM_PER_INCH;
        }

        private float MillimetersToPixelsY(float mm)
        {
            return mm * dpiY / MM_PER_INCH;
        }

        private float PixelsToMillimetersX(float pixels)
        {
            return pixels * MM_PER_INCH / dpiX;
        }

        private float PixelsToMillimetersY(float pixels)
        {
            return pixels * MM_PER_INCH / dpiY;
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Gets the rectangle of the paper in control coordinates.
        /// </summary>
        /// <returns></returns>
        private Rectangle GetPaperRectangle()
        {
            float paperWidth = a4WidthPixels * ScalingFactor;
            float paperHeight = a4HeightPixels * ScalingFactor;
            float x = (this.Width - paperWidth) / 2;
            float y = (this.Height - paperHeight) / 2;

            return new Rectangle((int)x, (int)y, (int)paperWidth, (int)paperHeight);
        }

        /// <summary>
        /// Converts paper coordinates to control coordinates.
        /// </summary>
        private PointF PaperToControl(PointF paperLocation)
        {
            return new PointF(
                paperRect.X + paperLocation.X * ScalingFactor,
                paperRect.Y + paperLocation.Y * ScalingFactor
            );
        }

        /// <summary>
        /// Converts control coordinates to paper coordinates.
        /// </summary>
        private PointF ControlToPaper(PointF controlLocation)
        {
            return new PointF(
                (controlLocation.X - paperRect.X) / ScalingFactor,
                (controlLocation.Y - paperRect.Y) / ScalingFactor
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
            float x = ConvertToPdfSpace(textField.Location.X, a4WidthPixels, pdfWidth);
            float y = pdfHeight - ConvertToPdfSpace(textField.Location.Y + textField.Size.Height, a4HeightPixels, pdfHeight);
            float width = ConvertToPdfSpace(textField.Size.Width, a4WidthPixels, pdfWidth);
            float height = ConvertToPdfSpace(textField.Size.Height, a4HeightPixels, pdfHeight);

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