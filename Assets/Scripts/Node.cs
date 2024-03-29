using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[SerializeField]
public class Node : MonoBehaviour {
    public enum TrafficLight { RED = 1, GREEN = 0, NONE }
    public Node PrevNode;
    public TrafficLight Light;
    public int Priority = 0;
    public TextMeshProUGUI nameDisplay;
    public bool IsAStop = true;
    public string displayName = "Node";

    private void Awake() {
        nameDisplay.text = displayName;
    }
}
