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
using System.Windows.Shapes;

using Microsoft.Kinect;
using System.IO;
using System.ComponentModel;

namespace KinectPostureRecognition
{
    /// <summary>
    /// Interaction logic for MainWindowPolling.xaml
    /// </summary>
    public partial class MainWindowPolling : Window
    {
         #region Member Variables
        private KinectSensor kinect;
        private WriteableBitmap colorImageBitmap;
        private Int32Rect colorImageBitmapRect;
        private int colorImageStride;
        private byte[] colorImagePixelData;
        private BackgroundWorker bgworker;
        #endregion Member Variables

        #region Constructor
        public MainWindowPolling()
        {
            InitializeComponent();
            MainWindow mw = new MainWindow();
            mw.Show();

            this.bgworker = new BackgroundWorker();
            this.bgworker.DoWork += Worker_DoWork;
            this.bgworker.RunWorkerAsync();

            this.Unloaded += (s, e) => { this.bgworker.CancelAsync(); };
            
        }
       
        #endregion Constructor

        #region Methods

        private void Worker_DoWork(object sender, DoWorkEventArgs e) { 
           if(bgworker!=null){
               while (!bgworker.CancellationPending) {
                   DiscoverKinectSensor();
                   PollColorImageStream();
               }
           }
   
        }
        
         private void PollColorImageStream()
         {
             if (this.kinect == null)
             {
                 //display message to plug in kinect

             }
             else {
                 try
                 {
                     using (ColorImageFrame colorFrame = this.kinect.ColorStream.OpenNextFrame(30))
                     {
                         if (colorFrame != null)
                 
                             colorFrame.CopyPixelDataTo(this.colorImagePixelData);
                             this.ColorImageElementPolling.Dispatcher.BeginInvoke(new Action(() =>
                             {
                               this.colorImageBitmap.WritePixels(this.colorImageBitmapRect, this.colorImagePixelData, this.colorImageStride, 0);
                             }));
                         }
                     }
                 
                 catch (Exception ex) { 
                    //report the message
                 }
             }
         }

         private void DiscoverKinectSensor()
         {
             if (this.kinect != null && this.kinect.Status != KinectStatus.Connected) { 
                //if this sensor is no longer connected, we need to discover a new one:
                 this.kinect = null;
             }

             if (this.kinect == null) { 
                //find the firts connected sensor
                 this.kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);

                 if (this.kinect != null) { 
                    //initialize the sensor
                     this.kinect.ColorStream.Enable();
                     this.kinect.Start();

                     ColorImageStream colorStream = this.kinect.ColorStream;

                     this.ColorImageElementPolling.Dispatcher.BeginInvoke(new Action(() =>
                     {

                         this.colorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                                                                  96, 96, PixelFormats.Bgr32, null);
                         this.colorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                         this.colorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                         this.ColorImageElementPolling.Source = this.colorImageBitmap;
                         this.colorImagePixelData = new byte[colorStream.FramePixelDataLength];
                     }));
                 }
             }
         }
      
        
        #endregion Methods

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
                     BitmapSource imageToSave = (BitmapSource)ColorImageElementPolling.Source;

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
