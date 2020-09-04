using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Path;
using UnityEngine;
using UnityEngine.UIElements;

public class SlidePuzzleGameMode : MonoBehaviour, PuzzlePiece.Dependency
{
    [Range(2, 7)]
    public int puzzleDiagonalNum = 3;
    [Range(1f, 20f)]
    public float puzzleSize = 10;
    public Texture2D imageTexture;
    public SpriteRenderer preview;
    public GameObject puzzlePrefab;
    public GameObject emptyPuzzlePrefab;
    public int shuffleTime = 20;
    public bool shouldShuffleRealTime;
    public bool fastRealTimeShffle;

    private Dictionary<Vector2Int, PuzzlePiece> initialPieceMap;
    private Dictionary<Vector2Int, PuzzlePiece> currentPieceMap;

    private Vector2 spriteSize { get { return new Vector2(imageTexture.width / puzzleDiagonalNum, imageTexture.height / puzzleDiagonalNum); } }
    private Vector2 SizeFittingMultiplier { get { return new Vector2(Math.Min(1, spriteSize.x / spriteSize.y), Math.Min(1, spriteSize.y / spriteSize.x)); } }
    private Vector2 pieceSize { get { return new Vector2(puzzleSize / puzzleDiagonalNum * SizeFittingMultiplier.x, puzzleSize / puzzleDiagonalNum * SizeFittingMultiplier.y); } }

    // Start is called before the first frame update
    void Start()
    {

        PuzzlePiece invisiblePiece = InstantiateInvisiblePiece();
        Dictionary<Vector2Int, PuzzlePiece> visiblePieces = InstantiateVisiblePieces();

        initialPieceMap = new Dictionary<Vector2Int, PuzzlePiece>(visiblePieces);
        initialPieceMap.Add(Vector2Int.zero, invisiblePiece);

        currentPieceMap = new Dictionary<Vector2Int, PuzzlePiece>(initialPieceMap);

        ConnectEachPieces(initialPieceMap);

        Rect previewImageRect = new Rect(0, 0, imageTexture.width, imageTexture.height);
        preview.sprite = Sprite.Create(imageTexture, previewImageRect, new Vector2(.5f, .5f));

        if (shouldShuffleRealTime)
            StartCoroutine(RealtimeShuffle(invisiblePiece, shuffleTime));
        else
            Shuffle(invisiblePiece, shuffleTime);
    }

    PuzzlePiece InstantiateInvisiblePiece()
    {
        GameObject invisiblePieceGo = Instantiate<GameObject>(emptyPuzzlePrefab, Vector3.zero, Quaternion.identity);
        PuzzlePiece invisiblePiece = invisiblePieceGo.GetComponent<PuzzlePiece>();
        invisiblePiece.dependency = this;
        invisiblePiece.isInvisiblePiece = true;

        return invisiblePiece;
    }

    Dictionary<Vector2Int, PuzzlePiece> InstantiateVisiblePieces()
    {
        float pixelsPerUnit = Math.Max(spriteSize.x / pieceSize.x, spriteSize.y / pieceSize.y);
        
        List<Vector2Int> visiblePiecesIndices = CalcVisiblePiecesIndices();

        Dictionary<Vector2Int, PuzzlePiece> visiblePieces = new Dictionary<Vector2Int, PuzzlePiece>();
        foreach (var index in visiblePiecesIndices)
        {
            Vector3 tilePosition = new Vector3();
            tilePosition.x = pieceSize.x * index.x;
            tilePosition.y = pieceSize.y * index.y;

            GameObject visibleTileGo = Instantiate<GameObject>(puzzlePrefab, tilePosition, Quaternion.identity);

            visibleTileGo.GetComponent<BoxCollider2D>().size = pieceSize;
            visibleTileGo.GetComponent<SpriteRenderer>().sprite = CreateSpriteForPiece(imageTexture, index, spriteSize, pixelsPerUnit);
            visibleTileGo.GetComponent<PuzzlePiece>().dependency = this;
            visiblePieces.Add(index, visibleTileGo.GetComponent<PuzzlePiece>());
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

    Sprite CreateSpriteForPiece(Texture2D texture, Vector2Int pieceCoord, Vector2 spriteSize, float pixelsPerUnit)
    {
        float xPadding = spriteSize.x * pieceCoord.x;
        float yPadding = spriteSize.y * pieceCoord.y;
        Vector2 tilePadding = new Vector2(xPadding, yPadding);

        Rect roi = new Rect(tilePadding, spriteSize);

        Vector2 pivot = new Vector2(.5f, .5f);
        return Sprite.Create(texture, roi, pivot, pixelsPerUnit);
    }

    void ConnectEachPieces(Dictionary<Vector2Int, PuzzlePiece> pieceMap)
    {
        for (int x = 0; x < puzzleDiagonalNum; x++)
        {
            for (int y = 0; y < puzzleDiagonalNum; y++)
            {
                Vector2Int idx = new Vector2Int(x, y);

                Vector2Int topIdx = new Vector2Int(x, y + 1);
                Vector2Int bottomIdx = new Vector2Int(x, y - 1);
                Vector2Int leftIdx = new Vector2Int(x - 1, y);
                Vector2Int rightIdx = new Vector2Int(x + 1, y);

                if (pieceMap.ContainsKey(topIdx))
                    pieceMap[idx].top = pieceMap[topIdx];
                if (pieceMap.ContainsKey(bottomIdx))
                    pieceMap[idx].bottom = pieceMap[bottomIdx];
                if (pieceMap.ContainsKey(leftIdx))
                    pieceMap[idx].left = pieceMap[leftIdx];
                if (pieceMap.ContainsKey(rightIdx))
                    pieceMap[idx].right = pieceMap[rightIdx];
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    void Shuffle(PuzzlePiece emptyPiece, int times)
    {
        PuzzlePiece usedPiece = null;
        for(int i = 0; i < times; i++)
        {
            usedPiece = RandomMovement(emptyPiece, usedPiece);
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

    PuzzlePiece RandomMovement(PuzzlePiece invisiblePiece, PuzzlePiece exceptPiece)
    {
        List<PuzzlePiece> selectablePieces = new List<PuzzlePiece>();
        if (invisiblePiece.top && invisiblePiece.top != exceptPiece)
            selectablePieces.Add(invisiblePiece.top);
        if (invisiblePiece.bottom && invisiblePiece.bottom != exceptPiece)
            selectablePieces.Add(invisiblePiece.bottom);
        if (invisiblePiece.left && invisiblePiece.left != exceptPiece)
            selectablePieces.Add(invisiblePiece.left);
        if (invisiblePiece.right && invisiblePiece.right != exceptPiece)
            selectablePieces.Add(invisiblePiece.right);

        System.Random random = new System.Random();
        int randomIdx = random.Next(selectablePieces.Count);
        Debug.Log(selectablePieces.Count);
        PuzzlePiece selectedPiece = selectablePieces[randomIdx];

        selectedPiece.OnMouseDown();

        return selectedPiece;
    }

    void StartNewPuzzle()
    {
        Debug.Log("Delete All puzzles. create new one. set new sprite. reorder connectivity. check destruction.");
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

    public Vector2 GetPieceSize()
    {
        return pieceSize;
    }
}
