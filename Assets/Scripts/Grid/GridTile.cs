using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridTile : MonoBehaviour {

	public GridPosition gridPosition;

	public enum TileType {
		Active,
		BlocksMovement,
		Inactive
	}
	public TileType tileType;

	// FOR PATHFINDING ALGORITHMS //
	public int worldHeight;
	public int g;
	public int h;
	public int f;
	public int distance;
	public GridTile parent;

	///<summary>
	///Use this position when moving objects on the grid.
	///</summary>
	///<returns>The top center point of the tile.</returns>
	public virtual Vector3 GetAnchorPosition()
	{
		float height = GetComponent<Renderer>().bounds.size.y * .5f;
		return new Vector3(transform.position.x, transform.position.y + height, transform.position.z);
	}
}
