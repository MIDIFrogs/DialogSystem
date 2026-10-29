# MIDIFrogs Dialog System

Node-based dialogs for Unity: a graph editor that saves as you type, a runtime that plays linear lines and
answers with text conditions, and uGUI views you can replace piece by piece.

- Linear lines continue with a click, Submit or the gamepad button ("Продолжить…"); no button needed.
- Answers can be **hidden** (visible-if) or **locked** (enabled-if). A locked answer shows what it needs
  ("Требуется: Gold ≥ 10"), built from its condition or written by hand.
- Conditions and actions are text: `Gold >= 10 && !QuestDone`, `Gold -= 10; Hired = true`.
- Localization, sound and input are hooks, so the package has no dependency on a localization system, FMOD or a
  save system.

> 2.0 does not read 1.x dialogs. Conditions and actions assets, the author provider and the separate layout cache
> are gone; rebuild old dialogs in the new editor.

## Installation

Prerequisites:

- Unity 6000.3 or newer.
- [UniTask](https://github.com/Cysharp/UniTask) (install it first; UPM cannot resolve it as a dependency).
- Optional: Input System (`com.unity.inputsystem`) for the ready-made `InputSystemDialogInput`.

Add the package in **Package Manager → Add package from git URL**, pinned to a release:

```
https://github.com/MIDIFrogs/DialogSystem.git#v2.0.0
```

The **Basic Example** sample (Package Manager → Samples) has a scene, prefabs for the dialog view and answer
button, an author with typing blips and a demo dialog.

## Quick start: a linear dialog

1. Open **Window → Dialog Editor → Editor**. Press **+** next to *Диалоги* to create a dialog, and **+** next to
   *Авторы* to create an author (edit its name, colour, avatar and typing sound in the Inspector).
2. Select the author in the palette. Drag from the **Старт** node's port into empty space: a new line appears,
   linked to Start, with that author. Type its text in the node.
3. Drag from the line's **Далее** port into empty space to add the next line. A line without Next ends the dialog.
4. Put the sample's `Dialog View` prefab on a canvas (or build your own, see *Views*), and play the dialog:

```csharp
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.UI;
using MIDIFrogs.DialogSystem.Unity.Conversion;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.Integration;
using UnityEngine;

public class TalkToGuard : MonoBehaviour
{
    [SerializeField] private DialogAsset dialog;
    [SerializeField] private DialogRootView view;

    public async void Talk()
    {
        var service = new DialogService(view);
        var context = new DialogContext();
        context.SetValue("Gold", 5);                       // game state the dialog reads
        await service.Play(DialogImporter.Import(dialog, service.Functions), context, destroyCancellationToken);
        context.TryGetValue("Gold", out object gold);      // read changes back
    }
}
```

`DialogService.Stop()` ends a playing dialog; `Play` then returns normally. Cancelling your own token throws
`OperationCanceledException` as usual.

## Lines and answers

- Every dialog has one **Старт** node (it cannot be deleted). Its link chooses the first line.
- A line has an author, text (rich text allowed), font style, an optional text colour (off = the view's colour),
  a voice, a background sprite, a **Далее** link and answers (**+ Ответ**).
- A line with no enabled answers shows the continue prompt and follows Next. While the text types, the first
  press shows it all, the second continues. On a line with enabled answers the answers appear when the text is
  out (a press or click finishes the typing); then the line waits for an answer, and clicking elsewhere does not
  skip it.
- If a line's answers are all hidden or locked and it has no Next, the dialog ends there (the editor warns).
- Each answer has text, **visible-if**, **enabled-if**, an optional **hint**, **actions** and a target line.
  The node shows *вид* / *доступ* / *действ.* badges with the number of clauses or statements.

## Expressions

Conditions (visible-if, enabled-if):

```
Gold >= 10 && !QuestDone || Role == "Merchant"
HasItem("Sword") && (Reputation > 3 || MetElder)
```

- Operators: `||`, `&&`, `!`, parentheses, `==` `!=` `<` `<=` `>` `>=`.
- Values: numbers (`double`), `"strings"` (case-sensitive), `true` / `false`, variables (identifiers, dots
  allowed: `quest.stage`) and function calls.
- An empty condition is true.
- An unset variable reads as the default of the type it is compared with: `false`, `0` or `""`. Comparing
  different types (`Gold > "ten"`) is false and logs one warning. A game function that throws logs one warning
  and counts as unset.

Actions are statements separated by `;`:

```
Gold -= 10; Hired = true; quest.stage += 1; GiveItem("Map")
```

`x = e`, `x += e`, `x -= e` and function calls are allowed.

The editor checks expressions while you type and shows the error under the field. At runtime a dialog is parsed
once in `DialogImporter.Import`; a broken expression logs one warning and counts as false (a broken action list
is skipped).

### Variables

Variables live in the `IDialogContext` you pass to `Play`: the state of one played dialog. Load what the dialog
needs into it before playing and read the results after. The package never saves anything; keep the values in
your own save data.

### Functions

Register game functions on the service before importing:

```csharp
service.Functions.Register("HasItem", 1, args => DialogValue.FromBool(inventory.Has(args[0].String)),
    hint: "нужен предмет");
```

The editor validates against `MIDIFrogs.DialogSystem.Editor.Model.DialogEditorFunctions.Functions`; register the
same functions there from an `[InitializeOnLoad]` editor class, or the editor reports them as unknown.

### Hints for locked answers

The answer's own hint text wins. Otherwise the hint is built from the failed parts of enabled-if:

- each failed top-level `&&` clause gives one line;
- a comparison renders as `<name> <op> <value>` (`Gold ≥ 10`), with the name from the localized key
  `dialog.var.<name>` when the text resolver has it;
- a function renders its registered hint;
- an `||` clause renders as `одно из: a / b`;
- `!x` renders its source text.

The views show `Требуется:` (key `dialog.required`) above these lines.

## Localization

Every text has a stable key, built from the dialog's `Id` (set from the file name when the dialog is created and
kept when the file is renamed; editable in the properties panel):

| Text | Key |
|---|---|
| Line | `dialog.<dialog id>.<line id>` |
| Answer | `dialog.<dialog id>.<line id>.<answer id>` |
| Answer hint | `dialog.<dialog id>.<line id>.<answer id>.hint` |
| Author name | `dialog.author.<author id>` |
| Continue prompt | `dialog.continue` |
| Hint heading | `dialog.required` |

At runtime set `DialogService.Text` to your `IDialogTextResolver` (`Resolve(key, sourceText)`); the default shows
the written text. In the editor, open **Window → Dialog Editor → Settings** to set the source language (default
`ru`) and a **string sink**: a ScriptableObject implementing `IDialogStringSink`. With a sink assigned the editor
shows **Обновить таблицу строк**, which writes every dialog text and author name with its key.

## Sound

Voices and typing sounds are `[SerializeReference] IDialogSound` values with a type picker. The package ships
`AudioClipSound` and `DialogSoundPlayer` (two AudioSources). Another audio engine needs its own `IDialogSound`
type and an `IDialogSoundPlayer` that plays it; sound kinds a player does not know are ignored.

- A line's voice plays while the line types, and the typing speed follows the voice length.
- Without a voice, the author's typing sound plays every *Blip Every* characters (default 2) with a random pitch
  of ±*Pitch Variance* (default 0.05). Authors without one use the view's default typing sound.

## Input

`DialogRootView` polls an `IDialogInput` for "continue". `InputSystemDialogInput` (compiled when the Input System
is installed) uses an action reference such as `UI/Submit`. Clicks come from the continue prompt, which catches
them anywhere on screen, so no click action is needed. Answers are uGUI buttons: the first enabled answer is selected, so arrows, Submit and the gamepad work.
Locked answers can be selected to read their hint, but pressing them does nothing.

## Views

`DialogRootView` ties optional pieces together; leave out what you do not need:

| Piece | Interface | Shipped |
|---|---|---|
| Line text | `ILineView` | `TypewriterLineView` (unscaled time, keeps rich-text tags), `LineView` (instant) |
| Author | `IAuthorView` | `AuthorView` (name, colour, avatar) |
| Answers | `IChoicesView` | `ChoicesView` + `ResponseButton` prefab |
| Background | `IStoryboardView` | `AnimatedStoryboardView` (cross-fade), `StoryboardView` |
| Sound | `IDialogSoundPlayer` | `DialogSoundPlayer` |
| Input | `IDialogInput` | `InputSystemDialogInput` |
| Continue prompt | — | `ContinuePrompt` (full-screen transparent Image behind the answers) |

`DialogService.PlaybackStrategy` chooses how lines advance: `InteractiveStrategy` (default), `AutoplayStrategy`
(fixed time per line; a player's pick still wins) or `SkipStrategy` (first enabled answer, nothing shown).
You can also implement `IDialogView` from scratch.

## Editor

- Left: dialog list and author palette (drag the splitter between them). Double-click an item to select it in
  the Project window. The selected author is used for new lines; *Без автора* makes lines without one.
- Centre: the graph. Drop a link on empty space to create a linked line; right-click for *Добавить реплику* and
  *Сгруппировать выделенное*. Toolbar: mini-map, *Показать всё*, the properties panel toggle and the save status.
- Right: properties of the selected line (author, text, style, colour, voice, background, answers with their
  conditions, hint and actions), or the dialog's Id and its problems when nothing is selected.
- Changes are saved about half a second after the last edit. Undo/Redo work, one step per typing pause.
- Problems (Start not linked, unreachable lines, all answers conditional without Next, links to deleted lines,
  expression errors, duplicate group titles) show as an orange "!" on nodes and never block saving.
- Layout (positions, groups) is stored in the dialog asset itself.

## Tests

The package has EditMode and PlayMode tests (`Tests/Editor`, `Tests/Runtime`). To run them from a project that
installs the package from git, add `com.midifrogs.dialogsystem` to `testables` in its `Packages/manifest.json`.
