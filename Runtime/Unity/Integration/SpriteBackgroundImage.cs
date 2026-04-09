using MIDIFrogs.DialogSystem.Core;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Integration
{
    public class SpriteBackgroundImage : IBackgroundImage
    {
        private readonly Sprite sprite;

        public SpriteBackgroundImage(Sprite sprite) => this.sprite = sprite;

        public bool TryGetImage<T>(out T image) where T : class
        {
            image = sprite as T;
            return image != null;
        }
    }
}
