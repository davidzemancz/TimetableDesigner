using System.Windows.Forms;

namespace TimetableDesignerApp
{
    /// <summary>
    /// Represents a table section in the report.
    /// </summary>
    public class RDTableSection : RDSection
    {
        public RDTableSection()
        {
            Resizable = false;
            MarginMM = new Padding(0);
            HeightMM = 20; // Default height
        }
    }
}