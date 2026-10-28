using UnityEngine;

namespace MIDIFrogs.DialogSystem.Core
{
    public class StyledText
    {
        public string Content = string.Empty;
        public string Key;
        public FontStyle Style = FontStyle.Normal;

        /// <summary>When false the view keeps its own text colour.</summary>
        public bool HasColor;
        public Color Color = Color.white;
    }
}
