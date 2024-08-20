
using PdfSharp.Drawing.Layout;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design.Behavior;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using PdfSharp.Fonts;
using System.Net;

namespace TimetableDesignerApp
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
        private const int RESIZE_HANDLE_SIZE = 8;
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
        public float ScaleFactor { get; set; } = 0.5f;

        /// <summary>
        /// Scale elements font size while resizing.
        /// </summary>
        public bool ScaleFontWhileResizing { get; set; } = false;

        /// <summary>
        /// Gets or sets the margins for the paper in millimeters.
        /// </summary>
        public Padding PaperMargin { get; set; } = new Padding(10); // Default 10mm margin on all sides

        #endregion

        #region Private fields 

        private float a4WidthPixels;
        private float a4HeightPixels;
        private List<TimetableDesignerElement> elements = new List<TimetableDesignerElement>();
        private TimetableDesignerElement selectedElement;
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

        #region Public Methods

        /// <summary>
        /// Adds a new text field to the timetable.
        /// </summary>
        public void AddTextField(string text, PointF location, SizeF size, Font font, Color color)
        {
            elements.Add(new TimetableDesignerTextField
            {
                Text = text,
                Location = location,
                Size = size,
                Font = font,
                TextColor = color
            });
            this.Invalidate();
        }

        /// <summary>
        /// Adds a new rectangle to the timetable.
        /// </summary>
        public void AddRectangle(PointF location, SizeF size, Color fillColor, Color borderColor, float borderWidth)
        {
            elements.Add(new TimetableDesignerRectangle
            {
                Location = location,
                Size = size,
                FillColor = fillColor,
                BorderColor = borderColor,
                BorderWidth = borderWidth
            });
            this.Invalidate();
        }

        /// <summary>
        /// Adds line to the timetable.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="lineColor"></param>
        /// <param name="lineWidth"></param>
        public void AddLine(PointF startPoint, PointF endPoint, Color lineColor, float lineWidth)
        {
            elements.Add(new TimetableDesignerLine
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                LineColor = lineColor,
                LineWidth = lineWidth,
            });
            this.Invalidate();
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
            DrawElements(e.Graphics);
            DrawSnapLines(e.Graphics);
        }

        /// <summary>
        /// Handles the MouseDown event of the TimetableDesigner.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            PointF paperPosition = ControlToPaper(e.Location);
            selectedElement = null;
            isResizing = false;

            SelectElement(paperPosition);

            lastMousePosition = paperPosition;

            if (e.Button == MouseButtons.Right && selectedElement != null)
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
            if (selectedElement != null)
            {
                if (e.Button == MouseButtons.Left)
                {
                    PointF paperPosition = ControlToPaper(e.Location);
                    float deltaX = paperPosition.X - lastMousePosition.X;
                    float deltaY = paperPosition.Y - lastMousePosition.Y;

                    if (isResizing)
                    {
                        Cursor = Cursors.SizeNWSE;
                        ResizeTextField(deltaX, deltaY);
                    }
                    else
                    {
                        Cursor = Cursors.SizeAll;
                        MoveElement(deltaX, deltaY);
                    }

                    lastMousePosition = paperPosition;
                    this.Invalidate();
                }
                else
                {
                    RectangleF fieldRect = new RectangleF(selectedElement.Location, selectedElement.Size);
                    RectangleF innerFieldRect = new RectangleF(fieldRect.Location, fieldRect.Size);
                    float innerRectMargin = fieldRect.Height / 5;
                    innerFieldRect.Inflate(-innerRectMargin, -innerRectMargin);

                    RectangleF resizeHandle = GetFieldResizeHandle(fieldRect);
                    PointF paperPosition = ControlToPaper(e.Location);


                    if (resizeHandle.Contains(paperPosition))
                    {
                        Cursor = Cursors.SizeNWSE;
                    }
                    else if (fieldRect.Contains(paperPosition) && !innerFieldRect.Contains(paperPosition))
                    {
                        Cursor = Cursors.SizeAll;
                    }
                    else
                    {
                        Cursor = Cursors.Default;
                    }
                }
            }
            else
            {
                Cursor = Cursors.Default;
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

            Cursor = Cursors.Default;

            Invalidate();
        }

        /// <summary>
        /// Handles the MouseDoubleClick event of the TimetableDesigner.
        /// </summary>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            PointF paperPosition = ControlToPaper(e.Location);

            foreach (var element in elements)
            {
                RectangleF fieldRect = new RectangleF(element.Location, element.Size);
                if (fieldRect.Contains(paperPosition) && element is TimetableDesignerTextField textField)
                {
                    StartEditingTextField(textField);
                    break;
                }
            }
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);

            Invalidate();
        }

        #endregion

        #region Drawing Methods

        /// <summary>
        /// Draws the A4 paper on the panel.
        /// </summary>
        private void DrawPaper(Graphics g)
        {
            float paperWidth = a4WidthPixels * ScaleFactor;
            float paperHeight = a4HeightPixels * ScaleFactor;
            float x = (this.Width - paperWidth) / 2;
            float y = (this.Height - paperHeight) / 2;

            g.FillRectangle(Brushes.White, x, y, paperWidth, paperHeight);
            g.DrawRectangle(Pens.Black, x, y, paperWidth, paperHeight);

            DrawMarginLines(g, x, y, paperWidth, paperHeight);
        }

        /// <summary>
        /// Draws margin lines on the paper.
        /// </summary>
        private void DrawMarginLines(Graphics g, float x, float y, float paperWidth, float paperHeight)
        {
            using (Pen marginPen = new Pen(Color.LightGray, 1))
            {
                marginPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                // Left margin
                float leftMargin = x + MillimetersToPixelsX(PaperMargin.Left) * ScaleFactor;
                g.DrawLine(marginPen, leftMargin, y, leftMargin, y + paperHeight);

                // Right margin
                float rightMargin = x + paperWidth - MillimetersToPixelsX(PaperMargin.Right) * ScaleFactor;
                g.DrawLine(marginPen, rightMargin, y, rightMargin, y + paperHeight);

                // Top margin
                float topMargin = y + MillimetersToPixelsY(PaperMargin.Top) * ScaleFactor;
                g.DrawLine(marginPen, x, topMargin, x + paperWidth, topMargin);

                // Bottom margin
                float bottomMargin = y + paperHeight - MillimetersToPixelsY(PaperMargin.Bottom) * ScaleFactor;
                g.DrawLine(marginPen, x, bottomMargin, x + paperWidth, bottomMargin);
            }
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
                float x = MillimetersToPixelsX(mm) * ScaleFactor + paperRect.Left;
                int tickHeight = mm % MAJOR_TICK_INTERVAL == 0 ? TICK_SIZE * 2 : TICK_SIZE;
                g.DrawLine(Pens.Black, x, paperRect.Top - tickHeight, x, paperRect.Top);

                if (mm % MAJOR_TICK_INTERVAL == 0)
                {
                    g.DrawString(mm.ToString(), this.Font, Brushes.Black, x - 10, paperRect.Top - RULER_SIZE);
                }
            }

            for (int mm = 0; mm <= A4_HEIGHT_MM; mm++)
            {
                float y = MillimetersToPixelsY(mm) * ScaleFactor + paperRect.Top;
                int tickWidth = mm % MAJOR_TICK_INTERVAL == 0 ? TICK_SIZE * 2 : TICK_SIZE;
                g.DrawLine(Pens.Black, paperRect.Left - tickWidth, y, paperRect.Left, y);

                if (mm % MAJOR_TICK_INTERVAL == 0)
                {
                    g.DrawString(mm.ToString(), this.Font, Brushes.Black, paperRect.Left - markSize.Width - 4, y - 10);
                }
            }

            // Draw position indicators for selected text field
            if (selectedElement != null)
            {
                DrawRulerPositionIndicators(g);
            }
        }

        private void DrawRulerPositionIndicators(Graphics g)
        {
            PointF fieldTopLeft = PaperToControl(selectedElement.Location);
            PointF fieldBottomRight = PaperToControl(new PointF(
                selectedElement.Location.X + selectedElement.Size.Width,
                selectedElement.Location.Y + selectedElement.Size.Height
            ));

            // Draw X position indicators (both on top ruler)
            DrawTriangleIndicator(g, fieldTopLeft.X, paperRect.Top - RULER_SIZE, true);
            DrawTriangleIndicator(g, fieldBottomRight.X, paperRect.Top - RULER_SIZE, true);

            // Draw Y position indicators (both on left ruler)
            DrawTriangleIndicator(g, paperRect.Left - RULER_SIZE, fieldTopLeft.Y, false);
            DrawTriangleIndicator(g, paperRect.Left - RULER_SIZE, fieldBottomRight.Y, false);

            SizeF markSize = g.MeasureString("100 mm", Font);

            // Calculate positions in millimeters
            float leftMm = PixelsToMillimetersX(selectedElement.Location.X);
            float topMm = PixelsToMillimetersY(selectedElement.Location.Y);
            float rightMm = PixelsToMillimetersX(selectedElement.Location.X + selectedElement.Size.Width);
            float bottomMm = PixelsToMillimetersY(selectedElement.Location.Y + selectedElement.Size.Height);

            // Draw position values in millimeters
            float yOffset = paperRect.Top - RULER_SIZE - markSize.Height - 2;
            g.DrawString($"{leftMm:F0} mm", this.Font, Brushes.Black, fieldTopLeft.X - (markSize.Width / 2), yOffset);
            g.DrawString($"{rightMm:F0} mm", this.Font, Brushes.Black, fieldBottomRight.X - (markSize.Width / 2), yOffset);

            float xOffset = paperRect.Left - RULER_SIZE - markSize.Width - 2;
            g.DrawString($"{topMm:F0} mm", this.Font, Brushes.Black, xOffset, fieldTopLeft.Y - (markSize.Height / 2));
            g.DrawString($"{bottomMm:F0} mm", this.Font, Brushes.Black, xOffset, fieldBottomRight.Y - (markSize.Height / 2));
        }

        private void DrawTriangleIndicator(Graphics g, float x, float y, bool isHorizontal)
        {
            Point[] trianglePoints;
            if (isHorizontal)
            {
                trianglePoints = new Point[]
                {
            new Point((int)x - 5, (int)y),
            new Point((int)x + 5, (int)y),
            new Point((int)x, (int)y + 5)
                };
            }
            else
            {
                trianglePoints = new Point[]
                {
            new Point((int)x, (int)y - 5),
            new Point((int)x, (int)y + 5),
            new Point((int)x + 5, (int)y)
                };
            }
            g.FillPolygon(Brushes.Red, trianglePoints);
        }

        /// <summary>
        /// Draws all elements.
        /// </summary>
        private void DrawElements(Graphics g)
        {
            foreach (var element in elements)
            {
                PointF elementLocation = PaperToControl(element.Location);
                SizeF elementSize = new SizeF(element.Size.Width * ScaleFactor, element.Size.Height * ScaleFactor);

                RectangleF rect = new RectangleF(elementLocation, elementSize);

                if (element is TimetableDesignerTextField textField)
                {
                    DrawTextField(g, textField, rect);
                    
                    if (element == selectedElement) DrawResizeHandle(g, rect);
                }
                else if (element is TimetableDesignerRectangle rectangle)
                {
                    DrawRectangle(g, rectangle, rect);
                    if (element == selectedElement) DrawResizeHandle(g, rect);
                }
                else if (element is TimetableDesignerLine line)
                {
                    DrawLine(g, line);
                    if (element == selectedElement) DrawResizeHandle(g, rect);
                }
                
            }
        }

        /// <summary>
        /// Draws a line on the panel.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="line"></param>
        private void DrawLine(Graphics g, TimetableDesignerLine line)
        {
            PointF startPoint = PaperToControl(line.StartPoint);
            PointF endPoint = PaperToControl(line.EndPoint);

            using (Pen linePen = new Pen(line.LineColor, line.LineWidth * ScaleFactor))
            {
                g.DrawLine(linePen, startPoint, endPoint);
            }
        }

        /// <summary>
        /// Draws rectangles on the panel.
        /// </summary>
        private void DrawRectangle(Graphics g, TimetableDesignerRectangle rectangle, RectangleF rect)
        {
            using (SolidBrush fillBrush = new SolidBrush(rectangle.FillColor))
            using (Pen borderPen = new Pen(rectangle.BorderColor, rectangle.BorderWidth * ScaleFactor))
            {
                g.FillRectangle(fillBrush, rect);
                g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
    
        /// <summary>
        /// Draws a single text field on the panel.
        /// </summary>
        private void DrawTextField(Graphics g, TimetableDesignerTextField textField, RectangleF rect)
        {
            if (textField == selectedElement)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 0, 120, 215)), rect);
                g.DrawRectangle(new Pen(Color.FromArgb(0, 120, 215), 2f), rect.X, rect.Y, rect.Width, rect.Height);
            }
            else
            {
                g.DrawRectangle(Pens.Blue, rect.X, rect.Y, rect.Width, rect.Height);
            }

            using (Font scaledFont = GetScaledFont(textField.Font))
            {
                DrawWrappedText(g, textField.Text, rect, scaledFont, textField.TextColor);
                DrawResizeHandle(g, rect);
            }

        }

        /// <summary>
        /// Draws wrapped text within a rectangle.
        /// </summary>
        private void DrawWrappedText(Graphics g, string text, RectangleF rect, Font font, Color textColor)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
                sf.Trimming = StringTrimming.Word;
                sf.FormatFlags = StringFormatFlags.LineLimit;

                using (Brush textBrush = new SolidBrush(textColor))
                {
                    g.DrawString(text, font, textBrush, rect, sf);
                }
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
            if (!SnappingEnabled) return;

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

        #region Element Manipulation

        /// <summary>
        /// Selects a text field based on the given paper position.
        /// </summary>
        private void SelectElement(PointF paperPosition)
        {
            selectedElement = null;
            isResizing = false;

            foreach (var element in elements.Reverse<TimetableDesignerElement>())
            {
                RectangleF elementRect = new RectangleF(element.Location, element.Size);
                if (element is TimetableDesignerLine line)
                {
                    if (IsPointNearLine(line, paperPosition))
                    {
                        selectedElement = line;

                        // Check if mouse is over resize handle
                        RectangleF resizeHandle = GetFieldResizeHandle(elementRect);
                        if (resizeHandle.Contains(paperPosition))
                        {
                            isResizing = true;
                        }

                        break;
                    }
                }
                else
                {
                    if (elementRect.Contains(paperPosition))
                    {
                        selectedElement = element;

                        // Check if mouse is over resize handle
                        RectangleF resizeHandle = GetFieldResizeHandle(elementRect);
                        if (resizeHandle.Contains(paperPosition))
                        {
                            isResizing = true;
                        }

                        break;
                    }
                }
            }
        }

        private bool IsPointNearLine(TimetableDesignerLine line, PointF point)
        {
            const float tolerance = 5f; // Adjust tolerance as needed
            PointF startPoint = line.StartPoint;
            PointF endPoint = line.EndPoint;

            float dx = endPoint.X - startPoint.X;
            float dy = endPoint.Y - startPoint.Y;

            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return false;

            float projection = ((point.X - startPoint.X) * dx + (point.Y - startPoint.Y) * dy) / length;
            if (projection < 0 || projection > length) return false;

            float closestX = startPoint.X + projection * dx / length;
            float closestY = startPoint.Y + projection * dy / length;

            float distance = (float)Math.Sqrt(Math.Pow(closestX - point.X, 2) + Math.Pow(closestY - point.Y, 2));

            return distance <= tolerance;
        }


        private RectangleF GetFieldResizeHandle(RectangleF fieldRect)
        {
            return new RectangleF(
                fieldRect.Right - RESIZE_HANDLE_SIZE / ScaleFactor,
                fieldRect.Bottom - RESIZE_HANDLE_SIZE / ScaleFactor,
                RESIZE_HANDLE_SIZE / ScaleFactor,
                RESIZE_HANDLE_SIZE / ScaleFactor);
        }

        /// <summary>
        /// Resizes the selected text field.
        /// </summary>
        private void ResizeTextField(float deltaX, float deltaY)
        {
            // Scale size
            selectedElement.Size = new SizeF(
                Math.Max(10, selectedElement.Size.Width + deltaX),
                Math.Max(10, selectedElement.Size.Height + deltaY)
            );

            // Scale font
            if (ScaleFontWhileResizing && selectedElement is TimetableDesignerTextField textField)
            {
                var scaledFont = new Font(textField.Font.FontFamily, selectedElement.Size.Height / 2.2f, textField.Font.Style, GraphicsUnit.Pixel);
                textField.Font = scaledFont;
            }

        }

        /// <summary>
        /// Moves the selected text field.
        /// </summary>
        private void MoveElement(float deltaX, float deltaY)
        {
            if (selectedElement is TimetableDesignerLine line)
            {
                var startPoint = new PointF(line.StartPoint.X + deltaX, line.StartPoint.Y + deltaY);
                var endPoint = new PointF(line.EndPoint.X + deltaX, line.EndPoint.Y + deltaY);

                line.StartPoint = startPoint;
                line.EndPoint = endPoint;
                
            }
            else
            {
                PointF newLocation = new PointF(
                selectedElement.Location.X + deltaX,
                    selectedElement.Location.Y + deltaY
                );

                // Check if the text field is not moved outside the paper
                if (newLocation.X < 0) newLocation.X = 0;
                if (newLocation.Y < 0) newLocation.Y = 0;
                if (newLocation.X + selectedElement.Size.Width > a4WidthPixels) newLocation.X = a4WidthPixels - selectedElement.Size.Width;
                if (newLocation.Y + selectedElement.Size.Height > a4HeightPixels) newLocation.Y = a4HeightPixels - selectedElement.Size.Height;

                // Apply snapping
                newLocation = ApplySnapping(newLocation);

                // Move the text field
                selectedElement.Location = newLocation;
            }
        }

        /// <summary>
        /// Applies snapping to the given location.
        /// </summary>
        private PointF ApplySnapping(PointF newLocation)
        {
            if (!SnappingEnabled) return newLocation;

            snapLines.Clear();
            float snapX = newLocation.X;
            float snapY = newLocation.Y;

            foreach (var field in elements.Where(f => f != selectedElement))
            {
                // Snap to left edge
                if (Math.Abs(newLocation.X - field.Location.X) < SNAP_THRESHOLD)
                {
                    snapX = field.Location.X;
                    snapLines.Add(new TimetableDesignerSnapLine { IsVertical = true, Position = snapX });
                }
                // Snap to right edge
                else if (Math.Abs((newLocation.X + selectedElement.Size.Width) - (field.Location.X + field.Size.Width)) < SNAP_THRESHOLD)
                {
                    snapX = field.Location.X + field.Size.Width - selectedElement.Size.Width;
                    snapLines.Add(new TimetableDesignerSnapLine { IsVertical = true, Position = field.Location.X + field.Size.Width });
                }

                // Snap to top edge
                if (Math.Abs(newLocation.Y - field.Location.Y) < SNAP_THRESHOLD)
                {
                    snapY = field.Location.Y;
                    snapLines.Add(new TimetableDesignerSnapLine { IsVertical = false, Position = snapY });
                }
                // Snap to bottom edge
                else if (Math.Abs((newLocation.Y + selectedElement.Size.Height) - (field.Location.Y + field.Size.Height)) < SNAP_THRESHOLD)
                {
                    snapY = field.Location.Y + field.Size.Height - selectedElement.Size.Height;
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
            SizeF fieldSize = new SizeF(textField.Size.Width * ScaleFactor, textField.Size.Height * ScaleFactor);

            editTextBox.Location = Point.Round(fieldLocation);
            editTextBox.Size = Size.Round(fieldSize);
            editTextBox.Text = textField.Text;
            editTextBox.Font = GetScaledFont(textField.Font);
            editTextBox.ForeColor = textField.TextColor;
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

        private Font GetScaledFont(Font font)
        {
            return new Font(font.FontFamily, font.Size * ScaleFactor, font.Style);
        }

        /// <summary>
        /// Gets the rectangle of the paper in control coordinates.
        /// </summary>
        /// <returns></returns>
        private Rectangle GetPaperRectangle()
        {
            float paperWidth = a4WidthPixels * ScaleFactor;
            float paperHeight = a4HeightPixels * ScaleFactor;
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
                paperRect.X + paperLocation.X * ScaleFactor,
                paperRect.Y + paperLocation.Y * ScaleFactor
            );
        }

        /// <summary>
        /// Converts control coordinates to paper coordinates.
        /// </summary>
        private PointF ControlToPaper(PointF controlLocation)
        {
            return new PointF(
                (controlLocation.X - paperRect.X) / ScaleFactor,
                (controlLocation.Y - paperRect.Y) / ScaleFactor
            );
        }

        #endregion

        #region PDF Export

        /// <summary>
        /// Exports the current timetable design to a PDF file using PDFsharp.
        /// </summary>
        public void ExportToPdf(string filePath)
        {
            using (PdfDocument document = new PdfDocument())
            {
                PdfPage page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                XGraphics gfx = XGraphics.FromPdfPage(page);

                double pdfWidth = page.Width.Point;
                double pdfHeight = page.Height.Point;

                // Add margin lines to the PDF
                AddMarginLinesToPdf(gfx, pdfWidth, pdfHeight);

                // Add elements to the PDF
                foreach (var element in elements)
                {
                    if (element is TimetableDesignerTextField textField)
                    {
                        AddTextFieldToPdf(gfx, textField, pdfWidth, pdfHeight);
                    }
                    else if (element is TimetableDesignerRectangle rectangle)
                    {
                        AddRectangleToPdf(gfx, rectangle, pdfWidth, pdfHeight);
                    }
                }

                // Save the document
                document.Save(filePath);
            }
        }

        /// <summary>
        /// Adds margin lines to the PDF document.
        /// </summary>
        private void AddMarginLinesToPdf(XGraphics gfx, double pdfWidth, double pdfHeight)
        {
            XPen marginPen = new XPen(XColors.LightGray, 0.5);
            marginPen.DashStyle = XDashStyle.Dash;

            // Left margin
            gfx.DrawLine(marginPen, ConvertToPdfSpace(Margin.Left, a4WidthPixels, pdfWidth), 0,
                         ConvertToPdfSpace(Margin.Left, a4WidthPixels, pdfWidth), pdfHeight);

            // Right margin
            gfx.DrawLine(marginPen, pdfWidth - ConvertToPdfSpace(Margin.Right, a4WidthPixels, pdfWidth), 0,
                         pdfWidth - ConvertToPdfSpace(Margin.Right, a4WidthPixels, pdfWidth), pdfHeight);

            // Top margin
            gfx.DrawLine(marginPen, 0, ConvertToPdfSpace(Margin.Top, a4HeightPixels, pdfHeight),
                         pdfWidth, ConvertToPdfSpace(Margin.Top, a4HeightPixels, pdfHeight));

            // Bottom margin
            gfx.DrawLine(marginPen, 0, pdfHeight - ConvertToPdfSpace(Margin.Bottom, a4HeightPixels, pdfHeight),
                         pdfWidth, pdfHeight - ConvertToPdfSpace(Margin.Bottom, a4HeightPixels, pdfHeight));
        }

        /// <summary>
        /// Adds a text field to the PDF document.
        /// </summary>
        private void AddTextFieldToPdf(XGraphics gfx, TimetableDesignerTextField textField, double pdfWidth, double pdfHeight)
        {
            double x = ConvertToPdfSpace(textField.Location.X, a4WidthPixels, pdfWidth);
            double y = ConvertToPdfSpace(textField.Location.Y, a4HeightPixels, pdfHeight);
            double width = ConvertToPdfSpace(textField.Size.Width, a4WidthPixels, pdfWidth);
            double height = ConvertToPdfSpace(textField.Size.Height, a4HeightPixels, pdfHeight);

            XFontStyleEx xFontStyleEx = XFontStyleEx.Regular;
            if (textField.Font.Style == FontStyle.Bold)
            {
                xFontStyleEx |= XFontStyleEx.Bold;
            }
            if (textField.Font.Style == FontStyle.Italic)
            {
                xFontStyleEx |= XFontStyleEx.Italic;
            }
           
            XFont font = new XFont(textField.Font.FontFamily.Name, textField.Font.Size, xFontStyleEx);
            XBrush brush = new XSolidBrush(XColor.FromArgb(textField.TextColor.A, textField.TextColor.R, textField.TextColor.G, textField.TextColor.B));

            XRect layoutRectangle = new XRect(x, y, width, height);
            XTextFormatter tf = new XTextFormatter(gfx);
            tf.DrawString(textField.Text, font, brush, layoutRectangle, XStringFormats.TopLeft);
        }

        /// <summary>
        /// Adds a rectangle to the PDF document.
        /// </summary>
        private void AddRectangleToPdf(XGraphics gfx, TimetableDesignerRectangle rectangle, double pdfWidth, double pdfHeight)
        {
            double x = ConvertToPdfSpace(rectangle.Location.X, a4WidthPixels, pdfWidth);
            double y = ConvertToPdfSpace(rectangle.Location.Y, a4HeightPixels, pdfHeight);
            double width = ConvertToPdfSpace(rectangle.Size.Width, a4WidthPixels, pdfWidth);
            double height = ConvertToPdfSpace(rectangle.Size.Height, a4HeightPixels, pdfHeight);

            XSolidBrush fillBrush = new XSolidBrush(XColor.FromArgb(rectangle.FillColor.A, rectangle.FillColor.R, rectangle.FillColor.G, rectangle.FillColor.B));
            XPen borderPen = new XPen(XColor.FromArgb(rectangle.BorderColor.A, rectangle.BorderColor.R, rectangle.BorderColor.G, rectangle.BorderColor.B), rectangle.BorderWidth);

            gfx.DrawRectangle(borderPen, fillBrush, x, y, width, height);
        }

        /// <summary>
        /// Converts a value from paper space to PDF space.
        /// </summary>
        private double ConvertToPdfSpace(float value, float originalDimension, double pdfDimension)
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
            if (selectedElement != null)
            {
                if(selectedElement is TimetableDesignerTextField textField)
                {
                    var newTextField = textField.Clone() as TimetableDesignerTextField;
                    newTextField.Location = new PointF(selectedElement.Location.X + 20, selectedElement.Location.Y + 20);
                    elements.Add(newTextField);
                    selectedElement = newTextField;
                    this.Invalidate();
                }
                else if(selectedElement is TimetableDesignerRectangle rectangle)
                {
                    var newRectangle = rectangle.Clone() as TimetableDesignerRectangle;
                    newRectangle.Location = new PointF(selectedElement.Location.X + 20, selectedElement.Location.Y + 20);
                    elements.Add(newRectangle);
                    selectedElement = newRectangle;
                    this.Invalidate();
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a text field in the timetable designer.
    /// </summary>
    public abstract class TimetableDesignerElement
    {
        public virtual PointF Location { get; set; }
        public virtual SizeF Size { get; set; }

        public virtual TimetableDesignerElement Clone()
        {
            return (TimetableDesignerElement)this.MemberwiseClone();
        }
    }

    public class TimetableDesignerTextField : TimetableDesignerElement
    {
        public string Text { get; set; }
        public Font Font { get; set; }
        public Color TextColor { get; set; }

        public override TimetableDesignerElement Clone()
        {
            var newTextField = (TimetableDesignerTextField)this.MemberwiseClone();
            newTextField.Font = new Font(this.Font.FontFamily, this.Font.Size, this.Font.Style);
            return newTextField;
        }
    }

    public class TimetableDesignerRectangle : TimetableDesignerElement
    {
        public Color FillColor { get; set; }
        public Color BorderColor { get; set; }
        public float BorderWidth { get; set; }

        public override TimetableDesignerElement Clone()
        {
            return (TimetableDesignerElement)this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Represents a snap line for alignment in the timetable designer.
    /// </summary>
    public class TimetableDesignerSnapLine
    {
        public bool IsVertical { get; set; }
        public float Position { get; set; }
    }

    
    public class TimetableDesignerLine : TimetableDesignerElement
    {
        private PointF startPoint;
        private PointF endPoint;

        public PointF StartPoint
        {
            get => startPoint;
            set
            {
                startPoint = value;
                base.Location = startPoint;
                Size = new SizeF(Math.Abs(endPoint.X - startPoint.X), Math.Abs(endPoint.Y - startPoint.Y));
            }
        }
        public PointF EndPoint 
        { 
            get => endPoint; 
            set
            { 
                endPoint = value;
                base.Size = new SizeF(Math.Abs(EndPoint.X - startPoint.X), Math.Abs(EndPoint.Y - startPoint.Y));
            }
        }
        public override PointF Location 
        { 
            get => base.Location;
            set
            {
                base.Location = value;
                StartPoint = value;
            } 
        }

        public override SizeF Size
        {
            get => base.Size;
            set
            {
                base.Size = value;
                EndPoint = new PointF(StartPoint.X + value.Width, StartPoint.Y + value.Height);
            }
        }

        public Color LineColor { get; set; }
        public float LineWidth { get; set; }

        public override TimetableDesignerElement Clone()
        {
            return (TimetableDesignerLine)this.MemberwiseClone();
        }
    }



}