//
// VimeoApi.cs
//
// Authors:
//  Andrea Schiavinato <andrea.schiavinato84@gmail.com>
//
// Copyright (C) 2019 Andrea Schiavinato 
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace VimeoUploader
{
    class VimeoApi
    {
        private string authorization;

        #region Vimeo data structures
        public struct UserInfoQuotaSpace
        {
            public long free, max, used;
        }

        public struct UserInfoQuotaQuota
        {
            public bool hd, sd;
        }

        public struct UserInfoQuota
        {
            public UserInfoQuotaSpace space;
            public UserInfoQuotaQuota quota;
        }

        public struct UserMetadataConnectionsItem
        {
            public string uri;
            public List<string> options;
            public int total;
        }

        public struct UserMetadataConnections
        {
            public UserMetadataConnectionsItem activities;
            public UserMetadataConnectionsItem albums;
            public UserMetadataConnectionsItem channels;
            public UserMetadataConnectionsItem feed;
            public UserMetadataConnectionsItem followers;
            public UserMetadataConnectionsItem following;
            public UserMetadataConnectionsItem groups;
            public UserMetadataConnectionsItem likes;
            public UserMetadataConnectionsItem portfolios;
            public UserMetadataConnectionsItem videos;
            public UserMetadataConnectionsItem watchlater;
            public UserMetadataConnectionsItem shared;
            public UserMetadataConnectionsItem pictures;
        }

        public struct UserMetadata
        {
            public UserMetadataConnections connections;
        }

        public struct UserInfo
        {
            public string uri, name, link, created_time, account;
            public UserInfoQuota upload_quota;
            public UserMetadata metadata;
        }

        public struct VideoEntryPaging
        {
            public string next, previous, first, last;
        }

        public struct VideoEntryPrivacy
        {
            public string view, embed, comments;
            public bool download, add;
        }

        public struct VideoEntryStats
        {
            public int plays;
        }

        public struct VideoEntryData
        {
            public string uri, name, description, link, language, license, status, created_time, modified_time;
            public int duration, width, height;
            public VideoEntryPrivacy privacy;
            public VideoEntryStats stats;
        }

        public struct VideoEntry
        {
            public int total;
            public int page;
            public int per_page;
            public VideoEntryPaging paging;
            public List<VideoEntryData> data;
        }

        public struct UploadToken
        {
            public string uri, ticket_id, upload_link_secure, complete_uri;
        }

        public struct PictureUploadInfo
        {
            public string uri;
            public bool active;
            public string link;
        }
        #endregion

        //Callback to notify upload status
        public struct UploadStatus
        {
            public string range;
            public int bytesSent;

            public UploadStatus(string s)
            {
                range = s;
                bytesSent = Convert.ToInt32(s.Split('-')[1]);
            }
        }

        /// <param name="Token">Authorization token for Vimeo</param>
        public VimeoApi(string Token)
        {
            if (Token != "" && Token != null)            
                authorization = "bearer " + Token.Trim().ToLower();            
            else
                ReadAuth();
        }

        /// <summary> Get user videos </summary>          
        /// <returns>Structure containing the video list</returns>
        public VideoEntry GetVideos()
        {
            var uri = "https://api.vimeo.com/me/videos?fields=uri,name,status,created_time,modified_time";
            var res = DoGenericHttpCall("GET", uri, "", "", null, HttpStatusCode.OK);
            return JsonConvert.DeserializeObject<VideoEntry>(res,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });            
        }

        /// <summary> Get information about the Vimeo account </summary>  
        /// <param name="videoId">The ID of the video assigned by Vimeo</param> 
        /// <returns>Video status description</returns>
        public string GetVideoStatus(string videoId)
        {
            var uri = String.Format("https://api.vimeo.com/me/videos/{0}?fields=status", videoId);
            var res = DoGenericHttpCall("GET", uri, "", "", null, HttpStatusCode.OK);
            var resStruct = JsonConvert.DeserializeObject<VideoEntryData>(res,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return resStruct.status;
        }

        /// <summary> Get information about a video </summary>  
        /// <param name="videoId">The ID of the video assigned by Vimeo</param> 
        /// <returns>Structure containing the video information</returns>
        public VideoEntryData GetVideoDetails(string videoId)
        {
            var uri = String.Format("https://api.vimeo.com/me/videos/{0}", videoId);
            var res = DoGenericHttpCall("GET", uri, "", "", null, HttpStatusCode.OK);
            var resStruct = JsonConvert.DeserializeObject<VideoEntryData>(res,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return resStruct;
        }

        /// <summary> Get space available on Viemo </summary>  
        /// <returns> Structure containing user information, with only the fileds related to the quota valorized</returns>
        public UserInfo GetQuota()
        {
            var uri = "https://api.vimeo.com/me?fields=name,upload_quota";
            var res = DoGenericHttpCall("GET", uri, "", "", null, HttpStatusCode.OK);
            return JsonConvert.DeserializeObject<UserInfo>(res,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        /// <summary> Get information about the Vimeo account </summary>  
        /// <returns> Structure containing user information</returns>
        public UserInfo GetUserInfo()
        {
            var uri = "https://api.vimeo.com/me";
            var res = DoGenericHttpCall("GET", uri, "", "", null, HttpStatusCode.OK);
            return JsonConvert.DeserializeObject<UserInfo>(res,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private UploadToken GetUploadToken()
        {
            var uri = "https://api.vimeo.com/me/videos?type=streaming";
            var headers = new List<Tuple<string, string>>();
            headers.Add(new Tuple<string, string>("type", "streaming"));
            var res = DoGenericHttpCall("POST", uri, "", "", headers, HttpStatusCode.Created, true);
            return JsonConvert.DeserializeObject<UploadToken>(res,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private string UploadChunck(UploadToken token, string fileName, long fileSize, int offset, int bytesToUpload, Action<long> cb)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(token.upload_link_secure);
            r.Method = "PUT";
            r.ContentLength = fileSize;
            r.ContentType = "video/x-ms-wmv";
            r.KeepAlive = true;
            r.Timeout = System.Threading.Timeout.Infinite;
            r.ReadWriteTimeout = System.Threading.Timeout.Infinite;

            r.Headers[HttpRequestHeader.Authorization] = authorization;
            if (offset > 0)
                r.Headers[HttpRequestHeader.ContentRange] = String.Format("bytes {0}-{1}/{2}", offset, fileSize, fileSize);            

            var file = File.OpenRead(fileName);
            file.Seek(offset, SeekOrigin.Begin);

            const int BUFSIZE = 1048576;
            byte[] buffer = new byte[BUFSIZE];

            string s = "";
            try
            {
                using (Stream str = r.GetRequestStream())
                {
                    long bytesRead = 0;
                    while (bytesRead < fileSize)
                    {
                        int n = file.Read(buffer, 0, BUFSIZE);
                        if (n == 0) break;
                        str.Write(buffer, 0, n);
                        bytesRead += n;
                        cb(bytesRead);
                    }
                }
                file.Close();
                file.Dispose();
                file = null;
            }
            catch (WebException e)
            {
                s = e.Message + " - " + e.Status.ToString();
            }
            catch (IOException e)
            {
                s = e.Message;
            }
            return s;
        }

        private UploadStatus VerifyUpload(UploadToken token)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(token.upload_link_secure);
            r.Method = "PUT";
            r.ContentLength = 0;
            r.Headers[HttpRequestHeader.ContentRange] = "bytes */*";
            r.Headers[HttpRequestHeader.Authorization] = authorization;

            try
            {
                HttpWebResponse response = (HttpWebResponse)r.GetResponse();
            }
            catch (WebException e)
            {
                var s = e.Response.Headers["Range"];
                return new UploadStatus(s);
            }

            throw new Exception();
        }

        private string CompleteUpload(UploadToken token)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create("https://api.vimeo.com" + token.complete_uri);
            r.Method = "DELETE";
            r.Headers[HttpRequestHeader.Authorization] = authorization;

            HttpWebResponse response = (HttpWebResponse)r.GetResponse();
            var s = response.Headers["Location"];
            response.Close();
            return s.Replace("/videos/", "");
        }

        /// <summary> Upload a video</summary>  
        /// <param name="videoFileName">File name of the video</param>
        /// <param name="videoName">Name to set for the video</param>
        /// <param name="videoDescription">Description to set for the video</param>
        /// <param name="uploadCB">Callback function called every MB sent</param>
        /// <returns>Video ID assigned from Vimeo</returns>
        public string UploadVideo(string videoFileName, string videoName, string videoDescription, Action<long, long> uploadCB)
        {
            var tk = GetUploadToken();

            FileInfo finfo = new FileInfo(videoFileName);

            int bytesSent = 0;
            int bytesChunk;            
            while (bytesSent < finfo.Length)
            {
                bytesChunk = (int)finfo.Length - bytesSent;
                var s = UploadChunck(tk, videoFileName, finfo.Length, bytesSent, bytesChunk,
                    x => uploadCB(x, finfo.Length));
                var status = VerifyUpload(tk);
                bytesSent += status.bytesSent;                
            }
            finfo = null;
            var videoUri = CompleteUpload(tk);

            if (videoName != "" || videoDescription != "")
            {
                SetVideoMetadata(videoUri, videoName, videoDescription);
            }
            return videoUri;
        }

        /// <summary> Set the thumbnail for a given video </summary>  
        /// <param name="videoId">The ID of the video assigned by Vimeo</param>
        /// <param name="timeOffset">Time offset of the frame containing the preview image</param>
        public void SetPicture(string videoId, int timeOffset)
        {            
            string uri = string.Format("https://api.vimeo.com/videos/{0}/pictures", videoId);
            string PictureInfo = string.Format("time={0}&active=true", timeOffset);

            DoGenericHttpCall("POST", uri, "application/x-www-form-urlencoded", PictureInfo, null, HttpStatusCode.Created); 
        }

        private void SendPicture(string link, string fileName)
        {
            FileInfo finfo = new FileInfo(fileName);

            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(link);
            r.Method = "PUT";
            r.ContentLength = finfo.Length;
            //r.ContentType = "video/x-ms-wmv";
            r.KeepAlive = true;
            r.Timeout = System.Threading.Timeout.Infinite;
            r.ReadWriteTimeout = System.Threading.Timeout.Infinite;
            r.Headers[HttpRequestHeader.Authorization] = authorization;

            var file = File.OpenRead(fileName);            

            const int BUFSIZE = 1048576;
            byte[] buffer = new byte[BUFSIZE];

            string s = "";
            try
            {
                using (Stream str = r.GetRequestStream())
                {
                    long bytesRead = 0;
                    while (bytesRead < finfo.Length)
                    {
                        int n = file.Read(buffer, 0, BUFSIZE);
                        if (n == 0) break;
                        str.Write(buffer, 0, n);
                        bytesRead += n;                        
                    }
                }
                file.Close();
                file.Dispose();
            }
            catch (WebException e)
            {
                s = e.Message + " - " + e.Status.ToString();
            }
            catch (IOException e)
            {
                s = e.Message;
            }           
        }

        /// <summary> Set the thumbnail for a given video </summary>  
        /// <param name="videoId">The ID of the video assigned by Vimeo</param>
        /// /// <param name="fileName">Image file to use as thumbnail</param>
        public void SetPicture(string videoId, string fileName)
        {
            var uri = String.Format("https://api.vimeo.com/videos/{0}/pictures", videoId);
            var res = DoGenericHttpCall("POST", uri, "", "", null, HttpStatusCode.Created, true);
            var x = JsonConvert.DeserializeObject<PictureUploadInfo>(res,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            SendPicture(x.link, fileName);            
            uri = String.Format("https://api.vimeo.com{0}", x.uri);

            var t0 = Environment.TickCount;
            bool repeat = true;
            while (repeat)
            {
                try
                {
                    DoGenericHttpCall("PATCH", uri, "application/x-www-form-urlencoded", "active=true", null, HttpStatusCode.OK);
                    repeat = false;
                }
                catch (Exception e)
                {
                    repeat = ((Environment.TickCount - t0) < 10000);
                    if (repeat)
                        Thread.Sleep(1000);
                    else
                        throw e;
                }
            }
        }

        /// <summary> Set name and description for the specified video </summary>  
        /// <param name="videoId">The ID of the video assigned by Vimeo</param>
        /// <param name="name">The name to set </param>
        /// <param name="description">The description to set</param>
        public void SetVideoMetadata(string videoId, string name, string description)
        {            
            string uri = string.Format("https://api.vimeo.com/videos/{0}", videoId);
            string videoInfo = "";
            if (name != "") videoInfo = videoInfo + string.Format("name={0}", name);
            if (description != "") videoInfo = (videoInfo == "" ? "" : videoInfo + "&") + string.Format("description={0}", description);
            
            DoGenericHttpCall("PATCH", uri, "application/x-www-form-urlencoded", videoInfo, null, HttpStatusCode.OK); 
        }

        /// <summary> Delete the specified video </summary>  
        /// <param name="videoId">The ID of the video assigned by Vimeo</param>
        public void DeleteVideo(string videoId)
        {            
            string uri = string.Format("https://api.vimeo.com/videos/{0}", videoId);
            DoGenericHttpCall("DELETE", uri, "", "", null, HttpStatusCode.NoContent); 
        }

        private void ChekResponse(string uri, HttpStatusCode codeRecv, HttpStatusCode codeExp)
        {
            if (codeRecv != codeExp)
                throw new Exception(String.Format("Received {0} from {1}, expected {2}",
                    codeRecv, uri, codeExp));
        }

        private string DoGenericHttpCall(string method, string uri, string contentType, string body, 
            List<Tuple<string,string>> headers, HttpStatusCode expectedRes, bool readResponse = false)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(uri);
            r.Method = method;
            r.Headers[HttpRequestHeader.Authorization] = authorization;
            r.Accept = "application/vnd.vimeo.*+json;version=3.2";            
            if (headers != null)
            {
                foreach (var h in headers)
                    r.Headers[h.Item1] = h.Item2;
            }
            if (contentType != "")
                r.ContentType = contentType;
            if (body != "")
            {
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(body);
                using (Stream str = r.GetRequestStream())
                {
                    str.Write(bytes, 0, bytes.Length);
                    str.Close();
                }
            }

            HttpWebResponse response = (HttpWebResponse)r.GetResponse();
            var respCode = response.StatusCode;
            string respondeBody = "";
            if (readResponse || method == "GET")
            {
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                respondeBody = reader.ReadToEnd();
                reader.Close();
            }
            response.Close();
            ChekResponse(uri, respCode, expectedRes);
            return respondeBody;
        }

        /// <summary> Save authorization for Vimeo on the registry </summary>          
        public void SaveAuth()
        {
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("ViemoUploader");
            regKey.SetValue("authorization", authorization);
            regKey.Close();
        }

        /// <summary> Read authorization for Vimeo from the registry </summary>  
        public void ReadAuth()
        {
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("ViemoUploader");
            authorization = regKey.GetValue("authorization", authorization).ToString();
            regKey.Close();
        }
    }
}
