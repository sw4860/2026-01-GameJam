using UnityEngine;

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

    [SerializeField] private GameObject windowPrefab;
    [SerializeField] private Transform windowParent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OpenWindow(WindowData data)
    {
        GameObject newWindow = Instantiate(windowPrefab, windowParent != null ? windowParent : transform);
        WindowContentUI ui = newWindow.GetComponent<WindowContentUI>();
        ui.Setup(data);
        
        newWindow.transform.SetAsLastSibling();
    }
}
