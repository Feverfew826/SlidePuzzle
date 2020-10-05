using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public interface Dependency
    {
        Vector2 GetPieceSize();
        void HandleInput(PuzzlePiece inputPiece);
    }
    public Dependency dependency;

    private class InterpolatedMovement
    {
        private RectTransform transform;
        public bool shouldMove { get; private set; }
        public Vector2 startPosition { get; private set; }
        public Vector2 targetPosition { get; private set; }
        public float movementTime { get; private set; }
        public float passedTime { get; private set; }

        public InterpolatedMovement(RectTransform transform)
        {
            this.transform = transform;
            shouldMove = false;
            startPosition = transform.anchoredPosition;
            targetPosition = startPosition;
            movementTime = 0.001f;
            passedTime = 0;
        }

        public void SetMovement(Vector2 targetPosition, float time, bool skipPrevious = true)
        {
            if (time < 0.001f)
                time = 0.001f;

            this.movementTime = time;
            passedTime = 0;

            if (skipPrevious)
                transform.anchoredPosition = this.targetPosition;

            startPosition = transform.anchoredPosition;
            this.targetPosition = targetPosition;
            Debug.Log(startPosition);
            Debug.Log(targetPosition);
            shouldMove = true;
        }
        public void SetRelativeMovement(Vector2 distance, float time, bool skipPrevious = true)
        {
            if (skipPrevious)
                SetMovement(targetPosition + distance, time, true);
            else
                SetMovement(transform.anchoredPosition + distance, time, false);
        }
        public void UpdateMovement(float deltaTime)
        {
            if (shouldMove)
            {
                passedTime += deltaTime;
                transform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, passedTime / movementTime);

                if (transform.anchoredPosition == targetPosition)
                    shouldMove = false;
            }
        }
        public void ForceToFinishMovement()
        {
            shouldMove = false;
            transform.anchoredPosition = targetPosition;
        }

        public void BlinkTo(Vector3 targetPosition)
        {
            shouldMove = false;
            transform.anchoredPosition = startPosition = this.targetPosition = targetPosition;
            movementTime = 0.001f;
            passedTime = 0;
        }

        public void BlinkRelative(Vector2 distance, bool skipPrevious = true)
        {
            if (skipPrevious)
                BlinkTo(targetPosition + distance);
            else
                BlinkTo(transform.anchoredPosition + distance);

        }
    }

    private System.Lazy<InterpolatedMovement> movement;

    public Vector2Int index;
    public bool isInvisiblePiece = false;

    void Awake()
    {
        movement = new System.Lazy<InterpolatedMovement>(() => new InterpolatedMovement(GetComponent<RectTransform>()));
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        movement.Value.UpdateMovement(Time.deltaTime);
    }
    
    public void OnMouseDown()
    {
        dependency.HandleInput(this);
    }

    public void MoveRelative(Vector2Int distance)
    {
        movement.Value.SetRelativeMovement(distance * dependency.GetPieceSize(), 0.1f, true);
    }
    public void BlinkRelative(Vector2Int distance)
    {
        movement.Value.BlinkRelative(distance * dependency.GetPieceSize(), true);
    }

    static public void SwapIndex(PuzzlePiece op0, PuzzlePiece op1)
    {
        Vector2Int backup = op0.index;
        op0.index = op1.index;
        op1.index = backup;
    }
}
