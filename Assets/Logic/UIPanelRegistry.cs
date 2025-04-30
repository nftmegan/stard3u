using UnityEngine;
using System;
using System.Collections.Generic;

public class UIPanelRegistry : MonoBehaviour
{
    [Serializable] private struct Mapping
    {
        public UIState state;
        public GameObject[] panels;    // assign in Inspector
    }

    [SerializeField] private Mapping[] panelSets;

    private readonly Dictionary<UIState, GameObject[]> dict = new();

    private void Awake()
    {
        foreach (var m in panelSets)
            dict[m.state] = m.panels;
    }

    public void Hook(UIStateController controller)
        => controller.OnStateChanged += ApplyState;

    private void ApplyState(UIStateChanged ev)
    {
        if (dict.TryGetValue(ev.Previous, out var prev))
            foreach (var go in prev) go.SetActive(false);

        if (dict.TryGetValue(ev.Current, out var cur))
            foreach (var go in cur)  go.SetActive(true);
    }
}