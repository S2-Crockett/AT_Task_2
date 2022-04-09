using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class Pathfinding : MonoBehaviour {

    public Transform player, target;
    Grid grid;

    void Awake() 
    {
        grid = GetComponent<Grid> ();
    }

    void Update() 
    {
        CreatePath (player.position, target.position);
    }

    void CreatePath(Vector3 playerPos, Vector3 targetPos) {
        Node playerNode = grid.GetNodeAtWorldPoint(playerPos);
        Node targetNode = grid.GetNodeAtWorldPoint(targetPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(playerNode);

        while (openSet.Count > 0) {
            Node node = openSet[0];
            for (int i = 1; i < openSet.Count; i ++) 
            {
                if (openSet[i].fCost < node.fCost || openSet[i].fCost == node.fCost) 
                {
                    if (openSet[i].hCost < node.hCost)
                        node = openSet[i];
                }
            }

            openSet.Remove(node);
            closedSet.Add(node);

            if (node == targetNode) {
                DrawPath(playerNode,targetNode);
                return;
            }

            foreach (Node neighbour in grid.CheckNeighbours(node)) {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) {
                    continue;
                }

                int costToNeighbour = node.gCost + GetDistanceToNode(node, neighbour);
                if (costToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) 
                {
                    neighbour.gCost = costToNeighbour;
                    neighbour.hCost = GetDistanceToNode(neighbour, targetNode);
                    neighbour.parent = node;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
    }

    void DrawPath(Node startNode, Node endNode) {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();

        grid.path = path;
    }

    int GetDistanceToNode(Node nodeA, Node nodeB) {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
            return 14*dstY + 10* (dstX-dstY);
        return 14*dstX + 10 * (dstY-dstX);
    }
}
