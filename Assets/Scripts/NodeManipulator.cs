using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NodeManipulator : MonoBehaviour
{
    public Node node;
    [SerializeField] private TextMeshProUGUI nodeName;
    [SerializeField] private TMP_InputField AmountOfPeople;
    private TMP_Dropdown trafficLightState;

    private void Awake() {
        trafficLightState = GetComponentInChildren<TMP_Dropdown>();
        gameObject.SetActive(false);
    }

    public void Init(Node newNode) {
        node = newNode;
        nodeName.text = node.displayName;
        trafficLightState.value = (int)node.Light;
        AmountOfPeople.text = "0";
        AmountOfPeople.onValueChanged.AddListener(delegate{
            node.Priority = int.Parse(AmountOfPeople.text);
            FindObjectOfType<WaypointNavigator>().updateStops(node);
        });
    }

    public void onTrafficLightChange() {
        node.Light = (Node.TrafficLight)trafficLightState.value;
        if(node.Light == Node.TrafficLight.RED) {FindAnyObjectByType<WaypointNavigator>().DisconnectNode(node);} 
        else {FindAnyObjectByType<WaypointNavigator>().ConnectNodeBack(node);}
    }
}
