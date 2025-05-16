
using UnityEngine;
using DG.Tweening;
public class PopoutButton : MonoBehaviour
{
    public Vector3 startScale;
    private Vector3 startRotation;
    public bool Static = false;
    public bool wasScaleAssigned;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float delay = 0.3f;
    void Awake()
    {
        if (!wasScaleAssigned)
            startScale = transform.localScale;
        startRotation = transform.localEulerAngles;
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
