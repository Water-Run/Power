using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Power.Assets;
using Power.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Power.Studio
{
    [DisallowMultipleComponent]
    public sealed class PowerStudio : MonoBehaviour
    {
        // Unity schedules presentation work; the model owns its integer simulation clock.
        public const ulong PresentationStepNanoseconds = 20_000_000;
        private const ulong MaxPresentationTicks = 2000;
        [SerializeField] private PowerModelAsset modelAsset;
        private readonly List<Material> _materials = new List<Material>();
        private readonly Dictionary<uint, Transform> _rotors = new Dictionary<uint, Transform>();
        private readonly Dictionary<uint, Material> _thermalMaterials = new Dictionary<uint, Material>();
        private readonly Dictionary<ulong, TextField> _inputFields = new Dictionary<ulong, TextField>();
        private readonly Dictionary<ulong, double> _inputValues = new Dictionary<ulong, double>();
        private readonly Dictionary<ulong, int> _valueIndices = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, Label> _outputLabels = new Dictionary<ulong, Label>();
        private PowerAsset _asset;
        private AssetPlayback _playback;
        private Simulation _simulation;
        private Scalar[] _values;
        private SnapshotInfo _snapshot;
        private GameObject _world;
        private GameObject _ui;
        private PanelSettings _panel;
        private Camera _camera;
        private ulong _firstSpeed, _secondSpeed, _firstCurrent, _firstTemperature;
        private Label _timeLabel;
        private Label _stateLabel;
        private Label _motorLabel;
        private Label _loadLabel;
        private Label _currentLabel;
        private Label _temperatureLabel;
        private Label _balanceLabel;
        private Button _pauseButton;
        private TelemetryPlot _plot;
        private bool _running;
        private int _displayEvent;
        private string _outcome = "";
        private double _nextTelemetryTime;
        private float _yaw;
        private float _pitch;
        private float _distance;
        private int _dragPointer = -1;
        private Vector2 _lastPointer;

        public ulong SimulationTimeNanoseconds => _snapshot.TimeNanoseconds;
        public ulong StateHash => _snapshot.StateHash;
        public bool IsRunning => _running;
        public string ModelName => _asset == null ? "" : _asset.Name;

        public void SetModelAsset(PowerModelAsset source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            source.Load(); // Validate before replacing a running laboratory.
            bool rebuild = Application.isPlaying && isActiveAndEnabled;
            if (rebuild) OnDisable();
            modelAsset = source;
            if (rebuild) OnEnable();
        }

        private void OnEnable()
        {
            _yaw = -25;
            _pitch = 26;
            _distance = 12;
            _dragPointer = -1;
            try
            {
                var source = modelAsset != null ? modelAsset : Resources.Load<PowerModelAsset>("Electrothermal");
                if (source == null) throw new InvalidOperationException("No model asset. Run tools/Build.cs build or assign a .powerasset to the Studio.");
                _asset = source.Load();
                _values = new Scalar[_asset.Model.OutputCount];
                _asset.Model.CreateSimulation().ReadSnapshot(_values);
                for (int i = 0; i < _values.Length; ++i) _valueIndices.Add(_values[i].Channel, i);
                var speeds = _asset.Model.Channels.Where(c => !c.IsInput && c.Quantity == "speed").Select(c => c.Id).ToArray();
                _firstSpeed = speeds.Length > 0 ? speeds[0] : 0;
                _secondSpeed = speeds.Length > 1 ? speeds[1] : 0;
                _firstCurrent = FirstOutput("current");
                _firstTemperature = FirstOutput("temperature");
                BuildWorld();
                BuildInterface();
                ResetSimulation();
            }
            catch (Exception error)
            {
                _running = false;
                Debug.LogException(error, this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            _running = false;
            _simulation = null;
            _playback = null;
            // Own all generated objects. Re-entering Play with domain reload disabled is safe.
            if (_ui != null) { _ui.SetActive(false); Destroy(_ui); }
            if (_world != null) { _world.SetActive(false); Destroy(_world); }
            if (_panel != null) Destroy(_panel);
            foreach (var material in _materials) if (material != null) Destroy(material);
            _materials.Clear();
            _rotors.Clear(); _thermalMaterials.Clear(); _inputFields.Clear(); _inputValues.Clear();
            _valueIndices.Clear(); _outputLabels.Clear();
            _plot = null;
        }

        private ulong FirstOutput(string quantity)
        {
            foreach (var channel in _asset.Model.Channels)
                if (!channel.IsInput && channel.Quantity == quantity) return channel.Id;
            return 0;
        }

        public void ResetSimulation()
        {
            _playback = null;
            _simulation = _asset.Model.CreateSimulation();
            _snapshot = _simulation.ReadSnapshot(_values);
            _inputValues.Clear();
            foreach (var component in _asset.Components)
                if (component.InputChannel != 0) _inputValues.Add(component.InputChannel, component.InitialInput.Value);
            _running = true;
            _displayEvent = 0;
            _outcome = "";
            _nextTelemetryTime = 0;
            _plot.ClearHistory();
            PlotSnapshot();
            RefreshInterface();
            RefreshWorld();
        }

        public void RunReferenceExperiment()
        {
            ResetSimulation();
            _playback = _asset.CreatePlayback();
            _simulation = null;
            ReadCurrentSnapshot();
            RefreshInterface();
        }

        public void SetRunning(bool running)
        {
            _running = running;
            if (_pauseButton != null) _pauseButton.text = running ? "Pause" : "Resume";
        }

        public void AdvanceOnePresentationStep()
        {
            if (_simulation == null && _playback == null) return;
            if (_playback != null && _playback.Completed)
            {
                SetRunning(false);
                return;
            }
            ulong step = _asset.Model.StepNanoseconds;
            ulong delta = Math.Min(MaxPresentationTicks, Math.Max(1UL, PresentationStepNanoseconds / step)) * step;
            if (_playback != null) delta = Math.Min(delta, _asset.DurationNanoseconds - _snapshot.TimeNanoseconds);
            var status = _playback != null ? _playback.Advance(delta) : _simulation.Step(delta);
            if (status != SimulationStatus.Ok)
            {
                SetRunning(false);
                _outcome = "STOPPED / " + status;
                RefreshInterface();
                Debug.LogError("Power simulation stopped: " + status, this);
                return;
            }
            ReadCurrentSnapshot();
            PlotSnapshot();
            if (_playback != null && _playback.Completed)
            {
                SetRunning(false);
                int passed = 0;
                foreach (var check in _asset.Checks)
                {
                    double value = Value(check.ObjectId, check.Field);
                    if (value >= (check.Min ?? double.NegativeInfinity) && value <= (check.Max ?? double.PositiveInfinity) &&
                        Math.Abs(value) <= (check.AbsMax ?? double.PositiveInfinity)) ++passed;
                }
                _outcome = _asset.Checks.Count == 0 ? "COMPLETED / NO KPI CHECKS" :
                    (passed == _asset.Checks.Count ? "PASS / " : "FAIL / ") + passed + " OF " + _asset.Checks.Count + " CHECKS";
                RefreshInterface();
            }
        }

        private void ReadCurrentSnapshot()
        {
            _snapshot = _playback != null ? _playback.ReadSnapshot(_values) : _simulation.ReadSnapshot(_values);
            if (_playback == null) return;
            while (_displayEvent < _asset.Inputs.Count && _asset.Inputs[_displayEvent].TimeNanoseconds <= _snapshot.TimeNanoseconds)
            {
                var input = _asset.Inputs[_displayEvent++];
                _inputValues[input.Channel] = input.Value;
            }
        }

        private void PlotSnapshot() => _plot.Push((float)(Output(_firstSpeed) * 30 / Math.PI), (float)(Output(_secondSpeed) * 30 / Math.PI));

        private void ChangeInput(ulong channel, string text)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                _outcome = "INVALID INPUT / EXPECTED A NUMBER";
                RefreshInterface();
                return;
            }
            var simulation = _playback != null ? _playback.ForkSimulation() : _simulation;
            var status = simulation.SubmitInputs(new[] { new Scalar(channel, value) });
            if (status == SimulationStatus.Ok)
            {
                _simulation = simulation; _playback = null;
                _inputValues[channel] = value; _outcome = "";
                ReadCurrentSnapshot();
            }
            else _outcome = "INPUT REJECTED / " + status;
            RefreshInterface();
        }

        private void FixedUpdate()
        {
            if (_running) AdvanceOnePresentationStep();
        }

        private void Update()
        {
            if (_simulation == null && _playback == null) return;
            RefreshWorld();
            if (Time.unscaledTimeAsDouble >= _nextTelemetryTime)
            {
                RefreshInterface();
                _nextTelemetryTime = Time.unscaledTimeAsDouble + 0.1;
            }
        }

        private double Value(uint id, Field field) => Output(Channels.Output(id, field));
        private double Output(ulong channel) => channel == 0 ? 0 : _values[_valueIndices[channel]].Value;

        private void RefreshInterface()
        {
            _timeLabel.text = (_snapshot.TimeNanoseconds * 1e-9).ToString("000.00", CultureInfo.InvariantCulture) + " s";
            _motorLabel.text = _firstSpeed == 0 ? "—" : (Output(_firstSpeed) * 30 / Math.PI).ToString("F1") + " rpm";
            _loadLabel.text = _secondSpeed == 0 ? "—" : (Output(_secondSpeed) * 30 / Math.PI).ToString("F1") + " rpm";
            _currentLabel.text = _firstCurrent == 0 ? "—" : Output(_firstCurrent).ToString("F2") + " A";
            _temperatureLabel.text = _firstTemperature == 0 ? "—" : (Output(_firstTemperature) - 273.15).ToString("F2") + " °C";
            _balanceLabel.text = Value(0, Field.EnergyResidual).ToString("E2") + " J";
            foreach (var field in _inputFields)
            {
                var focused = field.Value.panel == null ? null : field.Value.panel.focusController.focusedElement as VisualElement;
                if (focused == field.Value || (focused != null && field.Value.Contains(focused))) continue;
                field.Value.SetValueWithoutNotify(_inputValues[field.Key].ToString("G17", CultureInfo.InvariantCulture));
            }
            foreach (var label in _outputLabels) label.Value.text = Output(label.Key).ToString("G6", CultureInfo.InvariantCulture);
            _pauseButton.text = _running ? "Pause" : "Resume";
            _stateLabel.text = _outcome.Length > 0 ? _outcome : (_playback != null ? "SAVED EXPERIMENT" : "INTERACTIVE LAB") + "  /  " + (_running ? "RUNNING" : "PAUSED");
        }

        private void RefreshWorld()
        {
            // Reduce in double before converting to Unity's float transforms.
            foreach (var rotor in _rotors)
                rotor.Value.localRotation = Quaternion.Euler((float)(Value(rotor.Key, Field.Angle) % (2 * Math.PI) * Mathf.Rad2Deg), 0, 0);
            foreach (var heat in _thermalMaterials)
                heat.Value.SetColor("_BaseColor", Color.Lerp(new Color(0.12f, 0.40f, 0.46f), new Color(1, 0.35f, 0.12f),
                    Mathf.Clamp01((float)(Value(heat.Key, Field.Temperature) - 290) / 80)));
        }

        private Material Surface(Color color, float metallic = 0.35f)
        {
            var template = Resources.Load<Material>("PowerLit");
            if (template == null) throw new InvalidOperationException("Missing PowerLit material. Run Power > Prepare project in the Unity Editor.");
            var material = new Material(template);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", 0.6f);
            _materials.Add(material);
            return material;
        }

        private GameObject Shape(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, Material surface, Transform parent = null)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = objectName;
            item.transform.SetParent(parent == null ? _world.transform : parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = surface;
            var collider = item.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return item;
        }

        private Transform Rotor(string rotorName, Vector3 position, float radius, Material surface, Material bright)
        {
            var pivot = new GameObject(rotorName).transform;
            pivot.SetParent(_world.transform, false);
            pivot.localPosition = position;
            var disc = Shape("Flywheel", PrimitiveType.Cylinder, Vector3.zero, new Vector3(radius * 2, 0.20f, radius * 2), surface, pivot);
            disc.transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < 8; ++i)
            {
                float angle = i * Mathf.PI / 4;
                var spoke = Shape("Rotor marker", PrimitiveType.Cube,
                    new Vector3(0.23f, radius * 0.64f * Mathf.Cos(angle), radius * 0.64f * Mathf.Sin(angle)),
                    new Vector3(0.06f, radius * 0.48f, 0.06f), bright, pivot);
                spoke.transform.localRotation = Quaternion.Euler(i * 45, 0, 0);
            }
            return pivot;
        }

        private void BuildWorld()
        {
            _world = new GameObject("Power generated lab");
            _world.transform.SetParent(transform, false);
            var floor = Surface(new Color(0.035f, 0.055f, 0.07f), 0.15f);
            var steel = Surface(new Color(0.26f, 0.33f, 0.37f), 0.8f);
            var mint = Surface(new Color(0.37f, 0.89f, 0.69f));
            var blue = Surface(new Color(0.35f, 0.63f, 0.91f));
            var grid = Surface(new Color(0.065f, 0.10f, 0.12f), 0);
            var copper = Surface(new Color(0.65f, 0.36f, 0.18f));
            Shape("Lab deck", PrimitiveType.Cube, new Vector3(0, -0.2f, 0), new Vector3(40, 0.3f, 40), floor);
            for (int i = -10; i <= 10; ++i)
            {
                Shape("Grid X", PrimitiveType.Cube, new Vector3(i, -0.04f, 0), new Vector3(0.012f, 0.006f, 20), grid);
                Shape("Grid Z", PrimitiveType.Cube, new Vector3(0, -0.04f, i), new Vector3(20, 0.006f, 0.012f), grid);
            }
            var positions = new Dictionary<uint, Vector3>();
            int columns = Math.Min(4, Math.Max(1, (int)Math.Ceiling(Math.Sqrt(_asset.Nodes.Count))));
            int rows = (_asset.Nodes.Count + columns - 1) / columns;
            for (int i = 0; i < _asset.Nodes.Count; ++i)
            {
                var node = _asset.Nodes[i];
                var position = new Vector3((i % columns - (columns - 1) * 0.5f) * 3.2f, 1.6f, (i / columns - (rows - 1) * 0.5f) * 2.8f);
                if (node.Domain == Domain.Rotational)
                {
                    var surface = Surface(i % 2 == 0 ? new Color(0.12f, 0.40f, 0.36f) : new Color(0.16f, 0.30f, 0.46f));
                    _rotors.Add(node.Id, Rotor("Rotor " + node.Id, position, 0.75f, surface, i % 2 == 0 ? mint : blue));
                    Shape("Mount " + node.Id, PrimitiveType.Cube, new Vector3(position.x, 0.3f, position.z), new Vector3(1.6f, 0.6f, 1.5f), surface);
                    Shape("Bearing " + node.Id, PrimitiveType.Cube, new Vector3(position.x - 0.35f, 1, position.z), new Vector3(0.2f, 1.1f, 0.3f), steel);
                }
                else
                {
                    position.y = 0.7f;
                    var surface = Surface(new Color(0.12f, 0.40f, 0.46f));
                    _thermalMaterials.Add(node.Id, surface);
                    Shape("Thermal node " + node.Id, PrimitiveType.Cube, position, new Vector3(1.4f, 1.2f, 1.2f), surface);
                    for (int fin = 0; fin < 5; ++fin)
                        Shape("Heat fin", PrimitiveType.Cube, position + new Vector3(-0.56f + fin * 0.28f, 0.72f, 0), new Vector3(0.06f, 0.4f, 1.2f), steel);
                }
                positions.Add(node.Id, position);
            }
            foreach (var component in _asset.Components)
            {
                Vector3 a = positions[component.NodeA];
                if (component.Kind == ComponentKind.Shaft || component.Kind == ComponentKind.ThermalLink)
                    Connection("Component " + component.Id, a, component.NodeB == 0 ? new Vector3(a.x, 0.05f, a.z + 1) : positions[component.NodeB],
                        component.Kind == ComponentKind.Shaft ? steel : copper, component.Kind == ComponentKind.Shaft ? 0.15f : 0.07f);
                if (component.HeatNode != 0) Connection("Loss path " + component.Id, a, positions[component.HeatNode], copper, 0.05f);
            }
            _distance = Math.Max(12, Math.Max(columns * 3.2f, rows * 2.8f) * 1.6f);
            var cameraObject = new GameObject("Lab camera");
            cameraObject.transform.SetParent(_world.transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.025f, 0.04f, 0.055f);
            _camera.fieldOfView = 38;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 100;
            PositionCamera();
            var lightObject = new GameObject("Lab key light");
            lightObject.transform.SetParent(_world.transform, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.3f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(45, -35, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.42f, 0.49f);
        }

        private void Connection(string connectionName, Vector3 a, Vector3 b, Material surface, float width)
        {
            Vector3 direction = b - a;
            var item = Shape(connectionName, PrimitiveType.Cylinder, (a + b) * 0.5f,
                new Vector3(width, direction.magnitude * 0.5f, width), surface);
            item.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
        }

        private void PositionCamera()
        {
            var target = transform.TransformPoint(new Vector3(0, 0.8f, 0));
            _camera.transform.position = target + Quaternion.Euler(_pitch, _yaw, 0) * new Vector3(0, 0, -_distance);
            _camera.transform.LookAt(target);
        }

        private void BuildInterface()
        {
            _panel = ScriptableObject.CreateInstance<PanelSettings>();
            _panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _panel.referenceResolution = new Vector2Int(1600, 1000);
            _panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _panel.match = 0.5f;
            _panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("PowerTheme");
            _ui = new GameObject("Power interface");
            _ui.transform.SetParent(transform, false);
            var document = _ui.AddComponent<UIDocument>();
            document.panelSettings = _panel;
            var root = document.rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("PowerStudio"));
            root.AddToClassList("studio");

            var viewport = new VisualElement { name = "viewport" };
            viewport.AddToClassList("viewport");
            root.Add(viewport);
            viewport.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                _dragPointer = e.pointerId;
                _lastPointer = new Vector2(e.position.x, e.position.y);
                viewport.CapturePointer(e.pointerId);
            });
            viewport.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (_dragPointer != e.pointerId) return;
                var point = new Vector2(e.position.x, e.position.y);
                var delta = point - _lastPointer;
                _lastPointer = point;
                _yaw += delta.x * 0.25f;
                _pitch = Mathf.Clamp(_pitch + delta.y * 0.2f, 8, 75);
                PositionCamera();
            });
            viewport.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_dragPointer != e.pointerId) return;
                _dragPointer = -1;
                viewport.ReleasePointer(e.pointerId);
            });
            viewport.RegisterCallback<PointerCaptureOutEvent>(_ => _dragPointer = -1);
            viewport.RegisterCallback<WheelEvent>(e => { _distance = Mathf.Clamp(_distance + e.delta.y * 0.3f, 7, 60); PositionCamera(); });

            var header = Box(root, "header");
            var brand = Box(header, "brand");
            Text(brand, "POWER!", "wordmark");
            Text(brand, "ENGINEERING STUDIO", "eyebrow");
            _stateLabel = Text(header, "INTERACTIVE LAB", "status");
            _timeLabel = Text(header, "000.00 s", "clock");

            var title = Box(root, "scene-title");
            Text(title, "MODEL LABORATORY", "eyebrow");
            Text(title, _asset.Name, "hero-title");
            Text(title, _asset.Nodes.Count + " nodes · " + _asset.Components.Count + " components · " + _asset.Model.Fidelity, "subtitle");
            Text(title, "Drag to orbit  /  Scroll to zoom", "hint");

            var controls = Box(root, "controls");
            Text(controls, "EXPERIMENT CONTROL", "eyebrow");
            var inputList = new ScrollView(ScrollViewMode.Vertical);
            inputList.AddToClassList("input-list");
            controls.Add(inputList);
            foreach (var channel in _asset.Model.Channels)
            {
                if (!channel.IsInput) continue;
                ulong id = channel.Id;
                var field = new TextField(channel.Quantity + " / " + channel.Id) { isDelayed = true };
                field.tooltip = "Unit: " + channel.Unit + ". Decimal point: '.'; press Enter to apply.";
                field.RegisterValueChangedCallback(e => ChangeInput(id, e.newValue));
                _inputFields.Add(id, field);
                inputList.Add(field);
            }
            if (_inputFields.Count == 0) Text(inputList, "This model has no external inputs.", "hint");
            var actions = Box(controls, "row");
            _pauseButton = AddButton(actions, "Pause", () => SetRunning(!_running));
            AddButton(actions, "Reset", ResetSimulation);
            AddButton(controls, "Run saved experiment", RunReferenceExperiment).AddToClassList("primary-button");
            Text(controls, (_asset.DurationNanoseconds * 1e-9).ToString("G6") + " s · " + _asset.Inputs.Count + " input changes", "hint");
            var outputList = new ScrollView(ScrollViewMode.Vertical);
            outputList.AddToClassList("output-list");
            controls.Add(outputList);
            foreach (var channel in _asset.Model.Channels)
            {
                if (channel.IsInput) continue;
                var row = Box(outputList, "output-row");
                var label = Text(row, channel.ObjectId + " / " + channel.Quantity, "hint");
                label.tooltip = "Channel " + channel.Id + " / " + channel.Unit;
                _outputLabels.Add(channel.Id, Text(row, "—", "output-value"));
            }

            var telemetry = Box(root, "telemetry");
            var metrics = Box(telemetry, "metrics");
            _motorLabel = Metric(metrics, "FIRST ROTOR", "—", "mint");
            _loadLabel = Metric(metrics, "SECOND ROTOR", "—", "blue");
            _currentLabel = Metric(metrics, "CURRENT", "0.00 A", "");
            _temperatureLabel = Metric(metrics, "FIRST THERMAL NODE", "—", "");
            _balanceLabel = Metric(metrics, "ENERGY RESIDUAL", "0 J", "");
            var plotRow = Box(telemetry, "row");
            Text(plotRow, "RECENT SPEED / 500 SAMPLES", "eyebrow");
            Text(plotRow, "FIRST ROTOR  /  SECOND ROTOR   ·   rpm", "hint");
            _plot = new TelemetryPlot();
            telemetry.Add(_plot);
            var footer = Box(root, "footer");
            Text(footer, "Calibration: " + _asset.Model.Calibration + " · model " + _asset.Model.Fingerprint.ToString("x16"), "hint");
            Text(footer, "POWER STUDIO / 0.3", "eyebrow");
        }

        private static VisualElement Box(VisualElement parent, string className)
        {
            var box = new VisualElement(); box.AddToClassList(className); parent.Add(box); return box;
        }
        private static Label Text(VisualElement parent, string text, string className)
        {
            var label = new Label(text); label.AddToClassList(className); parent.Add(label); return label;
        }
        private static Button AddButton(VisualElement parent, string text, Action action)
        {
            var button = new Button(action) { text = text }; parent.Add(button); return button;
        }
        private static Label Metric(VisualElement parent, string title, string value, string color)
        {
            var card = Box(parent, "metric");
            Text(card, title, "eyebrow");
            var label = Text(card, value, "metric-value");
            if (color.Length > 0) label.AddToClassList(color);
            return label;
        }
    }
}
