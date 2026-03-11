using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Overlays;
using UnityEngine;

namespace SpaceMousePro
{
    /// <summary>
    /// Preferences panel: Edit > Preferences > SpaceMouse Pro
    /// </summary>
    static class SpaceMouseSettingsProvider
    {
        [SettingsProvider]
        static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Preferences/SpaceMouse Pro", SettingsScope.User)
            {
                label      = "SpaceMouse Pro",
                guiHandler = DrawGUI,
                keywords   = new HashSet<string> { "SpaceMouse", "3Dconnexion", "3D Mouse", "6DOF" }
            };
        }

        static bool _rawAxisFoldout = false;

        // Column widths for the axis mapping table
        const float kLabelWidth  = 130f;
        const float kDropWidth   = 190f;
        const float kInvWidth    = 50f;
        const float kValueWidth  = 60f;

        static void DrawDivider()
        {
            EditorGUILayout.Space(8);
            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            rect.x     += 4;
            rect.width -= 8;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(8);
        }

        static void DrawGUI(string searchContext)
        {
            EditorGUILayout.Space(4);

            // ── Status ───────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Device", EditorStyles.boldLabel);

            bool available = SpaceMouseDevice.IsAvailable;
            bool connected = SpaceMouseDevice.IsConnected;

            string statusText;
            Color  statusColor;

            if (!available)
            {
                statusText  = "Framework not found — install 3DxWareMac driver";
                statusColor = new Color(1f, 0.4f, 0.4f);
            }
            else if (connected)
            {
                statusText  = "SpaceMouse Pro connected";
                statusColor = new Color(0.4f, 1f, 0.4f);
            }
            else
            {
                statusText  = "Driver ready — no device detected";
                statusColor = new Color(1f, 1f, 0.4f);
            }

            using (new EditorGUI.IndentLevelScope())
            {
                Color prev = GUI.color;
                GUI.color = statusColor;
                EditorGUILayout.LabelField("Status", statusText);
                GUI.color = prev;

                EditorGUILayout.Space(4);
                if (GUILayout.Button("Show Navigation Overlay", GUILayout.Width(180)))
                {
                    SceneView sv = SceneView.lastActiveSceneView;
                    if (sv == null && SceneView.sceneViews.Count > 0)
                        sv = SceneView.sceneViews[0] as SceneView;
                    if (sv != null)
                    {
                        sv.Focus();
                        if (sv.TryGetOverlay("spacemouse-nav", out Overlay overlay))
                            overlay.displayed = true;
                    }
                }
            }

            EditorGUILayout.Space(8);

            // ── Speed ────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Speed", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                SpaceMouseSettings.TranslationSpeed = EditorGUILayout.Slider(
                    new GUIContent("Translation", "Pan and zoom speed multiplier"),
                    SpaceMouseSettings.TranslationSpeed, 0f, 10f);

                SpaceMouseSettings.RotationSpeed = EditorGUILayout.Slider(
                    new GUIContent("Rotation", "Tilt and spin speed multiplier"),
                    SpaceMouseSettings.RotationSpeed, 0f, 10f);
            }

            EditorGUILayout.Space(8);

            // ── Navigation Mode ──────────────────────────────────────────────
            EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                SpaceMouseSettings.NavigationMode = (NavigationMode)EditorGUILayout.EnumPopup(
                    new GUIContent("Rotation Mode",
                        "Orbit: camera moves on a sphere around the pivot; always looks at it.\n" +
                        "FreeLook: first-person — camera rotates in place and moves freely."),
                    SpaceMouseSettings.NavigationMode);

                bool newLock = EditorGUILayout.Toggle(
                    new GUIContent("Lock Horizon (Roll)", "When enabled, camera roll is disabled and the horizon stays level."),
                    SpaceMouseSettings.HorizonLocked);
                if (newLock != SpaceMouseSettings.HorizonLocked)
                    SpaceMouseSettings.HorizonLocked = newLock;
            }

            EditorGUILayout.Space(8);

            // ── Dead zone ────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Dead Zone", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                SpaceMouseSettings.DeadZone = EditorGUILayout.Slider(
                    new GUIContent("Dead Zone", "Normalised threshold below which axis input is ignored"),
                    SpaceMouseSettings.DeadZone, 0f, 0.5f);
            }

            DrawDivider();

            // ── Axis Mapping ─────────────────────────────────────────────────
            EditorGUILayout.LabelField("Axis Mapping", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // Table header
            DrawTableHeader();

            // One row per output channel
            for (int ch = 0; ch < SpaceMouseSettings.ChannelCount; ch++)
                DrawChannelRow(ch, connected);

            DrawDivider();

            // ── Button Mapping ───────────────────────────────────────────────
            EditorGUILayout.LabelField("Button Mapping", EditorStyles.boldLabel);

            EditorGUILayout.Space(4);
            DrawButtonMappingSection(SpaceMouseDevice.Buttons);

            DrawDivider();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15 + 4);
                if (GUILayout.Button("Export Settings…", GUILayout.Width(120)))
                    EditorApplication.delayCall += SpaceMouseSettings.ExportToFile;
                if (GUILayout.Button("Import Settings…", GUILayout.Width(120)))
                    EditorApplication.delayCall += SpaceMouseSettings.ImportFromFile;
                GUILayout.Space(8);
                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140)))
                    SpaceMouseSettings.ResetToDefaults();
            }

            EditorGUILayout.Space(8);

            // ── Raw axis monitor ─────────────────────────────────────────────
            if (connected)
            {
                _rawAxisFoldout = EditorGUILayout.Foldout(_rawAxisFoldout, "Raw Axis Monitor", true, EditorStyles.foldoutHeader);
                if (_rawAxisFoldout)
                    DrawRawAxisMonitor();
            }

            // Keep the panel live so LEDs and axis bars update every frame
            if (connected && EditorWindow.focusedWindow != null)
                EditorWindow.focusedWindow.Repaint();

            // ── Version ──────────────────────────────────────────────────────
            EditorGUILayout.Space(16);
            EditorGUILayout.LabelField("SpaceMouse Pro driver for Unity on MacOS", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Made by Erlend Dal Sakshaug", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Version 1.1", EditorStyles.centeredGreyMiniLabel);
        }

        static void DrawTableHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                padding   = new RectOffset(2, 2, 2, 2),
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(15);
                GUILayout.Label("Channel",     headerStyle, GUILayout.Width(kLabelWidth));
                GUILayout.Label("Source Axis", headerStyle, GUILayout.Width(kDropWidth));
                GUILayout.Label("Invert",      headerStyle, GUILayout.Width(kInvWidth));
                GUILayout.Label("Value",       headerStyle, GUILayout.Width(kValueWidth));
            }

            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            rect.x      += 15;
            rect.width  -= 19;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
            GUILayout.Space(2);
        }

        // Popup entries: index 0 = None, indices 1–6 = AxisNames[0–5]
        // Stored source axis: -1 = None, 0–5 = real axis
        static readonly string[] _axisPopupOptions = BuildAxisPopupOptions();
        static string[] BuildAxisPopupOptions()
        {
            var opts = new string[SpaceMouseDevice.AxisNames.Length + 1];
            opts[0] = "— None —";
            for (int i = 0; i < SpaceMouseDevice.AxisNames.Length; i++)
                opts[i + 1] = SpaceMouseDevice.AxisNames[i];
            return opts;
        }

        static void DrawChannelRow(int ch, bool connected)
        {
            int  currentSrc = SpaceMouseSettings.GetSourceAxis(ch);  // -1..5
            bool isNone     = currentSrc < 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(15);

                GUILayout.Label(SpaceMouseSettings.ChannelNames[ch], GUILayout.Width(kLabelWidth));

                // Source axis dropdown (popup index = source + 1)
                int popupIdx    = currentSrc + 1;
                int newPopupIdx = EditorGUILayout.Popup(popupIdx, _axisPopupOptions, GUILayout.Width(kDropWidth));
                if (newPopupIdx != popupIdx)
                    SpaceMouseSettings.SetSourceAxis(ch, newPopupIdx - 1);

                // Invert toggle — greyed out when source is None
                GUILayout.Space(8);
                using (new EditorGUI.DisabledGroupScope(isNone))
                {
                    bool currentInv = SpaceMouseSettings.GetInvert(ch);
                    bool newInv     = GUILayout.Toggle(currentInv, GUIContent.none, GUILayout.Width(kInvWidth - 8));
                    if (newInv != currentInv)
                        SpaceMouseSettings.SetInvert(ch, newInv);
                }

                // Live value bar — empty when source is None
                if (connected && !isNone)
                {
                    float v = SpaceMouseSettings.GetChannel(ch);
                    DrawMiniBar(v, kValueWidth);
                }
                else
                {
                    GUILayout.Space(kValueWidth);
                }
            }
        }

        /// <summary>Draws a small horizontal bar showing a normalised value in [–1, 1].</summary>
        static void DrawMiniBar(float value, float totalWidth)
        {
            Rect r = GUILayoutUtility.GetRect(totalWidth, 16f,
                         GUILayout.Width(totalWidth), GUILayout.Height(16f));

            // Background
            EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.2f, 0.6f));

            // Centre line
            float cx = r.x + r.width * 0.5f;
            EditorGUI.DrawRect(new Rect(cx - 0.5f, r.y + 2, 1f, r.height - 4),
                               new Color(0.5f, 0.5f, 0.5f, 0.8f));

            // Value fill
            if (Mathf.Abs(value) > 0.001f)
            {
                float fillW = Mathf.Abs(value) * r.width * 0.5f;
                float fillX = value >= 0 ? cx : cx - fillW;
                var fillColor = value >= 0
                    ? new Color(0.3f, 0.7f, 1f, 0.9f)
                    : new Color(1f,   0.5f, 0.2f, 0.9f);
                EditorGUI.DrawRect(new Rect(fillX, r.y + 2, fillW, r.height - 4), fillColor);
            }

            // Numeric label
            GUI.Label(r, $"{value:+0.00;-0.00; 0.00}",
                      new GUIStyle(EditorStyles.miniLabel)
                      {
                          alignment = TextAnchor.MiddleCenter,
                          normal    = { textColor = Color.white },
                      });
        }

        // ── Button mapping UI ────────────────────────────────────────────────

        struct ButtonDef
        {
            public int    BitIndex;
            public string Name;
            public string Group;
            public ButtonDef(int bit, string name, string group)
                { BitIndex = bit; Name = name; Group = group; }
        }

        static readonly ButtonDef[] _buttons =
        {
            new(12, "App Key 1",       "App Keys"),
            new(13, "App Key 2",       "App Keys"),
            new(14, "App Key 3",       "App Keys"),
            new(15, "App Key 4",       "App Keys"),
            new(22, "Esc",             "Modifiers"),
            new(24, "Shift",           "Modifiers"),
            new(25, "Ctrl",            "Modifiers"),
            new(23, "Alt",             "Modifiers"),
            new(8,  "Roll +90\u00b0",  "QuickView"),
            new(2,  "Top",             "QuickView"),
            new(26, "Rotation Toggle", "QuickView"),
            new(5,  "Front",           "QuickView"),
            new(4,  "Right",           "QuickView"),
            new(0,  "Menu",            "Utility"),
            new(1,  "Fit",             "Utility"),
        };

        const float kBtnLedWidth    = 14f;
        const float kBtnLabelWidth  = 110f;
        const float kBtnDisplayWidth = 180f;
        const float kBtnPickWidth   = 26f;
        const float kBtnClearWidth  = 46f;

        // Per-button dropdown state for AdvancedDropdown (must persist across frames)
        static readonly Dictionary<int, AdvancedDropdownState> _dropdownStates = new();

        static void DrawButtonMappingSection(uint btnMask)
        {
            var groupStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                padding   = new RectOffset(4, 2, 4, 2),
            };

            string currentGroup = null;
            foreach (ButtonDef btn in _buttons)
            {
                if (btn.Group != currentGroup)
                {
                    currentGroup = btn.Group;
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField(currentGroup, groupStyle);
                }

                bool   active = (btnMask & (1u << btn.BitIndex)) != 0;
                string saved  = SpaceMouseSettings.GetButtonAction(btn.BitIndex);
                DrawButtonRow(btn.BitIndex, btn.Name, saved, active);
            }
        }

        static void DrawButtonRow(int bitIndex, string name, string saved, bool active)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(15);

                // LED
                Rect ledRect = GUILayoutUtility.GetRect(kBtnLedWidth, 16f,
                    GUILayout.Width(kBtnLedWidth), GUILayout.Height(16f));
                float ledSize = 8f;
                ledRect = new Rect(
                    ledRect.x + (ledRect.width  - ledSize) * 0.5f,
                    ledRect.y + (ledRect.height - ledSize) * 0.5f,
                    ledSize, ledSize);
                EditorGUI.DrawRect(ledRect, active
                    ? new Color(0.2f, 1f, 0.3f, 1f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.8f));

                // Button name
                var labelStyle = new GUIStyle(EditorStyles.label);
                if (active) labelStyle.fontStyle = FontStyle.Bold;
                GUILayout.Label(name, labelStyle, GUILayout.Width(kBtnLabelWidth));

                // Display label (current action's friendly name)
                string displayName = SpaceMouseShortcutPicker.DisplayName(saved);
                var displayStyle = new GUIStyle(EditorStyles.miniTextField)
                {
                    alignment = TextAnchor.MiddleLeft,
                };
                EditorGUILayout.LabelField(displayName, displayStyle, GUILayout.Width(kBtnDisplayWidth));

                // "…" picker button
                if (GUILayout.Button("\u2026", GUILayout.Width(kBtnPickWidth), GUILayout.Height(18f)))
                {
                    Rect pickerBtnRect = GUILayoutUtility.GetLastRect();
                    if (!_dropdownStates.TryGetValue(bitIndex, out var state))
                    {
                        state = new AdvancedDropdownState();
                        _dropdownStates[bitIndex] = state;
                    }

                    var picker = new SpaceMouseShortcutPicker(state);
                    int capturedBit = bitIndex;
                    picker.OnSelected += chosen => SpaceMouseSettings.SetButtonAction(capturedBit, chosen);
                    picker.Show(pickerBtnRect);
                }

                // Clear button
                using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(saved)))
                {
                    if (GUILayout.Button("Clear", GUILayout.Width(kBtnClearWidth), GUILayout.Height(18f)))
                        SpaceMouseSettings.SetButtonAction(bitIndex, "");
                }
            }
        }

        static void DrawRawAxisMonitor()
        {
            // Header
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                var s = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
                GUILayout.Label("Axis", s, GUILayout.Width(kDropWidth + kLabelWidth - 4));
                GUILayout.Label("Value", s, GUILayout.Width(kValueWidth));
            }

            for (int i = 0; i < 6; i++)
            {
                float raw = SpaceMouseDevice.GetRawAxis(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(4);
                    GUILayout.Label(SpaceMouseDevice.AxisNames[i],
                                    GUILayout.Width(kDropWidth + kLabelWidth - 4));
                    DrawMiniBar(raw, kValueWidth);
                }
            }

            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledGroupScope(true))
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.TextField("Buttons",
                    $"0x{SpaceMouseDevice.Buttons:X8}");
            }
        }
    }
}
