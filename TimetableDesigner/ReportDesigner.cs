using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TimetableDesignerApp
{
    public enum PaperSize
    {
        A4,
        A4Landscape,
        A5,
        A5Landscape
    }
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

        #endregion

        #region Fields

        private List<ReportSection> sections;
        private ToolStrip toolStrip;
        private PaperSize paperSize;
        private float paperWidthMm;
        private float paperHeightMm;
        private float dpiX;
        private float dpiY;
        private float zoomFactor = 1.0f;
        private ReportSection resizingSection;
        private float resizeStartY;
        private bool isOverResizeHandle;
        private Padding paperMargin = new Padding(10);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the paper size of the report.
        /// </summary>
        public PaperSize PaperSize
        {
            get { return paperSize; }
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
            get { return zoomFactor; }
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
            get { return paperMargin; }
            set
            {
                paperMargin = value;
                UpdateSectionPositions();
                Invalidate();
            }
        }

        #endregion

        #region Constructor

        public ReportDesigner()
        {
            sections = new List<ReportSection>();
            InitializeComponent();
            PaperSize = PaperSize.A4;
            UpdateDpi();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            this.BorderStyle = BorderStyle.FixedSingle;

            toolStrip = new ToolStrip();
            toolStrip.Items.Add("Add General Section", null, AddGeneralSection_Click);
            toolStrip.Items.Add("Add Table Section", null, AddTableSection_Click);
            toolStrip.Items.Add("Zoom In", null, ZoomIn_Click);
            toolStrip.Items.Add("Zoom Out", null, ZoomOut_Click);
            this.Controls.Add(toolStrip);
        }

        private void UpdateDpi()
        {
            using (Graphics g = this.CreateGraphics())
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
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (i == 0)
                {
                    // Only the first section respects the top margin
                    section.LocationMM = new PointF(paperMargin.Left, yOffsetMm);
                }
                else
                {
                    // Subsequent sections have no top margin
                    section.LocationMM = new PointF(paperMargin.Left, yOffsetMm);
                }
                section.WidthMM = paperWidthMm - paperMargin.Left - paperMargin.Right;
                yOffsetMm += section.HeightMM;
                if (section.Resizable) yOffsetMm += RESIZE_HANDLE_HEIGHT_MM;
            }
            paperHeightMm = Math.Max(A4_HEIGHT_MM, yOffsetMm + paperMargin.Bottom);
        }

        /// <summary>
        /// Adds a new section to the report.
        /// </summary>
        /// <param name="section">The section to add.</param>
        private void AddSection(ReportSection section)
        {
            sections.Add(section);
            UpdateSectionPositions();
            Invalidate();
        }

        #endregion

        #region Drawing

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(this.BackColor);
            e.Graphics.ScaleTransform(zoomFactor, zoomFactor);

            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float x = (this.ClientSize.Width / zoomFactor - paperWidthPixels) / 2;
            x = Math.Max(0, x);

            DrawPaper(e.Graphics, x);
            DrawGrid(e.Graphics, x);
            DrawRulers(e.Graphics, x);
            DrawSections(e.Graphics, x);
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

        private void DrawRulers(Graphics g, float x)
        {
            float paperWidthPixels = MmToPixels(paperWidthMm, dpiX);
            float paperHeightPixels = MmToPixels(paperHeightMm, dpiY);
            float topMarginPixels = MmToPixels(PAPER_TOP_MARGIN_MM, dpiY);
            float rulerWidthPixels = MmToPixels(RULER_WIDTH_MM, dpiX);

            using (Font rulerFont = new Font(Font.FontFamily, RULER_FONT_SIZE_PT, FontStyle.Regular))
            {
                // Vertical ruler
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

                // Horizontal ruler
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
                section.Draw(g, x, topMarginPixels, dpiX, dpiY);
                if (section.Resizable)
                {
                    float resizeHandleY = topMarginPixels + MmToPixels(section.LocationMM.Y + section.HeightMM, dpiY);
                    float resizeHandleHeight = MmToPixels(RESIZE_HANDLE_HEIGHT_MM, dpiY);
                    g.FillRectangle(Brushes.Gray, x + MmToPixels(section.LocationMM.X, dpiX), resizeHandleY, MmToPixels(section.WidthMM, dpiX), resizeHandleHeight);
                }
            }
        }

        #endregion

        #region Event Handlers

        private void ZoomIn_Click(object sender, EventArgs e)
        {
            ZoomFactor *= 1.2f;
        }

        private void ZoomOut_Click(object sender, EventArgs e)
        {
            ZoomFactor /= 1.2f;
        }

        private void AddGeneralSection_Click(object sender, EventArgs e)
        {
            AddSection(new GeneralSection());
        }

        private void AddTableSection_Click(object sender, EventArgs e)
        {
            AddSection(new TableSection());
        }

        #endregion

        #region Mouse Handling

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            float mouseYMm = PixelsToMm(e.Y / zoomFactor - MmToPixels(PAPER_TOP_MARGIN_MM, dpiY), dpiY);

            if (resizingSection != null)
            {
                ResizeSection(mouseYMm);
            }
            else
            {
                UpdateCursorForResizeHandle(mouseYMm);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            float mouseYMm = PixelsToMm(e.Y / zoomFactor - MmToPixels(PAPER_TOP_MARGIN_MM, dpiY), dpiY);
            resizingSection = GetResizingSection(mouseYMm);
            if (resizingSection != null)
            {
                resizeStartY = mouseYMm;
                this.Cursor = Cursors.SizeNS;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (resizingSection != null)
            {
                resizingSection = null;
                float mouseYMm = PixelsToMm(e.Y / zoomFactor - MmToPixels(PAPER_TOP_MARGIN_MM, dpiY), dpiY);
                UpdateCursorForResizeHandle(mouseYMm);
            }
        }

        /// <summary>
        /// Resizes the current section being resized.
        /// </summary>
        private void ResizeSection(float mouseYMm)
        {
            float deltaYMm = mouseYMm - resizeStartY;
            resizingSection.HeightMM = Math.Max(20, resizingSection.HeightMM + deltaYMm);
            resizeStartY = mouseYMm;
            UpdateSectionPositions();
            Invalidate();
            this.Cursor = Cursors.SizeNS;
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
                this.Cursor = isOverResizeHandle ? Cursors.SizeNS : Cursors.Default;
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
        private ReportSection GetResizingSection(float mouseYMm)
        {
            foreach (var section in sections)
            {
                if (section.Resizable)
                {
                    float resizeHandleYMm = section.LocationMM.Y + section.HeightMM;
                    if (Math.Abs(mouseYMm - resizeHandleYMm) <= RESIZE_HANDLE_HEIGHT_MM)
                    {
                        return section;
                    }
                }
            }
            return null;
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

    public abstract class ReportSection
    {
        public PointF LocationMM { get; set; }
        public float WidthMM { get; set; }
        public float HeightMM { get; set; }
        public virtual bool Resizable { get; set; }

        public abstract void Draw(Graphics g, float offsetX, float offsetY, float dpiX, float dpiY);
    }

    public class GeneralSection : ReportSection
    {
        public override bool Resizable { get; set; } = true;
        public GeneralSection()
        {
            HeightMM = 100; // Default height
        }

        public override void Draw(Graphics g, float offsetX, float offsetY, float dpiX, float dpiY)
        {
            float x = offsetX + ReportDesigner.MmToPixels(LocationMM.X, dpiX);
            float y = offsetY + ReportDesigner.MmToPixels(LocationMM.Y, dpiY);
            float width = ReportDesigner.MmToPixels(WidthMM, dpiX);
            float height = ReportDesigner.MmToPixels(HeightMM, dpiY);

            RectangleF rect = new RectangleF(x, y, width, height);
            g.FillRectangle(Brushes.LightBlue, rect);
            g.DrawRectangle(Pens.Blue, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    public class TableSection : ReportSection
    {
        public override bool Resizable { get; set; } = false;
        public TableSection()
        {
            HeightMM = 20; // Default height in mm
        }

        public override void Draw(Graphics g, float offsetX, float offsetY, float dpiX, float dpiY)
        {
            float x = offsetX + ReportDesigner.MmToPixels(LocationMM.X, dpiX);
            float y = offsetY + ReportDesigner.MmToPixels(LocationMM.Y, dpiY);
            float width = ReportDesigner.MmToPixels(WidthMM, dpiX);
            float height = ReportDesigner.MmToPixels(HeightMM, dpiY);

            RectangleF rect = new RectangleF(x, y, width, height);
            g.FillRectangle(Brushes.LightGreen, rect);
            g.DrawRectangle(Pens.Green, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}