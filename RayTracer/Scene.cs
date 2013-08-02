using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using RayTracer.Geometry;

namespace RayTracer
{
    public interface Element
    {
        bool GetPixel(Ray3f pointer, IEnumerable<Element> lights, int level, ref Point3f pt, ref XColor result, ref float distFromSrc);
    }

    public interface LitElement
    {
    }

    public class LightSphere : Element
    {
        public XColor color;
        public Sphere3f geom;
        public bool visible;

        public bool GetPixel(Ray3f pointer, IEnumerable<Element> lights, int level, ref Point3f pt, ref XColor result, ref float distFromSrc)
        {
            if (!visible || !geom.FirstIntersect(pointer, ref pt))
                return false;

            distFromSrc = pointer.Src.Subtract(pt).Magnitude();
            result = color.ScaleBrightness(geom.R * geom.R / (distFromSrc * distFromSrc));
            return true;
        }
    }

    public class SolidSphere : Element
    {
        public XColor color;
        public float reflectivity;
        public float opacity;
        public float refractiveIndex;
        public Sphere3f geom;

        public bool GetPixel(Ray3f pointer, IEnumerable<Element> lights, int level, ref Point3f pt, ref XColor result, ref float distFromSrc)
        {
            if (!geom.FirstIntersect(pointer, ref pt))
                return false;
            if (level <= 0)
            {
                result = new XColor(Color.Black, 0);
                return true;
            }

            XColor reflResult = new XColor(Color.Black, 0);
            XColor refrResult = new XColor(Color.Black, 0);

            //Direction3f touchPt = new Direction3f(touch.Subtract(geom.Pos));
            Plane3f tangent = new Plane3f(pt, pt.Subtract(geom.Pos));
            if (reflectivity > 0)
            {
                Ray3f income = new Ray3f(pt, tangent.Reflect(pointer.Dir));
                Element myLight = null;
                XColor myColor = new XColor();
                XColor tempColor = new XColor();
                Point3f dummy = new Point3f();
                float curMag = float.MaxValue;
                float curSrcDist = float.MaxValue, srcDist = float.MaxValue;
                foreach (var light in lights)
                {
                    if (this == light)
                        continue;
                    if (light.GetPixel(income, lights, level - 1, ref dummy, ref tempColor, ref srcDist))
                    {
                        float thisMag = dummy.Subtract(pt).Magnitude();
                        if (thisMag < curMag)
                        {
                            myLight = light;
                            curMag = thisMag;
                            myColor = tempColor;
                            curSrcDist = srcDist;
                        }
                    }
                }
                if (myLight != null)
                {
                    float seg2Dist = pointer.Src.Subtract(pt).Magnitude();
                    float totDist = seg2Dist + curSrcDist;
                    float seg2Decr = (curSrcDist * curSrcDist) / (totDist * totDist);
                    reflResult = myColor.Tint(color).ScaleBrightness(seg2Decr * reflectivity);
                    distFromSrc = totDist;
                }
            }
            else
            {
                Direction3f internDir = tangent.Refract(pointer.Dir, refractiveIndex);
                float cos = (float)Math.Abs(internDir.UnitVector().Dot(tangent.UnitNormal()));
                float offset = 2.1f * geom.R * cos;
                Ray3f intern = new Ray3f(
                    new Ray3f(pt, internDir).PointAt(offset),
                    new Direction3f(internDir.UnitVector().Scale(-1)));
                Point3f nextPt = pt;
                bool test = geom.FirstIntersect(intern, ref nextPt);
                Plane3f exitTangent = new Plane3f(nextPt, geom.Pos.Subtract(nextPt));
                Direction3f exitDir = exitTangent.Refract(internDir, 1 / refractiveIndex);
                Ray3f exitRay = new Ray3f(nextPt, exitDir);
                Element myLight = null;
                XColor myColor = new XColor();
                XColor tempColor = new XColor();
                Point3f dummy = new Point3f();
                float curMag = float.MaxValue;
                float curSrcDist = float.MaxValue;
                float srcDist = float.MaxValue;
                foreach (var light in lights)
                {
                    if (this == light)
                        continue;
                    if (light.GetPixel(exitRay, lights, level - 1, ref dummy, ref tempColor, ref srcDist))
                    {
                        float thisMag = dummy.Subtract(nextPt).Magnitude();
                        if (thisMag < curMag)
                        {
                            myLight = light;
                            curMag = thisMag;
                            myColor = tempColor;
                        }
                    }
                }
                if (myLight != null)
                {
                    float seg2Mag = pointer.Src.Subtract(pt).Magnitude();
                    float seg3Mag = 0;//pt.Subtract(nextPt).Magnitude();
                    float segTot = seg2Mag + seg3Mag;
                    float seg2Decr = 1;// / (segTot * segTot);
                    reflResult = myColor.ChangeBrightness(myColor.Brightness * seg2Decr);
                }
            }

            result = reflResult;
            return true;
        }
    }

    public class SolidPlane : Element
    {
        public XColor color;
        public Plane3f geom;

        public bool GetPixel(Ray3f pointer, IEnumerable<Element> lights, int level, ref Point3f pt, ref XColor result, ref float distFromSrc)
        {
            if (!geom.Intersect(pointer, ref pt))
                return false;
            result = new XColor(Color.Black, 0);
            if (level <= 0)
                return true;

            foreach (Element e in lights)
            {
                if (e is LightSphere)
                {
                    LightSphere light = e as LightSphere;
                    Ray3f litRay = new Ray3f(pt, new Direction3f(light.geom.Pos.Subtract(pt)));
                    Point3f temp = new Point3f();
                    XColor c = new XColor();
                    float srcDist = float.MaxValue;
                    if (light.GetPixel(litRay, lights, level - 1, ref temp, ref c, ref srcDist))
                    {
                        float seg2Mag = pointer.Src.Subtract(pt).Magnitude();
                        float totMag = srcDist + seg2Mag;
                        result = c.Tint(color).ScaleBrightness(srcDist * srcDist / (totMag * totMag));
                        distFromSrc = totMag;
                        break;
                    }
                }
            }
            return true;
        }
    }

    public class Scene
    {
        public List<Element> _objects = new List<Element>();

        public Scene()
        {
            var l = new LightSphere();
            l.color = new XColor(Color.Red, 10000);
            l.geom = new Sphere3f(new Point3f(-0.2f, 2.9f, 2.9f), 0.1f);
            l.visible = true;
            //_objects.Add(l);

            l = new LightSphere();
            l.color = new XColor(Color.White, 10000);
            l.geom = new Sphere3f(new Point3f(-0.2f, -1f, 0f), 0.1f);
            l.visible = true;
            _objects.Add(l);

            var p = new SolidPlane();
            p.color = new XColor(Color.DarkGray, 1);
            p.geom = new Plane3f(new Point3f(3f, 3f, 0), new Point3f(0, -1, 0));
            _objects.Add(p);

            p = new SolidPlane();
            p.color = new XColor(Color.White, 1);
            p.geom = new Plane3f(new Point3f(3f, -3f, 0), new Point3f(0, 1, 0));
            _objects.Add(p);

            p = new SolidPlane();
            p.color = new XColor(Color.Orange, 1);
            p.geom = new Plane3f(new Point3f(5f, 0f, 0), new Point3f(-1, 0, 0));
            _objects.Add(p);

            p = new SolidPlane();
            p.color = new XColor(Color.Purple, 1);
            p.geom = new Plane3f(new Point3f(-5f, 0f, 0), new Point3f(1, 0, 0));
            _objects.Add(p);

            p = new SolidPlane();
            p.color = new XColor(Color.Green, 1);
            p.geom = new Plane3f(new Point3f(3f, 0f, 3f), new Point3f(0, 0, -1));
            _objects.Add(p);

            p = new SolidPlane();
            p.color = new XColor(Color.Blue, 1);
            p.geom = new Plane3f(new Point3f(3f, 0f, -3f), new Point3f(0, 0, 1));
            _objects.Add(p);

            var s = new SolidSphere();
            s.reflectivity = 0;
            s.refractiveIndex = 1.01f;
            s.color = new XColor(Color.FromArgb(200, 180, 255), 1);
            s.geom = new Sphere3f(new Point3f(3f, 1f, 0), 1f);
            _objects.Add(s);

            /*s = new SolidSphere();
            s.reflectivity = 1;
            s.color = new XColor(Color.White, 1);
            s.geom = new Sphere3f(new Point3f(4, 0f, 0f), 0.5f);
            _objects.Add(s);*/
        }

        public void Render(IRenderSurface surface)
        {
            Point3f origin = new Point3f(0, 0, 0);
            Plane3f renderSurface = new Plane3f(1, 0, 0, 2);
            float step = 1f / 257;
            int x = 0;
            for (float h = step; h < 1; h += step, x++)
            {
                int y = 0;
                for (float v = step; v < 1; v += step, y++)
                {
                    Point3f pix = FromCoord(h, v);
                    Direction3f dir = new Direction3f(pix.Subtract(origin));
                    Ray3f ray = new Ray3f(origin, dir);
                    XColor c = new XColor(Color.Black, 0);
                    Point3f dummy = new Point3f();
                    XColor tempColor = new XColor();
                    float curMag = float.MaxValue;
                    float srcDist = float.MaxValue;
                    Element myObj = null;
                    foreach (var obj in _objects)
                    {
                        if (obj.GetPixel(ray, _objects, 3, ref dummy, ref tempColor, ref srcDist))
                        {
                            float thisMag = dummy.Subtract(ray.Src).Magnitude();
                            if (thisMag < curMag)
                            {
                                myObj = obj;
                                curMag = thisMag;
                                c = tempColor;
                            }
                        }
                    }
                    surface.SetPixel(x, y, c);
                }
            }
        }

        private Point3f FromCoord(float hPortion, float vPortion)
        {
            return new Point3f(1, 1 - 2 * hPortion, 1 - 2 * vPortion);
        }
    }

    public class Eye
    {
        public Ray3f Center;
        public float Tilt;
        public float HField;
        public float VField;

        //public Ray3f ViewPosition(float hPortion, float vPortion)
        //{

        //}
    }

    public interface IRenderSurface
    {
        void SetPixel(int x, int y, XColor c);
    }
}
