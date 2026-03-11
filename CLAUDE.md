# SpaceMouse Pro Driver — Claude Instructions

## Project Overview

Unity 6 (6000.3) Editor plugin that enables SpaceMouse Pro 6-DoF navigation in the Unity Scene View on macOS. Editor-only — not a game/runtime plugin. Targets macOS with 3DxWare 10 installed.

Architecture: native C dylib (polling) → C# P/Invoke → SceneView camera control. Polling avoids Apple Silicon + Mono native callback issues.

Published via UPM at: `https://github.com/figgy78/Spacemouse-pro-for-unity-on-macos.git#upm`

## Deployment Checklist

Whenever deploying / releasing changes, always do **both** of the following:

1. **Bump the version** in `package.json` on the `upm` branch (follow SemVer: MINOR for new features, PATCH for bug fixes).
2. **Sync the `upm` branch** with the latest code from `main`:
   - New/changed Editor CS files: `Assets/SpaceMousePro/Editor/` → `Editor/`
   - README and images if changed
   - **Keep `.meta` files in sync**: every file/folder in the package must have a corresponding `.meta` file or Unity will log warnings and ignore the asset. When adding new files or folders, generate and commit their `.meta` files.
   - Commit and push `upm`

Workflow:
```bash
git checkout upm
# copy changed files from main, e.g.:
git show origin/main:Assets/SpaceMousePro/Editor/Foo.cs > Editor/Foo.cs
git checkout origin/main -- README.md images/
# bump version in package.json, then:
git add -p && git commit -m "Sync vX.Y.Z from main" && git push origin upm
git checkout main
```

## Unity Version

Unity 6 (6000.3), macOS only, Apple Silicon + x86_64 universal binary.

---

## Project Structure

```
NativePlugin/
├── SpaceMouseBridge.c      # Thin C wrapper around 3DconnexionClient.framework
└── build.sh                # clang compile script → outputs universal dylib

Assets/
├── Plugins/macOS/
│   └── libSpaceMouseBridge.dylib   # Universal arm64+x86_64 (Editor-only meta)
└── SpaceMousePro/Editor/
    ├── SpaceMouseProEditor.asmdef
    ├── SpaceMouseDevice.cs          # P/Invoke + axis normalization (÷500)
    ├── SpaceMouseSettings.cs        # EditorPrefs, dead zone, invert flags
    ├── SpaceMouseController.cs      # EditorApplication.update → SceneView camera
    └── SpaceMouseSettingsProvider.cs # Edit > Preferences > SpaceMouse Pro

3DxMacWare SDK/                     # Official 3Dconnexion SDK (reference only)
├── 3DxMacWare SDK.pdf
└── 3DxSampleCode/
```

## SDK Reference

The folder `3DxMacWare SDK/` contains the official 3Dconnexion SDK for macOS:

- `3DxMacWare SDK.pdf` — full API reference (framework functions, structs, constants)
- `3DxSampleCode/Connexion Client Test/` — reference app using the newer `SetConnexionHandlers` API (matches our driver)
- `3DxSampleCode/3DxMultithreadedValues/` — multithreaded polling example
- `3DxSampleCode/3DxDemo/` and `3DxSNAxisDemo/` — additional usage examples

Consult this folder when working on native plugin changes, adding new SDK features (button labels, prefs pane integration, device switch modes), or verifying API usage.

## Assembly Definitions (.asmdef)

Always use `.asmdef` files — they control compilation scope and prevent accidental cross-references.

- **Runtime asmdef**: no platform restrictions
- **Editor asmdef**: set `includePlatforms: ["Editor"]` and reference the Runtime asmdef
- **Test asmdefs**: reference `UnityEngine.TestRunner` and `UnityEditor.TestRunner`; use `[assembly: InternalsVisibleTo("YourPlugin.Tests.EditMode")]` in Runtime if tests need internal access

---

## C# Conventions

### Fields & Serialization
- Use `[SerializeField] private` instead of `public` for inspector-exposed fields
- Use `[field: SerializeField]` for auto-properties when needed
- Avoid public fields unless the type is a struct used as a value container

### Unity Lifecycle
Execution order: `Awake` → `OnEnable` → `Start` → `FixedUpdate` → `Update` → `LateUpdate` → `OnDisable` → `OnDestroy`

- Cache `GetComponent<T>()` in `Awake`, never call in `Update`
- Initialize in `Awake`, resolve cross-component references in `Start`
- Clean up subscriptions and allocated resources in `OnDestroy`

### Object Creation
- **Never** use `new MonoBehaviour()` or `new ScriptableObject()`
- Use `gameObject.AddComponent<T>()` for MonoBehaviours
- Use `ScriptableObject.CreateInstance<T>()` for ScriptableObjects
- Use `Object.Destroy()` / `Object.DestroyImmediate()` (Editor only) to remove objects

### Async & Destroyed Objects
Unity overloads `==` for destroyed objects — always check `if (this == null) return;` at the start of async continuations targeting MonoBehaviours.

---

## Editor Code Patterns

### Inspector & Property Drawers
- `[CustomEditor(typeof(MyComponent))]` + override `OnInspectorGUI()`
- `[CustomPropertyDrawer(typeof(MyAttribute))]` for reusable field rendering
- Always call `serializedObject.Update()` at the start and `serializedObject.ApplyModifiedProperties()` at the end of `OnInspectorGUI`

### Undo & Dirty Marking
```csharp
Undo.RecordObject(target, "Change Value");   // before modifying
EditorUtility.SetDirty(target);              // after modifying non-serialized assets
AssetDatabase.SaveAssets();                  // persist ScriptableObject changes
```

### Asset Database
- Call `AssetDatabase.Refresh()` after programmatically creating or modifying assets on disk
- Use `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()` to batch imports

### Editor Windows
- Inherit from `EditorWindow`, use `[MenuItem("Tools/YourPlugin/...")]` to register
- Use `GUILayout`/`EditorGUILayout` for simple UI; `UIToolkit` (UXML/USS) for complex or reusable UI

---

## Common Pitfalls

| Issue | Fix |
|-------|-----|
| `MissingReferenceException` in async callbacks | Check `if (obj == null) return;` before use |
| Asset changes not persisting | Call `EditorUtility.SetDirty()` + `AssetDatabase.SaveAssets()` |
| Editor code in builds | Keep all `UnityEditor` imports inside `#if UNITY_EDITOR` or Editor assembly |
| Slow inspector | Cache `SerializedProperty` references in `OnEnable`, not `OnInspectorGUI` |
| Undo not working | Call `Undo.RecordObject()` *before* making changes |
| Compilation errors on non-Editor platforms | Move Editor-only code to `Editor/` folder or behind `#if UNITY_EDITOR` |

---

## Platform Defines

```csharp
#if UNITY_EDITOR
    // Editor-only code
#endif

#if UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID
    // Platform-specific code
#endif
```

---

## Testing

Run tests via **Window > General > Test Runner**.

- **EditMode tests**: Fastest. Use for pure logic, ScriptableObjects, asset operations.
- **PlayMode tests**: Full scene/component lifecycle. Use for MonoBehaviour interactions.

```csharp
// EditMode example
[TestFixture]
public class MySystemTests
{
    [Test]
    public void DoesExpectedThing()
    {
        var so = ScriptableObject.CreateInstance<MyData>();
        Assert.That(so.Value, Is.EqualTo(0));
        Object.DestroyImmediate(so);
    }
}
```

Always clean up created objects (`Object.DestroyImmediate`) and loaded assets in tests to avoid polluting the test environment.

---

## Performance Guidelines

- **Avoid allocations in hot paths** (`Update`, `OnGUI`): cache collections, avoid LINQ, use `StringBuilder` for string ops
- **Use Jobs + Burst** for CPU-intensive work: `IJob`, `IJobParallelFor` with `NativeArray<T>`
- **Profile first**: Unity Profiler → CPU Usage, before optimizing anything
- **`WaitForSeconds`**: cache as a field — `new WaitForSeconds()` allocates every call

---

## UPM Package Manifest (`package.json`)

```json
{
  "name": "com.yourname.pluginname",
  "version": "1.0.0",
  "displayName": "Your Plugin Name",
  "description": "Short description.",
  "unity": "2022.3",
  "author": {
    "name": "Your Name",
    "email": "you@example.com"
  },
  "dependencies": {}
}
```

Follow **SemVer**: `MAJOR.MINOR.PATCH`. Bump MAJOR for breaking changes, MINOR for new features, PATCH for bug fixes.

---

## Key APIs Reference

| Need | API |
|------|-----|
| Custom inspector UI | `Editor`, `EditorGUILayout`, `UIToolkit` |
| Custom window | `EditorWindow.GetWindow<T>()` |
| Asset creation | `AssetDatabase.CreateAsset()` |
| Find assets by type | `AssetDatabase.FindAssets("t:MyType")` |
| Progress dialog | `EditorUtility.DisplayProgressBar()` |
| Serialized field access | `SerializedObject`, `SerializedProperty` |
| Gizmos in scene | `OnDrawGizmos()`, `Handles` (Editor only) |
