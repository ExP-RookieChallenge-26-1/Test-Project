using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [HideInInspector]
    public Movement Movement; // 이동 관리

    public virtual void Init()
    {
        // 이동 초기화
        Movement?.InitMovement(gameObject.GetComponent<Rigidbody2D>());
    }
}