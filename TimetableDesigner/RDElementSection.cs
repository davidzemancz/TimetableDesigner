using System.Windows.Forms;

namespace TimetableDesignerApp
{
    /// <summary>
    /// Represents a section that can contain elements.
    /// </summary>
    public class RDElementSection : RDSection
    {
        public RDElementSection()
        {
            Resizable = true;
            MarginMM = new Padding(2);
            HeightMM = 100; // Default height
        }
    }
}