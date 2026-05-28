using OfficeFlipOut.Systems;
using OfficeFlipOut.UI.Hud;
using OfficeFlipOut.UI.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace OfficeFlipOut.UI
{
    /// <summary>
    /// Single source of truth for spawning the persistent UI hierarchy.
    /// Runs after every scene load and creates any missing UI controllers
    /// under a shared "UIRoot" GameObject.
    ///
    /// All controllers stay scene-scoped (NOT DontDestroyOnLoad) so they
    /// re-bind to per-scene systems like ProgressTracker on reload. The
    /// "persistent root" referenced in the audit plan refers to this
    /// consolidated bootstrap, not to DontDestroyOnLoad lifetimes (which
    /// would require deeper rebind plumbing and is deferred).
    /// </summary>
    public static class UIRootBootstrap
    {
        private const string RootName = "UIRoot";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Need at least one PanelSettings asset somewhere in the active
            // scene (typically attached to the clipboard's UIDocument). If the
            // scene has none, we can't render Toolkit UI anyway.
            PanelSettings settings = FindAnyPanelSettingsInScene();
            if (settings == null)
            {
                return;
            }

            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
            }

            EnsureChildController<HudController>(root, settings, "Hud", -100f);
            EnsureChildController<PauseMenuController>(root, settings, "PauseMenu", 50f);
            EnsureChildController<MainMenuController>(root, settings, "MainMenu", 100f);
        }

        private static void EnsureChildController<T>(
            GameObject root,
            PanelSettings settings,
            string label,
            float sortingOffset) where T : MonoBehaviour
        {
            if (ProjectBootstrap.FindFirst<T>() != null)
            {
                return;
            }

            GameObject host = new GameObject(typeof(T).Name);
            host.transform.SetParent(root.transform, false);
            UIDocument doc = host.AddComponent<UIDocument>();
            doc.panelSettings = settings;
            doc.sortingOrder = settings.sortingOrder + sortingOffset;
            host.AddComponent<T>();
        }

        private static PanelSettings FindAnyPanelSettingsInScene()
        {
            UIDocument[] docs = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < docs.Length; i++)
            {
                if (docs[i] != null && docs[i].panelSettings != null)
                {
                    return docs[i].panelSettings;
                }
            }
            return null;
        }
    }
}
