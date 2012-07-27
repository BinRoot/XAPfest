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

                PageTitle.Text = friend;
            }

            //DataUse.Instance.EventId = long.Parse(eventid);

            MessageBox.Show("I want to join the chatroom: " + eventid);
            // join meeting at eventid
            JoinMeeting(eventid);

            base.OnNavigatedTo(e);
        }

        private void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataUse.Instance.DS == null)
            {
                DataUse.Instance.DS = new DataService(null);
            }

            DataUse.Instance.DS.GetPushKey(friendid, this);

            string message = friend+" hello from invitation page";
            MessageBox.Show("I want to send the a message to the chatroom I joined");
           // send toast
            SendText(message);
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




        // --------- HERE WE GO!
        private SmashSession session;
        private SmashTable<Channels.ChatRecord> chat;
        private const string ApplicationSecret = "0B797E34-905A-406D-B8CF-F57DC6EB0839";

        private void JoinMeeting(string token)
        {
            if (this.session != null)
            {
                this.session.Shutdown();
                this.session = null;
            }

            //this.Join.IsEnabled = false;
            //this.Create.IsEnabled = false;

            string user = "Sample User";
            string email = "sample@sample.com";


            if (token.Length != 6)
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Please type a 6 character alphanumeric session code first.");
                    //this.Join.IsEnabled = true;
                    //this.Create.IsEnabled = true;
                });
            }
            else
            {
                this.chat = new SmashTable<Channels.ChatRecord>("Chat");
                Dispatcher.BeginInvoke(() =>
                {
                    ChatText.DataContext = this.chat;
                });
                SessionManager sessionManager = new SessionManager();
                sessionManager.JoinSessionCompleted += new JoinSessionCompletedHandler(this.SessionManager_JoinSessionCompleted);
                sessionManager.JoinSessionAsync(HawaiiClient.HawaiiApplicationId, this.Dispatcher, this.GetMeetingToken(token), user, email, Microsoft.Phone.Info.DeviceStatus.DeviceName, new ISmashTable[] { this.chat }, null);
            }
        }

        private void SessionManager_JoinSessionCompleted(object sender, JoinSessionCompletedArgs e)
        {
            this.session = e.Session;
            this.Dispatcher.BeginInvoke(() =>
            {
            });
        }

        private Guid GetMeetingToken(string token)
        {
            Guid tmp = new Guid(ApplicationSecret);
            byte[] b0 = tmp.ToByteArray();
            byte[] b1 = UnicodeEncoding.Unicode.GetBytes(token.ToUpperInvariant());

            for (int i = 0; i < b0.Length && i < b1.Length; i++)
            {
                b0[i] ^= b1[i];
            }

            return new Guid(b0);
        }


        // ------- SENDING MESSAGE ZOMG!

        public void SendText(string textStr)
        {
            if (this.chat != null)
            {
                ISmashTableChangeContext context = this.chat.GetTableChangeContext();
                context.Add(new Channels.ChatRecord(textStr));
                context.SaveChangesCompleted += new SaveChangesCompletedHandler(this.Context_SaveChangesCompleted);
                context.SaveChangesAsync(null);
            }
        }

        private void Context_SaveChangesCompleted(object sender, SaveChangesCompletedArgs e)
        {
            if (e.Error != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(e.Error.ToString());
                }));
            }
        }
    }
}