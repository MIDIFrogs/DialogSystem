# MIDIFrogs Dialog System

Node-based dialog system for Unity with editor tooling and runtime UI.

## Features

- Graph-based dialog editor
- Choices with conditions and actions
- Runtime dialog playback system
- Voiceover support
- Background (storyboard) images
- Flexible UI via interfaces
- Autoplay / Interactive / Skip strategies

## Installation

### Via Git URL

Add package via Package Manager:
```
https://github.com/MIDIFrogs/DialogSystem.git
```

### Local

Place the package into your project's `Packages` folder.

---

## Quick Start

1. Create a Dialog Asset  
2. Open **Window → Dialog Editor**  
3. Build your dialog graph  
4. Add `DialogRunner` to scene  
5. Assign UI (from Samples or custom)

---

## Quick Onboarding

### 1. Create Dialog

- Open **Window → Dialog Editor**
- Click on `+` button to create new dialog file
- Add nodes and connect them
- Fill nodes with some text

---

### 2. Setup Scene

- Open sample scene OR create new scene
- Add **Dialog View prefab** to Canvas
- Add **Sample Dialog Runner** to any GameObject

---

### 3. Connect

Assign in `DialogRunner`:

- Dialog Asset
- Dialog View (link from canvas)

---

### 4. Play

Enter Play Mode  
→ Dialog starts

---

## Minimal Setup Example

```csharp
public class Example : MonoBehaviour
{
    [SerializeField] private DialogAsset dialog;
    [SerializeField] private DialogRootView view;

    private void Start()
    {
        // Initialize environment
        var dialogService = new DialogService(rootView, context);
        
        // Import dialog
        var imported = DialogImporter.Import(dialog);
        
        // Begin the show!
        dialogService.Play(imported);
    }
}
```

---

## Architecture

### Runtime

- **Core** — pure dialog logic (no Unity dependencies)
- **Unity** — asset conversion and integrations
- **UI** — default UI implementation

### Editor

- Graph-based dialog editor
- Validation and layout system

---

## UI Customization

The system is interface-driven:

- `IDialogView`
- `ILineView`
- `IChoicesView`
- `IStoryboardView`
- `IVoicePlayer`

You can:
- modify provided prefabs
- or fully replace UI implementation

---

## Text & Styling

Supports:
- Custom font styles
- Extendable text processing pipeline

---

## Samples

Check `Samples~/BasicExample` for:
- working scene
- example dialog
- UI prefabs

---

## Dependencies

- TextMeshPro
- UniTask

---

## License

MIT
