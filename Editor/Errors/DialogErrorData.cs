using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Errors
{
    public class DialogErrorData
    {
        public Color Color { get; set; }

        public DialogErrorData()
        {
            GenerateRandomColor();
        }

        private void GenerateRandomColor()
        {
            Color = new Color32(
                (byte) Random.Range(65, 256),
                (byte) Random.Range(50, 144),
                (byte) Random.Range(50, 144),
                255
            );
        }
    }
}