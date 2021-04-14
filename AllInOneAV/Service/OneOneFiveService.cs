﻿using DataBaseManager.ScanDataBaseHelper;
using Model.OneOneFive;
using Model.ScanModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace Service
{
    public class OneOneFiveService
    {
        private static readonly string FinFolder = "fin\\";
        private static readonly string UpFolder = "up115\\";

        public static bool Get115SearchResult(CookieContainer cc, string content, string host = "115.com", string reffer = "https://115.com/?cid=0&offset=0&mode=wangpan", string ua = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36 115Browser/12.0.0")
        {
            bool ret = false;

            var url = string.Format(string.Format("https://webapi.115.com/files/search?search_value={0}&format=json", content));
            var htmlRet = HtmlManager.GetHtmlWebClient("https://webapi.115.com", url, cc);
            if (htmlRet.Success)
            {
                if (!string.IsNullOrEmpty(htmlRet.Content))
                {
                    var data = Newtonsoft.Json.Linq.JObject.Parse(htmlRet.Content);

                    if (data.Property("count").HasValues && int.Parse(data.Property("count").Value.ToString()) > 0)
                    {
                        ret = true;
                    }
                }
            }

            return ret;
        }

        public static bool Get115SearchResultInFinFolder(CookieContainer cc, string content, string host = "115.com", string reffer = "https://115.com/?cid=0&offset=0&mode=wangpan", string ua = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36 115Browser/12.0.0")
        {
            bool ret = false;

            var url = string.Format(string.Format("https://webapi.115.com/files/search?search_value={0}&format=json", content));
            var htmlRet = HtmlManager.GetHtmlWebClient("https://115.com", url, cc);
            if (htmlRet.Success)
            {
                if (!string.IsNullOrEmpty(htmlRet.Content))
                {
                    var data = Newtonsoft.Json.Linq.JObject.Parse(htmlRet.Content);

                    if (data.Property("count").HasValues && int.Parse(data.Property("count").Value.ToString()) > 0 && data.Property("data").FirstOrDefault().FirstOrDefault().Value<string>("dp").ToUpper() == "FIN")
                    {
                        ret = true;
                    }
                }
            }

            return ret;
        }

        public static FileListModel Get115SearchResultInFolder(CookieContainer cc, string content, string host = "115.com", string reffer = "https://115.com/?cid=0&offset=0&mode=wangpan", string ua = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36 115Browser/12.0.0")
        {
            FileListModel ret = new FileListModel();

            var url = string.Format(string.Format("https://webapi.115.com/files/search?search_value={0}&format=json&offset=0&limit=10&&date=&aid=1&cid=0", content));
            var htmlRet = HtmlManager.GetHtmlWebClient("https://115.com", url, cc);
            if (htmlRet.Success)
            {
                if (!string.IsNullOrEmpty(htmlRet.Content))
                {
                    ret = JsonConvert.DeserializeObject<FileListModel>(htmlRet.Content);
                }
            }

            return ret;
        }

        public static bool Get115SearchExist(CookieContainer cc, string content, string host = "115.com", string reffer = "https://115.com/?cid=0&offset=0&mode=wangpan", string ua = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36 115Browser/12.0.0")
        {
            bool ret = false;

            var url = string.Format(string.Format("https://webapi.115.com/files/search?search_value={0}&format=json&limit=3&offset=0", content));
            var htmlRet = HtmlManager.GetHtmlWebClient("https://webapi.115.com", url, cc);
            if (htmlRet.Success)
            {
                if (!string.IsNullOrEmpty(htmlRet.Content))
                {
                    var data = Newtonsoft.Json.Linq.JObject.Parse(htmlRet.Content);

                    if (data.Property("count").HasValues && int.Parse(data.Property("count").Value.ToString()) == 1)
                    {
                        ret = true;
                    }
                }
            }

            return ret;
        }

        public static ValueTuple<bool, string> Add115MagTask(string cookieStr, string mag, string uid, string sign, string host = "115.com", string reffer = "https://115.com/?cid=1835025974666577373&offset=0&tab=download&mode=wangpan", string ua = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36 115Browser/12.0.0")
        {
            bool ret = false;
            string msg = "";

            CookieContainer cc = Get115Cookie();

            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("url", mag);
            param.Add("sign", sign);
            param.Add("uid", uid);
            param.Add("time", DateTime.Now.ToFileTimeUtc() + "");

            var returnStr = HtmlManager.Post("https://115.com/web/lixian/?ct=lixian&ac=add_task_url", param, cc);

            if (!string.IsNullOrEmpty(returnStr))
            {
                var data = Newtonsoft.Json.Linq.JObject.Parse(returnStr);

                bool.TryParse(data.Property("state").Value.ToString(), out ret);

                if (ret == false)
                {
                    msg = data.Property("error_msg").Value.ToString();
                }
            }

            return new ValueTuple<bool, string>(ret, msg);
        }

        public static CookieContainer Get115Cookie()
        {
            CookieContainer cc = new CookieContainer();

            var cookieData = new ChromeCookieReader().ReadCookies(".115.com");

            foreach (var item in cookieData.Where(x => !x.Value.Contains(",")).Distinct())
            {
                if (item.Name == "PHPSESSID" || item.Name == "UID" || item.Name == "CID" || item.Name == "SEID" || item.Name == "115_lang")
                {
                    Cookie c = new Cookie(item.Name, item.Value, "/", "115.com");
                    cc.Add(c);
                }
            }

            cookieData = new ChromeCookieReader().ReadCookies("webapi.115.com");

            foreach (var item in cookieData.Where(x => !x.Value.Contains(",")).Distinct())
            {
                Cookie c = new Cookie(item.Name, item.Value, "/", "115.com");
                cc.Add(c);
            }

            return cc;
        }

        public static int Get115PagesInFolder(int pageSize = 1, string folder = "1834397846621504875")
        {
            var url = $"https://webapi.115.com/files?aid=1&cid={folder}&o=user_ptime&asc=0&offset=0&show_dir=1&limit={pageSize}&code=&scid=&snap=0&natsort=1&record_open_time=1&source=&format=json";
            var cc = Get115Cookie();

            var htmlRet = HtmlManager.GetHtmlWebClient("https://webapi.115.com", url, cc);
            if (htmlRet.Success)
            {
                if (!string.IsNullOrEmpty(htmlRet.Content))
                {
                    var data = JsonConvert.DeserializeObject<FileListModel>(htmlRet.Content);

                    if (data != null && data.count > 0)
                    {
                        if (data.data == null)
                        {
                            return data.count % pageSize == 0 ? data.count / pageSize : data.count / pageSize + 1;
                        }

                        return data.count % data.page_size == 0 ? data.count / data.page_size : data.count / data.page_size + 1;
                    }
                }
            }

            return 0;
        }

        public static List<FileItemModel> Get115Files(int start = 0, int end = 0, int pageSize = 1150)
        {
            List<FileItemModel> ret = new List<FileItemModel>();

            var cc = Get115Cookie();
            for (int i = start; i <= end; i++)
            {
                var url = string.Format(@"https://webapi.115.com/files?aid=1&cid=1834397846621504875&o=user_ptime&asc=0&offset={0}&show_dir=1&limit={1}&code=&scid=&snap=0&natsort=1&record_open_time=1&source=&format=json", i * pageSize, pageSize);

                var htmlRet = HtmlManager.GetHtmlWebClient("https://115.com", url, cc);
                if (htmlRet.Success)
                {
                    if (!string.IsNullOrEmpty(htmlRet.Content))
                    {
                        var data = JsonConvert.DeserializeObject<FileListModel>(htmlRet.Content);

                        if (data != null && data.data != null)
                        {
                            foreach (var d in data.data)
                            {
                                if (!ret.Exists(x => x.sha == d.sha))
                                {
                                    ret.Add(d);
                                }
                            }
                        }
                    }
                }
            }

            return ret;
        }

        public static Dictionary<string, List<FileItemModel>> GetRepeatFiles(int pageSize = 1)
        {
            Dictionary<string, List<FileItemModel>> ret = new Dictionary<string, List<FileItemModel>>();
            var pattern = @"\(\d+\)";
            var data = OneOneFiveService.Get115Files(0, OneOneFiveService.Get115PagesInFolder(pageSize), pageSize);

            var retRepeat = data.Where(x => Regex.IsMatch(x.n, pattern)).ToList();

            foreach (var repeat in retRepeat)
            {
                var ori = Regex.Replace(repeat.n, pattern, "");

                if (!ret.ContainsKey(ori))
                {
                    var oriItem = data.FirstOrDefault(x => x.n == ori);

                    if (oriItem != null)
                    {
                        List<FileItemModel> temp = new List<FileItemModel>();

                        temp.Add(oriItem);
                        temp.Add(repeat);

                        ret.Add(ori, temp);
                    }
                    else
                    {
                        List<FileItemModel> temp = new List<FileItemModel>();

                        temp.Add(repeat);

                        ret.Add(ori, temp);
                    }
                }
                else
                {
                    ret[ori].Add(repeat);
                }
            }

            return ret;
        }

        public static string DeleteAndRename(Dictionary<string, List<FileItemModel>> input)
        {
            double deleteSize = 0;
            CookieContainer cc = Get115Cookie();
            var pattern = @"\(\d+\)";

            foreach (var data in input)
            {
                if (data.Value.Count >= 2)
                {
                    Console.WriteLine("正在处理 " + data.Key);

                    var biggest = data.Value.LastOrDefault();
                    var chinese = data.Value.FirstOrDefault(x => x.n.Contains("-C."));

                    Console.WriteLine("\t最大文件为 " + biggest.n + " 大小为 " + FileSize.GetAutoSizeString(biggest.s, 2));
                    Console.WriteLine("\t" + chinese == null ? "没有中文" : "有中文");

                    if (chinese != null)
                    {
                        data.Value.Remove(chinese);
                    }
                    else
                    {
                        data.Value.Remove(biggest);
                    }

                    foreach (var de in data.Value)
                    {
                        Console.WriteLine("\t删除 " + de.n + " 大小为 " + FileSize.GetAutoSizeString(de.s, 2));
                        Delete(de.fid, cc);
                        deleteSize += de.s;
                    }

                    Console.WriteLine("\t重命名 " + biggest.n + " 到 " + Regex.Replace(biggest.n, pattern, ""));
                    Rename(biggest.fid, Regex.Replace(biggest.n, pattern, ""), cc);
                    Console.WriteLine();
                }

                if (data.Value.Count == 1)
                {
                    Console.WriteLine("\t重命名 " + data.Value.LastOrDefault().n + " 到 " + Regex.Replace(data.Value.LastOrDefault().n, pattern, ""));
                    Rename(data.Value.LastOrDefault().fid, Regex.Replace(data.Value.LastOrDefault().n, pattern, ""), cc);
                }
            }

            return FileSize.GetAutoSizeString(deleteSize, 2);
        }

        public static void Delete(string fid, CookieContainer cc)
        {
            var url = @"https://webapi.115.com/rb/delete";

            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("pid", "0");
            param.Add("fid[0]", fid);

            HtmlManager.Post(url, param, cc);
        }

        public static void Rename(string fid, string newName, CookieContainer cc)
        {
            var url = @"https://webapi.115.com/files/batch_rename";

            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("files_new_name[" + fid + "]", newName);

            HtmlManager.Post(url, param, cc);
        }

        public static void Match115(bool overRide = false, bool writeJson = true, bool includeUpFolder = true)
        {
            Dictionary<string, bool> SearchResult = new Dictionary<string, bool>();
            var cc = Get115Cookie();

            var files = GetAllLocalAvs(includeUpFolder);
            int index = 1;

            foreach (var file in files)
            {
                var result = false;
                var possibleSha = ScanDataBaseManager.GetPossibleMaping(file.FullName, file.Length);

                if (possibleSha != null && overRide == false)
                {
                    ScanDataBaseManager.DeleteShaMapping(possibleSha.Sha1);

                    if (possibleSha.IsExist == 0)
                    {
                        possibleSha.IsExist = result ? 1 : 0;

                        ScanDataBaseManager.InsertShaMapping(possibleSha);
                    }
                    else
                    {
                        result = true;
                        possibleSha.IsExist = 1;

                        ScanDataBaseManager.InsertShaMapping(possibleSha);
                    }

                    Console.WriteLine("处理 " + index++ + " / " + files.Count());
                }

                if (possibleSha == null || overRide)
                {
                    DateTime start = DateTime.Now;

                    var sha1 = FileUtility.ComputeSHA1(file.FullName);

                    DateTime finishSha1 = DateTime.Now;

                    result = OneOneFiveService.Get115SearchResultInFinFolder(cc, sha1);

                    Console.WriteLine("处理 " + index++ + " / " + files.Count() + " 结果 " + (result ? "存在" : "不存在") + " 计算用时 " + (finishSha1 - start).TotalSeconds + " 秒 搜索用时 " + (DateTime.Now - finishSha1).TotalSeconds + " 秒 总共用时 " + (DateTime.Now - start).TotalSeconds + " 秒");

                    AvAndShaMapping aasm = new AvAndShaMapping();
                    aasm.FilePath = file.FullName;
                    aasm.FileSize = file.Length;
                    aasm.IsExist = result ? 1 : 0;
                    aasm.Sha1 = sha1;

                    ScanDataBaseManager.DeleteShaMapping(aasm.Sha1);
                    ScanDataBaseManager.InsertShaMapping(aasm);
                }

                SearchResult.Add(file.FullName, result);
            }

            if (writeJson)
            {
                var fileResult = @"c:\setting\filter115.json";

                if (File.Exists(fileResult))
                {
                    File.Delete(fileResult);
                    Thread.Sleep(50);
                }

                File.Create(fileResult).Close();
                StreamWriter sw = new StreamWriter(fileResult);
                sw.WriteLine(JsonConvert.SerializeObject(SearchResult));
                sw.Close();
            }
        }

        public static FileListModel GetOneOneFileInFolder(string folder, int page = 0, int pageSize = 1150)
        {
            FileListModel ret = new FileListModel();
            var cc = Get115Cookie();
            var url = $"https://webapi.115.com/files?aid=1&cid={folder}&o=user_ptime&asc=0&offset={page}&show_dir=1&limit={pageSize}&code=&scid=&snap=0&natsort=1&record_open_time=1&source=&format=json&type=&star=&is_q=&is_share=";

            var htmlRet = HtmlManager.GetHtmlWebClient("https://115.com", url, cc);
            if (htmlRet.Success)
            {
                if (!string.IsNullOrEmpty(htmlRet.Content))
                {
                    ret = JsonConvert.DeserializeObject<FileListModel>(htmlRet.Content);
                }
            }

            return ret;
        }

        public static string RemoveDuplicated115Files()
        {
            var repeat = GetRepeatFiles(1150);

            return DeleteAndRename(repeat);
        }

        public static void GetOneOneFiveDoesntHave()
        {
            List<FileItemModel> list = new List<FileItemModel>();
            List<ValueTuple<string, string, long>> localList = new List<(string, string, long)>();

            List<string> oneOneFiveDoesnt = new List<string>();
            List<string> localDoesnt = new List<string>();

            var pages = OneOneFiveService.Get115PagesInFolder(1150);

            for (int i = 0; i < pages; i++)
            {
                var files = OneOneFiveService.GetOneOneFileInFolder("1834397846621504875", i * 1150, 1150);

                if (files != null && files.data != null)
                {
                    list.AddRange(files.data);
                }
            }

            var localFiles = ScanDataBaseManager.GetAllMatch();

            foreach (var lf in localFiles)
            {
                localList.Add((lf.Name, lf.Location + "\\" + lf.Name, new FileInfo(lf.Location + "\\" + lf.Name).Length));
            }

            foreach (var l in localList)
            {
                var temp = list.Where(x => x.n == l.Item1);

                if (!temp.Any())
                {
                    oneOneFiveDoesnt.Add(l.Item2);
                }
                else
                {
                    var matched = temp.FirstOrDefault(x => x.s == l.Item3);

                    if (matched != null)
                    {
                        temp.Where(x => x.fid != matched.fid).ToList().ForEach(x => localDoesnt.Add(x.fid));
                    }
                }
            }
        }

        public static List<string> MoveFilesThatDoesnotExistIn115(string drive)
        {
            List<string> ret = new List<string>();
            var cc = OneOneFiveService.Get115Cookie();

            int index = 1;

            var targetFolder = drive + "\\fin\\";
            var up115 = drive + "\\up115\\";

            if (!Directory.Exists(up115))
            {
                Directory.CreateDirectory(up115);
            }

            if (Directory.Exists(targetFolder))
            {
                var files = Directory.GetFiles(targetFolder);

                foreach (var f in files)
                {
                    Console.WriteLine($"{index++} / {files.Count()} ");
                    var fileName = Path.GetFileName(f);
                    var result = OneOneFiveService.Get115SearchResultInFolder(cc, fileName);

                    var found = false;

                    if (result != null && result.count > 0)
                    {
                        foreach (var r in result.data)
                        {
                            if (r.n == fileName && !string.IsNullOrEmpty(r.fid) && r.s == new FileInfo(f).Length)
                            {
                                found = true;
                                Console.WriteLine($"找到{f}在{r.cid}");
                                break;
                            }
                        }

                        if (!found)
                        {
                            ret.Add(f);
                        }
                    }
                }

                var res = FileUtility.TransferFileUsingSystem(ret, up115, true, true);
            }

            return ret;
        }

        public static void MoveBackToFin(string drive)
        {
            List<string> ret = new List<string>();
            var cc = OneOneFiveService.Get115Cookie();

            int index = 1;

            var targetFolder = drive + "\\up115\\";
            var finFolder = drive + "\\fin\\";

            if (Directory.Exists(targetFolder))
            {
                var files = Directory.GetFiles(targetFolder);

                foreach (var f in files)
                {
                    Console.WriteLine($"{index++} / {files.Count()} ");
                    var fileName = Path.GetFileName(f);
                    var result = OneOneFiveService.Get115SearchResultInFolder(cc, fileName);

                    var found = false;

                    if (result != null && result.count > 0)
                    {
                        foreach (var r in result.data)
                        {
                            if (r.n == fileName && !string.IsNullOrEmpty(r.fid) && r.s == new FileInfo(f).Length)
                            {
                                found = true;
                                Console.WriteLine($"找到{f}在{r.cid}");
                                break;
                            }
                        }

                        if (found)
                        {
                            ret.Add(f);
                        }
                    }
                }

                var res = FileUtility.TransferFileUsingSystem(ret, finFolder, true, true);
            }
        }

        public static List<FileItemModel> Get115FilesModel(string folder = "1834397846621504875")
        {
            List<FileItemModel> list = new List<FileItemModel>();

            var pages = OneOneFiveService.Get115PagesInFolder(1150);

            for (int i = 0; i < pages; i++)
            {
                var files = OneOneFiveService.GetOneOneFileInFolder(folder, i * 1150, 1150);

                if (files != null && files.data != null)
                {
                    list.AddRange(files.data);
                }
            }

            return list;
        }

        public static void Insert115FileSha()
        {
            var files = Get115FilesModel().Where(x => !string.IsNullOrEmpty(x.fid)).ToList();

            Console.WriteLine($"获取到{files.Count}个文件");
            int index = 1;

            foreach (var file in files)
            {
                Console.WriteLine($"正在处理{index++}");

                OneOneFiveFileShaMapping entity = new OneOneFiveFileShaMapping()
                {
                    FileName = file.n,
                    FileSize = file.s,
                    Sha = file.sha,
                };

                ScanDataBaseManager.InserOneOneFiveFileShaMapping(entity);
            }
        }

        public static List<FileInfo> GetAllLocalAvs(bool includeUpFolder = true)
        {
            List<FileInfo> ret = new List<FileInfo>();

            foreach (var drive in Environment.GetLogicalDrives())
            {
                if (Directory.Exists(drive + FinFolder))
                {
                    ret.AddRange(new DirectoryInfo(drive + FinFolder).GetFiles());
                }

                if (includeUpFolder && Directory.Exists(drive + UpFolder))
                {
                    ret.AddRange(new DirectoryInfo(drive + UpFolder).GetFiles());
                }
            }

            return ret;
        }

        public static void MatchLocalAndOneOneFive()
        {
            Console.WriteLine($"获取115文件");
            var oneOneFiveFiles = Get115FilesModel();
            Console.WriteLine($"获取{oneOneFiveFiles.Count}个115文件");

            Console.WriteLine($"获取本地文件");
            var localFiles = GetAllLocalAvs();
            Console.WriteLine($"获取{oneOneFiveFiles.Count}个本地文件");

            Console.WriteLine($"获取Sha Mapping");
            var shaMapping = ScanDataBaseManager.GetAllAvAndShaMapping();
            Console.WriteLine($"获取{oneOneFiveFiles.Count}个Sha Mapping");

            foreach (var oneOneFiveFile in oneOneFiveFiles)
            {
                var matchRecord = shaMapping.FirstOrDefault(x => x.Sha1 == oneOneFiveFile.sha);
                var matchFile = localFiles.FirstOrDefault(x => x.Name == oneOneFiveFile.n && x.Length == oneOneFiveFile.s);

                if(matchRecord != null && matchFile != null)
                {
                    Console.WriteLine($"{oneOneFiveFile.n} 完全匹配");
                    ScanDataBaseManager.UpdateShaMapping(matchFile.FullName, matchFile.Length, true);
                }
                else
                {
                    if (matchFile != null)
                    {
                        Console.WriteLine($"{oneOneFiveFile.n} 大致匹配");
                        AvAndShaMapping temp = new AvAndShaMapping
                        {
                            FilePath = matchFile.FullName,
                            Sha1 = oneOneFiveFile.sha,
                            FileSize = matchFile.Length,
                            IsExist = 1
                        };

                        var possibleSha = ScanDataBaseManager.GetPossibleMaping(temp.FilePath, temp.FileSize);

                        if (possibleSha != null)
                        {
                            ScanDataBaseManager.UpdateShaMapping(temp.FilePath, temp.FileSize, true);
                        }
                        else
                        {
                            ScanDataBaseManager.InsertShaMapping(temp);
                        }
                    }
                }
            }
        }
    }
}
