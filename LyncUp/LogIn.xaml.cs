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
using System.Windows.Media.Imaging;
using Microsoft.Phone.UserData;

namespace LyncUp
{
    public partial class LogIn : PhoneApplicationPage
    {
        private LiveConnectClient client;
        private LiveConnectSession session;

        public LogIn()
        {
            InitializeComponent();


            Contacts cons = new Contacts();

            //Identify the method that runs after the asynchronous search completes.
            cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(Contacts_SearchCompleted);

            //Start the asynchronous search.
            cons.SearchAsync(String.Empty, FilterKind.None, "Contacts Test #1");
        }

        void Contacts_SearchCompleted(object sender, ContactsSearchEventArgs e)
        {
            //Do something with the results.
            //MessageBox.Show(e.Results.Count().ToString());

            IEnumerable<Contact> contacts = e.Results;
            foreach (Contact c in contacts)
            {

            }
        }

        private void btnSignin_SessionChanged(object sender, LiveConnectSessionChangedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                session = e.Session;
                client = new LiveConnectClient(session);
                infoTextBlock.Text = "Signed in.";
                client.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(OnGetCompleted);
                client.GetAsync("me/contacts", null);
            }
            else
            {
                infoTextBlock.Text = "Not signed in.";
                client = null;
            }

            //client.GetAsync("me/contacts");
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
                        "Hello, " + firstName + lastName + "!";
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
        }


    }
}