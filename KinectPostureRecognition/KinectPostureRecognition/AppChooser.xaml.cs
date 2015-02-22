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

namespace KinectPostureRecognition
{
    /// <summary>
    /// Interaction logic for AppChooser.xaml
    /// </summary>
    public partial class AppChooser : Window
    {
        public AppChooser()
        {
            InitializeComponent();
        }

        public void sidewaysView_Click(object sender, EventArgs e) {
            SkeletonOverlapping mainWindow = new SkeletonOverlapping();
            mainWindow.Show();
            this.Close();

        }

        public void frontView_Click(object sender, EventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();

        }
    }
}
