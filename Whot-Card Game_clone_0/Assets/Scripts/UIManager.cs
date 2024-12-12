using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake() => Instance = this;

    public void ShowWinPanel() => GameManager.Instance.winPanel.SetActive(true);
    public void ShowLosePanel() => GameManager.Instance.losePanel.SetActive(true);
    public void HideAllPanels()
    {
        GameManager.Instance.winPanel.SetActive(false);
        GameManager.Instance.losePanel.SetActive(false);
        GameManager.Instance.shapeSelectionPanel.SetActive(false);
    }

    public void ShowShapeSelection() => GameManager.Instance.shapeSelectionPanel.SetActive(true);
    public void HideShapeSelection() => GameManager.Instance.shapeSelectionPanel.SetActive(false);
}
