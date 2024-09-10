using System.Drawing;
using System.Windows.Forms;

namespace TimetableDesignerApp
{
    /// <summary>
    /// Represents a section in the report.
    /// </summary>
    public class RDSection
    {
        public PointF LocationMM { get; set; }
        public float WidthMM { get; set; }
        public float HeightMM { get; set; }
        public bool Resizable { get; set; }
        public Padding MarginMM { get; set; } = new Padding(0);
    }
}