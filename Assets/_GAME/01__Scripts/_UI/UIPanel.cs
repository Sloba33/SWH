using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public abstract class UIPanel : MonoBehaviour
{
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected float fadeDuration = 0.3f;

    public virtual void Show()
    {
        gameObject.SetActive(true);
        canvasGroup.DOFade(1, fadeDuration);
        OnPanelOpened();
    }

    public virtual void Hide()
    {
        canvasGroup.DOFade(0, fadeDuration)
            .OnComplete(() => gameObject.SetActive(false));
    }

    protected virtual void OnPanelOpened() { }
}
