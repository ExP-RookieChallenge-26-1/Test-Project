using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterMovement : Movement
{
    public float Drag = 5f; // 관성 효과를 위한 드래그 값

    private Vector2 _movement;
    private Vector2 _velocity; // 현재 속도

    public override void InitFirst()
    {
        base.InitFirst();
        MoveAction += SetCharacterDirection;
    }


    public void SetCharacterDirection()
    {
        // 선형 보간을 사용해 현재 속도를 목표 속도로 서서히 변경 (관성 효과)
        MoveDirection = Vector2.Lerp(_velocity, _movement * MoveSpeed, Drag * Time.fixedDeltaTime);
    }
}