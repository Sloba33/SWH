
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
public class CurrencyPanelButtonPopout : MonoBehaviour
{
    public Vector3 startScale;
    private Vector3 startRotation;
    public bool Static = false;
    public bool wasScaleAssigned;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float delay = 0.3f;
    private RectTransform rect;
    void Awake()
    {
        rect = GetComponent<RectTransform>();

        if (!wasScaleAssigned)
            startScale = rect.localScale;

        startRotation = rect.localEulerAngles;

        rect.pivot = new Vector2(0.5f, 1f); // top center
    }
    void OnEnable()
    {
        EnableWindow();
    }
    void OnDisable()
    {
        DisableWindow();
    }
    public void EnableWindow()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(startScale, duration).Play().SetDelay(delay);
        if (!Static) transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), duration).Play().SetDelay(duration + delay);
    }
    public void DisableWindow()
    {
        transform.DOScale(Vector3.zero, duration).Play().SetDelay(delay);
    }
}
