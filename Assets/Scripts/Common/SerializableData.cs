using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct GridPosition {

	public int x;
	public int y;

	public GridPosition(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public static int GetDistance(GridPosition a, GridPosition b)
	{
		return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
	}

	public static bool operator == (GridPosition a, GridPosition b)
	{
		return a.x == b.x && a.y == b.y;
	}

	public static bool operator != (GridPosition a, GridPosition b)
	{
		return a.x != b.x || a.y != b.y;
	}

	public static GridPosition operator + (GridPosition a, GridPosition b)
	{
		return new GridPosition(a.x + b.x, a.y + b.y);
	}

	public static GridPosition operator - (GridPosition a, GridPosition b) 
	{
		return new GridPosition(a.x - b.x, a.y - b.y);
	}

	public override bool Equals(object obj)
	{
		if (obj is GridPosition)
		{
			GridPosition p = (GridPosition)obj;
			return x == p.x && y == p.y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return x ^ y;
	}
}

[System.Serializable]
public class EditorGridPosition {

	public GridPosition gridPosition;
	public Vector3 worldPosition;
	public Vector3 savedWorldPosition;
	public Vector3 offsetY;
	public int worldUnitHeight;
	public int savedWorldUnitHeight;
	public int baseWorldUnitHeight;
	public EditorTile tile;

	public int handleType
	{
		get { return tile == null ? 0 : 1; }
	} 

	public EditorGridPosition(GridPosition gridPosition, Vector3 worldPosition, int height, EditorTile tile = null)
	{
		this.gridPosition = gridPosition;
		this.worldPosition = worldPosition;
		this.savedWorldPosition = worldPosition;
		this.tile = tile;
		this.worldUnitHeight = height;
		this.savedWorldUnitHeight = height;
		this.baseWorldUnitHeight = height;
		this.offsetY = new Vector3();
	}

	public static bool operator == (EditorGridPosition a, EditorGridPosition b)
	{
		return a.gridPosition == b.gridPosition;
	}

	public static bool operator != (EditorGridPosition a, EditorGridPosition b)
	{
		return a.gridPosition != b.gridPosition;
	}

	public override bool Equals(object obj)
	{
		if (obj is EditorGridPosition)
		{
			EditorGridPosition p = (EditorGridPosition)obj;
			return gridPosition == p.gridPosition;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return gridPosition.x ^ gridPosition.y;
	}
}

[System.Serializable]
public class Tile2DInfo {

	public Sprite sprite;
	public GridTile.TileType tileType;
	public int placementLayer = 1;
	public int numLayers = 2;
}