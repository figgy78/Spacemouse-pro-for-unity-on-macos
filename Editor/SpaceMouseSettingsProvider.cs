using System.Collections.Generic;
using UnityEditor;
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

            EditorGUILayout.Space(8);

            // ── Axis Mapping ─────────────────────────────────────────────────
            EditorGUILayout.LabelField("Axis Mapping", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // Table header
            DrawTableHeader();

            // One row per output channel
            for (int ch = 0; ch < SpaceMouseSettings.ChannelCount; ch++)
                DrawChannelRow(ch, connected);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                GUILayout.Label("Button mapping coming soon.",
                    new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Italic });
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15 + 4);
                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140)))
                    SpaceMouseSettings.ResetToDefaults();
            }

            EditorGUILayout.Space(8);

            // ── Scene View Overlay ───────────────────────────────────────────
            EditorGUILayout.LabelField("Scene View", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
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

            // ── Raw axis monitor ─────────────────────────────────────────────
            if (connected)
            {
                _rawAxisFoldout = EditorGUILayout.Foldout(_rawAxisFoldout, "Raw Axis Monitor", true, EditorStyles.foldoutHeader);
                if (_rawAxisFoldout)
                {
                    DrawRawAxisMonitor();
                }
                if (EditorWindow.focusedWindow != null)
                    EditorWindow.focusedWindow.Repaint();
            }

            // ── Version ──────────────────────────────────────────────────────
            EditorGUILayout.Space(16);
            EditorGUILayout.LabelField("SpaceMouse Pro driver for Unity on MacOS", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Made by Erlend Dal Sakshaug", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Version 1.0", EditorStyles.centeredGreyMiniLabel);
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
                GUILayout.Space(4);
                GUILayout.Label("Channel",     headerStyle, GUILayout.Width(kLabelWidth));
                GUILayout.Label("Source Axis", headerStyle, GUILayout.Width(kDropWidth));
                GUILayout.Label("Invert",      headerStyle, GUILayout.Width(kInvWidth));
                GUILayout.Label("Value",       headerStyle, GUILayout.Width(kValueWidth));
            }

            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            rect.x      += 4;
            rect.width  -= 8;
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
                GUILayout.Space(4);

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
