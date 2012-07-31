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
        string myname;

        MainPage MP;
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        public InvitationPage()
        {
            DataUse.Instance.ActiveLocationMode = false;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {

                if (this.NavigationContext.QueryString.ContainsKey("eventid")
                    && this.NavigationContext.QueryString.ContainsKey("friend")
                    && this.NavigationContext.QueryString.ContainsKey("friendid")
                    && this.NavigationContext.QueryString.ContainsKey("myid")
                    && this.NavigationContext.QueryString.ContainsKey("myname"))
                {
                    eventid = this.NavigationContext.QueryString["eventid"];
                    friend = this.NavigationContext.QueryString["friend"];
                    friendid = this.NavigationContext.QueryString["friendid"];
                    myid = this.NavigationContext.QueryString["myid"];
                    myname = this.NavigationContext.QueryString["myname"];

                    AddOrUpdateSettings("eventid", eventid);
                    AddOrUpdateSettings("friend", friend);
                    AddOrUpdateSettings("friendid", friendid);
                    AddOrUpdateSettings("myid", myid);
                    AddOrUpdateSettings("myname", myname);
                    AddOrUpdateSettings("ready", false);
                    AddOrUpdateSettings("setupMode", false);

                    DataUse.Instance.MyUserId = myid;
                    DataUse.Instance.MyUserName = myname;
                    PageTitle.Text = friend;


                    MP = new MainPage(false);

                    // MessageBox.Show("I want to join the chatroom: " + eventid);
                    MP.JoinMeeting(eventid);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("> "+err.Message);
            }


            base.OnNavigatedTo(e);
        }


        private string transportation;
        private void SendReply(string transportation)
        {
            AddOrUpdateSettings("transportation", transportation);

            this.transportation = transportation;

            if (DataUse.Instance.DS == null)
            {
                DataUse.Instance.DS = new DataService(null);
            }

            DataUse.Instance.DS.GetPushKey(friendid, this);

            if (DataUse.Instance.RoomCreated)
            {
                MP.SendText(DataUse.Instance.MessageToSend);

                NoButton.IsEnabled = true;
                WalkButton.IsEnabled = true;
                BikeButton.IsEnabled = true;
                CarButton.IsEnabled = true;
            }
        }


        public void SendToastToUser(string pushkey)
        {
            Debug.WriteLine("sending push to " + pushkey);
            string Title = "LyncUp";
            string SubTitle = friend + " accepted your Lync Up request";
            IDictionary DataTable = new Dictionary<String, String>();
            DataTable["friendacceptid"] = myid;
            DataTable["friendstatus"] = "yes";
            DataTable["transportation"] = transportation;
            PushAPI.SendToastToUser(pushkey, Title, SubTitle, DataTable, "MainPage");

            WalkButton.IsEnabled = true;
            BikeButton.IsEnabled = true;
            CarButton.IsEnabled = true;

            if (transportation == "walk")
                WalkButtonText.Text = "...";
            if (transportation == "bike")
                BikeButtonText.Text = "...";
            if (transportation == "car")
                CarButtonText.Text = "...";

            NoButton.IsEnabled = false;
            Thread thread1 = new Thread(new ThreadStart(GoBackLater));
            thread1.Start();
        }

        public void GoBackLater()
        {
            MP.LeaveSmash();
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

        private void Walk_Button_Click(object sender, RoutedEventArgs e)
        {
            SendReply("walk");
        }

        private void Bike_Button_Click(object sender, RoutedEventArgs e)
        {
            SendReply("bike");
        }

        private void Car_Button_Click(object sender, RoutedEventArgs e)
        {
            SendReply("car");
        }

    }
}