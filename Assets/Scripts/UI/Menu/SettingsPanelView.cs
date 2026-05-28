using OfficeFlipOut.Systems;
using OfficeFlipOut.UI.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace OfficeFlipOut.UI.Menu
{
    /// <summary>
    /// Reusable settings panel used inside PauseMenu (and later, the main menu
    /// settings screen). Builds itself programmatically so the Pause/Main menu
    /// UXMLs don't need to know about specific controls. Hooks into AudioManager.
    /// </summary>
    /// <remarks>
    /// Each row gets BOTH the legacy <c>settings-row*</c> class names AND the
    /// new <c>field-row</c> / <c>field-label</c> / <c>field-control</c> /
    /// <c>section-title</c> primitives so the panel renders identically whether
    /// it's mounted in a host that loads the legacy PauseMenu.uss or the new
    /// primitives.uss only.
    /// </remarks>
    public class SettingsPanelView
    {
        public VisualElement Root { get; }
        private readonly Slider musicSlider;
        private readonly Slider sfxSlider;
        private readonly Toggle hintsToggle;
        private readonly Toggle reduceMotionToggle;

        public SettingsPanelView()
        {
            Root = new VisualElement { name = "SettingsPanel" };
            Root.AddToClassList("settings-panel");

            // First section gets the --first modifier because Unity USS does
            // not support the :first-child pseudo-class. The marker class is
            // used by PauseMenu.uss to zero the top margin so the panel
            // doesn't open with a wasted gap above "Audio".
            AddSectionTitle("Audio", "AudioHeader", isFirst: true);

            musicSlider = AddSliderRow("Music", "MusicVolumeSlider", AudioManager.MusicVolume);
            musicSlider.RegisterValueChangedCallback(evt => AudioManager.SetMusicVolume(evt.newValue));

            sfxSlider = AddSliderRow("Sound Effects", "SfxVolumeSlider", AudioManager.SfxVolume);
            sfxSlider.RegisterValueChangedCallback(evt => AudioManager.SetSfxVolume(evt.newValue));

            AddSectionTitle("Gameplay", "GameplayHeader");

            hintsToggle = AddToggleRow("Show Interaction Hints", "HintsToggle", HudSignals.HintsEnabled);
            hintsToggle.RegisterValueChangedCallback(evt => HudSignals.SetHintsEnabled(evt.newValue));

            AddSectionTitle("Accessibility", "AccessibilityHeader");

            reduceMotionToggle = AddToggleRow("Reduce Motion", "ReduceMotionToggle", HudSignals.ReduceMotion);
            reduceMotionToggle.RegisterValueChangedCallback(evt =>
            {
                HudSignals.SetReduceMotion(evt.newValue);
                ApplyReduceMotionClassToActivePanels(evt.newValue);
            });

            // Apply current pref on first build so the project starts in the
            // right state when the player opens settings on a fresh launch.
            ApplyReduceMotionClassToActivePanels(HudSignals.ReduceMotion);
        }

        public void SetVisible(bool visible)
        {
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                musicSlider.SetValueWithoutNotify(AudioManager.MusicVolume);
                sfxSlider.SetValueWithoutNotify(AudioManager.SfxVolume);
                hintsToggle.SetValueWithoutNotify(HudSignals.HintsEnabled);
                reduceMotionToggle.SetValueWithoutNotify(HudSignals.ReduceMotion);
            }
        }

        private void AddSectionTitle(string text, string elementName, bool isFirst = false)
        {
            Label section = new Label(text) { name = elementName };
            section.AddToClassList("section-title");
            section.AddToClassList("settings-section-title");
            if (isFirst) section.AddToClassList("section-title--first");
            Root.Add(section);
        }

        private Slider AddSliderRow(string labelText, string name, float initialValue)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("field-row");
            row.AddToClassList("settings-row");

            Label label = new Label(labelText);
            label.AddToClassList("field-label");
            label.AddToClassList("settings-row-label");
            row.Add(label);

            Slider slider = new Slider(0f, 1f) { name = name, value = initialValue };
            slider.AddToClassList("field-control");
            slider.AddToClassList("settings-row-control");
            slider.showInputField = true;
            row.Add(slider);

            Root.Add(row);
            return slider;
        }

        private Toggle AddToggleRow(string labelText, string name, bool initialValue)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("field-row");
            row.AddToClassList("settings-row");

            Label label = new Label(labelText);
            label.AddToClassList("field-label");
            label.AddToClassList("settings-row-label");
            row.Add(label);

            Toggle toggle = new Toggle { name = name, value = initialValue };
            toggle.AddToClassList("field-control");
            toggle.AddToClassList("settings-row-control");
            row.Add(toggle);

            Root.Add(row);
            return toggle;
        }

        // Walk every UIDocument in the scene and toggle the .motion-reduced
        // class on its top-most panel root so transition-duration tokens
        // collapse to 0ms project-wide. Cheap, runs only on toggle change.
        private static void ApplyReduceMotionClassToActivePanels(bool enabled)
        {
            UIDocument[] docs = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < docs.Length; i++)
            {
                UIDocument doc = docs[i];
                if (doc == null || doc.rootVisualElement == null) continue;
                VisualElement root = doc.rootVisualElement;
                if (enabled) root.AddToClassList("motion-reduced");
                else root.RemoveFromClassList("motion-reduced");
            }
        }
    }
}
