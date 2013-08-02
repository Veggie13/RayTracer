using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RayTracer;

namespace RayTracerTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            panel1.Paint += new PaintEventHandler(panel1_Paint);
        }

        void panel1_Paint(object sender, PaintEventArgs e)
        {
            var scene = new Scene();
            var surface = new RenderSurface();
            Bitmap bm = new Bitmap(512, 512);
            //using (Graphics g = Graphics.FromImage(bm))
            {
                surface.gc = e.Graphics;
                scene.Render(surface);
            }

            /*Bitmap bm2 = new Bitmap(512, 512);
            using (Graphics g2 = Graphics.FromImage(bm2))
            {
                Color p2 = bm.GetPixel(0, 0),
                    p3 = bm.GetPixel(1, 0);
                for (int x = 0; x < 511; x++)
                    for (int y = 0; y < 511; y++)
                    {
                        Color p0 = p2, p1 = p3;
                        p2 = bm.GetPixel(x, y + 1);
                        p3 = bm.GetPixel(x + 1, y + 1);
                        int r = p0.R + p1.R + p2.R + p3.R;
                        int g = p0.G + p1.G + p2.G + p3.G;
                        int b = p0.B + p1.B + p2.B + p3.B;
                        g2.DrawRectangle(
                            new Pen(Color.FromArgb(r / 4, g / 4, b / 4)),
                            new Rectangle(x, y, 1, 1));
                    }
            }/**/
            //bm.Save("E:\\render.png");
            //Bitmap bm2 = new Bitmap(bm, 512, 512);
            //e.Graphics.DrawImage(bm, 0, 0);
        }
    }

    public class RenderSurface : IRenderSurface
    {
        public Graphics gc;
        public int scale = 2;
        public void SetPixel(int x, int y, XColor c)
        {
            Color cc = c.AsColor();
            gc.FillRectangle(new SolidBrush(cc), new Rectangle(scale * x, scale * y, scale, scale));
        }
    }
}
