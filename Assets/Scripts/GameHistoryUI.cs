using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHistoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject historyObject;
    public ScrollRect historyScrollRect;
    public RectTransform contentTransform;
    public GameObject messageItemPrefab;

    public void AddMessage(string message, bool important = false)
    {
        var go = Instantiate(messageItemPrefab, contentTransform);
        var text = go.GetComponent<TMP_Text>();
        text.text = message;

        if (important)
        {
            text.fontStyle = FontStyles.Bold;
            text.color = Color.green;
        }

        Canvas.ForceUpdateCanvases();  // ensure layouts are up to date
        historyScrollRect.verticalNormalizedPosition = 0f;
    }

    public void ClearMessages()
    {
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        Canvas.ForceUpdateCanvases();
        historyScrollRect.verticalNormalizedPosition = 1f; // Reset scroll to top
    }

    public void ToggleHistoryObject()
    {
        if (historyObject.activeSelf)
            historyObject.SetActive(false);
        else
            historyObject.SetActive(true);
    }

}