using System;
using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Inspectors
{
    /// <summary>Type picker ("Нет", "Audio Clip Sound", …) plus the fields of the chosen sound.</summary>
    [CustomPropertyDrawer(typeof(DialogSoundAttribute))]
    public sealed class DialogSoundDrawer : PropertyDrawer
    {
        private const string NoneChoice = "Нет";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            List<Type> types = SoundTypes();
            var choices = new List<string> { NoneChoice };
            choices.AddRange(types.Select(type => ObjectNames.NicifyVariableName(type.Name)));

            SerializedObject serializedObject = property.serializedObject;
            string path = property.propertyPath;
            var root = new VisualElement();
            var dropdown = new DropdownField(property.displayName, choices, IndexOf(property, types));
            dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            var children = new VisualElement();
            children.AddToClassList("dialog-sound__fields");
            children.style.marginLeft = 15;
            root.Add(dropdown);
            root.Add(children);
            BuildChildren(property, children);

            dropdown.RegisterValueChangedCallback(_ =>
            {
                serializedObject.Update();
                SerializedProperty current = serializedObject.FindProperty(path);
                int index = dropdown.index;
                current.managedReferenceValue = index <= 0 ? null : Activator.CreateInstance(types[index - 1]);
                serializedObject.ApplyModifiedProperties();
                BuildChildren(serializedObject.FindProperty(path), children);
            });
            return root;
        }

        public static List<Type> SoundTypes() =>
            TypeCache.GetTypesDerivedFrom<IDialogSound>()
                .Where(type => !type.IsAbstract && !type.IsGenericType && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                    && type.IsDefined(typeof(SerializableAttribute), false) && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name)
                .ToList();

        private static int IndexOf(SerializedProperty property, List<Type> types)
        {
            object value = property.managedReferenceValue;
            return value == null ? 0 : types.IndexOf(value.GetType()) + 1;
        }

        private static void BuildChildren(SerializedProperty property, VisualElement container)
        {
            container.Clear();
            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            if (!child.NextVisible(true))
            {
                return;
            }

            while (!SerializedProperty.EqualContents(child, end))
            {
                var field = new PropertyField(child.Copy());
                field.Bind(property.serializedObject);
                container.Add(field);
                if (!child.NextVisible(false))
                {
                    break;
                }
            }
        }
    }
}
