using System;
using System.Collections.Generic;
using StructureViewer.Application.Commands;
using StructureViewer.Application.Display;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Selection;
using StructureViewer.Application.Visibility;
using StructureViewer.Domain.Display;
using StructureViewer.Infrastructure.Parsing;
using StructureViewer.Infrastructure.Quality;
using StructureViewer.Presentation.CameraControl;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Display;
using StructureViewer.Presentation.Info;
using StructureViewer.Presentation.Input;
using StructureViewer.Presentation.Labels;
using StructureViewer.Presentation.Layers;
using StructureViewer.Presentation.Measure;
using StructureViewer.Presentation.Shell;
using StructureViewer.Presentation.Structure;
using StructureViewer.Presentation.Takeoff;
using StructureViewer.Presentation.Viewport;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Bootstrap
{
    // Composition root: the only place that constructs and wires services, presenters and views.
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset _structureJson;
        [SerializeField] private RenderingConfig _rendering;
        [SerializeField] private DisplayPaletteAsset _palette;
        [SerializeField] private StructureRenderer _renderer;
        [SerializeField] private CameraController _camera;
        [SerializeField] private PointerInput _pointer;
        [SerializeField] private ShortcutInput _shortcuts;
        [SerializeField] private ShellView _shell;
        [SerializeField] private MeasureView _measureView;
        [SerializeField] private LabelsView _labelsView;
        [SerializeField] private VisualTreeAsset _infoLayout;
        [SerializeField] private VisualTreeAsset _layersLayout;
        [SerializeField] private VisualTreeAsset _legendLayout;
        [SerializeField] private VisualTreeAsset _takeoffLayout;

        // Disposed in reverse creation order.
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private LoadStructureUseCase _load;
        private MaterialApplier _materials;

        // Exposed for end-to-end tests; nothing else reads these.
        public EventBus Bus { get; private set; }
        public StructureSession Session { get; private set; }
        public SelectionService Selection { get; private set; }
        public VisibilityActions Visibility { get; private set; }
        public VisibilityService VisibilityState { get; private set; }
        public DisplaySettings Display { get; private set; }
        public MeasurePresenter Measure { get; private set; }
        public AppActions Actions { get; private set; }

        private void Awake() => QualitySelector.Apply();

        // Start, not Awake: ShellView builds its UI tree in OnEnable.
        private void Start()
        {
            if (!HasAllReferences())
                return;

            Bus = new EventBus();
            Session = new StructureSession();
            var history = new CommandHistory();
            _load = new LoadStructureUseCase(new JsonStructureParser(), Session, Bus);

            _renderer.Camera = _camera.Camera;
            _camera.Bind(_pointer);
            _measureView.Camera = _camera.Camera;
            _labelsView.Camera = _camera.Camera;
            _shell.AddViewportOverlay(_labelsView.Overlay);
            _shell.AddViewportOverlay(_measureView.Overlay);

            // Subscribed first so the renderer is rebuilt before any feature reacts to a load.
            Own(Bus.Subscribe<StructureLoaded>(OnStructureLoaded));

            Display = new DisplaySettings(Bus);
            _materials = Own(new MaterialApplier(_renderer, _rendering, _palette, Display, Bus));
            Selection = Own(new SelectionService(Session, Bus));
            VisibilityState = Own(new VisibilityService(Bus));
            Visibility = Own(new VisibilityActions(VisibilityState, history, Session, Bus));
            Own(new VisibilityApplier(_renderer, Bus));
            var viewport = Own(new ViewportPresenter(_pointer, _renderer, _camera, Selection, Bus));

            var info = new InfoPanelView(_infoLayout);
            var infoPresenter = Own(new InfoPanelPresenter(info, Session, Selection, Bus));
            var layers = new LayerPanelView(_layersLayout);
            Own(new LayerPanelPresenter(layers, Visibility, VisibilityState, Session, Bus));
            var legend = new LegendView(_legendLayout);
            Own(new LegendPresenter(legend, Display, Session, _palette, Bus));
            var takeoff = new TakeoffView(_takeoffLayout);
            var takeoffPresenter = Own(new TakeoffPresenter(takeoff, Session, Bus));
            var picker = new DisplayPickerView();
            Own(new DisplayPickerPresenter(picker, Display, Bus));
            Measure = Own(new MeasurePresenter(_pointer, _renderer, _measureView, Session, Bus));
            var labels = Own(new LabelsPresenter(_labelsView, Bus));

            var panels = Own(new PanelLayout(_shell, info.Root, layers.Root, legend.Root, takeoff.Root, picker.Root));
            Actions = Own(new AppActions(_shell, _camera, _renderer, Session, viewport, Visibility, VisibilityState, history,
                Display, Measure, labels, panels, Bus));
            Actions.RegisterToolbar();
            _shortcuts.Triggered += Actions.Execute;

            // Cross-feature reactions.
            Own(Bus.Subscribe<StructureLoaded>(_ => history.Clear()));
            Own(Bus.Subscribe<VisibilityChanged>(evt => Selection.RemoveWhere(i => !evt.Visibility.IsVisible(i))));
            Own(Bus.Subscribe<SelectionChanged>(evt => panels.OnSelectionChanged(!evt.Selection.IsEmpty)));
            infoPresenter.IsolateAssemblyRequested += group => Visibility.IsolateGroup(group);
            var typeColors = new TypeColorProvider(_palette, Session);
            Own(Bus.Subscribe<DisplayModeChanged>(evt =>
                takeoffPresenter.SetSwatchProvider(evt.Mode == DisplayMode.ColorBy && evt.Field == ColorByField.Type ? typeColors.ColorOf : null)));

            Load(_structureJson);
        }

        // A scene from an older setup misses newer references; say how to fix it instead of failing with a NullReferenceException.
        private bool HasAllReferences()
        {
            var missing = new List<string>();
            void Check(UnityEngine.Object value, string name)
            {
                if (value == null)
                    missing.Add(name);
            }

            Check(_rendering, nameof(_rendering));
            Check(_palette, nameof(_palette));
            Check(_renderer, nameof(_renderer));
            Check(_camera, nameof(_camera));
            Check(_pointer, nameof(_pointer));
            Check(_shortcuts, nameof(_shortcuts));
            Check(_shell, nameof(_shell));
            Check(_measureView, nameof(_measureView));
            Check(_labelsView, nameof(_labelsView));
            Check(_infoLayout, nameof(_infoLayout));
            Check(_layersLayout, nameof(_layersLayout));
            Check(_legendLayout, nameof(_legendLayout));
            Check(_takeoffLayout, nameof(_takeoffLayout));
            if (missing.Count == 0)
                return true;

            Debug.LogError($"AppBootstrap is missing {string.Join(", ", missing)}. Run Tools > Structure Viewer > Setup Scene.", this);
            return false;
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

        // The applier's base material makes a fresh build match the current display mode.
        private void OnStructureLoaded(StructureLoaded evt)
        {
            _renderer.Build(evt.Model, _materials.BaseMaterial);
            _camera.FitAll(_renderer.ModelBounds);
        }

        private T Own<T>(T disposable) where T : IDisposable
        {
            _disposables.Add(disposable);
            return disposable;
        }

        private void OnDestroy()
        {
            if (_shortcuts != null && Actions != null)
                _shortcuts.Triggered -= Actions.Execute;
            for (int i = _disposables.Count - 1; i >= 0; i--)
                _disposables[i].Dispose();
            _disposables.Clear();
        }
    }
}
