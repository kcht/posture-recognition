using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;
using System.IO;

namespace KinectPostureRecognition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SkeletonOverlapping : Window
    {
        #region Member Variables
        private KinectSensor kinect;
        private WriteableBitmap colorImageBitmap;
        private Int32Rect colorImageBitmapRect;
        private int colorImageStride;

        private WriteableBitmap depthImageBitmap;
        private Int32Rect depthImageBitmapRect;
        private int depthImageStride;

        byte[] pixelData;
        short[] depthData;
        byte[] depthColorImage;

        private Skeleton[] skeletonData;

        private DrawingGroup drawingGroupColor;
        private DrawingImage imageSourceColor;
        private DrawingGroup drawingGroupDepth;
        private DrawingImage imageSourceDepth;
        private Pen trackedBonePen = new Pen(Brushes.Red, 6);
        private Pen inferredBonePen = new Pen(Brushes.Green, 4);
        private Brush trackedJointBrush = Brushes.Red;
        private Brush inferredJointBrush = Brushes.Green;

        private bool skeletonRenderEnabled = true;
        private bool seatedModeEnabled = false;
        private bool showHalfSkeletonEnabled = false;

        System.Windows.Threading.DispatcherTimer timer;
        TimeSpan time;

        private double idealSpinalDistance;
        private double idealHipShoulderZDistance;
        private double currentSpinaelDistance;
        private double currentHipShoulderZDistance;
        #endregion Member Variables

        #region Constructor
        public SkeletonOverlapping()
        {
            InitializeComponent();

            this.Loaded += (s, e) => { DiscoverKinectSensor(); };
            this.Unloaded += (s, e) => { this.kinect = null; };


        }
        #endregion Constructor

        #region Methods
        private void DiscoverKinectSensor()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (this.Kinect == null)
                    {
                        this.Kinect = e.Sensor;
                    }
                    break;

                case KinectStatus.Disconnected:
                    if (this.Kinect == e.Sensor)
                    {
                        this.Kinect = null;
                        this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);

                        if (this.Kinect == null)
                        {
                            //notify user that the sensor is disconnected. 
                        }

                    }
                    break;

                //other cases 

            }

        }

        private void InitializeKinectSensor(KinectSensor sensor)
        {

            if (kinect != null)
            {
                ColorImageStream colorStream = kinect.ColorStream;
                colorStream.Enable();

                this.colorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                                                            96, 96, PixelFormats.Bgr32, null);
                this.colorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                this.colorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                //initialize drawing 
                this.drawingGroupColor = new DrawingGroup();
                this.imageSourceColor = new DrawingImage(this.drawingGroupColor);
                SkeletonColorOverlappingImage.Source = this.imageSourceColor;

                DepthImageStream depthStream = kinect.DepthStream;
                depthStream.Enable();

                this.depthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight,
                                                           96, 96, PixelFormats.Gray16, null);
                this.depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                this.depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;

                //initialize drawing 
                this.drawingGroupDepth = new DrawingGroup();
                this.imageSourceDepth = new DrawingImage(this.drawingGroupDepth);
                SkeletonDepthOverlappingImage.Source = this.imageSourceDepth;
                //  SkeletonDepthOverlappingImage.Source = depthImageBitmap;


                //\ parameters
                TransformSmoothParameters smoothingParameters = new TransformSmoothParameters();
                {
                    smoothingParameters.Smoothing = 0.5f;
                    smoothingParameters.Correction = 0.5f;
                    smoothingParameters.Prediction = 0.5f;
                    smoothingParameters.JitterRadius = 0.05f;
                    smoothingParameters.MaxDeviationRadius = 0.04f;
                }

                sensor.SkeletonStream.Enable(smoothingParameters);
                skeletonData = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];

                this.kinect.AllFramesReady += kinect_AllFramesReady;
                sensor.Start();

                //timer start
                

                time = TimeSpan.FromSeconds(0);
                timer = new System.Windows.Threading.DispatcherTimer(
                                        new TimeSpan(0,0,1), 
                                        System.Windows.Threading.DispatcherPriority.Normal,
                                        delegate{
                                            timerLabel.Content = time.ToString();
                                            if(time == TimeSpan.FromSeconds(10)) {timer.Stop();}
                                            time = time.Add(TimeSpan.FromSeconds(1));
                                        },
                                        Application.Current.Dispatcher
                                        );

                  
                timer.Start();

                // 
                WrongPostureClassifier.setRef(this);

            }
        }

    

        void kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    if (pixelData == null)
                    {
                        pixelData = new byte[colorFrame.PixelDataLength];
                    }
                    colorFrame.CopyPixelDataTo(pixelData);

                    this.colorImageBitmap.WritePixels(this.colorImageBitmapRect, pixelData, this.colorImageStride, 0);
                }
            }


            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null) { return; }
                if (depthData == null)
                {
                    depthData = new short[depthFrame.PixelDataLength];
                }
                if (depthColorImage == null)
                {
                    depthColorImage = new byte[depthFrame.PixelDataLength * 4];
                }

                depthFrame.CopyPixelDataTo(depthData);
                int depthColorImagePos = 0;
                for (int depthPos = 0; depthPos < depthFrame.PixelDataLength; depthPos++)
                {
                    int depthVal = depthData[depthPos] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                    
                        byte depthByte = (byte)(255 - (depthVal) >> 4);
                        depthColorImage[depthColorImagePos++] = depthByte;
                        depthColorImage[depthColorImagePos++] = depthByte;
                        depthColorImage[depthColorImagePos++] = depthByte;
                    
                    //transparency 
                    depthColorImagePos++;
                }

                

                this.depthImageBitmap.WritePixels(new Int32Rect(0, 0, depthFrame.Width, depthFrame.Height), 
                                           depthData, depthFrame.Width * 2, 0);

            }

            //handle skeleton data

            if (seatedModeEnabled)
            {
                kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            }
            else {
                kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            }
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) {
                if (skeletonFrame != null && this.skeletonData != null) {
                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);
                }

                

                //draw the output
                using (DrawingContext drawingContext = this.drawingGroupColor.Open()) {
                    //color stream output
                    drawingContext.DrawImage(this.colorImageBitmap, new Rect(0,0,kinect.ColorStream.FrameWidth, kinect.ColorStream.FrameHeight));
                    //draw skeleton steram data
                    if (skeletonRenderEnabled == true)
                    {
                        DrawSkeletons(drawingContext);
                    }
                    //define limieted aread
                    this.drawingGroupColor.ClipGeometry = new RectangleGeometry(new Rect(0, 0, kinect.ColorStream.FrameWidth, kinect.ColorStream.FrameHeight));
                }
                using (DrawingContext drawingContext = this.drawingGroupDepth.Open()) {
                    //color stream output
                    drawingContext.DrawImage(this.depthImageBitmap, new Rect(0, 0, kinect.DepthStream.FrameWidth, kinect.DepthStream.FrameHeight));
                    //draw skeleton steram data
                    if (skeletonRenderEnabled == true)
                    {
                        DrawSkeletons(drawingContext);
                    }
                    //define limieted aread
                    this.drawingGroupColor.ClipGeometry = new RectangleGeometry(new Rect(0, 0, kinect.DepthStream.FrameWidth, kinect.DepthStream.FrameHeight));
                
                }
            }
        }

        private void DrawSkeletons(DrawingContext drawingContext)
        {
            foreach (Skeleton skeleton in this.skeletonData) {
                if (skeleton != null) {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked) {
                        DrawTrackedSkeletonJoints(skeleton.Joints, drawingContext);
                        measureAngles(skeleton);
                    }
                    else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly) {
                        DrawSkeletonPosition(skeleton.Position, drawingContext);
                    }
                    RenderClippedEdges(skeleton, drawingContext);
                }
            }
        }

        private void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
             if(skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom)){
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, kinect.ColorStream.FrameHeight -10, 
                                    kinect.ColorStream.FrameWidth, 10));
             }
             if(skeleton.ClippedEdges.HasFlag(FrameEdges.Top)){
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, kinect.ColorStream.FrameWidth, 10));
             }
            if(skeleton.ClippedEdges.HasFlag(FrameEdges.Right)){
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(kinect.ColorStream.FrameWidth-10, 0, 
                                    10, kinect.ColorStream.FrameHeight));
             }
            if(skeleton.ClippedEdges.HasFlag(FrameEdges.Left)){
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 10, kinect.ColorStream.FrameHeight));
                                    
             }
        }

        private void DrawSkeletonPosition(SkeletonPoint skeletonPoint, DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(Brushes.Gold, null, this.SkeletonPointToScreen(skeletonPoint), 20, 20);
        }

        private void DrawTrackedSkeletonJoints(JointCollection jointCollection, DrawingContext drawingContext)
        {
          
            //spine
            DrawBone(jointCollection[JointType.Head], jointCollection[JointType.ShoulderCenter], drawingContext);
            DrawBone(jointCollection[JointType.ShoulderCenter], jointCollection[JointType.Spine], drawingContext);
            DrawBone(jointCollection[JointType.Spine], jointCollection[JointType.HipCenter], drawingContext);

            //right side
            //Right arm
            DrawBone(jointCollection[JointType.ShoulderCenter], jointCollection[JointType.ShoulderRight], drawingContext);
            DrawBone(jointCollection[JointType.ShoulderRight], jointCollection[JointType.ElbowRight], drawingContext);
            DrawBone(jointCollection[JointType.ElbowRight], jointCollection[JointType.WristRight], drawingContext);
            DrawBone(jointCollection[JointType.WristRight], jointCollection[JointType.HandRight], drawingContext);

            //Right leg
            DrawBone(jointCollection[JointType.HipCenter], jointCollection[JointType.HipRight], drawingContext);
            DrawBone(jointCollection[JointType.HipRight], jointCollection[JointType.KneeRight], drawingContext);
            DrawBone(jointCollection[JointType.KneeRight], jointCollection[JointType.AnkleRight], drawingContext);
            DrawBone(jointCollection[JointType.AnkleRight], jointCollection[JointType.FootRight], drawingContext);

            if (!showHalfSkeletonEnabled)
            {
                //left arm
                DrawBone(jointCollection[JointType.ShoulderCenter], jointCollection[JointType.ShoulderLeft], drawingContext);
                DrawBone(jointCollection[JointType.ShoulderLeft], jointCollection[JointType.ElbowLeft], drawingContext);
                DrawBone(jointCollection[JointType.ElbowLeft], jointCollection[JointType.WristLeft], drawingContext);
                DrawBone(jointCollection[JointType.WristLeft], jointCollection[JointType.HandLeft], drawingContext);

                //left leg
                DrawBone(jointCollection[JointType.HipCenter], jointCollection[JointType.HipLeft], drawingContext);
                DrawBone(jointCollection[JointType.HipLeft], jointCollection[JointType.KneeLeft], drawingContext);
                DrawBone(jointCollection[JointType.KneeLeft], jointCollection[JointType.AnkleLeft], drawingContext);
                DrawBone(jointCollection[JointType.AnkleLeft], jointCollection[JointType.FootLeft], drawingContext);
            }

            foreach (Joint singleJoint in jointCollection) {
                if (!showHalfSkeletonEnabled) {
                    DrawJoint(singleJoint, drawingContext);
                }
                else if (singleJoint.JointType == JointType.AnkleRight || singleJoint.JointType == JointType.ElbowRight
                    || singleJoint.JointType == JointType.FootRight || singleJoint.JointType == JointType.HandRight
                    || singleJoint.JointType == JointType.HipRight || singleJoint.JointType == JointType.KneeRight
                    || singleJoint.JointType == JointType.ShoulderRight || singleJoint.JointType == JointType.WristRight ) {
                        DrawJoint(singleJoint, drawingContext);
                }
            }

        }

        private void DrawJoint(Joint singleJoint, DrawingContext drawingContext)
        {
            if (singleJoint.TrackingState == JointTrackingState.NotTracked )
            {
                return;
            }
            if (singleJoint.TrackingState == JointTrackingState.Inferred)
            {
                DrawInferredJoint(singleJoint.Position, drawingContext);
            }
            if (singleJoint.TrackingState == JointTrackingState.Tracked)
            {
                DrawTrackedJoint(singleJoint.Position, drawingContext);
            }
        }

        private void DrawTrackedJoint(SkeletonPoint skeletonPoint, DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(this.trackedJointBrush, null, this.SkeletonPointToScreen(skeletonPoint), 10, 10);
        }

        private void DrawInferredJoint(SkeletonPoint skeletonPoint, DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(this.inferredJointBrush, null, this.SkeletonPointToScreen(skeletonPoint), 10, 10);
        }

        private void DrawBone(Joint joint1, Joint joint2, DrawingContext drawingContext)
        {
            if (joint1.TrackingState == JointTrackingState.NotTracked || joint2.TrackingState == JointTrackingState.NotTracked) {
                return;
            }
            if (joint1.TrackingState == JointTrackingState.Inferred || joint2.TrackingState == JointTrackingState.Inferred) {
                DrawInferredBoneLine(joint1.Position, joint2.Position, drawingContext);
            }
            if (joint1.TrackingState == JointTrackingState.Tracked && joint2.TrackingState == JointTrackingState.Tracked) {
                DrawTrackedBoneLine(joint1.Position, joint2.Position, drawingContext);
            }

        }

        private void DrawInferredBoneLine(SkeletonPoint skeletonPoint1, SkeletonPoint skeletonPoint2, DrawingContext drawingContext)
        {
            drawingContext.DrawLine(this.inferredBonePen, this.SkeletonPointToScreen(skeletonPoint1), this.SkeletonPointToScreen(skeletonPoint2));
        }

        private void DrawTrackedBoneLine(SkeletonPoint skeletonPoint1, SkeletonPoint skeletonPoint2, DrawingContext drawingContext)
        {
            drawingContext.DrawLine(this.trackedBonePen, this.SkeletonPointToScreen(skeletonPoint1), this.SkeletonPointToScreen(skeletonPoint2));
        }

        private Point SkeletonPointToScreen(SkeletonPoint skeletonPoint1)
        {
            ColorImagePoint colorPoint = this.kinect.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint1, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(colorPoint.X, colorPoint.Y);
        }

        

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
             
            if (sensor != null)
            {
                sensor.Stop();
                sensor.AllFramesReady -= kinect_AllFramesReady;
            }
        }

        public void measureAngles(Skeleton skeleton)
        {
            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                //todo: all tracking states
                double kneeAngle=0, bodyAngle=0, headAngle=0;
                double sidewaysRatio = 0, spinalRatio = 0;
                if (skeleton.Joints[JointType.AnkleRight].TrackingState == JointTrackingState.Tracked ||
                    skeleton.Joints[JointType.AnkleRight].TrackingState == JointTrackingState.Inferred)
                {
                     kneeAngle = AngleHelper.measureAngle2D(skeleton.Joints[JointType.AnkleRight],
                                            skeleton.Joints[JointType.KneeRight],
                                            skeleton.Joints[JointType.HipRight]);
                    kneeAngleText.Text = kneeAngle.ToString();
                }
                if(skeleton.Joints[JointType.HipCenter].TrackingState == JointTrackingState.Tracked){

                    bodyAngle = AngleHelper.measureVerticalDerivationAngle(skeleton.Joints[JointType.HipCenter],                                          
                                            skeleton.Joints[JointType.ShoulderCenter]);
                    bodyAngleText.Text = bodyAngle.ToString();

                    //spine distance
                     currentSpinaelDistance = AngleHelper.getLengthOfLineBetween2D(skeleton.Joints[JointType.HipCenter],
                                            skeleton.Joints[JointType.ShoulderCenter]);
                     spinalRatio = DistanceRatioHelper.distanceRatio(idealSpinalDistance, currentSpinaelDistance);
                     spinalRatioText.Text = spinalRatio.ToString();
                }
                if (skeleton.Joints[JointType.ShoulderCenter].TrackingState == JointTrackingState.Tracked)
                {

                    headAngle = AngleHelper.measureVerticalDerivationAngle(skeleton.Joints[JointType.ShoulderCenter],
                                            skeleton.Joints[JointType.Head]);
                   headAngleText.Text = headAngle.ToString();

                   //sideways distance
                   currentHipShoulderZDistance = AngleHelper.getZDistance(skeleton.Joints[JointType.ShoulderRight],
                                           skeleton.Joints[JointType.HipCenter]);
                   sidewaysRatio = DistanceRatioHelper.distanceRatio(idealHipShoulderZDistance, currentHipShoulderZDistance);
                   sidewaysRatioText.Text = sidewaysRatio.ToString();

                    
                }

                

                postureInfoText.Text = WrongPostureClassifier.postureToString(
                    WrongPostureClassifier.diagnozeWrongPosture(kneeAngle, bodyAngle, headAngle, spinalRatio, sidewaysRatio));

               
                
                
                    
                    
                    //if (kneeAngle < 100)
                    //{
                    //    controlError.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    //}
                    //else
                    //{
                    //    controlError.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    //}

                
            }
        }


        #endregion Methods


        #region Properties
        public KinectSensor Kinect
        {
            get { return this.kinect; }
            set
            {
                if (this.kinect != value)
                {
                    if (this.kinect != null)
                    {
                        UninitializeKinectSensor(this.kinect);
                        this.kinect = null;
                    }
                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        this.kinect = value;
                        InitializeKinectSensor(this.kinect);
                    }
                }
            }
        }
        #endregion Properties
        private void TakePictureButtonDepth_Click(object sender, RoutedEventArgs e) {
            //configure save file dialog box
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "snapshot";
            dialog.DefaultExt = ".jpg";
            dialog.Filter = "Pictures (.jpg)|*.jpg";

            //process save file dialgo box results
            if (dialog.ShowDialog() == true)
            {
                //save file
                string filename = dialog.FileName;
                using (FileStream fileStream = new FileStream(filename, FileMode.Create))
                {
                    BitmapSource imageToSave = Converters.ToBitmapSource(SkeletonDepthOverlappingImage.Source);

                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.QualityLevel = 70;
                    encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                    encoder.Save(fileStream);

                    fileStream.Flush();
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
        }

        private void TakePictureButtonColor_Click(object sender, RoutedEventArgs e)
        {
            //configure save file dialog box
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "snapshot";
            dialog.DefaultExt = ".jpg";
            dialog.Filter = "Pictures (.jpg)|*.jpg";

            //process save file dialgo box results
            if (dialog.ShowDialog() == true)
            {
                //save file
                string filename = dialog.FileName;
                using (FileStream fileStream = new FileStream(filename, FileMode.Create))
                {
                    BitmapSource imageToSave = Converters.ToBitmapSource(SkeletonColorOverlappingImage.Source);

                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.QualityLevel = 70;
                    encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                    encoder.Save(fileStream);

                    fileStream.Flush();
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
        }

        private void SkeletonVisible_Checked(object sender, RoutedEventArgs e)
        {
            skeletonRenderEnabled = true;
        }

        private void SkeletonVisible_Unchecked(object sender, RoutedEventArgs e)
        {
            skeletonRenderEnabled = false;
        }
        private void SeatedMode_Checked(object sender, RoutedEventArgs e)
        {
            seatedModeEnabled = true;
        }

        private void SeatedMode_Unchecked(object sender, RoutedEventArgs e)
        {
            seatedModeEnabled = false;
        }

        private void ShowHalfSkeleton_Checked(object sender, RoutedEventArgs e)
        {
            showHalfSkeletonEnabled = true;
        }

        private void ShowHalfSkeleton_Unchecked(object sender, RoutedEventArgs e)
        {
            showHalfSkeletonEnabled = false;
        }

        private void timerButton_Click(object sender, RoutedEventArgs e) {
            ResetTimer();
        }

        private void ResetTimer() { 
            timer.Stop();
            time = TimeSpan.FromSeconds(0); 
            timer.Start();
            timerLabel.Content = time.ToString();
        }
        private void recordGoodPosture_Click(object sender, RoutedEventArgs e) {
            idealSpinalDistance = currentSpinaelDistance;
            idealHipShoulderZDistance = currentHipShoulderZDistance;
        }
        

    }
}
