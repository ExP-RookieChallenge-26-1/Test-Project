using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Weapon;
using static HandUtil;

[System.Serializable]
public class Hand : IObserver
{
    private Swap swap => Swap.instance;
    private List<Melee> melees => Melee.instances;

    public enum HandType
    {
        Standard = 0,
        Left = 1,
        Right = 2,
    }

    public enum HandLayer
    {
        beforeBody = -2,
        InClothes = 0,
        afterBody = 2,
        beforeGun = 3,
        afterGun = 5,
    }

    public enum WeaponSlot
    {
        FirstRanged = 0,
        SecondRanged = 1,
        FirstMelee = 2,
    }

    // Player에게 Update 실행을 넘기기 위한 Action, Player쪽에서 실행시켜준다.
    public Action HandAction;

    // 각종 키 관련 데이터, 임시로 넣은거고 키 데이터 관련은 따로 뺄 생각임!
    /// <summary>
    /// 공격 키
    /// </summary>
    public int AttackKeyIndex = 0;
    /// <summary>
    /// 무기 교체 키
    /// </summary>
    public KeyCode SlotChangeKey = KeyCode.R;
    public KeyCode SlotChangeTo1Key = KeyCode.Alpha1;
    public KeyCode SlotChangeTo2Key = KeyCode.Alpha2;
    public KeyCode SlotChangeTo3Key = KeyCode.Alpha3;


    // 실질적인 무기 데이터, 각 슬롯에 무엇을 장착 중인지, 어떤 무기를 사용 중인지
    /// <summary>
    /// 원거리 무기1
    /// </summary>
    public EnumData.Weapon FirstSlot;
    /// <summary>
    /// 원거리 무기2
    /// </summary>
    public EnumData.Weapon SecondSlot;
    /// <summary>
    /// 근거리 무기1
    /// </summary>
    public EnumData.Weapon ThirdSlot;
    /// <summary>
    /// 사용 중인 무기
    /// </summary>
    public WeaponSlot CurrentSlotIndex = WeaponSlot.FirstRanged;

    public EnumData.Weapon CurrentSlotWeaponType
    {
        get
        { // 현재 무기가 무엇인지 알려주는 프로퍼티
            return GetSlotWeaponType(CurrentSlotIndex);
        }
    }


    /// <summary>
    /// 무기 교체에 필요한 시간
    /// </summary>
    public float WeaponSwitchTime = 2f;

    // 뒤집힌 여부를 판별하여 손 배치를 반대로 해야하는지
    private bool _changeHand;
    private Camera _mainCamera;

    /// <summary>
    /// 무기들 저장 공간
    /// </summary>
    private Weapon[] _weapons; public Weapon[] _Weapons { get => _weapons; }
    /// <summary>
    /// 현재 사용 중인 무기
    /// </summary>
    private Weapon _currentWeapon; public Weapon _CurrentWeapon { get => _currentWeapon; set => _currentWeapon = value; }

    /// <summary>
    /// 변화를 감지하고 새롭게 세팅하기 위한 감지용
    /// </summary>
    private bool _lastWeaponParent;

    // 위치 조정에 필요한 오브젝트들의 데이터들
    private GameObject _leftArm;
    private GameObject _rightArm;
    private SpriteRenderer _leftRenderer;
    private SpriteRenderer _rightRenderer;
    private Transform _leftWeaponLocation;
    private Transform _rightWeaponLocation;
    private GameObject _player;
    private GameObject _weaponRoot;
    private List<Transform> _weaponHoldingLocation;
    private Transform _weaponHoldingLocationRoot;
    private GameObject _clothes;
    private PlayerMovement _movement;

    // Flip 판별용 1, -1(뒤집힘) 으로 구분한다
    private int _playerFlip;
    private int _mouseFlip;
    /// <summary>
    /// 최종적으로 flip 되었는지
    /// </summary>
    private int _flip;

    // 장전에 사용된다
    /// <summary>
    /// 무기 교체 중인지, 교체 중엔 다른 행동은 안한다
    /// </summary>
    private bool _isSwitchWeapon = false; public bool IsSwitchWeapon { get => _isSwitchWeapon; }
    private bool _isHoldingWeapon = false;
    /// <summary>
    /// 무기 교체 종료 위치
    /// </summary>
    private Quaternion _switchLeftTargetRot;
    private Quaternion _switchRightTargetRot;
    /// <summary>
    /// 무기 교체 시작 위치
    /// </summary>
    private Quaternion _switchLeftOriRot;
    private Quaternion _switchRightOriRot;
    /// <summary>
    /// 교체 호출 전 위치
    /// </summary>
    private Quaternion _firstLeftTargetRot;
    private Quaternion _firstRightTargetRot;
    /// <summary>
    /// 교체 진행도
    /// </summary>
    private float _switchProgressTime;


    // Clothes 반동 관련
    public float ShakeAmount;
    public float HealShake = 0.5f; // 흔들림 회복 시간
    public float MaxShake = 15f; // 최대 흔들림 각도 제한

    // NoneUsed Hand 관련
    public float HandShakeAmount;
    public float HandHealShake = 2f; // 흔들림 회복 시간
    public float HandMoveShake = 1f;
    public float HandMaxShake = 15f; // 최대 흔들림 각도 제한


    /// <summary>
    /// 무기들 회전값 자체 보정 없으면 안됨 매직 넘버 맞음 건드리지 마삼
    /// </summary>
    const float HandAngleCorrection = 90;

    public void OnNotify(string state)
    {
    }

    public void Init()
    {
        InitObj();

        CurrentSlotIndex = WeaponSlot.FirstRanged;
        SetWeaponData();
        // HandAction = CheckAttack; // 공격했는지 체크
        HandAction += CheckSlotSwitch; // 슬롯이 바뀌었는지 체크
        HandAction += SetClothes;
        HandAction += SetHandShake;
        HandAction += CheckFlipData; // 방향이 바뀌었는지 체크 후 방향 적용
        HandAction += CheckCurrentSlotWeaponParent; // 무기의 위치가 바뀌었는지 체크 후 적용
        HandAction += SetArmRot; // 손의 방향을 설정

        SetSlotWeapons(); // 최초 시작시 일단 기본값으로 세팅
    }

    /// <summary>
    /// 오브젝트들 세팅 (동적 세팅) 이름 기반으로 감으로 Player 내부 오브젝트들 이름 변경 금지
    /// </summary>
    private void InitObj()
    {
        _mainCamera = Camera.main;
        _player = GameObject.FindWithTag("Player");
        _leftArm = _player.transform.Find("LeftArm").gameObject;
        _rightArm = _player.transform.Find("RightArm").gameObject;
        _leftRenderer = _leftArm.GetComponent<SpriteRenderer>();
        _rightRenderer = _rightArm.GetComponent<SpriteRenderer>();
        _leftWeaponLocation = _leftArm.transform.Find("LeftWeaponLocation");
        _rightWeaponLocation = _rightArm.transform.Find("RightWeaponLocation");
        _clothes = _player.transform.Find("Clothes").gameObject;
        _movement = _player.GetComponent<Player>().PlayerMovement;
        _weaponRoot = _player.transform.Find("WeaponRoot").gameObject;
        _weaponHoldingLocationRoot = _player.transform.Find("WeaponHoldingLocationRoot");
        _weaponHoldingLocation = new();
        for (int i = 0; i < _weaponHoldingLocationRoot.childCount; i++)
        {
            _weaponHoldingLocation.Add(_weaponHoldingLocationRoot.GetChild(i)); // 서순으로 가져오니까 순서대로 넣어!!
        }
    }


    /// <summary>
    /// _weaponRoot에 존재하는 모든 Weapon들 찾아서 _weapons에 넣어준다
    /// </summary>
    private void SetWeaponData()
    {
        var count = (int)EnumData.Weapon.None;
        _weapons = new Weapon[count];
        for (int i = 0; i < _weaponRoot.transform.childCount; i++)
        {
            var weapon = _weaponRoot.transform.GetChild(i);
            EnumData.Weapon enumWeapon = (EnumData.Weapon)Enum.Parse(typeof(EnumData.Weapon), weapon.name);
            _weapons[(int)enumWeapon] = weapon.GetComponent<Weapon>();
        }
    }

    /// <summary>
    /// 나머지 값들은 무시하고 현재 무기슬롯 데이터 상으로 등록 되어 있는 무기들로 전부 세팅하고 초기화한다
    /// 만약 무기를 교체 중이였다면 전부 초기화 시키고 세팅한다.
    /// </summary>
    public void SetSlotWeapons()
    {
        // 장전 관련 초기화

        int[] holdWeapons = new int[3] { (int)FirstSlot, (int)SecondSlot, (int)ThirdSlot };

        for (int i = 0; i < _weapons.Length; i++)
        {
            if (holdWeapons.Contains(i)) // 착용 목록에 있다면
            {
                if (!_weapons[i].gameObject.activeSelf)
                {
                    _weapons[i].gameObject.SetActive(true);
                }
                if (holdWeapons[(int)CurrentSlotIndex] == i) // 손에 들고 있는 무기라면 손에 장착시킨다
                {
                    _currentWeapon = _weapons[i]; // 현재 장착 중인 무기로 표시한다.
                    _currentWeapon.Renderer.sortingOrder = 4;
                    SetCurrentSlotWeapon(); // 장착 중인 무기를 메인 손에 장착
                }
                else // 손에 들고 있는 무기가 아니라면 홀딩 위치로 이동시킨다
                {
                    SetHoldSlotWeapon(_weapons[i]);
                }
            }
            else // 착용 목록에 없다면 WeaponRoot로 이동시킴. 만약 이미 WeaponRoot라면 스킵
            // 사용은 안하지만 활성화 되어있는 총들을 정리하기 위해 activeSelf로 처리
            {
                if (_weapons[i] == null)
                    continue;
                if (_weapons[i].gameObject.activeSelf)
                {
                    ReturnWeaponToRoot(_weapons[i]);
                }
            }
        }
    }

    public void SetClothes()
    {
        if (ShakeAmount > 0)
        {
            ShakeAmount -= HealShake * Time.deltaTime;
            if (ShakeAmount < 0)
                ShakeAmount = 0;
        }
        else
        {
            ShakeAmount += HealShake * Time.deltaTime;
            if (ShakeAmount > 0)
                ShakeAmount = 0;
        }

        _clothes.transform.localRotation = Quaternion.Euler(0f, 0f, ShakeAmount);
    }

    public void ShakeClothes(float shakeRange)
    {
        if (_flip == 1)
            ShakeAmount += UnityEngine.Random.Range(0, shakeRange);
        else
            ShakeAmount -= UnityEngine.Random.Range(0, shakeRange);

        ShakeAmount = Mathf.Clamp(ShakeAmount, -MaxShake, MaxShake);
    }

    public void SetHandShake()
    {
        ShakeHand();

        if (_playerFlip == 1)
        {
            HandShakeAmount -= HandHealShake * Time.deltaTime;
            if (HandShakeAmount < 0)
                HandShakeAmount = 0;
        }
        else
        {
            HandShakeAmount += HandHealShake * Time.deltaTime;
            if (HandShakeAmount > 0)
                HandShakeAmount = 0;
        }
    }

    public void ShakeHand()
    {
        HandShakeAmount -= _movement.MoveDirection.x * HandMoveShake * Time.deltaTime;
        HandShakeAmount = Mathf.Clamp(HandShakeAmount, -HandMaxShake, HandMaxShake);
    }


    private int _lastPlayerFlip = 1;
    private int _lastMouseFlip = 1;
    /// <summary>
    /// 현재플레이어 상황이 뒤집힌 상황인지 계산하고 적용시킨다
    /// </summary>
    private void CheckFlipData()
    {
        // 플레이어 Y축 방향 (1 : 기본 방향, -1 : 뒤집힌 방향)
        _playerFlip = _player.transform.localEulerAngles.y == 0 ? 1 : -1;
        if (_lastPlayerFlip != _playerFlip)
        {
            ShakeAmount = -ShakeAmount;
            HandShakeAmount = -HandShakeAmount;
        }
        _lastPlayerFlip = _playerFlip;

        if (!_currentWeapon.MouseFlipPrio)
            // 마우스가 플레이어 상대 방향 (1 : 왼쪽, -1 : 오른쪽)
            _mouseFlip = _player.transform.position.x > ScreenToWorld2D(Input.mousePosition, _mainCamera).x ? 1 : -1;


        // 3. 최종 방향 결정 (1: 기본, -1: 뒤집힘)
        _flip = _playerFlip * _mouseFlip;
        SetFlip();
    }

    /// <summary>
    /// 계산한 FipData를 기반으로 플레이어의 손, 무기의 Scale를 조정하고 손 교체 여부를 설정한다
    /// </summary>
    private void SetFlip()
    {
        // 팔과 무기의 크기 설정
        // 기본적으로 오른손에 총이 올라간경우 플립되어야한다. 따라서 flip, -flip으로 계산한다.
        // 하지만 뒤집힌 경우 반대로 바뀌기에 최정 방향을 기준으로 보정해준다.
        _leftArm.transform.localScale = new Vector3(_flip, 1, 1);
        _rightArm.transform.localScale = new Vector3(-_flip, 1, 1);

        // 360도로 회전시켜 방향을 나타내기에 우측으로 가는경우 (정방향 기준) y축 플립해줘야한다.
        // 하지만 이 또한 플레이어가 뒤집히면 반대로 바뀌기에 플레이어 방향을 기준으로 보정해준다.
        if (!_currentWeapon.WeaponScalePrio)
        {
            if (_currentWeapon.HandState.StandardHand) // 오른손에 있을때만
            {
                if (_currentWeapon.IsImageWidth)
                    _currentWeapon.transform.localScale = new Vector3(1, -_mouseFlip, 1);
                else
                    _currentWeapon.transform.localScale = new Vector3(-_mouseFlip, 1, 1);
            }
            else
                if (_currentWeapon.IsImageWidth)
                _currentWeapon.transform.localScale = new Vector3(1, _mouseFlip, 1);
            else
                _currentWeapon.transform.localScale = new Vector3(_mouseFlip, 1, 1);
        }

        if (_mouseFlip != _lastMouseFlip)
        {
            if (_currentWeapon.AttackType == WeaponAttackType.Charge)
                ((ChargeMeleeWeapon)_currentWeapon).SkipSwingEffect();
            if (_currentWeapon.AttackType == WeaponAttackType.Gage)
                ((GageMeleeWeapon)_currentWeapon).WeaponAngleCorrection();
        }
        _lastMouseFlip = _mouseFlip;

        // 5. _changeHand 플래그 설정
        _changeHand = (_flip == -1);
    }

    /// <summary>
    /// 양 쪽 손을 세팅한다
    /// </summary>
    private void SetArmRot()
    {
        if (_currentWeapon.HandPrio)
            return;
        _currentWeapon.HandLogic.SetLeftArm(_leftArm, HandShakeAmount, _leftRenderer, _currentWeapon, _changeHand, _mainCamera, _currentWeapon.IsHandFixed, _currentWeapon.FixedRot);
        _currentWeapon.HandLogic.SetRightArm(_rightArm, HandShakeAmount, _rightRenderer, _currentWeapon, _changeHand, _mainCamera, _currentWeapon.IsHandFixed, _currentWeapon.FixedRot);
    }

    /// <summary>
    /// Slot을 변경하는지를 체크한다
    /// </summary>
    public void CheckSlotSwitch() // _crruentWeapon만 결국 변경되면 됨
    {
        if (Input.GetKeyDown(SlotChangeKey))
        {
            if (_isSwitchWeapon) return;
            if (CurrentSlotIndex == WeaponSlot.FirstRanged) CurrentSlotIndex = WeaponSlot.SecondRanged;
            else if (CurrentSlotIndex == WeaponSlot.SecondRanged) CurrentSlotIndex = WeaponSlot.FirstRanged;
            SwitchWeapon();
        }
        if (Input.GetKeyDown(SlotChangeTo1Key))
        {
            if (_isSwitchWeapon)
                return;
            CurrentSlotIndex = WeaponSlot.FirstRanged;
            SwitchWeapon();
        }
        if (Input.GetKeyDown(SlotChangeTo2Key))
        {
            if (_isSwitchWeapon)
                return;
            CurrentSlotIndex = WeaponSlot.SecondRanged;
            SwitchWeapon();
        }

        /*
        if (Input.GetKeyDown(SlotChangeTo3Key))
        {
            if (_isSwitchWeapon)
                return;
            CurrentSlotIndex = WeaponSlot.FirstMelee;
            SwitchWeapon();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_isSwitchWeapon)
                return;
            FirstSlot = (EnumData.Weapon)(((int)FirstSlot + 3) % 8);
            SecondSlot = (EnumData.Weapon)(((int)SecondSlot + 3) % 8);
            ThirdSlot = (EnumData.Weapon)(((int)ThirdSlot + 3) % 8);

            SetSlotWeapons();
        }
        */
    }

    public void SwitchWeapon()
    {
        foreach (Melee melee in melees) melee.Load();

        _isSwitchWeapon = true;
        HandAction -= CheckFlipData;
        HandAction -= CheckCurrentSlotWeaponParent;
        HandAction -= SetArmRot; // 교체 중엔 일단 스탑

        _switchLeftTargetRot = ForwardToObj(_leftArm, _weaponHoldingLocation[(int)_currentWeapon.HoldingIndex].position, HandAngleCorrection);
        _switchRightTargetRot = ForwardToObj(_rightArm, _weaponHoldingLocation[(int)_currentWeapon.HoldingIndex].position, HandAngleCorrection);
        _firstLeftTargetRot = _leftArm.transform.rotation;
        _firstRightTargetRot = _rightArm.transform.rotation;
        _switchLeftOriRot = _leftArm.transform.rotation;
        _switchRightOriRot = _rightArm.transform.rotation;
        _switchProgressTime = 0;

        _currentWeapon.InitBeforeChange();
        HandAction += HoldStandardWeapon;
    }

    private void HoldStandardWeapon() // 무기를 홀더에 넣는 과정, 실행되면 무조건 끝나야함 !최우선!
    {
        _isHoldingWeapon = true;
        _switchProgressTime += Time.deltaTime; // 이동은 시간 비례로 이동
        _leftArm.transform.rotation = Quaternion.Slerp(_switchLeftOriRot, _switchLeftTargetRot, _switchProgressTime / (WeaponSwitchTime / 2f));
        _rightArm.transform.rotation = Quaternion.Slerp(_switchRightOriRot, _switchRightTargetRot, _switchProgressTime / (WeaponSwitchTime / 2f));
        if (_switchProgressTime >= (WeaponSwitchTime - 0.4f) / 2f) // 이동 완료
        {
            _leftArm.transform.rotation = _switchLeftTargetRot;
            _rightArm.transform.rotation = _switchRightTargetRot;

            _switchProgressTime = 0;

            HandAction -= HoldStandardWeapon;
            HandAction += WaitHold;
        }
    }

    private void WaitHold()
    {
        _switchProgressTime += Time.deltaTime;
        if (_switchProgressTime >= 0.2f) // 이동 완료
        {
            SetHoldSlotWeapon(_currentWeapon);
            _isHoldingWeapon = false;

            _currentWeapon = _weapons[(int)CurrentSlotWeaponType];

            _switchLeftTargetRot = ForwardToObj(_leftArm, _weaponHoldingLocation[(int)_currentWeapon.HoldingIndex].position, HandAngleCorrection);
            _switchRightTargetRot = ForwardToObj(_rightArm, _weaponHoldingLocation[(int)_currentWeapon.HoldingIndex].position, HandAngleCorrection);
            _switchLeftOriRot = _leftArm.transform.rotation;
            _switchRightOriRot = _rightArm.transform.rotation;

            _switchProgressTime = 0;

            HandAction -= WaitHold;
            HandAction += DrawStandardWeapon;
        }
    }

    private void DrawStandardWeapon() // 무기를 넣은 상태에서 선택된 무기를 꺼내는 과정
    {
        _switchProgressTime += Time.deltaTime; // 이동은 시간 비례로 이동
        _leftArm.transform.rotation = Quaternion.Slerp(_switchLeftOriRot, _switchLeftTargetRot, _switchProgressTime / (WeaponSwitchTime / 2f));
        _rightArm.transform.rotation = Quaternion.Slerp(_switchRightOriRot, _switchRightTargetRot, _switchProgressTime / (WeaponSwitchTime / 2f));

        if (_switchProgressTime >= (WeaponSwitchTime - 0.4f) / 2f) // 이동 완료
        {
            _leftArm.transform.rotation = _switchLeftTargetRot;
            _rightArm.transform.rotation = _switchRightTargetRot;
            _switchLeftOriRot = _leftArm.transform.rotation;
            _switchRightOriRot = _rightArm.transform.rotation;
            SetCurrentSlotWeapon();
            _currentWeapon.Renderer.sortingOrder = 4;
            _switchProgressTime = 0;

            HandAction -= DrawStandardWeapon; // 주무기까지 꺼냄
            HandAction += WaitDraw;
        }
    }

    private void WaitDraw()
    {
        _switchProgressTime += Time.deltaTime;
        if (_switchProgressTime >= 0.2f) // 이동 완료
        {
            _switchProgressTime = 0;

            HandAction -= WaitDraw;
            HandAction += ReadyStandardWeapon;
        }
    }

    private void ReadyStandardWeapon() // 무기를 넣은 상태에서 선택된 무기를 꺼내는 과정
    {
        _switchProgressTime += Time.deltaTime; // 이동은 시간 비례로 이동
        _leftArm.transform.rotation = Quaternion.Slerp(_switchLeftOriRot, _firstLeftTargetRot, _switchProgressTime / (WeaponSwitchTime / 2f));
        _rightArm.transform.rotation = Quaternion.Slerp(_switchRightOriRot, _firstLeftTargetRot, _switchProgressTime / (WeaponSwitchTime / 2f));

        if (_switchProgressTime >= 0.2f) // 이동 완료
        {
            _leftArm.transform.rotation = _firstLeftTargetRot;
            _rightArm.transform.rotation = _firstRightTargetRot;


            HandAction -= ReadyStandardWeapon;
            HandAction += CheckFlipData;
            HandAction += CheckCurrentSlotWeaponParent;
            HandAction += SetArmRot;
            _isSwitchWeapon = false;
        }
    }

    public void CheckAttack()
    {
        if (CurrentSlotWeaponType == EnumData.Weapon.None)
            return;
        if (_isSwitchWeapon)
            return;
        if (Input.GetMouseButtonDown(AttackKeyIndex))
        {
            if (_currentWeapon != null)
                _currentWeapon.AttackStart();
        }
        if (Input.GetMouseButton(AttackKeyIndex))
        {
            if (!_currentWeapon.IsReload && _currentWeapon.AttackType == Weapon.WeaponAttackType.Ranged || _currentWeapon.AttackType == Weapon.WeaponAttackType.Melee)
                ShakeClothes(_currentWeapon.ClothesShake);
            if (_currentWeapon.AttackType == Weapon.WeaponAttackType.Gage)
                ShakeClothes(_currentWeapon.ClothesShake);
            if (_currentWeapon != null)
                _currentWeapon.Attack();
        }
        if (Input.GetMouseButtonUp(AttackKeyIndex))
        {
            if (!_currentWeapon.IsReload && _currentWeapon.AttackType == Weapon.WeaponAttackType.Charge)
                ShakeClothes(_currentWeapon.ClothesShake);
            if (_currentWeapon != null)
                _currentWeapon.AttackEnd();
        }
    }

    private void SetCurrentSlotWeapon()
    {
        SetCurrentSlotWeaponParent();
        InitCurrentSlotSetting();
    }

    private void CheckCurrentSlotWeaponParent()
    {
        if (_lastWeaponParent != IsRightHandStandard(_currentWeapon, _changeHand))
            SetCurrentSlotWeaponParent();
    }

    private void SetCurrentSlotWeaponParent()
    {
        if (IsRightHandStandard(_currentWeapon, _changeHand))
        {
            _currentWeapon.transform.SetParent(_rightWeaponLocation, false);
            _lastWeaponParent = true;
        }
        else
        {
            _currentWeapon.transform.SetParent(_leftWeaponLocation, false);
            _lastWeaponParent = false;
        }
        _currentWeapon.transform.localPosition = Vector3.zero;
        if (_currentWeapon.AttackType == WeaponAttackType.Gage)
            ((GageMeleeWeapon)_currentWeapon).ShakeWeaponCorrection();
    }

    private void InitCurrentSlotSetting()
    {
        _currentWeapon.SetMain();
    }

    private void SetHoldSlotWeaponParent(WeaponSlot targetSlot)
    {
        var weapon = _weapons[(int)GetSlotWeaponType(targetSlot)];
        weapon.transform.SetParent(_weaponHoldingLocation[(int)_currentWeapon.HoldingIndex], false);
        weapon.Renderer.sortingOrder = (int)_currentWeapon.HoldingOrder;
    }

    private void SetHoldSlotWeapon(Weapon targetWeapon)
    {
        SetHoldSlotWeaponParent(targetWeapon);
        InitHoldSlotWeaponSetting(targetWeapon);
    }

    private void SetHoldSlotWeaponParent(Weapon targetWeapon)
    {
        targetWeapon.transform.SetParent(_weaponHoldingLocation[(int)targetWeapon.HoldingIndex], false);
        targetWeapon.Renderer.sortingOrder = (int)targetWeapon.HoldingOrder;
        targetWeapon.transform.localPosition = Vector3.zero;
        targetWeapon.transform.localScale = Vector3.one;
    }

    private void InitHoldSlotWeaponSetting(Weapon targetWeapon)
    {
        targetWeapon.InitSetHold();
    }

    private void ReturnWeaponToRoot(Weapon weapon)
    {
        weapon.gameObject.SetActive(false);
        weapon.transform.SetParent(_weaponRoot.transform, false);
    }

    private EnumData.Weapon GetSlotWeaponType(WeaponSlot slot)
    {
        return slot switch
        {
            WeaponSlot.FirstRanged => FirstSlot,
            WeaponSlot.SecondRanged => SecondSlot,
            WeaponSlot.FirstMelee => ThirdSlot,
            _ => throw new NotImplementedException()
        };
    }
}