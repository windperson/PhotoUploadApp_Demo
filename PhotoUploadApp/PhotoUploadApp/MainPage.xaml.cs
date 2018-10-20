using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace PhotoUploadApp
{
    public partial class MainPage : ContentPage
    {
        private MediaFile _photo;

        private const string _url =
            @"https://photouploadappdemo-api.azurewebsites.net/api/upload?code=2XEqXUnSaCaldczAgia7cFRbeGtZIO6vNbmSbgDP5niVlRD33FfF6w==";

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
                var accessToken = await GetCachedSignInTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    accessToken = await GetSignInToken();
                }

                if (string.IsNullOrEmpty(accessToken))
                {
                    await DisplayAlert("Failed", "Sign in failed", "OK");
                    return;
                }

                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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


        private async Task<string> GetSignInToken()
        {
            var client = App.AuthClient;
            try
            {
                var loginResult = await client.AcquireTokenAsync(
                    new string[] {
                        @"https://netconf2018taichung4demo.onmicrosoft.com/uplaod-api/user_impersonation"
                       }, 
                    App.CurrentUiParent);
                return loginResult.AccessToken;
            }
            catch (MsalServiceException ex)
            {
                if (ex.ErrorCode == MsalClientException.AuthenticationCanceledError)
                {
                    await DisplayAlert("Cancelled", "User cancelled", "Got it");
                }
                else if (ex.ErrorCode == "access_denied") // yeah, there's not a constant in the library for this
                {
                    await DisplayAlert("Cancelled", "Access denied", "Got it");
                }
                else
                {
                    await DisplayAlert("Cancelled", $"ex={ex}", "Got it");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"ex={ex}", "OK");
            }

            return string.Empty;
        }


        private async Task<string> GetCachedSignInTokenAsync()
        {
            var client = App.AuthClient;
            try
            {
                var firstAccount = (await client.GetAccountsAsync()).FirstOrDefault();

                if (firstAccount == null )
                {
                    return string.Empty;
                }

                var result = await client.AcquireTokenSilentAsync(
                    new string[]
                    {
                        @"https://netconf2018taichung4demo.onmicrosoft.com/uplaod-api/user_impersonation"
                    },
                    firstAccount);

                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                await DisplayAlert("warning", "token expired", "OK");
            }
            catch (MsalClientException ex)
            {
                await DisplayAlert("error", $"ex={ex}", "OK");
            }
            return string.Empty;
        }


    }
}
