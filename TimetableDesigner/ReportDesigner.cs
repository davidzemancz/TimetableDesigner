using PdfSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace TimetableDesignerApp
{
   

    /// <summary>
    /// Main class for designing reports. Inherits from Panel for UI integration.
    /// </summary>
    public class ReportDesigner : Panel
    {
        #region Constants

        private const float MM_PER_INCH = 25.4f;
        private const float A4_WIDTH_MM = 210f;
        private const float A4_HEIGHT_MM = 297f;
        private const float A5_WIDTH_MM = 148f;
        private const float A5_HEIGHT_MM = 210f;
        private const float GRID_SIZE_MM = 10f;
        private const float PAPER_TOP_MARGIN_MM = 20f;
        private const int RESIZE_HANDLE_HEIGHT_MM = 1;
        private const float RULER_WIDTH_MM = 10f;
        private const float RULER_TICK_SIZE_MM = 3f;
        private const float RULER_FONT_SIZE_PT = 12;
        private const float RESIZE_HANDLE_SIZE_MM = 2;
        private const int MIN_TEXT_ELEMENT_HEIGHT = 3;
        private const int MIN_TEXT_ELEMENT_WIDTH = 5;
        private const int RULER_ARROW_SIZE = 5;

        #endregion

        #region Enums

        /// <summary>
        /// Enumeration of supported paper sizes.
        /// </summary>
        public enum PaperSizes
        {
            A4,
            A4Landscape,
            A5,
            A5Landscape
        }

        #endregion

        #region Fields

        private List<RDSection> sections = new List<RDSection>();
        private RDSection selectedSection;
        private RDElement selectedElement;
        private PaperSizes paperSize;
        private float paperWidthMm;
        private float paperHeightMm;
        private float dpiX;
        private float dpiY;
        private float zoomFactor = 1.0f;
        private float resizeStartY;
        private Padding paperMargin = new Padding(10);
        private List<RDElement> elements = new List<RDElement>();
        private bool showGrid;
        private PointF moveStartOffset;
        private bool isMovingElement;
        private bool isResizingSection;
        private bool isResizingElement;
        private PointF resizeStartOffset;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the currently selected element.
        /// </summary>
        public RDElement SelectedElement
        {
            get => selectedElement;
            set
            {
                selectedElement = value;
                if (selectedElement != null) SelectedSection = selectedElement.ParentSection;
                else Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the currently selected section.
        /// </summary>
        public RDSection SelectedSection
        {
            get => selectedSection;
            set
            {
                selectedSection = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the paper size of the report.
        /// </summary>
        public PaperSizes PaperSize
        {
            get => paperSize;
            set
            {
                paperSize = value;
                UpdatePaperSize();
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the zoom factor of the report view.
        /// </summary>
        public float ZoomFactor
        {
            get => zoomFactor;
            set
            {
                zoomFactor = Math.Max(0.1f, Math.Min(value, 5.0f));
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the margins inside the paper.
        /// </summary>
        public Padding PaperMargin
        {
            get => paperMargin;
            set
            {
                paperMargin = value;
                UpdateSectionPositions();
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets whether to show the grid.
        /// </summary>
        public bool ShowGrid
        {
            get => showGrid;
            set
            {
                showGrid = value;
                Invalidate();
            }
        }

        #region Private properties
        
        private float PaperX => Math.Max(0, (ClientSize.Width / zoomFactor - MmToPixels(paperWidthMm, dpiX)) / 2);
        private float PaperY => MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);

        #endregion

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initializes a new instance of the ReportDesigner class.
        /// </summary>
        public ReportDesigner()
        {
            PaperSize = PaperSizes.A4;
            UpdateDpi();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
        }

        /// <summary>
        /// Updates the DPI values used for drawing.
        /// </summary>
        private void UpdateDpi()
        {
            using (Graphics g = CreateGraphics())
            {
                dpiX = g.DpiX;
                dpiY = g.DpiY;
            }
        }

        #endregion

        #region Section and Element Management

        /// <summary>
        /// Adds a new section to the report.
        /// </summary>
        /// <param name="section">The section to add.</param>
        public void AddSection(RDSection section)
        {
            sections.Add(section);
            UpdateSectionPositions();
            SelectedSection = section;
            Invalidate();
        }

        /// <summary>
        /// Removes a section from the report.
        /// </summary>
        /// <param name="section">The section to remove.</param>
        public void RemoveSection(RDSection section)
        {
            if (sections.Remove(section))
            {
                if (section is RDElementSection elementSection)
                {
                    elements.RemoveAll(e => e.ParentSection == elementSection);
                }
                UpdateSectionPositions();
                SelectedSection = null;
                Invalidate();
            }
        }

        /// <summary>
        /// Adds a new element to the report.
        /// </summary>
        /// <param name="element">The element to add.</param>
        public void AddElement(RDElement element)
        {
            if (element.ParentSection is RDElementSection)
            {
                // Default position
                element.LocationMM = new PointF(0, 0);

                // Setup element size
                if (element is RDTextElement textElement)
                {
                    using (Graphics g = CreateGraphics()) 
                    {
                        SizeF stringSize = g.MeasureString(textElement.Text, textElement.Font);
                        textElement.WidthMM = PixelsToMm(stringSize.Width, dpiX);
                        textElement.HeightMM = PixelsToMm(stringSize.Height, dpiY);
                    }
                }
                else
                {
                    element.WidthMM = 50;
                    element.HeightMM = 50;
                }

                elements.Add(element);
                SelectedElement = element;
                Invalidate();
            }
            else
            {
                throw new ArgumentException("Element must have a valid parent section.");
            }
        }

        /// <summary>
        /// Removes an element from the report.
        /// </summary>
        /// <param name="element">The element to remove.</param>
        public void RemoveElement(RDElement element)
        {
            if (elements.Remove(element))
            {
                if (SelectedElement == element)
                {
                    SelectedElement = null;
                }
                Invalidate();
            }
        }

        #endregion

        #region Drawing Methods

        /// <summary>
        /// Handles the paint event for the ReportDesigner.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(BackColor);
            e.Graphics.ScaleTransform(zoomFactor, zoomFactor);

            DrawPaper(e.Graphics);
            DrawGrid(e.Graphics);
            DrawRulers(e.Graphics);
            DrawSections(e.Graphics);
            DrawElements(e.Graphics);
        }

        /// <summary>
        /// Draws the paper on the graphics object.
        /// </summary>
        private void DrawPaper(Graphics g)
        {
            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float paperHeightPixels = MmToPixels(paperHeightMm, dpiY);
            float topMarginPixels = MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);

            g.FillRectangle(Brushes.White, PaperX, topMarginPixels, paperWidthPixels, paperHeightPixels);
            g.DrawRectangle(Pens.Black, PaperX, topMarginPixels, paperWidthPixels, paperHeightPixels);
        }

        /// <summary>
        /// Draws the grid on the graphics object.
        /// </summary>
        private void DrawGrid(Graphics g)
        {
            if (!showGrid) return;

            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float paperHeightPixels = MmToPixels(paperHeightMm, dpiY);
            float gridSizePixels = MmToPixels(GRID_SIZE_MM, dpiX);

            using (Pen gridPen = new Pen(Color.LightGray, 1))
            {
                // Draw vertical grid lines
                for (float gridX = PaperX + gridSizePixels; gridX <= PaperX + paperWidthPixels; gridX += gridSizePixels)
                {
                    g.DrawLine(gridPen, gridX, PaperY, gridX, PaperY + paperHeightPixels);
                }

                // Draw horizontal grid lines
                for (float gridY = PaperY + gridSizePixels; gridY <= PaperY + paperHeightPixels; gridY += gridSizePixels)
                {
                    g.DrawLine(gridPen, PaperX, gridY, PaperX + paperWidthPixels, gridY);
                }
            }
        }

        /// <summary>
        /// Draws the rulers on the graphics object.
        /// </summary>
        private void DrawRulers(Graphics g)
        {
            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float paperHeightPixels = MmToPixels(paperHeightMm, dpiY);
            float rulerWidthPixels = MmToPixels(RULER_WIDTH_MM, dpiX);

            using (Font rulerFont = new Font(Font.FontFamily, RULER_FONT_SIZE_PT, FontStyle.Regular))
            using (Brush textBrush = new SolidBrush(Color.Black))
            using (Pen rulerPen = new Pen(Color.Black, 1))
            {
                // Draw horizontal ruler
                for (int i = 0; i <= paperWidthMm; i += 10)
                {
                    float tickX = PaperX + MmToPixels(i, dpiX);
                    float tickHeight = MmToPixels(i % 50 == 0 ? RULER_TICK_SIZE_MM * 2 : RULER_TICK_SIZE_MM, dpiY);
                    g.DrawLine(rulerPen, tickX, PaperY - tickHeight, tickX, PaperY);

                    if (i % 50 == 0)
                    {
                        string label = i.ToString();
                        SizeF labelSize = g.MeasureString(label, rulerFont);
                        g.DrawString(label, rulerFont, textBrush, tickX - labelSize.Width / 2, PaperY - tickHeight - labelSize.Height);
                    }
                }

                // Draw vertical ruler
                for (int i = 0; i <= paperHeightMm; i += 10)
                {
                    float tickY = PaperY + MmToPixels(i, dpiY);
                    float tickWidth = MmToPixels(i % 50 == 0 ? RULER_TICK_SIZE_MM * 2 : RULER_TICK_SIZE_MM, dpiX);
                    g.DrawLine(rulerPen, PaperX - tickWidth, tickY, PaperX, tickY);

                    if (i % 50 == 0)
                    {
                        string label = i.ToString();
                        SizeF labelSize = g.MeasureString(label, rulerFont);
                        g.DrawString(label, rulerFont, textBrush, PaperX - tickWidth - labelSize.Width - 2, tickY - labelSize.Height / 2);
                    }
                }
            }
        }

        /// <summary>
        /// Draws all sections on the graphics object.
        /// </summary>
        private void DrawSections(Graphics g)
        {
            foreach (var section in sections)
            {
                bool selected = section == selectedSection;
                DrawSection(g, section, selected);
                if (section.Resizable)
                {
                    DrawSectionResizeHandle(g, section);
                }
            }
        }

        /// <summary>
        /// Draws a single section on the graphics object.
        /// </summary>
        private void DrawSection(Graphics g, RDSection section, bool selected)
        {
            float x = PaperX + MmToPixels(section.LocationMM.X, dpiX);
            float y = PaperY + MmToPixels(section.LocationMM.Y, dpiY);
            float width = MmToPixels(section.WidthMM, dpiX);
            float height = MmToPixels(section.HeightMM, dpiY);

            RectangleF rect = new RectangleF(x, y, width, height);

            if (section is RDElementSection)
            {
                g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
            }
            else if (section is RDTableSection)
            {
                g.FillRectangle(Brushes.LightGreen, rect);
            }

            if (selected)
            {
                using (Pen selectedSectionPen = new Pen(Color.CornflowerBlue, 2))
                {
                    g.DrawRectangle(selectedSectionPen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        /// <summary>
        /// Draws the resize handle for a section.
        /// </summary>
        private void DrawSectionResizeHandle(Graphics g, RDSection section)
        {
            float resizeHandleY = PaperY + MmToPixels(section.LocationMM.Y + section.HeightMM, dpiY);
            float resizeHandleHeight = MmToPixels(RESIZE_HANDLE_HEIGHT_MM, dpiY);
            float resizeHandleX = PaperX + MmToPixels(section.LocationMM.X, dpiX);
            float resizeHandleWidth = MmToPixels(section.WidthMM, dpiX);
            g.FillRectangle(Brushes.CornflowerBlue, resizeHandleX, resizeHandleY, resizeHandleWidth, resizeHandleHeight);
        }

        /// <summary>
        /// Draws all elements on the graphics object.
        /// </summary>
        private void DrawElements(Graphics g)
        {
            
            foreach (var element in elements)
            {
                bool selected = element == selectedElement;
                DrawElement(g, element, selected);
            }
        }

        /// <summary>
        /// Draws a single element on the graphics object.
        /// </summary>
        private void DrawElement(Graphics g, RDElement element, bool selected)
        {
            // Draw element
            if (element is RDTextElement textElement)
            {
                DrawTextElement(g, textElement, selected);
            }

            // Draw marks at rulers
            if (selected)
            {
                // Left top corner
                PointF ltPoint = new PointF(PaperX - RULER_WIDTH_MM, PaperY + MmToPixels(element.LocationMM.Y + element.ParentSection.LocationMM.Y, dpiY));
                PointF ltPoint1 = new PointF(ltPoint.X - RULER_ARROW_SIZE, ltPoint.Y - RULER_ARROW_SIZE);
                PointF ltPoint2 = new PointF(ltPoint.X - RULER_ARROW_SIZE, ltPoint.Y + RULER_ARROW_SIZE);
                g.FillPolygon(Brushes.Red, new PointF[] { ltPoint, ltPoint1, ltPoint2 });

                // Left bottom corner
                PointF lbPoint = new PointF(PaperX - RULER_WIDTH_MM, PaperY + MmToPixels(element.LocationMM.Y + element.ParentSection.LocationMM.Y + element.HeightMM, dpiY));
                PointF lbPoint1 = new PointF(lbPoint.X - RULER_ARROW_SIZE, lbPoint.Y - RULER_ARROW_SIZE);
                PointF lbPoint2 = new PointF(lbPoint.X - RULER_ARROW_SIZE, lbPoint.Y + RULER_ARROW_SIZE);
                g.FillPolygon(Brushes.Red, new PointF[] { lbPoint, lbPoint1, lbPoint2 });

                // Top left corner
                PointF tlPoint = new PointF(PaperX + MmToPixels(element.LocationMM.X + element.ParentSection.LocationMM.X, dpiX), PaperY - RULER_WIDTH_MM);
                PointF tlPoint1 = new PointF(tlPoint.X - RULER_ARROW_SIZE, tlPoint.Y - RULER_ARROW_SIZE);
                PointF tlPoint2 = new PointF(tlPoint.X + RULER_ARROW_SIZE, tlPoint.Y - RULER_ARROW_SIZE);
                g.FillPolygon(Brushes.Red, new PointF[] { tlPoint, tlPoint1, tlPoint2 });

                // Top right corner
                PointF trPoint = new PointF(PaperX + MmToPixels(element.LocationMM.X + element.WidthMM + element.ParentSection.LocationMM.X, dpiX), PaperY - RULER_WIDTH_MM);
                PointF trPoint1 = new PointF(trPoint.X - RULER_ARROW_SIZE, trPoint.Y - RULER_ARROW_SIZE);
                PointF trPoint2 = new PointF(trPoint.X + RULER_ARROW_SIZE, trPoint.Y - RULER_ARROW_SIZE);
                g.FillPolygon(Brushes.Red, new PointF[] { trPoint, trPoint1, trPoint2 });

             

            }
        }

        /// <summary>
        /// Draws a text element on the graphics object.
        /// </summary>
        private void DrawTextElement(Graphics g, RDTextElement element, bool selected)
        {
            float x = PaperX + MmToPixels(element.ParentSection.LocationMM.X + element.LocationMM.X, dpiX);
            float y = PaperY + MmToPixels(element.ParentSection.LocationMM.Y + element.LocationMM.Y, dpiY);
            float width = MmToPixels(element.WidthMM, dpiX);
            float height = MmToPixels(element.HeightMM, dpiY);
            RectangleF rect = new RectangleF(x, y, width, height);

            using (Brush brush = new SolidBrush(element.TextColor))
            {
                g.DrawString(element.Text, element.Font, brush, rect);
            }

            if (selected)
            {
                using (Pen selectedElementPen = new Pen(Color.CornflowerBlue, 2))
                {
                    g.DrawRectangle(selectedElementPen, rect.X, rect.Y, rect.Width, rect.Height);
                }
                DrawElementResizeHandle(g, rect);
            }
        }

        /// <summary>
        /// Draws the resize handle for an element.
        /// </summary>
        private void DrawElementResizeHandle(Graphics g, RectangleF elementRect)
        {
            float handleSizePixels = MmToPixels(RESIZE_HANDLE_SIZE_MM, dpiX);
            float x = elementRect.Right - handleSizePixels;
            float y = elementRect.Bottom - handleSizePixels;

            g.FillPolygon(Brushes.CornflowerBlue, new PointF[] { new PointF(x, elementRect.Bottom), new PointF(elementRect.Right, elementRect.Bottom), new PointF(elementRect.Right, y) });
        }

        #endregion

        #region Mouse Handling (continued)

        /// <summary>
        /// Handles the mouse down event.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            PointF paperPoint = ControlToPaper(e.X, e.Y);
            float mouseXMm = PixelsToMm(paperPoint.X, dpiX);
            float mouseYMm = PixelsToMm(paperPoint.Y, dpiY);

            if (IsOverElementResizeHandle(mouseXMm, mouseYMm))
            {
                StartResizingElement(mouseXMm, mouseYMm);
            }
            else if (IsOverSectionResizeHandle(mouseYMm))
            {
                StartResizingSection(mouseYMm);
            }
            else
            {
                SelectedElement = GetElementAtPoint(mouseXMm, mouseYMm);
                if (SelectedElement != null)
                {
                    StartMovingElement(mouseXMm, mouseYMm);
                }
                else
                {
                    SelectedSection = GetSectionAtPoint(mouseXMm, mouseYMm);
                }
            }
        }

        private PointF ControlToPaper(int x, int y)
        {
            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);

            float mouseX = (x / zoomFactor) - PaperX;
            float mouseY = (y / zoomFactor) - PaperY;

            return new PointF(mouseX, mouseY);
        }

        /// <summary>
        /// Handles the mouse move event.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);

            float mouseXMm = PixelsToMm((e.X / zoomFactor) - PaperX, dpiX);
            float mouseYMm = PixelsToMm((e.Y / zoomFactor) - PaperY, dpiY);

            if (isResizingElement)
            {
                ResizeElement(mouseXMm, mouseYMm);
            }
            else if (isResizingSection)
            {
                ResizeSection(mouseYMm);
            }
            else if (isMovingElement)
            {
                MoveElement(mouseXMm, mouseYMm);
            }
            else
            {
                UpdateCursor(mouseXMm, mouseYMm);
            }
        }

        /// <summary>
        /// Handles the mouse up event.
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            isResizingElement = false;
            isResizingSection = false;
            isMovingElement = false;
            Cursor = Cursors.Default;
        }

        #endregion

        #region Element and Section Manipulation

        /// <summary>
        /// Starts the element resizing operation.
        /// </summary>
        private void StartResizingElement(float xMm, float yMm)
        {
            if (SelectedElement != null && SelectedElement.Resizable)
            {
                isResizingElement = true;
                resizeStartOffset = new PointF(
                    xMm - (SelectedElement.ParentSection.LocationMM.X + SelectedElement.LocationMM.X + SelectedElement.WidthMM),
                    yMm - (SelectedElement.ParentSection.LocationMM.Y + SelectedElement.LocationMM.Y + SelectedElement.HeightMM)
                );
                Cursor = Cursors.SizeNWSE;
            }
        }

        /// <summary>
        /// Starts the section resizing operation.
        /// </summary>
        private void StartResizingSection(float yMm)
        {
            RDSection sectionToResize = GetResizableSection(yMm);
            if (sectionToResize != null)
            {
                SelectedSection = sectionToResize;
                isResizingSection = true;
                resizeStartY = yMm;
                Cursor = Cursors.SizeNS;
            }
        }

        /// <summary>
        /// Starts the element moving operation.
        /// </summary>
        private void StartMovingElement(float xMm, float yMm)
        {
            if (SelectedElement != null)
            {
                isMovingElement = true;
                moveStartOffset = new PointF(
                    xMm - (SelectedElement.ParentSection.LocationMM.X + SelectedElement.LocationMM.X),
                    yMm - (SelectedElement.ParentSection.LocationMM.Y + SelectedElement.LocationMM.Y)
                );
                Cursor = Cursors.SizeAll;
            }
        }

        /// <summary>
        /// Resizes the selected element.
        /// </summary>
        private void ResizeElement(float xMm, float yMm)
        {
            if (SelectedElement != null && SelectedElement.ParentSection is RDElementSection parentSection)
            {
                float newWidth = xMm - SelectedElement.ParentSection.LocationMM.X - SelectedElement.LocationMM.X - resizeStartOffset.X;
                float newHeight = yMm - SelectedElement.ParentSection.LocationMM.Y - SelectedElement.LocationMM.Y - resizeStartOffset.Y;

                // Constrain the element within the parent section
                newWidth = Math.Max(MIN_TEXT_ELEMENT_WIDTH, Math.Min(newWidth, parentSection.WidthMM - SelectedElement.LocationMM.X));
                newHeight = Math.Max(MIN_TEXT_ELEMENT_HEIGHT, Math.Min(newHeight, parentSection.HeightMM - SelectedElement.LocationMM.Y));

                // Auto -scale the font size if enabled
                if (SelectedElement is RDTextElement textElement && textElement.AutoScaleFont)
                {
                    float fontSize = textElement.Font.Size * newHeight / textElement.HeightMM;
                    textElement.Font = new Font(textElement.Font.FontFamily, fontSize, textElement.Font.Style);
                }

                SelectedElement.WidthMM = newWidth;
                SelectedElement.HeightMM = newHeight;

                Invalidate();
            }
        }

        /// <summary>
        /// Resizes the selected section.
        /// </summary>
        private void ResizeSection(float mouseYMm)
        {
            if (SelectedSection != null && SelectedSection.Resizable)
            {
                float deltaYMm = mouseYMm - resizeStartY;
                float newHeight = SelectedSection.HeightMM + deltaYMm;

                // Calculate the minimum height required to contain all elements
                float minHeight = GetMinimumSectionHeight(SelectedSection);

                // Ensure the new height is not smaller than the minimum required height
                newHeight = Math.Max(newHeight, minHeight);

                // Apply the new height
                SelectedSection.HeightMM = newHeight;
                resizeStartY = mouseYMm;
                UpdateSectionPositions();
                Invalidate();
            }
        }

        /// <summary>
        /// Moves the selected element.
        /// </summary>
        private void MoveElement(float xMm, float yMm)
        {
            if (SelectedElement != null && SelectedElement.ParentSection is RDElementSection parentSection)
            {
                float newX = xMm - moveStartOffset.X - parentSection.LocationMM.X;
                float newY = yMm - moveStartOffset.Y - parentSection.LocationMM.Y;

                // Constrain the element within the parent section
                newX = Math.Max(0, Math.Min(newX, parentSection.WidthMM - SelectedElement.WidthMM));
                newY = Math.Max(0, Math.Min(newY, parentSection.HeightMM - SelectedElement.HeightMM));

                SelectedElement.LocationMM = new PointF(newX, newY);
                Invalidate();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if the mouse is over an element's resize handle.
        /// </summary>
        private bool IsOverElementResizeHandle(float xMm, float yMm)
        {
            if (SelectedElement == null || !SelectedElement.Resizable) return false;

            float handleX = SelectedElement.ParentSection.LocationMM.X + SelectedElement.LocationMM.X + SelectedElement.WidthMM - RESIZE_HANDLE_SIZE_MM;
            float handleY = SelectedElement.ParentSection.LocationMM.Y + SelectedElement.LocationMM.Y + SelectedElement.HeightMM - RESIZE_HANDLE_SIZE_MM;

            return xMm >= handleX && xMm <= handleX + RESIZE_HANDLE_SIZE_MM &&
                   yMm >= handleY && yMm <= handleY + RESIZE_HANDLE_SIZE_MM;
        }

        /// <summary>
        /// Checks if the mouse is over a section's resize handle.
        /// </summary>
        private bool IsOverSectionResizeHandle(float yMm)
        {
            return GetResizableSection(yMm) != null;
        }

        /// <summary>
        /// Gets the resizable section at the given Y coordinate.
        /// </summary>
        private RDSection GetResizableSection(float yMm)
        {
            return sections.FirstOrDefault(section =>
                section.Resizable &&
                Math.Abs(yMm - (section.LocationMM.Y + section.HeightMM)) <= RESIZE_HANDLE_HEIGHT_MM);
        }

        /// <summary>
        /// Updates the cursor based on the mouse position.
        /// </summary>
        private void UpdateCursor(float xMm, float yMm)
        {
            if (IsOverElementResizeHandle(xMm, yMm))
            {
                Cursor = Cursors.SizeNWSE;
            }
            else if (IsOverSectionResizeHandle(yMm))
            {
                Cursor = Cursors.SizeNS;
            }
            else if (GetElementAtPoint(xMm, yMm) != null)
            {
                Cursor = Cursors.SizeAll;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Gets the section at the given point.
        /// </summary>
        private RDSection GetSectionAtPoint(float xMm, float yMm)
        {
            return sections.FirstOrDefault(section =>
                xMm >= section.LocationMM.X &&
                xMm <= section.LocationMM.X + section.WidthMM &&
                yMm >= section.LocationMM.Y &&
                yMm <= section.LocationMM.Y + section.HeightMM);
        }

        /// <summary>
        /// Gets the element at the given point.
        /// </summary>
        private RDElement GetElementAtPoint(float xMm, float yMm)
        {
            return elements.FirstOrDefault(element =>
                xMm >= element.ParentSection.LocationMM.X + element.LocationMM.X &&
                xMm <= element.ParentSection.LocationMM.X + element.LocationMM.X + element.WidthMM &&
                yMm >= element.ParentSection.LocationMM.Y + element.LocationMM.Y &&
                yMm <= element.ParentSection.LocationMM.Y + element.LocationMM.Y + element.HeightMM);
        }

        /// <summary>
        /// Calculates the minimum height required for a section.
        /// </summary>
        private float GetMinimumSectionHeight(RDSection section)
        {
            if (section is RDElementSection elementSection)
            {
                // Find the element with the lowest bottom edge
                float maxBottomEdge = elements
                    .Where(e => e.ParentSection == elementSection)
                    .Select(e => e.LocationMM.Y + e.HeightMM)
                    .DefaultIfEmpty(0)
                    .Max();

                return maxBottomEdge;
            }
            else
            {
                // For non-element sections (like table sections), return a minimum height (e.g., 20mm)
                return 20;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Converts millimeters to pixels.
        /// </summary>
        public static float MmToPixels(float mm, float dpi)
        {
            return mm * dpi / MM_PER_INCH;
        }

        /// <summary>
        /// Converts pixels to millimeters.
        /// </summary>
        public static float PixelsToMm(float pixels, float dpi)
        {
            return pixels * MM_PER_INCH / dpi;
        }

        /// <summary>
        /// Updates the paper size based on the selected PaperSize enum.
        /// </summary>
        private void UpdatePaperSize()
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
                case PaperSizes.A4Landscape:
                    paperWidthMm = A4_HEIGHT_MM;
                    paperHeightMm = A4_WIDTH_MM;
                    break;
                case PaperSizes.A5Landscape:
                    paperWidthMm = A5_HEIGHT_MM;
                    paperHeightMm = A5_WIDTH_MM;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paperSize));
            }

            UpdateSectionPositions();
        }

        /// <summary>
        /// Updates the positions of all sections.
        /// </summary>
        private void UpdateSectionPositions()
        {
            float yOffsetMm = paperMargin.Top;
            foreach (var section in sections)
            {
                section.LocationMM = new PointF(paperMargin.Left, yOffsetMm);
                section.WidthMM = paperWidthMm - paperMargin.Left - paperMargin.Right;
                yOffsetMm = section.LocationMM.Y + section.HeightMM + section.MarginMM.Bottom;
            }
        }

        #endregion
    }
}