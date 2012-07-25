// ----------------------------------------------------------
// <copyright file="ChatRecord.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ----------------------------------------------------------

namespace Channels
{
    using System;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using Microsoft.Hawaii.Smash.Client;

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class ChatRecord : SmashRecordBase<ChatRecord>
    {
        /// <summary>
        /// 
        /// </summary>
        [IgnoreDataMember]
        private BitmapSource picture;

        /// <summary>
        /// 
        /// </summary>
        [IgnoreDataMember]
        private string text;

        /// <summary>
        /// 
        /// </summary>
        [IgnoreDataMember]
        private string sender;

        /// <summary>
        /// 
        /// </summary>
        [IgnoreDataMember]
        private string sentTime;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public ChatRecord(string text)
        {
            this.Text = text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="image"></param>
        public ChatRecord(string text, BitmapSource image)
        {
            this.Text = text;
            this.Picture = image;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="sentTime"></param>
        /// <param name="text"></param>
        public ChatRecord(string sender, string sentTime, string text)
        {
            this.Text = text;
            this.SentTime = sentTime;
            this.Sender = sender;
        }

        /// <summary>
        /// 
        /// </summary>
        [IgnoreDataMember]
        public string ChatEntry
        {
            get
            {
                return string.Format("{0} ({1}): {2}", this.Sender, this.SentTime, this.Text);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Text
        {
            get
            {
                return this.text;
            }

            set
            {
                this.OnChange();
                this.text = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Sender
        {
            get
            {
                return this.sender;
            }

            set
            {
                this.OnChange();
                this.sender = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string SentTime
        {
            get
            {
                return this.sentTime;
            }

            set
            {
                this.OnChange();
                this.sentTime = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public byte[] ImageBytes
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:We need to save the image here", Justification = "Image needs to be serialized here")]
            get
            {
                byte[] result = null;
                if (this.Picture != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
#if TARGET_DESKTOP
                        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                        WriteableBitmap bmp = new WriteableBitmap(this.Picture);
                        encoder.Frames.Add(BitmapFrame.Create(bmp));
                        encoder.Save(ms);
#else
                            WriteableBitmap bmp = new WriteableBitmap(this.Picture);
                            bmp.SaveJpeg(ms, bmp.PixelWidth, bmp.PixelHeight, 0, 85);
#endif
                        result = ms.ToArray();
                    }
                }

                return result;
            }

            set
            {
                if (value != null)
                {
                    using (MemoryStream ms = new MemoryStream(value))
                    {
#if TARGET_DESKTOP
                        JpegBitmapDecoder decoder = new JpegBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        this.Picture = decoder.Frames[0];
#else
                            WriteableBitmap bmp = Microsoft.Phone.PictureDecoder.DecodeJpeg(ms);
                            this.Picture = bmp;
#endif
                    }
                }
                else
                {
                    this.Picture = null;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource Picture
        {
            get
            {
                return this.picture;
            }

            set
            {
                this.OnChange();
                this.picture = value;
            }
        }
    }
}
