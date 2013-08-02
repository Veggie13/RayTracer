using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RayTracer.Geometry;

namespace RayTracer
{
    interface ISurface
    {
        void ModifyReflection(Point3f defNormal, out Point3f newNormal, out float reflectivity);
    }

    class MirrorSurface : ISurface
    {
        public float _reflectivity = 1;
        public void ModifyReflection(Point3f defNormal, out Point3f newNormal, out float reflectivity)
        {
            newNormal = defNormal.Clone();
            reflectivity = _reflectivity;
        }
    }

    class MatteSurface : ISurface
    {
        public void ModifyReflection(Point3f defNormal, out Point3f newNormal, out float reflectivity)
        {
            throw new NotImplementedException();
        }
    }
}
