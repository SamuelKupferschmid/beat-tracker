using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeatTracker.Tracking;

namespace BeatTracker.UI
{
    public partial class SpectrumVisualization : Form
    {
        private readonly ISpectrumProvider _provider;

        public SpectrumVisualization(ISpectrumProvider provider)
        {
            _provider = provider;
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            this.Text = _provider.Name;

            this._provider.DataChanged += UpdateData;
            this.canvas.Paint += UpdateDrawing;

            base.OnLoad(e);
        }

        private void UpdateData(object sender, EventArgs e)
        {
            this.canvas.Invalidate();
        }

        private void UpdateDrawing(object sender, PaintEventArgs e)
        {
            if (_provider.Samples.Count > 0)
            {
                var width = _provider.Samples.Count;
                var height = _provider.Samples[0].Length;
                using (var img = new Bitmap(width, height))
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            // TODO improve value -> color mapping
                            img.SetPixel(x, y, Color.FromArgb((int) (_provider.Samples[x][y] * 255), Color.Blue));
                        }
                    }

                    e.Graphics.DrawImage(img, e.ClipRectangle);
                }
            }
        }

        private void UpdateDrawing(object sender, EventArgs e)
        {
            this.canvas.Invalidate();
        }
    }
}
