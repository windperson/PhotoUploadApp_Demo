using System;
using Microsoft.Identity.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace PhotoUploadApp
{
    public partial class App : Application
    {
        public static readonly string Tenant = @"netconf2018taichung4demo.onmicrosoft.com";
        public static readonly string ClientID = @"d53c6459-fc4e-418e-95b9-777b62b52232";
        public static readonly string SignUpInPolicy = @"B2C_1_PhotoUploadAppSignUpIn";
        public static readonly string Authority = $"https://login.microsoftonline.com/tfp/{Tenant}/{SignUpInPolicy}";
        //public static readonly string[] Scopes = new string[]{};

        public static PublicClientApplication AuthClient = null;
        public static UIParent CurrentUiParent = null;

        public App()
        {
            InitializeComponent();

            AuthClient = new PublicClientApplication(ClientID, Authority);
            AuthClient.ValidateAuthority = false;
            AuthClient.RedirectUri = $"msal{ClientID}://auth";

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
