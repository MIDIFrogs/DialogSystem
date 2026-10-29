using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Editor.Model;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using FontStyle = MIDIFrogs.DialogSystem.Core.FontStyle;

namespace MIDIFrogs.DialogSystem.Editor.Window
{
    /// <summary>Properties of the selected line, or of the dialog when nothing is selected.</summary>
    internal sealed class PropertiesPanel : VisualElement
    {
        private readonly DialogEditorWindow editor;
        private readonly ScrollView scroll;
        private readonly HashSet<string> openAnswers = new();
        private readonly List<(string answerId, Foldout foldout)> foldouts = new();
        private string lineId;
        private TextField textField;
        private ObjectField authorField;
        private VisualElement warningsBox;

        public PropertiesPanel(DialogEditorWindow editor)
        {
            this.editor = editor;
            AddToClassList("dialog-panel");
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            Add(scroll);
        }

        public void OpenAnswer(string answerId) => openAnswers.Add(answerId);

        public void Show(string selectedLineId)
        {
            lineId = selectedLineId;
            scroll.Clear();
            foldouts.Clear();
            textField = null;
            authorField = null;
            DialogAsset dialog = editor.Dialog;
            if (dialog == null)
            {
                scroll.Add(Hint("Выберите или создайте диалог слева."));
                return;
            }

            int index = dialog.Lines.FindIndex(line => line.Id == lineId);
            if (index < 0)
            {
                lineId = null;
                BuildDialog(dialog);
            }
            else
            {
                BuildLine(dialog, dialog.Lines[index], index);
            }

            warningsBox = new VisualElement();
            scroll.Add(warningsBox);
        }

        /// <summary>Picks up values changed elsewhere (inline node text, author menu) without rebuilding.</summary>
        public void Refresh()
        {
            DialogAsset.Line line = editor.Dialog != null ? editor.Dialog.FindLine(lineId) : null;
            if (line == null)
            {
                return;
            }

            if (textField != null && textField.value != line.Text)
            {
                textField.SetValueWithoutNotify(line.Text);
            }

            if (authorField != null && authorField.value != line.Author)
            {
                authorField.SetValueWithoutNotify(line.Author);
            }

            foreach ((string answerId, Foldout foldout) in foldouts)
            {
                DialogAsset.Answer answer = line.Answers.Find(a => a.Id == answerId);
                if (answer != null)
                {
                    foldout.text = AnswerTitle(answer);
                }
            }
        }

        public void ShowWarnings(IReadOnlyList<DialogWarning> warnings)
        {
            if (warningsBox == null)
            {
                return;
            }

            warningsBox.Clear();
            List<DialogWarning> shown = lineId == null
                ? warnings.ToList()
                : warnings.Where(w => w.LineId == lineId && w.Error == null).ToList();
            if (shown.Count == 0)
            {
                return;
            }

            warningsBox.Add(Header("Проблемы"));
            foreach (DialogWarning warning in shown)
            {
                var label = new Label(warning.Message);
                label.AddToClassList("dialog-panel__warning");
                if (lineId == null && warning.LineId != null)
                {
                    string target = warning.LineId;
                    label.RegisterCallback<ClickEvent>(_ => editor.SelectLine(target));
                    label.tooltip = "Показать реплику";
                }
                warningsBox.Add(label);
            }
        }

        private void BuildDialog(DialogAsset dialog)
        {
            scroll.Add(Header("Диалог"));
            var id = new TextField("Id") { value = dialog.Id };
            id.RegisterValueChangedCallback(evt => editor.Change("Id диалога", d => d.Id = evt.newValue, ChangeKind.Typing, "dialog.id"));
            scroll.Add(id);
            scroll.Add(Hint("Id входит в ключи локализации (dialog.<id>....). Он не меняется при переименовании файла."));
            scroll.Add(Hint("Выделите реплику, чтобы редактировать её."));
        }

        private void BuildLine(DialogAsset dialog, DialogAsset.Line line, int index)
        {
            scroll.Add(Header("Реплика"));

            authorField = new ObjectField("Автор") { objectType = typeof(DialogAuthorAsset), value = line.Author };
            authorField.RegisterValueChangedCallback(evt =>
                EditLine("Автор реплики", target => target.Author = evt.newValue as DialogAuthorAsset, ChangeKind.Value));
            scroll.Add(authorField);

            textField = new TextField("Текст") { multiline = true, value = line.Text };
            textField.AddToClassList("dialog-panel__text");
            textField.RegisterValueChangedCallback(evt =>
                EditLine("Текст реплики", target => target.Text = evt.newValue, ChangeKind.Typing, lineId + ".text"));
            scroll.Add(textField);

            var styleRow = new VisualElement();
            styleRow.AddToClassList("dialog-panel__row");
            styleRow.Add(new Label("Стиль") { name = "label" });
            styleRow.Add(StyleToggle(line, FontStyle.Bold, "B", "Жирный"));
            styleRow.Add(StyleToggle(line, FontStyle.Italic, "I", "Курсив"));
            styleRow.Add(StyleToggle(line, FontStyle.Underline, "U", "Подчёркнутый"));
            styleRow.Add(StyleToggle(line, FontStyle.Strikethrough, "S", "Зачёркнутый"));
            scroll.Add(styleRow);

            var colorField = new ColorField("Цвет текста") { value = line.Color };
            colorField.SetEnabled(line.OverrideColor);
            var overrideColor = new Toggle("Свой цвет") { value = line.OverrideColor };
            overrideColor.RegisterValueChangedCallback(evt =>
            {
                colorField.SetEnabled(evt.newValue);
                EditLine("Свой цвет", target => target.OverrideColor = evt.newValue, ChangeKind.Value);
            });
            colorField.RegisterValueChangedCallback(evt => EditLine("Цвет текста", target => target.Color = evt.newValue, ChangeKind.Value));
            scroll.Add(overrideColor);
            scroll.Add(colorField);

            var serialized = new SerializedObject(dialog);
            SerializedProperty voice = serialized.FindProperty($"{nameof(DialogAsset.Lines)}.Array.data[{index}].{nameof(DialogAsset.Line.Voice)}");
            var voiceField = new PropertyField(voice, "Голос");
            voiceField.Bind(serialized);
            voiceField.TrackPropertyValue(voice, _ => editor.AfterChange(ChangeKind.Value));
            scroll.Add(voiceField);

            var background = new ObjectField("Фон") { objectType = typeof(Sprite), value = line.Background };
            background.RegisterValueChangedCallback(evt => EditLine("Фон", target => target.Background = evt.newValue as Sprite, ChangeKind.Value));
            scroll.Add(background);

            scroll.Add(Header("Ответы"));
            if (line.Answers.Count == 0)
            {
                scroll.Add(Hint("Без ответов реплика листается кликом или Submit по ссылке «Далее»."));
            }

            foreach (DialogAsset.Answer answer in line.Answers)
            {
                scroll.Add(AnswerFoldout(answer));
            }

            var add = new Button(() => editor.AddAnswer(lineId)) { text = "+ Ответ" };
            scroll.Add(add);
        }

        private Foldout AnswerFoldout(DialogAsset.Answer answer)
        {
            string answerId = answer.Id;
            var foldout = new Foldout { text = AnswerTitle(answer), value = openAnswers.Contains(answerId) };
            foldout.AddToClassList("dialog-panel__answer");
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.target != foldout)
                {
                    return;
                }

                if (evt.newValue)
                {
                    openAnswers.Add(answerId);
                }
                else
                {
                    openAnswers.Remove(answerId);
                }
            });
            foldouts.Add((answerId, foldout));

            var text = new TextField("Текст") { value = answer.Text };
            text.RegisterValueChangedCallback(evt =>
                EditAnswer(answerId, "Текст ответа", target => target.Text = evt.newValue, answerId + ".text"));
            foldout.Add(text);

            foldout.Add(ExpressionField("Видимость", answer.VisibleIf, "нет условий: ответ виден всегда", false,
                value => EditAnswer(answerId, "Видимость ответа", target => target.VisibleIf = value, answerId + ".visible")));
            foldout.Add(ExpressionField("Доступность", answer.EnabledIf, "нет условий: ответ доступен всегда", false,
                value => EditAnswer(answerId, "Доступность ответа", target => target.EnabledIf = value, answerId + ".enabled")));

            var hint = new TextField("Подсказка") { value = answer.Hint };
            hint.textEdition.placeholder = "авто из условия доступности";
            hint.RegisterValueChangedCallback(evt =>
                EditAnswer(answerId, "Подсказка ответа", target => target.Hint = evt.newValue, answerId + ".hint"));
            foldout.Add(hint);

            foldout.Add(ExpressionField("Действия", answer.Actions, "например: Gold -= 10; Met = true", true,
                value => EditAnswer(answerId, "Действия ответа", target => target.Actions = value, answerId + ".actions")));

            var remove = new Button(() => editor.RemoveAnswer(lineId, answerId)) { text = "Удалить ответ" };
            remove.AddToClassList("dialog-panel__remove");
            foldout.Add(remove);
            return foldout;
        }

        private VisualElement ExpressionField(string label, string value, string placeholder, bool script,
            System.Action<string> onChange)
        {
            var box = new VisualElement();
            var field = new TextField(label) { value = value };
            field.textEdition.placeholder = placeholder;
            var error = new Label();
            error.AddToClassList("dialog-error");
            box.Add(field);
            box.Add(error);
            ShowError(error, value, script);
            field.RegisterValueChangedCallback(evt =>
            {
                ShowError(error, evt.newValue, script);
                onChange(evt.newValue);
            });
            return box;
        }

        private void ShowError(Label label, string source, bool script)
        {
            ExpressionError error;
            bool ok = script
                ? DialogScript.Parse(source, editor.Functions, out error) != null
                : DialogExpression.Parse(source, editor.Functions, out error) != null;
            label.style.display = ok ? DisplayStyle.None : DisplayStyle.Flex;
            label.text = ok ? string.Empty : $"{error.Message} (символ {error.Position + 1})";
        }

        private ToolbarToggle StyleToggle(DialogAsset.Line line, FontStyle flag, string text, string tooltip)
        {
            var toggle = new ToolbarToggle { text = text, tooltip = tooltip, value = (line.FontStyle & flag) != 0 };
            toggle.AddToClassList("dialog-panel__style");
            toggle.RegisterValueChangedCallback(evt => EditLine("Стиль текста", target =>
                target.FontStyle = evt.newValue ? target.FontStyle | flag : target.FontStyle & ~flag, ChangeKind.Value));
            return toggle;
        }

        private void EditLine(string action, System.Action<DialogAsset.Line> apply, ChangeKind kind, string typingKey = null)
        {
            string id = lineId;
            editor.Change(action, dialog =>
            {
                DialogAsset.Line line = dialog.FindLine(id);
                if (line != null)
                {
                    apply(line);
                }
            }, kind, typingKey);
        }

        private void EditAnswer(string answerId, string action, System.Action<DialogAsset.Answer> apply, string typingKey)
        {
            EditLine(action, line =>
            {
                DialogAsset.Answer answer = line.Answers.Find(a => a.Id == answerId);
                if (answer != null)
                {
                    apply(answer);
                }
            }, ChangeKind.Typing, typingKey);
        }

        private static string AnswerTitle(DialogAsset.Answer answer) =>
            string.IsNullOrEmpty(answer.Text) ? "(пустой ответ)" : answer.Text;

        private static Label Header(string text)
        {
            var label = new Label(text);
            label.AddToClassList("dialog-panel__header");
            return label;
        }

        private static Label Hint(string text)
        {
            var label = new Label(text);
            label.AddToClassList("dialog-panel__hint");
            return label;
        }
    }
}
