using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;


namespace FaceTrackingBasics
{
    class WrongPostureClassifier
    {

        static MainWindow mainWindowRef;

        public static void setRef(MainWindow mainWindow){
            mainWindowRef = mainWindow;
        }
        
        public enum WrongPostures
        {
            NeckFront,
            Lounge,
            Sideways,
            Crouching,
            LeaningForward,
            KneesBended,
            PostureOK
        };

        //todo: define all gestures
        public static WrongPostures diagnozeWrongPosture(CurrentPostureParams cpp) {

            double averageShouldersZ = (cpp.shoulderLeftZcurrent + cpp.shoulderRightZcurrent) /2;
            double shouldersZratio = cpp.shoulderCenterZcurrent / cpp.shouldersCenterZideal;
            double chinZratio = cpp.chinZcurrent / cpp.chinZideal;
            double headShouldersYdistanceRatio = (cpp.headYcurrent - cpp.shouldersCenterYcurrent) / cpp.headShouldersCenterYideal;
            double avereageShoulderZ_sideways_center = averageShouldersZ - cpp.shoulderCenterZcurrent;
            double averageShoulderLRRatio = averageShouldersZ / cpp.averageShouldersZideal;

            WrongPostures detectedPosture = WrongPostures.PostureOK;

            mainWindowRef.ratioChinZcontrol.Text = chinZratio.ToString("0.##");
            mainWindowRef.shoulderLeftZcontrol.Text = cpp.shoulderLeftZcurrent.ToString("0.##");
            mainWindowRef.shoulderRightZcontrol.Text = cpp.shoulderRightZcurrent.ToString("0.##");
            mainWindowRef.roznicaShoulderCenterShoulderLFcontrol.Text = avereageShoulderZ_sideways_center.ToString("0.##");
            mainWindowRef.ratioHeadShouldersYControl.Text = headShouldersYdistanceRatio.ToString("0.##");
            mainWindowRef.neckAngleControl.Text = cpp.neckAngleCurrent.ToString("0.##");
            mainWindowRef.ratioShoulderZcontrol.Text = shouldersZratio.ToString("0.##");
            mainWindowRef.averageShoulderLRRatioControl.Text = averageShouldersZ.ToString("0.##");

            if (checkNeckFrontOK(cpp.neckAngleCurrent, cpp.headTilt, chinZratio) == false)
            {
                changeControlState(true, WrongPostures.NeckFront);
                detectedPosture =  WrongPostures.NeckFront;
            }
            else {
                changeControlState(false, WrongPostures.NeckFront);

            }



             if (checkLeaningForwardOK(cpp.headTilt, chinZratio, shouldersZratio) == false)
            {
                changeControlState(true, WrongPostures.LeaningForward);

                detectedPosture = WrongPostures.LeaningForward;
            }
             else
             {
                 changeControlState(false, WrongPostures.LeaningForward);

             }


             if (checkLoungeOK(cpp.headTilt, chinZratio, shouldersZratio, cpp.neckAngleCurrent, headShouldersYdistanceRatio) == false)
            {
                changeControlState(true, WrongPostures.Lounge);

                detectedPosture = WrongPostures.Lounge;
            }
             else
             {
                 changeControlState(false, WrongPostures.Lounge);

             }
             
              
             if (checkSidewaysOK(cpp.shoulderLeftZcurrent, cpp.shoulderRightZcurrent, cpp.headRoll, 
                 cpp.headYaw, cpp.leftWristYposition, cpp.rightWristYpostion, cpp.shouldersCenterYcurrent) == false)
            {
                changeControlState(true, WrongPostures.Sideways);

                detectedPosture = WrongPostures.Sideways;
            }

             else
             {
                 changeControlState(false, WrongPostures.Sideways);

             }


             if (checkCrouchingOK(cpp.headTilt, avereageShoulderZ_sideways_center, headShouldersYdistanceRatio, averageShoulderLRRatio) == false)
            {
                changeControlState(true, WrongPostures.Crouching);

                detectedPosture = WrongPostures.Crouching;
            }
             else
             {
                 changeControlState(false, WrongPostures.Crouching);

             }

             if (detectedPosture == WrongPostures.PostureOK)
             {
                 changeControlState(false, WrongPostures.PostureOK);
             }
             return detectedPosture;
           

        }

        private static bool checkLeaningForwardOK(double headTilt, double chinZratio, double shouldersZratio)
        {
            if (chinZratio < 0.9 && shouldersZratio < 0.9) {
                return false;
            }
            //zmniejszenie odleglosci glowa sensor
            //zmniejszenie odleglosci ramiona sensor
            //nieznaczne zwiekszenie tiltu  
            return true;
        }

        private static bool checkLoungeOK(double headTilt, double chinZratio, double shouldersZratio, double neckAngle, double headShouldersYdistanceRatio)
        {
            //zmniejsza sie odl Y glowa ramiona 
            //chin z zwieksza sie
            //mniejszy tilt (glowa do gory!)
            //neck angle ujemny
            if (chinZratio >1.15 && shouldersZratio > 1.11 && neckAngle <0)
            {
                return false;
            }
            
            return true;
        }

        private static bool checkCrouchingOK(double headTilt, double dystansBokiCentrumRamion, double headShouldersYdistanceRatio, double averageShoulderLRRatio)
        {
            if (headTilt < -30 && dystansBokiCentrumRamion > 0.2) {
                return false;
            }
            return true;
        }

        private static bool checkSidewaysOK(double shoulderL_Z, double shoulderR_Z, double headRoll, double headYaw, double wristL_Y, double wristR_Y, double shoulderCenterY)
        {
            if (wristL_Y >= shoulderCenterY && headRoll > 20) { return true; }
            if (wristR_Y >= shoulderCenterY && headRoll < -20) { return true; }

            if (Math.Abs(shoulderR_Z - shoulderL_Z) > 0.1) {
                if (shoulderR_Z > shoulderL_Z)
                {
                    if (headYaw > 25) {
                        return false;
                    }
                }
                else {
                    if (headYaw < -25) {
                        return false;
                    }
                
                }
            }
            return true;
        }

        private static bool checkNeckFrontOK(double neckAngle, double headTilt, double chinZratio)
        {
            if (neckAngle >= 40 && chinZratio > 0.9) {
                return false;
            }
            return true;
        }

        public static String postureToString(WrongPostures wp) {
            switch (wp) {
                case WrongPostures.Crouching: 
                    
                    return "CROUCHING: Garbisz się!";
                case WrongPostures.KneesBended: return "KNEESBENDED: Wyprostuj kolana!";
                case WrongPostures.LeaningForward: return "LEANINGFWD: Pochylasz się do przodu!";
                case WrongPostures.Lounge: return "LOUNGE: Lezysz!";
                case WrongPostures.NeckFront: return "NECKFRONT: Szyja jest za bardzo do przodu!";
                case WrongPostures.Sideways: return "SIDEWAYS: Za bardzo na bok...";
                case WrongPostures.PostureOK: return "POSTURE OK :)";
                default: return "default";
                    
            }
        }

        private static void changeControlState(bool turnOn, WrongPostures wp)
        {

            SolidColorBrush brush;
            brush = turnOn ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.LightGray);

            switch (wp)
            {
                case WrongPostures.Crouching:
                    mainWindowRef.crouchingControl.Fill = brush;
                    break;
                
                case WrongPostures.LeaningForward: mainWindowRef.leaningForwardControl.Fill = brush;
                    break;
                case WrongPostures.Lounge: mainWindowRef.loungingControl.Fill = brush;
                    break;
                case WrongPostures.NeckFront: mainWindowRef.neckFrontControl.Fill = brush;
                    break;
                case WrongPostures.Sideways: mainWindowRef.sidewaysBendControl.Fill = brush;
                    break;
                case WrongPostures.PostureOK:
                    mainWindowRef.crouchingControl.Fill = brush;
                    mainWindowRef.leaningForwardControl.Fill = brush;
                    mainWindowRef.loungingControl.Fill = brush;
                    mainWindowRef.neckFrontControl.Fill = brush;
                    mainWindowRef.sidewaysBendControl.Fill = brush;
                    break;
            }
        }


       

        
    }
}
