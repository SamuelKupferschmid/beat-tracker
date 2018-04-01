using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeatTracker.Tracking;
using BeatTracker.Utils;

namespace BeatTracker.UI
{
    public partial class SpectrumVisualization : Form
    {
        public SpectrumVisualization()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            this.canvas.Paint += UpdateDrawing;
            Application.Idle += ApplicationOnIdle;
            base.OnLoad(e);
        }

        private void ApplicationOnIdle(object sender, EventArgs eventArgs)
        {
            this.canvas.Invalidate();
        }

        private readonly Queue<float[]> _frames = new Queue<float[]>();
        private const int SkipSize = 5;
        private int _offset = 0;
        private Bitmap _bitmap;

        public void AddFrame(object sender, float[] frame)
        {
            var min = float.MaxValue;
            var max = float.MinValue;

            foreach (var v in frame)
            {
                if (v < min)
                {
                    min = v;
                }

                if (v > max)
                {
                    max = v;
                }
            }

            if (min < MinValue)
            {
                MinValue = min;
            }

            if (max > MaxValue)
            {
                MaxValue = max;
            }

            lock (this)
            {
                _frames.Enqueue(frame);
            }
        }

        private void UpdateDrawing(object sender, PaintEventArgs e)
        {
            lock (this)
            {
                while (_frames.Any())
                {
                    var next = _frames.Dequeue();

                    if (_bitmap == null)
                    {
                        _bitmap = new Bitmap(canvas.Width, next.Length);
                    }

                    if (_offset == canvas.Width)
                    {
                        using (var g = Graphics.FromImage(_bitmap))
                        {
                            g.DrawImage(_bitmap, -SkipSize, 0);
                            g.FillRectangle(Brushes.White, _offset - SkipSize, 0, SkipSize, canvas.Height);
                            g.Save();
                        }

                        _offset -= SkipSize;
                    }

                    for (int i = 0; i < next.Length; i++)
                    {
                        _bitmap.SetPixel(_offset, i, GetColor(next[i]));
                    }

                    _offset++;
                }
            }

            if (_bitmap != null) e.Graphics.DrawImage(_bitmap, 0, 0, canvas.Width, canvas.Height);
        }

        private Color GetColor(float value)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;

            // 240° - 0° 

            value = value.Clamp(MinValue, MaxValue);

            var hue = (1 - ((MaxValue - MinValue) / (value - MinValue))) * 240 / 360;

            float v2 = ((0.5f + 1) - (0.5f * 1));
            float v1 = 2 * 0.5f - v2;

            r = (byte) (255 * HueToRGB(v1, v2, hue + (1.0f / 3)));
            g = (byte) (255 * HueToRGB(v1, v2, hue));
            b = (byte) (255 * HueToRGB(v1, v2, hue - (1.0f / 3)));

            return Color.FromArgb(255, r, g, b);
        }

        public float MaxValue { get; private set; }

        public float MinValue { get; private set; }

        private static float HueToRGB(float v1, float v2, float vH)
        {
            if (vH < 0)
                vH += 1;

            if (vH > 1)
                vH -= 1;

            if ((6 * vH) < 1)
                return (v1 + (v2 - v1) * 6 * vH);

            if ((2 * vH) < 1)
                return v2;

            if ((3 * vH) < 2)
                return (v1 + (v2 - v1) * ((2.0f / 3) - vH) * 6);

            return v1;
        }
    }
}