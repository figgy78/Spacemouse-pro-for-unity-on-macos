using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpaceMousePro
{
    public enum NavigationMode
    {
        /// <summary>Camera orbits around a fixed pivot point; always looks at the pivot.</summary>
        Orbit,
        /// <summary>First-person: camera rotates around its own position and moves freely through space.</summary>
        FreeLook,
    }

    /// <summary>
    /// Persisted settings for SpaceMouse Pro editor navigation.
    /// Values are stored in EditorPrefs (per-user, per-machine).
    ///
    /// Each of the 6 output channels can be mapped to any raw device axis and inverted.
    /// Source axis -1 means None — the channel always outputs 0.
    /// Channel indices:
    ///   0 = Pan Horizontal   default source: 0 (Tx)
    ///   1 = Pan Vertical     default source: 2 (Tz)
    ///   2 = Zoom             default source: 1 (Ty)
    ///   3 = Tilt (Pitch)     default source: 3 (Rx)
    ///   4 = Spin (Yaw)       default source: 5 (Rz)
    ///   5 = Roll             default source: 4 (Ry)
    /// </summary>
    public static class SpaceMouseSettings
    {
        // ── Channel definitions ──────────────────────────────────────────────

        public const int ChannelCount = 6;
        public const int MaxButtons   = 32;

        /// <summary>Display names for each output channel.</summary>
        public static readonly string[] ChannelNames =
        {
            "Pan Horizontal",
            "Pan Vertical",
            "Zoom",
            "Tilt (Pitch)",
            "Spin (Yaw)",
            "Roll",
        };

        // Default source axis for each channel (-1 = None)
        static readonly int[]  DefaultSource = { 0, 2, 1, 3, 5, 4 };
        static readonly bool[] DefaultInvert = { false, false, false, false, false, false };

        // EditorPrefs key helpers
        static string SrcKey(int ch) => $"SpaceMouse.AxisSrc.{ch}";
        static string InvKey(int ch) => $"SpaceMouse.AxisInv.{ch}";

        // ── Per-channel axis mapping ─────────────────────────────────────────

        /// <summary>Which raw device axis (0–5) drives this output channel, or -1 for None.</summary>
        public static int GetSourceAxis(int channel) =>
            EditorPrefs.GetInt(SrcKey(channel), DefaultSource[channel]);

        public static void SetSourceAxis(int channel, int value) =>
            EditorPrefs.SetInt(SrcKey(channel), Mathf.Clamp(value, -1, 5));

        /// <summary>Whether the channel value is negated after reading the source axis.</summary>
        public static bool GetInvert(int channel) =>
            EditorPrefs.GetBool(InvKey(channel), DefaultInvert[channel]);

        public static void SetInvert(int channel, bool value) =>
            EditorPrefs.SetBool(InvKey(channel), value);

        // ── Global settings ──────────────────────────────────────────────────

        public static float TranslationSpeed
        {
            get => EditorPrefs.GetFloat("SpaceMouse.TranslationSpeed", 6f);
            set => EditorPrefs.SetFloat("SpaceMouse.TranslationSpeed", value);
        }

        public static float RotationSpeed
        {
            get => EditorPrefs.GetFloat("SpaceMouse.RotationSpeed", 4f);
            set => EditorPrefs.SetFloat("SpaceMouse.RotationSpeed", value);
        }

        public static float DeadZone
        {
            get => EditorPrefs.GetFloat("SpaceMouse.DeadZone", 0.05f);
            set => EditorPrefs.SetFloat("SpaceMouse.DeadZone", value);
        }

        public static NavigationMode NavigationMode
        {
            get => (NavigationMode)EditorPrefs.GetInt("SpaceMouse.NavigationMode", (int)NavigationMode.Orbit);
            set => EditorPrefs.SetInt("SpaceMouse.NavigationMode", (int)value);
        }

        /// <summary>
        /// When true, roll output is suppressed regardless of axis mapping.
        /// The source axis setting is untouched — horizon lock is independent.
        /// </summary>
        public static bool HorizonLocked
        {
            get => EditorPrefs.GetBool("SpaceMouse.HorizonLocked", false);
            set => EditorPrefs.SetBool("SpaceMouse.HorizonLocked", value);
        }

        /// <summary>
        /// When true, all rotational input (tilt, spin, roll) is suppressed.
        /// Toggled at runtime by the #rotation/toggle-lock built-in command.
        /// Not persisted — resets to false when Unity restarts.
        /// </summary>
        public static bool RotationLocked { get; set; } = false;

        // ── Processed value accessors ────────────────────────────────────────

        /// <summary>
        /// Returns the processed value for the given channel:
        /// source axis → invert → dead zone. Returns 0 if source is None (-1).
        /// </summary>
        public static float GetChannel(int channel)
        {
            int src = GetSourceAxis(channel);
            if (src < 0) return 0f;

            float raw  = SpaceMouseDevice.GetRawAxis(src);
            float sign = GetInvert(channel) ? -1f : 1f;
            float v    = raw * sign;
            float dz   = DeadZone;
            return Mathf.Abs(v) < dz ? 0f : v;
        }

        // Named accessors used by SpaceMouseController
        public static float GetPanX() => GetChannel(0);
        public static float GetPanY() => GetChannel(1);
        public static float GetZoom() => GetChannel(2);
        public static float GetTilt() => GetChannel(3);
        public static float GetSpin() => GetChannel(4);
        public static float GetRoll() => HorizonLocked ? 0f : GetChannel(5);

        // ── Button action mapping ────────────────────────────────────────────

        static string BtnKey(int btn) => $"SpaceMouse.ButtonAction.{btn}";

        /// <summary>
        /// Default menu item paths for known buttons.
        /// Used when the user has not explicitly set or cleared a mapping.
        /// </summary>
        static readonly Dictionary<int, string> _buttonDefaults = new()
        {
            // App Keys
            { 12, "Main Menu/Edit/Undo"                  },   // App Key 1
            { 13, "Main Menu/Edit/Redo"                  },   // App Key 2
            { 14, "#view/toggle-ortho"                   },   // App Key 3
            { 15, "@Scene View/Toggle 2D Mode"           },   // App Key 4
            // Modifiers
            { 22, "#modifier/escape"                     },   // Esc
            { 24, "#modifier/shift"                      },   // Shift
            { 25, "#modifier/ctrl"                       },   // Ctrl
            { 23, "#modifier/option"                     },   // Alt
            // QuickView
            { 8,  "#view/roll+90"                        },   // Roll +90°
            { 2,  "#view/top"                            },   // Top
            { 26, "#rotation/toggle-lock"                },   // Rotation Toggle
            { 5,  "#view/front"                          },   // Front
            { 4,  "#view/right"                          },   // Right
            // Utility
            { 0,  "Main Menu/Edit/Project Settings..."   },   // Menu
            { 1,  "#frame/selected"                      },   // Fit
        };

        /// <summary>
        /// Menu item path to execute when button N is pressed (0-based).
        /// Returns the built-in default if the user has not overridden it,
        /// or "" if the user has explicitly cleared the mapping.
        /// </summary>
        public static string GetButtonAction(int button) =>
            EditorPrefs.GetString(BtnKey(button),
                _buttonDefaults.TryGetValue(button, out string def) ? def : "");

        /// <summary>Sets or clears the menu item path for a button. Pass "" to disable.</summary>
        public static void SetButtonAction(int button, string menuPath) =>
            EditorPrefs.SetString(BtnKey(button), menuPath ?? "");

        /// <summary>Canonical names for known SpaceMouse Pro buttons, keyed by bit index.</summary>
        static readonly Dictionary<int, string> _buttonNames = new()
        {
            { 0,  "Menu"            },
            { 1,  "Fit"             },
            { 2,  "Top"             },
            { 4,  "Right"           },
            { 5,  "Front"           },
            { 8,  "Roll +90\u00b0"  },
            { 12, "App Key 1"       },
            { 13, "App Key 2"       },
            { 14, "App Key 3"       },
            { 15, "App Key 4"       },
            { 22, "Esc"             },
            { 23, "Alt"             },
            { 24, "Shift"           },
            { 25, "Ctrl"            },
            { 26, "Rotation Toggle" },
        };

        /// <summary>Human-readable name for the button at the given bit index.</summary>
        public static string GetButtonName(int bitIndex) =>
            _buttonNames.TryGetValue(bitIndex, out string name) ? name : $"Bit {bitIndex}";

        // ── Export / Import ──────────────────────────────────────────────────

        [Serializable]
        class SettingsData
        {
            public float   translationSpeed;
            public float   rotationSpeed;
            public float   deadZone;
            public int     navigationMode;
            public bool    horizonLocked;
            public int[]   axisSrc      = new int[ChannelCount];
            public bool[]  axisInv      = new bool[ChannelCount];
            public string[] buttonActions = new string[MaxButtons];
        }

        /// <summary>Writes all settings to a JSON file chosen via a save dialog.</summary>
        public static void ExportToFile()
        {
            string path = EditorUtility.SaveFilePanel(
                "Export SpaceMouse Settings", "", "SpaceMouseSettings", "json");
            if (string.IsNullOrEmpty(path)) return;

            var data = new SettingsData
            {
                translationSpeed = TranslationSpeed,
                rotationSpeed    = RotationSpeed,
                deadZone         = DeadZone,
                navigationMode   = (int)NavigationMode,
                horizonLocked    = HorizonLocked,
            };

            for (int i = 0; i < ChannelCount; i++)
            {
                data.axisSrc[i] = GetSourceAxis(i);
                data.axisInv[i] = GetInvert(i);
            }

            for (int i = 0; i < MaxButtons; i++)
                data.buttonActions[i] = GetButtonAction(i);

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(data, prettyPrint: true));
                Debug.Log($"[SpaceMouse] Settings exported to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpaceMouse] Failed to write settings file: {e.Message}");
            }
        }

        /// <summary>Reads settings from a JSON file chosen via an open dialog and applies them.</summary>
        public static void ImportFromFile()
        {
            string path = EditorUtility.OpenFilePanel(
                "Import SpaceMouse Settings", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpaceMouse] Failed to read settings file: {e.Message}");
                return;
            }

            SettingsData data;
            try
            {
                data = JsonUtility.FromJson<SettingsData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpaceMouse] Failed to parse settings file: {e.Message}");
                return;
            }

            TranslationSpeed = data.translationSpeed;
            RotationSpeed    = data.rotationSpeed;
            DeadZone         = data.deadZone;
            NavigationMode   = Enum.IsDefined(typeof(NavigationMode), data.navigationMode)
                ? (NavigationMode)data.navigationMode
                : NavigationMode.Orbit;
            HorizonLocked    = data.horizonLocked;

            for (int i = 0; i < ChannelCount && i < data.axisSrc.Length; i++)
            {
                SetSourceAxis(i, data.axisSrc[i]);
                SetInvert(i, data.axisInv[i]);
            }

            if (data.buttonActions != null)
                for (int i = 0; i < MaxButtons && i < data.buttonActions.Length; i++)
                    SetButtonAction(i, data.buttonActions[i]);

            Debug.Log($"[SpaceMouse] Settings imported from {path}");
        }

        // ── Reset to defaults ────────────────────────────────────────────────

        public static void ResetToDefaults()
        {
            TranslationSpeed = 6f;
            RotationSpeed    = 4f;
            DeadZone         = 0.05f;
            NavigationMode   = NavigationMode.Orbit;
            HorizonLocked    = false;
            RotationLocked   = false;

            for (int i = 0; i < ChannelCount; i++)
            {
                SetSourceAxis(i, DefaultSource[i]);
                SetInvert(i, DefaultInvert[i]);
            }

            // Delete button overrides so built-in defaults take effect again
            for (int i = 0; i < 32; i++)
                EditorPrefs.DeleteKey(BtnKey(i));
        }
    }
}
