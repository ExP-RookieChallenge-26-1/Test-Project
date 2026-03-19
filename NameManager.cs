using System;
using System.Collections.Generic;

public class NameManager : Singleton<NameManager>
{
    private List<string> usedNames = new List<string>();
    private List<String> firstNames = new List<string>()
    {
        "김", "이", "박", "최", "정", "강", "조", "윤", "장", "임", "오", "남궁", "황", "안", "송", "서", "권", "홍", "곽", "신"
    };
    private List<String> middleNames = new List<string>()
    {
        "", "민", "중", "돌", "준", "만", "철", "상", "몽", "식", "동", "쇠", "석", "분", "염", "문", "병", "재"
    };
    private List<String> lastNames = new List<string>()
    {
        "철", "쇠", "돌", "식", "산", "용", "손", "준", "석", "정", "웅", "열", "달"
    };
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string GenerateName()
    {
        string middleName = "";
        string lastName = "";
        string newName;
        do
        {
            string firstName = firstNames[UnityEngine.Random.Range(0, firstNames.Count)];
            middleName = middleNames[UnityEngine.Random.Range(0, middleNames.Count)];
            lastName = lastNames[UnityEngine.Random.Range(0, lastNames.Count)];
            newName = firstName + middleName + lastName;
        }while(!usedNames.Contains(newName) && middleName != lastName);
        usedNames.Add(newName);
        return newName;
    }
    public void ClearNames()
    {
        usedNames.Clear();
    }
}
