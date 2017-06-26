using System;

namespace CombatMaster.Data
{
    public class Point3D
    {
        private double[] XYZ = new double[3];

        public double X { get { return XYZ[0]; } set { XYZ[0] = value; } }
        public double Y { get { return XYZ[1]; } set { XYZ[1] = value; } }
        public double Z { get { return XYZ[2]; } set { XYZ[2] = value; } }

        public Point3D(double[] coords)
        {
            if (coords == null)
                throw new ArgumentNullException();

            if (coords.Length != 3)
                throw new ArgumentException();

            XYZ[0] = coords[0];
            XYZ[1] = coords[1];
            XYZ[2] = coords[2];
        }

        public double this[int index]
        {
            get { return XYZ[index]; } set { XYZ[index] = value; }
        }
    }
}
