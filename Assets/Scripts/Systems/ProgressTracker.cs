using System;
using System.Collections.Generic;
using UnityEngine;

namespace OfficeFlipOut.Systems
{
    public class ProgressTracker : MonoBehaviour
    {
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

            public int CompletedTasksCount
            {
                get
                {
                    int completed = 0;
                    if (spilledDrink) completed++;
                    if (microwavedFish) completed++;
                    if (tookStapler) completed++;
                    return completed;
                }
            }
        }

        public static ProgressTracker Instance { get; private set; }

        [Header("Discovery")]
        [SerializeField] private bool autoFindRageMeters = true;
        [SerializeField, Min(0.1f)] private float refreshInterval = 0.25f;
        [SerializeField] private List<Rage_Meter> trackedMeters = new List<Rage_Meter>();

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

            int totalTasks = snapshots.Count * 3;
            int complete = 0;
            for (int i = 0; i < snapshots.Count; i++)
            {
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

            if (!target.spilledDrink)
            {
                nextSuggestedAction = "Spill a drink near their desk area.";
                return "Raise " + target.displayName + " rage by spilling a drink.";
            }

            if (!target.microwavedFish)
            {
                nextSuggestedAction = "Microwave fish while they are nearby.";
                return "Raise " + target.displayName + " rage with microwave fish.";
            }

            if (!target.tookStapler)
            {
                nextSuggestedAction = "Steal the stapler from their desk.";
                return "Raise " + target.displayName + " rage by taking the stapler.";
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
                    if (snapshots[i].npcId == focusNpcId && snapshots[i].CompletedTasksCount < 3)
                    {
                        return snapshots[i];
                    }
                }
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].CompletedTasksCount < 3)
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
                snapshot.displayName = meter.name;
                snapshot.currentRage = meter.CurrentRage;
                snapshot.requiredSignals = meter.RequiredSignals;
                snapshot.isFlippedOut = meter.IsFlippedOut;
                snapshot.spilledDrink = meter.HasReceivedSignal(RageSignalIds.SpillDrinkOnDesk);
                snapshot.microwavedFish = meter.HasReceivedSignal(RageSignalIds.MicrowaveFish);
                snapshot.tookStapler = meter.HasReceivedSignal(RageSignalIds.TakeStaplerFromDesk);
                snapshot.rageFaceSprite = meter.CurrentRageFaceSprite;
                snapshots.Add(snapshot);
            }

            ProgressChanged?.Invoke();
        }
    }
}
