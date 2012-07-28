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

namespace SmashSampleApp
{
    public partial class InvitationPage : PhoneApplicationPage
    {
        string eventid;
        string friend;
        string friendid;
        string myid;

        MainPage MP;

        public InvitationPage()
        {
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

                DataUse.Instance.MyUserId = myid;
                PageTitle.Text = friend;

                DataUse.Instance.ActiveLocationMode = false;
                MP = new MainPage(false);
            }

            //DataUse.Instance.EventId = long.Parse(eventid);

            MessageBox.Show("I want to join the chatroom: " + eventid);
            // join meeting at eventid
            //JoinMeeting(eventid);
            MP.JoinMeeting(eventid);


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

                DataUse.Instance.ActiveLocationMode = true;
                YesButtonText.Text = "Yes";
                NoButton.IsEnabled = true;
                YesButton.IsEnabled = true;

                NavigationService.GoBack();
            }
            else
            {
                YesButtonText.Text = "Loading...";

                NoButton.IsEnabled = false;
                YesButton.IsEnabled = false;

                Thread.Sleep(100);
                Yes_Button_Click(null, null);
            }
            
           // send toast
            // SendText(message);
            // MP.SendText(message);


            DataUse.Instance.ActiveLocationMode = true;
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
        }

    }
}