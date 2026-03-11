using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SpaceMousePro
{
    /// <summary>
    /// Searchable hierarchical dropdown for picking a SpaceMouse button action.
    /// Groups:
    ///   — None —
    ///   SpaceMouse Built-in   (#view/…, #frame/…)
    ///   Modifier Keys         (#modifier/… — macOS key down/up)
    ///   Unity Menus           (Main Menu/… — ExecuteMenuItem)
    ///   Other Shortcuts       (@shortcutId  — simulate via ShortcutManager key binding)
    /// </summary>
    sealed class SpaceMouseShortcutPicker : AdvancedDropdown
    {
        // Leaf item that carries its action string directly — no index lookups needed.
        sealed class ActionItem : AdvancedDropdownItem
        {
            public readonly string Action;
            public ActionItem(string label, string action, int uniqueId) : base(label)
            {
                Action   = action;
                id       = uniqueId;
            }
        }

        public event Action<string> OnSelected;

        int _nextId;

        public SpaceMouseShortcutPicker(AdvancedDropdownState state) : base(state)
        {
            minimumSize = new Vector2(340f, 380f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            _nextId = 0;

            var root = new AdvancedDropdownItem("Action");

            // ── None ─────────────────────────────────────────────────────────
            root.AddChild(MakeItem("— None —", ""));

            // ── SpaceMouse Built-in ───────────────────────────────────────────
            var builtIn = new AdvancedDropdownItem("SpaceMouse Built-in");
            foreach (var cmd in SpaceMouseCommands.ViewCommands)
                builtIn.AddChild(MakeItem(cmd.DisplayName, cmd.Id));
            root.AddChild(builtIn);

            // ── Modifier Keys ─────────────────────────────────────────────────
            var modifiers = new AdvancedDropdownItem("Modifier Keys");
            foreach (var cmd in SpaceMouseCommands.ModifierCommands)
                modifiers.AddChild(MakeItem(cmd.DisplayName, cmd.Id));
            root.AddChild(modifiers);

            // ── Unity Menus ───────────────────────────────────────────────────
            var unityMenus = new AdvancedDropdownItem("Unity Menus");
            BuildUnityMenusGroup(unityMenus);
            root.AddChild(unityMenus);

            // ── Other Shortcuts ───────────────────────────────────────────────
            var otherGroup = new AdvancedDropdownItem("Other Shortcuts");
            BuildOtherShortcutsGroup(otherGroup);
            root.AddChild(otherGroup);

            return root;
        }

        ActionItem MakeItem(string label, string action) =>
            new ActionItem(label, action, _nextId++);

        void BuildUnityMenusGroup(AdvancedDropdownItem parent)
        {
            const string prefix = "Main Menu/";

            var ids = new List<string>();
            try
            {
                foreach (string id in ShortcutManager.instance.GetAvailableShortcutIds())
                {
                    if (id.StartsWith(prefix, StringComparison.Ordinal))
                        ids.Add(id);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpaceMouse] ShortcutManager unavailable: {e.Message}");
            }

            ids.Sort(StringComparer.Ordinal);

            // Build a two-level hierarchy: top-level menu → leaf
            // e.g. "Main Menu/Edit/Undo" → group "Edit" → leaf "Undo"
            var subGroups = new Dictionary<string, AdvancedDropdownItem>();

            foreach (string fullId in ids)
            {
                string sub  = fullId.Substring(prefix.Length); // e.g. "Edit/Undo"
                int slash   = sub.IndexOf('/');

                string groupName = slash >= 0 ? sub.Substring(0, slash) : null;
                string leafName  = slash >= 0 ? sub.Substring(slash + 1) : sub;

                AdvancedDropdownItem container;
                if (groupName == null)
                {
                    container = parent;
                }
                else
                {
                    if (!subGroups.TryGetValue(groupName, out container))
                    {
                        container = new AdvancedDropdownItem(groupName);
                        parent.AddChild(container);
                        subGroups[groupName] = container;
                    }
                }

                container.AddChild(MakeItem(leafName, fullId));
            }
        }

        void BuildOtherShortcutsGroup(AdvancedDropdownItem parent)
        {
            const string mainMenuPrefix = "Main Menu/";

            var subGroups = new Dictionary<string, AdvancedDropdownItem>();

            try
            {
                var ids = new List<string>();
                foreach (string id in ShortcutManager.instance.GetAvailableShortcutIds())
                {
                    if (!id.StartsWith(mainMenuPrefix, StringComparison.Ordinal))
                        ids.Add(id);
                }
                ids.Sort(StringComparer.Ordinal);

                foreach (string id in ids)
                {
                    int slash      = id.IndexOf('/');
                    string group   = slash >= 0 ? id.Substring(0, slash) : "General";
                    string leaf    = slash >= 0 ? id.Substring(slash + 1) : id;

                    if (!subGroups.TryGetValue(group, out var container))
                    {
                        container = new AdvancedDropdownItem(group);
                        parent.AddChild(container);
                        subGroups[group] = container;
                    }

                    // Store as "@shortcutId" so DispatchAction routes to ExecuteShortcut
                    container.AddChild(MakeItem(leaf, "@" + id));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpaceMouse] ShortcutManager unavailable: {e.Message}");
            }
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is ActionItem actionItem)
                OnSelected?.Invoke(actionItem.Action);
        }

        // ── Static display name helper ────────────────────────────────────────

        /// <summary>
        /// Returns a human-readable label for the stored action string,
        /// for display in the button row.
        /// </summary>
        public static string DisplayName(string action)
        {
            if (string.IsNullOrEmpty(action))
                return "— None —";

            if (action.StartsWith("#", StringComparison.Ordinal))
            {
                if (SpaceMouseCommands.TryGetDisplayName(action, out string name))
                    return name;
                return action;
            }

            // "@shortcutId" — strip @ and show the shortcut ID
            if (action.StartsWith("@", StringComparison.Ordinal))
                return action.Substring(1);

            const string prefix = "Main Menu/";
            if (action.StartsWith(prefix, StringComparison.Ordinal))
                return action.Substring(prefix.Length);

            return action; // legacy plain path
        }
    }
}
