#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace AntistarAssets
{
    public class AntistarExporter : EditorWindow
    {
        private const string RootPath = "Assets/Antistar Store";
        // the support window is found by its script, so it works from the Antistar.Tooltip package
        // and from the old Assets/Antistar Store/Tooltip folder
        private const string TooltipScriptGuid = "94df6bed996edc34aa69fe7873cb20a3";
        private const string OldTooltipPath = "Assets/Antistar Store/Tooltip";
        // an old tools folder some of our projects still have, its not a pack
        private const string AssistantFolder = "Asset Assistant";
        private const string OutputDir = "Exports";
        private const string SelfFileName = "AntistarExporter.cs";

        private static readonly Color ColBg = new Color(0.051f, 0.047f, 0.039f);
        private static readonly Color ColLine = new Color(0.165f, 0.157f, 0.125f);
        private static readonly Color ColText = new Color(0.941f, 0.937f, 0.914f);
        private static readonly Color ColMuted = new Color(0.659f, 0.651f, 0.612f);
        private static readonly Color ColDim = new Color(0.435f, 0.427f, 0.392f);
        private static readonly Color ColYellow = new Color(1f, 0.839f, 0f);
        private static readonly Color ColWarn = new Color(0.85f, 0.62f, 0.3f);

        private struct PackEntry
        {
            public string path;
            public string clothing;
            public string avatar;
            public bool selected;
        }

        private readonly List<PackEntry> _packs = new List<PackEntry>();
        private Vector2 _scroll;
        private bool _scanned;
        private bool _includeDependencies = true;
        private string _filter = "";
        private string _versionSuffix = "";

        private GUIStyle _styleKicker;
        private GUIStyle _styleHeader;
        private GUIStyle _styleBody;
        private GUIStyle _styleGroup;
        private GUIStyle _styleRow;
        private GUIStyle _styleMini;
        private GUIStyle _styleBtnPrimary;
        private bool _stylesBuilt;

        [MenuItem("Antistar Assets/Export Packs")]
        public static void ShowExporter()
        {
            var win = GetWindow<AntistarExporter>(true, "Antistar Pack Exporter", true);
            float w = Mathf.Clamp(Screen.currentResolution.width * 0.34f, 760f, 1000f);
            float h = Mathf.Clamp(Screen.currentResolution.height * 0.66f, 560f, 950f);
            float x = (Screen.currentResolution.width - w) * 0.5f;
            float y = (Screen.currentResolution.height - h) * 0.5f;
            win.minSize = new Vector2(720f, 520f);
            win.position = new Rect(x, y, w, h);
            win.Show();
        }

        private void OnEnable()
        {
            ScanPacks();
        }

        private void BuildStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _styleKicker = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = ColYellow }
            };

            _styleHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = ColText }
            };

            _styleBody = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = ColMuted }
            };

            _styleGroup = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = ColText }
            };

            _styleRow = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = ColMuted }
            };

            _styleMini = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal = { textColor = ColDim }
            };

            _styleBtnPrimary = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black },
                hover = { textColor = Color.black },
                active = { textColor = Color.black }
            };
        }

        private void ScanPacks()
        {
            _packs.Clear();

            if (!AssetDatabase.IsValidFolder(RootPath))
            {
                _scanned = true;
                return;
            }

            string[] clothingGuids = AssetDatabase.FindAssets("t:Folder", new[] { RootPath });
            foreach (string guid in clothingGuids)
            {
                string clothingPath = AssetDatabase.GUIDToAssetPath(guid);
                string parent = Path.GetDirectoryName(clothingPath).Replace('\\', '/');
                if (parent != RootPath) continue;

                string clothingName = Path.GetFileName(clothingPath);
                if (clothingName == "Tooltip" || clothingName == AssistantFolder || clothingName.StartsWith("_") || clothingName.StartsWith(".")) continue;

                string[] avatarGuids = AssetDatabase.FindAssets("t:Folder", new[] { clothingPath });
                bool hasAvatars = false;
                foreach (string avatarGuid in avatarGuids)
                {
                    string avatarPath = AssetDatabase.GUIDToAssetPath(avatarGuid);
                    string avatarParent = Path.GetDirectoryName(avatarPath).Replace('\\', '/');
                    if (avatarParent != clothingPath) continue;

                    string avatarName = Path.GetFileName(avatarPath);
                    if (avatarName.StartsWith("_") || avatarName.StartsWith(".")) continue;

                    _packs.Add(new PackEntry { path = avatarPath, clothing = clothingName, avatar = avatarName, selected = false });
                    hasAvatars = true;
                }

                if (!hasAvatars)
                {
                    _packs.Add(new PackEntry { path = clothingPath, clothing = clothingName, avatar = "", selected = false });
                }
            }

            _scanned = true;
        }

        private void OnGUI()
        {
            BuildStyles();
            DrawRect(new Rect(0, 0, position.width, position.height), ColBg);

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            GUILayout.Space(14);
            GUILayout.BeginVertical();

            GUILayout.Label("★ ANTISTAR PACK EXPORTER · DEV TOOL", _styleKicker);
            GUILayout.Label("Ship it.", _styleHeader);
            GUILayout.Label("Internal tool for the team. Picks up every pack in Antistar Store and exports ready-to-ship .unitypackage files, with the customer support window included automatically.", _styleBody);
            GUILayout.Space(8);

            if (!_scanned)
            {
                GUILayout.Label("Scanning...", _styleBody);
                EndFrame();
                return;
            }

            if (!AssetDatabase.IsValidFolder(RootPath))
            {
                GUILayout.Label("Root folder not found: " + RootPath, _styleBody);
                EndFrame();
                return;
            }

            if (TooltipPath() == null)
            {
                var warn = new GUIStyle(_styleBody) { normal = { textColor = ColWarn } };
                GUILayout.Label("Support window not found. Install Antistar.Tooltip before exporting so customers get it.", warn);
                GUILayout.Space(6);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter", _styleRow, GUILayout.Width(36));
            _filter = GUILayout.TextField(_filter, GUILayout.Height(20));
            if (GUILayout.Button("All", GUILayout.Width(44), GUILayout.Height(20))) SetAllVisible(true);
            if (GUILayout.Button("None", GUILayout.Width(48), GUILayout.Height(20))) SetAllVisible(false);
            if (GUILayout.Button("Rescan", GUILayout.Width(60), GUILayout.Height(20))) ScanPacks();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            SeparatorLine();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            string lastClothing = null;
            int visible = 0;
            for (int i = 0; i < _packs.Count; i++)
            {
                PackEntry p = _packs[i];
                if (!MatchesFilter(p)) continue;
                visible++;

                if (p.clothing != lastClothing)
                {
                    if (lastClothing != null) GUILayout.Space(6);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(p.clothing, _styleGroup);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("all", _styleMini, GUILayout.Width(24))) SetGroup(p.clothing, true);
                    GUILayout.Label("·", _styleMini, GUILayout.Width(8));
                    if (GUILayout.Button("none", _styleMini, GUILayout.Width(34))) SetGroup(p.clothing, false);
                    GUILayout.EndHorizontal();
                    lastClothing = p.clothing;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(14);
                bool newVal = EditorGUILayout.ToggleLeft(string.IsNullOrEmpty(p.avatar) ? "(whole folder)" : p.avatar, p.selected);
                GUILayout.EndHorizontal();
                if (newVal != p.selected)
                {
                    p.selected = newVal;
                    _packs[i] = p;
                }
            }

            if (visible == 0)
            {
                GUILayout.Label(_packs.Count == 0
                    ? "No packs found. Expected layout: Antistar Store / Clothing / Avatar."
                    : "Nothing matches the filter.", _styleBody);
            }
            EditorGUILayout.EndScrollView();

            SeparatorLine();
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Version suffix", _styleRow, GUILayout.Width(84));
            _versionSuffix = GUILayout.TextField(_versionSuffix, GUILayout.Width(90), GUILayout.Height(20));
            GUILayout.Label("optional, e.g. v1.2", _styleMini, GUILayout.Width(110));
            GUILayout.Space(12);
            _includeDependencies = GUILayout.Toggle(_includeDependencies, " Include dependencies (shared textures and shaders)", _styleRow);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            int selectedCount = 0;
            foreach (var p in _packs) if (p.selected) selectedCount++;

            GUILayout.BeginHorizontal();
            GUILayout.Label(selectedCount + " selected · output: " + Path.GetFullPath(OutputDir), _styleMini);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open output folder", GUILayout.Height(22)))
            {
                if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);
                EditorUtility.RevealInFinder(OutputDir + "/");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            bool canExport = selectedCount > 0 && TooltipPath() != null;
            Rect btn = GUILayoutUtility.GetRect(10f, 38f, GUILayout.ExpandWidth(true));
            bool btnHover = canExport && btn.Contains(Event.current.mousePosition);
            DrawRect(btn, !canExport ? new Color(0.35f, 0.3f, 0.06f) : btnHover ? new Color(1f, 0.898f, 0.25f) : ColYellow);
            if (btnHover) Repaint();
            var btnStyle = _styleBtnPrimary;
            if (!canExport) btnStyle = new GUIStyle(_styleBtnPrimary) { normal = { textColor = new Color(0.16f, 0.14f, 0.05f) } };
            GUI.Label(btn, "Export " + selectedCount + " pack" + (selectedCount == 1 ? "" : "s"), btnStyle);
            if (canExport && GUI.Button(btn, GUIContent.none, GUIStyle.none))
                ExportSelected();

            EndFrame();
        }

        private void EndFrame()
        {
            GUILayout.EndVertical();
            GUILayout.Space(14);
            GUILayout.EndHorizontal();
            GUILayout.Space(12);
        }

        private bool MatchesFilter(PackEntry p)
        {
            if (string.IsNullOrEmpty(_filter)) return true;
            string f = _filter.ToLowerInvariant();
            return p.clothing.ToLowerInvariant().Contains(f) || p.avatar.ToLowerInvariant().Contains(f);
        }

        private void SetAllVisible(bool val)
        {
            for (int i = 0; i < _packs.Count; i++)
            {
                if (!MatchesFilter(_packs[i])) continue;
                var p = _packs[i];
                p.selected = val;
                _packs[i] = p;
            }
        }

        private void SetGroup(string clothing, bool val)
        {
            for (int i = 0; i < _packs.Count; i++)
            {
                if (_packs[i].clothing != clothing) continue;
                var p = _packs[i];
                p.selected = val;
                _packs[i] = p;
            }
        }

        // where the support window is: the package root, or the old folder in Assets. null if neither
        private static string TooltipPath()
        {
            string script = AssetDatabase.GUIDToAssetPath(TooltipScriptGuid);
            if (!string.IsNullOrEmpty(script))
            {
                // <root>/Editor/AntistarTooltip.cs
                string root = Path.GetDirectoryName(Path.GetDirectoryName(script)).Replace('\\', '/');
                if (AssetDatabase.IsValidFolder(root)) return root;
            }
            return AssetDatabase.IsValidFolder(OldTooltipPath) ? OldTooltipPath : null;
        }

        private static string[] BuildExportPaths(string packPath)
        {
            var paths = new List<string> { packPath };
            var seen = new HashSet<string>();
            string tooltip = TooltipPath();
            if (tooltip == null) return paths.ToArray();
            AddToolFiles(paths, seen, tooltip);
            // from the package it goes in with its package.json, so it shows up as a package for the customer too
            string manifest = tooltip + "/package.json";
            if (tooltip.StartsWith("Packages/") && File.Exists(manifest) && seen.Add(manifest)) paths.Add(manifest);
            return paths.ToArray();
        }

        private static void AddToolFiles(List<string> paths, HashSet<string> seen, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            foreach (string guid in AssetDatabase.FindAssets("", new[] { folder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!seen.Add(p)) continue;
                if (AssetDatabase.IsValidFolder(p)) continue;
                if (Path.GetFileName(p) == SelfFileName) continue;
                paths.Add(p);
            }
        }

        private void ExportSelected()
        {
            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);

            var selected = new List<PackEntry>();
            foreach (var p in _packs) if (p.selected) selected.Add(p);

            string suffix = _versionSuffix.Trim();
            if (suffix.Length > 0 && !suffix.StartsWith("_")) suffix = "_" + suffix;

            var options = ExportPackageOptions.Recurse;
            if (_includeDependencies) options |= ExportPackageOptions.IncludeDependencies;

            int exported = 0;
            try
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    var p = selected[i];
                    if (!AssetDatabase.IsValidFolder(p.path))
                    {
                        Debug.LogWarning("[Antistar Exporter] Folder not found, skipping: " + p.path);
                        continue;
                    }

                    string baseName = string.IsNullOrEmpty(p.avatar) ? p.clothing : p.clothing + "_" + p.avatar;
                    string outputPath = Path.Combine(OutputDir, baseName + suffix + ".unitypackage");

                    EditorUtility.DisplayProgressBar("Antistar Pack Exporter", baseName, (float)i / selected.Count);
                    AssetDatabase.ExportPackage(BuildExportPaths(p.path), outputPath, options);
                    Debug.Log("[Antistar Exporter] Exported: " + outputPath);
                    exported++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (exported > 0)
            {
                EditorUtility.RevealInFinder(OutputDir + "/");
                EditorUtility.DisplayDialog(
                    "Export complete",
                    exported + " pack" + (exported == 1 ? "" : "s") + " exported to:\n" + Path.GetFullPath(OutputDir),
                    "Nice!");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Nothing exported",
                    "No valid packs were exported. Check the console for warnings.",
                    "Ok");
            }
        }

        private static void DrawRect(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, color);
        }

        private void SeparatorLine()
        {
            Rect r = GUILayoutUtility.GetRect(10f, 1f, GUILayout.ExpandWidth(true));
            DrawRect(r, ColLine);
        }
    }
}
#endif
