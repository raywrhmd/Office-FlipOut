using OfficeFlipOut.Data;
using UnityEngine;

namespace OfficeFlipOut.Systems
{
    public static class ProjectBootstrap
    {
        public static T FindFirst<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        public static EmployeeProfileDatabase LoadEmployeeProfileDatabase(bool createIfMissing)
        {
            EmployeeProfileDatabase database = Resources.Load<EmployeeProfileDatabase>("EmployeeProfileDatabase");
            if (database != null || !createIfMissing)
            {
                return database;
            }

            database = ScriptableObject.CreateInstance<EmployeeProfileDatabase>();
            database.hideFlags = HideFlags.DontSave;
            return database;
        }
    }
}