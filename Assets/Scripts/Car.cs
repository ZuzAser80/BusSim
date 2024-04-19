using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Car : MonoBehaviour
{
    public Action OnNodeReached;
    public Action OnNodeExited;
    public Action reconnect;

    private List<Node> _path = new List<Node>();
    private Node lastNode;
    public Node nextNode;
    public Transform nextBusPos;
    public bool isFree = true;

    public void Init(Action onReached, Action onExited, List<Node> path) {
        OnNodeExited = onExited;
        OnNodeReached = onReached;
        _path = path;
        StartCoroutine(StartMoving());
    }

    public void Init(List<Node> path) {
        _path = path;
        StartCoroutine(StartMoving());
    }

    public IEnumerator StartMoving() {
        isFree = false;
        foreach(var node in _path) {
            yield return StartCoroutine(moveObject(node, 5f));
        }
        Debug.Log(name + " has stopped");
        isFree = true;
    }

    public Node getLastNode() {
        return lastNode;
    }

    public List<Node> getPath() {
        return _path;
    }

    public void returnToLastNodeByBus(Car bus) {
        Debug.Log("return: " + bus.name + " :: to " + bus.lastNode.name);
        StopCoroutine("moveObject");
        StartCoroutine(moveObject(bus.lastNode, 5));
    }

    public void returnToLastNode() {
        Debug.Log("return: " + name + " :: to " + lastNode.name);
        OnNodeExited = delegate {};
        OnNodeReached = delegate {};
        StopCoroutine("moveObject");
        //rethink:::
        StartCoroutine(moveObject(lastNode, 5));
    }

    public Node getClosestNode() {
        return FindObjectsOfType<Node>().ToList().Find(x => Vector3.Distance(transform.position, x.transform.position) <= 0.1f);
    }

    public IEnumerator moveObject(Node node, float desiredMoveTime) {
        nextNode = node;
        for(float i = 0; i < desiredMoveTime; i += Time.deltaTime) {
            while(Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1) && hit.collider.gameObject.layer == LayerMask.NameToLayer("Car")) yield return null;
            transform.position = Vector3.Lerp(transform.position, node.transform.position, i/(desiredMoveTime*100));
            transform.LookAt(node.transform.position, Vector3.up);
            yield return new WaitForEndOfFrame();
        }
        lastNode = node;
        transform.position = node.transform.position;
        OnNodeReached?.Invoke();
        OnNodeExited?.Invoke();
        StopCoroutine(moveObject(node, desiredMoveTime));
    }   

    private void OnDrawGizmos() {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
