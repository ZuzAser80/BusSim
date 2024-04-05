using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BusController : MonoBehaviour
{
    public Car firstBus;
    public List<Car> buses = new List<Car>();
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private string _name;

    private void Awake() {
        
    }

    public void InitController() {
        Debug.Log("InitController");
        for(int i = 0; i < buses.Count(); i++) {
            buses[i] = Instantiate(firstBus);
            buses[i].name += "::" + i;
            Debug.Log(":: " + buses[i].name + "::" + i);
        }
        StartCoroutine(startBuses());
    }
    
    //OnExited - присоединияем его назад
    //OnReached - отсоединяем 1ого ребенка

    IEnumerator startBuses() {
        buses[0].transform.parent = firstBus.nextBusPos;
        buses[0].transform.localPosition = Vector3.zero;
        firstBus.OnNodeExited += delegate {
            Debug.Log("OnNodeExited");
            StartCoroutine(waitAndDo());
        };
        firstBus.OnNodeReached += delegate {
            Debug.Log("OnNodeReached");
            buses[0].transform.parent = null;
        };
        foreach(var b in buses) {
            //b.Init(firstBus.OnNodeReached, firstBus.OnNodeExited, firstBus.getPath());
            yield return new WaitForSeconds(1);
        }
    }

    IEnumerator waitAndDo() {
        Debug.Log("n1");
        yield return buses[0].moveObject(firstBus.getClosestNode(), 2f);
        Debug.Log("n2");
        buses[0].transform.forward = firstBus.transform.forward;
        buses[0].transform.parent = firstBus.nextBusPos;
        buses[0].transform.localPosition = Vector3.zero;
    }

    public void detachBusesFrom(Car lastBus) {
        for(int i = 0; i < buses.Count()-1; i++) {
            Debug.Log(" : : " + Vector3.Distance(buses[i].transform.position, buses[i-1].transform.position));
        }
        var r = buses.GetRange(buses.IndexOf(lastBus), buses.Count - buses.IndexOf(lastBus)).ToList();
        r.ForEach(x => x.returnToLastNode());
        r.ForEach(x => Debug.Log(": " + x));
    }

}
