using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;



namespace ProjectOrganizerTool
{
    
    [Serializable]
    public class FileMoveOperation
    {
        public string Source;
        public string Destination;

        public FileMoveOperation(string source, string destination)
        {
            Source = source;
            Destination = destination;
        }
    }

    [Serializable]
    public class UndoMoveBatch
    {
        public List<FileMoveOperation> Moves = new();
    }
    
    public class ProjectOrganizerWindow : EditorWindow
    {
        private const string ConfigAssetPath = "Assets/Editor/ProjectOrganizerConfig.asset";
        private const string UndoLogPath = "Library/ProjectOrganizerUndo.json";
        private readonly Color _accent = new(0.82f, 0.14f, 1.0f, 1f);
        private ProjectOrganizerConfig _config;
        private readonly Color _dangerColor = new(0.8f, 0.08f, 0.15f, 1f);
        private readonly Color _dryRunColor = new(0.6f, 0.8f, 1f, 1f);
        private bool _dryRunMode = true;
        private string _fileSearch = "";
        private bool _filesScanned;

        private readonly Dictionary<string, bool> _fileToggles = new();
        private readonly Dictionary<string, string> _fileToTargetFolder = new();
        private readonly Color _footer = new(0.72f, 0.80f, 1f, 1f);
        private readonly Color _glowWhite = new(0.95f, 0.95f, 1f, 1f);
        private readonly Color _hotPink = new(0.82f, 0.14f, 1.0f, 1f);

        // === Undo ===
        private UndoMoveBatch _lastUndoBatch;
        private Texture2D _logoTexture;

        private readonly Color _mainBg = new(0.09f, 0.09f, 0.11f, 0.98f);

        // UI de config (mapping, dossiers, extensions)
        private Vector2 _mappingScroll;

        private readonly Color _okColor = new(0.55f, 0.14f, 0.50f, 1f);
        private Vector2 _previewScroll;

        private Vector2 _scrollPos;
        private string _statusMessage = "";
        private Vector2 _treeScroll;
        private readonly Color _warnColor = new(0.55f, 0.14f, 0.50f, 1f);

        [MenuItem("Tools/🗂️ Project Organizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectOrganizerWindow>("Project Organizer");
            window.minSize = new Vector2(720, 800);
        }

        private void OnEnable()
        {
            _logoTexture = Resources.Load<Texture2D>("Icon-160x160 - Project Organizer");
            LoadOrCreateConfig();
        }

        private void OnGUI()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), _mainBg);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (_logoTexture != null)
                GUILayout.Label(_logoTexture, GUILayout.Width(100), GUILayout.Height(100));
            else
                GUILayout.Label("🗂️",
                    new GUIStyle(EditorStyles.label) { fontSize = 54, alignment = TextAnchor.MiddleCenter });
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = _glowWhite }
            };
            var genStyle = new GUIStyle(titleStyle) { normal = { textColor = _accent } };
            var titleHeight = titleStyle.CalcHeight(new GUIContent("Project"), 400);
            GUI.Label(GUILayoutUtility.GetRect(new GUIContent("Project "), titleStyle, GUILayout.Height(titleHeight)),
                "Project ", titleStyle);
            GUI.Label(GUILayoutUtility.GetRect(new GUIContent("Organizer"), genStyle, GUILayout.Height(titleHeight)),
                "Organizer", genStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            var subStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _hotPink }
            };
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("A CLEAN, PRO WORKFLOW — EDITABLE, SAFE, PERSISTENT", subStyle, GUILayout.Width(420));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(14);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            try
            {
                DrawNeonSection(() =>
                {
                    EditorGUILayout.HelpBox(
                        "Organize and migrate your project files into a clean, professional folder structure! " +
                        "All mappings and options are PERSISTENT. Full preview, dry-run, undo-friendly.",
                        MessageType.Info);
                });

                DrawConfigUI();

                DrawNeonSection(() =>
                {
                    if (DrawHoverButton("🔍 Scan files", ScanFiles, _warnColor, "Scan files in included folders", 0,
                            true, false, true))
                    {
                        _filesScanned = true;
                        _fileSearch = "";
                    }

                    if (_filesScanned && _fileToggles.Count > 0)
                    {
                        GUILayout.Space(8);
                        EditorGUILayout.BeginHorizontal();
                        _fileSearch = EditorGUILayout.TextField("🔎 Search file:", _fileSearch);
                        DrawHoverButton("✔️ All", () =>
                        {
                            foreach (var k in _fileToggles.Keys.ToList()) _fileToggles[k] = true;
                        }, _okColor, "Check all", 62);
                        DrawHoverButton("❌ None", () =>
                        {
                            foreach (var k in _fileToggles.Keys.ToList()) _fileToggles[k] = false;
                        }, _dangerColor, "Uncheck all", 70);
                        EditorGUILayout.EndHorizontal();

                        var filtered = string.IsNullOrWhiteSpace(_fileSearch)
                            ? _fileToggles.Keys.ToList()
                            : _fileToggles.Keys
                                .Where(k => Path.GetFileName(k).ToLower().Contains(_fileSearch.ToLower())).ToList();

                        var maxHeight = Mathf.Min(_fileToggles.Count * 21 + 16, 320);
                        using (var v = new EditorGUILayout.VerticalScope(GUILayout.MaxHeight(maxHeight)))
                        {
                            foreach (var file in filtered)
                            {
                                EditorGUILayout.BeginHorizontal();
                                _fileToggles[file] = EditorGUILayout.ToggleLeft(
                                    new GUIContent(Path.GetFileName(file), file), _fileToggles[file],
                                    GUILayout.Width(230));
                                var folderStyle = new GUIStyle(EditorStyles.miniLabel)
                                    { normal = { textColor = new Color(0.6f, 0.8f, 1f) }, fontSize = 10 };
                                GUILayout.Label($"[{Path.GetDirectoryName(file)}]", folderStyle);
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                });

                DrawNeonSection(() =>
                {
                    SectionHeader("Preview: Upcoming Moves",
                        "Each checked file will be moved to its target folder.");
                    _dryRunMode = EditorGUILayout.ToggleLeft("Dry run (just preview, doesn't move anything)",
                        _dryRunMode);

                    if (_fileToggles.Count > 0)
                    {
                        _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.Height(220));
                        foreach (var kv in _fileToggles.Where(kv => kv.Value))
                        {
                            var file = kv.Key;
                            var fileName = Path.GetFileName(file);
                            var assetPath = file.Substring(file.IndexOf("Assets/"));
                            var targetFolder = _fileToTargetFolder.ContainsKey(file)
                                ? _fileToTargetFolder[file]
                                : "Assets/Misc";
                            var targetPath = $"{targetFolder}/{fileName}";
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("→", GUILayout.Width(16));
                            GUILayout.Label(fileName, GUILayout.Width(170));
                            GUILayout.Label("From:", GUILayout.Width(38));
                            GUILayout.Label(Path.GetDirectoryName(assetPath), EditorStyles.miniLabel,
                                GUILayout.Width(200));
                            GUILayout.Label("To:", GUILayout.Width(18));
                            GUILayout.Label(targetFolder, EditorStyles.boldLabel, GUILayout.Width(200));
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.EndScrollView();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Nothing to move. Scan and check files first.", MessageType.None);
                    }
                });

                DrawNeonSection(() =>
                {
                    GUILayout.Space(10);
                    DrawHoverButton(
                        _dryRunMode ? "👀 SIMULATE" : "🚀 ORGANIZE NOW",
                        () => Organize(!_dryRunMode),
                        _dryRunMode ? _warnColor : _accent,
                        _dryRunMode
                            ? "Preview what would be moved (safe)"
                            : "Actually move the selected files",
                        0, true, false, true);

                    // == Bouton UNDO si possible, toujours charger à la volée ==
                    UndoMoveBatch undoBatch = _lastUndoBatch ?? LoadUndoBatchFromFile();
                    if (undoBatch != null && undoBatch.Moves != null && undoBatch.Moves.Count > 0)
                    {
                        EditorGUILayout.Space(5);
                        if (DrawHoverButton("⏪ UNDO last organize", () =>
                        {
                            _lastUndoBatch = undoBatch;
                            UndoLastMoveBatch();
                            Repaint();
                        }, _warnColor, "Undo the last organization", 0, true, false, true))
                        {
                            // Action déjà dans le callback
                        }
                    }

                    if (!string.IsNullOrEmpty(_statusMessage))
                    {
                        Color statusColor;
                        if (_lastUndoBatch != null && _statusMessage.Contains("UNDO"))
                            statusColor = new Color(1f, 0.86f, 0.45f); // Jaune Undo
                        else if (_dryRunMode)
                            statusColor = _dryRunColor;
                        else if (_statusMessage.Contains("Moved"))
                            statusColor = _okColor;
                        else
                            statusColor = _dangerColor;

                        var style = new GUIStyle(EditorStyles.label)
                        {
                            wordWrap = true,
                            fontSize = 13,
                            normal = { textColor = statusColor }
                        };
                        EditorGUILayout.LabelField(_statusMessage, style);
                    }
                });
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(7);
            var sigStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = _footer }
            };
            GUILayout.Label("© 2025 ProjectOrganizerTool • v1.6", sigStyle);
        }

        private void LoadOrCreateConfig()
        {
            _config = AssetDatabase.LoadAssetAtPath<ProjectOrganizerConfig>(ConfigAssetPath);
            if (_config == null)
            {
                _config = CreateInstance<ProjectOrganizerConfig>();
                AssetDatabase.CreateAsset(_config, ConfigAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("ProjectOrganizerConfig asset created at: " + ConfigAssetPath);
            }
        }

        private void DrawConfigUI()
        {
            DrawNeonSection(() =>
            {
                SectionHeader("Extension to Folder Mapping (editable)",
                    "Add, modify or remove your sorting rules freely.");

                _mappingScroll = EditorGUILayout.BeginScrollView(_mappingScroll, GUILayout.Height(190));
                int mappingToRemove = -1;
                for (int i = 0; i < _config.ExtensionMappings.Count; i++)
                {
                    var m = _config.ExtensionMappings[i];
                    EditorGUILayout.BeginHorizontal();
                    m.Extension = EditorGUILayout.TextField(m.Extension, GUILayout.Width(50));
                    GUILayout.Label("→", GUILayout.Width(15));
                    m.TargetFolder = EditorGUILayout.TextField(m.TargetFolder);
                    if (DrawHoverButton("🗑️", null, _dangerColor, "Delete this mapping", 28, false, true))
                        mappingToRemove = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (mappingToRemove >= 0)
                    _config.ExtensionMappings.RemoveAt(mappingToRemove);

                EditorGUILayout.EndScrollView();

                if (DrawHoverButton("➕ Add mapping",
                        () => _config.ExtensionMappings.Add(new ExtensionMapping(".ext", "Assets/Dossier")), _okColor,
                        "Add a rule"))
                {
                }

                EditorUtility.SetDirty(_config);
            });

            DrawNeonSection(() =>
            {
                SectionHeader("Included / Excluded Folders", "Modify the list if needed.");
                // INCLUDES
                List<int> includeToRemove = new List<int>();
                for (var i = 0; i < _config.IncludeFolders.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    _config.IncludeFolders[i] = EditorGUILayout.TextField(_config.IncludeFolders[i]);
                    if (DrawHoverButton("🗑️", null, _dangerColor, "Delete", 28, false, true))
                        includeToRemove.Add(i);
                    EditorGUILayout.EndHorizontal();
                }
                for (int i = includeToRemove.Count - 1; i >= 0; i--)
                    _config.IncludeFolders.RemoveAt(includeToRemove[i]);

                if (DrawHoverButton("➕ Add folder", () => _config.IncludeFolders.Add("Assets"), _okColor, "Add")) { }

                // EXCLUDES
                GUILayout.Space(5);
                List<int> excludeToRemove = new List<int>();
                for (var i = 0; i < _config.ExcludeFolders.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    _config.ExcludeFolders[i] = EditorGUILayout.TextField(_config.ExcludeFolders[i]);
                    if (DrawHoverButton("🗑️", null, _dangerColor, "Delete", 28, false, true))
                        excludeToRemove.Add(i);
                    EditorGUILayout.EndHorizontal();
                }
                for (int i = excludeToRemove.Count - 1; i >= 0; i--)
                    _config.ExcludeFolders.RemoveAt(excludeToRemove[i]);

                if (DrawHoverButton("➕ Add exclusion", () => _config.ExcludeFolders.Add(""), _warnColor, "Add exclusion")) { }

                EditorUtility.SetDirty(_config);
            });

            DrawNeonSection(() =>
            {
                SectionHeader("Extensions to scan", "Format: .cs, .png, .asset etc. (no spaces)");
                _config.ExtensionsToScan = EditorGUILayout.TextField(_config.ExtensionsToScan);
                EditorUtility.SetDirty(_config);
            });
        }

        private void DrawNeonSection(Action drawContent)
        {
            var cardStyle = new GUIStyle(GUI.skin.box);
            cardStyle.normal.background = MakeTex(2, 2, new Color(0.13f, 0.14f, 0.18f, 0.94f));
            cardStyle.margin = new RectOffset(12, 12, 0, 0);
            cardStyle.padding = new RectOffset(14, 14, 10, 10);
            EditorGUILayout.BeginVertical(cardStyle);
            drawContent.Invoke();
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (var i = 0; i < pix.Length; ++i) pix[i] = col;
            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void SectionHeader(string title, string desc)
        {
            GUILayout.Space(6);
            var hdr = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = _accent } };
            GUILayout.Label(title, hdr);
            var mini = new GUIStyle(EditorStyles.miniLabel)
                { wordWrap = true, fontSize = 10, normal = { textColor = Color.grey } };
            GUILayout.Label(desc, mini);
        }

        private bool DrawHoverButton(string label, Action onClick, Color color, string tooltip = "", float width = 0,
            bool big = false, bool danger = false, bool fullWidth = false)
        {
            var rect = fullWidth
                ? GUILayoutUtility.GetRect(new GUIContent(label),
                    big ? EditorStyles.miniButton : EditorStyles.miniButton, GUILayout.Height(big ? 36 : 22),
                    GUILayout.ExpandWidth(true))
                : GUILayoutUtility.GetRect(new GUIContent(label),
                    big ? EditorStyles.miniButton : EditorStyles.miniButton, GUILayout.Height(big ? 36 : 22),
                    width > 0 ? GUILayout.Width(width) : GUILayout.ExpandWidth(false));
            var isHover = rect.Contains(Event.current.mousePosition);
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = isHover ? new Color(color.r + 0.08f, color.g + 0.08f, color.b + 0.08f, 1) : color;
            if (isHover) EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            var clicked = GUI.Button(rect, new GUIContent(label, tooltip));
            GUI.backgroundColor = prevBg;
            if (clicked && onClick != null) onClick();
            return clicked;
        }

        private void ScanFiles()
        {
            _fileToggles.Clear();
            _fileToTargetFolder.Clear();

            var exts = _config.ExtensionsToScan.Split(',', ';')
                .Select(e => e.Trim().ToLower())
                .Where(e => !string.IsNullOrEmpty(e) && e.StartsWith("."))
                .ToArray();

            foreach (var folder in _config.IncludeFolders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var ext in exts)
                {
                    var files = Directory.GetFiles(folder, "*" + ext, SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var norm = file.Replace("\\", "/");
                        if (!_config.ExcludeFolders.Exists(e => !string.IsNullOrEmpty(e) && norm.StartsWith(e)))
                            if (!_fileToggles.ContainsKey(norm))
                            {
                                _fileToggles.Add(norm, true);
                                var targetFolder = GetTargetFolderForFile(norm);
                                _fileToTargetFolder.Add(norm, targetFolder);
                            }
                    }
                }
            }
        }

        private string GetTargetFolderForFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            var mapping = _config.ExtensionMappings.FirstOrDefault(m => m.Extension.ToLower() == ext);
            return mapping != null ? mapping.TargetFolder : "Assets/Misc";
        }

        private void Organize(bool applyMoves)
        {
            foreach (var folder in _config.TemplateFolders)
            {
                var fullPath = Path.Combine("Assets", folder).Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(fullPath))
                {
                    var parts = fullPath.Substring(7).Split('/');
                    var parent = "Assets";
                    foreach (var part in parts)
                    {
                        var testPath = $"{parent}/{part}";
                        if (!AssetDatabase.IsValidFolder(testPath))
                            AssetDatabase.CreateFolder(parent, part);
                        parent = testPath;
                    }
                }
            }

            if (_fileToggles.Count == 0 || !_fileToggles.Any(kv => kv.Value))
            {
                _statusMessage = "Nothing to organize! Please scan and check files first.";
                return;
            }

            int moved = 0, skipped = 0, conflict = 0;
            var log = new StringBuilder();
            var undoBatch = new UndoMoveBatch();

            foreach (var kv in _fileToggles.Where(kv => kv.Value))
            {
                var file = kv.Key;
                var fileName = Path.GetFileName(file);
                var targetFolder = GetTargetFolderForFile(file);
                var assetPath = file.Substring(file.IndexOf("Assets/"));
                var targetPath = $"{targetFolder}/{fileName}";
                if (assetPath == targetPath)
                {
                    log.AppendLine($"[SKIP] {fileName} is already in the correct folder.");
                    skipped++;
                    continue;
                }

                if (File.Exists(targetPath))
                {
                    log.AppendLine($"[CONFLICT] {fileName} already exists at destination (ignored).");
                    conflict++;
                    continue;
                }

                if (applyMoves)
                {
                    var moveResult = AssetDatabase.MoveAsset(assetPath, targetPath);
                    if (string.IsNullOrEmpty(moveResult))
                    {
                        moved++;
                        log.AppendLine($"[MOVE] {fileName} → {targetFolder}");
                        undoBatch.Moves.Add(new FileMoveOperation(targetPath, assetPath));
                    }
                    else
                    {
                        log.AppendLine($"[ERROR] {fileName}: {moveResult}");
                    }
                }
                else
                {
                    log.AppendLine($"[SIMULATE] {fileName} → {targetFolder}");
                }
            }

            AssetDatabase.Refresh();

            if (applyMoves)
            {
                _lastUndoBatch = undoBatch;
                SaveUndoBatchToFile(undoBatch);
                _statusMessage =
                    $"Moved {moved} files! ({skipped} already in place, {conflict} conflicts)\nYou can undo this move with UNDO.";

            }
            else
            {
                _statusMessage = $"[DRY RUN] {moved + skipped + conflict} simulated moves — no actual changes made.\n{log}";
            }
        }

        // === UNDO system ===
        private void SaveUndoBatchToFile(UndoMoveBatch batch)
        {
            try
            {
                var json = JsonUtility.ToJson(batch, true);
                File.WriteAllText(UndoLogPath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not save undo log: " + e);
            }
        }

        private UndoMoveBatch LoadUndoBatchFromFile()
        {
            if (!File.Exists(UndoLogPath)) return null;
            try
            {
                var json = File.ReadAllText(UndoLogPath);
                return JsonUtility.FromJson<UndoMoveBatch>(json);
            }
            catch
            {
                return null;
            }
        }

        private void UndoLastMoveBatch()
        {
            int undone = 0, failed = 0, conflicts = 0;
            var log = new StringBuilder();

            foreach (var move in _lastUndoBatch.Moves)
            {
                if (!File.Exists(move.Source))
                {
                    log.AppendLine($"[SKIP] {move.Source} does not exist (already moved or deleted).");
                    continue;
                }

                if (File.Exists(move.Destination))
                {
                    log.AppendLine($"[CONFLICT] {move.Destination} already exists (not overwritten).");
                    conflicts++;
                    continue;
                }

                var result = AssetDatabase.MoveAsset(move.Source, move.Destination);
                if (string.IsNullOrEmpty(result))
                {
                    log.AppendLine($"[UNDO] {Path.GetFileName(move.Source)} → {move.Destination}");
                    undone++;
                }
                else
                {
                    log.AppendLine($"[ERROR] {move.Source}: {result}");
                    failed++;
                }
            }

            AssetDatabase.Refresh();
            File.Delete(UndoLogPath);
            _lastUndoBatch = null;
            _statusMessage =
                $"UNDO: {undone} files restored. {conflicts} conflicts, {failed} errors.\n{log}";
            Repaint();
        }
    }
}
