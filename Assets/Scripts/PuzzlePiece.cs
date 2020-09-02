using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public abstract class Dependency
    {
        public abstract Vector2 GetTileSize();
    }
    public Dependency dependency;

    private class InterpolatedMovement
    {
        private Transform transform;
        public Vector3 startPosition { get; private set; }
        public Vector3 targetPosition { get; private set; }
        public float movementTime { get; private set; }
        public float passedTime { get; private set; }
        public bool shouldMove { get; private set; }

        public InterpolatedMovement(Transform transform)
        {
            this.transform = transform;
            startPosition = transform.position;
            targetPosition = startPosition;
            movementTime = 0.001f;
            passedTime = 0;
            shouldMove = false;
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
    }
    private InterpolatedMovement movement;

    public PuzzlePiece top;
    public PuzzlePiece bottom;
    public PuzzlePiece left;
    public PuzzlePiece right;

    private IEnumerator moveCoroutine;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    public bool isEmptyPiece = false;
    void Awake()
    {
        targetPosition = transform.position;
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
        if (top != null && top.isEmptyPiece)
        {
            AimToTop(dependency.GetTileSize());
        }
        else if (bottom != null && bottom.isEmptyPiece)
        {
            AimToBottom(dependency.GetTileSize());
        }
        else if (left != null && left.isEmptyPiece)
        {
            AimToLeft(dependency.GetTileSize());
        }
        else if (right != null && right.isEmptyPiece)
        {
            AimToRight(dependency.GetTileSize());
        }
    }

    void AimToTop(Vector2 tileSize)
    {
        Debug.Log("Up");

        Vector3 distance = new Vector3(0, tileSize.y, 0);
        movement.SetRelativeMovement(distance, 0.1f, true);

        PuzzlePiece topBackup = top;
        PuzzlePiece bottomBackup = bottom;
        PuzzlePiece leftBackup = left;
        PuzzlePiece rightBackup = right;

        if (bottomBackup)
            bottomBackup.top = topBackup;
        if (leftBackup)
            leftBackup.right = topBackup;
        if (rightBackup)
            rightBackup.left = topBackup;

        if (topBackup.top)
            topBackup.top.bottom = this;
        if (topBackup.left)
            topBackup.left.right = this;
        if (topBackup.right)
            topBackup.right.left = this;

        top = topBackup.top;
        bottom = topBackup;
        left = topBackup.left;
        right = topBackup.right;

        topBackup.top = this;
        topBackup.bottom = bottomBackup;
        topBackup.left = leftBackup;
        topBackup.right = rightBackup;
    }
    void AimToBottom(Vector2 tileSize)
    {
        Debug.Log("Bottom");

        Vector3 distance = new Vector3(0, -tileSize.y, 0);
        movement.SetRelativeMovement(distance, 0.1f, true);

        PuzzlePiece topBackup = top;
        PuzzlePiece bottomBackup = bottom;
        PuzzlePiece leftBackup = left;
        PuzzlePiece rightBackup = right;

        if (topBackup)
            topBackup.bottom = bottomBackup;
        if (leftBackup)
            leftBackup.right = bottomBackup;
        if (rightBackup)
            rightBackup.left = bottomBackup;

        if (bottomBackup.bottom)
            bottomBackup.bottom.top = this;
        if (bottomBackup.left)
            bottomBackup.left.right = this;
        if (bottomBackup.right)
            bottomBackup.right.left = this;

        top = bottomBackup;
        bottom = bottomBackup.bottom;
        left = bottomBackup.left;
        right = bottomBackup.right;

        bottomBackup.top = topBackup;
        bottomBackup.bottom = this;
        bottomBackup.left = leftBackup;
        bottomBackup.right = rightBackup;
    }
    void AimToLeft(Vector2 tileSize)
    {
        Debug.Log("Left");

        Vector3 distance = new Vector3(-tileSize.x, 0, 0);
        movement.SetRelativeMovement(distance, 0.1f, true);

        PuzzlePiece topBackup = top;
        PuzzlePiece bottomBackup = bottom;
        PuzzlePiece leftBackup = left;
        PuzzlePiece rightBackup = right;

        if (topBackup)
            topBackup.bottom = leftBackup;
        if (bottomBackup)
            bottomBackup.top = leftBackup;
        if (rightBackup)
            rightBackup.left = leftBackup;

        if (leftBackup.top)
            leftBackup.top.bottom = this;
        if (leftBackup.bottom)
            leftBackup.bottom.top = this;
        if (leftBackup.left)
            leftBackup.left.right = this;

        top = leftBackup.top;
        bottom = leftBackup.bottom;
        left = leftBackup.left;
        right = leftBackup;

        leftBackup.top = topBackup;
        leftBackup.bottom = bottomBackup;
        leftBackup.left = this;
        leftBackup.right = rightBackup;
    }
    void AimToRight(Vector2 tileSize)
    {
        Debug.Log("Right");

        Vector3 distance = new Vector3(tileSize.x, 0, 0);
        movement.SetRelativeMovement(distance, 0.1f, true);

        PuzzlePiece topBackup = top;
        PuzzlePiece bottomBackup = bottom;
        PuzzlePiece leftBackup = left;
        PuzzlePiece rightBackup = right;

        if (topBackup)
            topBackup.bottom = rightBackup;
        if (bottomBackup)
            bottomBackup.top = rightBackup;
        if (leftBackup)
            leftBackup.right = rightBackup;

        if (rightBackup.top)
            rightBackup.top.bottom = this;
        if (rightBackup.bottom)
            rightBackup.bottom.top = this;
        if (rightBackup.right)
            rightBackup.right.left = this;

        top = rightBackup.top;
        bottom = rightBackup.bottom;
        left = rightBackup;
        right = rightBackup.right;

        rightBackup.top = topBackup;
        rightBackup.bottom = bottomBackup;
        rightBackup.left = leftBackup;
        rightBackup.right = this;
    }

}
