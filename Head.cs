using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static HandUtil;

[System.Serializable]
public class Head : IObserver
{
    public Action HeadAction;
    private GameObject _player;
    private GameObject _headObj;
    private Camera _mainCamera;

    public void OnNotify(string state)
    {

    }

    public void Init()
    {
        InitObj();
        HeadAction = HeadSet;
    }

    public void InitObj()
    {
        _player = GameObject.FindWithTag("Player");
        _headObj = _player.transform.Find("Head").gameObject;
        _mainCamera = Camera.main;
    }

    public void HeadSet()
    {
        _headObj.transform.rotation = RimitedForwardToMouse(_headObj, _mainCamera, 25, 25, 180, true);
    }
}
