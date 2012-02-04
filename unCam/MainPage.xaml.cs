using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone.Controls;
using Microsoft.Devices;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;

namespace unCam
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Variables
        private int savedCounter = 0;
        PhotoCamera cam;
        MediaLibrary library = new MediaLibrary();

        // Holds the current flash mode.
        private string currentFlashMode;

        // Holds the current resolution index.
        int currentResIndex = 0;

        PhotoChooserTask selectphoto = null;


        enum CaptureMode {CaptureFrame, CapturePhoto, PostView, LAST};
        CaptureMode cm = CaptureMode.CaptureFrame;
        string frameFilename = null;
        string photoFilename = null;
        string frameFilename_th = null;
        string photoFilename_th = null;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            //PhotoCamera cam = new Microsoft.Devices.PhotoCamera();
            //viewfinderBrush.SetSource(cam);

            selectphoto = new PhotoChooserTask();
            selectphoto.Completed += new EventHandler<PhotoResult>(selectphoto_Completed);

            //canvas1.Visibility = Visibility.Visible;
            GuidelineIn.Begin();
            cm = CaptureMode.CaptureFrame;
        }
        //Code for initialization, capture completed, image availability events; also setting the source for the viewfinder.
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            // Check to see if the camera is available on the device.
            if ((PhotoCamera.IsCameraTypeSupported(CameraType.Primary) == true) ||
                 (PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing) == true))
            {
                // Initialize the camera, when available.
                if (PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing))
                {
                    // Use front-facing camera if available.
                    cam = new Microsoft.Devices.PhotoCamera(CameraType.FrontFacing);
                }
                else
                {
                    // Otherwise, use standard camera on back of device.
                    cam = new Microsoft.Devices.PhotoCamera(CameraType.Primary);
                }

                // Event is fired when the PhotoCamera object has been initialized.
                cam.Initialized += new EventHandler<Microsoft.Devices.CameraOperationCompletedEventArgs>(cam_Initialized);

                // Event is fired when the capture sequence is complete.
                cam.CaptureCompleted += new EventHandler<CameraOperationCompletedEventArgs>(cam_CaptureCompleted);

                // Event is fired when the capture sequence is complete and an image is available.
                cam.CaptureImageAvailable += new EventHandler<Microsoft.Devices.ContentReadyEventArgs>(cam_CaptureImageAvailable);

                // Event is fired when the capture sequence is complete and a thumbnail image is available.
                cam.CaptureThumbnailAvailable += new EventHandler<ContentReadyEventArgs>(cam_CaptureThumbnailAvailable);

                // The event is fired when auto-focus is complete.
                cam.AutoFocusCompleted += new EventHandler<CameraOperationCompletedEventArgs>(cam_AutoFocusCompleted);

                // The event is fired when the viewfinder is tapped (for focus).
                viewfinderCanvas.Tap += new EventHandler<GestureEventArgs>(focus_Tapped);

                // The event is fired when the shutter button receives a half press.
                CameraButtons.ShutterKeyHalfPressed += OnButtonHalfPress;

                // The event is fired when the shutter button receives a full press.
                CameraButtons.ShutterKeyPressed += OnButtonFullPress;

                // The event is fired when the shutter button is released.
                CameraButtons.ShutterKeyReleased += OnButtonRelease;

                //Set the VideoBrush source to the camera.
                viewfinderBrush.SetSource(cam);
            }
            else
            {
                // The camera is not supported on the device.
                this.Dispatcher.BeginInvoke(delegate()
                {
                    // Write message.
                    txtDebug.Text = "A Camera is not available on this device.";
                });

                // Disable UI.
                ShutterButton.IsEnabled = false;
                FlashButton.IsEnabled = false;
                AFButton.IsEnabled = false;
                ResButton.IsEnabled = false;
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (cam != null)
            {
                // Dispose camera to minimize power consumption and to expedite shutdown.
                cam.Dispose();

                // Release memory, ensure garbage collection.
                cam.Initialized -= cam_Initialized;
                cam.CaptureCompleted -= cam_CaptureCompleted;
                cam.CaptureImageAvailable -= cam_CaptureImageAvailable;
                cam.CaptureThumbnailAvailable -= cam_CaptureThumbnailAvailable;
                cam.AutoFocusCompleted -= cam_AutoFocusCompleted;
                CameraButtons.ShutterKeyHalfPressed -= OnButtonHalfPress;
                CameraButtons.ShutterKeyPressed -= OnButtonFullPress;
                CameraButtons.ShutterKeyReleased -= OnButtonRelease;
            }
        }

        // Update the UI if initialization succeeds.
        void cam_Initialized(object sender, Microsoft.Devices.CameraOperationCompletedEventArgs e)
        {
            if (e.Succeeded)
            {
                cam.FlashMode = FlashMode.Off;

                this.Dispatcher.BeginInvoke(delegate()
                {
                    // Write message.
                    txtDebug.Text = "Camera initialized.";

                    // Set flash button text.
                    FlashButton.Content = "Fl:" + cam.FlashMode.ToString();
                });
            }
        }

        // Ensure that the viewfinder is upright in LandscapeRight.
        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if (cam != null)
            {
                // LandscapeRight rotation when camera is on back of device.
                int landscapeRightRotation = 180;

                // Change LandscapeRight rotation for front-facing camera.
                if (cam.CameraType == CameraType.FrontFacing) landscapeRightRotation = -180;

                // Rotate video brush from camera.
                if (e.Orientation == PageOrientation.LandscapeRight)
                {
                    return; // not allow to rotate to LandscapeRight

                    // Rotate for LandscapeRight orientation.
                    //viewfinderBrush.RelativeTransform =
                    //    new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = landscapeRightRotation };
                }
                else
                {
                    // Rotate for standard landscape orientation.
                    viewfinderBrush.RelativeTransform =
                        new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = 0 };
                }
            }

            base.OnOrientationChanged(e);
        }

        private void shutterButton_Click(object sender, RoutedEventArgs e)
        {
            if (cm == CaptureMode.PostView)
            {
                lastView.Visibility = Visibility.Collapsed;
                //canvas1.Visibility = Visibility.Visible;
                GuidelineIn.Begin();
                cm = CaptureMode.CaptureFrame;
                return;
            }

            if (cam != null)
            {
                try
                {
                    // Start image capture.
                    cam.CaptureImage();

                    Stream stream = TitleContainer.OpenStream("Sounds/camera-click-1.wav");
                    SoundEffect effect = SoundEffect.FromStream(stream);
                    FrameworkDispatcher.Update();
                    effect.Play();
                }
                catch (Exception ex)
                {
                    this.Dispatcher.BeginInvoke(delegate()
                    {
                        // Cannot capture an image until the previous capture has completed.
                        txtDebug.Text = ex.Message;
                    });
                }
            }

            Console.Write("------------------- debug -------------------\n");
            PictureCollection pictures = library.Pictures;

            foreach (Picture pict in pictures)
            {
                Debug.WriteLine(String.Format("{0}\t{1}\t{2}", pict.Name, pict.Album.Name, pict.Date));
            }
                
        
        }

        void cam_CaptureCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
            Debug.WriteLine("cam_CaptureCompleted");

            switch (cm)
            {
                case CaptureMode.CaptureFrame:
                    cm = CaptureMode.CapturePhoto;
                    break;
                case CaptureMode.CapturePhoto:
                    cm = CaptureMode.PostView;
                    //cm = CaptureMode.CaptureFrame;
                    //Deployment.Current.Dispatcher.BeginInvoke(delegate()
                    //{
                    //    NavigationService.Navigate(new Uri("/Postview.xaml", UriKind.Relative));
                    //    lastView.Visibility = Visibility.Collapsed;
                    //    GuidelineIn.Begin();
                    //});
                    break;
            }

            // Increments the savedCounter variable used for generating JPEG file names.
            savedCounter++;
            savedCounter %= 2; // toggle 0 and 1
        }

        // Informs when full resolution picture has been taken, saves to local media library and isolated storage.
        void cam_CaptureImageAvailable(object sender, Microsoft.Devices.ContentReadyEventArgs e)
        {
            Debug.WriteLine("cam_CaptureImageAvailable");

            string fileName = savedCounter + ".jpg";

            try
            {   // Write message to the UI thread.
                Deployment.Current.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "Captured image available, saving picture.";
                });

                // Save picture to the library camera roll.
                //library.SavePictureToCameraRoll(fileName, e.ImageStream);
                // not saving in Media Library camera roll here. Will save after merging faces

                // Write message to the UI thread.
                Deployment.Current.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "Picture has been saved to camera roll.";
                });

                // Set the position of the stream back to start
                e.ImageStream.Seek(0, SeekOrigin.Begin);

                // Save picture as JPEG to isolated storage.
                using (IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream targetStream = isStore.OpenFile(fileName, FileMode.Create, FileAccess.Write))
                    {
                        // Initialize the buffer for 4KB disk pages.
                        byte[] readBuffer = new byte[4096];
                        int bytesRead = -1;

                        // Copy the image to isolated storage. 
                        while ((bytesRead = e.ImageStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                        {
                            targetStream.Write(readBuffer, 0, bytesRead);
                        }
                    }
                }

                switch (cm)
                {
                    case CaptureMode.CaptureFrame:
                        frameFilename = fileName;
                        //cm = CaptureMode.CapturePhoto;
                        break;
                    case CaptureMode.CapturePhoto:
                        photoFilename = fileName;
                        // Write message to UI thread.
                        Deployment.Current.Dispatcher.BeginInvoke(delegate()
                        {
                            using (IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                using (IsolatedStorageFileStream frameStream = isStore.OpenFile(frameFilename, FileMode.Open, FileAccess.Read))
                                {
                                    Debug.WriteLine("Reading {0} from Isolated Storage", frameFilename);
                                    BitmapImage fb = new BitmapImage();
                                    fb.SetSource(frameStream);
                                    WriteableBitmap fwb = new WriteableBitmap(fb);

                                    createHole(fwb, 0.5F, 0.4F, 0.3F, 0.5F);

                                    using (IsolatedStorageFileStream photoStream = isStore.OpenFile(photoFilename, FileMode.Open, FileAccess.Read))
                                    {
                                        BitmapImage pb = new BitmapImage();
                                        pb.SetSource(photoStream);
                                        WriteableBitmap pwb = new WriteableBitmap(pb);

                                        blit(fwb, pwb, 0.5F, 0.4F, 0.3F, 0.5F);

                                        Deployment.Current.Dispatcher.BeginInvoke(delegate()
                                        {
                                            using (IsolatedStorageFile isStore2 = IsolatedStorageFile.GetUserStoreForApplication())
                                            {
                                                using (IsolatedStorageFileStream galleryStream = isStore2.OpenFile("SavedImage.jpg", FileMode.Create, FileAccess.ReadWrite))
                                                {
                                                    fwb.SaveJpeg(galleryStream, fwb.PixelWidth, fwb.PixelHeight, 0, 85);
                                                    galleryStream.Seek(0, SeekOrigin.Begin);
                                                    library.SavePictureToCameraRoll(photoFilename, galleryStream);
                                                }
                                            }
                                        });
                                    }
                                }
                            }
                        });
                        //cm = CaptureMode.PostView;
                        break;
                }


                // Write message to the UI thread.
                Deployment.Current.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "Picture has been saved to isolated storage.";
                });
            }
            finally
            {
                // Close image stream
                e.ImageStream.Close();
            }

        }

        void createHole(WriteableBitmap wb, float centerX, float centerY, float width, float height)
        {
            int x1 = (int)(wb.PixelWidth  * (centerX - width  / 2));
            int x2 = (int)(wb.PixelWidth  * (centerX + width  / 2));
            int y1 = (int)(wb.PixelHeight * (centerY - height / 2));
            int y2 = (int)(wb.PixelHeight * (centerY + height / 2));

            if (x1 < 0) x1 = 0;
            if (x2 > wb.PixelWidth) x2 = wb.PixelWidth;
            if (y1 < 0) y1 = 0;
            if (y2 > wb.PixelHeight) x2 = wb.PixelHeight;

            for (int y = y1; y < y2; y++)
            {
                for (int x = x1; x < x2; x++)
                {
                    float xf = (float)x / wb.PixelWidth;
                    float yf = (float)y / wb.PixelHeight;
                    float distX = Math.Abs(xf - centerX) * 2 / width;
                    float distY = Math.Abs(yf - centerY) * 2 / height;
                    float opacityLevel = (float)Math.Sqrt(distX * distX + distY * distY);
                    //float opacityLevel = distX * distX + distY * distY;
                    if (opacityLevel > 1) opacityLevel = 1;
                    if (opacityLevel < 0.9f)
                        opacityLevel = 0;
                    else
                        opacityLevel = (opacityLevel - 0.9f) * 10f;

                    int p = wb.Pixels[x + y * wb.PixelWidth];
                    byte A = (byte)((p & 0xFF000000) >> 24);
                    byte R = (byte)((p & 0x00FF0000) >> 16);
                    byte G = (byte)((p & 0x0000FF00) >> 8);
                    byte B = (byte)((p & 0x000000FF));

                    A = (byte)(opacityLevel * A);
                    R = (byte)(opacityLevel * R);
                    G = (byte)(opacityLevel * G);
                    B = (byte)(opacityLevel * B);

                    wb.Pixels[x + y * wb.PixelWidth] = A << 24 | R << 16 | G << 8 | B;

                    if (y == 240)
                    {
                        Debug.WriteLine("{0:X} => {1:X}", p, wb.Pixels[x + y * wb.PixelWidth]);
                    }
                }
            }
        }

        void blit(WriteableBitmap tgt, WriteableBitmap src, float centerX, float centerY, float width, float height)
        {
            int x1 = (int)(src.PixelWidth  * (centerX - width  / 2));
            int x2 = (int)(src.PixelWidth  * (centerX + width  / 2));
            int y1 = (int)(src.PixelHeight * (centerY - height / 2));
            int y2 = (int)(src.PixelHeight * (centerY + height / 2));

            if (x1 < 0) x1 = 0;
            if (x2 > src.PixelWidth) x2 = src.PixelWidth;
            if (y1 < 0) y1 = 0;
            if (y2 > src.PixelHeight) x2 = src.PixelHeight;

            for (int y = y1; y < y2; y++)
            {
                for (int x = x1; x < x2; x++)
                {
                    int t = tgt.Pixels[x + y * tgt.PixelWidth];
                    int s = src.Pixels[x + y * src.PixelWidth];
                    byte tA = (byte)((t & 0xFF000000) >> 24);
                    byte tR = (byte)((t & 0x00FF0000) >> 16);
                    byte tG = (byte)((t & 0x0000FF00) >> 8);
                    byte tB = (byte)((t & 0x000000FF));
                    byte sA = (byte)((s & 0xFF000000) >> 24);
                    byte sR = (byte)((s & 0x00FF0000) >> 16);
                    byte sG = (byte)((s & 0x0000FF00) >> 8);
                    byte sB = (byte)((s & 0x000000FF));

                    tR = (byte)(tR + (255 - tA) * sR / 255);
                    tG = (byte)(tG + (255 - tA) * sG / 255);
                    tB = (byte)(tB + (255 - tA) * sB / 255);
                    tA = (byte)(tA + (255 - tA) * sA / 255);

                    tgt.Pixels[x + y * tgt.PixelWidth] = tA << 24 | tR << 16 | tG << 8 | tB;
                }
            }
        }

        // Informs when thumbnail picture has been taken, saves to isolated storage
        // User will select this image in the pictures application to bring up the full-resolution picture. 
        public void cam_CaptureThumbnailAvailable(object sender, ContentReadyEventArgs e)
        {
            Debug.WriteLine("cam_CaptureThumbnailAvailable");
            string fileName = savedCounter + "_th.jpg";

            try
            {
                // Write message to UI thread.
                Deployment.Current.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "Captured image available, saving thumbnail.";
                });

                // Save thumbnail as JPEG to isolated storage.
                using (IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream targetStream = isStore.OpenFile(fileName, FileMode.Create, FileAccess.Write))
                    {
                        // Initialize the buffer for 4KB disk pages.
                        byte[] readBuffer = new byte[4096];
                        int bytesRead = -1;

                        // Copy the thumbnail to isolated storage. 
                        while ((bytesRead = e.ImageStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                        {
                            targetStream.Write(readBuffer, 0, bytesRead);
                        }
                    }
                }

                Debug.WriteLine("filename <= {0}", fileName);
                switch (cm)
                {
                    case CaptureMode.CaptureFrame:
                        frameFilename_th = fileName;
                        // Write message to UI thread.
                        Deployment.Current.Dispatcher.BeginInvoke(delegate()
                        {
                            using (IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                using (IsolatedStorageFileStream tgtStream = isStore.OpenFile(frameFilename_th, FileMode.Open, FileAccess.Read))
                                {
                                    Debug.WriteLine("Reading {0} from Isolated Storage", frameFilename_th);
                                    BitmapImage bi = new BitmapImage();
                                    bi.SetSource(tgtStream);
                                    WriteableBitmap wb = new WriteableBitmap(bi);

                                    createHole(wb, 0.5F, 0.4F, 0.3F, 0.5F);

                                    lastView.Source = wb;
                                    lastView.Visibility = Visibility.Visible;
                                }
                            }
                        });
                        break;
                    case CaptureMode.CapturePhoto:
                        photoFilename_th = fileName;

                        //break; // separate postview page is shown. no need to perform below procedure.

                        // Write message to UI thread.
                        Deployment.Current.Dispatcher.BeginInvoke(delegate()
                        {
                            using (IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                using (IsolatedStorageFileStream frameStream = isStore.OpenFile(frameFilename_th, FileMode.Open, FileAccess.Read))
                                {
                                    Debug.WriteLine("Reading {0} from Isolated Storage", frameFilename_th);
                                    BitmapImage fb = new BitmapImage();
                                    fb.SetSource(frameStream);
                                    WriteableBitmap fwb = new WriteableBitmap(fb);

                                    createHole(fwb, 0.5F, 0.4F, 0.3F, 0.5F);

                                    using (IsolatedStorageFileStream photoStream = isStore.OpenFile(photoFilename_th, FileMode.Open, FileAccess.Read))
                                    {
                                        BitmapImage pb = new BitmapImage();
                                        pb.SetSource(photoStream);
                                        WriteableBitmap pwb = new WriteableBitmap(pb);

                                        blit(fwb, pwb, 0.5F, 0.4F, 0.3F, 0.5F);

                                        lastView.Source = fwb;
                                        lastView.Visibility = Visibility.Visible;
                                        //canvas1.Visibility = Visibility.Collapsed;
                                        GuidelineOut.Begin();
                                    }
                                }
                            }
                        });
                        break;
                }
            }
            finally
            {
                // Close image stream
                e.ImageStream.Close();
            }
        }

        // Activate a flash mode.
        // Cycle through flash mode options when the flash button is pressed.
        private void changeFlash_Clicked(object sender, RoutedEventArgs e)
        {

            switch (cam.FlashMode)
            {
                case FlashMode.Off:
                    if (cam.IsFlashModeSupported(FlashMode.On))
                    {
                        // Specify that flash should be used.
                        cam.FlashMode = FlashMode.On;
                        FlashButton.Content = "Fl:On";
                        currentFlashMode = "Flash mode: On";
                    }
                    break;
                case FlashMode.On:
                    if (cam.IsFlashModeSupported(FlashMode.RedEyeReduction))
                    {
                        // Specify that the red-eye reduction flash should be used.
                        cam.FlashMode = FlashMode.RedEyeReduction;
                        FlashButton.Content = "Fl:RER";
                        currentFlashMode = "Flash mode: RedEyeReduction";
                    }
                    else if (cam.IsFlashModeSupported(FlashMode.Auto))
                    {
                        // If red-eye reduction is not supported, specify automatic mode.
                        cam.FlashMode = FlashMode.Auto;
                        FlashButton.Content = "Fl:Auto";
                        currentFlashMode = "Flash mode: Auto";
                    }
                    else
                    {
                        // If automatic is not supported, specify that no flash should be used.
                        cam.FlashMode = FlashMode.Off;
                        FlashButton.Content = "Fl:Off";
                        currentFlashMode = "Flash mode: Off";
                    }
                    break;
                case FlashMode.RedEyeReduction:
                    if (cam.IsFlashModeSupported(FlashMode.Auto))
                    {
                        // Specify that the flash should be used in the automatic mode.
                        cam.FlashMode = FlashMode.Auto;
                        FlashButton.Content = "Fl:Auto";
                        currentFlashMode = "Flash mode: Auto";
                    }
                    else
                    {
                        // If automatic is not supported, specify that no flash should be used.
                        cam.FlashMode = FlashMode.Off;
                        FlashButton.Content = "Fl:Off";
                        currentFlashMode = "Flash mode: Off";
                    }
                    break;
                case FlashMode.Auto:
                    if (cam.IsFlashModeSupported(FlashMode.Off))
                    {
                        // Specify that no flash should be used.
                        cam.FlashMode = FlashMode.Off;
                        FlashButton.Content = "Fl:Off";
                        currentFlashMode = "Flash mode: Off";
                    }
                    break;
            }

            // Display current flash mode.
            this.Dispatcher.BeginInvoke(delegate()
            {
                txtDebug.Text = currentFlashMode;
            });
        }

        // Provide auto-focus in the viewfinder.
        private void focus_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (cam.IsFocusSupported == true)
            {
                //Focus when a capture is not in progress.
                try
                {
                    cam.Focus();
                }
                catch (Exception focusError)
                {
                    // Cannot focus when a capture is in progress.
                    this.Dispatcher.BeginInvoke(delegate()
                    {
                        txtDebug.Text = focusError.Message;
                    });
                }
            }
            else
            {
                // Write message to UI.
                this.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "Camera does not support programmable auto focus.";
                });
            }
        }

        void cam_AutoFocusCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(delegate()
            {
                // Write message to UI.
                txtDebug.Text = "Auto focus has completed.";

                // Hide the focus brackets.
                focusBrackets.Visibility = Visibility.Collapsed;

            });
        }

        // Provide touch focus in the viewfinder.
        void focus_Tapped(object sender, GestureEventArgs e)
        {
            if (cam != null)
            {
                if (cam.IsFocusAtPointSupported == true)
                {
                    try
                    {
                        // Determine location of tap.
                        System.Windows.Point tapLocation = e.GetPosition(viewfinderCanvas);

                        // Position focus brackets with estimated offsets.
                        focusBrackets.SetValue(Canvas.LeftProperty, tapLocation.X - 30);
                        focusBrackets.SetValue(Canvas.TopProperty, tapLocation.Y - 28);

                        // Determine focus point.
                        double focusXPercentage = tapLocation.X / viewfinderCanvas.Width;
                        double focusYPercentage = tapLocation.Y / viewfinderCanvas.Height;

                        // Show focus brackets and focus at point
                        focusBrackets.Visibility = Visibility.Visible;
                        cam.FocusAtPoint(focusXPercentage, focusYPercentage);

                        // Write a message to the UI.
                        this.Dispatcher.BeginInvoke(delegate()
                        {
                            txtDebug.Text = String.Format("Camera focusing at point: {0:N2} , {1:N2}", focusXPercentage, focusYPercentage);
                        });
                    }
                    catch (Exception focusError)
                    {
                        // Cannot focus when a capture is in progress.
                        this.Dispatcher.BeginInvoke(delegate()
                        {
                            // Write a message to the UI.
                            txtDebug.Text = focusError.Message;
                            // Hide focus brackets.
                            focusBrackets.Visibility = Visibility.Collapsed;
                        });
                    }
                }
                else
                {
                    // Write a message to the UI.
                    this.Dispatcher.BeginInvoke(delegate()
                    {
                        txtDebug.Text = "Camera does not support FocusAtPoint().";
                    });
                }
            }
        }

        private void changeRes_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            // Variables
            IEnumerable<Size> resList = cam.AvailableResolutions;
            int resCount = resList.Count<Size>();
            Size res;

            // Poll for available camera resolutions.
            for (int i = 0; i < resCount; i++)
            {
                res = resList.ElementAt<Size>(i);
                Debug.WriteLine("Resolution[{0}] = {1}", i, res);
            }

            // Set the camera resolution.
            res = resList.ElementAt<Size>((currentResIndex + 1) % resCount);
            cam.Resolution = res;
            currentResIndex = (currentResIndex + 1) % resCount;

            // Update the UI.
            txtDebug.Text = String.Format("Setting capture resolution: {0}x{1}", res.Width, res.Height);
            ResButton.Content = "R" + res.Width;
        }


        // Provide auto-focus with a half button press using the hardware shutter button.
        private void OnButtonHalfPress(object sender, EventArgs e)
        {
            Debug.WriteLine("OnButtonHalfPress");

            if (cm == CaptureMode.PostView)
            {
                lastView.Visibility = Visibility.Collapsed;
                //canvas1.Visibility = Visibility.Visible;
                GuidelineIn.Begin();
                cm = CaptureMode.CaptureFrame;
                return;
            }

            if (cam != null)
            {
                // Focus when a capture is not in progress.
                try
                {
                    this.Dispatcher.BeginInvoke(delegate()
                    {
                        txtDebug.Text = "Half Button Press: Auto Focus";
                    });

                    cam.Focus();
                }
                catch (Exception focusError)
                {
                    // Cannot focus when a capture is in progress.
                    this.Dispatcher.BeginInvoke(delegate()
                    {
                        txtDebug.Text = focusError.Message;
                    });
                }
            }
        }

        // Capture the image with a full button press using the hardware shutter button.
        private void OnButtonFullPress(object sender, EventArgs e)
        {
            Debug.WriteLine("OnButtonFullPress");

            if (cm == CaptureMode.PostView)
            {
                lastView.Visibility = Visibility.Collapsed;
                //canvas1.Visibility = Visibility.Visible;
                GuidelineIn.Begin();
                cm = CaptureMode.CaptureFrame;
                return;
            }

            if (cam != null)
            {
                try
                {
                    cam.CaptureImage();

                    Stream stream = TitleContainer.OpenStream("Sounds/camera-click-1.wav");
                    SoundEffect effect = SoundEffect.FromStream(stream);
                    FrameworkDispatcher.Update();
                    effect.Play();
                }
                catch (Exception ex)
                {
                    txtDebug.Text = ex.Message;
                    Debug.WriteLine("Error, {0}", ex.Message);
                }
            }
        }

        // Cancel the focus if the half button press is released using the hardware shutter button.
        private void OnButtonRelease(object sender, EventArgs e)
        {
            Debug.WriteLine("OnButtonRelease");

            if (cm == CaptureMode.PostView)
            {
                lastView.Visibility = Visibility.Collapsed;
                //canvas1.Visibility = Visibility.Visible;
                GuidelineIn.Begin();
                cm = CaptureMode.CaptureFrame;
                return;
            }

            if (cam != null)
            {
                cam.CancelFocus();
            }
        }

        private void galleryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                selectphoto.Show();
            }
            catch (System.InvalidOperationException ex)
            {
                Debug.WriteLine("Exception occured when openining PhotoChooserTask, {0}", ex.Message);
            }
        }

        void selectphoto_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                BinaryReader reader = new BinaryReader(e.ChosenPhoto);
                image1.Source = new BitmapImage(new Uri(e.OriginalFileName));
            }
            else
            {
                Debug.WriteLine("An error occured on PhotoChooseTask: {0}", e.Error);
            }
        }

    }
}