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
        private Transform transform;
        public bool shouldMove { get; private set; }
        public Vector3 startPosition { get; private set; }
        public Vector3 targetPosition { get; private set; }
        public float movementTime { get; private set; }
        public float passedTime { get; private set; }

        public InterpolatedMovement(Transform transform)
        {
            this.transform = transform;
            shouldMove = false;
            startPosition = transform.position;
            targetPosition = startPosition;
            movementTime = 0.001f;
            passedTime = 0;
        }

        public void SetMovement(Vector3 targetPosition, float time, bool skipPrevious = true)
        {
            if (time < 0.001f)
                time = 0.001f;

            this.movementTime = time;
            passedTime = 0;

            if (skipPrevious)
                transform.position = this.targetPosition;

            startPosition = transform.position;
            this.targetPosition = targetPosition;

            shouldMove = true;
        }
        public void SetRelativeMovement(Vector3 distance, float time, bool skipPrevious = true)
        {
            if (skipPrevious)
                SetMovement(targetPosition + distance, time, true);
            else
                SetMovement(transform.position + distance, time, false);
        }
        public void UpdateMovement(float deltaTime)
        {
            if (shouldMove)
            {
                passedTime += deltaTime;
                transform.position = Vector3.Lerp(startPosition, targetPosition, passedTime / movementTime);

                if (transform.position == targetPosition)
                    shouldMove = false;
            }
        }
        public void ForceToFinishMovement()
        {
            shouldMove = false;
            transform.position = targetPosition;
        }

        public void BlinkTo(Vector3 targetPosition)
        {
            shouldMove = false;
            transform.position = startPosition = this.targetPosition = targetPosition;
            movementTime = 0.001f;
            passedTime = 0;
        }

        public void BlinkRelative(Vector3 distance, bool skipPrevious = true)
        {
            if (skipPrevious)
                BlinkTo(targetPosition + distance);
            else
                BlinkTo(transform.position + distance);

        }
    }
    private InterpolatedMovement movement;

    public Vector2Int index;
    public bool isInvisiblePiece = false;

    void Awake()
    {
        movement = new InterpolatedMovement(transform);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        movement.UpdateMovement(Time.deltaTime);
    }
    
    public void OnMouseDown()
    {
        dependency.HandleInput(this);
    }

    public void MoveRelative(Vector2Int distance)
    {
        movement.SetRelativeMovement(distance * dependency.GetPieceSize(), 0.1f, true);
    }
    public void BlinkRelative(Vector2Int distance)
    {
        movement.BlinkRelative(distance * dependency.GetPieceSize(), true);
    }

    static public void SwapIndex(PuzzlePiece op0, PuzzlePiece op1)
    {
        Vector2Int backup = op0.index;
        op0.index = op1.index;
        op1.index = backup;
    }
}
