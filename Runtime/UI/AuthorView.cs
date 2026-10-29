using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MIDIFrogs.DialogSystem.UI
{
    /// <summary>Name (localized), colour and avatar from the author asset; an author without one shows only the name.</summary>
    public class AuthorView : MonoBehaviour, IAuthorView
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image avatarImage;

        public void ShowAuthor(DialogAuthor author, IDialogTextResolver text)
        {
            if (author == null)
            {
                HideAuthor();
                return;
            }

            text ??= SourceTextResolver.Instance;
            nameText.text = text.Resolve(author.NameKey, author.Name ?? string.Empty);
            var asset = author.Source as DialogAuthorAsset;
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
