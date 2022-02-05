using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public enum MoveObstacles
{
	CheckObstacles,
	MoveObstacles,
	PathCreated
}

public enum ChangeObstacles
{
	CheckObstacle,
	Wait,
}


public class Grid : MonoBehaviour {

	public LayerMask unwalkableMask;
	public Vector2 gridWorldSize;
	public float nodeRadius;
	Node[,] grid;
	
	public Collider[] hitColliders;

	public List<Collider> _collidedObstacles = new List<Collider>();
	public List<Collider> _newCollidedObstacles = new List<Collider>();
	public List<bool> _collided = new List<bool>();

	public List<GameObject> changeableGameObjects = new List<GameObject>();
	public List<GameObject> usableGameObjects = new List<GameObject>();


	[SerializeField] private List<Obstacles> ObstaclesList = new List<Obstacles>();

	float nodeDiameter;
	int gridSizeX, gridSizeY;

	private Vector3 worldBottomLeft;
	
	private bool moved = false;

	private int index = 0;

	public MoveObstacles _moveObstacles;
	public ChangeObstacles _changeObstacles;
	private bool checked_ = false;
	private bool change = true;
	private bool set = false;
	private bool set_ = false;

	private GameObject defaultObject;
	private GameObject newObject;

	void Awake() {
		nodeDiameter = nodeRadius*2;
		gridSizeX = Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
		gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);
		CreateGrid();
		hitColliders = new Collider[GameObject.FindGameObjectsWithTag("Obstacle").Length];
		foreach (var obstacle in GameObject.FindGameObjectsWithTag("Obstacle"))
		{
			hitColliders[index] = obstacle.GetComponent<Collider>();
			index += 1;
		}
		_moveObstacles = MoveObstacles.CheckObstacles;
	}
	

	private void Update()
	{
		switch (_moveObstacles)
		{
			case MoveObstacles.CheckObstacles:
			{
				if (!checked_)
				{
					StartCoroutine(CheckObstacles());
					checked_ = true;
				}
				break;
			}
			case MoveObstacles.MoveObstacles:
			{
				switch (_changeObstacles)
				{
					case ChangeObstacles.CheckObstacle:
					{
						if (!set_)
						{
							defaultObject = _collidedObstacles[0].gameObject;
							ObstaclesList[0].hitColliders = defaultObject;
							print(_collidedObstacles[0].gameObject);
							set_ = true;
						}
						

						if (!set)
						{
							newObject = Instantiate(changeableGameObjects[0], defaultObject.transform.position,
								defaultObject.transform.rotation);
							_collidedObstacles[0].gameObject.SetActive(false);
							_collidedObstacles.RemoveAt(0);


							Transform[] children;
							children = newObject.GetComponentsInChildren<Transform>();
							for (int c = 0; c < children.Length - 1; c++)
							{
								_newCollidedObstacles.Add(newObject.GetComponentsInChildren<Collider>()[c]);
							}
							set = true;
						}

						_changeObstacles = ChangeObstacles.Wait;
						break;
					}
					case ChangeObstacles.Wait:
					{
						CheckPath();
						if (_collided.All(c => c == false) && _collided.Count != 0)
						{
							ObstaclesList[0].changeableGameObjects.Add(newObject);
							_moveObstacles = MoveObstacles.PathCreated;
						}
						break;
					}
				}
				
				break;
			}
			case MoveObstacles.PathCreated:
			{

				break;
			}
		}
	}




	public void CheckPath()
	{
		_collided.Clear();
		if (grid != null && path != null)
		{
			foreach (Node n in grid)
			{
				for (int i = 0; i < path.Count; i++)
				{
					if (n.worldPosition == path[i].worldPosition)
					{
						bool clear = !(Physics.CheckSphere(n.worldPosition, nodeRadius, unwalkableMask));
						if (!clear)
						{
							_collided.Add(true);
						}
						else
						{
							_collided.Add(false);
						}
					}
				}
			}
		}
	}
	
	IEnumerator CheckObstacles()
	{
		yield return new WaitForSeconds(1f);

		CheckPath();
		
		if (_collided.All(c => c == false) && _collided.Count != 0)
		{
			_moveObstacles = MoveObstacles.PathCreated;
			StopCoroutine(CheckObstacles());
		}
		else
		{
			foreach (var objects in hitColliders)
			{
				objects.gameObject.gameObject.layer = 7;
			}
		}
		yield return new WaitForSeconds(2f);
		
		foreach (var objects in hitColliders)
		{
			objects.gameObject.gameObject.layer = 6;
		}
		if (grid != null && path != null)
		{
			foreach (Node n in grid)
			{
				for (int i = 0; i < path.Count; i++)
				{
					if (n.worldPosition == path[i].worldPosition)
					{
						bool clear = !(Physics.CheckSphere(n.worldPosition, nodeRadius, unwalkableMask));
						if (!clear)
						{
							Collider[] collider = Physics.OverlapSphere(n.worldPosition, nodeRadius, unwalkableMask);
							if (!_collidedObstacles.Contains(collider[0]))
							{
								_collidedObstacles.Add(collider[0]);
							}
						}
					}
				}
			}
		}
		
		ObstaclesList.Add(new Obstacles());
		print("Created List");
		_moveObstacles = MoveObstacles.MoveObstacles;
	}
	
	
	
	void CreateGrid() {
		grid = new Node[gridSizeX,gridSizeY];
		worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2;

		for (int x = 0; x < gridSizeX; x ++) {
			for (int y = 0; y < gridSizeY; y ++) {
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
				bool walkable = !(Physics.CheckSphere(worldPoint,nodeRadius,unwalkableMask));
				grid[x,y] = new Node(walkable,worldPoint, x,y);
			}
		}
	}

	public List<Node> GetNeighbours(Node node) {
		List<Node> neighbours = new List<Node>();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if (x == 0 && y == 0)
					continue;

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
					neighbours.Add(grid[checkX,checkY]);
				}
			}
		}

		return neighbours;
	}
	

	public Node NodeFromWorldPoint(Vector3 worldPosition) {
		float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
		float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
		percentX = Mathf.Clamp01(percentX);
		percentY = Mathf.Clamp01(percentY);

		int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY-1) * percentY);
		return grid[x,y];
	}

	public List<Node> path;
	void OnDrawGizmos() {
		Gizmos.DrawWireCube(transform.position,new Vector3(gridWorldSize.x,1,gridWorldSize.y));

		if (grid != null) {
			foreach (Node n in grid) {
				Gizmos.color = (n.walkable)?Color.white:Color.red;
				if (path != null)
					if (path.Contains(n))
						Gizmos.color = Color.black;
				Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter-.1f));
			}
			for (int x = 0; x < gridSizeX; x++)
			{
				for (int y = 0; y < gridSizeY; y++)
				{
					Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) +
					                     Vector3.forward * (y * nodeDiameter + nodeRadius);
                    
					bool clear = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
					grid[x, y] = new Node(clear, worldPoint, x, y);
				}
			}
		}
	}
}

[System.Serializable]
public class Obstacles
{
	public GameObject hitColliders;
	public List<GameObject> changeableGameObjects = new List<GameObject>();
}
