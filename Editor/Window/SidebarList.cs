using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Window
{
    /// <summary>A titled, independently scrolling list with a "+" button.</summary>
    internal sealed class SidebarList : VisualElement
    {
        private readonly ScrollView scroll;

        public SidebarList(string title, Action onCreate)
        {
            AddToClassList("dialog-side__section");
            var header = new VisualElement();
            header.AddToClassList("dialog-side__header");
            header.Add(new Label(title));
            var add = new Button(onCreate) { text = "+", tooltip = "Создать" };
            add.AddToClassList("dialog-side__add");
            header.Add(add);
            Add(header);
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            Add(scroll);
        }

        public void ClearItems() => scroll.Clear();

        public VisualElement AddItem(string text, Color? dot, bool selected, Action onClick, Action onDoubleClick)
        {
            var item = new VisualElement();
            item.AddToClassList("dialog-side__item");
            item.EnableInClassList("dialog-side__item--selected", selected);
            if (dot.HasValue)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("dialog-side__dot");
                swatch.style.backgroundColor = dot.Value;
                item.Add(swatch);
            }

            item.Add(new Label(text));
            item.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    onDoubleClick?.Invoke();
                }
                else
                {
                    onClick?.Invoke();
                }
            });
            scroll.Add(item);
            return item;
        }
    }
}
