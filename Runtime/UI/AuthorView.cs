using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MIDIFrogs.DialogSystem.UI
{
    public class AuthorView : MonoBehaviour, IAuthorView
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image avatarImage;

        public void ShowAuthor(DialogAuthor author)
        {
            if (author == null)
            {
                HideAuthor();
                return;
            }

            var asset = author.Source as DialogAuthorAsset;
            nameText.text = author.Name;
            if (asset != null)
            {
                nameText.color = asset.NameColor;
                avatarImage.sprite = asset.Avatar;
            }
            avatarImage.gameObject.SetActive(asset != null && asset.Avatar != null);
        }

        public void HideAuthor()
        {
            nameText.text = string.Empty;
            avatarImage.gameObject.SetActive(false);
        }
    }
}
