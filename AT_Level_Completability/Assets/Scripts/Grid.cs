using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine.Serialization;

public enum MoveObstacles
{
	CheckObstacles,
	MoveObstacles,
	PathCreated,
	Wait
}

public enum ChangeObstacles
{
	CheckObstacle,
	Wait,
}


public class Grid : MonoBehaviour {

	public LayerMask unwalkableMask;
	public Vector2 gridSize;
	public float nodeRadius;
	private Node[,] grid;
	
	private Collider[] hitColliders;

	private List<Collider> _collidedObstacles = new List<Collider>();
	private List<Collider> _newCollidedObstacles = new List<Collider>();
	private List<bool> _collided = new List<bool>();

	public List<GameObject> changeableGameObjects = new List<GameObject>();
	private List<GameObject> usableGameObjects = new List<GameObject>();

	[HideInInspector]
	public List<Obstacles> ObstaclesList = new List<Obstacles>();

	private float nodeDiameter;
	private int gridX, gridY;

	private Vector3 gridStart;
	
	private bool moved = false;

	private int index = 0;

	[HideInInspector]
	public MoveObstacles _moveObstacles;
	[HideInInspector]
	public ChangeObstacles _changeObstacles;
	private bool checked_ = false;
	private bool change = true;
	private bool set_ = false;

	private GameObject defaultObject;
	private GameObject[] newObject;
	
	private bool set = false;
	private float timer = 2f;
	private float timer_ = 2f;
	private int changeIndex = 0;
	private int obstacleIndex = 0;
	private int newObjIndex = 0;
	private int checkIndex = 0;

	void Awake() {
		nodeDiameter = nodeRadius*2;
		gridX = Mathf.RoundToInt(gridSize.x/nodeDiameter);
		gridY = Mathf.RoundToInt(gridSize.y/nodeDiameter);
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
					StartCoroutine(CheckObstacles(checkIndex));
					timer_ = 1f;
					checked_ = true;
				}
				break;
			}
			case MoveObstacles.Wait:
			{
				timer_ -= Time.deltaTime;
				if (timer_ <= 0)
				{
					checked_ = false;
					checkIndex += 1;
					_moveObstacles = MoveObstacles.CheckObstacles;
				}
				break;
			}
			case MoveObstacles.MoveObstacles:
			{
				switch (_changeObstacles)
				{
					case ChangeObstacles.CheckObstacle:
					{
						CheckObstacle();
						break;
					}
					case ChangeObstacles.Wait:
					{
						Wait();
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
	private void CheckObstacle()
	{
		if (!set_)
		{
			defaultObject = _collidedObstacles[obstacleIndex].gameObject;
			ObstaclesList[obstacleIndex].hitColliders = defaultObject;
			print(defaultObject);
			set_ = true;
		}
		if (!set)
		{
			newObject[newObjIndex] = Instantiate(changeableGameObjects[changeIndex], defaultObject.transform.position,
				defaultObject.transform.rotation);
			if (_collidedObstacles[obstacleIndex] != null)
			{
				_collidedObstacles[obstacleIndex].gameObject.SetActive(false);
			}
			if (newObjIndex > 0)
			{
				newObject[newObjIndex - 1].gameObject.SetActive(false);
			}

			Transform[] children;
			children = newObject[newObjIndex].GetComponentsInChildren<Transform>();
			if (children.Length > 0)
			{
				for (int c = 0; c < children.Length - 1; c++)
				{
					_newCollidedObstacles.Add(newObject[newObjIndex].GetComponentsInChildren<Collider>()[c]);
				}
			}
			else
			{
				_newCollidedObstacles.Add(newObject[newObjIndex].GetComponent<Collider>());
			}

			set = true;
		}
		timer = 2.0f;
		_changeObstacles = ChangeObstacles.Wait;
	}
	private void Wait()
	{
		CheckPath();
		timer -= Time.deltaTime;
		if (timer <= 0)
		{
			if (_collided.All(c => c == false) && _collided.Count != 0)
			{
				ObstaclesList[obstacleIndex].changeableGameObjects.Add(newObject[newObjIndex]);
			}

			if (changeIndex < changeableGameObjects.Count - 1)
			{
				print("Test 1");
				changeIndex += 1;
				newObjIndex += 1;
				set = false;
				_changeObstacles = ChangeObstacles.CheckObstacle;
			}
			else if (changeIndex == changeableGameObjects.Count - 1 && 
			         obstacleIndex < _collidedObstacles.Count - 1)
			{
				print("Test 2");
				obstacleIndex += 1;
				changeIndex = 0;
				set_ = false;
				_changeObstacles = ChangeObstacles.CheckObstacle;
			}
			else if(changeIndex   == changeableGameObjects.Count - 1 && 
			        obstacleIndex == _collidedObstacles.Count - 1)
			{
				foreach (var obstacles in _collidedObstacles)
				{
					obstacles.gameObject.SetActive(true);
				}
				for (int i = 0; i < ObstaclesList.Count; i++)
				{
					for (int j = 0; j < ObstaclesList[i].changeableGameObjects.Count; j++)
					{
						ObstaclesList[i].changeableGameObjects[j].SetActive(false);
					}
				}
				_moveObstacles = MoveObstacles.PathCreated;
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
	
	IEnumerator CheckObstacles(int index)
	{
		yield return new WaitForSeconds(1f);

		CheckPath();
		
		if (_collided.All(c => c == false) && _collided.Count != 0)
		{
			_moveObstacles = MoveObstacles.PathCreated;
			StopCoroutine(CheckObstacles(0));
		}
		else
		{
			hitColliders[index].gameObject.gameObject.layer = 7;
		}
		yield return new WaitForSeconds(1f);
		
		hitColliders[index].gameObject.gameObject.layer = 6;
		
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

		foreach (var obstacle in _collidedObstacles)
		{
			ObstaclesList.Add(new Obstacles());
		}
		

		newObject = new GameObject[changeableGameObjects.Count * _collidedObstacles.Count];
		if (_collidedObstacles.Count == 0)
		{
			_moveObstacles = MoveObstacles.Wait;
		}
		else
		{
			_moveObstacles = MoveObstacles.MoveObstacles;
		}

	}
	
	
	
	void CreateGrid() {
		grid = new Node[gridX,gridY];
		gridStart = transform.position - Vector3.right * gridSize.x/2 - Vector3.forward * gridSize.y/2;

		for (int x = 0; x < gridX; x ++) {
			for (int y = 0; y < gridY; y ++) {
				Vector3 worldPoint = gridStart + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
				bool walkable = !(Physics.CheckSphere(worldPoint,nodeRadius,unwalkableMask));
				grid[x,y] = new Node(walkable,worldPoint, x,y);
			}
		}
	}
	public List<Node> CheckNeighbours(Node node) {
		List<Node> neighbours = new List<Node>();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if (x == 0 && y == 0)
				{
					continue;
				}
				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX >= 0 && checkX < gridX && 
				    checkY >= 0 && checkY < gridY) 
				{
					neighbours.Add(grid[checkX,checkY]);
				}
			}
		}

		return neighbours;
	}
	
	public Node GetNodeAtWorldPoint(Vector3 worldPosition) {
		float xPos = (worldPosition.x + gridSize.x/2) / gridSize.x;
		float yPos = (worldPosition.z + gridSize.y/2) / gridSize.y;
		xPos = Mathf.Clamp01(xPos);
		yPos = Mathf.Clamp01(yPos);

		int x = Mathf.RoundToInt((gridX-1) * xPos);
		int y = Mathf.RoundToInt((gridY-1) * yPos);
		return grid[x,y];
	}

	public List<Node> path;
	void OnDrawGizmos() {
		Gizmos.DrawWireCube(transform.position,new Vector3(gridSize.x,1,gridSize.y));

		if (grid != null) {
			foreach (Node n in grid) {
				Gizmos.color = (n.walkable)?Color.white:Color.black;
				if (path != null)
					if (path.Contains(n))
						Gizmos.color = Color.blue;
				Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter-.1f));
			}
			for (int x = 0; x < gridX; x++)
			{
				for (int y = 0; y < gridY; y++)
				{
					Vector3 worldPoint = gridStart + Vector3.right * (x * nodeDiameter + nodeRadius) +
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
