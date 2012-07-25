// ----------------------------------------------------------
// <copyright file="MainPage.Xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ----------------------------------------------------------

namespace SmashSampleApp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shapes;

    using Microsoft.Hawaii.Smash.Client;

    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;
    using Microsoft.Phone.UserData;
    using System.Device.Location;
    using SmashSampleApp.Model;
    using System.Windows.Controls.Primitives;
    using Microsoft.Phone.Controls.Maps;
    using Microsoft.Phone.Tasks;

    /// <summary>
    /// 
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// 
        /// </summary>
        private const string ApplicationSecret = "0B797E34-905A-406D-B8CF-F57DC6EB0839";

        /// <summary>
        /// Just for this sample application, we hard-code a managementID.
        /// In a real application, this should be generated once and kept securely as an owner-specific secrect.
        /// </summary>
        private const string ManagementID = "0B797E34-905A-406D-B8CF-F57DC6EB083A";

        /// <summary>
        /// 
        /// </summary>
        private static MainPage mainPage;

        /// <summary>
        /// 
        /// </summary>
        private static IDictionary<string, object> state;

        /// <summary>
        /// 
        /// </summary>
        private SmashSession session;

        /// <summary>
        /// 
        /// </summary>
        private SmashTable<Channels.ChatRecord> chat;

        /// <summary>
        /// Constructor
        /// </summary>
        ///


        GeoCoordinateWatcher watcher;
        IDataService DS;


        public MainPage()
        {
            this.InitializeComponent();

            this.VerifyHawaiiId(); 
            
            //this.SendText.Click += new RoutedEventHandler(this.SendText_Click);
            //this.Join.Click += new RoutedEventHandler(this.Join_Click);
            //this.Create.Click += new RoutedEventHandler(this.Create_Click);

            mainPage = this;
            if (state != null)
            {
                this.chat = new SmashTable<Channels.ChatRecord>("Chat");
                this.Dispatcher.BeginInvoke(() =>
                {
                    ChatText.DataContext = this.chat;
                });
                this.session = SmashSession.JoinSessionFromState(HawaiiClient.HawaiiApplicationId, this.Dispatcher, state, new ISmashTable[] { this.chat });
            }



            MainPanorama.DefaultItem = MainPanorama.Items[1];
            DS = new DataService(this);
            DataUse.Instance.DS = DS;
            DataUse.Instance.DS.GetFriends(DataUse.Instance.MyUserId, DataUse.Instance.MyUserName);
            DataUse.Instance.DS.GetNextEventId();
            // Set the data context of the listbox control to the sample data
           
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            PeopleList.DataContext = DataUse.Instance.ActiveFriends;


            #region [MAP]
            // The watcher variable was previously declared as type GeoCoordinateWatcher. 
            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
                watcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            }
            watcher.Start();

            LyncUpMap.Tap += this.map_Tap;
            #endregion

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="statePreserved"></param>
        public static void ActivateSession(IDictionary<string, object> s, bool statePreserved)
        {
            if (statePreserved && mainPage != null)
            {
                state = null;
                mainPage.session.ResumeSession();
            }
            else
            {
                state = s;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        public static void DeactivateSession(IDictionary<string, object> s)
        {
            if (mainPage != null && mainPage.session != null)
            {
                mainPage.session.DeactivateSession(s);
            }
        }









        public void RespondToGetFriends(List<Friend> Friends)
        {
            DataUse.Instance.FriendsList = Friends;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NavigationService.RemoveBackEntry();
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            PeopleList.DataContext = null;
            PeopleList.DataContext = DataUse.Instance.ActiveFriends;
        }

        private void AddPerson_Button(object sender, RoutedEventArgs e)
        {

            Uri uri = new Uri("/FriendAdder.xaml", UriKind.Relative);
            NavigationService.Navigate(uri);
        }

        private void NamePanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Popup popup = new Popup();
            popup.Height = 300;
            popup.Width = 400;
            popup.VerticalOffset = System.Windows.Application.Current.Host.Content.ActualHeight / 2.5;
            DisplayNamePopupUserControl control = new DisplayNamePopupUserControl();
            popup.Child = control;
            popup.IsOpen = true;


            control.btnOK.Click += (s, args) =>
            {
                popup.IsOpen = false;
                name.Text = control.tbx.Text;
            };

            control.btnCancel.Click += (s, args) =>
            {
                popup.IsOpen = false;
            };
        }

        private void TransportationPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBox.Show("I just met you");
        }

        private void RadiusPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBox.Show("and this is crazy");

            PhoneCallTask phoneCallTask = new PhoneCallTask();

            phoneCallTask.PhoneNumber = "2065550123";
            phoneCallTask.DisplayName = "Gage";

            phoneCallTask.Show();
        }

        private void UpdatePanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // MessageBox.Show("But here's my number, so call me maybe?");

        }

        private void RemovePerson_Button(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            string id = (string)b.Tag;
            foreach (Friend f in DataUse.Instance.ActiveFriends)
            {
                if (f.id == id)
                {
                    DataUse.Instance.ActiveFriends.Remove(f);
                    break;
                }
            }

            PeopleList.DataContext = null;
            PeopleList.DataContext = DataUse.Instance.ActiveFriends;
        }

        // Event handler for the GeoCoordinateWatcher.StatusChanged event.
        void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    // The Location Service is disabled or unsupported.
                    // Check to see whether the user has disabled the Location Service.
                    if (watcher.Permission == GeoPositionPermission.Denied)
                    {
                        // The user has disabled the Location Service on their device.
                        MessageBox.Show("you have blocked this application from access to location.");
                    }
                    else
                    {
                        MessageBox.Show("location is not functioning on this device");
                    }
                    break;

                case GeoPositionStatus.Initializing:
                    // The Location Service is initializing.
                    // Disable the Start Location button.
                    break;

                case GeoPositionStatus.NoData:
                    // The Location Service is working, but it cannot get location data.
                    // Alert the user and enable the Stop Location button.
                    MessageBox.Show("location data is not available.");
                    break;

                case GeoPositionStatus.Ready:
                    // The Location Service is working and is receiving location data.
                    // Show the current position and enable the Stop Location button.
                    break;
            }
        }

        public void plotVenues(List<Venue> venues)
        {
            var list = (from x in venues select x.location).ToArray();


            for (int i = 0; i < venues.Count; i++)
            {
                Pushpin venuePushpin = new Pushpin();
                venuePushpin.Location = venues[i].location;
                venuePushpin.Tap += this.pin_Tap;

                venuePushpin.Content = new TextBlock();
                ((TextBlock)venuePushpin.Content).Text = venues[i].name;
                ((TextBlock)venuePushpin.Content).Visibility = Visibility.Collapsed;

                LyncUpMap.Children.Add(venuePushpin);
            }

            LyncUpMap.SetView(LocationRect.CreateLocationRect(list));
        }

        private void clearTooltips()
        {
            if (LyncUpMap.Children.Count != 0)
            {
                List<UIElement> pushpins = LyncUpMap.Children.ToList();

                foreach (var item in pushpins)
                {
                    if (item != null && item.GetType() == typeof(Pushpin))
                    {
                        ((TextBlock)((Pushpin)item).Content).Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void pin_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            clearTooltips();

            //So I heard you like casting...
            ((TextBlock)((Pushpin)sender).Content).Visibility = Visibility.Visible;
            e.Handled = true;
        }

        private void map_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            clearTooltips();
        }

        private void clearMap()
        {
            //Add logic only to remove location pins versus venue pins
            if (LyncUpMap.Children.Count != 0)
            {
                List<UIElement> pushpins = LyncUpMap.Children.ToList();

                foreach (var item in pushpins)
                {
                    if (item != null && item.GetType() == typeof(Pushpin))
                    {
                        LyncUpMap.Children.Remove(item);
                    }
                }
            }
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            LyncUpMap.Center = new GeoCoordinate(e.Position.Location.Latitude, e.Position.Location.Longitude);

            clearMap();

            plotLocation();

        }



        private void plotLocation()
        {
            Pushpin locationPushpin = new Pushpin();
            locationPushpin.Location = watcher.Position.Location;

            locationPushpin.Tap += this.pin_Tap;

            locationPushpin.Content = new TextBlock();
            ((TextBlock)locationPushpin.Content).Text = "My Location";
            ((TextBlock)locationPushpin.Content).Visibility = Visibility.Collapsed;

            LyncUpMap.Children.Add(locationPushpin);
            LyncUpMap.SetView(watcher.Position.Location, 14);
        }

        private void CoffeeButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap();

            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.coffee
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }

        private void BarsButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap();

            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.bars
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }

        private void ShoppingButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap();

            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.shopping
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }

        private void AttractionsButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap();

            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.entertainment
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }

        private void ServiceButton_Click(object sender, RoutedEventArgs e)
        {
            clearMap();


            plotLocation();

            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.parks,
                    FourSquareAPICall.animalShelters,
                    FourSquareAPICall.cafeterias,
                    FourSquareAPICall.medicalCenters,
                    FourSquareAPICall.schools
	                }, LyncUpMap.Center, this
            );

            fs.GetVenues();
        }








        /// <summary>
        /// Varify that the Hawaii application Id is set correctly.
        /// </summary>
        private void VerifyHawaiiId()
        {
            if (string.IsNullOrEmpty(HawaiiClient.HawaiiApplicationId))
            {
                MessageBox.Show("The Hawaii Application Id is missing. In order to run this sample you need to obtain a Hawaii Application Id from http://hawaiiguidgen.cloudapp.net. Use that Id in the source file HawaiiClient.cs");
            }
        }


        private void ChatText_LayoutUpdated(object sender, EventArgs e)
        {
            SmashTable<Channels.ChatRecord> v = (SmashTable<Channels.ChatRecord>)ChatText.DataContext;

            if (v != null)
            {
                if (v.Count != 0)
                {
                    string newMessage = v[v.Count - 1].ChatEntry;
                    MessageBox.Show("new message: " + newMessage);
                    
                }
            }
        }



    }
}