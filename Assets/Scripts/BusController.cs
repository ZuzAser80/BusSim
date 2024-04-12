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
    private WaypointNavigator nav;

    private void Awake() {
        nav = GetComponent<WaypointNavigator>();
    }

    public void InitController() {
        Debug.Log("InitController");
        for(int i = 0; i < buses.Count(); i++) {
            buses[i] = Instantiate(firstBus);
            buses[i].name += "::" + i;
            //Debug.Log(":: " + buses[i].name + "::" + i);
        }
        startBuses();
    }
    
    //OnExited - присоединияем его назад
    //OnReached - отсоединяем 1ого ребенка

    void startBuses() {
        startBus(buses[0], firstBus);
        //startBus(buses[1], buses[0]);
        for(int i = 1; i < buses.Count(); i++) {
            startBus(buses[i], buses[i-1]);
        }
    }

    private void startBus(Car currentBus, Car prevBus) {
        currentBus.transform.parent = prevBus.nextBusPos;
        currentBus.transform.localPosition = Vector3.zero;
        prevBus.reconnect = delegate {
            StartCoroutine(waitAndDo1(currentBus, prevBus));
        };
        prevBus.OnNodeReached = delegate {
            StartCoroutine(waitAndDo(currentBus, prevBus));
            
        };
    }

    IEnumerator waitAndDo(Car currentBus, Car prevBus) {
        currentBus.transform.parent = null;
        if(prevBus.getPath().IndexOf(prevBus.getLastNode()) + 1 < 0 || prevBus.getPath().Count > prevBus.getPath().IndexOf(prevBus.getLastNode()) + 1) yield return null;
        yield return currentBus.moveObject(prevBus.getLastNode(), 2f);
        Debug.Log("waitAndDo: " + currentBus);
        prevBus.reconnect?.Invoke();
    }

    IEnumerator waitAndDo1(Car currentBus, Car prevBus) {
        //rework later
        Debug.Log("Started waiting to glue: " + currentBus);
        yield return new WaitUntil(() => Vector3.Distance(currentBus.transform.position, prevBus.nextBusPos.transform.position) <= 0.1f);
        Debug.Log("done waiting glueing again :" + currentBus);
        currentBus.transform.parent = prevBus.nextBusPos;
        currentBus.transform.localRotation = Quaternion.Euler(Vector3.zero);
        currentBus.transform.localScale = Vector3.one;
        currentBus.transform.localPosition = Vector3.zero;
        currentBus.reconnect?.Invoke();
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
