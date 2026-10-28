using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Integration
{
    public interface IDialogAuthorProvider
    {
        Sprite GetAvatar(string authorId);
        Color GetColor(string authorId);
        string GetName(string authorId);
    }
}
