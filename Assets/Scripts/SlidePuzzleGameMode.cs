using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEditor.U2D.Path;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

public class SlidePuzzleGameMode : MonoBehaviour, PuzzlePiece.Dependency
{
    public GameObject puzzlePiecePrefab;
    public GameObject PuzzlePieceInvisiblePrefab;
    public RectTransform puzzleGui;
    public Image previewGui;
    public Text revealTextGui;
    public ToggleGroup puzzleSelectionToggleGroup;
    public GameObject puzzleSelectionTogglePrefab;

    public PuzzleDefinition[] puzzleDefinitions;
    PuzzleDefinition currentPuzzleDefinition;

    [Range(2, 7)]
    public int puzzleDiagonalNum = 3;
    public int shuffleTime = 20;
    public bool shouldShuffleRealTime;
    public bool fastRealTimeShffle;

    private Dictionary<Vector2Int, PuzzlePiece> initialPieceMap;
    private Dictionary<Vector2Int, PuzzlePiece> currentPieceMap;

    private Vector2 spriteSize { get { return new Vector2(currentPuzzleDefinition.image.width / puzzleDiagonalNum, currentPuzzleDefinition.image.height / puzzleDiagonalNum); } }
    private Vector2 sizeFittingMultiplier { get { return new Vector2(Math.Min(1, spriteSize.x / spriteSize.y), Math.Min(1, spriteSize.y / spriteSize.x)); } }
    private Vector2 pieceSize { get { return new Vector2(puzzleGui.sizeDelta.x / puzzleDiagonalNum * sizeFittingMultiplier.x, puzzleGui.sizeDelta.y / puzzleDiagonalNum * sizeFittingMultiplier.y); } }

    // Start is called before the first frame update
    void Start()
    {
        ScrollRect scrollRect = puzzleSelectionToggleGroup.GetComponentInParent<ScrollRect>();
        Scrollbar scrollbar = scrollRect.GetComponentInChildren<Scrollbar>();

        for (int i = 0; i < puzzleDefinitions.Length; i++)
        {
            RectTransform toggleGroupRectTransform = puzzleSelectionToggleGroup.GetComponent<RectTransform>();
            GameObject toggleGameObject = Instantiate<GameObject>(puzzleSelectionTogglePrefab, toggleGroupRectTransform);

            RectTransform toggleRectTransform = toggleGameObject.GetComponent<RectTransform>();

            RectTransform scrollRectRectTransform = scrollRect.GetComponent<RectTransform>();
            RectTransform scrollbarRectTransform = scrollbar.GetComponent<RectTransform>();
            float toggleGroupWidth = scrollRectRectTransform.rect.width - scrollbarRectTransform.rect.width;

            toggleRectTransform.anchorMin = new Vector2(0, 1);
            toggleRectTransform.anchorMax = new Vector2(0, 1);
            toggleRectTransform.anchoredPosition = new Vector2(toggleGroupWidth / 2, - toggleGroupWidth * i);
            toggleRectTransform.sizeDelta = new Vector2(toggleGroupWidth, toggleGroupWidth);

            PuzzleDefinitionContainer container = toggleGameObject.GetComponent<PuzzleDefinitionContainer>();
            container.puzzle.image = puzzleDefinitions[i].image;
            container.puzzle.revealText = puzzleDefinitions[i].revealText;

            Image toggleImage = toggleGameObject.GetComponentInChildren<Image>();
            Vector2 imageSize = new Vector2(container.puzzle.image.width, container.puzzle.image.height);

            toggleImage.sprite = Sprite.Create(container.puzzle.image, new Rect(new Vector2(0, 0), imageSize), new Vector2(0.5f, 0.5f));

            Toggle toggle = toggleGameObject.GetComponent<Toggle>();
            toggle.group = puzzleSelectionToggleGroup;
        }

        currentPuzzleDefinition = puzzleDefinitions[0];

        StartNewGamePuzzle();
    }

    PuzzlePiece InstantiateInvisiblePiece()
    {
        GameObject invisiblePieceGo = Instantiate<GameObject>(PuzzlePieceInvisiblePrefab, puzzleGui);
        invisiblePieceGo.GetComponent<RectTransform>().sizeDelta = pieceSize;
        invisiblePieceGo.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        PuzzlePiece invisiblePiece = invisiblePieceGo.GetComponent<PuzzlePiece>();
        invisiblePiece.dependency = this;
        invisiblePiece.isInvisiblePiece = true;
        invisiblePiece.index = Vector2Int.zero;

        return invisiblePiece;
    }

    Dictionary<Vector2Int, PuzzlePiece> InstantiateVisiblePieces()
    {   
        List<Vector2Int> visiblePiecesIndices = CalcVisiblePiecesIndices();

        Dictionary<Vector2Int, PuzzlePiece> visiblePieces = new Dictionary<Vector2Int, PuzzlePiece>();
        foreach (var index in visiblePiecesIndices)
        {
            Vector3 tilePosition = new Vector2();
            tilePosition.x = pieceSize.x * index.x;
            tilePosition.y = pieceSize.y * index.y;

            GameObject visiblePieceGo = Instantiate<GameObject>(puzzlePiecePrefab, puzzleGui);
            visiblePieceGo.GetComponent<RectTransform>().sizeDelta = pieceSize;
            visiblePieceGo.GetComponent<RectTransform>().anchoredPosition = tilePosition;
            PuzzlePiece visiblePiece = visiblePieceGo.GetComponent<PuzzlePiece>();

            visiblePieceGo.GetComponent<Image>().sprite = CreateSpriteForPiece(currentPuzzleDefinition.image, index, spriteSize);
            visiblePiece.dependency = this;
            visiblePiece.index = index;
            visiblePieces.Add(index, visiblePiece);
        }

        return visiblePieces;
    }

    List<Vector2Int> CalcVisiblePiecesIndices()
    {
        List<Vector2Int> visiblePiecesIndices = new List<Vector2Int>();
        for (int x = 0; x < puzzleDiagonalNum; x++)
        {
            for (int y = 0; y < puzzleDiagonalNum; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                visiblePiecesIndices.Add(new Vector2Int(x, y));
            }
        }

        return visiblePiecesIndices;
    }

    Sprite CreateSpriteForPiece(Texture2D texture, Vector2Int pieceCoord, Vector2 spriteSize)
    {
        float xPadding = spriteSize.x * pieceCoord.x;
        float yPadding = spriteSize.y * pieceCoord.y;
        Vector2 tilePadding = new Vector2(xPadding, yPadding);

        Rect roi = new Rect(tilePadding, spriteSize);

        Vector2 pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(texture, roi, pivot);
    }

    // Update is called once per frame
    void Update()
    {
    }

    void Shuffle(PuzzlePiece invisiblePiece, int times)
    {
        PuzzlePiece usedPiece = null;
        for(int i = 0; i < times; i++)
        {
            usedPiece = RandomMovement(invisiblePiece, usedPiece);
        }
    }

    IEnumerator RealtimeShuffle(PuzzlePiece invisiblePiece, int times)
    {
        PuzzlePiece usedPiece = null;
        for (int i = 0; i < times; i++)
        {
            if(fastRealTimeShffle)
                yield return new WaitForSeconds(0.2f);
            else
                yield return new WaitForSeconds(1.0f);

            usedPiece = RandomMovement(invisiblePiece, usedPiece);
        }
    }

    List<Vector2Int> GetNeighborDirections()
    {
        List<Vector2Int> neighborDirections = new List<Vector2Int>();
        neighborDirections.Add(Vector2Int.up);
        neighborDirections.Add(Vector2Int.down);
        neighborDirections.Add(Vector2Int.left);
        neighborDirections.Add(Vector2Int.right);

        return neighborDirections;
    }

    List<Vector2Int> GetNeighborIndices(PuzzlePiece piece)
    {
        List<Vector2Int> neighborDirections = GetNeighborDirections();

        List<Vector2Int> neighborIndices = new List<Vector2Int>();
        foreach (var direction in neighborDirections)
            neighborIndices.Add(piece.index + direction);

        return neighborIndices;
    }

    List<PuzzlePiece> GetNeighbors(PuzzlePiece piece)
    {
        List<Vector2Int> neighborIndices = GetNeighborIndices(piece);

        List<PuzzlePiece> neighbors = new List<PuzzlePiece>();
        foreach(var index in neighborIndices)
        {
            PuzzlePiece neighbor;
            if (currentPieceMap.TryGetValue(index, out neighbor))
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    PuzzlePiece RandomMovement(PuzzlePiece invisiblePiece, PuzzlePiece exceptPiece)
    {
        List<PuzzlePiece> selectablePieces = GetNeighbors(invisiblePiece);
        selectablePieces.Remove(exceptPiece);

        int randomIdx = UnityEngine.Random.Range(0, selectablePieces.Count);
        PuzzlePiece selectedPiece = selectablePieces[randomIdx];

        selectedPiece.HandleClickEvent ();

        return selectedPiece;
    }

    void StartNewGamePuzzle()
    {
        if (initialPieceMap != null)
        {
            foreach (var piece in initialPieceMap.Values)
            {
                GameObject.Destroy(piece.gameObject);
            }
        }

        PuzzlePiece invisiblePiece = InstantiateInvisiblePiece();
        Dictionary<Vector2Int, PuzzlePiece> visiblePieces = InstantiateVisiblePieces();

        initialPieceMap = new Dictionary<Vector2Int, PuzzlePiece>(visiblePieces);
        initialPieceMap.Add(Vector2Int.zero, invisiblePiece);

        currentPieceMap = new Dictionary<Vector2Int, PuzzlePiece>(initialPieceMap);

        Rect previewImageRect = new Rect(0, 0, currentPuzzleDefinition.image.width, currentPuzzleDefinition.image.height);
        previewGui.sprite = Sprite.Create(currentPuzzleDefinition.image, previewImageRect, new Vector2(.5f, .5f));

        if (shouldShuffleRealTime)
            StartCoroutine(RealtimeShuffle(invisiblePiece, shuffleTime));
        else
            Shuffle(invisiblePiece, shuffleTime);

        revealTextGui.text = "";
    }

    bool CheckCompleted()
    {
        bool isDifferent = false;
        foreach(var key in currentPieceMap.Keys)
        {
            if(currentPieceMap[key] != initialPieceMap[key])
                isDifferent = true;
        }

        if (isDifferent)
            return false;
        else
            return true;
    }

    Vector2 PuzzlePiece.Dependency.GetPieceSize()
    {
        return pieceSize;
    }
    void PuzzlePiece.Dependency.HandleInput(PuzzlePiece inputPiece)
    {
        List<Vector2Int> neighborDirections = GetNeighborDirections();
        foreach (var direction in neighborDirections)
        {
            Vector2Int neighborIndex = inputPiece.index + direction;
            PuzzlePiece targetPiece;
            if (currentPieceMap.TryGetValue(neighborIndex, out targetPiece) && targetPiece.isInvisiblePiece)
            {
                PuzzlePiece.SwapIndex(inputPiece, targetPiece);
                currentPieceMap[inputPiece.index] = inputPiece;
                currentPieceMap[targetPiece.index] = targetPiece;
                inputPiece.MoveRelative(direction);
                targetPiece.MoveRelative(direction * -1);
                break;
            }
        }

        if (CheckCompleted())
        {
            revealTextGui.text = currentPuzzleDefinition.revealText;
        }
    }

    public void OnClickNextPuzzleButton()
    {
        Toggle activateToggle = puzzleSelectionToggleGroup.GetFirstActiveToggle();
        PuzzleDefinitionContainer container = activateToggle.GetComponent<PuzzleDefinitionContainer>();
        currentPuzzleDefinition.image = container.puzzle.image;
        currentPuzzleDefinition.revealText = container.puzzle.revealText;

        StartNewGamePuzzle();
    }
}
