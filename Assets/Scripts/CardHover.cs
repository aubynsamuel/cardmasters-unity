using UnityEngine;

public class CardHover : MonoBehaviour
{
    public float hoverScale = 1.1f;
    public float scaleSpeed = 15f;
    public CardUI cardUI;

    private Vector3 originalScale;
    private Vector3 targetScale;
    // private bool isHovering = false;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        cardUI = gameObject.GetComponent<CardUI>();
    }

    void OnMouseEnter()
    {
        if (cardUI != null && cardUI.isClickable)
        {
            targetScale = originalScale * hoverScale;
            // isHovering = true;
        }
    }

    void OnMouseExit()
    {
        if (cardUI != null && cardUI.isClickable)
        {
            targetScale = originalScale;
            // isHovering = false;
        }
    }
    void Update()
    {
        // if (isHovering)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }
}
