using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace unCam
{
    public partial class Postview : PhoneApplicationPage
    {
        public Postview()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            using (IsolatedStorageFile isStore2 = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream savedImageStream = isStore2.OpenFile("1_th.jpg", FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        BitmapImage b = new BitmapImage();
                        b.SetSource(savedImageStream);
                        image1.Source = b;
                    }
                    catch (Exception ex)
                    {
                    }                   
                }
            }
        }

        private void OK_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}