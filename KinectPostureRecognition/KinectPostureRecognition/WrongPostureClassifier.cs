using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace KinectPostureRecognition
{
    class WrongPostureClassifier
    {

        static SkeletonOverlapping mainWindowRef;

        public static void setRef(SkeletonOverlapping mainWindow){
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
        public static WrongPostures diagnozeWrongPosture(double angleKnees, double angleBody, double headAngle,
                                                        double spinalRatio, double sidewaysRatio) {

            if (headAngle < -30)
            {
                changeControlState(true, WrongPostures.NeckFront);
                return WrongPostures.NeckFront;
            }
            else {
                changeControlState(false, WrongPostures.NeckFront);

            }
             if (angleBody <= -3)
            {
                changeControlState(true, WrongPostures.LeaningForward);

                return WrongPostures.LeaningForward;
            }
             else
             {
                 changeControlState(false, WrongPostures.LeaningForward);

             }
             if (angleBody >= 10 )
            {
                changeControlState(true, WrongPostures.Lounge);

                return WrongPostures.Lounge;
            }
             else
             {
                 changeControlState(false, WrongPostures.Lounge);

             }
              if (angleKnees < 113.0)
             {
                 changeControlState(true, WrongPostures.KneesBended);

                return WrongPostures.KneesBended;
                                
            }
              else
              {
                  changeControlState(false, WrongPostures.KneesBended);

              }
             if (sidewaysRatio <= 0.5 || sidewaysRatio >= 2)
            {
                changeControlState(true, WrongPostures.Sideways);

                return WrongPostures.Sideways;
            }
             else
             {
                 changeControlState(false, WrongPostures.Sideways);

             }
             if (spinalRatio <0.95)
            {
                changeControlState(true, WrongPostures.Crouching);

                return WrongPostures.Crouching;
            }
             else
             {
                 changeControlState(false, WrongPostures.Crouching);

             }

             changeControlState(false, WrongPostures.PostureOK);
                return WrongPostures.PostureOK;
           

        }

        public static String postureToString(WrongPostures wp) {
            switch (wp) {
                case WrongPostures.Crouching: 
                    
                    return "CROUCHING: Garbisz się!";
                    break;
                case WrongPostures.KneesBended: return "KNEESBENDED: Wyprostuj kolana!";
                    break;
                case WrongPostures.LeaningForward: return "LEANINGFWD: Pochylasz się do przodu!";
                    break;
                case WrongPostures.Lounge: return "LOUNGE: Lezysz!";
                    break;
                case WrongPostures.NeckFront: return "NECKFRONT: Szyja jest za bardzo do przodu!";
                    break;
                case WrongPostures.Sideways: return "SIDEWAYS: Za bardzo na bok...";
                    break;
                case WrongPostures.PostureOK: return "POSTURE OK :)";
                    break;
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
                case WrongPostures.KneesBended:
                    mainWindowRef.kneesBentControl.Fill = brush;
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
                    mainWindowRef.kneesBentControl.Fill = brush;
                    mainWindowRef.leaningForwardControl.Fill = brush;
                    mainWindowRef.loungingControl.Fill = brush;
                    mainWindowRef.neckFrontControl.Fill = brush;
                    mainWindowRef.sidewaysBendControl.Fill = brush;
                    break;
            }
        }


       

        
    }
}
