using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class CleanupRagePropFromLargeFurniture
{
    private static readonly string[] LARGE_FURNITURE_AND_CEILING_LIGHTS = 
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

    [MenuItem("Office Flip Out/Remove RageFlipOutProp from Large Furniture & Ceiling Lights")]
    public static void RemoveRagePropFromLargeFurniture()
    {
        string scenePath = "Assets/Scenes/OfficeScene.unity";
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        int removedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            removedCount += ProcessGameObject(obj);
        }

        Debug.Log($"[Cleanup] Removed RageFlipOutProp from {removedCount} large furniture items and ceiling lights.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static int ProcessGameObject(GameObject obj)
    {
        int count = 0;

        // Check if this object is large furniture or ceiling light
        foreach (string largeItem in LARGE_FURNITURE_AND_CEILING_LIGHTS)
        {
            if (obj.name.Contains(largeItem))
            {
                // Remove RageFlipOutProp script
                RageFlipOutProp rageProp = obj.GetComponent<RageFlipOutProp>();
                if (rageProp != null)
                {
                    Object.DestroyImmediate(rageProp);
                    Debug.Log($"Removed RageFlipOutProp from: {obj.name}");
                    count++;
                }

                // Optionally remove Rigidbody too (if you don't want physics on these)
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Object.DestroyImmediate(rb);
                    Debug.Log($"Removed Rigidbody from: {obj.name}");
                }
                
                break;
            }
        }

        // Recursively process children
        foreach (Transform child in obj.transform)
        {
            count += ProcessGameObject(child.gameObject);
        }

        return count;
    }
}
