using MinhaVila.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MinhaVila.Unity
{
    public sealed class CameraController : MonoBehaviour
    {
        private GameController _game;
        private Camera _camera;
        private Vector3 _target = Vector3.zero;
        private Vector2 _pointerStart;
        private Vector2 _lastPointer;
        private bool _pressed;
        private bool _dragging;
        private float _zoom = 18;
        private float _lastPinchDistance;

        public float Zoom { get { return _zoom; } }
        public Vector3 Target { get { return _target; } }

        public void Initialize(GameController game)
        {
            _game = game;
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = gameObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = _zoom;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 180;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            transform.rotation = Quaternion.Euler(52, 0, 0);
            ApplyCamera();
        }

        public void SetCamera(double x, double z, double zoom)
        {
            _target = new Vector3((float)x, 0, (float)z);
            _zoom = Mathf.Clamp((float)zoom, 8, 34);
            ApplyCamera();
        }

        private void Update()
        {
            if (_game == null || _camera == null) return;
            HandleMouse();
            HandleTouch();
            if (Input.mouseScrollDelta.y != 0 && !IsPointerOverUi())
            {
                ZoomBy(-Input.mouseScrollDelta.y * 1.2f);
            }
        }

        private void HandleMouse()
        {
            if (Input.touchCount > 0)
            {
                _pressed = false;
                _dragging = false;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUi()) return;
                _pressed = true;
                _dragging = false;
                _pointerStart = Input.mousePosition;
                _lastPointer = _pointerStart;
            }

            if (_pressed && Input.GetMouseButton(0))
            {
                Vector2 pointer = Input.mousePosition;
                if (!_dragging && Vector2.Distance(pointer, _pointerStart) > 8) _dragging = true;
                if (_dragging) Pan(_lastPointer, pointer);
                _lastPointer = pointer;
            }

            if (_pressed && Input.GetMouseButtonUp(0))
            {
                Vector2 pointer = Input.mousePosition;
                if (!_dragging && !IsPointerOverUi()) HandleTap(pointer);
                _pressed = false;
                _dragging = false;
            }
        }

        private void HandleTouch()
        {
            if (Input.touchCount == 2)
            {
                _pressed = false;
                Touch first = Input.GetTouch(0);
                Touch second = Input.GetTouch(1);
                float distance = Vector2.Distance(first.position, second.position);
                if (_lastPinchDistance > 0) ZoomBy((_lastPinchDistance - distance) * 0.025f);
                _lastPinchDistance = distance;
                return;
            }

            _lastPinchDistance = 0;
            if (Input.touchCount != 1) return;
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverUi(touch.fingerId)) return;
                _pressed = true;
                _dragging = false;
                _pointerStart = touch.position;
                _lastPointer = _pointerStart;
            }
            else if (_pressed && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                Vector2 pointer = touch.position;
                if (!_dragging && Vector2.Distance(pointer, _pointerStart) > 10) _dragging = true;
                if (_dragging) Pan(_lastPointer, pointer);
                _lastPointer = pointer;
            }
            else if (_pressed && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                if (!_dragging && touch.phase == TouchPhase.Ended) HandleTap(touch.position);
                _pressed = false;
                _dragging = false;
            }
        }

        private void Pan(Vector2 previous, Vector2 current)
        {
            Vector2 delta = current - previous;
            float worldPerPixel = _zoom * 2f / Mathf.Max(1, Screen.height);
            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            _target -= right * delta.x * worldPerPixel;
            _target -= forward * delta.y * worldPerPixel;
            ClampTarget();
            ApplyCamera();
        }

        private void ZoomBy(float delta)
        {
            _zoom = Mathf.Clamp(_zoom + delta, 8, 34);
            ApplyCamera();
        }

        private void HandleTap(Vector2 screenPosition)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float distance)) return;
            Vector3 point = ray.GetPoint(distance);
            GridPoint tile = TownRenderer.WorldToTile(point);
            if (tile.X < 0 || tile.Z < 0 || tile.X >= 48 || tile.Z >= 48) return;
            _game.HandleWorldTileClicked(tile);
        }

        private bool IsPointerOverUi(int pointerId = -1)
        {
            return EventSystem.current != null && (pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject());
        }

        private void ClampTarget()
        {
            _target.x = Mathf.Clamp(_target.x, -22, 22);
            _target.z = Mathf.Clamp(_target.z, -22, 22);
        }

        private void ApplyCamera()
        {
            if (_camera == null) return;
            _camera.orthographicSize = _zoom;
            transform.position = _target - transform.forward * 38 + Vector3.up * 10;
            transform.LookAt(_target);
        }
    }
}
