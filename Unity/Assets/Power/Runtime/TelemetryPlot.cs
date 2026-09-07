using UnityEngine;
using UnityEngine.UIElements;

namespace Power.Studio
{
    // Bounded view history. Physics and replay never depend on this display buffer.
    public sealed class TelemetryPlot : VisualElement
    {
        private readonly float[] _motor = new float[500];
        private readonly float[] _load = new float[500];
        private int _head;
        private int _count;

        public TelemetryPlot()
        {
            AddToClassList("telemetry-plot");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void Push(float motorRpm, float loadRpm)
        {
            _motor[_head] = motorRpm;
            _load[_head] = loadRpm;
            _head = (_head + 1) % _motor.Length;
            _count = Mathf.Min(_count + 1, _motor.Length);
            MarkDirtyRepaint();
        }

        public void ClearHistory()
        {
            _head = 0;
            _count = 0;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (_count < 2 || contentRect.width < 1 || contentRect.height < 1) return;
            var painter = context.painter2D;
            float width = contentRect.width;
            float height = contentRect.height;
            float maximum = 350;
            float minimum = 0;
            for (int i = 0; i < _count; ++i)
            {
                int at = (_head - _count + i + _motor.Length) % _motor.Length;
                maximum = Mathf.Max(maximum, Mathf.Max(_motor[at], _load[at]) * 1.1f);
                minimum = Mathf.Min(minimum, Mathf.Min(_motor[at], _load[at]) * 1.1f);
            }
            painter.lineWidth = 1;
            painter.strokeColor = new Color(0.19f, 0.26f, 0.30f);
            for (int i = 1; i < 4; ++i)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, height * i / 4));
                painter.LineTo(new Vector2(width, height * i / 4));
                painter.Stroke();
            }
            DrawLine(_motor, new Color(0.45f, 0.94f, 0.75f));
            DrawLine(_load, new Color(0.46f, 0.71f, 1));

            void DrawLine(float[] series, Color color)
            {
                painter.lineWidth = 2;
                painter.strokeColor = color;
                painter.BeginPath();
                for (int i = 0; i < _count; ++i)
                {
                    int at = (_head - _count + i + series.Length) % series.Length;
                    var point = new Vector2(width * i / (_motor.Length - 1),
                        height - 5 - (height - 10) * (series[at] - minimum) / (maximum - minimum));
                    if (i == 0) painter.MoveTo(point); else painter.LineTo(point);
                }
                painter.Stroke();
            }
        }
    }
}
