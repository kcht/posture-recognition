using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace FaceTrackingBasics
{
    class AngleHelper
    {

        public static double measureAngle2D(Joint jointA, Joint jointB, Joint jointC)
        {

            //measurement based on the cosine theorem 
            double z = getLengthOfLineBetween2D(jointA, jointC);
            double x = getLengthOfLineBetween2D(jointA, jointB);
            double y = getLengthOfLineBetween2D(jointB, jointC);

            return rad2deg(Math.Acos((x * x + y * y - z * z) / (2 * x * y)));

        }

        public static double rad2deg(double rad)
        {
            return 180 * rad / Math.PI;
        }

        public static double getLengthOfLineBetween2D(Joint j1, Joint j2)
        {

            return Math.Sqrt((j1.Position.X - j2.Position.X) * (j1.Position.X - j2.Position.X) +
                (j1.Position.Y - j2.Position.Y) * (j1.Position.Y - j2.Position.Y));

        }

        public static double getZDistance(Joint j1, Joint j2)
        {
            return j1.Position.Z - j2.Position.Z;
        }

        // jak dla body angle.
        public static double measureVerticalDerivationAngle(Joint j1, Joint j2)
        {

            double yDistance = j1.Position.Y - j2.Position.Y;
            double xDistance = j1.Position.X - j2.Position.X;

            return rad2deg(Math.Atan(xDistance / yDistance));

        }
    }
}
