using System;
using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Editor.Metadata;
using MIDIFrogs.DialogSystem.Unity;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Dialogs
{
    public class DialogGraphRepository
    {
        private readonly DialogAsset _dialog;
        private readonly string _fileName;
        private readonly Dictionary<string, LineNode> _nodeMap = new();
        private readonly DialogGraphView _view;
        private DialogGraphData _cache;

        public DialogGraphRepository(DialogGraphView view, DialogAsset dialog)
        {
            _view = view;
            _dialog = dialog;
            _fileName = dialog.name;
            EnsureCacheExists();
        }

        public static string GetCachePath(DialogAsset dialog)
        {
            var settings = DialogEditorSettings.GetOrCreateSettings();
            if (!AssetDatabase.IsValidFolder(settings.CachePath))
            {
                var parts = settings.CachePath.Split('/');
                string accum = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = parts[i];
                    var candidate = $"{accum}/{next}";
                    if (!AssetDatabase.IsValidFolder(candidate))
                        AssetDatabase.CreateFolder(accum, next);
                    accum = candidate;
                }
            }
            return $"{settings.CachePath}/{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dialog))}.asset";
        }

        public void Load()
        {
            if (!ValidateDialog(out var errors))
            {
                Debug.LogWarning("Dialog validation failed. Cache will be rebuilt.");
                foreach (var error in errors)
                {
                    Debug.LogWarning("Validation error detected: " + error);
                }
                RebuildCache();
            }

            _view.ClearGraph();
            _nodeMap.Clear();

            var groupsById = new Dictionary<string, LinesGroup>();
            var groupsByName = new Dictionary<string, LinesGroup>(StringComparer.Ordinal);

            foreach (var grpData in _cache.Groups)
            {
                var group = new LinesGroup(grpData.Name, grpData.Position);
                group.ID = grpData.ID;

                _view.AddElement(group);

                groupsById[group.ID] = group;
                if (!string.IsNullOrEmpty(group.title))
                    groupsByName[group.title] = group;
            }

            var layoutById = _cache.Lines.ToDictionary(l => l.ID, l => l);

            bool hasLayout = layoutById.Count > 0 && layoutById.Values.Any(l => l.Position != Vector2.zero);

            Dictionary<string, Vector2> autoLayout = null;

            if (!hasLayout)
            {
                autoLayout = BuildAutoLayout(_dialog.Nodes);
            }

            foreach (var assetNode in _dialog.Nodes)
            {
                Vector2 position;

                if (autoLayout != null && autoLayout.TryGetValue(assetNode.GUID, out var autoPos))
                {
                    position = autoPos;
                }
                else if (layoutById.TryGetValue(assetNode.GUID, out var layout))
                {
                    position = layout.Position;
                }
                else
                {
                    position = Vector2.zero;
                }

                var node = new LineNode
                {
                    ID = assetNode.GUID,
                    Author = assetNode.Author,
                    Text = assetNode.Text,
                    Voice = assetNode.Voice,
                    FontStyle = (TMPro.FontStyles)assetNode.FontStyle,
                    FrameSplash = assetNode.FrameSplash,
                    Responses = new List<DialogResponseData>(),
                };

                foreach (var choice in assetNode.Choices ?? new List<DialogAsset.Choice>())
                {
                    node.Responses.Add(new DialogResponseData
                    {
                        Text = choice.Text,
                        NodeID = choice.NextNodeGuid,
                        Conditions = choice.Conditions?.ToList() ?? new List<ConditionAsset>(),
                        Actions = choice.Actions?.ToList() ?? new List<ActionAsset>()
                    });
                }

                _view.AddElement(node);

                if (!string.IsNullOrEmpty(assetNode.GroupId) &&
                    groupsByName.TryGetValue(assetNode.GroupId, out var group))
                {
                    try
                    {
                        node.Group = group;
                        group.AddElement(node);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(ex);
                    }
                }

                node.Initialize(_view, position);
                _nodeMap[assetNode.GUID] = node;
            }

            foreach (var node in _nodeMap.Values)
            {
                for (int i = 0; i < node.Responses.Count; i++)
                {
                    var resp = node.Responses[i];

                    if (string.IsNullOrEmpty(resp.NodeID) ||
                        !_nodeMap.TryGetValue(resp.NodeID, out var target))
                        continue;

                    var outputPort = node.outputContainer[i] as Port;
                    var inputPort = target.inputContainer[0] as Port;

                    if (outputPort == null || inputPort == null)
                        continue;

                    var edge = outputPort.ConnectTo(inputPort);
                    _view.Add(edge);
                }
            }

            _view.OnGraphChanged();
        }

        public void Save()
        {
            Undo.RecordObject(_cache, "Update Dialog Cache");
            _cache.Groups.Clear();
            _cache.Lines.Clear();

            foreach (var grp in _view.graphElements.OfType<LinesGroup>())
            {
                _cache.Groups.Add(new DialogGroupData
                {
                    ID = grp.ID,
                    Name = grp.title,
                    Position = grp.GetPosition().position
                });
            }

            var groupIdByGroup = _view.graphElements.OfType<LinesGroup>().ToDictionary(g => g, g => g.ID);
            foreach (var node in _view.graphElements.OfType<LineNode>())
            {
                _cache.Lines.Add(new LineNodeData
                {
                    ID = node.ID,
                    GroupID = node.Group != null && groupIdByGroup.TryGetValue(node.Group, out var gid) ? gid : null,
                    Position = node.GetPosition().position,
                });
            }

            EditorUtility.SetDirty(_cache);

            Undo.RecordObject(_dialog, "Save Dialog Asset");
            var existingByGuid = _dialog.Nodes.ToDictionary(n => n.GUID, n => n);
            var viewNodes = _view.graphElements.OfType<LineNode>().ToList();
            var newGuidByTempId = new Dictionary<string, string>();
            var newAssetNodes = new List<DialogAsset.Node>();

            string RemapNodeId(string id)
            {
                if (string.IsNullOrEmpty(id)) return id;
                return newGuidByTempId.TryGetValue(id, out var mapped) ? mapped : id;
            }

            foreach (var ln in viewNodes)
            {
                DialogAsset.Node assetNode;
                if (!existingByGuid.TryGetValue(ln.ID, out assetNode))
                {
                    assetNode = new DialogAsset.Node();
                    newAssetNodes.Add(assetNode);
                    var newGuid = assetNode.GUID;
                    newGuidByTempId[ln.ID] = newGuid;
                    ln.ID = newGuid;
                }
                else
                {
                    newAssetNodes.Add(assetNode);
                }

                assetNode.Text = ln.Text;
                assetNode.Author = ln.Author;
                assetNode.Voice = ln.Voice;
                assetNode.FontStyle = (Core.FontStyle)ln.FontStyle;
                assetNode.FrameSplash = ln.FrameSplash;
                assetNode.GroupId = ln.Group?.title ?? string.Empty;

                assetNode.Choices = new List<DialogAsset.Choice>();
                foreach (var resp in ln.Responses ?? Enumerable.Empty<DialogResponseData>())
                {
                    assetNode.Choices.Add(new DialogAsset.Choice
                    {
                        Text = resp.Text,
                        Conditions = resp.Conditions?.ToList() ?? new List<ConditionAsset>(),
                        Actions = resp.Actions?.ToList() ?? new List<ActionAsset>(),
                        NextNodeGuid = RemapNodeId(resp.NodeID)
                    });
                }
            }

            var allTargets = new HashSet<string>(viewNodes.SelectMany(n => n.Responses ?? Enumerable.Empty<DialogResponseData>())
                .Select(r => RemapNodeId(r.NodeID))
                .Where(id => !string.IsNullOrEmpty(id)));

            var finalIds = new HashSet<string>(viewNodes.Select(n => n.ID));
            var rootNodeId = finalIds.FirstOrDefault(id => !allTargets.Contains(id)) ?? viewNodes.FirstOrDefault()?.ID;

            if (!string.IsNullOrEmpty(rootNodeId))
                newAssetNodes = newAssetNodes.OrderBy(n => n.GUID == rootNodeId ? 0 : 1).ToList();

            _dialog.Nodes = newAssetNodes;
            EditorUtility.SetDirty(_dialog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private bool ValidateDialog(out List<string> errors)
        {
            errors = new List<string>();

            var nodes = _dialog.Nodes;

            if (nodes == null || nodes.Count == 0)
                return true;

            var guidSet = new HashSet<string>();
            var duplicates = new HashSet<string>();

            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.GUID))
                {
                    errors.Add("Node with empty GUID detected.");
                    continue;
                }

                if (!guidSet.Add(node.GUID))
                {
                    duplicates.Add(node.GUID);
                }
            }

            foreach (var dup in duplicates)
                errors.Add($"Duplicate GUID detected: {dup}");

            var validGuids = new HashSet<string>(guidSet);

            foreach (var node in nodes)
            {
                foreach (var choice in node.Choices ?? Enumerable.Empty<DialogAsset.Choice>())
                {
                    if (string.IsNullOrEmpty(choice.NextNodeGuid))
                        continue;

                    if (!validGuids.Contains(choice.NextNodeGuid))
                    {
                        errors.Add($"Broken link: {node.GUID} -> {choice.NextNodeGuid}");
                    }
                }
            }

            return errors.Count == 0;
        }

        private void EnsureCacheExists()
        {
            string cachePath = GetCachePath(_dialog);
            _cache = AssetDatabase.LoadAssetAtPath<DialogGraphData>(cachePath);
            if (_cache != null)
                return;
            _cache = ScriptableObject.CreateInstance<DialogGraphData>();
            _cache.Initialize(_fileName);
            AssetDatabase.CreateAsset(_cache, cachePath);
            AssetDatabase.SaveAssets();

            BuildCacheFromDialogAsset();

            EditorUtility.SetDirty(_cache);
            AssetDatabase.SaveAssets();
        }

        private void RebuildCache()
        {
            string cachePath = GetCachePath(_dialog);

            if (_cache != null)
            {
                AssetDatabase.DeleteAsset(cachePath);
            }

            _cache = ScriptableObject.CreateInstance<DialogGraphData>();
            _cache.Initialize(_fileName);

            AssetDatabase.CreateAsset(_cache, cachePath);

            BuildCacheFromDialogAsset();

            EditorUtility.SetDirty(_cache);
            AssetDatabase.SaveAssets();
        }

        private void BuildCacheFromDialogAsset()
        {
            var groupNames = new HashSet<string>(_dialog.Nodes.Select(n => n.GroupId).Where(n => !string.IsNullOrEmpty(n)));
            var groupIdByName = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var name in groupNames)
            {
                var id = Guid.NewGuid().ToString();
                _cache.Groups.Add(new DialogGroupData { ID = id, Name = name, Position = Vector2.zero });
                groupIdByName[name] = id;
            }

            foreach (var n in _dialog.Nodes)
            {
                _cache.Lines.Add(new LineNodeData
                {
                    ID = n.GUID,
                    GroupID = string.IsNullOrEmpty(n.GroupId) ? null : (groupIdByName.TryGetValue(n.GroupId, out var gid) ? gid : null),
                    Position = Vector2.zero,
                });
            }
        }

        private Dictionary<string, Vector2> BuildAutoLayout(List<DialogAsset.Node> nodes)
        {
            var result = new Dictionary<string, Vector2>();

            if (nodes == null || nodes.Count == 0)
                return result;

            var incoming = new HashSet<string>(
                nodes.SelectMany(n => n.Choices ?? Enumerable.Empty<DialogAsset.Choice>())
                     .Select(c => c.NextNodeGuid)
                     .Where(id => !string.IsNullOrEmpty(id))
            );

            var root = nodes.FirstOrDefault(n => !incoming.Contains(n.GUID)) ?? nodes[0];

            var levels = new Dictionary<string, int>();
            var queue = new Queue<(DialogAsset.Node node, int depth)>();
            queue.Enqueue((root, 0));
            levels[root.GUID] = 0;

            while (queue.Count > 0)
            {
                var (node, depth) = queue.Dequeue();

                foreach (var choice in node.Choices ?? Enumerable.Empty<DialogAsset.Choice>())
                {
                    if (string.IsNullOrEmpty(choice.NextNodeGuid))
                        continue;

                    if (levels.ContainsKey(choice.NextNodeGuid))
                        continue;

                    levels[choice.NextNodeGuid] = depth + 1;

                    var next = nodes.FirstOrDefault(n => n.GUID == choice.NextNodeGuid);
                    if (next != null)
                        queue.Enqueue((next, depth + 1));
                }
            }

            var grouped = levels
                .GroupBy(kv => kv.Value)
                .OrderBy(g => g.Key)
                .ToList();

            const float X_SPACING = 400f;
            const float Y_SPACING = 650f;

            foreach (var group in grouped)
            {
                int index = 0;
                int count = group.Count();

                foreach (var (id, depth) in group)
                {
                    float x = depth * X_SPACING;
                    float y = index * Y_SPACING;

                    result[id] = new Vector2(x, y);
                    index++;
                }
            }

            int extraIndex = 0;
            var extras = nodes.Select(x => x.GUID).Except(result.Keys);
            foreach (var id in extras)
            {
                float x = -1 * Y_SPACING;
                float y = extraIndex * X_SPACING;
                result[id] = new Vector2(x, y);
                extraIndex++;
            }

            return result;
        }
    }
}