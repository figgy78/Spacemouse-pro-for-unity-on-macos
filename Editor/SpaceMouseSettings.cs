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

        // ── Reset to defaults ────────────────────────────────────────────────

        public static void ResetToDefaults()
        {
            TranslationSpeed = 6f;
            RotationSpeed    = 4f;
            DeadZone         = 0.05f;
            NavigationMode   = NavigationMode.Orbit;
            HorizonLocked    = false;

            for (int i = 0; i < ChannelCount; i++)
            {
                SetSourceAxis(i, DefaultSource[i]);
                SetInvert(i, DefaultInvert[i]);
            }
        }
    }
}
