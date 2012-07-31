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
using Microsoft.Phone.Controls;
using Microsoft.Live;
using Microsoft.Live.Controls;

namespace SmashSampleApp
{
    public partial class LogIn : PhoneApplicationPage
    {
        private LiveConnectClient client;

        public LogIn()
        {
            InitializeComponent();
        }

        private void btnSignin_SessionChanged(object sender, LiveConnectSessionChangedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                client = new LiveConnectClient(e.Session);
                infoTextBlock.Text = "Signed in.";
                client.GetCompleted +=
                    new EventHandler<LiveOperationCompletedEventArgs>(OnGetCompleted);
                client.GetAsync("me", null);
            }
            else
            {
                infoTextBlock.Text = "Not signed in.";
                client = null;
            }
        }

        void OnGetCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                string firstName = "";
                string lastName = "";
                if (e.Result.ContainsKey("first_name") ||
                    e.Result.ContainsKey("last_name"))
                {
                    if (e.Result.ContainsKey("first_name"))
                    {
                        if (e.Result["first_name"] != null)
                        {
                            firstName = e.Result["first_name"].ToString();
                        }
                    }
                    if (e.Result.ContainsKey("last_name"))
                    {
                        if (e.Result["last_name"] != null)
                        {
                            lastName = e.Result["last_name"].ToString();
                        }
                    }

                    infoTextBlock.Text =
                        "Welcome, " + firstName + " " + lastName + "!";

                    DataUse.Instance.MyUserName = firstName + " " + lastName;
                }
                else
                {
                    infoTextBlock.Text = "Hello, signed-in user!";
                }
            }
            else
            {
                infoTextBlock.Text = "Error calling API: " +
                    e.Error.ToString();
            }

            DataUse.Instance.MyUserId = e.Result["id"].ToString();
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));

        }
    }
}