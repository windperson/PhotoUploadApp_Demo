using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace PhotoUploadApp
{
    public partial class MainPage : ContentPage
    {
        private MediaFile _photo;

        private const string _url =
            @"the upload url";

        public MainPage()
        {
            InitializeComponent();
            CameraButton.Clicked += CameraButton_Clicked;
            UploadButton.IsEnabled = false;
            UploadButton.Clicked += UploadButton_Clicked;
        }

        private async void CameraButton_Clicked(object sender, EventArgs e)
        {
            _photo = await Plugin.Media.CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions());

            if (_photo != null)
            {
                PhotoImage.Source = ImageSource.FromStream(() => _photo.GetStream());
            }

            UploadButton.IsEnabled = true;
        }

        private async void UploadButton_Clicked(object sender, EventArgs e)
        {
            if (_photo == null)
            {
                await DisplayAlert("no image", "please take a picture", "OK");
                return;
            }

            try
            {
                HttpClient client = new HttpClient();
                MultipartFormDataContent content = new MultipartFormDataContent();

                content.Add(new StringContent($"User-{Guid.NewGuid()}"), "userid");
                content.Add(new StringContent(CommentEditor.Text), "comment");

                ByteArrayContent imageByteArrayContent = new ByteArrayContent(_photo.GetStream().ToByteArray());
                imageByteArrayContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    FileName = "image.jpg",
                };
                imageByteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

                content.Add(imageByteArrayContent);

                var response = await client.PostAsync(_url, content);
                var responseMsg = await response.Content.ReadAsStringAsync();

                await DisplayAlert("server response", responseMsg, "OK");
            }
            catch (Exception ex)
            {
               await DisplayAlert("Error", $"ex={ex}", "OK");
            }
        }
    }
}
