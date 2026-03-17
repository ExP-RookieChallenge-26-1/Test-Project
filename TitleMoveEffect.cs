using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class TitleMoveEffect : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 2.0f;
    [SerializeField] private float floatAmplitude = 40.0f;

    [SerializeField] private Vector2 targetAnchorPos;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Vector3 targetScale = Vector3.one;
    [SerializeField] private CanvasGroup btns;

    private RectTransform rectTransform;
    private Sequence currentSequence;
    private bool isClicked = false;
    private Vector3 startScale;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (btns != null) btns.alpha = 0f;
    }

    void Start()
    {
        startScale = transform.localScale;
        StartFloating(rectTransform.anchoredPosition);
    }

    void Update()
    {
        if (!isClicked && Mouse.current.leftButton.IsPressed())
        {
            MoveToTarget();
        }
    }

    private void StartFloating(Vector2 basePosition)
    {
        btns.interactable = true;
        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        currentSequence.Append(rectTransform.DOAnchorPos(basePosition + new Vector2(0, floatAmplitude), floatSpeed)
            .SetEase(Ease.InOutSine));
        currentSequence.Append(rectTransform.DOAnchorPos(basePosition, floatSpeed)
            .SetEase(Ease.InOutSine));

        currentSequence.SetLoops(-1);
    }

    public void MoveToTarget()
    {
        if (isClicked) return;
        isClicked = true;

        currentSequence?.Kill();

        Sequence transitionSeq = DOTween.Sequence();

        transitionSeq.Join(rectTransform.DOAnchorPos(targetAnchorPos, transitionDuration).SetEase(Ease.OutQuint));
        transitionSeq.Join(rectTransform.DOScale(targetScale, transitionDuration).SetEase(Ease.OutQuint));

        if (btns != null)
        {
            transitionSeq.Join(btns.DOFade(1f, transitionDuration).SetEase(Ease.OutQuint));
        }

        transitionSeq.OnComplete(() =>
        {
            StartFloating(targetAnchorPos);
        });
    }
}
