using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class IsoGridGenerator : MonoBehaviour {

    public string stageName = "New Stage";
	public int startWidth = 5;
	public int startLength = 5;

    public bool useHeightForPathfinding = true;
    public int tileHeightPerLayer = 2;

	public float handleSize = .15f;
	public float handleColliderSize = .75f;

	private Transform container;

	public Color gridDefault;
	public Color gridMouseOver;
	public Color gridSelected;
	public Color handleDefault;
	public Color handleActive;
	public Color handleMouseOver;
	public Color handleSelected;
	public Color tileGroupSelection;

    public float pixelWidth = 64f;
    public float unitsPerPixel = 20f;
    public int widthToHeightRatio = 2;
    public Vector3 startPosition;
    public bool centerStartPos = true;
    public float pixelHeight { get { return pixelWidth / 2f; } }
    public float scaledWidth { get { return (pixelWidth / unitsPerPixel); } }
	public float scaledHeight { get { return scaledWidth / widthToHeightRatio; } }

    public List<Tile2DInfo> tileInfo = new List<Tile2DInfo>();
    public List<EditorGridPosition> editorPositions = new List<EditorGridPosition>();
    public List<EditorTile> selectedTiles = new List<EditorTile>();
    public TileDictionary tileDictionary;
    
    public enum OutlineViewMode {
        SplitLayers,
        SingleLayer,
        DoubleLayer,
        None
    }
    public OutlineViewMode outlineViewMode;

	public void CreateNewGrid()
    {
        container = new GameObject(stageName).transform;
		editorPositions = new List<EditorGridPosition>();
		tileDictionary = new TileDictionary();
        Vector3 placePosition = centerStartPos ? new Vector3(-(startWidth * scaledWidth) * .5f + scaledWidth * .5f, 0, 0) : startPosition;

		for (int i = 0; i < startWidth; i++)
		{
			for (int j = 0; j < startLength; j++)
			{
                float isoX = (i * scaledWidth * .5f) + (j * scaledWidth * .5f);
				float isoY = (j * scaledHeight * .5f) - (i * scaledHeight * .5f);
                float depth = i - j;
				Vector3 worldPosition = placePosition + new Vector3(isoX, isoY, -depth);
				GridPosition gridPosition = new GridPosition(i, j);
                editorPositions.Add(new EditorGridPosition(gridPosition, worldPosition, 0));
			}
		}
    }

    public void PlaceTile(EditorGridPosition info, int tileIndex)
    {
        container = GameObject.Find(stageName).transform;
        if (container == null){
            container = new GameObject(stageName).transform;
        }
        EditorTile editorTile = new GameObject(
            string.Format("[{0}, {1}] {2}", info.gridPosition.x, info.gridPosition.y, tileInfo[tileIndex].sprite.name)
        ).AddComponent<EditorTile>();

        editorTile.gameObject.AddComponent<SpriteRenderer>().sprite = tileInfo[tileIndex].sprite;
        editorTile.gridPosition = info.gridPosition;
        editorTile.placementLayer = tileInfo[tileIndex].placementLayer;
        editorTile.numLayers = tileInfo[tileIndex].numLayers;

        Vector3 offset = new Vector3();
        if (tileInfo[tileIndex].numLayers == 1)
        {
            offset.y = tileInfo[tileIndex].placementLayer == 1 ? scaledHeight * .25f : -scaledHeight * .25f;
        }
        info.offsetY += offset;
        editorTile.transform.position = info.worldPosition + offset;

        editorTile.transform.SetParent(container);
        editorTile.tileType = tileInfo[tileIndex].tileType;
        editorTile.savedPosition = info.worldPosition;
        editorTile.scaleX = editorTile.transform.localScale.x;
        editorTile.scaleY = editorTile.transform.localScale.y;
        info.tile = editorTile;
        GridTile tile = editorTile.gameObject.AddComponent<GridTile>();
        tile.gridPosition = info.gridPosition;
        tile.tileType = editorTile.tileType;

        if (tileInfo[tileIndex].numLayers == 1)
        {
            info.worldUnitHeight += tileInfo[tileIndex].placementLayer == 2 ? tileHeightPerLayer : tileHeightPerLayer * 2;
        }
        else
        {
            info.worldUnitHeight += tileHeightPerLayer * 2;
        }
        info.savedWorldUnitHeight = info.worldUnitHeight;
        tile.worldHeight = info.worldUnitHeight;

        tileDictionary.Add(editorTile.gridPosition, tile);
        GetAdjacentTiles(info);
    }

    public virtual void RemoveTile(EditorGridPosition info)
	{
		tileDictionary.Remove(info.gridPosition);
        info.worldUnitHeight = info.baseWorldUnitHeight;
        info.savedWorldUnitHeight = info.baseWorldUnitHeight;
        DestroyImmediate(info.tile.gameObject);
	}

    public void GetAdjacentTiles(EditorGridPosition info)
    {
        for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				GridPosition newGridPos = info.gridPosition + new GridPosition(x, y);
				float isoX = (x * scaledWidth * .5f) + (y * scaledWidth * .5f);
				float isoY = (y * scaledHeight * .5f) - (x * scaledHeight * .5f);
                float depth = x - y;
				Vector3 newWorldPos = info.worldPosition + new Vector3(isoX, isoY, -depth);
				EditorGridPosition newInfo = new EditorGridPosition(newGridPos, newWorldPos, info.baseWorldUnitHeight);
				if (!editorPositions.Contains(newInfo))
				{
					editorPositions.Add(newInfo);
				}
			}
		}
    }

    public void ExtrudeTiles(Vector3 dir)
    {
        foreach (EditorTile selectedTile in selectedTiles)
        {
            EditorGridPosition info = editorPositions.Find(x => x.gridPosition == selectedTile.gridPosition);
            GridTile tile = tileDictionary[info.tile.gridPosition];
            tile.tileType = GridTile.TileType.Inactive;
            Vector3 scaledDir = new Vector3(dir.x * scaledWidth, dir.y * scaledHeight, dir.z);
            Vector3 pos = info.worldPosition + scaledDir;
            Instantiate(tile, pos, Quaternion.identity, container);
        }
    }

    public void AdjustTilePositions()
    {
        foreach (EditorTile selectedTile in selectedTiles)
        {
            EditorGridPosition info = editorPositions.Find(x => x.gridPosition == selectedTile.gridPosition);
            selectedTile.moveGridX = selectedTiles[0].moveGridX;
            selectedTile.moveGridY = selectedTiles[0].moveGridY;
            selectedTile.moveWorldX = selectedTiles[0].moveWorldX;
            selectedTile.moveWorldY = selectedTiles[0].moveWorldY;
            float isoX = (selectedTile.moveGridX * scaledWidth * .5f) + (selectedTile.moveGridY * scaledWidth * .5f);
            float isoY = (selectedTile.moveGridY * scaledHeight * .5f) - (selectedTile.moveGridX * scaledHeight * .5f);
            Vector3 newPosition = new Vector3(isoX + (selectedTile.moveWorldX * scaledWidth), isoY + (selectedTile.moveWorldY * scaledHeight), 0);
            selectedTile.transform.position = selectedTile.savedPosition + newPosition + info.offsetY;
            info.worldPosition = info.savedWorldPosition + newPosition;
            CalculateHeight(selectedTile, info, selectedTile.moveWorldY);
        }
    }

    public void CalculateHeight(EditorTile tile, EditorGridPosition info, float y)
    {
        int numLayers = y >= .75 ? 2 : y >= .5 ? 1 : y <= -.75 ? -2 : y <= .5 ? -1 : 0;
        int height = Mathf.FloorToInt(numLayers * tileHeightPerLayer);
        info.worldUnitHeight = info.savedWorldUnitHeight + height;
        tileDictionary[tile.gridPosition].worldHeight = info.worldUnitHeight;
    }

    public void ScaleTiles(float scaleX, float scaleY)
    {
        Vector3 scale = new Vector3(scaleX, scaleY, 0);
        foreach (EditorTile selectedTile in selectedTiles)
        {
            selectedTile.transform.localScale = Vector3.zero + scale;
            selectedTile.scaleX = scaleX;
            selectedTile.scaleY = scaleY;
        }
    }

    public void AdjustTileDepth(int posIndex)
    {

    }

    public void ResetTilePositions()
    {
        foreach (EditorTile selectedTile in selectedTiles)
        {
            EditorGridPosition info = editorPositions.Find(x => x.gridPosition == selectedTile.gridPosition);
            selectedTile.moveGridX = 0;
            selectedTile.moveGridY = 0;
            selectedTile.moveWorldX = 0;
            selectedTile.moveWorldY = 0;
            selectedTile.transform.position = selectedTile.savedPosition + info.offsetY;
            info.worldPosition = info.savedWorldPosition;
            info.worldUnitHeight = info.savedWorldUnitHeight;
            tileDictionary[selectedTile.gridPosition].worldHeight = info.savedWorldUnitHeight;
        }
    }

    public void ResetTileScaling()
    {
        foreach (EditorTile selectedTile in selectedTiles)
        {
            selectedTile.scaleX = 1f;
			selectedTile.scaleY = 1f;
			selectedTile.transform.localScale = Vector3.one;
        }
    }

    public void SelectTile(EditorGridPosition info)
    {
        selectedTiles.Add(tileDictionary[info.gridPosition].GetComponent<EditorTile>());
    }
}
