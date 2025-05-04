using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CardUI : MonoBehaviour
{
    [HideInInspector] public Card cardData;
    // [HideInInspector] public int handIndex;

    private CardsSetup gm;
    private Vector3 startingPosition;
    private Vector3 destination;
    private bool isMoving;
    private bool setStartingPos = false;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("NewGameManager").GetComponent<CardsSetup>();
    }

    void Update()
    {
        if (!isMoving) return;
        if (!setStartingPos)
        {
            startingPosition = transform.position;
            setStartingPos = true;
        }

        // move from wherever we are _towards_ the current destination
        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            gm.moveSpeed * Time.deltaTime
        );

        // if we’ve arrived (within a tiny threshold), stop
        if (Vector3.Distance(transform.position, destination) < 0.01f)
        {
            gm.audioSource.PlayOneShot(gm.dealSound);
            isMoving = false;
        }
    }

    void OnMouseDown()
    {
        var success = gm.gameClass.HumanPlayCard(cardData);
        if (!success) return;
        if (isMoving) return;

        // toggle destination
        var playPos = gm.playSpot.position;
        destination = Vector3.Distance(transform.position, playPos) < 0.1f
            ? startingPosition   // back home
            : playPos;           // go to play spot

        isMoving = true;
        gm.gameClass.humanCardTODestroy = gameObject;
    }
    public void ComputerPlay()
    {
        if (isMoving) return;

        // toggle destination
        var playPos = gm.playSpot2.position;
        destination = Vector3.Distance(transform.position, playPos) < 0.1f
            ? startingPosition   // back home
            : playPos;           // go to play spot

        isMoving = true;
    }
}