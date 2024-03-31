using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Car : MonoBehaviour
{
    public Action OnNodeReached;
    public Action OnNodeExited;

    private List<Node> _path = new List<Node>();
    private Node lastNode;

    public void Init(Action onReached, Action onExited, List<Node> path) {
        OnNodeExited = onExited;
        OnNodeReached = onReached;
        _path = path;
        StartCoroutine(StartMoving());
    }

    public IEnumerator StartMoving() {
        foreach(var node in _path) {
            yield return StartCoroutine(moveObject(node, 5f));
        }
    }

    public void returnToLastNode() {
        StopCoroutine("moveObject");
        StartCoroutine(moveObject(lastNode, 5));
    }

    public Node getClosestNode() {
        return FindObjectsOfType<Node>().ToList().Find(x => Vector3.Distance(transform.position, x.transform.position) <= 0.1f);
    }

    public IEnumerator moveObject(Node node, float desiredMoveTime) {
        for(float i = 0; i < desiredMoveTime; i += Time.deltaTime) {
            while(Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1) && hit.collider.gameObject.layer == LayerMask.NameToLayer("Car")) yield return null;
            transform.position = Vector3.Lerp(transform.position, node.transform.position, i/(desiredMoveTime*100));
            transform.LookAt(node.transform.position, Vector3.up);
            yield return new WaitForEndOfFrame();
        }
        transform.position = node.transform.position;
        OnNodeExited?.Invoke();
        OnNodeReached?.Invoke();
        lastNode = node;
    }   

    private void OnDrawGizmos() {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
