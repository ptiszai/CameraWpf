using System;
using System.Linq;
using System.Windows;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace CameraWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MediaCapture _mediaCapture;
        private readonly CaptureElement _captureElement;
        private StorageFolder _captureFolder;
        private bool _initialized = false;      
      
        public MainWindow()
        {           
            _mediaCapture = new MediaCapture();
            InitializeComponent();
            _captureElement = new CaptureElement
            {
                Stretch = Windows.UI.Xaml.Media.Stretch.Uniform
            };
            _captureElement.Loaded += CaptureElement_Loaded;
            _captureElement.Unloaded += CaptureElement_Unloaded;

            XamlHost.Child = _captureElement;
        }
        private async void CaptureElement_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await _mediaCapture.StopPreviewAsync();
        }
        private async void CaptureElement_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!_initialized)
            {
                var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                // Fall back to the local app storage if the Pictures Library is not available
                _captureFolder = picturesLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;

                // Get available devices for capturing pictures
                var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                if (allVideoDevices.Count > 0)
                {
                    // try to find back camera
                    DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);

                    // If there is no device mounted on the back panel, return the first device found
                    var device = desiredDevice ?? allVideoDevices.FirstOrDefault();

                    await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings() { VideoDeviceId = device.Id });
                    _captureElement.Source = _mediaCapture;

                    _initialized = true;
                }
            }

            if (_initialized)
            {
                await _mediaCapture.StartPreviewAsync();
            }
        }
    
        private async void Photo_Click(object sender, RoutedEventArgs e)
        {
            if (!_initialized)
            {
                return;
            }

            using var stream = new InMemoryRandomAccessStream();

            await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

            try
            {
                var file = await _captureFolder.CreateFileAsync("Photo.jpg", CreationCollisionOption.GenerateUniqueName);

                var decoder = await BitmapDecoder.CreateAsync(stream);

                using var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);

                await encoder.FlushAsync();
            }
            catch (Exception)
            {
            }
        }
    }
}



   
