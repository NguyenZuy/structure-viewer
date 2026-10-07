using System;
using System.Collections.Generic;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Structure;
using StructureViewer.Infrastructure.Parsing;
using StructureViewer.Infrastructure.Quality;
using StructureViewer.Presentation.CameraControl;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Input;
using StructureViewer.Presentation.Shell;
using StructureViewer.Presentation.Structure;
using UnityEngine;

namespace StructureViewer.Bootstrap
{
    // Composition root: the only place that constructs and wires services, presenters and views.
    public sealed class AppBootstrap : MonoBehaviour
    {
        public const string FitAllId = "fit-all";

        [SerializeField] private TextAsset _structureJson;
        [SerializeField] private RenderingConfig _rendering;
        [SerializeField] private StructureRenderer _renderer;
        [SerializeField] private CameraController _camera;
        [SerializeField] private PointerInput _pointer;
        [SerializeField] private ShellView _shell;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private EventBus _bus;
        private StructureSession _session;
        private LoadStructureUseCase _load;

        private void Awake() => QualitySelector.Apply();

        // Start, not Awake: ShellView builds its UI tree in OnEnable.
        private void Start()
        {
            _bus = new EventBus();
            _session = new StructureSession();
            _load = new LoadStructureUseCase(new JsonStructureParser(), _session, _bus);

            _renderer.Camera = _camera.Camera;
            _camera.Bind(_pointer);
            _disposables.Add(_bus.Subscribe<StructureLoaded>(OnStructureLoaded));

            _shell.AddToolbarItem(new ToolbarItem(FitAllId, "Fit all", "⌂", "Fit the whole model in view", FitAll, priority: 100));

            Load(_structureJson);
        }

        private void Load(TextAsset json)
        {
            if (json == null)
            {
                _shell.ShowToast("No structure file is assigned.", isError: true);
                return;
            }

            var result = _load.Execute(json.text);
            if (result.Success)
                return;

            string more = result.Errors.Count > 1 ? $" (+{result.Errors.Count - 1} more)" : string.Empty;
            _shell.ShowToast($"Could not load {json.name}: {result.Errors[0]}{more}", isError: true);
            foreach (var error in result.Errors)
                Debug.LogWarning($"{json.name}: {error}");
        }

        private void OnStructureLoaded(StructureLoaded evt)
        {
            _renderer.Build(evt.Model, RealisticMaterial);
            _camera.FitAll(_renderer.ModelBounds);
        }

        private Material RealisticMaterial(Element element) =>
            element.Kind switch
            {
                ElementKind.Slab => _rendering.Concrete,
                ElementKind.Panel => _rendering.Sheathing,
                _ => _rendering.Wood
            };

        private void FitAll()
        {
            if (_session.HasModel)
                _camera.FitAll(_renderer.ModelBounds);
        }

        private void OnDestroy()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
            _disposables.Clear();
        }
    }
}
