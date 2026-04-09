using UnityEngine;

namespace Assets.MIDIFrogs.DialogSystem.Runtime.Unity.Integration
{
    public interface IDialogAuthorProvider
    {
        Sprite GetAvatar(string authorId);
        Color GetColor(string authorId);
        string GetName(string authorId);
    }
}
