using System.Collections.Generic;
using System.IO;
using OfficeFlipOut.Data;
using UnityEditor;
using UnityEngine;

public static class EmployeeProfileDatabaseGenerator
{
    private const string DatabaseAssetPath = "Assets/Resources/EmployeeProfileDatabase.asset";
    private const string ProfilesFolderPath = "Assets/Resources/EmployeeProfiles";

    [MenuItem("Office Flip Out/Generate Default Employee Data")]
    [MenuItem("Office Flip Out/Regenerate HR Clipboard NPC Data")]
    public static void RegenerateEmployeeData()
    {
        EnsureFolderExists("Assets", "Resources");
        EnsureFolderExists("Assets/Resources", "EmployeeProfiles");

        List<EmployeeProfileSeed> seeds = GddEmployeeSeeds.CreateDefaultSeeds();
        HashSet<string> expectedProfileAssetPaths = new HashSet<string>();
        List<EmployeeProfileData> generatedProfiles = new List<EmployeeProfileData>(seeds.Count);

        for (int i = 0; i < seeds.Count; i++)
        {
            EmployeeProfileSeed seed = seeds[i];
            if (seed == null)
            {
                continue;
            }

            string profileName = GetSafeFileName(!string.IsNullOrWhiteSpace(seed.NpcId) ? seed.NpcId : "Employee_" + i.ToString());
            string profileAssetPath = ProfilesFolderPath + "/" + profileName + ".asset";
            expectedProfileAssetPaths.Add(profileAssetPath);

            EmployeeProfileData profile = AssetDatabase.LoadAssetAtPath<EmployeeProfileData>(profileAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<EmployeeProfileData>();
                AssetDatabase.CreateAsset(profile, profileAssetPath);
            }

            profile.ApplySeed(seed);
            EditorUtility.SetDirty(profile);
            generatedProfiles.Add(profile);
        }

        RemoveStaleGeneratedProfiles(expectedProfileAssetPaths);

        EmployeeProfileDatabase database = AssetDatabase.LoadAssetAtPath<EmployeeProfileDatabase>(DatabaseAssetPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<EmployeeProfileDatabase>();
            AssetDatabase.CreateAsset(database, DatabaseAssetPath);
        }

        database.SetProfiles(generatedProfiles);
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = database;

        Debug.Log("[EmployeeProfileDatabaseGenerator] Regenerated HR clipboard NPC data from GDD seeds.", database);
    }

    private static void RemoveStaleGeneratedProfiles(HashSet<string> expectedProfileAssetPaths)
    {
        string[] profileGuids = AssetDatabase.FindAssets("t:EmployeeProfileData", new[] { ProfilesFolderPath });
        for (int i = 0; i < profileGuids.Length; i++)
        {
            string profileAssetPath = AssetDatabase.GUIDToAssetPath(profileGuids[i]);
            if (string.IsNullOrWhiteSpace(profileAssetPath) || expectedProfileAssetPaths.Contains(profileAssetPath))
            {
                continue;
            }

            AssetDatabase.DeleteAsset(profileAssetPath);
        }
    }

    private static void EnsureFolderExists(string parentFolder, string folderName)
    {
        string folderPath = parentFolder + "/" + folderName;
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        AssetDatabase.CreateFolder(parentFolder, folderName);
    }

    private static string GetSafeFileName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Employee";
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safeName = rawName.Trim();
        for (int i = 0; i < invalidChars.Length; i++)
        {
            safeName = safeName.Replace(invalidChars[i], '_');
        }

        return safeName;
    }
}
