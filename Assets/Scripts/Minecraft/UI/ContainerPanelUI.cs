using System.Collections.Generic;
using Minecraft.Containers;
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI
{
    /// <summary>
    /// Presents shared container inventory contents in the Unity UI by listening to ContainerManager events.
    /// </summary>
    public class ContainerPanelUI : MonoBehaviour
    {
        [Header("Data Sources")]
        [SerializeField] private ContainerManager containerManager;

        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private ContainerSlotView slotPrefab;
        [SerializeField] private Text containerTitleText;
        [SerializeField] private Text snapshotHashText;
        [SerializeField] private Text statusText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Button closeButton;

        private readonly Dictionary<int, ContainerSlotView> _slotViews = new();
        private int _activeContainerId = -1;

        private void Awake()
        {
            EnsureContainerManager();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnEnable()
        {
            EnsureContainerManager();
            if (containerManager == null)
            {
                return;
            }

            containerManager.ContainerOpened += HandleContainerOpened;
            containerManager.ContainerUpdated += HandleContainerUpdated;
            containerManager.ContainerClosed += HandleContainerClosed;
        }

        private void OnDisable()
        {
            if (containerManager != null)
            {
                containerManager.ContainerOpened -= HandleContainerOpened;
                containerManager.ContainerUpdated -= HandleContainerUpdated;
                containerManager.ContainerClosed -= HandleContainerClosed;
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void EnsureContainerManager()
        {
            if (containerManager == null)
            {
                containerManager = FindObjectOfType<ContainerManager>();
            }
        }

        private void HandleContainerOpened(ContainerState state)
        {
            if (state == null)
            {
                return;
            }

            _activeContainerId = state.ContainerId;
            EnsureSlotViews(state);
            UpdateHeader(state);
            UpdateSlots(state);

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        private void HandleContainerUpdated(ContainerState state)
        {
            if (state == null || state.ContainerId != _activeContainerId)
            {
                return;
            }

            EnsureSlotViews(state);
            UpdateHeader(state);
            UpdateSlots(state);
        }

        private void HandleContainerClosed(int containerId)
        {
            if (containerId != _activeContainerId)
            {
                return;
            }

            _activeContainerId = -1;
            ClearSlots();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (statusText != null)
            {
                statusText.text = "Container closed";
            }
        }

        private void EnsureSlotViews(ContainerState state)
        {
            if (slotsParent == null || slotPrefab == null)
            {
                return;
            }

            var slotCount = Mathf.Max(0, state.SlotCount);
            for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                if (!_slotViews.TryGetValue(slotIndex, out var slotView) || slotView == null)
                {
                    var instance = Instantiate(slotPrefab, slotsParent);
                    instance.Initialize(slotIndex);
                    _slotViews[slotIndex] = instance;
                    slotView = instance;
                }
                else
                {
                    slotView.Initialize(slotIndex);
                }

                slotView.gameObject.SetActive(true);
                slotView.SetRole(DeriveRole(state, slotIndex));
            }

            var slotsToDisable = new List<int>();
            foreach (var pair in _slotViews)
            {
                if (pair.Key >= slotCount && pair.Value != null)
                {
                    slotsToDisable.Add(pair.Key);
                }
            }

            foreach (var slot in slotsToDisable)
            {
                var view = _slotViews[slot];
                view.Clear();
                view.gameObject.SetActive(false);
            }
        }

        private static ContainerSlotRole DeriveRole(ContainerState state, int slotIndex)
        {
            if (state?.Properties == null)
            {
                return ContainerSlotRole.Generic;
            }

            if (state.Properties.FuelSlot == slotIndex)
            {
                return ContainerSlotRole.Fuel;
            }

            if (state.Properties.ResultSlot == slotIndex)
            {
                return ContainerSlotRole.Result;
            }

            return ContainerSlotRole.Generic;
        }

        private void UpdateSlots(ContainerState state)
        {
            foreach (var pair in _slotViews)
            {
                var view = pair.Value;
                if (view == null || !view.gameObject.activeSelf)
                {
                    continue;
                }

                if (state.Slots != null && state.Slots.TryGetValue(pair.Key, out var slotState))
                {
                    view.Bind(slotState);
                }
                else
                {
                    view.Clear();
                }
            }

            if (statusText != null)
            {
                statusText.text = $"Slots: {state.SlotCount} | Snapshot: {state.SnapshotHash}";
            }
        }

        private void ClearSlots()
        {
            foreach (var view in _slotViews.Values)
            {
                if (view == null)
                {
                    continue;
                }

                view.Clear();
                view.gameObject.SetActive(false);
            }
        }

        private void UpdateHeader(ContainerState state)
        {
            if (containerTitleText != null)
            {
                containerTitleText.text = state.Title;
            }

            if (snapshotHashText != null)
            {
                snapshotHashText.text = $"Hash: {state.SnapshotHash}";
            }

            UpdateProgress(state);
        }

        private void UpdateProgress(ContainerState state)
        {
            var progress = state?.Properties != null ? Mathf.Clamp01(state.Properties.Progress) : 0f;
            var showProgress = progress > 0f;

            if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(showProgress);
                if (showProgress)
                {
                    progressSlider.value = progress;
                }
            }

            if (progressLabel != null)
            {
                progressLabel.text = showProgress ? $"{Mathf.RoundToInt(progress * 100f)}%" : string.Empty;
            }
        }

        private void OnCloseClicked()
        {
            if (_activeContainerId == -1 || containerManager == null)
            {
                return;
            }

            containerManager.RequestClose(_activeContainerId);
        }
    }
}
