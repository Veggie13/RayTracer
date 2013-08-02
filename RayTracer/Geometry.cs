using System;

namespace RayTracer.Geometry
{
    public struct Point3f
    {
        public Point3f(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X;
        public float Y;
        public float Z;

        public Point3f Clone()
        {
            return new Point3f(X, Y, Z);
        }

        public Point3f Add(Point3f rhs)
        {
            return new Point3f(X + rhs.X, Y + rhs.Y, Z + rhs.Z);
        }

        public Point3f Subtract(Point3f rhs)
        {
            return new Point3f(X - rhs.X, Y - rhs.Y, Z - rhs.Z);
        }

        public Point3f Scale(float scale)
        {
            return new Point3f(X * scale, Y * scale, Z * scale);
        }

        public float Magnitude()
        {
            return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public float Dot(Point3f rhs)
        {
            return X * rhs.X + Y * rhs.Y + Z * rhs.Z;
        }

        public Point3f Cross(Point3f rhs)
        {
            return new Point3f(
                Y * rhs.Z - Z * rhs.Y,
                Z * rhs.X - X * rhs.Z,
                X * rhs.Y - Y * rhs.X);
        }
    }

    public struct Direction3f
    {
        public Direction3f(Point3f vec)
        {
            Brg = (float)Math.Atan2(vec.Y, vec.X);
            double subMag = Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
            Az = (float)Math.Atan(vec.Z / subMag);
        }

        public Direction3f(float b, float az)
        {
            Brg = b;
            Az = az;
        }

        public float Brg;
        public float Az;

        public Direction3f Clone()
        {
            return new Direction3f(Brg, Az);
        }

        //public void LineEquation(out float A, out float B, out float C, out float D)
        //{
        //}

        public Point3f UnitVector()
        {
            double cosAz = Math.Cos(Az);
            return new Point3f((float)(Math.Cos(Brg) * cosAz), (float)(Math.Sin(Brg) * cosAz), (float)Math.Sin(Az));
        }
    }

    public struct Transform3f
    {
        public static Transform3f DirectionalRotation(Direction3f dir)
        {
            var result = new Transform3f(0);
            float cosAz = (float)Math.Cos(dir.Az);
            float sinAz = (float)Math.Sin(dir.Az);
            float cosBrg = (float)Math.Cos(dir.Brg);
            float sinBrg = (float)Math.Sin(dir.Brg);

            result.matrix[0, 0] = cosBrg * cosAz;
            result.matrix[0, 1] = -sinBrg;
            result.matrix[0, 2] = -cosBrg * sinAz;

            result.matrix[1, 0] = sinBrg * cosAz;
            result.matrix[1, 1] = cosBrg;
            result.matrix[1, 2] = -sinBrg * sinAz;

            result.matrix[2, 0] = sinAz;
            result.matrix[2, 1] = 0;
            result.matrix[2, 2] = cosAz;

            return result;
        }

        public float[,] matrix;

        public Transform3f Compose(Transform3f rhs)
        {
            var result = new Transform3f(0);

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    for (int i = 0; i < 3; i++)
                        result.matrix[r, c] += matrix[i, c] * rhs.matrix[r, i];
                }
            }

            return result;
        }

        public Transform3f(float init)
        {
            matrix = new float[3, 3];
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    matrix[r, c] = init;
                }
            }
        }
        
        public Point3f Transform(Point3f rhs)
        {
            float[] vec = new float[] { rhs.X, rhs.Y, rhs.Z };
            float[] resVec = new float[3];

            for (int r = 0; r < 3; r++)
            {
                resVec[r] = 0;
                for (int i = 0; i < 3; i++)
                    resVec[r] += matrix[r, i] * vec[i];
            }

            return new Point3f(resVec[0], resVec[1], resVec[2]);
        }
    }

    public struct Ray3f
    {
        public Ray3f(Point3f src, Direction3f dir)
        {
            Src = src.Clone();
            Dir = dir.Clone();
        }

        public Point3f Src;
        public Direction3f Dir;

        public Point3f PointAt(float d)
        {
            return Src.Add(Dir.UnitVector().Scale(d));
        }
    }

    public struct Plane3f
    {
        public Plane3f(Point3f pt, Point3f normal)
        {
            A = normal.X;
            B = normal.Y;
            C = normal.Z;
            D = pt.Dot(normal);
        }

        public Plane3f(float a, float b, float c, float d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public float A;
        public float B;
        public float C;
        public float D;

        public Direction3f Normal()
        {
            return new Direction3f(new Point3f(A, B, C));
        }
        
        public Point3f UnitNormal()
        {
            Point3f temp = new Point3f(A, B, C);
            return temp.Scale(1 / temp.Magnitude());
        }

        public Direction3f Reflect(Direction3f inbound)
        {
            Point3f v = inbound.UnitVector();
            Point3f n = UnitNormal();
            return new Direction3f(v.Subtract(n.Scale(2 * v.Dot(n))));
        }

        public Direction3f Refract(Direction3f inbound, float refr)
        {
            Point3f v = inbound.UnitVector();
            Point3f n = UnitNormal();
            float cos1 = Math.Abs(n.Dot(v));
            float cos2 = (float)Math.Sqrt(1 - (1 - cos1 * cos1) / (refr * refr));

            return new Direction3f(v.Scale(1 / refr).Add(n.Scale(cos1 / refr - cos2)));
        }

        public bool Intersect(Ray3f inbound, ref Point3f result)
        {
            float D2 = A * inbound.Src.X + B * inbound.Src.Y + C * inbound.Src.Z;
            Point3f temp = new Point3f(A, B, C);
            double separation = Math.Abs(D - D2) / temp.Magnitude();
            float cos = inbound.Dir.UnitVector().Dot(UnitNormal());
            double dist = Math.Abs(separation / cos);
            temp = inbound.PointAt((float)dist);
            float D3 = A * temp.X + B * temp.Y + C * temp.Z;
            if (Math.Abs(D - D3) < 1e-5)
            {
                result = temp;
                return true;
            }
            return false;
        }
    }

    public struct Sphere3f
    {
        public Sphere3f(Point3f pos, float r)
        {
            Pos = pos.Clone();
            R = r;
        }

        public Point3f Pos;
        public float R;

        public bool FirstIntersect(Ray3f ray, ref Point3f result)
        {
            Point3f unit = ray.Dir.UnitVector();
            Point3f toCenter = Pos.Subtract(ray.Src);
            float mag = toCenter.Magnitude();
            float dot = unit.Dot(toCenter);
            if (dot < 0)
                return false;
            double separation2 = mag * mag - dot * dot;
            if (separation2 > R * R)
                return false;

            float interior = (float)Math.Sqrt(R * R - separation2);
            
            result = ray.PointAt(dot - interior);
            return true;
        }
    }

}
