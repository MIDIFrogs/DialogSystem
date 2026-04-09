using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.View;
using UnityEngine;
using UnityEngine.UI;

namespace MIDIFrogs.DialogSystem.UI
{
    public class StoryboardView : MonoBehaviour, IStoryboardView
    {
        [SerializeField] private Image image;

        public void HideStoryboard()
        {
            gameObject.SetActive(false);
        }

        public UniTask ShowStoryboard(IBackgroundImage background)
        {
            if (background.TryGetImage(out Sprite sprite))
            {
                gameObject.SetActive(true);
                image.sprite = sprite;
            }
            return UniTask.CompletedTask;
        }

        public void SkipAnimation() { }
    }
}
