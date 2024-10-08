﻿using System.Drawing;

namespace TimetableDesignerApp
{
    /// <summary>
    /// Represents a text element in the report.
    /// </summary>
    public class RDTextElement : RDElement
    {
        public string Text { get; set; }
        public Font Font { get; set; }
        public Color TextColor { get; set; }
        public bool AutoScaleFont { get; set; }

        public RDTextElement(RDElementSection parentSection, string text, Font font, Color textColor)
        {
            ParentSection = parentSection;
            Text = text;
            Font = font;
            TextColor = textColor;
        }
    }
}