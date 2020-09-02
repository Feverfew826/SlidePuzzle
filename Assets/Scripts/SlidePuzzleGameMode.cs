using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Path;
using UnityEngine;
using UnityEngine.UIElements;

public class SlidePuzzleGameMode : MonoBehaviour
{
    public int puzzleDiagonalNum;
    public float puzzleSize;
    public Texture2D imageTexture;
    public SpriteRenderer preview;
    public GameObject puzzlePrefab;
    public GameObject emptyPuzzlePrefab;
    public int shuffleTime;
    public bool shouldShuffleRealTime;
    public bool fastRealTimeShffle;

    private class PieceDependency : PuzzlePiece.Dependency
    {
        public Vector2 tileSize;
        public override Vector2 GetTileSize()
        {
            return tileSize;
        }
    }
    private PieceDependency pieceDependency;

    private struct TileCreationUnit
    {
        public Rect rect;
        public PuzzlePiece piece;
        public SpriteRenderer renderer;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (puzzleDiagonalNum < 2)
            puzzleDiagonalNum = 2;

        float tileImageWidth = imageTexture.width / puzzleDiagonalNum;
        float tileImageHeight = imageTexture.height / puzzleDiagonalNum;
        Vector2 tileImageSize = new Vector2(tileImageWidth, tileImageHeight);

        Vector2 ratio = new Vector2();
        ratio.x = Math.Min(1, tileImageWidth / tileImageHeight);
        ratio.y = Math.Min(1, tileImageHeight / tileImageWidth);

        if (puzzleSize < 1)
            puzzleSize = 1;

        Vector2 tileSize = new Vector2();
        tileSize.x = puzzleSize / puzzleDiagonalNum * ratio.x;
        tileSize.y = puzzleSize / puzzleDiagonalNum * ratio.y;

        pieceDependency = new PieceDependency();
        pieceDependency.tileSize = tileSize;

        Quaternion tileRotation = Quaternion.identity;

        PuzzlePiece emptyPiece = null;
        List<TileCreationUnit> creationUnits = new List<TileCreationUnit>();
        Dictionary<KeyValuePair<int, int>, PuzzlePiece> pieceMap = new Dictionary<KeyValuePair<int, int>, PuzzlePiece>();
        for (int x = 0; x < puzzleDiagonalNum; x++)
        {
            for (int y = 0; y < puzzleDiagonalNum; y++)
            {
                TileCreationUnit unit = new TileCreationUnit();
                if (x == 0 && y == 0)
                {
                    Vector3 tilePosition = new Vector3();
                    GameObject newObject = Instantiate<GameObject>(emptyPuzzlePrefab, tilePosition, tileRotation);

                    emptyPiece = newObject.GetComponent<PuzzlePiece>();
                    emptyPiece.isEmptyPiece = true;
                    unit.piece = emptyPiece;
                }
                else
                {
                    float xPadding = tileImageWidth * x;
                    float yPadding = tileImageHeight * y;
                    Vector2 tilePadding = new Vector2(xPadding, yPadding);
                    unit.rect = new Rect(tilePadding, tileImageSize);

                    Vector3 tilePosition = new Vector3();
                    tilePosition.x = tileSize.x * x;
                    tilePosition.y = tileSize.y * y;
                    GameObject newObject = Instantiate<GameObject>(puzzlePrefab, tilePosition, tileRotation);
                    newObject.GetComponent<BoxCollider2D>().size = tileSize;

                    unit.piece = newObject.GetComponent<PuzzlePiece>();
                    unit.renderer = newObject.GetComponent<SpriteRenderer>();

                    creationUnits.Add(unit);
                }

                unit.piece.dependency = pieceDependency;

                KeyValuePair<int, int> idx = new KeyValuePair<int, int>(x, y);
                pieceMap.Add(idx, unit.piece);
            }
        }
        Rect previewImageRect = new Rect(0, 0, imageTexture.width, imageTexture.height);
        preview.sprite = Sprite.Create(imageTexture, previewImageRect, new Vector2(.5f, .5f));

        SetSprite(creationUnits, tileImageSize, tileSize);
        ConnectEachPieces(pieceMap);

        if(shouldShuffleRealTime)
            StartCoroutine(RealtimeShuffle(emptyPiece, shuffleTime));
        else
            Shuffle(emptyPiece, shuffleTime);

        Debug.Log("EndOfStart");
    }

    void SetSprite(List<TileCreationUnit> creationUnits, Vector2 tileImageSize, Vector2 tileSize)
    {
        
        float pixelsPerUnit = Math.Max(tileImageSize.x / tileSize.x, tileImageSize.y / tileSize.y);
        foreach (var unit in creationUnits)
        {
            Vector2 pivot = new Vector2(.5f, .5f);
            Sprite newSprite = Sprite.Create(imageTexture, unit.rect, pivot, pixelsPerUnit);

            unit.renderer.sprite = newSprite;
        }
    }

    void ConnectEachPieces(Dictionary<KeyValuePair<int, int>, PuzzlePiece> pieceMap)
    {
        for (int x = 0; x < puzzleDiagonalNum; x++)
        {
            for (int y = 0; y < puzzleDiagonalNum; y++)
            {
                KeyValuePair<int, int> idx = new KeyValuePair<int, int>(x, y);

                KeyValuePair<int, int> topIdx = new KeyValuePair<int, int>(x, y + 1);
                KeyValuePair<int, int> bottomIdx = new KeyValuePair<int, int>(x, y - 1);
                KeyValuePair<int, int> leftIdx = new KeyValuePair<int, int>(x - 1, y);
                KeyValuePair<int, int> rightIdx = new KeyValuePair<int, int>(x + 1, y);

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

    IEnumerator RealtimeShuffle(PuzzlePiece emptyPiece, int times)
    {
        PuzzlePiece usedPiece = null;
        for (int i = 0; i < times; i++)
        {
            if(fastRealTimeShffle)
                yield return new WaitForSeconds(0.2f);
            else
                yield return new WaitForSeconds(1.0f);

            usedPiece = RandomMovement(emptyPiece, usedPiece);
        }
    }

    PuzzlePiece RandomMovement(PuzzlePiece emptyPiece, PuzzlePiece exceptPiece)
    {
        List<PuzzlePiece> selectablePieces = new List<PuzzlePiece>();
        if (emptyPiece.top && emptyPiece.top != exceptPiece)
            selectablePieces.Add(emptyPiece.top);
        if (emptyPiece.bottom && emptyPiece.bottom != exceptPiece)
            selectablePieces.Add(emptyPiece.bottom);
        if (emptyPiece.left && emptyPiece.left != exceptPiece)
            selectablePieces.Add(emptyPiece.left);
        if (emptyPiece.right && emptyPiece.right != exceptPiece)
            selectablePieces.Add(emptyPiece.right);

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
}
