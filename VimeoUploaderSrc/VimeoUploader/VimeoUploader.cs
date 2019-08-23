//
// VimeoUploader.cs
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
using Mono.Options;
using System.IO;
using System.Threading;

namespace VimeoUploader
{
    class VimeoUploader
    {
        private const string CMD_LV = "LV";
        private const string CMD_QUOTA = "QUOTA";
        private const string CMD_UPLOAD = "UPLOAD";
        private const string CMD_DELETE = "DEL";
        private const string CMD_EDT = "EDT";
        private const string CMD_SETPIC = "SETPIC";
        private const string CMD_USERINFO = "UINFO";
        private const string CMD_VIDEOINFO = "VINFO";
        private const string CMD_STATUS = "VSTATUS";
        private const string CMD_SAVETOKEN = "ST";
        private const string CMD_PULLPLD = "PULLFD";
        private const string CMD_HELP = "HELP";
        private static string[] availableCommands = new string[] { CMD_LV, CMD_QUOTA, CMD_UPLOAD, CMD_DELETE, CMD_EDT, CMD_SETPIC, CMD_USERINFO, CMD_VIDEOINFO, CMD_STATUS, CMD_SAVETOKEN, CMD_PULLPLD, CMD_HELP };
        
        private static string[] videoExtensions = new string[] 
            {".AVI", ".WMV", ".MOV", ".MP4"};

        struct ProgramOptions
        {
            public string Command;
            public string Token;
            public string FileVideo;
            public string FilePicture;
            public string VideoId;
            public string Name;
            public string Description;
            public int TimePicture;
            public string CheckFolder;
            public string DestFolder;
            public int CheckInterval;
        }

        static ProgramOptions ReadOptions(string[] args)
        {
            if (args.Count() == 0)
                throw new Exception("Invalid sintax\nRun \"VimeoUploader HELP\" for more information");
            ProgramOptions res = new ProgramOptions();
            OptionSet p = new OptionSet()
              .Add("t=|token=", s => res.Token = s)
              .Add("f=|file=", s => res.FileVideo = s)
              .Add("pf=|picfile=", s => res.FilePicture = s)
              .Add("ptime=|pictime=", s => res.TimePicture = Convert.ToInt32(s))
              .Add("v=|video=", s => res.VideoId = s)
              .Add("n=|name=", s => res.Name = s)
              .Add("d=|desciption=", s => res.Description = s)
              .Add("cf=|checkFld=", s => res.CheckFolder = s)
              .Add("df=|destFld=", s => res.DestFolder = s)
              .Add("ci=|checkInt=", s => res.CheckInterval = Convert.ToInt32(s));            
            var x = p.Parse(args);
            res.Command = x[0].ToUpper();
            if (!availableCommands.Contains(res.Command))
                throw new Exception(String.Format("Invalid command {0}\nRun \"VimeoUploader HELP\" for more information", res.Command));
            return res;
        }

        private static void displayUserInfoMetadata(string itemName, VimeoApi.UserMetadataConnectionsItem item)
        {                        
            Console.WriteLine("{0}\tTotal: {3}", 
                itemName, item.uri, String.Concat(item.options), item.total);
        }

        private static void displayUserInfo(VimeoApi.UserInfo userInfo, bool displayJustQuota)
        {
            Console.WriteLine("User: {0}", userInfo.name);

            if (!displayJustQuota)
            {
                Console.WriteLine("Uri: {0}", userInfo.uri);
                Console.WriteLine("Link: {0}", userInfo.link);
                Console.WriteLine("Ceated time: {0}", userInfo.created_time);
                Console.WriteLine("Account: {0}", userInfo.account);
                displayUserInfoMetadata("Activities", userInfo.metadata.connections.activities);
                displayUserInfoMetadata("Albums", userInfo.metadata.connections.albums);
                displayUserInfoMetadata("Channels", userInfo.metadata.connections.channels);
                displayUserInfoMetadata("Feed", userInfo.metadata.connections.feed);
                displayUserInfoMetadata("Followers", userInfo.metadata.connections.followers);
                displayUserInfoMetadata("Following", userInfo.metadata.connections.following);
                displayUserInfoMetadata("Groups", userInfo.metadata.connections.groups);
                displayUserInfoMetadata("Likes", userInfo.metadata.connections.likes);
                displayUserInfoMetadata("Pictures", userInfo.metadata.connections.pictures);
                displayUserInfoMetadata("Portfolios", userInfo.metadata.connections.portfolios);
                displayUserInfoMetadata("Shared", userInfo.metadata.connections.shared);
                displayUserInfoMetadata("Videos", userInfo.metadata.connections.videos);
                displayUserInfoMetadata("Watchlater", userInfo.metadata.connections.watchlater);
            }

            Console.WriteLine("Upload quota");
            Console.WriteLine("\tSpace");
            Console.WriteLine("\t\tFree: {0}", userInfo.upload_quota.space.free);
            Console.WriteLine("\t\tMax: {0}", userInfo.upload_quota.space.max);
            Console.WriteLine("\t\tUsed: {0}", userInfo.upload_quota.space.used);
            Console.WriteLine("\tQuota");
            Console.WriteLine("\t\tHd: {0}", userInfo.upload_quota.quota.hd.ToString());
            Console.WriteLine("\t\tSd: {0}", userInfo.upload_quota.quota.sd.ToString());
        }



        private static void displayVideoDetail(VimeoApi.VideoEntryData videoEntryData)
        {
            Console.WriteLine("Uri: {0}", videoEntryData.uri);
            Console.WriteLine("Name: {0}", videoEntryData.name);
            Console.WriteLine("Description: {0}", videoEntryData.description);
            Console.WriteLine("Link: {0}", videoEntryData.link);
            Console.WriteLine("Duration: {0}", videoEntryData.duration);
            Console.WriteLine("Width: {0}", videoEntryData.width);
            Console.WriteLine("Height: {0}", videoEntryData.height);
            Console.WriteLine("Language: {0}", videoEntryData.language);

            Console.WriteLine("Created time: {0}", videoEntryData.created_time);
            Console.WriteLine("Modified time: {0}", videoEntryData.modified_time);
            Console.WriteLine("Status: {0}", videoEntryData.status);
            Console.WriteLine("License: {0}", videoEntryData.license);

            Console.WriteLine("Privacy");
            Console.WriteLine("\tView: {0}", videoEntryData.privacy.view);
            Console.WriteLine("\tEmbed: {0}", videoEntryData.privacy.embed);
            Console.WriteLine("\tDownload: {0}", videoEntryData.privacy.download);
            Console.WriteLine("\tAdd: {0}", videoEntryData.privacy.add);
            Console.WriteLine("\tComments: {0}", videoEntryData.privacy.comments);


            Console.WriteLine("Stats");
            Console.WriteLine("\tPlays: {0}", videoEntryData.stats.plays);
        }

        private static void displayVideos(VimeoApi.VideoEntry videos)
        {
            Console.WriteLine("{0}\t{1}\t{2}", "uri", "name", "created_time");
            foreach (var v in videos.data)           
                Console.WriteLine("{0}\t{1}\t{2}", v.uri, v.name, v.created_time);            
            Console.WriteLine("\n\t {0} videos ", videos.total);
        }

        static void Main(string[] args)
        {
            try
            {
                ProgramOptions opt = ReadOptions(args);

                if (opt.Command == CMD_HELP)
                {
                    Console.WriteLine(VimeoUploaderRes.About);
                    return;
                }

                VimeoApi vimeo = new VimeoApi(opt.Token);

                if (opt.Command == CMD_LV)
                {
                    Console.WriteLine("Getting list of uploaded videos");
                    displayVideos(vimeo.GetVideos());
                }

                else if (opt.Command == CMD_VIDEOINFO)
                {
                    Console.WriteLine("Getting information of video {0}", opt.VideoId);
                    displayVideoDetail(vimeo.GetVideoDetails(opt.VideoId));
                }

                else if (opt.Command == CMD_STATUS)
                {
                    Console.WriteLine("Getting status of video {0}", opt.VideoId);
                    Console.WriteLine(vimeo.GetVideoStatus(opt.VideoId));
                }

                else if (opt.Command == CMD_USERINFO)
                {
                    Console.WriteLine("Getting user information");
                    displayUserInfo(vimeo.GetUserInfo(), false);
                }

                else if (opt.Command == CMD_QUOTA)
                {
                    Console.WriteLine("Getting quota information");
                    displayUserInfo(vimeo.GetQuota(), true);
                }

                else if (opt.Command == CMD_UPLOAD)
                {
                    UploadVideo(vimeo, opt.FileVideo, opt.Name, opt.Description);
                    Console.WriteLine("Done");
                }

                else if (opt.Command == CMD_DELETE)
                {
                    Console.WriteLine("Are you sure to delete video {0}?", opt.VideoId);
                    Console.Write("Press Y to confirm: ");
                    var x = Console.ReadKey();
                    if (Char.ToUpper(x.KeyChar) == 'Y')
                    {
                        Console.WriteLine("\nDeleting video {0}", opt.VideoId);
                        vimeo.DeleteVideo(opt.VideoId);
                        Console.WriteLine("Done");
                    }
                }

                else if (opt.Command == CMD_EDT)
                {
                    Console.WriteLine("Setting information for video {0}", opt.VideoId);
                    vimeo.SetVideoMetadata(opt.VideoId, opt.Name, opt.Description);
                    Console.WriteLine("Done");
                }

                else if (opt.Command == CMD_SETPIC)
                {
                    Console.WriteLine("Setting picture for video {0}", opt.VideoId);
                    if (opt.FilePicture != null || opt.FilePicture != "")
                        vimeo.SetPicture(opt.VideoId, opt.FilePicture);
                    else
                        vimeo.SetPicture(opt.VideoId, opt.TimePicture);
                    Console.WriteLine("Done");
                }

                else if (opt.Command == CMD_SAVETOKEN)
                {
                    vimeo.SaveAuth();
                    Console.WriteLine("Done");
                }

                else if (opt.Command == CMD_PULLPLD)
                {
                    if (opt.CheckInterval == null || opt.CheckInterval == 0)
                        opt.CheckInterval = 5000;
                    Console.WriteLine("Monitor folder: {0} - Destination Folder: {1}", opt.CheckFolder, opt.DestFolder);
                    while (true)
                    {
                        MonitorFld(vimeo, opt.CheckFolder, opt.DestFolder);
                        Thread.Sleep(opt.CheckInterval);
                    }
                }
                
            }
            catch (Exception e) 
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
            return;
        }

        private static string UploadVideo(VimeoApi vimeo, string videoFile, string videoName, string videoDescr)
        {
            Console.WriteLine("Uploading {0}", videoFile);
            long t0 = Environment.TickCount;
            long lastt = t0;
            long lastb = 0;            
            var s = vimeo.UploadVideo(videoFile, videoName, videoDescr,
                (n1, n2) => 
                {
                    if (n1 - lastb > 0.1)
                    {
                        var speed = ((double)(n1 - lastb) / (Environment.TickCount - lastt)) / 1000;
                        var tremaining = Math.Truncate((n2 - n1) / (1048576 * speed));
                        Console.WriteLine("Sent: {0}MB - Total: {1}MB - Speed: {2:0.##}MB/Sec - Remaining: {3}",
                        Math.Truncate((decimal)n1 / 1048576),
                        Math.Truncate((decimal)n2 / 1048576),
                        speed,
                        String.Format("{0:00}:{1:00}:{2:00}", tremaining / 3600, (Math.Floor(tremaining / 60)) % 60, tremaining % 60));
                        lastb = n1;
                        lastt = Environment.TickCount;
                    }
                });

            Console.WriteLine("Upload completed in {0}Sec", Math.Truncate((decimal)Environment.TickCount - t0) / 1000);
            Console.WriteLine("Video Uploaded: {0}", s);

            return s;
        }

        private static void MonitorFld(VimeoApi vimeo, string ChkFld, string DestFld)
        {
            var fs = Directory.GetFiles(ChkFld);
            foreach (var f in fs)
            {
                if (videoExtensions.Contains(Path.GetExtension(f).ToUpper()))
                {
                    string vname = Path.GetFileNameWithoutExtension(f);
                    var videoId = UploadVideo(vimeo, f, vname, "");
                                        
                    string imm = fs.First(s =>
                        Path.GetFileNameWithoutExtension(s) == vname && f != s && Path.GetExtension(s).ToUpper() == ".JPG"
                        );
                    if (imm != null || imm != "")
                    {
                        Thread.Sleep(1000);
                        Console.WriteLine("Setting picture");   
                        vimeo.SetPicture(videoId, imm);
                    }
                     
                    Console.WriteLine("Moving file {0}", Path.GetFileName(f));
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    File.Move(f, DestFld + "\\" + Path.GetFileName(f));
                    if (imm != null || imm != "")
                        File.Move(imm, DestFld + "\\" + Path.GetFileName(imm));
                    Console.WriteLine("Done");
                }
            }
        }
    }
}


