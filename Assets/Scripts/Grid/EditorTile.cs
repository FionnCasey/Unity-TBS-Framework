using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorTile : MonoBehaviour {

	public int tileInfoID;
	public float moveGridX;
	public float moveGridY;
	public float moveWorldX;
	public float moveWorldY;
	public float moveWorldZ;
	public float scaleX;
	public float scaleY;
	public int numLayers;
	public int placementLayer;
	public Vector3 savedPosition;

	public GridPosition gridPosition;
	public GridTile.TileType tileType;
}
