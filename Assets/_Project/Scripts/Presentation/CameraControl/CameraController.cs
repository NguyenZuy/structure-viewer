using StructureViewer.Presentation.Contracts;
using UnityEngine;

namespace StructureViewer.Presentation.CameraControl
{
    // Gestures move a target pose; the camera eases toward it every frame, which also animates FitAll/Focus.
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour, ICameraControl
    {
        // 3/4 view from the front-left: looking toward +X/+Z, i.e. standing at -X/-Z.
        public const float DefaultYaw = 45f;
        public const float DefaultPitch = 30f;
        private const float MinDistance = 0.3f;

        // Degrees per full screen height of drag; fingers cover more of a small screen, so touch is slower.
        [SerializeField] private float _mouseOrbitPerScreen = 300f;
        [SerializeField] private float _touchOrbitPerScreen = 240f;
        // Higher = snappier; 10 settles FitAll/Focus in ~0.3–0.4 s.
        [SerializeField] private float _sharpness = 10f;

        private Camera _camera;
        private IPointerEvents _pointer;
        private OrbitState _target;
        private OrbitState _current;

        public Camera Camera => _camera;
        public Vector3 Pivot => _target.Pivot;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _target = new OrbitState(Vector3.zero, DefaultYaw, DefaultPitch, 20f, MinDistance, 200f);
            _current = _target;
            Apply();
        }

        public void Bind(IPointerEvents pointer)
        {
            Unbind();
            _pointer = pointer;
            _pointer.Orbited += OnOrbited;
            _pointer.Panned += OnPanned;
            _pointer.Zoomed += OnZoomed;
        }

        public void FitAll(Bounds bounds)
        {
            float distance = FitDistance(bounds);
            _target = _target.WithLimits(MinDistance, Mathf.Max(distance * 4f, 5f)).WithFrame(bounds.center, distance);
        }

        public void Focus(Bounds bounds) => _target = _target.WithFrame(bounds.center, FitDistance(bounds));

        private float FitDistance(Bounds bounds) =>
            CameraFraming.FitDistance(bounds, _target.Rotation, _camera.fieldOfView, _camera.aspect);

        private void OnOrbited(Vector2 pixels)
        {
            float perScreen = _pointer.Current == PointerDevice.Touch ? _touchOrbitPerScreen : _mouseOrbitPerScreen;
            _target = _target.Orbit(pixels * (perScreen / _camera.pixelHeight));
        }

        private void OnPanned(Vector2 pixels)
        {
            float tanV = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _target = _target.Pan(pixels, 2f * _target.Distance * tanV / _camera.pixelHeight);
        }

        private void OnZoomed(float amount, Vector2 focus)
        {
            var viewport = new Vector2(focus.x / _camera.pixelWidth, focus.y / _camera.pixelHeight);
            _target = _target.Zoom(amount, _target.ViewportDirection(viewport, _camera.fieldOfView, _camera.aspect));
        }

        private void LateUpdate()
        {
            _current = OrbitState.Lerp(_current, _target, 1f - Mathf.Exp(-_sharpness * Time.unscaledDeltaTime));
            Apply();
        }

        private void Apply() => transform.SetPositionAndRotation(_current.Position, _current.Rotation);

        private void Unbind()
        {
            if (_pointer == null)
                return;
            _pointer.Orbited -= OnOrbited;
            _pointer.Panned -= OnPanned;
            _pointer.Zoomed -= OnZoomed;
            _pointer = null;
        }

        private void OnDestroy() => Unbind();
    }
}
