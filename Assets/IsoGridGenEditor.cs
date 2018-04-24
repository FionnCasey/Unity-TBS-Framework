using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(IsoGridGenerator))]
public class IsoGridGenEditor : Editor {

	private IsoGridGenerator gridGenerator;
	private SelectionInfo selectionInfo;

	private bool needsRepaint;
	private int tabIndex;
	private int tileIndex;
	private int removeAtIndex = -1;

	private string[] tabLabels = new string[] {"Grid", "Display", "Tile List", "Tile Editor"};
	private string[] numLayerLabels = new string[] {"Single", "Double"};
	private string[] placementlayerLabels = new string[] {"Top", "Bottom"};
	private string[] isoRatioLabels = new string[] {"2:1", "3:1"};

	private GUIStyle boxStyle;
	private GUIStyle innerBoxStyle;
	private GUIStyle richTextStyle;

	private const float selectedHandleSize = 2f;

	private KeyCode[] tileHotkeys = 
	{
		KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
		KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
	};

	void OnEnable()
	{
		gridGenerator = (IsoGridGenerator)target;
		selectionInfo = new SelectionInfo();
		gridGenerator.selectedTiles.Clear();
		selectionInfo.groupSelection.Clear();
	}

	public override void OnInspectorGUI()
	{
		SetStyles();

		GUILayout.BeginVertical("Box");
		GUILayout.BeginVertical(boxStyle);
		tabIndex = GUILayout.Toolbar(tabIndex, tabLabels);
		switch (tabIndex)
		{
			case 0:
			DrawGridSettings();
			break;

			case 1:
			DrawEditorSettings();
			break;

			case 2:
			DrawTileSettings();
			break;

			case 3:
			GUILayout.Space(10);
			if (gridGenerator.selectedTiles.Count == 0)
			{
				GUILayout.Label("<b><color=#a52a2aff>No Tile Selected</color></b>", richTextStyle);
			}
			else
			{
				DrawTransformEditor();
				DrawExtrudePanel();
			}
			break;
		}
		GUILayout.EndVertical();
		GUILayout.EndVertical();
	}

	void OnSceneGUI()
	{
		Event guiEvent = Event.current;

		if (guiEvent.type == EventType.Repaint)
		{
			DrawEditorGrid();
			DrawTileInfoToScreen();
		}
		else if (guiEvent.type == EventType.layout)
		{
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
		}
		else if (guiEvent.type == EventType.KeyDown)
		{
			HandleTileHotkeys(guiEvent);
		}
		else
		{
			HandleMouseInput(guiEvent);
			if (needsRepaint)
			{
				HandleUtility.Repaint();
			}
		}
	}

	private void HandleTileHotkeys(Event guiEvent)
	{
		for (int i = 0; i < tileHotkeys.Length; i++)
		{
			if (guiEvent.keyCode == tileHotkeys[i])
			{
				SetTileIndex(i == 9 ? 0 : i);
				guiEvent.Use();
			}
		}
	}

	private void HandleMouseInput(Event guiEvent)
	{
		Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
		Vector3 mousePosition = mouseRay.origin;
		if (guiEvent.type == EventType.mouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
		{
			HandleLeftMouseDown();
		}
		else if (guiEvent.type == EventType.mouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Shift)
		{
			HandleShiftLeftMouseDown();
		}
		else if (guiEvent.type == EventType.mouseDown && guiEvent.button == 1)
		{
			HandleRightMouseDown();
		}
		else
		{
			UpdateMouseOverSelection(mousePosition);
		}
	}

	private void UpdateMouseOverSelection(Vector3 mousePosition)
	{
		int mouseOverPointIndex = -1;
		for (int i = 0; i < gridGenerator.editorPositions.Count; i++)
		{
			if (Vector2.Distance(mousePosition, gridGenerator.editorPositions[i].worldPosition) < gridGenerator.handleColliderSize)
			{
				mouseOverPointIndex = i;
				break;
			}
		}
		if (mouseOverPointIndex != selectionInfo.mouseOverIndex)
        {
            selectionInfo.mouseOverIndex = mouseOverPointIndex;
            selectionInfo.mouseIsOverHandle = mouseOverPointIndex != -1;
            needsRepaint = true;
        }
	}

	private void HandleLeftMouseDown()
	{
		if (selectionInfo.mouseIsOverHandle)
		{
			if (gridGenerator.editorPositions[selectionInfo.mouseOverIndex].handleType == 0)
			{
				gridGenerator.PlaceTile(gridGenerator.editorPositions[selectionInfo.mouseOverIndex], tileIndex);
				Deselect();
			}
			else 
			{
				Deselect();
				selectionInfo.selectedHandleIndex = selectionInfo.mouseOverIndex;
				gridGenerator.SelectTile(gridGenerator.editorPositions[selectionInfo.selectedHandleIndex]);
				selectionInfo.handleIsSelected = true;
			}
			needsRepaint = true;
		}
		else
		{
			Deselect();
		}
	}

	private void HandleRightMouseDown()
	{
		Deselect();
	}

	private void Deselect()
	{
		gridGenerator.selectedTiles.Clear();
		selectionInfo.groupSelection.Clear();
		selectionInfo.selectedHandleIndex = -1;
		selectionInfo.handleIsSelected = false;
		needsRepaint = true;
	}

	private void HandleShiftLeftMouseDown()
	{
		if (selectionInfo.mouseIsOverHandle)
		{
			if (gridGenerator.editorPositions[selectionInfo.mouseOverIndex].handleType == 1
				&& selectionInfo.handleIsSelected)
			{
				gridGenerator.SelectTile(gridGenerator.editorPositions[selectionInfo.mouseOverIndex]);
				selectionInfo.groupSelection.Add(selectionInfo.mouseOverIndex);
			}
		}
	}

	private void DrawEditorGrid()
	{
		for (int i = 0; i < gridGenerator.editorPositions.Count; i++)
		{
			Handles.color = i == selectionInfo.selectedHandleIndex ? gridGenerator.gridSelected :
				i == selectionInfo.mouseOverIndex ? gridGenerator.gridMouseOver : gridGenerator.gridDefault;

			Handles.color = i == selectionInfo.selectedHandleIndex ? gridGenerator.gridSelected :
				selectionInfo.groupSelection.Contains(i) ? gridGenerator.tileGroupSelection : Handles.color;

			Vector3 center = gridGenerator.editorPositions[i].worldPosition;
			Vector3[] top = new Vector3[5]
			{
				new Vector3(center.x, center.y + (gridGenerator.scaledHeight * .5f) + (gridGenerator.scaledHeight * .5f), center.z),
				new Vector3(center.x + gridGenerator.scaledWidth * .5f, center.y + gridGenerator.scaledHeight * .5f, center.z),
				new Vector3(center.x, center.y - (gridGenerator.scaledHeight * .5f) + (gridGenerator.scaledHeight * .5f), center.z),
				new Vector3(center.x - gridGenerator.scaledWidth * .5f, center.y + gridGenerator.scaledHeight * .5f, center.z),
				new Vector3(center.x, center.y + (gridGenerator.scaledHeight * .5f) + (gridGenerator.scaledHeight * .5f), center.z)
			};
			Vector3[] middle = new Vector3[5]
			{
				new Vector3(center.x, center.y + gridGenerator.scaledHeight * .5f, center.z),
				new Vector3(center.x + gridGenerator.scaledWidth * .5f, center.y, center.z),
				new Vector3(center.x, center.y - gridGenerator.scaledHeight * .5f, center.z),
				new Vector3(center.x - gridGenerator.scaledWidth * .5f, center.y, center.z),
				new Vector3(center.x, center.y + gridGenerator.scaledHeight * .5f, center.z)
			};
			Vector3[] bottom = new Vector3[5]
			{
				new Vector3(center.x, center.y + (gridGenerator.scaledHeight * .5f) - (gridGenerator.scaledHeight * .5f), center.z),
				new Vector3(center.x + gridGenerator.scaledWidth * .5f, center.y - gridGenerator.scaledHeight * .5f, center.z),
				new Vector3(center.x, center.y - (gridGenerator.scaledHeight * .5f) - (gridGenerator.scaledHeight * .5f), center.z),
				new Vector3(center.x - gridGenerator.scaledWidth * .5f, center.y - gridGenerator.scaledHeight * .5f, center.z),
				new Vector3(center.x, center.y + (gridGenerator.scaledHeight * .5f) - (gridGenerator.scaledHeight * .5f), center.z)
			};
			
			if (gridGenerator.outlineViewMode != IsoGridGenerator.OutlineViewMode.None)
			{
				Handles.DrawAAPolyLine(bottom);
			}
			if (gridGenerator.outlineViewMode == IsoGridGenerator.OutlineViewMode.SplitLayers||
				gridGenerator.outlineViewMode == IsoGridGenerator.OutlineViewMode.SingleLayer)
			{
				if (gridGenerator.outlineViewMode == IsoGridGenerator.OutlineViewMode.SingleLayer)
				{
					Handles.DrawLine(bottom[0], middle[0]);
					Handles.DrawLine(bottom[1], middle[1]);
					Handles.DrawLine(bottom[2], middle[2]);
					Handles.DrawLine(bottom[3], middle[3]);
				}
				Handles.DrawAAPolyLine(middle);
			}
			if (gridGenerator.outlineViewMode == IsoGridGenerator.OutlineViewMode.DoubleLayer ||
				gridGenerator.outlineViewMode == IsoGridGenerator.OutlineViewMode.SplitLayers)
			{
				Handles.DrawLine(top[0], bottom[0]);
				Handles.DrawLine(top[1], bottom[1]);
				Handles.DrawLine(top[2], bottom[2]);
				Handles.DrawLine(top[3], bottom[3]);
				Handles.DrawAAPolyLine(top);
			}

			Handles.color = i == selectionInfo.mouseOverIndex ? gridGenerator.handleMouseOver :
				i == selectionInfo.selectedHandleIndex ? gridGenerator.handleSelected :
				gridGenerator.editorPositions[i].handleType == 1 ? gridGenerator.handleActive : gridGenerator.handleDefault;
			Handles.DrawSolidDisc(center, Vector3.back, gridGenerator.handleSize);

			if (i == selectionInfo.mouseOverIndex || i == selectionInfo.selectedHandleIndex)
			{
				Handles.DrawWireDisc(center, Vector3.back, gridGenerator.handleSize * 2.5f);
			}
		}
		needsRepaint = false;
	}

	private void DrawGridSettings()
	{
		GUILayout.Space(15);
		GUILayout.Label("Stage Name", EditorStyles.boldLabel);
		gridGenerator.stageName = EditorGUILayout.TextField(gridGenerator.stageName);
		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Initial Grid Width", EditorStyles.boldLabel);
		GUILayout.Label("Initial Grid Length", EditorStyles.boldLabel);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		gridGenerator.startWidth = EditorGUILayout.IntField(gridGenerator.startWidth);
		gridGenerator.startLength = EditorGUILayout.IntField(gridGenerator.startLength);
		GUILayout.EndHorizontal();
		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Tile Pixel Width", EditorStyles.boldLabel);
		GUILayout.Space(35);
		GUILayout.Label("Isometric Aspect Ratio", EditorStyles.boldLabel);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		gridGenerator.pixelWidth = EditorGUILayout.FloatField(gridGenerator.pixelWidth, GUILayout.MaxWidth(Screen.width * .5f));
		gridGenerator.widthToHeightRatio = GUILayout.Toolbar(
				gridGenerator.widthToHeightRatio - 2, isoRatioLabels, EditorStyles.miniButton, GUILayout.MaxWidth(Screen.width * .5f), GUILayout.Height(17)) + 2;
		GUILayout.EndHorizontal();
		GUILayout.Space(15);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Units per Pixel", EditorStyles.boldLabel);
		gridGenerator.unitsPerPixel = EditorGUILayout.Slider(gridGenerator.unitsPerPixel, 1f, 100f);
		GUILayout.EndHorizontal();
		GUILayout.Space(15);

		if (GUILayout.Button("Create New Grid"))
		{
			gridGenerator.CreateNewGrid();
			needsRepaint = true;
		}
		GUILayout.Space(5);
	}

	public void DrawEditorSettings()
	{
		GUILayout.Space(5);
		GUILayout.Label("Editor Settings", EditorStyles.boldLabel);
		GUILayout.Space(10);
		gridGenerator.outlineViewMode = (IsoGridGenerator.OutlineViewMode)EditorGUILayout.EnumPopup("View Mode", gridGenerator.outlineViewMode);

		GUILayout.Label("Grid Outline Colours", EditorStyles.boldLabel);
		gridGenerator.gridDefault = EditorGUILayout.ColorField("Default", gridGenerator.gridDefault);
		gridGenerator.gridMouseOver = EditorGUILayout.ColorField("Mouse Over", gridGenerator.gridMouseOver);
		gridGenerator.gridSelected = EditorGUILayout.ColorField("Selected", gridGenerator.gridSelected);

		GUILayout.Label("Handle Colours", EditorStyles.boldLabel);
		gridGenerator.handleDefault = EditorGUILayout.ColorField("Default", gridGenerator.handleDefault);
		gridGenerator.handleActive = EditorGUILayout.ColorField("Active", gridGenerator.handleActive);
		gridGenerator.handleMouseOver = EditorGUILayout.ColorField("Mouse Over", gridGenerator.handleMouseOver);
		gridGenerator.handleSelected = EditorGUILayout.ColorField("Selected", gridGenerator.handleSelected);
		gridGenerator.tileGroupSelection = EditorGUILayout.ColorField("Tile Editor Handles", gridGenerator.tileGroupSelection);

		GUILayout.Label("Handle Settings", EditorStyles.boldLabel);
		gridGenerator.handleSize = EditorGUILayout.Slider("Handle Size", gridGenerator.handleSize, 0, 1f);
		gridGenerator.handleColliderSize = EditorGUILayout.Slider("Handle Collider Size", gridGenerator.handleColliderSize, 0, 2f);
	}

	private void DrawTileSettings()
	{
		GUILayout.Space(15);
		GUILayout.BeginHorizontal();
		if (gridGenerator.tileInfo.Count == 0)
		{
			GUILayout.Label("No Tiles Added");
		}
		else 
		{
			GUILayout.Label("Number of Tiles: " + gridGenerator.tileInfo.Count);
		}
		if (GUILayout.Button("Add Tile"))
		{
			gridGenerator.tileInfo.Add(new Tile2DInfo());
		}
		if (GUILayout.Button("Remove All"))
		{
			gridGenerator.tileInfo.Clear();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		removeAtIndex = -1;
		for(int i = 0; i < gridGenerator.tileInfo.Count; i++)
		{
			GUILayout.BeginVertical("Box");
			GUILayout.BeginVertical(innerBoxStyle);

			GUILayout.BeginHorizontal();
			string tileName = string.Format(
				"<b>#{0}</b>  {1}", i + 1, gridGenerator.tileInfo[i].sprite != null ? gridGenerator.tileInfo[i].sprite.name : "Sprite Not Def"
			);
			GUILayout.Label(tileName, richTextStyle);
			if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
			{
				removeAtIndex = i;
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5);

			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical(GUILayout.MaxWidth(Screen.width * .5f - 20f));
			GUILayout.BeginHorizontal();
			GUILayout.Label("Tile Type");
			gridGenerator.tileInfo[i].tileType = (GridTile.TileType)EditorGUILayout.EnumPopup(
				gridGenerator.tileInfo[i].tileType, GUILayout.MaxWidth(Screen.width * .5f - 100f)
			);
			GUILayout.EndHorizontal();
			GUILayout.Space(5);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Layer Type");
			gridGenerator.tileInfo[i].numLayers = GUILayout.Toolbar(
				gridGenerator.tileInfo[i].numLayers - 1, numLayerLabels, EditorStyles.miniButton, GUILayout.MaxWidth(Screen.width * .5f - 100f)) + 1;
			GUILayout.EndHorizontal();
			GUILayout.Space(5);

			if (gridGenerator.tileInfo[i].numLayers == 1)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Placement Layer");
				gridGenerator.tileInfo[i].placementLayer = GUILayout.Toolbar(gridGenerator.tileInfo[i].placementLayer - 1, placementlayerLabels, EditorStyles.miniButton) + 1;
				GUILayout.EndHorizontal();
				GUILayout.Space(5);
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Space(2);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			gridGenerator.tileInfo[i].sprite = (Sprite)EditorGUILayout.ObjectField(
				gridGenerator.tileInfo[i].sprite, typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64)
			);
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUILayout.EndVertical();
			if (i != gridGenerator.tileInfo.Count - 1)
			{
				GUILayout.Space(-5);
			}
		}
		if (removeAtIndex != -1)
		{
			gridGenerator.tileInfo.RemoveAt(removeAtIndex);
			removeAtIndex = -1;
		}
	}

	private void SetTileIndex(int index)
	{
		tileIndex = Mathf.Clamp(index, 0, gridGenerator.tileInfo.Count - 1);
	}

	private void DrawTileInfoToScreen()
	{
		if (tabIndex == 2)
		{
			string tileName = gridGenerator.tileInfo[tileIndex].sprite == null ? "<color=#F0C328FF>Sprite Not Defined</color>" : gridGenerator.tileInfo[tileIndex].sprite.name;
			string label = string.Format(
				"<size=15><color=#C8C8C8FF>[{1}] Current Tile: {0}</color></size>", tileName, tileIndex + 1);
			Handles.Label(Camera.current.ViewportToWorldPoint(new Vector3(.02f, .97f, 0)), label, richTextStyle);
		}
	}

	private void DrawTransformEditor()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Adjust Position", EditorStyles.boldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Reset", EditorStyles.miniButton))
		{
			gridGenerator.ResetTilePositions();
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(5);

		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		EditorGUI.BeginChangeCheck();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Grid X");
		gridGenerator.selectedTiles[0].moveGridX = EditorGUILayout.Slider(gridGenerator.selectedTiles[0].moveGridX, -1f, 1f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Grid Y");
		gridGenerator.selectedTiles[0].moveGridY = EditorGUILayout.Slider(gridGenerator.selectedTiles[0].moveGridY, -1f, 1f);
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();

		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		GUILayout.Label("World X");
		gridGenerator.selectedTiles[0].moveWorldX = EditorGUILayout.Slider(gridGenerator.selectedTiles[0].moveWorldX, -1f, 1f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("World Y");
		gridGenerator.selectedTiles[0].moveWorldY = EditorGUILayout.Slider(gridGenerator.selectedTiles[0].moveWorldY, -1f, 1f);
		GUILayout.EndHorizontal();
		if (EditorGUI.EndChangeCheck())
		{
			gridGenerator.AdjustTilePositions();
		}
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Adjust Scale", EditorStyles.boldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Reset", EditorStyles.miniButton))
		{
			gridGenerator.ResetTileScaling();
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(5);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Scale X");
		EditorGUI.BeginChangeCheck();
		gridGenerator.selectedTiles[0].scaleX = EditorGUILayout.Slider(gridGenerator.selectedTiles[0].scaleX, 0, 2f);
		GUILayout.Label("Scale Y");
		gridGenerator.selectedTiles[0].scaleY = EditorGUILayout.Slider(gridGenerator.selectedTiles[0].scaleY, 0, 2f);
		GUILayout.EndHorizontal();
		if (EditorGUI.EndChangeCheck())
		{
			gridGenerator.ScaleTiles(gridGenerator.selectedTiles[0].scaleX, gridGenerator.selectedTiles[0].scaleY);
		}
		GUILayout.Space(10);
	}

	private void DrawExtrudePanel()
	{
		if (GUILayout.Button("Extrude"))
		{
			gridGenerator.ExtrudeTiles(Vector3.up);
		}
	}

	private void SetStyles()
	{
		if (boxStyle == null)
		{
			boxStyle = new GUIStyle();
			boxStyle.padding = new RectOffset(10, 10, 10, 10);
		}
		if (innerBoxStyle == null)
		{
			innerBoxStyle = new GUIStyle();
			innerBoxStyle.padding = new RectOffset(5, 5, 5, 5);
		}
		if (richTextStyle == null)
		{
			richTextStyle = new GUIStyle();
			richTextStyle.richText = true;
		}
	}

	public class SelectionInfo {

		public int mouseOverIndex = -1;
		public int selectedHandleIndex = -1;
		public bool mouseIsOverHandle;
		public bool handleIsSelected;
		public List<int> groupSelection = new List<int>();
	}
}
