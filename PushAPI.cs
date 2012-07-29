using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;
using System.Collections;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace SmashSampleApp
{
    public static class PushAPI
    {
        private static ManualResetEvent allDone = new ManualResetEvent(false);

        static string Title;
        static string SubTitle;
        static IDictionary DataTable;
        static string Page;


        public static void SendToastToUser(String pushkey, string _Title, string _SubTitle, IDictionary _DataTable, string _Page)
        {
            if (pushkey != null)
            {
                Title = _Title;
                SubTitle = _SubTitle;
                DataTable = _DataTable;
                Page = _Page;

                string subscriptionUri = pushkey; //"http://sn1.notify.live.net/throttledthirdparty/01.00/AAG0DaCaDbqgTbX_ZhmiM1pMAgAAAAADAwAAAAQUZm52OkJCMjg1QTg1QkZDMkUxREQ";


                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(subscriptionUri);
                request.ContentType = "text/xml";
                request.Method = "POST";
                request.Headers["X-WindowsPhone-Target"] = "toast";
                request.Headers["X-NotificationClass"] = "2";

                request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);
            }

        }

        private static void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            string DataStr = "?";
            foreach (String key in DataTable.Keys)
            {
                DataStr = DataStr + key + "=" + DataTable[key] + "&amp;";
            }
            DataStr = DataStr.Remove(DataStr.Length - 5);

            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

            Stream postStream = request.EndGetRequestStream(asynchronousResult);

            string postData = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<wp:Notification xmlns:wp=\"WPNotification\">" +
                   "<wp:Toast>" +
                        "<wp:Text1>" + Title + "</wp:Text1>" +
                        "<wp:Text2>" + SubTitle + "</wp:Text2>" +
                        "<wp:Param>/" + Page + ".xaml" + DataStr + "</wp:Param>" +
                   "</wp:Toast> " +
                "</wp:Notification>";

            // Convert the string into a byte array.
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);



            // Write to the request stream.
            postStream.Write(byteArray, 0, postData.Length);
            postStream.Close();

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
        }

        private static void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

                // End the operation
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                Stream streamResponse = response.GetResponseStream();
                StreamReader streamRead = new StreamReader(streamResponse);
                string responseString = streamRead.ReadToEnd();
                Console.WriteLine(responseString);
                // Close the stream object
                streamResponse.Close();
                streamRead.Close();

                // Release the HttpWebResponse
                response.Close();
                allDone.Set();
            }
            catch (Exception e)
            {
                Debug.WriteLine("PUSH ERR: " + e.Message);
            }

        }
    }
}
