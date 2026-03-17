using UnityEngine;
using UnityEngine.InputSystem;

public class SSALIcon : MonoBehaviour
{
    public Sprite RiceSprite;
    public Sprite RiceDropSprite;
    public Sprite GrainSprite;
    public Sprite GrainDropSprite;
    public Sprite SandSprite;
    public Sprite SandDropSprite;
    public Sprite DefaultSprite;

    public SpriteRenderer SSALIconObj;
    public GameObject SpawnerArea;

    private float _previousX;
    private float _tiltSensitivity = 5.0f;
    private float _maxTiltAngle = 40.0f;
    private float _smoothTime = 15.0f;

    void Update()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10.0f));

        if (SpawnerManager.Instance.CurrentMode == SSALMode.Default)
        {
            SSALIconObj.sprite = DefaultSprite;
            SSALIconObj.transform.position = worldPos;
            return;
        }

        float deltaX = (worldPos.x - _previousX) / Time.deltaTime;
        float targetZ = -deltaX * _tiltSensitivity;
        targetZ = Mathf.Clamp(targetZ, -_maxTiltAngle, _maxTiltAngle);

        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZ);
        SSALIconObj.transform.rotation = Quaternion.Lerp(SSALIconObj.transform.rotation, targetRotation, Time.deltaTime * _smoothTime);

        _previousX = worldPos.x;
        SSALIconObj.transform.position = worldPos;

        float xMin = SpawnerArea.transform.position.x - (SpawnerArea.transform.lossyScale.x / 2);
        float xMax = SpawnerArea.transform.position.x + (SpawnerArea.transform.lossyScale.x / 2);
        float yMin = SpawnerArea.transform.position.y - (SpawnerArea.transform.lossyScale.y / 2);
        float yMax = SpawnerArea.transform.position.y + (SpawnerArea.transform.lossyScale.y / 2);

        bool isInSpawnArea = (worldPos.x >= xMin && worldPos.x <= xMax &&
            worldPos.y >= yMin && worldPos.y <= yMax);

        if (isInSpawnArea) SSALIconObj.color = Color.white;
        else SSALIconObj.color = Color.gray;

        bool isPressed = Mouse.current.leftButton.isPressed && isInSpawnArea;

        if (SpawnerManager.Instance.CurrentMode == SSALMode.Rice)
        {
            SSALIconObj.sprite = isPressed ? RiceDropSprite : RiceSprite;
        }
        else if (SpawnerManager.Instance.CurrentMode == SSALMode.Sand)
        {
            SSALIconObj.sprite = isPressed ? SandDropSprite : SandSprite;
        }
        else if (SpawnerManager.Instance.CurrentMode == SSALMode.Grain)
        {
            SSALIconObj.sprite = isPressed ? GrainDropSprite : GrainSprite;
        }
    }
}