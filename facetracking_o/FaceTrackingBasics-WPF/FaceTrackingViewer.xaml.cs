// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FaceTrackingViewer.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace FaceTrackingBasics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit.FaceTracking;

    using Point = System.Windows.Point;

    /// <summary>
    /// Class that uses the Face Tracking SDK to display a face mask for
    /// tracked skeletons
    /// </summary>
    public partial class FaceTrackingViewer : UserControl, IDisposable
    {
        public static readonly DependencyProperty KinectProperty = DependencyProperty.Register(
            "Kinect", 
            typeof(KinectSensor), 
            typeof(FaceTrackingViewer), 
            new PropertyMetadata(
                null, (o, args) => ((FaceTrackingViewer)o).OnSensorChanged((KinectSensor)args.OldValue, (KinectSensor)args.NewValue)));

        private const uint MaxMissedFrames = 100;

        private readonly Dictionary<int, SkeletonFaceTracker> trackedSkeletons = new Dictionary<int, SkeletonFaceTracker>();

        private byte[] colorImage;

        private ColorImageFormat colorImageFormat = ColorImageFormat.Undefined;

        private short[] depthImage;

        private DepthImageFormat depthImageFormat = DepthImageFormat.Undefined;

        private bool disposed;

        private Skeleton[] skeletonData;

        private Pen trackedBonePen = new Pen(Brushes.Red, 6);
        private Pen inferredBonePen = new Pen(Brushes.Green, 4);
        private Brush trackedJointBrush = Brushes.Red;
        private Brush inferredJointBrush = Brushes.Green;


        //dla gui
        public static double yawVal;
        public static double pitchVal;
        public static double rollVal;
        


        public static double chinZ;

        CurrentPostureParams currentPostureParams = new CurrentPostureParams();
        private double shouldersCenterZideal;
        private double idealShoulderHead;
        private double averageShouldersZideal;
        private double idealChinZ;

        public static bool recordIdeal = false;

        public FaceTrackingViewer()
        {
            this.InitializeComponent();
        }

        ~FaceTrackingViewer()
        {
            this.Dispose(false);
        }

        public KinectSensor Kinect
        {
            get
            {
                return (KinectSensor)this.GetValue(KinectProperty);
            }

            set
            {
                this.SetValue(KinectProperty, value);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.ResetFaceTracking();

                this.disposed = true;
            }
        }

        

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            foreach (SkeletonFaceTracker faceInformation in this.trackedSkeletons.Values)
            {
                faceInformation.DrawFaceModel(drawingContext);
                
            }
            if (skeletonData != null)
            {
               DrawSkeletons(drawingContext);
            }

                
            
        }

        private void DrawSkeletons(DrawingContext drawingContext)
        {
            foreach (Skeleton skeleton in this.skeletonData) {
                if (skeleton != null) {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked) {
                        DrawTrackedSkeletonJoints(skeleton.Joints, drawingContext);
                        
                        //checkFor recording ideal posture
                        if (recordIdeal == true) {
                            recordIdeal = false;
                            recordIdealPosture(skeleton);
                        }

                        //tutaj przetwarzanie katow itd HERE
                        updateCurrentPostureParams(skeleton);

                        WrongPostureClassifier.diagnozeWrongPosture(currentPostureParams);
                    }
                    else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly) {
                        DrawSkeletonPosition(skeleton.Position, drawingContext);
                    }
                    RenderClippedEdges(skeleton, drawingContext);
                }
            }
        }

        private void recordIdealPosture(Skeleton skeleton)
        {
            this.shouldersCenterZideal = skeleton.Joints[JointType.ShoulderCenter].Position.Z;
            this.idealShoulderHead = skeleton.Joints[JointType.Head].Position.Y - skeleton.Joints[JointType.ShoulderCenter].Position.Y;
            this.averageShouldersZideal = (skeleton.Joints[JointType.ShoulderRight].Position.Z + skeleton.Joints[JointType.ShoulderLeft].Position.Z ) /2;
            this.idealChinZ = chinZ;

            
        }

        //todo: tilt itd. nalezy podmienic zeby korzystaly z wartosci wzglednych 
        private void updateCurrentPostureParams(Skeleton skeleton)
        {
            this.currentPostureParams.headRoll = rollVal;
            this.currentPostureParams.headTilt = pitchVal;
            this.currentPostureParams.headYaw = yawVal;

            this.currentPostureParams.headShouldersCenterYideal = idealShoulderHead;
            this.currentPostureParams.headYcurrent = skeleton.Joints[JointType.Head].Position.Y;
            this.currentPostureParams.shouldersCenterYcurrent = skeleton.Joints[JointType.ShoulderCenter].Position.Y;
            this.currentPostureParams.shoulderCenterZcurrent = skeleton.Joints[JointType.ShoulderCenter].Position.Z;
            this.currentPostureParams.shouldersCenterZideal = this.shouldersCenterZideal;

            this.currentPostureParams.chinZcurrent = chinZ;
            this.currentPostureParams.chinZideal = this.idealChinZ;

            this.currentPostureParams.shoulderLeftZcurrent = skeleton.Joints[JointType.ShoulderLeft].Position.Z;
           this.currentPostureParams.shoulderRightZcurrent = skeleton.Joints[JointType.ShoulderRight].Position.Z;
           this.currentPostureParams.averageShouldersZideal = this.averageShouldersZideal;

          Joint headJoint = skeleton.Joints[JointType.Head];
          Joint shoulderCenterJoint = skeleton.Joints[JointType.ShoulderCenter];
          double yDistance = headJoint.Position.Y - shoulderCenterJoint.Position.Y;
          double zDistance = headJoint.Position.Z - shoulderCenterJoint.Position.Z;
          currentPostureParams.neckAngleCurrent = AngleHelper.rad2deg(Math.Atan(zDistance / yDistance));

          this.currentPostureParams.leftWristYposition = skeleton.Joints[JointType.WristLeft].Position.Y;
          this.currentPostureParams.rightWristYpostion = skeleton.Joints[JointType.WristRight].Position.Y;
           
        }

        private void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
             if(skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom)){
                drawingContext.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0, Kinect.ColorStream.FrameHeight -10, 
                                    Kinect.ColorStream.FrameWidth, 10));
             }
             if(skeleton.ClippedEdges.HasFlag(FrameEdges.Top)){
                 drawingContext.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0, 0, Kinect.ColorStream.FrameWidth, 10));
             }
            if(skeleton.ClippedEdges.HasFlag(FrameEdges.Right)){
                drawingContext.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(Kinect.ColorStream.FrameWidth - 10, 0,
                                    10, Kinect.ColorStream.FrameHeight));
             }
            if(skeleton.ClippedEdges.HasFlag(FrameEdges.Left)){
                drawingContext.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0, 0, 10, Kinect.ColorStream.FrameHeight));
                                    
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
            

            foreach (Joint singleJoint in jointCollection) {
                
                DrawJoint(singleJoint, drawingContext);
                
                
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
            ColorImagePoint colorPoint = this.Kinect.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint1, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(colorPoint.X, colorPoint.Y);
        }


        private void OnAllFramesReady(object sender, AllFramesReadyEventArgs allFramesReadyEventArgs)
        {
            ColorImageFrame colorImageFrame = null;
            DepthImageFrame depthImageFrame = null;
            SkeletonFrame skeletonFrame = null;

            try
            {
                colorImageFrame = allFramesReadyEventArgs.OpenColorImageFrame();
                depthImageFrame = allFramesReadyEventArgs.OpenDepthImageFrame();
                skeletonFrame = allFramesReadyEventArgs.OpenSkeletonFrame();

                if (colorImageFrame == null || depthImageFrame == null || skeletonFrame == null)
                {
                    return;
                }

                // Check for image format changes.  The FaceTracker doesn't
                // deal with that so we need to reset.
                if (this.depthImageFormat != depthImageFrame.Format)
                {
                    this.ResetFaceTracking();
                    this.depthImage = null;
                    this.depthImageFormat = depthImageFrame.Format;
                }

                if (this.colorImageFormat != colorImageFrame.Format)
                {
                    this.ResetFaceTracking();
                    this.colorImage = null;
                    this.colorImageFormat = colorImageFrame.Format;
                }

                // Create any buffers to store copies of the data we work with
                if (this.depthImage == null)
                {
                    this.depthImage = new short[depthImageFrame.PixelDataLength];
                }

                if (this.colorImage == null)
                {
                    this.colorImage = new byte[colorImageFrame.PixelDataLength];
                }
                
                // Get the skeleton information
                if (this.skeletonData == null || this.skeletonData.Length != skeletonFrame.SkeletonArrayLength)
                {
                    this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                }

                colorImageFrame.CopyPixelDataTo(this.colorImage);
                depthImageFrame.CopyPixelDataTo(this.depthImage);
                skeletonFrame.CopySkeletonDataTo(this.skeletonData);

                // Update the list of trackers and the trackers with the current frame information
                foreach (Skeleton skeleton in this.skeletonData)
                {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked
                        || skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                    {
                        // We want keep a record of any skeleton, tracked or untracked.
                        if (!this.trackedSkeletons.ContainsKey(skeleton.TrackingId))
                        {
                            this.trackedSkeletons.Add(skeleton.TrackingId, new SkeletonFaceTracker());
                        }

                        // Give each tracker the upated frame.
                        SkeletonFaceTracker skeletonFaceTracker;
                        if (this.trackedSkeletons.TryGetValue(skeleton.TrackingId, out skeletonFaceTracker))
                        {
                            skeletonFaceTracker.OnFrameReady(this.Kinect, colorImageFormat, colorImage, depthImageFormat, depthImage, skeleton);
                            skeletonFaceTracker.LastTrackedFrame = skeletonFrame.FrameNumber;
                        }
                    }
                }

                this.RemoveOldTrackers(skeletonFrame.FrameNumber);

                this.InvalidateVisual();
            }
            finally
            {
                if (colorImageFrame != null)
                {
                    colorImageFrame.Dispose();
                }

                if (depthImageFrame != null)
                {
                    depthImageFrame.Dispose();
                }

                if (skeletonFrame != null)
                {
                    skeletonFrame.Dispose();
                }
            }
        }

        private void OnSensorChanged(KinectSensor oldSensor, KinectSensor newSensor)
        {
            if (oldSensor != null)
            {
                oldSensor.AllFramesReady -= this.OnAllFramesReady;
                this.ResetFaceTracking();
            }

            if (newSensor != null)
            {
                newSensor.AllFramesReady += this.OnAllFramesReady;
            }
        }

        /// <summary>
        /// Clear out any trackers for skeletons we haven't heard from for a while
        /// </summary>
        private void RemoveOldTrackers(int currentFrameNumber)
        {
            var trackersToRemove = new List<int>();

            foreach (var tracker in this.trackedSkeletons)
            {
                uint missedFrames = (uint)currentFrameNumber - (uint)tracker.Value.LastTrackedFrame;
                if (missedFrames > MaxMissedFrames)
                {
                    // There have been too many frames since we last saw this skeleton
                    trackersToRemove.Add(tracker.Key);
                }
            }

            foreach (int trackingId in trackersToRemove)
            {
                this.RemoveTracker(trackingId);
            }
        }

        private void RemoveTracker(int trackingId)
        {
            this.trackedSkeletons[trackingId].Dispose();
            this.trackedSkeletons.Remove(trackingId);
        }

        private void ResetFaceTracking()
        {
            foreach (int trackingId in new List<int>(this.trackedSkeletons.Keys))
            {
                this.RemoveTracker(trackingId);
            }
        }

        private class SkeletonFaceTracker : IDisposable
        {
            private static FaceTriangle[] faceTriangles;

            private EnumIndexableCollection<FeaturePoint, PointF> facePoints;

            private FaceTracker faceTracker;

            private bool lastFaceTrackSucceeded;

            private SkeletonTrackingState skeletonTrackingState;
            private EnumIndexableCollection<FeaturePoint, Vector3DF> test;

            public int LastTrackedFrame { get; set; }

            public void Dispose()
            {
                if (this.faceTracker != null)
                {
                    this.faceTracker.Dispose();
                    this.faceTracker = null;
                }
            }

            public void DrawFaceModel(DrawingContext drawingContext)
            {
                if (!this.lastFaceTrackSucceeded || this.skeletonTrackingState != SkeletonTrackingState.Tracked)
                {
                    return;
                }

                double x, y;
                ////czubek głowy -0, 
                //double x = this.facePoints[0].X;
                //double y = this.facePoints[0].Y;
                //curNoseZ = this.test[58].Z;
                ////drawingContext.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 1.0), new Point(x, y), 8, 8);

                ////IDEKS 4 brwi
                //x = this.facePoints[4].X;
                //y = this.facePoints[4].Y;
                //drawingContext.DrawEllipse(Brushes.Yellow, new Pen(Brushes.Yellow, 1.0), new Point(x,y), 8,8);

                ////IDEKS 35 centrum czola
                //x = this.facePoints[35].X;
                //y = this.facePoints[35].Y;
                //drawingContext.DrawEllipse(Brushes.ForestGreen, new Pen(Brushes.ForestGreen, 1.0), new Point(x, y), 8, 8);

                ////IDEKS 40 dolna warga
                //x = this.facePoints[40].X;
                //y = this.facePoints[40].Y;
                //drawingContext.DrawEllipse(Brushes.White, new Pen(Brushes.White, 1.0), new Point(x, y), 8, 8);

                ////IDEKS 10 broda
                x = this.facePoints[10].X;
                y = this.facePoints[10].Y;
                chinZ = this.test[10].Z;
                //drawingContext.DrawEllipse(Brushes.Magenta, new Pen(Brushes.Magenta, 1.0), new Point(x, y), 8, 8);

                ////??? nose
                //x = this.facePoints[25].X;
                //y = this.facePoints[25].Y;
                //drawingContext.DrawEllipse(Brushes.Blue, new Pen(Brushes.Blue, 1.0), new Point(x, y), 8, 8);
                
                var faceModelPts = new List<Point>();
                var faceModel = new List<FaceModelTriangle>();

                for (int i = 0; i < this.facePoints.Count; i++)
                {
                    faceModelPts.Add(new Point(this.facePoints[i].X + 0.5f, this.facePoints[i].Y + 0.5f));
                }

                foreach (var t in faceTriangles)
                {
                    var triangle = new FaceModelTriangle();
                    triangle.P1 = faceModelPts[t.First];
                    triangle.P2 = faceModelPts[t.Second];
                    triangle.P3 = faceModelPts[t.Third];
                    faceModel.Add(triangle);
                }

                var faceModelGroup = new GeometryGroup();
                for (int i = 0; i < faceModel.Count; i++)
                {
                    var faceTriangle = new GeometryGroup();
                    faceTriangle.Children.Add(new LineGeometry(faceModel[i].P1, faceModel[i].P2));
                    faceTriangle.Children.Add(new LineGeometry(faceModel[i].P2, faceModel[i].P3));
                    faceTriangle.Children.Add(new LineGeometry(faceModel[i].P3, faceModel[i].P1));
                    faceModelGroup.Children.Add(faceTriangle);
                }

                drawingContext.DrawGeometry(Brushes.LightYellow, new Pen(Brushes.LightYellow, 1.0), faceModelGroup);
            }

            /// <summary>
            /// Updates the face tracking information for this skeleton
            /// </summary>
            internal void OnFrameReady(KinectSensor kinectSensor, ColorImageFormat colorImageFormat, byte[] colorImage, DepthImageFormat depthImageFormat, short[] depthImage, Skeleton skeletonOfInterest)
            {
                this.skeletonTrackingState = skeletonOfInterest.TrackingState;

                if (this.skeletonTrackingState != SkeletonTrackingState.Tracked)
                {
                    // nothing to do with an untracked skeleton.
                    return;
                }


                if (this.faceTracker == null)
                {
                    try
                    {
                        this.faceTracker = new FaceTracker(kinectSensor);
                    }
                    catch (InvalidOperationException)
                    {
                        // During some shutdown scenarios the FaceTracker
                        // is unable to be instantiated.  Catch that exception
                        // and don't track a face.
                        Debug.WriteLine("AllFramesReady - creating a new FaceTracker threw an InvalidOperationException");
                        this.faceTracker = null;
                    }
                }

                if (this.faceTracker != null)
                {
                    
                    FaceTrackFrame frame = this.faceTracker.Track(
                        colorImageFormat, colorImage, depthImageFormat, depthImage, skeletonOfInterest);

                    this.lastFaceTrackSucceeded = frame.TrackSuccessful;
                    if (this.lastFaceTrackSucceeded)
                    {
                        if (faceTriangles == null)
                        {
                            // only need to get this once.  It doesn't change.
                            faceTriangles = frame.GetTriangles();
                        }

                        this.facePoints = frame.GetProjected3DShape();
                        this.test = frame.Get3DShape();

                        //info about rotations
                        pitchVal = frame.Rotation.X;
                        rollVal = frame.Rotation.Z;
                        yawVal = frame.Rotation.Y;

                        

                    }
                }
            }

            private struct FaceModelTriangle
            {
                public Point P1;
                public Point P2;
                public Point P3;
            }
        }
    }
}