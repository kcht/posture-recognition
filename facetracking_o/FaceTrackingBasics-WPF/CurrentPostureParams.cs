using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FaceTrackingBasics
{
    class CurrentPostureParams
    {
        public double headTilt;
        public double headYaw;
        public double headRoll;

        public double headShouldersCenterYideal;
        public double headYcurrent;
        public double shouldersCenterYcurrent;
        public double shoulderCenterZcurrent;
        public double shouldersCenterZideal;

        public double chinZcurrent;
        public double chinZideal;

        public double shoulderLeftZcurrent;
        public double shoulderRightZcurrent;
        public double averageShouldersZideal;

        public double neckAngleCurrent;

        public double leftWristYposition;
        public double rightWristYpostion;
    }
}
