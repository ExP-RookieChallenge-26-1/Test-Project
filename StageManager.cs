using Sirenix.OdinInspector.Editor.GettingStarted;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SoldierType
{
    Default = -1,
    soldier,
    squad,
    platoon,
    elite,
    overseer,
    begger,
}
public class GameData
{
    public float stageTime = 600f;

    public float currentComplainScore = 30;

    public List<List<int>> stageData = new();
    public int currentStage = 1;
    
    public List<SoldierType> curSoldierData = new();
    public int curSoldierIdx = 0;
    public int expectedVisitNum;

    public bool isGiveEnd = false;
    public float cpScore = 0;

    public int failureCount = 0;
    public int successCount = 0;

    public int Gold;

    public List<string> SoldierNames = new();
}

public static class ListExtensions
{
    // this List<T> list -> 리스트에서 바로 .Shuffle()을 쓸 수 있게 해줌
    public static void Shuffle<T>(this List<T> list)
    {
        // 리스트는 길이(Length)가 아니라 개수(Count)를 씁니다.
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIdx = UnityEngine.Random.Range(0, i + 1);

            // 자리 바꾸기 (Swap)
            T temp = list[i];
            list[i] = list[randomIdx];
            list[randomIdx] = temp;
        }
    }
}
    

public class StageManager : Singleton<StageManager>
{
    public Action timeoutAction;

    public GameData data;
    public static Dictionary<int, StageData_SoldierData> StageDics= new();
    public static Dictionary<int, StageData_SoldierScaleData> CpScoreAmountDics = new();

    private Coroutine timeCoroutine;
    private Coroutine distributeCoroutine;

    protected override void Awake()
    {
        data = new(); // [임시]
        base.Awake();
        StageDics = SheetDataUtil.DicByKey(StageData.SoldierData, x => x.key);
        CpScoreAmountDics = SheetDataUtil.DicByKey(StageData.SoldierScaleData, x => x.key);
        MoveStageD();
        //기본 튜토리얼. 후 스테이지 시작.   
    }

    private void OnEnable()
    {
        timeoutAction += OnTimeOut;
    }

    private void OnDisable()
    {
        timeoutAction -= OnTimeOut;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => UIManager.IsReady);
        UIManager.Show<UIDefault>();
        StartGame();
    }

    private void Tutorial()
    {
        //UIManager.Show<UIDefault>();
        ////사전준비 튜토리얼.
        //Distribution();
    }

    //각 스테이지 시작부
    private void StartGame() // 스테이지1일 때 준비 과정 없는 것처럼 예외 처리 필요.
    {
        SSALManager.Instance.Active = false;
        data.stageTime = 180f;

        //스테이지에 따른 병사 수 전달 받고, 해당 스테이지 인원 수 + 거지/감시부 독립적인 확률 계산. 
        SortSoldier();
        data.curSoldierIdx = 0;
        SetSoldiersName();

        data.failureCount = 0;
        data.successCount = 0;

        //준비 단계 이동
        // 스테이지 1일 경우, 사전준비 튜토리얼 후 배급 단계
        if (data.currentStage == 1)
        {
            data.currentComplainScore = 30;
            Tutorial();
        }
        SSALManager.Instance.SetSSALCnt(data.currentStage);
        UIManager.Get<UIDefault>().Init();

        timeCoroutine = StartCoroutine(TimeCounter());
        distributeCoroutine = StartCoroutine(Distribution());
    }

    //배급 함수: 배급하고, 현재 병사 수치 받아온 후 로직에 따라 AddScore 함수로 불만 수치 전달. (특별 이벤트(감시부)- 병사 이름 값이나 키 바탕으로 조건문.)
    //병사들 수만큼 반복해서 배급하도록. +시간이 초과한 경우 나머지 인원들 전부 큐에서 제거, 최대 불만 수치 추가.
    private IEnumerator Distribution()
    {
        int maxIdx = data.curSoldierData.Count;

        //큐가 다 비워질 때까지 반복. 시간이 다 되는 경우 - 정산으로 이동 조건 필요.
        for (int i = 0; i < maxIdx; i++)//시간 조건
        {
            data.cpScore = 0;
            SSALManager.Instance.Active = true;
            //시간 조건
            //배급
            
            yield return new WaitUntil(() => data.isGiveEnd);
            SSALManager.Instance.Active = false;

            //평가로직 주고받기
            if(data.currentComplainScore + data.cpScore > 0) { data.currentComplainScore += data.cpScore; }
            else { data.currentComplainScore = 0; }

            Debug.Log(data.currentComplainScore);
            UIManager.Get<UIDefault>().SetComplainSldr(data.currentComplainScore);

            //조건문에 따라 상태 결정 - 현재 불만도 확인
            //배드엔딩
            if (data.currentComplainScore >= 100)
            {
                StopCoroutine(timeCoroutine);
                timeCoroutine = null;
                distributeCoroutine = null;
                //배드엔딩 씬 전환
            }

            data.curSoldierIdx++;
            data.isGiveEnd = false;
            Debug.Log($"실패횟수 : {data.failureCount} / 성공횟수 : {data.successCount}");
        }
        
        if (timeCoroutine != null)
        {
            StopCoroutine(timeCoroutine);
            timeCoroutine = null;
        }

        //현재 불만도 반영
        //if (data.sumOf_personal_ComplainScore + data.currentComplainScore >= 0)
        //{
        //    data.currentComplainScore = data.sumOf_personal_ComplainScore;
        //}
        //else
        //{
        //    data.currentComplainScore = 0;
        //}

        //정산 화면 이동
        Debug.Log($"실패횟수 : {data.failureCount} / 성공횟수 : {data.successCount}");                                                                                                                                            
        ShowResults();
        distributeCoroutine = null;
    }

    private IEnumerator TimeCounter()
    {
        const float tick = 0.2f;

        while (data.stageTime > 0f)
        {
            data.stageTime -= tick;
            if (data.stageTime < 0f)
                data.stageTime = 0f;

            // 시간 UI 갱신
            UIManager.Get<UIDefault>().SetClock(data.stageTime);
            yield return new WaitForSeconds(tick);
        }

        // 타임 종료 처리
        timeoutAction?.Invoke();
        timeCoroutine = null;
    }

    private void OnTimeOut()
    {
        if (distributeCoroutine  != null)
        {
            StopCoroutine(distributeCoroutine);
            distributeCoroutine = null;
        }


        int maxidx = data.curSoldierData.Count;
        int curidx = data.curSoldierIdx + 1;

        for(int i = curidx; i < maxidx; i++)
        {
            data.currentComplainScore += CpScoreAmountDics[i].CPScoreAmount * StageData.SoldierScaleData[(int)data.curSoldierData[data.curSoldierIdx]].BadScale;
            data.failureCount++;

            if (data.currentComplainScore >= 100)
            {
                SceneManager.LoadScene("ResultScene");
            }
        }

        ShowResults();
     }

    //정산 함수 + 
    public void ShowResults()
    {
        //정산 화면 (병사 수 / 쌀 이나 소비 수 등...??? 어떤 값? 불만 수치 포함 필요 등 UI on.)
        UIManager.Show<UIResult>();
    }

    public void ScoreJudge()
    {

        if (data.currentStage == 5.0)
        {
            data.currentStage = 0;
            SceneManager.LoadScene("ResultScene");
        }
        else
        {
            //다음 스테이지 
            data.currentStage++;
            //준비 단계 이동
            StartGame();
        }

    }

    private void SortSoldier()
    {
        //엑셀 파일에서 스테이지 별 병사 큐에 push
        for(int i = 0;i< data.stageData[data.currentStage - 1].Count; i++)
        {
            data.curSoldierData.Add((SoldierType)data.stageData[data.currentStage - 1][i]);
        }
        
        //섞기
        data.curSoldierData.Shuffle();  
    }

    public void MoveStageD()
    {
        data.stageData = new();

        for (int i = 0; i < 5; i++)
        {
            data.stageData.Add(new());
        }

        //stage1
        for (int i = 0; i < 3; i++)
        {
            data.stageData[0].Add(StageDics[i].stage1);
        }
        //stage2
        for (int i = 0; i < 5; i++)
        {
            data.stageData[1].Add(StageDics[i].stage2);
        }
        //stage3
        for (int i = 0; i < 6; i++)
        {
            data.stageData[2].Add(StageDics[i].stage3);
        }
        //stage4
        for (int i = 0; i < 7; i++)
        {
            data.stageData[3].Add(StageDics[i].stage4);
        }
        //stage5
        for (int i = 0; i < 6; i++)
        {
            data.stageData[4].Add(StageDics[i].stage5);
        }
        return;
    }

    public void SetSoldiersName()
    {
        NameManager.Instance.ClearNames();
        for (int i = 0; i < data.curSoldierData.Count; i++)
        {
            string name = NameManager.Instance.GenerateName();
            data.SoldierNames.Add(name);
        }
    }
}