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
    public partial class DepthLevel : Window
    {
        #region Member Variables
        private KinectSensor kinect;
        private WriteableBitmap depthImageBitmap;
        private Int32Rect depthImageBitmapRect;
        private int depthImageStride;

        private short[] depthData;
        private byte[] depthColorImage;

        #endregion Member Variables

        #region Constructor
        public DepthLevel()
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
                DepthImageStream depthStream = kinect.DepthStream;
                depthStream.Enable();

                this.depthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight,
                                                            96, 96, PixelFormats.Bgr32, null);
                this.depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                this.depthImageStride = depthStream.FrameWidth * 4;
                DepthLevelsImage.Source = this.depthImageBitmap;


                SkeletonStream skeletonStream = kinect.SkeletonStream;
                skeletonStream.Enable();

                sensor.DepthFrameReady += kinect_DepthFrameReady;
                sensor.Start();
            }
        }

      
        void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null) { return;  }
                if (depthData == null) {
                    depthData = new short[depthFrame.PixelDataLength];
                }
                if (depthColorImage == null) {
                    depthColorImage = new byte[depthFrame.PixelDataLength * 4];
                }

                depthFrame.CopyPixelDataTo(depthData);
                int depthColorImagePos = 0;
                for (int depthPos = 0; depthPos < depthFrame.PixelDataLength; depthPos++)
                {
                    int depthVal = depthData[depthPos] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                    if (depthVal == kinect.DepthStream.UnknownDepth)
                    {
                        depthColorImage[depthColorImagePos++] = 0;
                        depthColorImage[depthColorImagePos++] = 0;
                        depthColorImage[depthColorImagePos++] = 255; //red

                    }
                    else if (depthVal == kinect.DepthStream.TooFarDepth)
                    {
                        depthColorImage[depthColorImagePos++] = 255; //blue
                        depthColorImage[depthColorImagePos++] = 0;
                        depthColorImage[depthColorImagePos++] = 0;

                    }
                    else if (depthVal == kinect.DepthStream.TooNearDepth)
                    {
                        depthColorImage[depthColorImagePos++] = 0;
                        depthColorImage[depthColorImagePos++] = 255;
                        depthColorImage[depthColorImagePos++] = 0; //green

                    }
                    else
                    {
                        byte depthByte = (byte)(255 - (depthVal) >> 4);
                        depthColorImage[depthColorImagePos++] = depthByte;
                        depthColorImage[depthColorImagePos++] = depthByte;
                        depthColorImage[depthColorImagePos++] = depthByte;
                    }
                    //transparency 
                    depthColorImagePos++;
                }

                //if (depthImageBitmap == null) {
                //    this.depthImageBitmap = new WriteableBitmap(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null);
                //}
                //DepthLevelsImage.Source = depthImageBitmap;

                this.depthImageBitmap.WritePixels(new Int32Rect(0,0,depthFrame.Width, depthFrame.Height), depthColorImage, depthFrame.Width*4, 0);

            }
        }

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.DepthFrameReady -= kinect_DepthFrameReady;
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

        private void TakePictureDepthButton_Click(object sender, RoutedEventArgs e)
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
                    BitmapSource imageToSave = (BitmapSource)DepthLevelsImage.Source;

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
    }
}
