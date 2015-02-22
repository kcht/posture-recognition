using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectPostureRecognition
{
    class DistanceRatioHelper
    {
        public static double distanceRatio(double originalDistance, double currentDistance){
            double ratio = currentDistance/ originalDistance;
            return Math.Truncate(ratio * 1000) / 1000;
        }
    }
}
