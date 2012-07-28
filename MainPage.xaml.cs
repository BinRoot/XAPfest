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
    using Microsoft.Phone.Notification;
    using System.Text;
    using System.IO.IsolatedStorage;
    using System.Windows.Media.Imaging;

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
        private string ManagementID = "0B797E34-905A-406D-B8CF-F57DC6EB083A";

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

        private bool InSetupMode { 
            get
            {
                bool setupMode = false;
                try
                {
                    setupMode = (Boolean)settings["setupMode"];
                }
                catch (KeyNotFoundException knfe)
                {
                    setupMode = true;
                }
                return setupMode;
            }
        }

        Dictionary<String, GeoCoordinate> friendMap = new Dictionary<string,GeoCoordinate>();
        GeoCoordinateWatcher watcher;
        IDataService DS;

        // MY GOD THIS IS GOING TO NEED REFACTORING!!!
        public MainPage(bool createNewEvent)
        {
            this.InitializeComponent();

            this.VerifyHawaiiId();

            ManagementID = Guid.NewGuid().ToString();



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

        public MainPage()
        {
            this.InitializeComponent();

            this.VerifyHawaiiId();

            DataUse.Instance.ActiveFriends = new List<Friend>();

            if (InSetupMode == false)
            {
                setUpDone();
            }
            else
            {
                ApplicationBar.IsVisible = false;
            }

            FinalizeList.DataContext = DataUse.Instance.ActiveFriends;

            //TestImage.Source = new BitmapImage(new Uri("https://apis.live.net/v5.0/" + DataUse.Instance.MyUserId + "/picture", UriKind.Absolute));
            
            //this.SendText.Click += new RoutedEventHandler(this.SendText_Click);
            //this.Join.Click += new RoutedEventHandler(this.Join_Click);
            //this.Create.Click += new RoutedEventHandler(this.Create_Click);

            ManagementID = Guid.NewGuid().ToString();

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


            #region [PUSH]
            /// Holds the push channel that is created or found.
            HttpNotificationChannel pushChannel;

            // The name of our push channel.
            string channelName = "ToastSampleChannel";

            // Try to find the push channel.
            pushChannel = HttpNotificationChannel.Find(channelName);

            // If the channel was not found, then create a new connection to the push service.
            if (pushChannel == null)
            {
                pushChannel = new HttpNotificationChannel(channelName);

                // Register for all the events before attempting to open the channel.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                pushChannel.Open();

                // Bind this new channel for toast events.
                pushChannel.BindToShellToast();

            }
            else
            {
                // The channel was already open, so just register for all the events.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                // Display the URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
                // MessageBox.Show(String.Format("Channel Uri is {0}", pushChannel.ChannelUri.ToString()));
                DataUse.Instance.DS.UpdatePushkey(DataUse.Instance.MyUserId, pushChannel.ChannelUri.ToString());
            }
            #endregion

        }



        #region [PUSH]
        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Display the new URI for testing purposes.   Normally, the URI would be passed back to your web service at this point.
                System.Diagnostics.Debug.WriteLine(e.ChannelUri.ToString());
                //MessageBox.Show(String.Format("Channel Uri is {0}", e.ChannelUri.ToString()));
                DataUse.Instance.DS.UpdatePushkey(DataUse.Instance.MyUserId, e.ChannelUri.ToString());

            });
        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic for your particular application would be here.
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
                    );
        }

        void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            StringBuilder message = new StringBuilder();
            string relativeUri = string.Empty;

            message.AppendFormat("Received Toast {0}:\n", DateTime.Now.ToShortTimeString());

            // Parse out the information that was part of the message.
            foreach (string key in e.Collection.Keys)
            {
                message.AppendFormat("{0}: {1}\n", key, e.Collection[key]);

                if (string.Compare(
                    key,
                    "wp:Param",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    relativeUri = e.Collection[key];

                    if (relativeUri.Contains("InvitationPage.xaml"))
                    {
                        Uri uri = new Uri(relativeUri, UriKind.Relative);
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => { NavigationService.Navigate(uri); });
                        return;
                    }
                    


                    string[] dataParts = (relativeUri.Split('?')[1]).Split('&');

                    string Status = "";
                    string FriendId = "";
                    for (int i = 0; i < dataParts.Length; i++)
                    {
                        string[] keyval = dataParts[i].Split('=');
                        if (keyval[0] == "friendacceptid")
                        {
                            FriendId = keyval[1];
                        }
                        else if (keyval[0] == "friendstatus")
                        {
                            Status = keyval[1];
                        }
                    }

                    if (Status == "yes")
                    {
                        for (int i = 0; i < DataUse.Instance.ActiveFriends.Count; i++)
                        {
                            Friend f1 = DataUse.Instance.ActiveFriends[i];
                            if (f1.id == FriendId)
                            {
                                DataUse.Instance.ActiveFriends[i].status = "yes";
                            }
                        }

                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            PeopleList.DataContext = null;
                            PeopleList.DataContext = DataUse.Instance.ActiveFriends;
                        });


                    }
                }
            }

            // Display a dialog of all the fields in the toast.
            // Dispatcher.BeginInvoke(() => MessageBox.Show(message.ToString()));

        }
        #endregion





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


            FinalizeList.DataContext = null;
            FinalizeList.DataContext = DataUse.Instance.ActiveFriends;
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


        #region [MAP]
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

            //TODO: remove this dummy test code
            setUpDone();
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
                venuePushpin.Background = new SolidColorBrush(Colors.Blue);
                venuePushpin.Opacity = 0.6;
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


            DataUse.Instance.MessageToSend = DataUse.Instance.MyUserId + "," + e.Position.Location.Latitude + "," + e.Position.Location.Longitude;
            if (DataUse.Instance.RoomCreated)
            {
                SendText(DataUse.Instance.MyUserId + "," + e.Position.Location.Latitude + "," + e.Position.Location.Longitude);
            }
            
        }

        private void drawCircle(GeoCoordinate center, double radius)
        {
            MapPolygon polygon = new MapPolygon();
            polygon.Fill = new SolidColorBrush(Colors.Green);
            polygon.Stroke = new SolidColorBrush(Colors.Blue);
            polygon.StrokeThickness = 2;
            polygon.Opacity = .1;

            polygon.Visibility = Visibility.Collapsed;
            if (watcher.Position.Location == null)
                return;

            polygon.Visibility = Visibility.Visible;
            polygon.Locations = new LocationCollection();

            var earthRadius = 6371;
            var lat = center.Latitude * Math.PI / 180.0; //radians
            var lon = center.Longitude * Math.PI / 180.0; //radians
            var d = radius / 1000 / earthRadius; // d = angular distance covered on earths surface

            for (int x = 0; x <= 360; x++)
            {
                var brng = x * Math.PI / 180.0; //radians
                var latRadians = Math.Asin(Math.Sin(lat) * Math.Cos(d) + Math.Cos(lat) * Math.Sin(d) * Math.Cos(brng));
                var lngRadians = lon + Math.Atan2(Math.Sin(brng) * Math.Sin(d) * Math.Cos(lat), Math.Cos(d) - Math.Sin(lat) * Math.Sin(latRadians));



                var pt = new GeoCoordinate(180.0 * latRadians / Math.PI, 180.0 * lngRadians / Math.PI);
                polygon.Locations.Add(pt);
            }

            LyncUpMap.Children.Add(polygon);
        }



        private void plotLocation()
        {
            LyncUpMap.SetView(watcher.Position.Location, 10);
            List<GeoCoordinate> friendLocations = new List<GeoCoordinate>();

            try
            {
                double averageLat = 0.0;
                double averageLong = 0.0;

                foreach (var friend in DataUse.Instance.ActiveFriends)
                {
                    GeoCoordinate loc = friendMap[friend.id];
                    friendLocations.Add(loc);

                    averageLat += loc.Latitude;
                    averageLong += loc.Longitude;

                    Pushpin locationPushpin = new Pushpin();
                    locationPushpin.Background = new SolidColorBrush(Colors.Green);
                    locationPushpin.Location = watcher.Position.Location;

                    locationPushpin.Tap += this.pin_Tap;

                    locationPushpin.Content = new TextBlock();
                    ((TextBlock)locationPushpin.Content).Text = friend.name;
                    ((TextBlock)locationPushpin.Content).Visibility = Visibility.Collapsed;

                    LyncUpMap.Children.Add(locationPushpin);
                }

                //Pick up radius from settings
                if (!(averageLat == 0.0) && !(averageLong == 0.0))
                {
                    drawCircle(new GeoCoordinate(averageLat / friendLocations.Count, averageLong / friendLocations.Count), 3218.69); 
                }
            }
            catch (Exception)
            {
                MessageBox.Show("shit");
            }
        }

        #endregion

        private void FoodButton_Click(object sender, RoutedEventArgs e)
        {
            FourSquareAPICall fs = new FourSquareAPICall(800, new List<string>()
                    {
	                FourSquareAPICall.food
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
                    
                    string [] strSplit = newMessage.Split(',');
                    if (strSplit.Length == 3)
                    {
                        // MessageBox.Show("msg received: " + newMessage);
                        string friendid = strSplit[0].Replace("(): ", "").TrimStart();
                        string latStr = strSplit[1];
                        string lonStr = strSplit[2];

                        try 
                        {
                            double latd = double.Parse(latStr);
                            double lond = double.Parse(lonStr);
                            GeoCoordinate g = new GeoCoordinate(latd, lond);

                            GeoCoordinate oldLoc = friendMap[friendid];
                            if (oldLoc != g)
                            {
                                friendMap[friendid] = g;
                                friendLocationUpdated();
                            }

                            
                        }
                        catch 
                        {
                            MessageBox.Show("err, couldn't parse double or something");
                        }
                        
                    }
                    else
                    {
                        MessageBox.Show("msg received, but incorrect format: " + newMessage);
                    }
                }
            }
        }

        private void setUpDone()
        {
            AddOrUpdateSettings("setupMode", false);
            ApplicationBar.IsVisible = true;
            MainPanorama.Visibility = Visibility.Collapsed;
            MainMap.Visibility = Visibility.Visible;

        }

        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
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


        private void friendLocationUpdated()
        {
            LyncUpMap.SetView(watcher.Position.Location, 10);
            List<GeoCoordinate> friendLocations = new List<GeoCoordinate>();

            try
            {
                double averageLat = 0.0;
                double averageLong = 0.0;

                foreach (var friend in DataUse.Instance.ActiveFriends)
                {
                    GeoCoordinate loc = friendMap[friend.id];
                    friendLocations.Add(loc);

                    averageLat += loc.Latitude;
                    averageLong += loc.Longitude;

                    Pushpin locationPushpin = new Pushpin();
                    locationPushpin.Background = new SolidColorBrush(Colors.Green);
                    locationPushpin.Location = watcher.Position.Location;

                    locationPushpin.Tap += this.pin_Tap;

                    locationPushpin.Content = new TextBlock();
                    ((TextBlock)locationPushpin.Content).Text = friend.name;
                    ((TextBlock)locationPushpin.Content).Visibility = Visibility.Collapsed;

                    LyncUpMap.Children.Add(locationPushpin);
                }

                //Pick up radius from settings
                if (!(averageLat == 0.0) && !(averageLong == 0.0))
                {
                    drawCircle(new GeoCoordinate(averageLat / friendLocations.Count, averageLong / friendLocations.Count), 3218.69);
                }
            }
            catch (Exception)
            {
                // MessageBox.Show("shit2");
            }
        }

        private void Leave_MenuItem_Click(object sender, EventArgs e)
        {
            AddOrUpdateSettings("setupMode", true);
            MainPanorama.Visibility = Visibility.Visible;
            MainMap.Visibility = Visibility.Collapsed;
            ApplicationBar.IsVisible = false;

        }

    }
}