using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace KinectPostureRecognition
{
    enum WavePosition { 
        None =0,
        Left =1,
        Right =2,
        Neutral =3
    }

    enum WaveGestureState { 
        None = 0,
        Success = 1,
        Failure = 2,
        InProgress =3
    }

    struct WaveGestureTracker
    {
        #region Fields
        public int iterationCount;
        public WaveGestureState state;
        public long timestamp;
        public WavePosition currentPosition, startPosition;
        #endregion Fields 

        #region Methods
        public void Reset() {
            iterationCount = 0;
            state = WaveGestureState.None;
            timestamp = 0;
            startPosition = WavePosition.None;
            currentPosition = WavePosition.None;
        }

        public void UpdateState(WaveGestureState state, long timestamp) {
            this.timestamp = timestamp;
            this.state = state;
        }

        public void UpdatePosition(WavePosition position, long timestamp) {
            if (currentPosition != position) {
                if (position == WavePosition.Left || position == WavePosition.Right) {
                    if (state != WaveGestureState.InProgress) {
                        state = WaveGestureState.InProgress;
                        iterationCount = 0;
                        startPosition = position;
                    }
                    iterationCount++;
                }
                this.currentPosition = position;
                this.timestamp = timestamp;
            }
        }
        #endregion Methods
    }
    class WaveGesture
    {
        #region Variables
        private const float WAVE_THRESHOLD = 0.1f;
        private const int WAVE_MOVEMENT_TIMEOUT = 5000;
        private const int REQUIRED_ITERATIONS = 4;

        private const int LEFT_HAND = 0;
        private const int RIGHT_HAND = 1; //todo: ???

        private WaveGestureTracker[,] playerWaveTracker = new WaveGestureTracker[6, 2];
        public event EventHandler GestureDetected;

        #endregion Variables

        #region Methods
        public void Update(Skeleton[] skeletons, long frameTimestamp) {
            if (skeletons != null) {
                Skeleton skeleton;
                for (int i = 0; i < skeletons.Length; i++) {
                    skeleton = skeletons[i];
                    if (skeleton.TrackingState != SkeletonTrackingState.NotTracked)
                    {
                        TrackWave(skeleton, true, ref this.playerWaveTracker[i, LEFT_HAND], frameTimestamp);
                        TrackWave(skeleton, false, ref this.playerWaveTracker[i, RIGHT_HAND], frameTimestamp);

                    }
                    else {
                        this.playerWaveTracker[i, LEFT_HAND].Reset();
                        this.playerWaveTracker[i, RIGHT_HAND].Reset();
                    }
                }
            }
        }

        private void TrackWave(Skeleton skeleton, bool isLeft, ref WaveGestureTracker tracker, long timestamp) {
            JointType handJointID = (isLeft) ? JointType.HandLeft : JointType.HandRight;
            JointType elbowJointID = (isLeft) ? JointType.ElbowLeft : JointType.ElbowRight;
            Joint hand = skeleton.Joints[handJointID];
            Joint elbow = skeleton.Joints[elbowJointID];

            if (hand.TrackingState != JointTrackingState.NotTracked && elbow.TrackingState != JointTrackingState.NotTracked)
            {
                if (tracker.state == WaveGestureState.InProgress && tracker.timestamp + WAVE_MOVEMENT_TIMEOUT < timestamp)
                {
                    tracker.UpdateState(WaveGestureState.Failure, timestamp);
                }
                else if (hand.Position.Y > elbow.Position.Y)
                {
                    if (hand.Position.X <= elbow.Position.X - WAVE_THRESHOLD)
                    {
                        tracker.UpdatePosition(WavePosition.Left, timestamp);
                    }
                    else if (hand.Position.X >= elbow.Position.X + WAVE_THRESHOLD)
                    {
                        tracker.UpdatePosition(WavePosition.Right, timestamp);
                    }
                    else
                    {
                        tracker.UpdatePosition(WavePosition.Neutral, timestamp);
                    }

                    if (tracker.state != WaveGestureState.Success && tracker.iterationCount == REQUIRED_ITERATIONS)
                    {
                        tracker.UpdateState(WaveGestureState.Success, timestamp);

                        if (GestureDetected != null)
                        {
                            GestureDetected(this, new EventArgs());
                        }
                    }
                }
                else
                {
                    if (tracker.state == WaveGestureState.InProgress)
                    {
                        tracker.UpdateState(WaveGestureState.Failure, timestamp);

                    }
                    else
                    {
                        tracker.Reset();
                    }
                }
            }
            else {
                tracker.Reset();
            }
        }



        #endregion Methods
    }
}
