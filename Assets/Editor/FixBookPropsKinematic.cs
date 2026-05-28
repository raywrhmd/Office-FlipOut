using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixBookPropsKinematic
{
    private static readonly string[] BOOK_PREFAB_NAMES = 
    {
        "(Prb)BookPile",
        "(Prb)BookOpen",
        "pile_of_books"
    };

    [MenuItem("Office Flip Out/Fix Book Props - Add KinematicAtStart")]
    public static void AddKinematicAtStartToBooks()
    {
        string scenePath = "Assets/Scenes/OfficeScene.unity";
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        int addedCount = 0;
        int skippedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            var result = ProcessGameObject(obj);
            addedCount += result.added;
            skippedCount += result.skipped;
        }

        Debug.Log($"[Book Props Fix] Added KinematicAtStart to {addedCount} book props. Skipped {skippedCount} (already had component).");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static (int added, int skipped) ProcessGameObject(GameObject obj)
    {
        int added = 0;
        int skipped = 0;

        // Check if this object is a book prefab
        foreach (string bookName in BOOK_PREFAB_NAMES)
        {
            if (obj.name.Contains(bookName))
            {
                // Check if it already has KinematicAtStart
                KinematicAtStart existing = obj.GetComponent<KinematicAtStart>();
                if (existing != null)
                {
                    skipped++;
                }
                else
                {
                    // Add KinematicAtStart component
                    KinematicAtStart kinematicAtStart = obj.AddComponent<KinematicAtStart>();
                    Debug.Log($"Added KinematicAtStart to: {obj.name}");
                    added++;
                }

                break;
            }
        }

        // Recursively process children
        foreach (Transform child in obj.transform)
        {
            var result = ProcessGameObject(child.gameObject);
            added += result.added;
            skipped += result.skipped;
        }

        return (added, skipped);
    }
}
