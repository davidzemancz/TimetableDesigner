using System.Drawing;

namespace TimetableDesignerApp
{
    /// <summary>
    /// Base class for all elements in the report.
    /// </summary>
    public abstract class RDElement
    {
        public PointF LocationMM { get; set; }
        public float WidthMM { get; set; }
        public float HeightMM { get; set; }
        public RDElementSection ParentSection { get; set; }
        public bool Resizable { get; set; } = true;
    }
}