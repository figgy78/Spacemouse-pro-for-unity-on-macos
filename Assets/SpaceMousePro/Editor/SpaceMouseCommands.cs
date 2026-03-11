using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace SpaceMousePro
{
    /// <summary>
    /// Registry of built-in SpaceMouse commands (prefix "#") and shortcut simulation
    /// (prefix "@") that are not reachable via ExecuteMenuItem.
    ///
    ///   #view/… #frame/…   — SceneView API (fire-and-forget on press)
    ///   #modifier/…        — macOS CGEvent key-down on press, key-up on release
    ///                        (ESC fires as a tap — down+up — on press only)
    ///   @shortcutId        — look up ShortcutManager binding, simulate key via CGEvent
    /// </summary>
    static class SpaceMouseCommands
    {
        public class CommandInfo
        {
            public string Id;
            public string DisplayName;
        }

        static readonly Dictionary<string, CommandInfo> _registry;
        static readonly List<CommandInfo> _viewCommands;
        static readonly List<CommandInfo> _modifierCommands;

        // Modifier IDs that fire as a single tap (down+up on press) rather than hold
        static readonly HashSet<string> _tapModifiers = new() { "#modifier/escape" };

        static SpaceMouseCommands()
        {
            _viewCommands = new List<CommandInfo>
            {
                new CommandInfo { Id = "#frame/selected",         DisplayName = "Frame Selected"      },
                new CommandInfo { Id = "#rotation/toggle-lock",   DisplayName = "Rotation Toggle"     },
                new CommandInfo { Id = "#view/top",               DisplayName = "Top View"            },
                new CommandInfo { Id = "#view/front",        DisplayName = "Front View"          },
                new CommandInfo { Id = "#view/right",        DisplayName = "Right View"          },
                new CommandInfo { Id = "#view/back",         DisplayName = "Back View"           },
                new CommandInfo { Id = "#view/left",         DisplayName = "Left View"           },
                new CommandInfo { Id = "#view/bottom",       DisplayName = "Bottom View"         },
                new CommandInfo { Id = "#view/perspective",  DisplayName = "Perspective View"    },
                new CommandInfo { Id = "#view/toggle-ortho", DisplayName = "Toggle Orthographic" },
                new CommandInfo { Id = "#view/roll+90",      DisplayName = "Roll +90\u00b0"      },
                new CommandInfo { Id = "#view/roll-90",      DisplayName = "Roll \u221290\u00b0" },
            };

            _modifierCommands = new List<CommandInfo>
            {
                new CommandInfo { Id = "#modifier/escape",       DisplayName = "Escape (Esc)"          },
                new CommandInfo { Id = "#modifier/shift",        DisplayName = "Shift \u21e7"          },
                new CommandInfo { Id = "#modifier/ctrl",         DisplayName = "Control \u2303"        },
                new CommandInfo { Id = "#modifier/cmd",          DisplayName = "Command \u2318"        },
                new CommandInfo { Id = "#modifier/option",       DisplayName = "Option \u2325"         },
                new CommandInfo { Id = "#modifier/caps-lock",    DisplayName = "Caps Lock"             },
                new CommandInfo { Id = "#modifier/right-shift",  DisplayName = "Right Shift \u21e7"    },
                new CommandInfo { Id = "#modifier/right-ctrl",   DisplayName = "Right Control \u2303"  },
                new CommandInfo { Id = "#modifier/right-cmd",    DisplayName = "Right Command \u2318"  },
                new CommandInfo { Id = "#modifier/right-option", DisplayName = "Right Option \u2325"   },
                new CommandInfo { Id = "#modifier/fn",           DisplayName = "Function (Fn)"         },
            };

            _registry = new Dictionary<string, CommandInfo>();
            foreach (var cmd in _viewCommands)     _registry[cmd.Id] = cmd;
            foreach (var cmd in _modifierCommands) _registry[cmd.Id] = cmd;
        }

        public static IReadOnlyList<CommandInfo> ViewCommands     => _viewCommands;
        public static IReadOnlyList<CommandInfo> ModifierCommands => _modifierCommands;

        public static bool TryGetDisplayName(string id, out string displayName)
        {
            if (_registry.TryGetValue(id, out var info))
            {
                displayName = info.DisplayName;
                return true;
            }
            displayName = null;
            return false;
        }

        public static bool IsModifier(string id) =>
            id != null && id.StartsWith("#modifier/", StringComparison.Ordinal);

        // ── SceneView commands ────────────────────────────────────────────────

        /// <summary>Execute a built-in "#" command on button press.</summary>
        public static void Execute(string id)
        {
            if (IsModifier(id))
            {
                if (_tapModifiers.Contains(id))
                    PostModifierKey(id, keyDown: true);   // tap fires down+up immediately
                else
                    PostModifierKey(id, keyDown: true);
                return;
            }

            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null) return;

            Vector3 pivot = sv.pivot;
            float   size  = sv.size;

            switch (id)
            {
                case "#frame/selected":
                    sv.FrameSelected();
                    break;
                case "#rotation/toggle-lock":
                    SpaceMouseSettings.RotationLocked = !SpaceMouseSettings.RotationLocked;
                    break;
                case "#view/top":
                    sv.LookAt(pivot, Quaternion.Euler(90f, 0f, 0f), size, true, true);
                    break;
                case "#view/front":
                    sv.LookAt(pivot, Quaternion.Euler(0f, 180f, 0f), size, true, true);
                    break;
                case "#view/right":
                    sv.LookAt(pivot, Quaternion.Euler(0f, 90f, 0f), size, true, true);
                    break;
                case "#view/back":
                    sv.LookAt(pivot, Quaternion.Euler(0f, 0f, 0f), size, true, true);
                    break;
                case "#view/left":
                    sv.LookAt(pivot, Quaternion.Euler(0f, -90f, 0f), size, true, true);
                    break;
                case "#view/bottom":
                    sv.LookAt(pivot, Quaternion.Euler(-90f, 0f, 0f), size, true, true);
                    break;
                case "#view/perspective":
                    sv.orthographic = false;
                    break;
                case "#view/toggle-ortho":
                    sv.orthographic = !sv.orthographic;
                    break;
                case "#view/roll+90":
                {
                    Vector3 fwd = sv.rotation * Vector3.forward;
                    sv.rotation = Quaternion.AngleAxis(90f, fwd) * sv.rotation;
                    break;
                }
                case "#view/roll-90":
                {
                    Vector3 fwd = sv.rotation * Vector3.forward;
                    sv.rotation = Quaternion.AngleAxis(-90f, fwd) * sv.rotation;
                    break;
                }
                default:
                    Debug.LogWarning($"[SpaceMouse] Unknown built-in command: \"{id}\"");
                    break;
            }
        }

        /// <summary>Called on button release. Sends key-up for held modifiers; no-op for taps.</summary>
        public static void ExecuteRelease(string id)
        {
            if (IsModifier(id) && !_tapModifiers.Contains(id))
                PostModifierKey(id, keyDown: false);
        }

        // ── Shortcut simulation (@shortcutId) ─────────────────────────────────

        /// <summary>
        /// Look up the ShortcutManager binding for <paramref name="shortcutId"/> and
        /// simulate a key tap via CGEvent so Unity's shortcut system handles it.
        /// </summary>
        public static void ExecuteShortcut(string shortcutId)
        {
            ShortcutBinding binding;
            try
            {
                binding = ShortcutManager.instance.GetShortcutBinding(shortcutId);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpaceMouse] Cannot get binding for '{shortcutId}': {e.Message}");
                return;
            }

            KeyCombination? combo = null;
            foreach (var kc in binding.keyCombinationSequence)
            {
                combo = kc;
                break;
            }

            if (!combo.HasValue)
            {
                Debug.LogWarning($"[SpaceMouse] Shortcut '{shortcutId}' has no key binding assigned.");
                return;
            }

            if (!_unityKeyToVK.TryGetValue(combo.Value.keyCode, out ushort vk))
            {
                Debug.LogWarning($"[SpaceMouse] No macOS VK mapping for {combo.Value.keyCode} (shortcut '{shortcutId}').");
                return;
            }

            PostKeyTap(vk, ShortcutModifiersToFlags(combo.Value.modifiers));
        }

        // ── macOS CGEvent ─────────────────────────────────────────────────────

        const int kCGHIDEventTap = 0;

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey,
            [MarshalAs(UnmanagedType.I1)] bool keyDown);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        static extern void CGEventSetFlags(IntPtr @event, ulong flags);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        static extern void CGEventPost(int tap, IntPtr @event);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        static extern void CFRelease(IntPtr cf);

        // Virtual key codes from Carbon HIToolbox/Events.h
        static readonly Dictionary<string, ushort> _modifierKeyCodes = new()
        {
            { "#modifier/escape",       0x35 },  // kVK_Escape
            { "#modifier/shift",        0x38 },  // kVK_Shift
            { "#modifier/ctrl",         0x3B },  // kVK_Control
            { "#modifier/cmd",          0x37 },  // kVK_Command
            { "#modifier/option",       0x3A },  // kVK_Option
            { "#modifier/caps-lock",    0x39 },  // kVK_CapsLock
            { "#modifier/right-shift",  0x3C },  // kVK_RightShift
            { "#modifier/right-ctrl",   0x3E },  // kVK_RightControl
            { "#modifier/right-cmd",    0x36 },  // kVK_RightCommand
            { "#modifier/right-option", 0x3D },  // kVK_RightOption
            { "#modifier/fn",           0x3F },  // kVK_Function
        };

        static void PostModifierKey(string id, bool keyDown)
        {
            if (!_modifierKeyCodes.TryGetValue(id, out ushort vk)) return;

            if (_tapModifiers.Contains(id))
            {
                if (!keyDown) return;   // tap only fires on the down edge
                PostKeyTap(vk, 0);
                return;
            }

            try
            {
                IntPtr evt = CGEventCreateKeyboardEvent(IntPtr.Zero, vk, keyDown);
                if (evt == IntPtr.Zero) return;
                CGEventPost(kCGHIDEventTap, evt);
                CFRelease(evt);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpaceMouse] Failed to post modifier event: {e.Message}");
            }
        }

        static void PostKeyTap(ushort vk, ulong flags)
        {
            try
            {
                IntPtr down = CGEventCreateKeyboardEvent(IntPtr.Zero, vk, true);
                if (down != IntPtr.Zero)
                {
                    if (flags != 0) CGEventSetFlags(down, flags);
                    CGEventPost(kCGHIDEventTap, down);
                    CFRelease(down);
                }
                IntPtr up = CGEventCreateKeyboardEvent(IntPtr.Zero, vk, false);
                if (up != IntPtr.Zero)
                {
                    CGEventPost(kCGHIDEventTap, up);
                    CFRelease(up);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpaceMouse] Failed to post key tap: {e.Message}");
            }
        }

        static ulong ShortcutModifiersToFlags(ShortcutModifiers mods)
        {
            ulong f = 0;
            if ((mods & ShortcutModifiers.Shift)   != 0) f |= 0x00020000; // kCGEventFlagMaskShift
            if ((mods & ShortcutModifiers.Control) != 0) f |= 0x00040000; // kCGEventFlagMaskControl
            if ((mods & ShortcutModifiers.Alt)     != 0) f |= 0x00080000; // kCGEventFlagMaskAlternate
            if ((mods & ShortcutModifiers.Action)  != 0) f |= 0x00100000; // kCGEventFlagMaskCommand
            return f;
        }

        // Unity KeyCode → macOS virtual key code (kVK_*) mapping
        // Source: Carbon HIToolbox/Events.h
        static readonly Dictionary<KeyCode, ushort> _unityKeyToVK = new()
        {
            // Letters (US QWERTY physical positions)
            { KeyCode.A, 0x00 }, { KeyCode.S, 0x01 }, { KeyCode.D, 0x02 },
            { KeyCode.F, 0x03 }, { KeyCode.H, 0x04 }, { KeyCode.G, 0x05 },
            { KeyCode.Z, 0x06 }, { KeyCode.X, 0x07 }, { KeyCode.C, 0x08 },
            { KeyCode.V, 0x09 }, { KeyCode.B, 0x0B }, { KeyCode.Q, 0x0C },
            { KeyCode.W, 0x0D }, { KeyCode.E, 0x0E }, { KeyCode.R, 0x0F },
            { KeyCode.Y, 0x10 }, { KeyCode.T, 0x11 },
            { KeyCode.O, 0x1F }, { KeyCode.U, 0x20 }, { KeyCode.I, 0x22 },
            { KeyCode.P, 0x23 }, { KeyCode.L, 0x25 }, { KeyCode.J, 0x26 },
            { KeyCode.K, 0x28 }, { KeyCode.N, 0x2D }, { KeyCode.M, 0x2E },
            // Number row
            { KeyCode.Alpha1, 0x12 }, { KeyCode.Alpha2, 0x13 }, { KeyCode.Alpha3, 0x14 },
            { KeyCode.Alpha4, 0x15 }, { KeyCode.Alpha5, 0x17 }, { KeyCode.Alpha6, 0x16 },
            { KeyCode.Alpha7, 0x1A }, { KeyCode.Alpha8, 0x1C }, { KeyCode.Alpha9, 0x19 },
            { KeyCode.Alpha0, 0x1D },
            // Punctuation
            { KeyCode.Minus,        0x1B }, { KeyCode.Equals,       0x18 },
            { KeyCode.LeftBracket,  0x21 }, { KeyCode.RightBracket, 0x1E },
            { KeyCode.Backslash,    0x2A }, { KeyCode.Semicolon,    0x29 },
            { KeyCode.Quote,        0x27 }, { KeyCode.BackQuote,    0x32 },
            { KeyCode.Comma,        0x2B }, { KeyCode.Period,       0x2F },
            { KeyCode.Slash,        0x2C },
            // Control keys
            { KeyCode.Return,    0x24 }, { KeyCode.Tab,      0x30 },
            { KeyCode.Space,     0x31 }, { KeyCode.Backspace, 0x33 },
            { KeyCode.Escape,    0x35 }, { KeyCode.Delete,   0x75 },
            // Navigation
            { KeyCode.Home,      0x73 }, { KeyCode.End,      0x77 },
            { KeyCode.PageUp,    0x74 }, { KeyCode.PageDown, 0x79 },
            { KeyCode.UpArrow,   0x7E }, { KeyCode.DownArrow,  0x7D },
            { KeyCode.LeftArrow, 0x7B }, { KeyCode.RightArrow, 0x7C },
            // Function keys
            { KeyCode.F1,  0x7A }, { KeyCode.F2,  0x78 }, { KeyCode.F3,  0x63 },
            { KeyCode.F4,  0x76 }, { KeyCode.F5,  0x60 }, { KeyCode.F6,  0x61 },
            { KeyCode.F7,  0x62 }, { KeyCode.F8,  0x64 }, { KeyCode.F9,  0x65 },
            { KeyCode.F10, 0x6D }, { KeyCode.F11, 0x67 }, { KeyCode.F12, 0x6F },
            { KeyCode.F13, 0x69 }, { KeyCode.F14, 0x6B }, { KeyCode.F15, 0x71 },
            // Numpad
            { KeyCode.Keypad0,        0x52 }, { KeyCode.Keypad1,    0x53 },
            { KeyCode.Keypad2,        0x54 }, { KeyCode.Keypad3,    0x55 },
            { KeyCode.Keypad4,        0x56 }, { KeyCode.Keypad5,    0x57 },
            { KeyCode.Keypad6,        0x58 }, { KeyCode.Keypad7,    0x59 },
            { KeyCode.Keypad8,        0x5B }, { KeyCode.Keypad9,    0x5C },
            { KeyCode.KeypadPeriod,   0x41 }, { KeyCode.KeypadDivide,   0x4B },
            { KeyCode.KeypadMultiply, 0x43 }, { KeyCode.KeypadMinus,    0x4E },
            { KeyCode.KeypadPlus,     0x45 }, { KeyCode.KeypadEnter,    0x4C },
            { KeyCode.KeypadEquals,   0x51 },
        };
    }
}
