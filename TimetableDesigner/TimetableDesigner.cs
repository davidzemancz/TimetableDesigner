﻿using PdfSharp.Drawing.Layout;
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
using System.Data;

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
        private const float A5_WIDTH_MM = 148;
        private const float A5_HEIGHT_MM = 210;

        private const float MM_PER_INCH = 25.4f;
        private const int RESIZE_HANDLE_SIZE = 8;
        private const float SNAP_THRESHOLD = 5f; // Threshold for snapping in paper space
        private const int RULER_SIZE = 20; // Width/Height of the rulers
        private const int TICK_SIZE = 5; // Size of tick marks on rulers
        private const int MAJOR_TICK_INTERVAL = 50; // Interval for major tick marks (in paper space)
        const int ELEMENT_MIN_SIZE = 10; // Minimal size of an element

        #endregion

        #region Enums

        public enum PaperSizes
        {
            A4 = 0,
            A5 = 1,
            A4_Landscape = 2,
            A5_Landscape = 3
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets whether snapping is enabled.
        /// </summary>
        public bool SnappingEnabled { get; set; } = true;

        /// <summary>
        /// Scale factor for the paper size.
        /// </summary>
        public float ScaleFactor
        {
            get => scaleFactor;
            set
            {
                scaleFactor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Scale elements font size while resizing.
        /// </summary>
        public bool ScaleFontWhileResizing { get; set; } = false;

        /// <summary>
        /// Gets or sets the margins for the paper in millimeters.
        /// </summary>
        public Padding PaperMargin
        {
            get => paperMargin;
            set
            {
                paperMargin = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Paper size.
        /// </summary>
        public PaperSizes PaperSize
        {
            get => paperSize;
            set
            {
                paperSize = value;
                UpdatePaperDimensions();
                Invalidate();
            }
        }

        #endregion

        #region Private Fields 

        private float paperWidthPixels;
        private float paperHeightPixels;
        private float paperWidthMm;
        private float paperHeightMm;
        private List<TimetableDesignerElement> elements = new List<TimetableDesignerElement>();
        private TimetableDesignerElement selectedElement;
        private PointF lastMousePosition;
        private bool isResizing = false;
        private List<TimetableDesignerSnapLine> snapLines = new List<TimetableDesignerSnapLine>();
        private TextBox editTextBox;
        private ContextMenuStrip contextMenu;
        private Rectangle paperRect;
        private float dpiX, dpiY;
        private PaperSizes paperSize = PaperSizes.A4;
        private float scaleFactor = 0.5f;
        private Padding paperMargin = new Padding(10);

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initializes a new instance of the TimetableDesigner class.
        /// </summary>
        public TimetableDesigner()
        {
            this.DoubleBuffered = true;

            // Initialize edit text box
            InitializeEditTextBox();

            // Initialize context menu
            InitializeContextMenu();
        }

        /// <summary>
        /// Initializes the edit text box for inline text editing.
        /// </summary>
        private void InitializeEditTextBox()
        {
            editTextBox = new TextBox
            {
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = this.Font,
                Multiline = true
            };
            editTextBox.KeyDown += EditTextBox_KeyDown;
            this.Controls.Add(editTextBox);
        }

        /// <summary>
        /// Initializes the context menu for element manipulation.
        /// </summary>
        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            var duplicateItem = new ToolStripMenuItem("Duplicate");
            duplicateItem.Click += (sender, e) => DuplicateSelectedItem();

            var deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.Click += (sender, e) => DeleteSelectedElement();

            var bringToFrontItem = new ToolStripMenuItem("Bring to Front");
            bringToFrontItem.Click += (sender, e) => BringElementToFront(selectedElement);

            var sendToBackItem = new ToolStripMenuItem("Send to Back");
            sendToBackItem.Click += (sender, e) => SendElementToBack(selectedElement);

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                duplicateItem,
                deleteItem,
                new ToolStripSeparator(),
                bringToFrontItem,
                sendToBackItem
            });

            this.ContextMenuStrip = contextMenu;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new text field to the timetable.
        /// </summary>
        public void AddTextField(string text, PointF location, Font font, Color color)
        {
            SizeF size = string.IsNullOrEmpty(text) ? new SizeF(100, 30) : MeasureText(text, font);

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
        /// Adds a line to the timetable.
        /// </summary>
        public void AddLine(PointF startPoint, PointF endPoint, Color lineColor, float lineWidth)
        {
            elements.Add(new TimetableDesignerLine
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                Color = lineColor,
                Width = lineWidth,
            });
            this.Invalidate();
        }

        /// <summary>
        /// Brings the given element to the front of the z-order.
        /// </summary>
        public void BringElementToFront(TimetableDesignerElement element)
        {
            if (elements.Remove(element))
            {
                elements.Add(element);
                Invalidate();
            }
        }

        /// <summary>
        /// Sends the given element to the back of the z-order.
        /// </summary>
        public void SendElementToBack(TimetableDesignerElement element)
        {
            if (elements.Remove(element))
            {
                elements.Insert(0, element);
                Invalidate();
            }
        }

        /// <summary>
        /// Adds a new jizdni rad to the timetable.
        /// </summary>
        public void AddJizdniRad(PointF location, SizeF size, string routeName, List<string> stationNames, List<List<string>> departures)
        {
            elements.Add(new TimetableDesignerJizdniRad
            {
                Location = location,
                Size = size,
                RouteName = routeName,
                StationNames = stationNames,
                Departures = departures,
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

            UpdateDpiAndPaperDimensions(e.Graphics);
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
                    HandleElementDragging(e);
                }
                else
                {
                    UpdateCursor(e);
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

        /// <summary>
        /// Handles the Resize event of the TimetableDesigner.
        /// </summary>
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
            float paperWidth = paperWidthPixels * ScaleFactor;
            float paperHeight = paperHeightPixels * ScaleFactor;
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
        private void DrawRulers(Graphics g)
        {
            // Draw ruler backgrounds
            g.FillRectangle(SystemBrushes.Control, paperRect.Left, paperRect.Top - RULER_SIZE, paperRect.Width, RULER_SIZE); // Top ruler
            g.FillRectangle(SystemBrushes.Control, paperRect.Left - RULER_SIZE, paperRect.Top, RULER_SIZE, paperRect.Height); // Left ruler

            // Mark size
            SizeF markSize = g.MeasureString("100", Font);

            // Draw tick marks and numbers
            DrawHorizontalRuler(g, markSize);
            DrawVerticalRuler(g, markSize);

            // Draw position indicators for selected text field
            if (selectedElement != null)
            {
                DrawRulerPositionIndicators(g);
            }
        }

        /// <summary>
        /// Draws the horizontal ruler with tick marks and numbers.
        /// </summary>
        private void DrawHorizontalRuler(Graphics g, SizeF markSize)
        {
            for (int mm = 0; mm <= paperWidthMm; mm++)
            {
                bool majorTick = mm % MAJOR_TICK_INTERVAL == 0 || mm == paperWidthMm;

                float x = MillimetersToPixelsX(mm) * ScaleFactor + paperRect.Left;
                int tickHeight = majorTick ? TICK_SIZE * 2 : TICK_SIZE;
                g.DrawLine(Pens.Black, x, paperRect.Top - tickHeight, x, paperRect.Top);

                if (majorTick)
                {
                    g.DrawString(mm.ToString(), this.Font, Brushes.Black, x - 10, paperRect.Top - RULER_SIZE);
                }
            }
        }

        /// <summary>
        /// Draws the vertical ruler with tick marks and numbers.
        /// </summary>
        private void DrawVerticalRuler(Graphics g, SizeF markSize)
        {
            for (int mm = 0; mm <= paperHeightMm; mm++)
            {
                bool majorTick = mm % MAJOR_TICK_INTERVAL == 0 || mm == paperHeightMm;

                float y = MillimetersToPixelsY(mm) * ScaleFactor + paperRect.Top;
                int tickWidth = majorTick ? TICK_SIZE * 2 : TICK_SIZE;
                g.DrawLine(Pens.Black, paperRect.Left - tickWidth, y, paperRect.Left, y);

                if (majorTick)
                {
                    g.DrawString(mm.ToString(), this.Font, Brushes.Black, paperRect.Left - markSize.Width - 4, y - 10);
                }
            }
        }

        /// <summary>
        /// Draws position indicators on rulers for the selected element.
        /// </summary>
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

            DrawPositionValues(g, fieldTopLeft, fieldBottomRight);
        }

        /// <summary>
        /// Draws a triangle indicator on the ruler.
        /// </summary>
        private void DrawTriangleIndicator(Graphics g, float x, float y, bool isHorizontal)
        {
            Point[] trianglePoints = isHorizontal
                ? new Point[] { new Point((int)x - 5, (int)y), new Point((int)x + 5, (int)y), new Point((int)x, (int)y + 5) }
                : new Point[] { new Point((int)x, (int)y - 5), new Point((int)x, (int)y + 5), new Point((int)x + 5, (int)y) };

            g.FillPolygon(Brushes.Red, trianglePoints);
        }

        /// <summary>
        /// Draws position values on the rulers for the selected element.
        /// </summary>
        private void DrawPositionValues(Graphics g, PointF fieldTopLeft, PointF fieldBottomRight)
        {
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

        /// <summary>
        /// Draws all elements on the timetable.
        /// </summary>
        private void DrawElements(Graphics g)
        {
            foreach (var element in elements)
            {
                PointF elementLocation = PaperToControl(element.Location);
                SizeF elementSize = new SizeF(element.Size.Width * ScaleFactor, element.Size.Height * ScaleFactor);

                RectangleF rect = new RectangleF(elementLocation, elementSize);

                switch (element)
                {
                    case TimetableDesignerTextField textField:
                        DrawTextField(g, textField, rect);
                        break;
                    case TimetableDesignerRectangle rectangle:
                        DrawRectangle(g, rectangle, rect);
                        break;
                    case TimetableDesignerLine line:
                        DrawLine(g, line);
                        break;
                    case TimetableDesignerJizdniRad jizdniRad:
                        DrawJizdniRad(g, jizdniRad, rect);
                        break;
                }

                if (element == selectedElement) DrawResizeHandle(g, rect);
            }
        }

        /// <summary>
        /// Draws a jizdni rad on the panel.
        /// </summary>
        private void DrawJizdniRad(Graphics g, TimetableDesignerJizdniRad jizdniRad, RectangleF rect)
        {
            const float padding = 5f;
            float rowHeight = rect.Height / (jizdniRad.StationNames.Count + 1);
            float columnWidth = rect.Width / (jizdniRad.Departures.Count + 1);

            // Draw background
            g.FillRectangle(new SolidBrush(Color.White), rect);
            g.DrawRectangle(Pens.Black, rect.X, rect.Y, rect.Width, rect.Height);

            // Draw header
            g.FillRectangle(new SolidBrush(jizdniRad.HeaderColor), rect.X, rect.Y, rect.Width, rowHeight);
            g.DrawString(jizdniRad.RouteName, jizdniRad.HeaderFont, new SolidBrush(jizdniRad.TextColor),
                new RectangleF(rect.X + padding, rect.Y, rect.Width - 2 * padding, rowHeight),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            // Draw station names
            for (int i = 0; i < jizdniRad.StationNames.Count; i++)
            {
                float y = rect.Y + (i + 1) * rowHeight;
                g.DrawString(jizdniRad.StationNames[i], jizdniRad.ContentFont, new SolidBrush(jizdniRad.TextColor),
                    new RectangleF(rect.X + padding, y, columnWidth - 2 * padding, rowHeight),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
            }

            // Draw departures
            for (int i = 0; i < jizdniRad.Departures.Count; i++)
            {
                for (int j = 0; j < jizdniRad.Departures[i].Count; j++)
                {
                    float x = rect.X + (i + 1) * columnWidth;
                    float y = rect.Y + (j + 1) * rowHeight;
                    g.DrawString(jizdniRad.Departures[i][j], jizdniRad.ContentFont, new SolidBrush(jizdniRad.TextColor),
                        new RectangleF(x + padding, y, columnWidth - 2 * padding, rowHeight),
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
            }

            // Draw grid lines
            using (Pen gridPen = new Pen(Color.LightGray))
            {
                for (int i = 1; i <= jizdniRad.StationNames.Count; i++)
                {
                    float y = rect.Y + i * rowHeight;
                    g.DrawLine(gridPen, rect.X, y, rect.Right, y);
                }
                for (int i = 1; i <= jizdniRad.Departures.Count; i++)
                {
                    float x = rect.X + i * columnWidth;
                    g.DrawLine(gridPen, x, rect.Y, x, rect.Bottom);
                }
            }
        }

        /// <summary>
        /// Draws a line on the panel.
        /// </summary>
        private void DrawLine(Graphics g, TimetableDesignerLine line)
        {
            PointF startPoint = PaperToControl(line.StartPoint);
            PointF endPoint = PaperToControl(line.EndPoint);

            using (Pen linePen = new Pen(line.Color, line.Width * ScaleFactor))
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
        /// Deletes the selected element.
        /// </summary>
        private void DeleteSelectedElement()
        {
            if (selectedElement != null)
            {
                elements.Remove(selectedElement);
                selectedElement = null;
                Invalidate();
            }
        }

        /// <summary>
        /// Selects an element based on the given paper position.
        /// </summary>
        private void SelectElement(PointF paperPosition)
        {
            selectedElement = null;
            isResizing = false;

            foreach (var element in elements.AsEnumerable().Reverse())
            {
                RectangleF elementRect = new RectangleF(element.Location, element.Size);
                if (element is TimetableDesignerLine line)
                {
                    if (IsPointNearLine(line, paperPosition))
                    {
                        selectedElement = line;
                        CheckResizeHandle(elementRect, paperPosition);
                        break;
                    }
                }
                else if (elementRect.Contains(paperPosition))
                {
                    selectedElement = element;
                    CheckResizeHandle(elementRect, paperPosition);
                    break;
                }
            }
        }

        /// <summary>
        /// Checks if the mouse is over the resize handle of an element.
        /// </summary>
        private void CheckResizeHandle(RectangleF elementRect, PointF paperPosition)
        {
            RectangleF resizeHandle = GetFieldResizeHandle(elementRect);
            if (resizeHandle.Contains(paperPosition))
            {
                isResizing = true;
            }
        }

        /// <summary>
        /// Checks if a point is near a line.
        /// </summary>
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

        /// <summary>
        /// Gets the rectangle representing the resize handle of a field.
        /// </summary>
        private RectangleF GetFieldResizeHandle(RectangleF fieldRect)
        {
            return new RectangleF(
                fieldRect.Right - RESIZE_HANDLE_SIZE / ScaleFactor,
                fieldRect.Bottom - RESIZE_HANDLE_SIZE / ScaleFactor,
                RESIZE_HANDLE_SIZE / ScaleFactor,
                RESIZE_HANDLE_SIZE / ScaleFactor);
        }

        /// <summary>
        /// Resizes the selected element.
        /// </summary>
        private void ResizeElement(float deltaX, float deltaY)
        {
            if (selectedElement is TimetableDesignerTextField textField)
            {
                ResizeTextField(textField, deltaX, deltaY);
            }
            else if (selectedElement is TimetableDesignerLine line)
            {
                ResizeLine(line, deltaX, deltaY);
            }
            else
            {
                ResizeGenericElement(selectedElement, deltaX, deltaY);
            }
        }

        /// <summary>
        /// Resizes a text field element.
        /// </summary>
        private void ResizeTextField(TimetableDesignerTextField textField, float deltaX, float deltaY)
        {
            selectedElement.Size = new SizeF(
                Math.Max(ELEMENT_MIN_SIZE, selectedElement.Size.Width + deltaX),
                Math.Max(ELEMENT_MIN_SIZE, selectedElement.Size.Height + deltaY)
            );

            if (ScaleFontWhileResizing)
            {
                var scaledFont = new Font(textField.Font.FontFamily, selectedElement.Size.Height / 2.2f, textField.Font.Style, GraphicsUnit.Pixel);
                textField.Font = scaledFont;
            }
        }

        /// <summary>
        /// Resizes a line element.
        /// </summary>
        private void ResizeLine(TimetableDesignerLine line, float deltaX, float deltaY)
        {
            selectedElement.Size = new SizeF(
               selectedElement.Size.Width + deltaX,
                selectedElement.Size.Height + deltaY
            );
        }

        /// <summary>
        /// Resizes a generic element.
        /// </summary>
        private void ResizeGenericElement(TimetableDesignerElement element, float deltaX, float deltaY)
        {
            element.Size = new SizeF(
                Math.Max(ELEMENT_MIN_SIZE, element.Size.Width + deltaX),
                Math.Max(ELEMENT_MIN_SIZE, element.Size.Height + deltaY)
            );
        }

        /// <summary>
        /// Moves the selected element.
        /// </summary>
        private void MoveElement(float deltaX, float deltaY)
        {
            if (selectedElement is TimetableDesignerLine line)
            {
                MoveLine(line, deltaX, deltaY);
            }
            else
            {
                MoveGenericElement(selectedElement, deltaX, deltaY);
            }
        }

        /// <summary>
        /// Moves a line element.
        /// </summary>
        private void MoveLine(TimetableDesignerLine line, float deltaX, float deltaY)
        {
            var startPoint = new PointF(line.StartPoint.X + deltaX, line.StartPoint.Y + deltaY);
            var endPoint = new PointF(line.EndPoint.X + deltaX, line.EndPoint.Y + deltaY);

            line.StartPoint = startPoint;
            line.EndPoint = endPoint;
        }

        /// <summary>
        /// Moves a generic element.
        /// </summary>
        private void MoveGenericElement(TimetableDesignerElement element, float deltaX, float deltaY)
        {
            PointF newLocation = new PointF(
                element.Location.X + deltaX,
                element.Location.Y + deltaY
            );

            // Check if the element is not moved outside the paper
            newLocation = ConstrainToPaper(newLocation, element.Size);

            // Apply snapping
            newLocation = ApplySnapping(newLocation);

            // Move the element
            element.Location = newLocation;
        }

        /// <summary>
        /// Constrains a location to be within the paper boundaries.
        /// </summary>
        private PointF ConstrainToPaper(PointF location, SizeF size)
        {
            location.X = Math.Max(0, Math.Min(location.X, paperWidthPixels - size.Width));
            location.Y = Math.Max(0, Math.Min(location.Y, paperHeightPixels - size.Height));
            return location;
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

            ApplySnappingToMarginLines(newLocation, ref snapX, ref snapY);
            ApplySnappingToElements(newLocation, ref snapX, ref snapY);

            return new PointF(snapX, snapY);
        }

        /// <summary>
        /// Applies snapping to other elements.
        /// </summary>
        private void ApplySnappingToElements(PointF newLocation, ref float snapX, ref float snapY)
        {
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
        }

        /// <summary>
        /// Applies snapping to margin lines.
        /// </summary>
        private void ApplySnappingToMarginLines(PointF newLocation, ref float snapX, ref float snapY)
        {
            var marginSnapLines = GetMarginSnapLines();

            foreach (var marginLine in marginSnapLines)
            {
                if (marginLine.IsVertical)
                {
                    // Snap left edge to margin
                    if (Math.Abs(newLocation.X - marginLine.Position) < SNAP_THRESHOLD)
                    {
                        snapX = marginLine.Position;
                        snapLines.Add(marginLine);
                    }
                    // Snap right edge to margin
                    else if (Math.Abs((newLocation.X + selectedElement.Size.Width) - marginLine.Position) < SNAP_THRESHOLD)
                    {
                        snapX = marginLine.Position - selectedElement.Size.Width;
                        snapLines.Add(marginLine);
                    }
                }
                else
                {
                    // Snap top edge to margin
                    if (Math.Abs(newLocation.Y - marginLine.Position) < SNAP_THRESHOLD)
                    {
                        snapY = marginLine.Position;
                        snapLines.Add(marginLine);
                    }
                    // Snap bottom edge to margin
                    else if (Math.Abs((newLocation.Y + selectedElement.Size.Height) - marginLine.Position) < SNAP_THRESHOLD)
                    {
                        snapY = marginLine.Position - selectedElement.Size.Height;
                        snapLines.Add(marginLine);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the snap lines for the margins.
        /// </summary>
        private List<TimetableDesignerSnapLine> GetMarginSnapLines()
        {
            return new List<TimetableDesignerSnapLine>
            {
                new TimetableDesignerSnapLine { IsVertical = true, Position = MillimetersToPixelsX(PaperMargin.Left) },
                new TimetableDesignerSnapLine { IsVertical = true, Position = paperWidthPixels - MillimetersToPixelsX(PaperMargin.Right) },
                new TimetableDesignerSnapLine { IsVertical = false, Position = MillimetersToPixelsY(PaperMargin.Top) },
                new TimetableDesignerSnapLine { IsVertical = false, Position = paperHeightPixels - MillimetersToPixelsY(PaperMargin.Bottom) }
            };
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
        /// Handles the Click event of the Duplicate menu item.
        /// </summary>
        private void DuplicateSelectedItem()
        {
            if (selectedElement != null)
            {
                var newElement = selectedElement.Clone() as TimetableDesignerElement;
                if (newElement != null)
                {
                    newElement.Location = new PointF(selectedElement.Location.X + 20, selectedElement.Location.Y + 20);
                    elements.Add(newElement);
                    selectedElement = newElement;
                    this.Invalidate();
                }
            }
        }

        #endregion

        #region Unit Conversion

        /// <summary>
        /// Converts millimeters to pixels for the X-axis.
        /// </summary>
        private float MillimetersToPixelsX(float mm)
        {
            return mm * dpiX / MM_PER_INCH;
        }

        /// <summary>
        /// Converts millimeters to pixels for the Y-axis.
        /// </summary>
        private float MillimetersToPixelsY(float mm)
        {
            return mm * dpiY / MM_PER_INCH;
        }

        /// <summary>
        /// Converts pixels to millimeters for the X-axis.
        /// </summary>
        private float PixelsToMillimetersX(float pixels)
        {
            return pixels * MM_PER_INCH / dpiX;
        }

        /// <summary>
        /// Converts pixels to millimeters for the Y-axis.
        /// </summary>
        private float PixelsToMillimetersY(float pixels)
        {
            return pixels * MM_PER_INCH / dpiY;
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Updates the paper dimensions based on the current paper size.
        /// </summary>
        private void UpdatePaperDimensions()
        {
            switch (paperSize)
            {
                case PaperSizes.A4:
                    paperWidthMm = A4_WIDTH_MM;
                    paperHeightMm = A4_HEIGHT_MM;
                    break;
                case PaperSizes.A5:
                    paperWidthMm = A5_WIDTH_MM;
                    paperHeightMm = A5_HEIGHT_MM;
                    break;
                case PaperSizes.A4_Landscape:
                    paperWidthMm = A4_HEIGHT_MM;
                    paperWidthMm = A4_HEIGHT_MM;
                    paperHeightMm = A4_WIDTH_MM;
                    break;
                case PaperSizes.A5_Landscape:
                    paperWidthMm = A5_HEIGHT_MM;
                    paperHeightMm = A5_WIDTH_MM;
                    break;
            }
            UpdatePaperPixels();
        }

        /// <summary>
        /// Updates the paper dimensions in pixels.
        /// </summary>
        private void UpdatePaperPixels()
        {
            paperWidthPixels = MillimetersToPixelsX(paperWidthMm);
            paperHeightPixels = MillimetersToPixelsY(paperHeightMm);
        }

        /// <summary>
        /// Gets a scaled font based on the current scale factor.
        /// </summary>
        private Font GetScaledFont(Font font)
        {
            return new Font(font.FontFamily, font.Size * ScaleFactor, font.Style);
        }

        /// <summary>
        /// Gets the rectangle of the paper in control coordinates.
        /// </summary>
        private Rectangle GetPaperRectangle()
        {
            float paperWidth = paperWidthPixels * ScaleFactor;
            float paperHeight = paperHeightPixels * ScaleFactor;
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
                page.Size = GetPdfPageSize();
                page.Orientation = GetPdfOrientation();
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
                    else if (element is TimetableDesignerLine line)
                    {
                        AddLineToPdf(gfx, line, pdfWidth, pdfHeight);
                    }
                    else if (element is TimetableDesignerJizdniRad jizdniRad)
                    {
                        AddJizdniRadToPdf(gfx, jizdniRad, pdfWidth, pdfHeight);
                    }
                }

                // Save the document
                document.Save(filePath);
            }
        }

        /// <summary>
        /// Gets the appropriate PDF page size based on the current paper size.
        /// </summary>
        private PdfSharp.PageSize GetPdfPageSize()
        {
            switch (paperSize)
            {
                case PaperSizes.A4:
                case PaperSizes.A4_Landscape:
                    return PdfSharp.PageSize.A4;
                case PaperSizes.A5:
                case PaperSizes.A5_Landscape:
                    return PdfSharp.PageSize.A5;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the appropriate PDF orientation based on the current paper size.
        /// </summary>
        private PdfSharp.PageOrientation GetPdfOrientation()
        {
            switch (paperSize)
            {
                case PaperSizes.A4:
                case PaperSizes.A5:
                    return PdfSharp.PageOrientation.Portrait;
                case PaperSizes.A4_Landscape:
                case PaperSizes.A5_Landscape:
                    return PdfSharp.PageOrientation.Landscape;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Adds a jizdni rad to the PDF document.
        /// </summary>
        private void AddJizdniRadToPdf(XGraphics gfx, TimetableDesignerJizdniRad jizdniRad, double pdfWidth, double pdfHeight)
        {
            const double padding = 2;

            double x = ConvertToPdfSpace(jizdniRad.Location.X, paperWidthPixels, pdfWidth);
            double y = ConvertToPdfSpace(jizdniRad.Location.Y, paperHeightPixels, pdfHeight);

            double width = ConvertToPdfSpace(jizdniRad.Size.Width, paperWidthPixels, pdfWidth);
            double height = ConvertToPdfSpace(jizdniRad.Size.Height, paperHeightPixels, pdfHeight);

            XRect rect = new XRect(x, y, width, height);
            gfx.DrawRectangle(XBrushes.White, rect);
            gfx.DrawRectangle(XPens.Black, rect);

            double rowHeight = height / (jizdniRad.StationNames.Count + 1);
            double columnWidth = width / (jizdniRad.Departures.Count + 1);

            // Draw header
            XRect headerRect = new XRect(x, y, width, rowHeight);
            XBrush textBrush = new XSolidBrush(ColorToXColor(jizdniRad.TextColor));
            gfx.DrawRectangle(new XSolidBrush(ColorToXColor(jizdniRad.HeaderColor)), headerRect);
            gfx.DrawString(jizdniRad.RouteName, new XFont(jizdniRad.HeaderFont.Name, jizdniRad.HeaderFont.Size, XFontStyleEx.Bold),
                new XSolidBrush(ColorToXColor(jizdniRad.TextColor)), headerRect, XStringFormats.Center);

            // Draw station names and departures
            XFont contentFont = new XFont(jizdniRad.ContentFont.Name, jizdniRad.ContentFont.Size, XFontStyleEx.Regular);
            for (int i = 0; i < jizdniRad.StationNames.Count; i++)
            {
                double cellY = y + (i + 1) * rowHeight;
                XRect cellRect = new XRect(x + padding, cellY, columnWidth - 2 * padding, rowHeight);
                gfx.DrawString(jizdniRad.StationNames[i], contentFont, textBrush, cellRect, XStringFormats.CenterLeft);
            }

            // Draw departures
            for (int i = 0; i < jizdniRad.Departures.Count; i++)
            {
                for (int j = 0; j < jizdniRad.Departures[i].Count; j++)
                {
                    double cellX = x + (i + 1) * columnWidth;
                    double cellY = y + (j + 1) * rowHeight;
                    XRect cellRect = new XRect(cellX + padding, cellY, columnWidth - 2 * padding, rowHeight);
                    gfx.DrawString(jizdniRad.Departures[i][j], contentFont, textBrush, cellRect, XStringFormats.Center);
                }
            }

            // Draw grid lines
            XPen gridPen = new XPen(XColors.LightGray, 0.5);
            for (int i = 1; i <= jizdniRad.StationNames.Count + 1; i++)
            {
                double lineY = y + i * rowHeight;
                gfx.DrawLine(gridPen, x, lineY, x + width, lineY);
            }
            for (int i = 1; i <= jizdniRad.Departures.Count; i++)
            {
                double lineX = x + i * columnWidth;
                gfx.DrawLine(gridPen, lineX, y, lineX, y + height);
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
            gfx.DrawLine(marginPen, ConvertToPdfSpace(Margin.Left, paperWidthPixels, pdfWidth), 0,
                         ConvertToPdfSpace(Margin.Left, paperWidthPixels, pdfWidth), pdfHeight);

            // Right margin
            gfx.DrawLine(marginPen, pdfWidth - ConvertToPdfSpace(Margin.Right, paperWidthPixels, pdfWidth), 0,
                         pdfWidth - ConvertToPdfSpace(Margin.Right, paperWidthPixels, pdfWidth), pdfHeight);

            // Top margin
            gfx.DrawLine(marginPen, 0, ConvertToPdfSpace(Margin.Top, paperHeightPixels, pdfHeight),
                         pdfWidth, ConvertToPdfSpace(Margin.Top, paperHeightPixels, pdfHeight));

            // Bottom margin
            gfx.DrawLine(marginPen, 0, pdfHeight - ConvertToPdfSpace(Margin.Bottom, paperHeightPixels, pdfHeight),
                         pdfWidth, pdfHeight - ConvertToPdfSpace(Margin.Bottom, paperHeightPixels, pdfHeight));
        }

        /// <summary>
        /// Adds a text field to the PDF document.
        /// </summary>
        private void AddTextFieldToPdf(XGraphics gfx, TimetableDesignerTextField textField, double pdfWidth, double pdfHeight)
        {
            double x = ConvertToPdfSpace(textField.Location.X, paperWidthPixels, pdfWidth);
            double y = ConvertToPdfSpace(textField.Location.Y, paperHeightPixels, pdfHeight);
            double width = ConvertToPdfSpace(textField.Size.Width, paperWidthPixels, pdfWidth);
            double height = ConvertToPdfSpace(textField.Size.Height, paperHeightPixels, pdfHeight);

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
            XBrush brush = new XSolidBrush(ColorToXColor(textField.TextColor));

            XRect layoutRectangle = new XRect(x, y, width, height);
            XTextFormatter tf = new XTextFormatter(gfx);
            tf.DrawString(textField.Text, font, brush, layoutRectangle, XStringFormats.TopLeft);
        }

        /// <summary>
        /// Adds a rectangle to the PDF document.
        /// </summary>
        private void AddRectangleToPdf(XGraphics gfx, TimetableDesignerRectangle rectangle, double pdfWidth, double pdfHeight)
        {
            double x = ConvertToPdfSpace(rectangle.Location.X, paperWidthPixels, pdfWidth);
            double y = ConvertToPdfSpace(rectangle.Location.Y, paperHeightPixels, pdfHeight);
            double width = ConvertToPdfSpace(rectangle.Size.Width, paperWidthPixels, pdfWidth);
            double height = ConvertToPdfSpace(rectangle.Size.Height, paperHeightPixels, pdfHeight);

            XSolidBrush fillBrush = new XSolidBrush(ColorToXColor(rectangle.FillColor));
            XPen borderPen = new XPen(ColorToXColor(rectangle.BorderColor));

            gfx.DrawRectangle(borderPen, fillBrush, x, y, width, height);
        }

        /// <summary>
        /// Adds a line to PDF document
        /// </summary>
        private void AddLineToPdf(XGraphics gfx, TimetableDesignerLine line, double pdfWidth, double pdfHeight)
        {
            double x1 = ConvertToPdfSpace(line.StartPoint.X, paperWidthPixels, pdfWidth);
            double y1 = ConvertToPdfSpace(line.StartPoint.Y, paperHeightPixels, pdfHeight);

            double x2 = ConvertToPdfSpace(line.EndPoint.X, paperWidthPixels, pdfWidth);
            double y2 = ConvertToPdfSpace(line.EndPoint.Y, paperHeightPixels, pdfHeight);

            XPen linePen = new XPen(ColorToXColor(line.Color), line.Width);

            gfx.DrawLine(linePen, x1, y1, x2, y2);
        }

        /// <summary>
        /// Converts a value from paper space to PDF space.
        /// </summary>
        private double ConvertToPdfSpace(float value, float originalDimension, double pdfDimension)
        {
            return (value / originalDimension) * pdfDimension;
        }

        /// <summary>
        /// Converts a System.Drawing.Color to PdfSharp.Drawing.XColor.
        /// </summary>
        private XColor ColorToXColor(Color color)
        {
            return XColor.FromArgb(color.A, color.R, color.G, color.B);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Updates DPI and paper dimensions based on the current graphics object.
        /// </summary>
        private void UpdateDpiAndPaperDimensions(Graphics g)
        {
            dpiX = g.DpiX;
            dpiY = g.DpiY;
            paperWidthPixels = MillimetersToPixelsX(paperWidthMm);
            paperHeightPixels = MillimetersToPixelsY(paperHeightMm);
        }

        /// <summary>
        /// Handles the dragging of an element.
        /// </summary>
        private void HandleElementDragging(MouseEventArgs e)
        {
            PointF paperPosition = ControlToPaper(e.Location);
            float deltaX = paperPosition.X - lastMousePosition.X;
            float deltaY = paperPosition.Y - lastMousePosition.Y;

            if (isResizing)
            {
                Cursor = Cursors.SizeNWSE;
                ResizeElement(deltaX, deltaY);
            }
            else
            {
                Cursor = Cursors.SizeAll;
                MoveElement(deltaX, deltaY);
            }

            lastMousePosition = paperPosition;
            this.Invalidate();
        }

        /// <summary>
        /// Updates the cursor based on the mouse position relative to the selected element.
        /// </summary>
        private void UpdateCursor(MouseEventArgs e)
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

        /// <summary>
        /// Measures the size of the given text using the specified font.
        /// </summary>
        private SizeF MeasureText(string text, Font font)
        {
            SizeF size = CreateGraphics().MeasureString(text, font);
            size.Width += RESIZE_HANDLE_SIZE * 2;
            return size;
        }

        #endregion
    }

    #region Element Classes

    /// <summary>
    /// Base class for all timetable designer elements.
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

    /// <summary>
    /// Represents a text field in the timetable designer.
    /// </summary>
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

    /// <summary>
    /// Represents a rectangle in the timetable designer.
    /// </summary>
    public class TimetableDesignerRectangle : TimetableDesignerElement
    {
        public Color FillColor { get; set; }
        public Color BorderColor { get; set; }
        public float BorderWidth { get; set; }
    }

    /// <summary>
    /// Represents a line in the timetable designer.
    /// </summary>
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

        public Color Color { get; set; }
        public float Width { get; set; }
    }

    /// <summary>
    /// Represents a jizdni rad (timetable) in the timetable designer.
    /// </summary>
    public class TimetableDesignerJizdniRad : TimetableDesignerElement
    {
        public List<string> StationNames { get; set; } = new List<string>();
        public List<List<string>> Departures { get; set; } = new List<List<string>>();
        public string RouteName { get; set; }
        public Color HeaderColor { get; set; } = Color.LightGray;
        public Color TextColor { get; set; } = Color.Black;
        public Font HeaderFont { get; set; }
        public Font ContentFont { get; set; }

        public TimetableDesignerJizdniRad()
        {
            HeaderFont = new Font("Arial", 10, FontStyle.Bold);
            ContentFont = new Font("Arial", 9, FontStyle.Regular);
        }

        public override TimetableDesignerElement Clone()
        {
            var clone = (TimetableDesignerJizdniRad)base.Clone();
            clone.StationNames = new List<string>(StationNames);
            clone.Departures = Departures.Select(l => new List<string>(l)).ToList();
            clone.HeaderFont = (Font)HeaderFont.Clone();
            clone.ContentFont = (Font)ContentFont.Clone();
            return clone;
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

    #endregion
}