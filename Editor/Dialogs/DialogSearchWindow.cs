using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Dialogs
{
    public class DialogSearchWindow : ScriptableObject, ISearchWindowProvider
	{
		private DialogGraphView graphView;
		private Texture2D indentationIcon;

        public void Initialize(DialogGraphView dsGraphView)
        {
            graphView = dsGraphView;

            indentationIcon = new Texture2D(1, 1);
            indentationIcon.SetPixel(0, 0, Color.clear);
            indentationIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context) => new()
            {
                new SearchTreeGroupEntry(new GUIContent("Create Elements")),
                new SearchTreeGroupEntry(new GUIContent("Dialog Nodes"), 1),
            };

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            return false;
        }
    }
}