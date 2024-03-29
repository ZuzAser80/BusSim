using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct NodeConnection {
    public Node parentNode;
    public Node connectedNode;
    //public List<Node> ConnectedNodes;
    public float MoveCost;
}

//Remember last node that we passed through
public class WaypointNavigator : MonoBehaviour
{
    [SerializeField] private Button startSim;
    [SerializeField] private Car bus;
    [SerializeField] private Car car;
    [SerializeField] private NodeManipulator nodeManipulator;
    [SerializeField] private TMP_InputField cars_inputfield;
    public Node Destination;
    public Node StartNode;
    public List<NodeConnection> nodes;
    private GameObject _cBus;
    public List<Node> stops = new List<Node>(); 
    private List<NodeConnection> availableNodes = new List<NodeConnection>();
    private Dictionary<NodeConnection, int> carsDictionary = new Dictionary<NodeConnection, int>();
    private List<Car> buses = new List<Car>();
    
    private void Awake() {
        startSim.onClick.AddListener(startSimulation);
        nodes.ForEach(x => availableNodes.Add(x));
        //stops = FindObjectsByType<Node>(FindObjectsSortMode.None).ToList();
    }

    public List<Node> getAvailableNeighbours(Node node) {
        List<Node> res = new List<Node>();
        availableNodes.FindAll(x => x.parentNode == node).ForEach(x => res.Add(x.connectedNode));
        return res;
    }

    public List<Node> getAllNeighbours(Node node) {
        List<Node> res = new List<Node>();
        nodes.FindAll(x => x.parentNode == node).ForEach(x => res.Add(x.connectedNode));
        return res;
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast (ray.origin, ray.direction, out RaycastHit hit, Mathf.Infinity) && hit.collider.gameObject.TryGetComponent(out Node node)) {
                nodeManipulator.gameObject.SetActive(true);
                nodeManipulator.Init(node);
            }
        }
    }

    void updateAllnodes() {
        nodes.ForEach(x => carsDictionary[x] = updateNodeConnection(x));
        nodes.Where(x => carsDictionary[x] > 2).ToList().ForEach(x => availableNodes.Remove(x));
        nodes.Where(x => carsDictionary[x] <= 2).ToList().ForEach(x => availableNodes.Add(x));
        //SORT BY PRIORITY AND THEN PATHFIND FOR EVERY BUS
        foreach(var bus in buses) {
            bus.Init(delegate{}, delegate{updateAllnodes();}, dijkstra(bus.getClosestNode(), Destination));
        }
    }

    int updateNodeConnection(NodeConnection node) {
        if (node.parentNode == null || node.connectedNode == null) return 0;
        var e = Physics.RaycastAll(node.parentNode.transform.position, 
        node.connectedNode.transform.position-node.parentNode.transform.position,
        Vector3.Distance(node.parentNode.transform.position, node.connectedNode.transform.position))
        .Where(x => x.collider.gameObject.layer == LayerMask.NameToLayer("Car")).Count();
        return e;
    }

    void startSimulation() {
        updateAllnodes();
        _cBus = Instantiate(bus.gameObject, StartNode.transform.position, Quaternion.identity);
        _cBus.GetComponent<Car>().Init(delegate{}, delegate{updateAllnodes();}, dijkstra(StartNode, Destination));
        if(cars_inputfield.text == "") return;
        summonCarsOnRanomNodes(car, int.Parse(cars_inputfield.text));
    }

    #region Pathfinding
    public List<Node> BFS() {
        HashSet<Node> visited = new HashSet<Node>
        {
            StartNode
        };
        
        Queue<Node> queue = new Queue<Node>();
        queue.Enqueue(StartNode);

        while (queue.Count > 0)
        {
                Node current = queue.Dequeue();
                List<Node> connectedNodes = new List<Node>();

                connectedNodes = getAvailableNeighbours(current);

                if (current == Destination)
                {
                    break;
                }

                foreach (var neighbor in connectedNodes)
                {
                    
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);

                        neighbor.PrevNode = current;
                    }
                }
        }
        Node _current = Destination;
        List<Node> path = new List<Node>();

            while (_current != null)
            {
                path.Add(_current);
                _current = _current.PrevNode;
            }
        path.Reverse();
        path.RemoveAt(0);
        path.ForEach(x => Debug.Log("::" + x.name));
        return path;
    }
    public List<Node> dijkstra(Node startNode, Node endNode, bool useOnlyAvailableNodes = true) {
        List<Node> visited = new List<Node>();

        Queue<Node> unvisited = new Queue<Node>();
        unvisited.Enqueue(startNode);

        Dictionary<Node, Node> previous = new Dictionary<Node, Node>();

        Dictionary<Node, float> distanceCost = new Dictionary<Node, float>();
        distanceCost[startNode] = 0;

        List<Node> _path = new List<Node>();

        while(unvisited.Count > 0) {
            Node currentNode = unvisited.Dequeue();
            if(currentNode == endNode) {
                while ( previous.ContainsKey ( currentNode ) )
                {
                    _path.Add( currentNode );
                    currentNode = previous [ currentNode ];
                }
                _path.Reverse();
                return _path;    
            } 
            var neighbours = useOnlyAvailableNodes ? getAvailableNeighbours(currentNode) : getAllNeighbours(currentNode);
            Debug.Log("c: " + currentNode + " :: " + neighbours);
            foreach(var n in neighbours) {
                unvisited.Enqueue(n);
                if(!distanceCost.ContainsKey(n)) {
                    distanceCost[n] = distanceCost[currentNode] + nodes.Find(x => x.parentNode == currentNode && x.connectedNode == n).MoveCost;
                    previous[n] = currentNode;
                }
                else if(distanceCost.ContainsKey(n) && distanceCost[n] > distanceCost[currentNode] + nodes.Find(x => x.parentNode == currentNode && x.connectedNode == n).MoveCost) {
                    distanceCost[n] = distanceCost[currentNode] + nodes.Find(x => x.parentNode == currentNode && x.connectedNode == n).MoveCost;
                    previous[n] = currentNode;
                }
            }
            visited.Add(currentNode);
        }
        return _path;
    }
    #endregion

    void summonCarsOnRanomNodes(Car car, int numberOfCars) {
        for (int i = 0; i < numberOfCars; i++) {
            var o = nodes.ElementAt(Random.Range(0, nodes.Count()));
            var g = Instantiate(car, o.parentNode.transform.position, Quaternion.identity);
            //TODO: RANDOMLY SELECT NODES AND THEN PATH TO THEM, THEN WAIT AND REPEAT
            g.GetComponent<Car>().Init(delegate{}, delegate{updateAllnodes();}, dijkstra(o.parentNode, nodes.ElementAt(Random.Range(0, nodes.Count())).connectedNode));
        }
    }

    public void updateStop(Node node) {
        if(stops.Contains(node)) {
            stops.Remove(node);
        } else {
            stops.Add(node);
        }
    }

    public void ConnectNodeBack(Node node) {
        nodes.FindAll(x => x.parentNode == node).Except(availableNodes).ToList().ForEach(x => availableNodes.Add(x));
        nodes.FindAll(x => x.connectedNode == node).Except(availableNodes).ToList().ForEach(x => availableNodes.Add(x));
    }

    public void DisconnectNode(Node node) {
        nodes.FindAll(x => x.parentNode == node).ForEach(x => RemoveConnection(node, x.connectedNode));
        nodes.FindAll(x => x.connectedNode == node).ForEach(x => RemoveConnection(x.parentNode, node));
    }

    public void RemoveConnection(Node node1, Node node2) {
        if(!availableNodes.Any(x => x.parentNode== node1 && x.connectedNode == node2)) return;
        availableNodes.Remove(availableNodes.Find(x => x.parentNode== node1 && x.connectedNode == node2));
    }

    private void OnDrawGizmos() {
        foreach(var connection in availableNodes) {
            drawLine(connection, Color.green);
        }
        foreach(var connection in nodes.Except(availableNodes).ToList()) {
            drawLine(connection, Color.red);
        }
        Gizmos.color = Color.blue;
        foreach(var node in stops) {
            Gizmos.DrawLine(node.transform.position, node.transform.position + Vector3.up);
        }
    }

    private void drawLine(NodeConnection connection, Color color) {
        if (connection.parentNode == null || connection.connectedNode == null || connection.connectedNode == connection.parentNode) return;
        Gizmos.color = color;
        Gizmos.DrawLine(connection.parentNode.transform.position, connection.connectedNode.transform.position);
        Vector3 direction = connection.parentNode.transform.position - connection.connectedNode.transform.position;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+20,0) * new Vector3(0,0,1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-20,0) * new Vector3(0,0,1);
        Gizmos.DrawLine(connection.connectedNode.transform.position, connection.connectedNode.transform.position - right);
        Gizmos.DrawLine(connection.connectedNode.transform.position, connection.connectedNode.transform.position - left);
    }
}