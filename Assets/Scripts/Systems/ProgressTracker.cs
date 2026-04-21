using System;
using System.Collections.Generic;
using OfficeFlipOut.Data;
using UnityEngine;

namespace OfficeFlipOut.Systems
{
    public class ProgressTracker : MonoBehaviour
    {
        private const string BrutusNpcId = "npc_2";

        [Serializable]
        public class EmployeeProgressSnapshot
        {
            public string npcId;
            public string displayName;
            public int currentRage;
            public int requiredSignals;
            public bool isFlippedOut;
            public bool spilledDrink;
            public bool microwavedFish;
            public bool tookStapler;
            public Sprite rageFaceSprite;

            public int TotalTasksCount
            {
                get
                {
                    if (npcId == BrutusNpcId)
                    {
                        return Mathf.Clamp(requiredSignals, 1, 2);
                    }

                    return Mathf.Clamp(requiredSignals, 1, 3);
                }
            }

            public int CompletedTasksCount
            {
                get
                {
                    int completed = 0;

                    if (npcId == BrutusNpcId)
                    {
                        if (TotalTasksCount >= 1 && microwavedFish) completed++;
                        if (TotalTasksCount >= 2 && tookStapler) completed++;
                        return completed;
                    }

                    if (TotalTasksCount >= 1 && spilledDrink) completed++;
                    if (TotalTasksCount >= 2 && microwavedFish) completed++;
                    if (TotalTasksCount >= 3 && tookStapler) completed++;
                    return completed;
                }
            }
        }

        public static ProgressTracker Instance { get; private set; }

        [Header("Discovery")]
        [SerializeField] private bool autoFindRageMeters = true;
        [SerializeField, Min(0.1f)] private float refreshInterval = 0.25f;
        [SerializeField] private List<Rage_Meter> trackedMeters = new List<Rage_Meter>();

        [Header("Display names")]
        [Tooltip("Optional. Used to populate snapshot display names by npc id (falls back to meter GameObject name).")]
        [SerializeField] private EmployeeProfileDatabase employeeProfileDatabase;

        private readonly List<EmployeeProgressSnapshot> snapshots = new List<EmployeeProgressSnapshot>();
        private float refreshTimer;

        public event Action ProgressChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (employeeProfileDatabase == null)
            {
                employeeProfileDatabase = Resources.Load<EmployeeProfileDatabase>("EmployeeProfileDatabase");
            }

            RebuildTrackedMeters();
            RefreshSnapshots();
        }

        private void OnEnable()
        {
            RageSignalHub.SignalRaised += HandleSignalRaised;
        }

        private void OnDisable()
        {
            RageSignalHub.SignalRaised -= HandleSignalRaised;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer < refreshInterval)
            {
                return;
            }

            refreshTimer = 0f;
            RefreshSnapshots();
        }

        public void RebuildTrackedMeters()
        {
            if (!autoFindRageMeters)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            Rage_Meter[] foundMeters = FindObjectsByType<Rage_Meter>(FindObjectsSortMode.None);
#else
            Rage_Meter[] foundMeters = FindObjectsOfType<Rage_Meter>();
#endif
            trackedMeters.Clear();
            for (int i = 0; i < foundMeters.Length; i++)
            {
                if (foundMeters[i] != null)
                {
                    trackedMeters.Add(foundMeters[i]);
                }
            }
        }

        public IReadOnlyList<EmployeeProgressSnapshot> GetSnapshots()
        {
            return snapshots;
        }

        public int GetFlipOutCount()
        {
            int count = 0;
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].isFlippedOut)
                {
                    count++;
                }
            }

            return count;
        }

        public float GetOverallCompletion01()
        {
            if (snapshots.Count == 0)
            {
                return 0f;
            }

            int totalTasks = 0;
            int complete = 0;
            for (int i = 0; i < snapshots.Count; i++)
            {
                totalTasks += snapshots[i].TotalTasksCount;
                complete += snapshots[i].CompletedTasksCount;
            }

            return Mathf.Clamp01((float)complete / Mathf.Max(1, totalTasks));
        }

        public float GetJobSecurity01()
        {
            if (snapshots.Count == 0)
            {
                return 0f;
            }

            float flipRatio = (float)GetFlipOutCount() / Mathf.Max(1, snapshots.Count);
            float taskRatio = GetOverallCompletion01();
            return Mathf.Clamp01((flipRatio * 0.75f) + (taskRatio * 0.25f));
        }

        public bool AreAllCoworkersFlipped(string excludedNpcId)
        {
            if (snapshots.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                EmployeeProgressSnapshot snapshot = snapshots[i];
                if (!string.IsNullOrWhiteSpace(excludedNpcId) && snapshot.npcId == excludedNpcId)
                {
                    continue;
                }

                if (!snapshot.isFlippedOut)
                {
                    return false;
                }
            }

            return true;
        }

        public string GetObjectiveText(string focusNpcId, out string nextSuggestedAction)
        {
            EmployeeProgressSnapshot target = FindFirstIncompleteSnapshot(focusNpcId);
            if (target == null)
            {
                nextSuggestedAction = "Open Pause tab when ready to regroup.";
                return "All known coworkers are fully destabilized.";
            }

            if (target.npcId == BrutusNpcId)
            {
                if (target.TotalTasksCount >= 1 && !target.microwavedFish)
                {
                    nextSuggestedAction = "Bring birthday cake near him.";
                    return "Raise " + target.displayName + " rage with birthday cake.";
                }

                if (target.TotalTasksCount >= 2 && !target.tookStapler)
                {
                    nextSuggestedAction = "Knock over his filing cabinet.";
                    return "Raise " + target.displayName + " rage by ruining his filing cabinet order.";
                }
            }

            if (target.TotalTasksCount >= 1 && !target.spilledDrink)
            {
                nextSuggestedAction = "Spill a drink near their desk area.";
                return "Raise " + target.displayName + " rage by spilling a drink.";
            }

            if (target.TotalTasksCount >= 2 && !target.microwavedFish)
            {
                nextSuggestedAction = "Microwave fish while they are nearby.";
                return "Raise " + target.displayName + " rage with microwave fish.";
            }

            if (target.TotalTasksCount >= 3 && !target.tookStapler)
            {
                nextSuggestedAction = "Steal one of their desk objects.";
                return "Raise " + target.displayName + " rage by stealing an object.";
            }

            nextSuggestedAction = "Push their rage to full with remaining interactions.";
            return "Finish forcing " + target.displayName + " to FLIP OUT.";
        }

        private EmployeeProgressSnapshot FindFirstIncompleteSnapshot(string focusNpcId)
        {
            if (!string.IsNullOrWhiteSpace(focusNpcId))
            {
                for (int i = 0; i < snapshots.Count; i++)
                {
                    if (snapshots[i].npcId == focusNpcId &&
                        snapshots[i].CompletedTasksCount < snapshots[i].TotalTasksCount)
                    {
                        return snapshots[i];
                    }
                }
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].CompletedTasksCount < snapshots[i].TotalTasksCount)
                {
                    return snapshots[i];
                }
            }

            return null;
        }

        private void HandleSignalRaised(string signalId, string targetNpcId)
        {
            RefreshSnapshots();
        }

        private void RefreshSnapshots()
        {
            if (autoFindRageMeters && (trackedMeters == null || trackedMeters.Count == 0))
            {
                RebuildTrackedMeters();
            }

            snapshots.Clear();

            for (int i = 0; i < trackedMeters.Count; i++)
            {
                Rage_Meter meter = trackedMeters[i];
                if (meter == null)
                {
                    continue;
                }

                EmployeeProgressSnapshot snapshot = new EmployeeProgressSnapshot();
                snapshot.npcId = string.IsNullOrWhiteSpace(meter.NpcSignalId) ? meter.name : meter.NpcSignalId;
                snapshot.displayName = ResolveDisplayName(snapshot.npcId, meter.name);
                snapshot.currentRage = meter.CurrentRage;
                snapshot.requiredSignals = meter.RequiredSignals;
                snapshot.isFlippedOut = meter.IsFlippedOut;
                snapshot.spilledDrink = meter.HasReceivedSignal(RageSignalIds.SpillDrinkOnDesk);
                if (snapshot.npcId == BrutusNpcId)
                {
                    snapshot.microwavedFish = meter.HasReceivedSignal(RageSignalIds.BringObjectNear);
                    snapshot.tookStapler = meter.HasReceivedSignal(RageSignalIds.KnockOver);
                }
                else
                {
                    snapshot.microwavedFish = meter.HasReceivedSignal(RageSignalIds.MicrowaveFish);
                    snapshot.tookStapler = meter.HasReceivedSignal(RageSignalIds.StealObject);
                }
                snapshot.rageFaceSprite = meter.CurrentRageFaceSprite;
                snapshots.Add(snapshot);
            }

            ProgressChanged?.Invoke();
        }

        private string ResolveDisplayName(string npcId, string meterObjectName)
        {
            if (employeeProfileDatabase != null && !string.IsNullOrWhiteSpace(npcId))
            {
                EmployeeProfileData profile = employeeProfileDatabase.GetProfileByNpcId(npcId);
                if (profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName))
                {
                    return profile.DisplayName;
                }
            }

            return meterObjectName;
        }
    }
}
