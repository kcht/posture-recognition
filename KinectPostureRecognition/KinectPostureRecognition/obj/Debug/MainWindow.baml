   M S B A M L   `   `   `  ����  KinectPostureRecognition)    #KinectPostureRecognition.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  
MainWindow��5   	   $
��Auto��6   $
��Auto��6*   .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5	      $
��Auto �6    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   �� -   5         ��Name$ ColorImageElement��5      $��0��6+   $��0��68   $	��320��6I   $��Bottom=�6V   �� 5   
   -   5        $ DepthImageElement��5      $��0��6+   $��1��68   $	��320��6I   $��Bottom=�6V   5      �� 5   
   -   5        $ EnhancedDepthImageElement��5      $��0��63   $��2��6@   $	��320��6Q   $��Bottom=�6^   �� 5   
   $��Center=�6   $	��Top=�62   $��0��6J   .+�5      �� -   5        $��Take Picture��5      6,   5      �� 5   
   $��Center=�6   $	��Top=�62   $��1��6J   $��0��6Z   .+�5      �� -   5        $��Take PictureDepth��5      61   5      �� 5   
   $��1��6   $��1��6%   $��Stretch=�62   .+�5      �� $��ClickedDepth��6   $��30(�6+   �� 5      -   5         ��Name$ PixelDepthTextBlock��5      $��30(�66   5      5       5!      