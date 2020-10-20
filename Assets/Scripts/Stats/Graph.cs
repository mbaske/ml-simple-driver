using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MBaske
{
    public class Graph : MonoBehaviour
    {
        public class Line
        {
            public bool ShowValue { get; private set; }
            public bool IsDrawable => Buffer.IsEnabled && Buffer.Length > 1;
            public bool IsFlat { get; set; }

            public Color Color { get; private set; }
            public string RGB { get; private set; }
            public string RGB_Avg { get; private set; }

            public StatsBuffer Buffer { get; private set; }

            public Line(StatsBuffer data, Color color, bool showValue)
            {
                Buffer = data;
                Color = color;
                RGB = "#" + ColorUtility.ToHtmlStringRGB(color);
                // Dimmed for average value.
                RGB_Avg = "#" + ColorUtility.ToHtmlStringRGB(color * 0.5f);
                ShowValue = showValue;
            }
        }

        public string Name = "Graph";
        public Vector2 Size = new Vector2(636, 60);
        public bool FitBounds { get; set; }

        private readonly List<Line> lines = new List<Line>();
        private readonly StringBuilder sb = new StringBuilder();
        private const string format = "0.###";

        private float minVal = Mathf.Infinity;
        private float maxVal = Mathf.NegativeInfinity;

        private Rect drawArea;
        private Text textDescr;
        private Text textMin;
        private Text textMax;
        private Text textVal;

        private const int descrWidth = 200;
        private const int minMaxWidth = 42;
        private const int lineHeight = 14;
        private float yCenter;
        private float yScale;

        public Graph Add(StatsBuffer data, Color color, bool showValue = false)
        {
            lines.Add(new Line(data, color, showValue));
            UpdateLayout();
            return this;
        }

        public void UpdateLayout()
        {
            Size.y = Mathf.Max(Size.y, (lines.Count + 1) * lineHeight + 1);
            // 5px margin between values and lines.
            drawArea = new Rect(descrWidth, 0, Size.x - minMaxWidth - descrWidth - 5, Size.y);
            yCenter = drawArea.height * -0.5f;

            RectTransform rect = this.GetComponent<RectTransform>();
            rect.sizeDelta = Size;

            // Background padding 2px above and below.
            rect = transform.GetChild(0).GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Size.x, Size.y + 4);

            textDescr = transform.GetChild(1).GetComponent<Text>();
            rect = textDescr.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(descrWidth, Size.y);

            textMin = transform.GetChild(2).GetComponent<Text>();
            rect = textMin.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(minMaxWidth, Size.y);

            textMax = transform.GetChild(3).GetComponent<Text>();
            rect = textMax.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(minMaxWidth, Size.y);

            textVal = transform.GetChild(4).GetComponent<Text>();
            rect = textVal.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(minMaxWidth, Size.y);

            UpdateText();
        }

        public void ResetBounds()
        {
            minVal = Mathf.Infinity;
            maxVal = Mathf.NegativeInfinity;
        }

        private void UpdateText()
        {
            sb.Clear();
            sb.AppendLine($"<color=#ffffff>{Name.ToUpper()}</color>");
            foreach (Line line in lines)
            {
                if (line.IsDrawable)
                {
                    sb.AppendLine($"<color={line.RGB}>{line.Buffer.Name}</color>");
                }
            }
            textDescr.text = sb.ToString();
            textMin.text = "";
            textMax.text = "";
            textVal.text = "";
        }

        private void OnRenderObject()
        {
            UpdateText();

            if (FitBounds)
            {
                ResetBounds();
            }

            float minTime = Mathf.Infinity;
            float maxTime = Mathf.NegativeInfinity;

            bool isFlatGraph = true;
            bool drawGraph = false;

            foreach (Line line in lines)
            {
                if (line.IsDrawable)
                {
                    drawGraph = true;

                    float min = line.Buffer.Min;
                    float max = line.Buffer.Max;

                    minVal = Mathf.Min(minVal, min);
                    maxVal = Mathf.Max(maxVal, max);
                    minTime = Mathf.Min(minTime, line.Buffer.Start);
                    maxTime = Mathf.Max(maxTime, line.Buffer.End);

                    line.IsFlat = min == max;
                    isFlatGraph = isFlatGraph && line.IsFlat;
                }
            }

            if (drawGraph)
            {
                yScale = 0;
                if (!isFlatGraph)
                {
                    textMin.text = minVal.ToString(format);
                    textMax.text = maxVal.ToString(format);
                    yScale = drawArea.height / (maxVal - minVal);
                }

                sb.Clear();
                CreateLineMaterial();
                lineMaterial.SetPass(0);
                GL.PushMatrix();
                GL.MultMatrix(transform.localToWorldMatrix);
                GL.Begin(GL.LINES);

                foreach (Line line in lines)
                {
                    if (line.IsDrawable)
                    {
                        GL.Color(line.Color);
                        string sVal = line.Buffer.Current.ToString(format);

                        if (line.IsFlat)
                        {
                            float y = isFlatGraph ? yCenter
                                : (line.Buffer.Current - minVal) * yScale - drawArea.height;
                            GL.Vertex3(drawArea.xMin, y, 0);
                            GL.Vertex3(drawArea.xMax, y, 0);

                            if (line.ShowValue)
                            {
                                sb.AppendLine($"<color={line.RGB}>{sVal}</color>");
                            }
                        }
                        else
                        {
                            TimedQueueItem<float> previous = default;
                            foreach (var current in line.Buffer.Items())
                            {
                                if (previous.time > 0)
                                {
                                    GL.Vertex3(
                                        MapTime(minTime, maxTime, previous.time),
                                        (previous.value - minVal) * yScale - drawArea.height,
                                        0);
                                    GL.Vertex3(
                                        MapTime(minTime, maxTime, current.time),
                                        (current.value - minVal) * yScale - drawArea.height,
                                        0);
                                }
                                previous = current;
                            }

                            if (line.ShowValue)
                            {
                                string sAvg = line.Buffer.Avg.ToString(format);
                                sb.AppendLine($"<color={line.RGB}>{sVal}</color>");
                                sb.AppendLine($"<color={line.RGB_Avg}>{sAvg}</color>");
                            }
                        }
                    }
                }
                GL.End();
                GL.PopMatrix();
                textVal.text = sb.ToString();
            }
        }

        private float MapTime(float min, float max, float t)
        {
            return Mathf.Lerp(drawArea.xMin, drawArea.xMax, Mathf.InverseLerp(min, max, t));
        }

        private static Material lineMaterial;
        private static void CreateLineMaterial()
        {
            if (!lineMaterial)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                lineMaterial = new Material(shader);
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending.
                lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off.
                lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes.
                lineMaterial.SetInt("_ZWrite", 0);
            }
        }
    }
}