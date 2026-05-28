using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AttachRagePropInteractToProps
{
    private static readonly string[] EXCLUDED_PREFIXES = { "wall_", "floor_", "door_", "ceiling" };
    private static readonly string[] EXCLUDED_NAMES = { "whiteboard_wall", "ceilingLight", "player" };
    
    // Large furniture and items that should NOT have physics/RageFlipOutProp
    private static readonly string[] EXCLUDED_PROPS = 
    {
        "(Prb)CeilingLight",
        "(Prb)OfficeChair",
        "(Prb)ConferenceTable",
        "(Prb)CoffeTable",
        "(Prb)Sofa3",
        "(Prb)BigDrawer",
        "(Prb)Shelves2",
        "(Prb)Shelves3",
        "(Prb)Printer",
        "(Prb)DeskPrinter",
        "(Prb)VendingMachine",
        "(Prb)WaterDispenser",
        "(Prb)KitchenModule"
    };

    [MenuItem("Office Flip Out/Attach RageFlipOutProp to All Props")]
    public static void AttachRageFlipOutPropToAllProps()
    {
        // Open the OfficeScene
        string scenePath = "Assets/Scenes/OfficeScene.unity";
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Get all root GameObjects in the scene
        GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        int attachedCount = 0;
        int skippedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            attachedCount += ProcessGameObject(obj, ref skippedCount);
        }

        Debug.Log($"[RageFlipOutProp Attachment] Attached to {attachedCount} props. Skipped {skippedCount} structural/large elements.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("OfficeScene saved with RageFlipOutProp attachments.");
    }

    private static int ProcessGameObject(GameObject obj, ref int skippedCount)
    {
        int count = 0;

        // Check if this object should be skipped (structural element or player)
        if (ShouldSkipObject(obj.name))
        {
            skippedCount++;
            // Still process children (they might be props)
            foreach (Transform child in obj.transform)
            {
                count += ProcessGameObject(child.gameObject, ref skippedCount);
            }
            return count;
        }

        // Try to attach RageFlipOutProp to this object if it's a prop
        if (IsProp(obj))
        {
            if (obj.GetComponent<RageFlipOutProp>() == null)
            {
                // Add Rigidbody if it doesn't have one (required by RageFlipOutProp)
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = obj.AddComponent<Rigidbody>();
                    Debug.Log($"Added Rigidbody to: {obj.name}");
                }

                obj.AddComponent<RageFlipOutProp>();
                Debug.Log($"Attached RageFlipOutProp to: {obj.name}");
                count++;
            }
        }

        // Recursively process children
        foreach (Transform child in obj.transform)
        {
            count += ProcessGameObject(child.gameObject, ref skippedCount);
        }

        return count;
    }

    private static bool ShouldSkipObject(string objectName)
    {
        // Skip structural elements and special objects
        foreach (string prefix in EXCLUDED_PREFIXES)
        {
            if (objectName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (string name in EXCLUDED_NAMES)
        {
            if (objectName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Skip large furniture and non-interactive props
        foreach (string excludedProp in EXCLUDED_PROPS)
        {
            if (objectName.Contains(excludedProp))
                return true;
        }

        return false;
    }

    private static bool IsProp(GameObject obj)
    {
        // Props are identified by:
        // 1. (Prb) prefix - prefab instances are interactive props (unless in exclusion list)
        // 2. Known props: sticky notes, Pizza Party Cake, Cabinet
        // 3. Child objects with physics (like mug_body inside (Prb)Mug)

        string name = obj.name;

        // Always treat (Prb) prefab instances as props (unless excluded)
        if (name.Contains("(Prb)"))
        {
            // Make sure it's not in the excluded list
            foreach (string excludedProp in EXCLUDED_PROPS)
            {
                if (name.Contains(excludedProp))
                    return false;
            }
            return true;
        }

        // Sticky notes are props
        if (name.Contains("sticky_note"))
            return true;

        // Known interactive props
        if (name == "Pizza Party Cake" || name == "Cabinet")
            return true;

        // Child objects inside props (like mug_body) if they have physics
        if (obj.GetComponent<Rigidbody>() != null && !ShouldSkipObject(name))
            return true;

        return false;
    }
}
