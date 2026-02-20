using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public static class DOTweenHelper
{
    public static Sequence OpenPanel(this GameObject panel, float scaleDuration = 0.3f, float fadeDuration = 0.2f, Ease easeType = Ease.OutBack, float startScale = 0f)
    {
        if (panel == null)
        {
            return null;
        }

        panel.SetActive(true);

        Transform panelTransform = panel.transform;
        panelTransform.DOKill(true);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        canvasGroup.DOKill(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelTransform.localScale = Vector3.one * startScale;

        Sequence seq = DOTween.Sequence();
        seq.Append(panelTransform.DOScale(Vector3.one, scaleDuration).SetEase(easeType));
        seq.Join(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, fadeDuration).SetEase(Ease.OutQuad));
        seq.OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        });

        return seq.SetTarget(panelTransform).SetUpdate(true);
    }

    public static Tweener AnchoredMoveTo(this RectTransform target, Vector2 endValue, float duration = 0.5f, Ease easeType = Ease.Linear)
    {
        target.DOKill(true);
        return target.DOAnchorPos(endValue, duration)
            .SetTarget(target)
            .SetEase(easeType)
            .SetUpdate(true);
    }

    public static Sequence MoveAndFadeIn(this RectTransform target, Vector2 endValue, float moveDuration = 0.5f, float fadeDuration = 0.3f, Ease easeType = Ease.Linear)
    {
        target.DOKill(true);
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();

        seq.Append(target.DOAnchorPos(endValue, moveDuration).SetEase(easeType));
        seq.Join(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, fadeDuration).SetEase(Ease.Linear));
        return seq.SetUpdate(true);
    }

    public static Tweener Scale(this Transform target, Vector3 endValue, float duration = 0.3f, Ease easeType = Ease.OutBack)
    {
        target.DOKill(true);
        return target.DOScale(endValue, duration)
            .SetTarget(target)
            .SetEase(easeType)
            .SetUpdate(true);
    }

    public static Tweener FadeIn(this CanvasGroup target, float duration = 0.3f, float delay = 0.5f, Ease easeType = Ease.OutBack)
    {
        target.DOKill(true);
        target.alpha = 0f;
        target.interactable = true;
        target.blocksRaycasts = true;

        return DOTween.To(() => target.alpha, x => target.alpha = x, 1f, duration)
            .SetDelay(delay)
            .SetTarget(target)
            .SetEase(easeType)
            .SetUpdate(true);
    }

    public static Tweener FadeOut(this CanvasGroup target, float duration = 0.3f, float delay = 0.5f, Ease easeType = Ease.Linear)
    {
        target.DOKill(true);
        target.interactable = false;
        target.blocksRaycasts = false;

        return DOTween.To(() => target.alpha, x => target.alpha = x, 0f, duration)
            .SetDelay(delay)
            .SetTarget(target)
            .SetEase(easeType)
            .SetUpdate(true);
    }

    public static Tweener FadeTo(this CanvasGroup target, float endValue, float duration = 0.3f, float delay = 0f, Ease easeType = Ease.Linear)
    {
        target.DOKill(true);
        return DOTween.To(() => target.alpha, x => target.alpha = x, endValue, duration)
            .SetDelay(delay)
            .SetTarget(target)
            .SetEase(easeType)
            .SetUpdate(true);
    }

    public static Sequence HoverEffect(this Button button, Color hoverColor, Vector2 punch, float duration = 0.2f)
    {
        button.targetGraphic.DOKill(true);
        button.transform.DOKill(true);

        Sequence seq = DOTween.Sequence();

        seq.Append(button.transform.DOPunchScale(punch * 0.5f, duration, vibrato: 3, elasticity: 0.3f));

        seq.Join(button.targetGraphic.DOColor(hoverColor, duration * 0.5f));

        return seq.SetUpdate(true);
    }

    public static Tweener ButtonPress(this Transform target, float scale = 0.95f, float duration = 0.1f, Ease easeType = Ease.OutQuad)
    {
        target.DOKill(true);
        return target.DOScale(scale, duration)
            .SetTarget(target)
            .SetEase(easeType)
            .SetUpdate(true)
            .SetLoops(2, LoopType.Yoyo);
    }

    public static Tweener ColorTo(this Graphic target, Color endValue, float duration = 0.3f, Ease easeType = Ease.Linear)
    {
        target.DOKill(true);
        return target.DOColor(endValue, duration)
            .SetTarget(target)
            .SetEase(easeType)
            .SetUpdate(true);
    }

    public static Tweener Shake(this Transform target, float duration = 0.5f, float strength = 10f, int vibrato = 10, float randomness = 90f)
    {
        target.DOKill(true);
        return target.DOShakePosition(duration, strength, vibrato, randomness)
            .SetTarget(target)
            .SetUpdate(true);
    }

    public static Tweener Flash(this Graphic target, Color flashColor, float duration = 0.5f, int flashes = 2, bool doAlpha = false)
    {
        target.DOKill(true);

        Color originalColor = target.color;

        if (doAlpha)
        {
            // Flash dengan alpha
            float originalAlpha = originalColor.a;

            return DOTween.To(
                    () => target.color.a,
                    x => target.color = new Color(originalColor.r, originalColor.g, originalColor.b, x),
                    0f,
                    duration * 0.5f)
                .SetLoops(flashes * 2, LoopType.Yoyo)
                .OnComplete(() =>
                {
                    target.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalAlpha);
                })
                .SetEase(Ease.InOutQuad)
                .SetTarget(target)
                .SetUpdate(true);
        }
        else
        {
            return target.DOColor(flashColor, duration * 0.5f)
                .SetLoops(flashes * 2, LoopType.Yoyo)
                .OnComplete(() => target.color = originalColor)
                .SetEase(Ease.InOutQuad)
                .SetTarget(target)
                .SetUpdate(true);
        }
    }

    public static Tweener PunchScale(this Transform target, Vector3 punch, float duration = 0.5f, int vibrato = 10, float elasticity = 1f)
    {
        target.DOKill(true);
        return target.DOPunchScale(punch, duration, vibrato, elasticity)
            .SetTarget(target)
            .SetUpdate(true);
    }

    public static Tweener RotateLoop(this Transform target, Vector3 rotation, float duration = 1f, LoopType loopType = LoopType.Incremental)
    {
        target.DOKill(true);
        return target.DORotate(rotation, duration, RotateMode.FastBeyond360)
            .SetTarget(target)
            .SetEase(Ease.Linear)
            .SetLoops(-1, loopType)
            .SetUpdate(true);
    }

    public static Tweener RotateTo(this Transform target, Vector3 endValue, float duration = 1f, Ease easeType = Ease.Linear)
    {
        target.DOKill(true);
        return target.DORotate(endValue, duration)
            .SetTarget(target)
            .SetEase(easeType)
            .SetUpdate(true);
    }

    public static Sequence Pulse(this Transform target, Vector3 scale, float duration = 0.5f, Ease easeType = Ease.InOutSine)
    {
        target.DOKill(true);
        Sequence seq = DOTween.Sequence();
        seq.Append(target.DOScale(scale, duration).SetEase(easeType));
        seq.Append(target.DOScale(Vector3.one, duration).SetEase(easeType));
        return seq.SetTarget(target).SetUpdate(true);
    }

    public static Tweener PulseLoop(this Transform target, Vector3 baseScale, Vector3 pulseScale, float duration = 0.5f, Ease easeType = Ease.InOutSine)
    {
        target.DOKill(false);
        target.localScale = baseScale;
        return target.DOScale(pulseScale, duration)
            .SetTarget(target)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    public static void StopPulse(this Transform target, Vector3 baseScale)
    {
        target.DOKill(false);
        target.localScale = baseScale;
    }
}
