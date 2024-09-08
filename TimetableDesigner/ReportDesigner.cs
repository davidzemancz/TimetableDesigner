using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TimetableDesignerApp
{
    /// <summary>
    /// Enumeration of supported paper sizes.
    /// </summary>
    public enum PaperSize
    {
        A4,
        A4Landscape,
        A5,
        A5Landscape
    }

    /// <summary>
    /// Main class for designing reports. Inherits from Panel for UI integration.
    /// </summary>
    public class ReportDesigner : Panel
    {
        #region Constants

        // Conversion and measurement constants
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

        #endregion

        #region Fields

        private List<RDSection> sections = new List<RDSection>();
        private RDSection selectedSection;
        private RDElement selectedElement;
        private ToolStrip toolStrip;
        private PaperSize paperSize;
        private float paperWidthMm;
        private float paperHeightMm;
        private float dpiX;
        private float dpiY;
        private float zoomFactor = 1.0f;
        private bool isResizingSection;
        private float resizeStartY;
        private bool isOverResizeHandle;
        private Padding paperMargin = new Padding(10);
        private List<RDElement> elements = new List<RDElement>();
        private bool showGrid;
        private RDElement movingElement;
        private PointF moveStartOffset;
        private bool isMovingElement;

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
                Invalidate(); // Redraw to show the selection
            }
        }


        /// <summary>
        /// Gets or sets the paper size of the report.
        /// </summary>
        public PaperSize PaperSize
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

        public bool ShowGrid
        {
            get 
            { 
                return showGrid; 
            }
            set
            {
                showGrid = value;
                Invalidate();
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ReportDesigner class.
        /// </summary>
        public ReportDesigner()
        {
            InitializeComponent();
            PaperSize = PaperSize.A4;
            UpdateDpi();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the ReportDesigner components.
        /// </summary>
        private void InitializeComponent()
        {
            BorderStyle = BorderStyle.FixedSingle;

            toolStrip = new ToolStrip();
            toolStrip.Items.Add("Add General Section", null, AddGeneralSection_Click);
            toolStrip.Items.Add("Add Table Section", null, AddTableSection_Click);
            toolStrip.Items.Add("Add Element", null, AddElement_Click);
            toolStrip.Items.Add("Zoom In", null, ZoomIn_Click);
            toolStrip.Items.Add("Zoom Out", null, ZoomOut_Click);
            toolStrip.Items.Add("Show grid", null, ShowGrid_Click);
            Controls.Add(toolStrip);
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

        #region Paper and Section Management

        /// <summary>
        /// Updates the paper size based on the selected PaperSize enum.
        /// </summary>
        private void UpdatePaperSize()
        {
            switch (paperSize)
            {
                case PaperSize.A4:
                    paperWidthMm = A4_WIDTH_MM;
                    paperHeightMm = A4_HEIGHT_MM;
                    break;
                case PaperSize.A4Landscape:
                    paperWidthMm = A4_HEIGHT_MM;
                    paperHeightMm = A4_WIDTH_MM;
                    break;
                case PaperSize.A5:
                    paperWidthMm = A5_WIDTH_MM;
                    paperHeightMm = A5_HEIGHT_MM;
                    break;
                case PaperSize.A5Landscape:
                    paperWidthMm = A5_HEIGHT_MM;
                    paperHeightMm = A5_WIDTH_MM;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paperSize));
            }

            UpdateSectionPositions();
        }

        /// <summary>
        /// Updates the positions of all sections and recalculates the paper height.
        /// </summary>
        private void UpdateSectionPositions()
        {
            float yOffsetMm = paperMargin.Top;
            foreach (var section in sections)
            {
                section.LocationMM = new PointF(paperMargin.Left + section.MarginMM.Left, yOffsetMm + section.MarginMM.Top);
                section.WidthMM = paperWidthMm - paperMargin.Left - paperMargin.Right - section.MarginMM.Left - section.MarginMM.Right;
                yOffsetMm = section.LocationMM.Y + section.HeightMM + section.MarginMM.Bottom;
                if (section.Resizable) yOffsetMm += RESIZE_HANDLE_HEIGHT_MM;
            }
            paperHeightMm = Math.Max(A4_HEIGHT_MM, yOffsetMm + paperMargin.Bottom);
        }

        /// <summary>
        /// Adds a new section to the report.
        /// </summary>
        /// <param name="section">The section to add.</param>
        private void AddSection(RDSection section)
        {
            sections.Add(section);
            UpdateSectionPositions();
            SelectedSection = section;
        }

        #endregion

        #region Elements

        private void AddElement(RDElement element)
        {
            elements.Add(element);
            Invalidate();
            SelectedElement = element;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Handles the paint event for the ReportDesigner.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(BackColor);
            e.Graphics.ScaleTransform(zoomFactor, zoomFactor);

            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float x = Math.Max(0, (ClientSize.Width / zoomFactor - paperWidthPixels) / 2);

            DrawPaper(e.Graphics, x);
            DrawGrid(e.Graphics, x);
            DrawRulers(e.Graphics, x);
            DrawSections(e.Graphics, x);
            DrawElements(e.Graphics, x);
        }

        /// <summary>
        /// Draws the paper on the graphics object.
        /// </summary>
        private void DrawPaper(Graphics g, float x)
        {
            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float paperHeightPixels = MmToPixels(paperHeightMm, dpiY);
            float topMarginPixels = MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);

            g.FillRectangle(Brushes.White, x, topMarginPixels, paperWidthPixels, paperHeightPixels);
            g.DrawRectangle(Pens.Black, x, topMarginPixels, paperWidthPixels, paperHeightPixels);
        }

        /// <summary>
        /// Draws the grid on the graphics object.
        /// </summary>
        private void DrawGrid(Graphics g, float x)
        {
            if(!showGrid) return;

            float gridSizePixels = MmToPixels(GRID_SIZE_MM, dpiX);
            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float paperHeightPixels = MmToPixels(paperHeightMm, dpiY);
            float topMarginPixels = MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);

            for (float gridX = x; gridX <= x + paperWidthPixels; gridX += gridSizePixels)
            {
                g.DrawLine(Pens.LightGray, gridX, topMarginPixels, gridX, topMarginPixels + paperHeightPixels);
            }
            for (float gridY = topMarginPixels; gridY <= topMarginPixels + paperHeightPixels; gridY += gridSizePixels)
            {
                g.DrawLine(Pens.LightGray, x, gridY, x + paperWidthPixels, gridY);
            }
        }

        /// <summary>
        /// Draws the rulers on the graphics object.
        /// </summary>
        private void DrawRulers(Graphics g, float x)
        {
            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float paperHeightPixels = MmToPixels(paperHeightMm, dpiY);
            float topMarginPixels = MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);
            float rulerWidthPixels = MmToPixels(RULER_WIDTH_MM, dpiX);

            using (Font rulerFont = new Font(Font.FontFamily, RULER_FONT_SIZE_PT, FontStyle.Regular))
            {
                // Draw vertical ruler
                for (int i = 0; i <= paperHeightMm; i += 10)
                {
                    float tickY = topMarginPixels + MmToPixels(i, dpiY);
                    float tickWidth = MmToPixels(RULER_TICK_SIZE_MM, dpiX);
                    g.DrawLine(Pens.Black, x - tickWidth, tickY, x, tickY);

                    if (i % 50 == 0)
                    {
                        string label = i.ToString();
                        SizeF labelSize = g.MeasureString(label, rulerFont);
                        g.DrawString(label, rulerFont, Brushes.Black, x - labelSize.Width - tickWidth, tickY - labelSize.Height / 2);
                    }
                }

                // Draw horizontal ruler
                for (int i = 0; i <= paperWidthMm; i += 10)
                {
                    float tickX = x + MmToPixels(i, dpiX);
                    float tickHeight = MmToPixels(RULER_TICK_SIZE_MM, dpiY);
                    g.DrawLine(Pens.Black, tickX, topMarginPixels - tickHeight, tickX, topMarginPixels);

                    if (i % 50 == 0)
                    {
                        string label = i.ToString();
                        SizeF labelSize = g.MeasureString(label, rulerFont);
                        g.DrawString(label, rulerFont, Brushes.Black, tickX - labelSize.Width / 2, topMarginPixels - tickHeight - labelSize.Height);
                    }
                }
            }
        }

        /// <summary>
        /// Draws all sections and their resize handles on the graphics object.
        /// </summary>
        private void DrawSections(Graphics g, float x)
        {
            float topMarginPixels = MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);
            foreach (var section in sections)
            {
                bool selected = section == selectedSection;
                section.Draw(g, x, topMarginPixels, dpiX, dpiY, selected);
                if (section.Resizable)
                {
                    DrawSectionResizeHandle(g, x, topMarginPixels, section);
                }
            }
        }

        /// <summary>
        /// Draws the resize handle for a section.
        /// </summary>
        private void DrawSectionResizeHandle(Graphics g, float x, float topMarginPixels, RDSection section)
        {
            //float resizeHandleY = topMarginPixels + MmToPixels(section.LocationMM.Y + section.HeightMM, dpiY);
            //float resizeHandleHeight = MmToPixels(RESIZE_HANDLE_HEIGHT_MM, dpiY);
            //float resizeHandleX = x + MmToPixels(section.LocationMM.X, dpiX);
            //float resizeHandleWidth = MmToPixels(section.WidthMM, dpiX);
            //g.FillRectangle(Brushes.CornflowerBlue, resizeHandleX, resizeHandleY, resizeHandleWidth, resizeHandleHeight);

            // pass...
        }

        /// <summary>
        /// Draws all elements
        /// </summary>
        private void DrawElements(Graphics g, float x)
        {
            float topMarginPixels = MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);
            foreach (var element in elements)
            {
                bool selected = element == selectedElement;
                element.Draw(g, x, topMarginPixels, dpiX, dpiY, selected);
            }
        }

        #endregion

        #region Event Handlers

        private void ShowGrid_Click(object sender, EventArgs e)
        {
            ShowGrid = !ShowGrid;
        }

        /// <summary>
        /// Handles the zoom in button click event.
        /// </summary>
        private void ZoomIn_Click(object sender, EventArgs e)
        {
            ZoomFactor *= 1.2f;
        }

        /// <summary>
        /// Handles the zoom out button click event.
        /// </summary>
        private void ZoomOut_Click(object sender, EventArgs e)
        {
            ZoomFactor /= 1.2f;
        }

        /// <summary>
        /// Handles the add general section button click event.
        /// </summary>
        private void AddGeneralSection_Click(object sender, EventArgs e)
        {
            AddSection(new RDElementSection());
        }

        /// <summary>
        /// Handles the add table section button click event.
        /// </summary>
        private void AddTableSection_Click(object sender, EventArgs e)
        {
            AddSection(new RDTableSection());
        }

        private void AddElement_Click(object sender, EventArgs e)
        {
            if(SelectedSection is RDElementSection elementSection)
            {
                AddElement(new RDTextElement(elementSection, "Hello World", new Font("Arial", 12), Color.Black));
            }
        }

        #endregion

        #region Mouse Handling

        /// <summary>
        /// Handles the mouse move event.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float paperX = Math.Max(0, (ClientSize.Width / zoomFactor - paperWidthPixels) / 2);
            float paperTopMarginPixels = MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);

            float mouseXMm = PixelsToMm((e.X / zoomFactor) - paperX, dpiX);
            float mouseYMm = PixelsToMm((e.Y / zoomFactor) - paperTopMarginPixels, dpiY);

            if (isResizingSection && SelectedSection != null)
            {
                ResizeSection(mouseYMm);
            }
            else if (isMovingElement && SelectedElement != null)
            {
                MoveElement(mouseXMm, mouseYMm);
            }
            else
            {
                UpdateCursorForResizeHandle(mouseYMm);
            }
        }

        /// <summary>
        /// Handles the mouse down event.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Calculate paper position
            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float paperX = Math.Max(0, (ClientSize.Width / zoomFactor - paperWidthPixels) / 2);
            float paperTopMarginPixels = MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);

            // Adjust mouse coordinates relative to paper
            float mouseXMm = PixelsToMm((e.X / zoomFactor) - paperX, dpiX);
            float mouseYMm = PixelsToMm((e.Y / zoomFactor) - paperTopMarginPixels, dpiY);

            // Check for resizing section
            RDSection sectionToResize = GetResizingSection(mouseYMm);
            if (sectionToResize != null)
            {
                isResizingSection = true;
                SelectedSection = sectionToResize;
                resizeStartY = mouseYMm;
                Cursor = Cursors.SizeNS;
                return;
            }

            // Check for moving element
            SelectedElement = GetElementAtPoint(mouseXMm, mouseYMm);
            if (SelectedElement != null && SelectedElement.ParentSection is RDElementSection parentSection)
            {
                isMovingElement = true;
                moveStartOffset = new PointF(
                      mouseXMm - (SelectedElement.LocationMM.X + parentSection.LocationMM.X),
                      mouseYMm - (SelectedElement.LocationMM.Y + parentSection.LocationMM.Y)
                  );
                Cursor = Cursors.SizeAll;
                return;
            }

            // If not resizing or moving, just select the section
            SelectedSection = GetSectionAtPoint(mouseXMm, mouseYMm);
            isMovingElement = false;
        }

        /// <summary>
        /// Handles the mouse up event.
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (isResizingSection)
            {
                isResizingSection = false;
                Cursor = Cursors.Default;
            }

            if (isMovingElement)
            {
                isMovingElement = false;
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Gets the section at the specified point.
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
        /// Gets the element at the specified point.
        /// </summary>
        private RDElement GetElementAtPoint(float xMm, float yMm)
        {
            return elements.FirstOrDefault(element =>
                xMm >= element.PaperLocationMM.X &&
                xMm <= element.PaperLocationMM.X + element.WidthMM &&
                yMm >= element.PaperLocationMM.Y &&
                yMm <= element.PaperLocationMM.Y + element.HeightMM);
        }

        /// <summary>
        /// Resizes the current section being resized.
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
        /// Returns the minumum height of the section
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

        /// <summary>
        /// Updates the cursor based on whether the mouse is over a resize handle.
        /// </summary>
        private void UpdateCursorForResizeHandle(float mouseY)
        {
            bool wasOverResizeHandle = isOverResizeHandle;
            isOverResizeHandle = IsOverResizeHandle(mouseY);

            if (isOverResizeHandle != wasOverResizeHandle)
            {
                Cursor = isOverResizeHandle ? Cursors.SizeNS : Cursors.Default;
            }
        }

        /// <summary>
        /// Checks if the mouse is over any resize handle.
        /// </summary>
        private bool IsOverResizeHandle(float mouseY)
        {
            return GetResizingSection(mouseY) != null;
        }

        /// <summary>
        /// Gets the section that the mouse is over for resizing, or null if none.
        /// </summary>
        private RDSection GetResizingSection(float mouseYMm)
        {
            return sections.FirstOrDefault(section =>
                section.Resizable &&
                Math.Abs(mouseYMm - (section.LocationMM.Y + section.HeightMM)) <= RESIZE_HANDLE_HEIGHT_MM);
        }

        private void MoveElement(float mouseXMm, float mouseYMm)
        {
            if (SelectedElement?.ParentSection is RDElementSection parentSection)
            {
                float newX = mouseXMm - moveStartOffset.X - parentSection.LocationMM.X;
                float newY = mouseYMm - moveStartOffset.Y - parentSection.LocationMM.Y;

                // Constrain the element within the parent section
                newX = Math.Max(0, Math.Min(newX, parentSection.WidthMM - SelectedElement.WidthMM));
                newY = Math.Max(0, Math.Min(newY, parentSection.HeightMM - SelectedElement.HeightMM));

                SelectedElement.LocationMM = new PointF(newX, newY);
                Invalidate();
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Converts millimeters to pixels based on the given DPI.
        /// </summary>
        public static float MmToPixels(float mm, float dpi)
        {
            return mm * dpi / MM_PER_INCH;
        }

        /// <summary>
        /// Converts pixels to millimeters based on the given DPI.
        /// </summary>
        public static float PixelsToMm(float pixels, float dpi)
        {
            return pixels * MM_PER_INCH / dpi;
        }

        #endregion
    }

    /// <summary>
    /// Abstract base class for report sections.
    /// </summary>
    public abstract class RDSection
    {
        public PointF LocationMM { get; set; }
        public float WidthMM { get; set; }
        public float HeightMM { get; set; }
        public virtual bool Resizable { get; set; }
        public virtual Padding MarginMM { get; set; } = new Padding(0);
        protected static readonly Pen selectedSectionPen = new Pen(Color.CornflowerBlue, 2);

        /// <summary>
        /// Draws the section on the graphics object.
        /// </summary>
        public abstract void Draw(Graphics g, float offsetX, float offsetY, float dpiX, float dpiY, bool selected);
    }

    /// <summary>
    /// Represents a general section in the report.
    /// </summary>
    public class RDElementSection : RDSection
    {
        public override bool Resizable { get; set; } = true;
        public override Padding MarginMM { get; set; } = new Padding(2);

        public RDElementSection()
        {
            HeightMM = 100; // Default height
        }

        public override void Draw(Graphics g, float offsetX, float offsetY, float dpiX, float dpiY, bool selected)
        {
            float x = offsetX + ReportDesigner.MmToPixels(LocationMM.X, dpiX);
            float y = offsetY + ReportDesigner.MmToPixels(LocationMM.Y, dpiY);
            float width = ReportDesigner.MmToPixels(WidthMM, dpiX);
            float height = ReportDesigner.MmToPixels(HeightMM, dpiY);

            RectangleF rect = new RectangleF(x, y, width, height);
            g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);

            if (selected)
            {
                g.DrawRectangle(selectedSectionPen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
    }

    /// <summary>
    /// Represents a table section in the report.
    /// </summary>
    public class RDTableSection : RDSection
    {
        public override bool Resizable { get; set; } = false;
        public override Padding MarginMM { get; set; } = new Padding(0);

        public RDTableSection()
        {
            HeightMM = 20; // Default height in mm
        }

        public override void Draw(Graphics g, float offsetX, float offsetY, float dpiX, float dpiY, bool selected)
        {
            float x = offsetX + ReportDesigner.MmToPixels(LocationMM.X, dpiX);
            float y = offsetY + ReportDesigner.MmToPixels(LocationMM.Y, dpiY);
            float width = ReportDesigner.MmToPixels(WidthMM, dpiX);
            float height = ReportDesigner.MmToPixels(HeightMM, dpiY);

            RectangleF rect = new RectangleF(x, y, width, height);
            g.FillRectangle(Brushes.LightGreen, rect);

            if (selected)
            {
                g.DrawRectangle(selectedSectionPen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
    }

    public abstract class RDElement
    {
        /// <summary>
        /// Element location relative to the parent section in millimeters.
        /// </summary>
        public PointF LocationMM { get; set; }

        /// <summary>
        /// Gets the location of the element on the paper in millimeters.
        /// </summary>
        public PointF PaperLocationMM => new PointF(LocationMM.X + ParentSection.LocationMM.X, LocationMM.Y + ParentSection.LocationMM.Y);
        public float WidthMM { get; set; }
        public float HeightMM { get; set; }
        public RDElementSection ParentSection { get; set; }
        protected static readonly Pen selectedElementPen = new Pen(Color.CornflowerBlue, 2);

        public RDElement(RDElementSection parentSection)
        {
            ParentSection = parentSection;
        }

        public abstract void Draw(Graphics g, float offsetX, float offsetY, float dpiX, float dpiY, bool selected);
    }

    public class RDTextElement : RDElement
    {
        public string Text { get; set; }
        public Font Font { get; set; }
        public Color TextColor { get; set; }

        public RDTextElement(RDElementSection parentSection, string text, Font font, Color textColor) : base(parentSection)
        {
            Text = text;
            Font = font;
            TextColor = textColor;
            WidthMM = 50; // Default width
            HeightMM = 10; // Default height
            LocationMM = new PointF(0,0);
        }

        public override void Draw(Graphics g, float offsetX, float offsetY, float dpiX, float dpiY, bool selected)
        {
            float x = offsetX + ReportDesigner.MmToPixels(LocationMM.X, dpiX) + ReportDesigner.MmToPixels(ParentSection.LocationMM.X, dpiX);
            float y = offsetY + ReportDesigner.MmToPixels(LocationMM.Y, dpiY) + ReportDesigner.MmToPixels(ParentSection.LocationMM.Y, dpiX);
            float width = ReportDesigner.MmToPixels(WidthMM, dpiX);
            float height = ReportDesigner.MmToPixels(HeightMM, dpiY);
            RectangleF rect = new RectangleF(x, y, width, height);

            using (Brush brush = new SolidBrush(TextColor))
            {
                g.DrawString(Text, Font, brush, x, y);
            }

            if (selected)
            {
                g.DrawRectangle(selectedElementPen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
    }
}