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
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using System.Collections;
using Microsoft.Hawaii.Smash.Client;
using System.Text;
using System.Threading;
using System.IO.IsolatedStorage;

namespace SmashSampleApp
{
    public partial class InvitationPage : PhoneApplicationPage
    {
        string eventid;
        string friend;
        string friendid;
        string myid;

        MainPage MP;
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        public InvitationPage()
        {
            DataUse.Instance.ActiveLocationMode = false;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (this.NavigationContext.QueryString.ContainsKey("eventid")
                && this.NavigationContext.QueryString.ContainsKey("friend")
                && this.NavigationContext.QueryString.ContainsKey("friendid")
                && this.NavigationContext.QueryString.ContainsKey("myid"))
            {
                eventid = this.NavigationContext.QueryString["eventid"];
                friend = this.NavigationContext.QueryString["friend"];
                friendid = this.NavigationContext.QueryString["friendid"];
                myid = this.NavigationContext.QueryString["myid"];

                AddOrUpdateSettings("eventid", eventid);
                AddOrUpdateSettings("friend", friend);
                AddOrUpdateSettings("friendid", friendid);
                AddOrUpdateSettings("myid", myid);
                AddOrUpdateSettings("ready", false);
                AddOrUpdateSettings("setupMode", false);

                DataUse.Instance.MyUserId = myid;
                PageTitle.Text = friend;

                
                MP = new MainPage(false);

                // MessageBox.Show("I want to join the chatroom: " + eventid);
                MP.JoinMeeting(eventid);
            }

            


            base.OnNavigatedTo(e);
        }

        private void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataUse.Instance.DS == null)
            {
                DataUse.Instance.DS = new DataService(null);
            }

            DataUse.Instance.DS.GetPushKey(friendid, this);

            if (DataUse.Instance.RoomCreated)
            {
                MP.SendText(DataUse.Instance.MessageToSend);

                YesButtonText.Text = "Let's go!";
                NoButton.IsEnabled = true;
                YesButton.IsEnabled = true;

                
            }
            else
            {
                YesButtonText.Text = "Loading...";

                NoButton.IsEnabled = false;
                YesButton.IsEnabled = false;

                Yes_Button_Click(null, null);
            }
            
           // send toast
            // SendText(message);
            // MP.SendText(message);

        }

        public void SendToastToUser(string pushkey)
        {
            Debug.WriteLine("sending push to " + pushkey);
            string Title = "LyncUp";
            string SubTitle = friend + " accepted your Lync Up request";
            IDictionary DataTable = new Dictionary<String, String>();
            DataTable["friendacceptid"] = myid;
            DataTable["friendstatus"] = "yes";
            PushAPI.SendToastToUser(pushkey, Title, SubTitle, DataTable, "MainPage");

            // NavigationService.GoBack();
            YesButton.IsEnabled = false;
            YesButtonText.Text = "Sending...";
            NoButton.IsEnabled = false;
            Thread thread1 = new Thread(new ThreadStart(GoBackLater));
            thread1.Start();
        }

        public void GoBackLater()
        {
            Thread.Sleep(2000);
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => { NavigationService.GoBack();});
            
        }

        
        public bool AddOrUpdateSettings(string Key, Object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (settings.Contains(Key))
            {
                // If the value has changed
                if (settings[Key] != value)
                {
                    // Store the new value
                    settings[Key] = value;
                    settings.Save();
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                settings.Add(Key, value);
                settings.Save();
                valueChanged = true;
            }
            return valueChanged;
        }
    }
}