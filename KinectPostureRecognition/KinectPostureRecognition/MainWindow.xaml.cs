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
    public partial class MainWindow : Window
    {
        #region Member Variables
        private KinectSensor kinect;
        private WriteableBitmap colorImageBitmap;
        private Int32Rect colorImageBitmapRect;
        private int colorImageStride;

        private short[] depthData;
        private WriteableBitmap depthImageBitmap;
        private Int32Rect depthImageBitmapRect;
        private int depthImageStride;
        private DepthImageFrame lastDepthFrame;

        private WriteableBitmap enhancedDepthImageBitmap;
        private Int32Rect enhancedDepthImageBitmapRect;
        private int enhancedDepthImageStride;

        #endregion Member Variables


        #region Constructor
        public MainWindow()
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

            if (sensor != null)
            {
                ColorImageStream colorStream = sensor.ColorStream;
                colorStream.Enable();

                this.colorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                                                            96, 96, PixelFormats.Bgr32, null);
                this.colorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                this.colorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                ColorImageElement.Source = this.colorImageBitmap;
                sensor.ColorFrameReady += kinect_ColorFrameReady;

                //depth
                DepthImageStream depthStream = sensor.DepthStream;
                sensor.DepthStream.Enable();

                this.depthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight,
                                                           96, 96, PixelFormats.Gray16, null);
                this.depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                this.depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                DepthImageElement.Source = this.depthImageBitmap;

                this.enhancedDepthImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                                                          96, 96, PixelFormats.Bgr32, null);
                this.enhancedDepthImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                this.enhancedDepthImageStride = colorImageStride;
                EnhancedDepthImageElement.Source = this.enhancedDepthImageBitmap;

                sensor.DepthFrameReady += kinect_DepthFrameReady;


                //skeleton
                sensor.SkeletonStream.Enable();
                sensor.Start();
            }
        }

        void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (this.lastDepthFrame == null)
            {
                this.lastDepthFrame = e.OpenDepthImageFrame();
                depthData = this.lastDepthFrame != null ? new short[lastDepthFrame.PixelDataLength] : depthData;
            }
            else
            {
                this.lastDepthFrame.Dispose();
                this.lastDepthFrame = null;
                this.lastDepthFrame = e.OpenDepthImageFrame();
            }
            if (this.lastDepthFrame != null)
            {
                this.lastDepthFrame.CopyPixelDataTo(this.depthData);
                this.depthImageBitmap.WritePixels(this.depthImageBitmapRect, depthData, this.depthImageStride, 0);

            }
           
                CreateBetterShadesOfGray(lastDepthFrame, depthData);
            
            //using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            //{
            //    if (depthFrame != null)
            //    {
            //        depthData = new short[depthFrame.PixelDataLength];
            //        depthFrame.CopyPixelDataTo(depthData);

            //        this.depthImageBitmap.WritePixels(this.depthImageBitmapRect, depthData, this.depthImageStride, 0);


            //    }
            //}
        }



        void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    byte[] pixelData = new byte[colorFrame.PixelDataLength];
                    colorFrame.CopyPixelDataTo(pixelData);

                    this.colorImageBitmap.WritePixels(this.colorImageBitmapRect, pixelData, this.colorImageStride, 0);
                }
            }
        }

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.ColorFrameReady -= kinect_ColorFrameReady;
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

        private void TakePictureButton_Click(object sender, RoutedEventArgs e)
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
                    BitmapSource imageToSave = (BitmapSource)ColorImageElement.Source;

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
                    BitmapSource imageToSave = (BitmapSource)DepthImageElement.Source;

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

        #region Event Handlers
        private void DepthImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(this.DepthImageElement);
            if (this.depthData != null && this.depthData.Length > 0)
            {
                int pixelIndex = (int)(p.X + ((int)p.Y * this.lastDepthFrame.Width));
                int depth = this.depthData[pixelIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                PixelDepthTextBlock.Text = string.Format("{0} mm", depth);
            }

        }

        #endregion Event Handlers


        public void CreateBetterShadesOfGray(DepthImageFrame depthFrame, short[] data)
        {
            if (depthFrame != null)
            {

                //commented the code to make better gray shade picutre, leaving code which colors the plauyer
                //    int depth;
                //    int gray;

                //    int lowThreshold = 1000;
                //    int highThreshold = 3880;

                //    int bytesPerPixel = 4;


                //    byte[] enhancedDepthData = new byte[depthFrame.Width * depthFrame.Height * bytesPerPixel];

                //    for (int i = 0, j = 0; i < data.Length; i++, j += bytesPerPixel)
                //    {
                //        depth = data[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                //        if (depth < lowThreshold || depth > highThreshold)
                //        {
                //            gray = 0xFF;
                //        }
                //        else
                //        {
                //            gray = (255 * depth / 0xFFF);
                //        }
                //        enhancedDepthData[j] = (byte)gray;
                //        enhancedDepthData[j + 1] = (byte)gray;

                //        enhancedDepthData[j + 2] = (byte)gray;

                //    }

                //    EnhancedDepthImageElement.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96,
                //            PixelFormats.Bgr32, null, enhancedDepthData, depthFrame.Width * bytesPerPixel);
                //

                int playerIndex;
                int bytesPerPixel = 4;
                byte[] enhancedPixelData = new byte[depthFrame.Height * this.enhancedDepthImageStride];
                for (int i = 0, j = 0; i < data.Length; i++, j += bytesPerPixel) {
                    playerIndex = data[i] & DepthImageFrame.PlayerIndexBitmaskWidth;

                    if (playerIndex == 0)
                    {
                        enhancedPixelData[j] = 0xFF;
                        enhancedPixelData[j + 1] = 0xAF;
                        enhancedPixelData[j + 2] = 0xFA;

                    }
                    else {
                        enhancedPixelData[j] = 0x00;
                        enhancedPixelData[j + 1] = 0x00;
                        enhancedPixelData[j + 2] = 0x00;


                    }
                }
                this.enhancedDepthImageBitmap.WritePixels(this.enhancedDepthImageBitmapRect, enhancedPixelData, this.enhancedDepthImageStride, 0);


            }
        }

     
        
    }

}
