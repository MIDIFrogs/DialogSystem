using MIDIFrogs.DialogSystem.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.MIDIFrogs.DialogSystem.Runtime.Unity.Integration;
using MIDIFrogs.DialogSystem.Unity.View;

namespace MIDIFrogs.DialogSystem.UI
{
    public class AuthorView : MonoBehaviour, IAuthorView
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private DialogAuthorProvider provider;

        public void ShowAuthor(DialogAuthor author)
        {
            if (author == null || provider == null)
            {
                HideAuthor();
                return;
            }

            nameText.text = provider.GetName(author.Id);
            nameText.color = provider.GetColor(author.Id);
            avatarImage.sprite = provider.GetAvatar(author.Id);

            avatarImage.gameObject.SetActive(true);
        }

        public void HideAuthor()
        {
            nameText.text = string.Empty;
            avatarImage.gameObject.SetActive(false);
        }
    }
}
