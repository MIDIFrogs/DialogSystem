using UnityEngine;
using UnityEngine.UIElements;

public class RenamableCard : VisualElement
{
    private readonly Label nameLabel;
    private readonly TextField renameField;
    private bool isRenaming = false;

    // Define a delegate for the rename event
    public delegate void RenameEventHandler(string newName);

    // Define the rename event
    public event RenameEventHandler OnRenamed;

    public Color TextColor
    {
        get => nameLabel.resolvedStyle.color; // Get the current text color
        set => nameLabel.style.color = value; // Set the new text color
    }

    public RenamableCard(string initialName)
    {
        // Create a card-like container
        AddToClassList("card");
        style.borderBottomWidth = 1;
        style.borderBottomColor = new Color(0.8f, 0.8f, 0.8f);
        //style.paddingBottom = this.style.paddingLeft = this.style.paddingRight = this.style.paddingTop = 10;
        //style.marginBottom = 5;
        style.backgroundColor = new Color(0, 0, 0, 0.1f);
        style.flexDirection = FlexDirection.Row; // Ensure elements are laid out in a row
        style.flexGrow = 1;

        // Create a label for the texture name
        nameLabel = new Label(initialName);
        nameLabel.AddToClassList("texture-name");
        nameLabel.style.flexGrow = 1; // Allow the label to grow and take available space
        nameLabel.style.marginBottom = nameLabel.style.marginTop = 3;
        nameLabel.style.marginRight = nameLabel.style.marginLeft = 10;
        Add(nameLabel);

        // Create a text field for renaming
        renameField = new TextField
        {
            value = nameLabel.text
        };
        renameField.RegisterCallback<BlurEvent>(evt => FinishRenaming());
        renameField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return)
            {
                FinishRenaming();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                CancelRenaming();
                evt.StopPropagation();
            }
        });

        var clickable = new Clickable(x =>
        {
            StartRenaming();
        });
        clickable.activators.Clear();
        clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });

        // Add a click event to the card
        this.AddManipulator(clickable);

        // Register a key down event to handle renaming
        this.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.F2 && !isRenaming)
            {
                StartRenaming();
                evt.StopPropagation();
            }
        });
    }

    private void StartRenaming()
    {
        isRenaming = true;
        renameField.value = nameLabel.text;
        // Replace the label with the text field
        Add(renameField);
        nameLabel.RemoveFromHierarchy();
        renameField.Focus();
    }

    private void FinishRenaming()
    {
        string newName = renameField.value; // Get the new name
        nameLabel.text = newName; // Update the label text
        renameField.RemoveFromHierarchy(); // Remove the text field
        Add(nameLabel); // Re-add the label
        isRenaming = false; // Reset renaming flag

        // Invoke the rename event
        OnRenamed?.Invoke(newName);
    }

    private void CancelRenaming()
    {
        renameField.RemoveFromHierarchy(); // Remove the text field
        Add(nameLabel); // Re-add the label
        isRenaming = false; // Reset renaming flag
    }
}