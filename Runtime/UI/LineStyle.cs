using MIDIFrogs.DialogSystem.Core;
using TMPro;

namespace MIDIFrogs.DialogSystem.UI
{
    internal static class LineStyle
    {
        /// <summary>Sets text and style; the colour changes only when the line overrides it.</summary>
        public static void Apply(TMP_Text target, StyledText value, string resolvedText)
        {
            target.text = resolvedText ?? string.Empty;
            target.fontStyle = (FontStyles)value.Style;
            if (value.HasColor)
            {
                target.color = value.Color;
            }
        }
    }
}
