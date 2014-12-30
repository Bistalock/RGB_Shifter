using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
/* ---------------------- Added Libraries ---------------------- */
using BitMiracle.LibTiff.Classic; // Use Tiff images
using System.Collections.Specialized; // String Collection
using System.IO; // Memory Stream
using System.Runtime.InteropServices; // DLL support


namespace RGB_Shifter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Importing the C++ MirrorImage dynamic link library with a P/Invoke call
        [DllImport("Mirror Image.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern IntPtr mirrorImage([In, Out] byte[] inputImageBuffer, int height, int width, int samples, int kernelHeight, int kernelWidth);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBox_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Overrides default behavior
            e.Handled = true;
        }

        private void TextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            // Get data object
            var dataObject = e.Data as DataObject;

            // Check for file list
            if (dataObject.ContainsFileDropList())
            {
                // Clear values
                ((TextBox)sender).Text = string.Empty;

                // Process file names
                StringCollection fileNames = dataObject.GetFileDropList();
                StringBuilder bd = new StringBuilder();
                foreach (var fileName in fileNames)
                {
                    bd.Append(fileName);
                }

                // Set text
                ((TextBox)sender).Text = bd.ToString();
            }
        }

        // Closes all preview windows when main window is closed
        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        private void loadImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.InitialDirectory = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"Resources");
            dlg.DefaultExt = ".tif";
            dlg.Filter = "TIFF Image (*.tif;*.tiff)|*.tif;.tiff|All files (*.*)|*.*";

            // Assigns the results value when Dialog is opened
            var result = dlg.ShowDialog();

            // Checks if value is true
            if (result == true)
            {
                textBox.Text = dlg.FileName;
            }
        }

        private async void previewButton_Click(object sender, RoutedEventArgs e)
        {
            #region Initiallization
            if (string.IsNullOrEmpty(textBox.Text))
            {
                MessageBoxResult result = MessageBox.Show("Please input the TIFF image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }

            Tiff image = Tiff.Open(textBox.Text, "r");

            // Obtain basic tag information of the image
            int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            byte bits = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToByte();
            byte samples = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToByte();

            // store the image information in 2d byte array
            // reserve memory for storing the size of 1 line
            byte[] scanline = new byte[image.ScanlineSize()];
            // reserve memory for the size of image

            // creating the 1 dimentional buffer containing information for ALL 3 DIMENTIONS
            byte[] inputImageBuffer = new byte[samples * height * width];

            // loop gathering the values from a single scanline at a time
            for (int i = 0; i < height; i++)
            {
                image.ReadScanline(scanline, i); // read the scanline for each column
                for (int j = 0; j < samples * width; j++)
                {
                    // writing the entire image as a 1 dimentional integer buffer.
                    inputImageBuffer[image.ScanlineSize() * i + j] = scanline[j];
                }
            } // end grabbing intensity values              

            if (textBox1.Text == "" && shiftComboBox.SelectedIndex != 1)
            {
                // Error Windows when no number of samples entered
                MessageBoxResult result = MessageBox.Show("No vertical shift size selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }

            int shiftHeight = 0;

            // the kernel window to be used
            if (shiftComboBox.SelectedIndex == 1)
            {
                shiftHeight = 0;
            }
            else
            {
                shiftHeight = Convert.ToInt32(textBox1.Text) * 3;
            }


            if (textBox2.Text == "" && shiftComboBox.SelectedIndex != 0)
            {
                // Error Windows when no number of samples entered
                MessageBoxResult result = MessageBox.Show("No horizontal shift size selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }

            int shiftWidth = 0;

            if (shiftComboBox.SelectedIndex == 0)
            {
                shiftWidth = 0;
            }
            else
            {
                shiftWidth = Convert.ToInt32(textBox2.Text) * 3;
            }

            int offsetHeight = (shiftHeight - 3) / 2 + 1; // calculation of the center 
            int offsetWidth = (shiftWidth - 3) / 2 + 1; // calculation of the center  

            int redShiftWidth = 0;
            int redShiftHeight = 0;

            int greenShiftWidth = 0;
            int greenShiftHeight = 0;

            int blueShiftWidth = 0;
            int blueShiftHeight = 0;

            // Vertical Shift
            if (shiftComboBox.SelectedIndex == 0)
            {
                // Red, Green, Blue * ----- Working! ------- *
                if (offsetComboBox.SelectedIndex == 0)
                {
                    redShiftHeight = offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = -offsetHeight;
                }
                // Red, Blue, Green
                else if (offsetComboBox.SelectedIndex == 1)
                {
                    redShiftHeight = offsetHeight;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = 0;
                }
                // Green, Red, Blue
                else if (offsetComboBox.SelectedIndex == 2)
                {
                    redShiftHeight = 0;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = -offsetHeight;
                }
                // Green, Blue, Red
                else if (offsetComboBox.SelectedIndex == 3)
                {
                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = 0;
                }
                // Blue, Green, Red * ----- Working! ------- *
                else if (offsetComboBox.SelectedIndex == 4)
                {
                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = offsetHeight;
                }
                // Blue, Red, Green
                else if (offsetComboBox.SelectedIndex == 5)
                {
                    redShiftHeight = 0;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = offsetHeight;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("No color order entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
            }
            // Horizontal Shift
            else if (shiftComboBox.SelectedIndex == 1)
            {
                // Red, Green, Blue * ----- Working! ------- *
                if (offsetComboBox.SelectedIndex == 0)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = -offsetWidth * samples;
                }
                // Red, Blue, Green
                else if (offsetComboBox.SelectedIndex == 1)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = 0;
                }
                // Green, Red, Blue
                else if (offsetComboBox.SelectedIndex == 2)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = -offsetWidth * samples;
                }
                // Green, Blue, Red
                else if (offsetComboBox.SelectedIndex == 3)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = 0;
                }
                // Blue, Green, Red * ----- Working! ------- *
                else if (offsetComboBox.SelectedIndex == 4)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = offsetWidth * samples;
                }
                // Blue, Red, Green
                else if (offsetComboBox.SelectedIndex == 5)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = offsetWidth * samples;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("No color order entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
            }
            // Uphill Shift
            else if (shiftComboBox.SelectedIndex == 2)
            {
                // Red, Green, Blue * ----- Working! ------- *
                if (offsetComboBox.SelectedIndex == 0)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = -offsetWidth * samples;

                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = offsetHeight;
                }
                // Red, Blue, Green
                else if (offsetComboBox.SelectedIndex == 1)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = 0;

                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = 0;
                }
                // Green, Red, Blue
                else if (offsetComboBox.SelectedIndex == 2)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = -offsetWidth * samples;

                    redShiftHeight = 0;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = offsetHeight;
                }
                // Green, Blue, Red
                else if (offsetComboBox.SelectedIndex == 3)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = 0;

                    redShiftHeight = offsetHeight;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = 0;
                }
                // Blue, Green, Red * ----- Working! ------- *
                else if (offsetComboBox.SelectedIndex == 4)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = offsetWidth * samples;

                    redShiftHeight = offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = -offsetHeight;
                }
                // Blue, Red, Green
                else if (offsetComboBox.SelectedIndex == 5)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = offsetWidth * samples;

                    redShiftHeight = 0;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = -offsetHeight;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("No color order entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
            }
            // Downhill Shift
            else if (shiftComboBox.SelectedIndex == 3)
            {
                // Red, Green, Blue
                if (offsetComboBox.SelectedIndex == 0)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = -offsetWidth * samples;

                    redShiftHeight = offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = -offsetHeight;
                }
                // Red, Blue, Green
                else if (offsetComboBox.SelectedIndex == 1)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = 0;

                    redShiftHeight = offsetHeight;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = 0;
                }
                // Green, Red, Blue
                else if (offsetComboBox.SelectedIndex == 2)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = -offsetWidth * samples;

                    redShiftHeight = 0;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = -offsetHeight;
                }
                // Green, Blue, Red
                else if (offsetComboBox.SelectedIndex == 3)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = 0;

                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = 0;
                }
                // Blue, Green, Red
                else if (offsetComboBox.SelectedIndex == 4)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = offsetWidth * samples;

                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = offsetHeight;
                }
                // Blue, Red, Green
                else if (offsetComboBox.SelectedIndex == 5)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = offsetWidth * samples;

                    redShiftHeight = 0;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = offsetHeight;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("No color order entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("No shift orientation entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }

            if (samples == 1)
            {
                MessageBoxResult result = MessageBox.Show("The image entered is grayscale. This will cause the image entered to shift to the orientation provided.\n\nDo you wish to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            else if (samples == 4)
            {
                MessageBoxResult result = MessageBox.Show("This program does not support alpha channels.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }
            #endregion

            #region Processing

            Title = "RGB Shifter (Working)";
            previewButton.IsEnabled = false;
            processImageButton.IsEnabled = false;           

            // Mirrorimage before the creation of the kernelTh
            int MirroredHeight = height + (offsetHeight * 2);
            int MirroredWidth = width + (offsetWidth * 2);

            // initiallizing the 1 dimentional resulting image
            byte[] mirrorImageBuffer = new byte[samples * MirroredHeight * MirroredWidth];

            // initiallizing the 1 dimentional resulting image
            byte[] processedImageBuffer = new byte[samples * width * height];

            // Calling the native mirrorImage C++ function via a P/Invoke in C#
            IntPtr unmanagedMirrorBuffer = await Task.Run(() => mirrorImage(inputImageBuffer, height, width, samples, shiftHeight, shiftWidth));

            // Marshaling the resulting interger pointer unmanaged array into a managed 1 dimentional array.
            Marshal.Copy(unmanagedMirrorBuffer, mirrorImageBuffer, 0, samples * MirroredWidth * MirroredHeight);
            #endregion

            #region Preview Image


            //int colorshiftWidth = hShift * offsetWidth * samples;
            //int colorshiftHeight = vShift * offsetHeight;

            // loop is [height,width,samples] because of how Tiff scanlines work
            for (int i = offsetHeight; i < MirroredHeight - offsetHeight; i++)
            {
                for (int j = offsetWidth; j < MirroredWidth - offsetWidth; j++)
                {

                    for (int k = 0; k < samples; k++)
                    {     
                        if (k == 0)
                        {
                            processedImageBuffer[((samples * width) * (i - offsetHeight)) + (samples * (j - offsetWidth)) + k] = mirrorImageBuffer[((samples * MirroredWidth) * (i + redShiftHeight)) + (samples * j) + k + redShiftWidth]; // saving the resulting image to file
                        }
                        if (k == 1)
                        {
                            processedImageBuffer[((samples * width) * (i - offsetHeight)) + (samples * (j - offsetWidth)) + k] = mirrorImageBuffer[((samples * MirroredWidth) * (i + greenShiftHeight)) + (samples * j) + k + greenShiftWidth]; // saving the resulting image to file                        
                        }
                        if (k == 2)
                        {
                            processedImageBuffer[((samples * width) * (i - offsetHeight)) + (samples * (j - offsetWidth)) + k] = mirrorImageBuffer[((samples * MirroredWidth) * (i + blueShiftHeight)) + (samples * j) + k + blueShiftWidth]; // saving the resulting image to file
                        }
                    }
                }
            }
            PixelFormat pixelFormat;

            if (samples == 1) pixelFormat = PixelFormats.Gray8;
            else if (samples == 3) pixelFormat = PixelFormats.Rgb24;
            else
            {
                pixelFormat = PixelFormats.Rgba64;
                MessageBoxResult result = MessageBox.Show("This program does not support alpha channels.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                previewButton.IsEnabled = true;
                processImageButton.IsEnabled = true;
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }

            var bytesPerPixel = (pixelFormat.BitsPerPixel + 7) / 8;

            var bitmap = BitmapImage.Create(width, height, 96, 96, pixelFormat, null, processedImageBuffer, samples * width);

            var Preview = new Preview(bitmap);
            Preview.Show();

            Title = "RGB Shifter";
            previewButton.IsEnabled = true;
            processImageButton.IsEnabled = true;
            #endregion
        }

        private async void processImageButton_Click(object sender, RoutedEventArgs e)
        {
            #region Initiallization
            if (string.IsNullOrEmpty(textBox.Text))
            {
                MessageBoxResult result = MessageBox.Show("Please input the TIFF image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }

            Tiff image = Tiff.Open(textBox.Text, "r");

            // Obtain basic tag information of the image
            int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            byte bits = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToByte();
            byte samples = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToByte();

            // store the image information in 2d byte array
            // reserve memory for storing the size of 1 line
            byte[] scanline = new byte[image.ScanlineSize()];
            // reserve memory for the size of image

            // creating the 1 dimentional buffer containing information for ALL 3 DIMENTIONS
            byte[] inputImageBuffer = new byte[samples * height * width];

            // loop gathering the values from a single scanline at a time
            for (int i = 0; i < height; i++)
            {
                image.ReadScanline(scanline, i); // read the scanline for each column
                for (int j = 0; j < samples * width; j++)
                {
                    // writing the entire image as a 1 dimentional integer buffer.
                    inputImageBuffer[image.ScanlineSize() * i + j] = scanline[j];
                }
            } // end grabbing intensity values              

            if (textBox1.Text == "" && shiftComboBox.SelectedIndex != 1)
            {
                // Error Windows when no number of samples entered
                MessageBoxResult result = MessageBox.Show("No vertical shift size selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }

            int shiftHeight = 0;

            // the kernel window to be used
            if (shiftComboBox.SelectedIndex == 1)
            {
                shiftHeight = 0;
            }
            else
            {
                shiftHeight = Convert.ToInt32(textBox1.Text) * 3;
            }


            if (textBox2.Text == "" && shiftComboBox.SelectedIndex != 0)
            {
                // Error Windows when no number of samples entered
                MessageBoxResult result = MessageBox.Show("No horizontal shift size selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }

            int shiftWidth = 0;

            if (shiftComboBox.SelectedIndex == 0)
            {
                shiftWidth = 0;
            }
            else
            {
                shiftWidth = Convert.ToInt32(textBox2.Text) * 3;
            }

            int offsetHeight = (shiftHeight - 3) / 2 + 1; // calculation of the center 
            int offsetWidth = (shiftWidth - 3) / 2 + 1; // calculation of the center  

            int redShiftWidth = 0;
            int redShiftHeight = 0;

            int greenShiftWidth = 0;
            int greenShiftHeight = 0;

            int blueShiftWidth = 0;
            int blueShiftHeight = 0;

            // Vertical Shift
            if (shiftComboBox.SelectedIndex == 0)
            {
                // Red, Green, Blue
                if (offsetComboBox.SelectedIndex == 0)
                {
                    redShiftHeight = offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = -offsetHeight;
                }
                // Red, Blue, Green
                else if (offsetComboBox.SelectedIndex == 1)
                {
                    redShiftHeight = offsetHeight;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = 0;
                }
                // Green, Red, Blue
                else if (offsetComboBox.SelectedIndex == 2)
                {
                    redShiftHeight = 0;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = -offsetHeight;
                }
                // Green, Blue, Red
                else if (offsetComboBox.SelectedIndex == 3)
                {
                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = 0;
                }
                // Blue, Green, Red
                else if (offsetComboBox.SelectedIndex == 4)
                {
                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = offsetHeight;
                }
                // Blue, Red, Green
                else if (offsetComboBox.SelectedIndex == 5)
                {
                    redShiftHeight = 0;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = offsetHeight;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("No color order entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
            }
            // Horizontal Shift
            else if (shiftComboBox.SelectedIndex == 1)
            {
                // Red, Green, Blue
                if (offsetComboBox.SelectedIndex == 0)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = -offsetWidth * samples;
                }
                // Red, Blue, Green
                else if (offsetComboBox.SelectedIndex == 1)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = 0;
                }
                // Green, Red, Blue
                else if (offsetComboBox.SelectedIndex == 2)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = -offsetWidth * samples;
                }
                // Green, Blue, Red
                else if (offsetComboBox.SelectedIndex == 3)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = 0;
                }
                // Blue, Green, Red
                else if (offsetComboBox.SelectedIndex == 4)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = offsetWidth * samples;
                }
                // Blue, Red, Green
                else if (offsetComboBox.SelectedIndex == 5)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = offsetWidth * samples;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("No color order entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
            }
            // Uphill Shift
            else if (shiftComboBox.SelectedIndex == 2)
            {
                // Red, Green, Blue
                if (offsetComboBox.SelectedIndex == 0)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = -offsetWidth * samples;

                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = offsetHeight;
                }
                // Red, Blue, Green
                else if (offsetComboBox.SelectedIndex == 1)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = 0;

                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = 0;
                }
                // Green, Red, Blue
                else if (offsetComboBox.SelectedIndex == 2)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = -offsetWidth * samples;

                    redShiftHeight = 0;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = offsetHeight;
                }
                // Green, Blue, Red
                else if (offsetComboBox.SelectedIndex == 3)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = 0;

                    redShiftHeight = offsetHeight;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = 0;
                }
                // Blue, Green, Red
                else if (offsetComboBox.SelectedIndex == 4)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = offsetWidth * samples;

                    redShiftHeight = offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = -offsetHeight;
                }
                // Blue, Red, Green
                else if (offsetComboBox.SelectedIndex == 5)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = offsetWidth * samples;

                    redShiftHeight = 0;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = -offsetHeight;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("No color order entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
            }
            // Downhill Shift
            else if (shiftComboBox.SelectedIndex == 3)
            {
                // Red, Green, Blue
                if (offsetComboBox.SelectedIndex == 0)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = -offsetWidth * samples;

                    redShiftHeight = offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = -offsetHeight;
                }
                // Red, Blue, Green
                else if (offsetComboBox.SelectedIndex == 1)
                {
                    redShiftWidth = offsetWidth * samples;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = 0;

                    redShiftHeight = offsetHeight;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = 0;
                }
                // Green, Red, Blue
                else if (offsetComboBox.SelectedIndex == 2)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = -offsetWidth * samples;

                    redShiftHeight = 0;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = -offsetHeight;
                }
                // Green, Blue, Red
                else if (offsetComboBox.SelectedIndex == 3)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = offsetWidth * samples;
                    blueShiftWidth = 0;

                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = offsetHeight;
                    blueShiftHeight = 0;
                }
                // Blue, Green, Red
                else if (offsetComboBox.SelectedIndex == 4)
                {
                    redShiftWidth = -offsetWidth * samples;
                    greenShiftWidth = 0;
                    blueShiftWidth = offsetWidth * samples;

                    redShiftHeight = -offsetHeight;
                    greenShiftHeight = 0;
                    blueShiftHeight = offsetHeight;
                }
                // Blue, Red, Green
                else if (offsetComboBox.SelectedIndex == 5)
                {
                    redShiftWidth = 0;
                    greenShiftWidth = -offsetWidth * samples;
                    blueShiftWidth = offsetWidth * samples;

                    redShiftHeight = 0;
                    greenShiftHeight = -offsetHeight;
                    blueShiftHeight = offsetHeight;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("No color order entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("No shift orientation entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }

            if (samples == 1)
            {
                MessageBoxResult result = MessageBox.Show("The image entered is grayscale. This will cause the image entered to shift to the orientation provided.\n\nDo you wish to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            else if (samples == 4)
            {
                MessageBoxResult result = MessageBox.Show("This program does not support alpha channels.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    return;
                }
            }
            #endregion

            #region Processing

            Title = "RGB Shifter (Working)";
            previewButton.IsEnabled = false;
            processImageButton.IsEnabled = false;

            // Mirrorimage before the creation of the kernelTh
            int MirroredHeight = height + (offsetHeight * 2);
            int MirroredWidth = width + (offsetWidth * 2);

            // initiallizing the 1 dimentional resulting image
            byte[] mirrorImageBuffer = new byte[samples * MirroredHeight * MirroredWidth];

            // initiallizing the 1 dimentional resulting image
            byte[] processedImageBuffer = new byte[samples * width * height];

            // Calling the native mirrorImage C++ function via a P/Invoke in C#
            IntPtr unmanagedMirrorBuffer = await Task.Run(() => mirrorImage(inputImageBuffer, height, width, samples, shiftHeight, shiftWidth));

            // Marshaling the resulting interger pointer unmanaged array into a managed 1 dimentional array.
            Marshal.Copy(unmanagedMirrorBuffer, mirrorImageBuffer, 0, samples * MirroredWidth * MirroredHeight);
            #endregion

            #region Save image
            string fileName = System.IO.Path.GetFileNameWithoutExtension(textBox.Text) + "_Processed" + ".tif";

            // Create OpenFileDialog 
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            // Set the dialog box variables
            dlg.DefaultExt = ".tif";
            dlg.Filter = "TIFF Image (*.tif;*.tiff)|*.tif;.tiff|All files (*.*)|*.*";
            dlg.FileName = fileName;
            // Assigns the results value when Dialog is opened
            var dlgresult = dlg.ShowDialog();

            // Checks if value is true
            if (dlgresult == true)
            {
                // Recreation of the image from 3d byte array image
                using (Tiff output = Tiff.Open(dlg.FileName, "w"))
                {
                    #region SetTagInfo
                    // set tag information
                    output.SetField(TiffTag.IMAGEWIDTH, width);
                    output.SetField(TiffTag.IMAGELENGTH, height);
                    output.SetField(TiffTag.BITSPERSAMPLE, bits);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, samples);
                    output.SetField(TiffTag.ORIENTATION, BitMiracle.LibTiff.Classic.Orientation.TOPLEFT);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                    output.SetField(TiffTag.ROWSPERSTRIP, 1);
                    output.SetField(TiffTag.XRESOLUTION, 96); //dpiX);
                    output.SetField(TiffTag.YRESOLUTION, 96); //dpiY);
                    if (samples == 1) output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    else if (samples == 3) output.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                    output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);
                    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
                    output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                    #endregion

                    // reserve buffer
                    byte[] buffer = new byte[width * samples];
                    // obtain each line of the final byte arrays and write them to a file

                    // loop is [height,width,samples] because of how Tiff scanlines work
                    for (int i = offsetHeight; i < MirroredHeight - offsetHeight; i++)
                    {
                        for (int j = offsetWidth; j < MirroredWidth - offsetWidth; j++)
                        {

                            for (int k = 0; k < samples; k++)
                            {
                                if (k == 0)
                                {
                                    buffer[samples * (j - offsetWidth) + k] = mirrorImageBuffer[((samples * MirroredWidth) * (i + redShiftHeight)) + (samples * j) + k + redShiftWidth]; // saving the resulting image to file
                                }
                                if (k == 1)
                                {
                                    buffer[samples * (j - offsetWidth) + k] = mirrorImageBuffer[((samples * MirroredWidth) * (i + greenShiftHeight)) + (samples * j) + k + greenShiftWidth]; // saving the resulting image to file                        
                                }
                                if (k == 2)
                                {
                                    buffer[samples * (j - offsetWidth) + k] = mirrorImageBuffer[((samples * MirroredWidth) * (i + blueShiftHeight)) + (samples * j) + k + blueShiftWidth]; // saving the resulting image to file
                                }
                            }
                        }
                        // write
                        if (samples == 1) output.WriteScanline(buffer, i - offsetHeight);
                        else if (samples == 3) output.WriteEncodedStrip(i - offsetHeight, buffer, samples * width);
                    }
                    // write to file
                    output.WriteDirectory();
                    output.Dispose();

                    //System.Diagnostics.Process.Start(dlg.FileName); // displays the result
                }// end inner using
            }

            Title = "RGB Shifter";
            previewButton.IsEnabled = true;
            processImageButton.IsEnabled = true;
            #endregion
        }

        private void shiftComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Vertical Shift
            if (shiftComboBox.SelectedIndex == 0)
            {
                label1.IsEnabled = true;
                label2.IsEnabled = false;
                textBox1.IsEnabled = true;
                textBox2.IsEnabled = false;
            }
            // Horizontal Shift
            else if (shiftComboBox.SelectedIndex == 1)
            {
                label1.IsEnabled = false;
                label2.IsEnabled = true;
                textBox1.IsEnabled = false;
                textBox2.IsEnabled = true;
            }
            // Uphill Shift
            else if (shiftComboBox.SelectedIndex == 2)
            {
                label1.IsEnabled = true;
                label2.IsEnabled = true;
                textBox1.IsEnabled = true;
                textBox2.IsEnabled = true;
            }
            // Downhill Shift
            else if (shiftComboBox.SelectedIndex == 3)
            {
                label1.IsEnabled = true;
                label2.IsEnabled = true;
                textBox1.IsEnabled = true;
                textBox2.IsEnabled = true;
            }
        }
    }
}
