using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private float minWaitTime = 3.0f;
    public CanvasGroup Content;

    private void Start()
    {
        StartCoroutine(LoadSceneProcess());

        foreach (var p in phases)
        {
            if (p.canvasGroup != null) p.canvasGroup.alpha = 0f;
        }

        ExecuteNextPhase();
    }

    private IEnumerator LoadSceneProcess()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);

        op.allowSceneActivation = false;

        float timer = 0f;
        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;


            if (op.progress > 0.9f)
            {
                op.allowSceneActivation = true;
            }
            if (op.progress >= 0.9f && timer >= minWaitTime)
            {
                op.allowSceneActivation = true;
                yield break;
            }
        }
    }

    [System.Serializable]
    public struct UIPhaseData
    {
        public RectTransform targetRT;
        public CanvasGroup canvasGroup;
        public Vector3 customScale;
        public float duration;
    }

    [Header("--- Transition Settings ---")]
    [SerializeField] private RectTransform blackOverlay; // 화면을 가릴 검은 배경
    [SerializeField] private float overlayMoveDistance = 2000f; // 배경이 이동할 거리
    [SerializeField] private List<UIPhaseData> phases;

    private int currentIdx = 0;
    private Vector2 overlayStartPos;

    void Awake()
    {
        if (blackOverlay != null)
            overlayStartPos = blackOverlay.anchoredPosition;
    }

    private void ExecuteNextPhase()
    {
        if (currentIdx >= phases.Count || currentIdx >= 5) return;

        UIPhaseData data = phases[currentIdx];

        if (currentIdx == 0)
        {
            RunFadeInOut(data);
        }
        else
        {
            RunOverlayTransition(data);
        }
    }

    private void RunFadeInOut(UIPhaseData data)
    {
        data.canvasGroup.alpha = 0f;
        Sequence seq = DOTween.Sequence();
        seq.Append(data.canvasGroup.DOFade(1f, data.duration));
        seq.AppendInterval(0.5f);
        seq.Append(data.canvasGroup.DOFade(0f, data.duration));
        seq.OnComplete(() =>
        {
            currentIdx++;
            ExecuteNextPhase();
        });
    }

    private void RunOverlayTransition(UIPhaseData data)
    {
        Content.alpha = 1;
        data.targetRT.localScale = data.customScale;
        data.canvasGroup.alpha = 1f;

        blackOverlay.anchoredPosition = overlayStartPos;

        blackOverlay.DOAnchorPos(overlayStartPos + new Vector2(-overlayMoveDistance, 0), data.duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                data.canvasGroup.alpha = 0f;
                currentIdx++;
                ExecuteNextPhase();
            });
    }
}