using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private Car _carPrefab;
    [SerializeField] private NodeManipulator nodeManipulator;
    [SerializeField] private TMP_InputField cars_inputfield;
    public Node Destination;
    public Node StartNode;
    public List<NodeConnection> nodes;
    private GameObject _cBus;
    public List<Node> stops = new List<Node>(); 
    private List<Car> buses = new List<Car>();
    private List<NodeConnection> availableNodes = new List<NodeConnection>();
    private Dictionary<NodeConnection, int> carsDictionary = new Dictionary<NodeConnection, int>();
    private Dictionary<Node, Car> busesDestiantions = new Dictionary<Node, Car>();
    private BusController controller;
    
    private void Awake() {
        startSim.onClick.AddListener(startSimulation);
        nodes.ForEach(x => availableNodes.Add(x));
        controller = GetComponent<BusController>();
        //stops = FindObjectsByType<Node>(FindObjectsSortMode.None).ToList();
    }

    #region Neighbours operations
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
    #endregion

    private void Start() {
        startSimulation();
        StartCoroutine(updateAllnodes());
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
        if(Input.GetKeyDown(KeyCode.Alpha4)) {
            controller.detachBusesFrom(controller.buses.ElementAt(2));
        }
    }

    IEnumerator updateAllnodes() {
        nodes.ForEach(x => carsDictionary[x] = updateNodeConnection(x));
        nodes.Where(x => carsDictionary[x] > 2).ToList().ForEach(x => availableNodes.Remove(x));
        nodes.Where(x => carsDictionary[x] <= 2).ToList().ForEach(x => availableNodes.Add(x));
        //SORT BY PRIORITY AND THEN PATHFIND FOR EVERY BUS
        stops.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        stops.Reverse();
        //ITERATE OVER BUSES AND ASSIGN NEW PATHS
        foreach(var l in buses) {
            l.GetComponent<Car>().Init(dijkstra(l.getLastNode(), stops[buses.IndexOf(l)]));
        }
        yield return new WaitForSeconds(1);
        StartCoroutine(updateAllnodes());
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
        //StopCoroutine(updateAllnodes());
        controller.firstBus = Instantiate(controller.firstBus.gameObject, StartNode.transform.position, Quaternion.identity).GetComponent<Car>();
        controller.firstBus.Init(dijkstra(StartNode, Destination));
        controller.InitController();
        //_cBus.GetComponent<Car>().Init(delegate{}, delegate{}, dijkstra(StartNode, Destination));
        if(cars_inputfield.text == "") return;
        StartCoroutine(summonCarsOnRanomNodes(_carPrefab, int.Parse(cars_inputfield.text)));
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

    IEnumerator summonCarsOnRanomNodes(Car car, int numberOfCars) {
        var allNodes = FindObjectsByType<Node>(FindObjectsSortMode.None).ToList().Where(x => x != StartNode).ToList();
        for (int i = 0; i < numberOfCars; i++) {
            var startNode = allNodes.ElementAt(Random.Range(0, allNodes.Count()));
            var endNode = allNodes.Where(x => x != startNode).ElementAt(Random.Range(0, allNodes.Where(x => x != startNode).Count()));
            var g = Instantiate(car, startNode.transform.position, Quaternion.identity);
            g.GetComponent<Car>().Init(delegate{if(g.getClosestNode() == endNode) Destroy(g.gameObject); }, delegate{if(g.getPath().Count <= 0) Destroy(g.gameObject);}, dijkstra(startNode, endNode));
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(Random.Range(1, 10));
        StartCoroutine(summonCarsOnRanomNodes(car, Random.Range(2, 10)));
    }

    public void updateStop(Node node) {
        if(stops.Contains(node)) {
            stops.Remove(node);
        } else {
            stops.Add(node);
        }
    }

    #region Node operations
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

    public NodeConnection FindConnection(Node node1, Node node2) {
        return nodes.Find(x => x.parentNode == node1 && x.connectedNode == node2);
    }

    public bool CheckIfConnectionAvailable(NodeConnection connection) {
        return availableNodes.Contains(connection);
    }
    public Node GetHighestPriorityStop() {
        var r = new List<float>();
        stops.ForEach(x => r.Add(x.Priority));
        return stops.Find(x => x.Priority == r.Max());
    }

    public void BusReroute(Car bus, Node startNode) {
        if(dijkstra(startNode, GetHighestPriorityStop()).Count() > 0) {
            bus.Init(dijkstra(startNode, GetHighestPriorityStop()));
        } else {
            Debug.Log("FFFUCK YOU");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmos() {
        foreach(var connection in availableNodes) {
            drawLine(connection, Color.green);
        }
        foreach(var connection in nodes.Except(availableNodes).ToList()) {
            drawLine(connection, Color.red);
        }
        Gizmos.color = Color.blue;
        foreach(var node in stops) {
            Gizmos.DrawLine(node.transform.position, node.transform.position + Vector3.up*3);
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
    #endregion
}