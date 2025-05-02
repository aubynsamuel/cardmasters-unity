using UnityEngine;

public class CardScript : MonoBehaviour
{
    public bool fromPlayer;
    public Transform opponentSlot;
    public Transform playerSlot;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isMoving = false;
    public float moveSpeed = 5f;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition;
        targetRotation = transform.rotation;
    }

    void Update()
    {
        // Animate position
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, moveSpeed * 20f * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                isMoving = false;
            }
        }
    }

    void OnMouseDown()
    {
        if (fromPlayer)
        {
            if (Vector3.Distance(transform.position, startPosition) < 0.01f)
            {
                MoveTo(playerSlot.position, playerSlot.rotation);
            }
            else
            {
                MoveTo(startPosition, Quaternion.identity);
            }
        }
        else
        {
            if (Vector3.Distance(transform.position, startPosition) < 0.01f)
            {
                MoveTo(opponentSlot.position, opponentSlot.rotation);
            }
            else
            {
                MoveTo(startPosition, Quaternion.identity);
            }
        }
    }

    void MoveTo(Vector3 position, Quaternion rotation)
    {
        targetPosition = position;
        targetRotation = rotation;
        isMoving = true;
    }
}