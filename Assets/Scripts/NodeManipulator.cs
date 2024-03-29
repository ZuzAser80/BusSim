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
    [SerializeField] private Toggle isStop;
    private TMP_Dropdown trafficLightState;

    private void Awake() {
        trafficLightState = GetComponentInChildren<TMP_Dropdown>();
        gameObject.SetActive(false);
    }

    public void Init(Node newNode) {
        node = newNode;
        nodeName.text = node.displayName;
        trafficLightState.value = (int)node.Light;
        isStop.isOn = node.IsAStop;
        isStop.onValueChanged.AddListener(delegate{node.IsAStop = isStop;});
        isStop.onValueChanged.AddListener(delegate{FindAnyObjectByType<WaypointNavigator>().updateStop(node);});
        AmountOfPeople.onValueChanged.AddListener(delegate{node.Priority = int.Parse(AmountOfPeople.text);});
    }

    public void onTrafficLightChange() {
        node.Light = (Node.TrafficLight)trafficLightState.value;
        if(node.Light == Node.TrafficLight.RED) {FindAnyObjectByType<WaypointNavigator>().DisconnectNode(node);} 
        else {FindAnyObjectByType<WaypointNavigator>().ConnectNodeBack(node);}
    }
}
