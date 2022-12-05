using Unity.Mathematics;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float2 _sizeMinMax;
    [SerializeField] private float _zoomSpeed = 1f;
    [SerializeField] private float _moveSpeed = 1f;
    private float _targetSize;
    private CameraControl _cameraControl;

    private const float DeltaSizeThreshold = 0.01f;

    private void Start()
    {
        _cameraControl = new();
        _cameraControl.Enable(); // enable all actions
        _targetSize = _camera.orthographicSize;
        _cameraControl.Camera.Zoom.performed += (context) =>
        {
            var zoom = context.ReadValue<float>();

            // adjust target size depending on zoom value if zoom value has same direction as previous
            // otherwise turn target size to opposite direction
            _targetSize = math.clamp((math.sign(zoom) == math.sign(_targetSize) ? _targetSize : _camera.orthographicSize) + zoom, _sizeMinMax.x, _sizeMinMax.y);
        };
        _cameraControl.Camera.Move.performed += (context) =>
        {
            var moveOffset = context.ReadValue<Vector2>() * _moveSpeed;
            _camera.transform.Translate(moveOffset.x, moveOffset.y, 0f);
        };
    }
    private void Update()
    {
        var deltaSize = math.abs(_camera.orthographicSize - _targetSize);
        if(deltaSize > DeltaSizeThreshold)
            _camera.orthographicSize = math.lerp(_camera.orthographicSize, _targetSize, math.saturate(Time.deltaTime * _zoomSpeed / deltaSize));
    }
    private void OnEnable()
    {
        _cameraControl?.Enable();
    }
    private void OnDisable()
    {
        _cameraControl?.Disable();
    }
}