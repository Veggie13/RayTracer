using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RayTracer
{
    public struct XColor
    {
        public XColor(Color c, float scaleBrightness)
        {
            float nonlinBrightness = (float)Math.Pow(scaleBrightness, 0.45);
            R = nonlinBrightness * c.R / 255.0f;
            G = nonlinBrightness * c.G / 255.0f;
            B = nonlinBrightness * c.B / 255.0f;
        }

        public XColor(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public float R;
        public float G;
        public float B;

        public float Brightness
        {
            get
            {
                return (float)Math.Pow(Math.Max(R, Math.Max(G, B)), 20 / 9.0);
            }
        }

        public XColor Clone()
        {
            return new XColor(R, G, B);
        }

        public XColor ScaleBrightness(float linScale)
        {
            float scale = (float)Math.Pow(linScale, 0.45);
            XColor result = this.Clone();
            result.R *= scale;
            result.G *= scale;
            result.B *= scale;
            /*if (result.R > 1) result.R = 1;
            if (result.G > 1) result.G = 1;
            if (result.B > 1) result.B = 1;*/
            if (result.R < 0) result.R = 0;
            if (result.G < 0) result.G = 0;
            if (result.B < 0) result.B = 0;

            return result;
        }

        public XColor ChangeBrightness(float linBright)
        {
            if (linBright < 0) linBright = 0;
            float bright = (float)Math.Pow(linBright, 0.45);
            //if (bright > 1) bright = 1;
            float scale = 0;
            if (R >= G && R >= B)
            {
                if (R == 0)
                    scale = 0;
                else
                    scale = bright / R;
            }
            else if (G >= B)
            {
                scale = bright / G;
            }
            else
            {
                scale = bright / B;
            }
            return ScaleBrightness((float)Math.Pow(scale, 20 / 9.0));
        }

        public XColor Unit()
        {
            return this.ChangeBrightness(1);
        }

        public XColor Tint(XColor rhs)
        {
            XColor newColor = new XColor(R, G, B);
            XColor tint = rhs.Unit();
            newColor.R *= tint.R;
            newColor.G *= tint.G;
            newColor.B *= tint.B;
            return newColor;
        }

        public XColor Mix(XColor rhs)
        {
            XColor newColor = new XColor();
            float linTotalBrightness = Brightness + rhs.Brightness;
            float portion1 = Brightness / linTotalBrightness;
            float portion2 = rhs.Brightness / linTotalBrightness;
            newColor.R = portion1 * R + portion2 * rhs.R;
            newColor.G = portion1 * G + portion2 * rhs.G;
            newColor.B = portion1 * B + portion2 * rhs.B;
            return newColor.ChangeBrightness(linTotalBrightness);
        }

        public Color AsColor()
        {
            int nR = (int)(255 * R);
            int nG = (int)(255 * G);
            int nB = (int)(255 * B);
            if (nR > 255) nR = 255;
            if (nG > 255) nG = 255;
            if (nB > 255) nB = 255;
            return Color.FromArgb(nR, nG, nB);
        }
    }
}
