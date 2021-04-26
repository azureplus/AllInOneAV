using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using Model.JavModels;
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

        public static bool Get115SearchResult(CookieContainer cc, string content, string folder = "1834397846621504875", string host = "115.com", string reffer = "https://115.com/?cid=0&offset=0&mode=wangpan", string ua = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36 115Browser/12.0.0")
        {
            bool ret = false;

            var url = string.Format(string.Format("https://webapi.115.com/files/search?search_value={0}&format=json&cid={1}", content, folder));
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

            var sessionCookie = ScanDataBaseManager.GetOneOneFiveCookie();

            if (sessionCookie != null && !string.IsNullOrEmpty(sessionCookie.OneOneFiveCookie))
            {
                List<CookieItem> sessionCookieItems = JsonConvert.DeserializeObject<List<CookieItem>>(sessionCookie.OneOneFiveCookie);

                foreach (var item in sessionCookieItems)
                {
                    Cookie temp = new Cookie(item.Name, item.Value, "/", "115.com");
                    cc.Add(temp);
                }
            }

            //cookieData = new ChromeCookieReader().ReadCookies("webapi.115.com");

            //foreach (var item in cookieData.Where(x => !x.Value.Contains(",")).Distinct())
            //{
            //    Cookie c = new Cookie(item.Name, item.Value, "/", "115.com");
            //    cc.Add(c);
            //}

            //var tempCc = HtmlManager.GetCookies("http://www.115.com", "utf-8", cc);

            return cc;
        }

        public static int Get115PagesInFolder(OneOneFiveSearchType type, int pageSize = 1, string folder = "1834397846621504875")
        {
            var url = $"https://webapi.115.com/files?aid=1&cid={folder}&o=user_ptime&asc=0&offset=0&show_dir=1&limit={pageSize}&code=&scid=&snap=0&natsort=1&record_open_time=1&source=&format=json&type={((int)type).ToString()}";
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

        public static Dictionary<string, List<FileItemModel>> GetRepeatFiles(int pageSize = 1)
        {
            Dictionary<string, List<FileItemModel>> ret = new Dictionary<string, List<FileItemModel>>();
            var pattern = @"\(\d+\)";
            var data = Get115FilesModel();

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

                    data.Value.Remove(biggest);
  
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

        public static string DeleteList(List<FileItemModel> files, string pid)
        {
            var cc = Get115Cookie();

            var url = @"https://webapi.115.com/rb/delete";

            Dictionary<string, string> param = new Dictionary<string, string>();
            int index = 0;
            long deleteSize = 0;

            param.Add("pid", pid);

            foreach (var file in files)
            {
                param.Add($"fid[{index++}]", file.fid);
                deleteSize += file.s;
            }

            try
            {
                HtmlManager.Post(url, param, cc);
            }
            catch (Exception)
            { 
                
            }

            return FileSize.GetAutoSizeString(deleteSize, 1);
        }

        public static void Rename(string fid, string newName, CookieContainer cc)
        {
            var url = @"https://webapi.115.com/files/batch_rename";

            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("files_new_name[" + fid + "]", newName);

            HtmlManager.Post(url, param, cc);
        }

        public static void Move(string fid, string folder, CookieContainer cc)
        {
            var url = @"https://webapi.115.com/files/move";

            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("pid", folder);
            param.Add("fid[0]", fid);

            HtmlManager.Post(url, param, cc);
        }

        public static FileListModel GetOneOneFileInFolder(string folder, OneOneFiveSearchType type, int page = 0, int pageSize = 1150)
        {
            FileListModel ret = new FileListModel();
            var cc = Get115Cookie();
            var url = $"https://webapi.115.com/files?aid=1&cid={folder}&o=user_ptime&asc=0&offset={page}&show_dir=1&limit={pageSize}&code=&scid=&snap=0&natsort=1&record_open_time=1&source=&format=json&type={((int)type).ToString()}&star=&is_q=&is_share=";

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

        public static List<FileItemModel> Get115FilesModel(string folder = "1834397846621504875", OneOneFiveSearchType type = OneOneFiveSearchType.All)
        {
            List<FileItemModel> list = new List<FileItemModel>();

            var pages = OneOneFiveService.Get115PagesInFolder(type, 1150, folder);

            for (int i = 0; i < pages; i++)
            {
                var files = OneOneFiveService.GetOneOneFileInFolder(folder, type, i * 1150, 1150);

                if (files != null && files.data != null)
                {
                    list.AddRange(files.data);
                }
            }

            return list;
        }

        public static void Insert115FileSha(List<FileItemModel> models, bool truncate = false)
        {
            var files = models.Where(x => !string.IsNullOrEmpty(x.fid)).ToList();

            Console.WriteLine($"获取到{files.Count}个文件");
            int index = 1;

            if (files != null && files.Any())
            {
                if (truncate)
                {
                    ScanDataBaseManager.TruncateOneOneFiveFileShaMapping();
                }

                foreach (var file in files)
                {
                    Console.WriteLine($"正在处理{index++}");

                    OneOneFiveFileShaMapping entity = new OneOneFiveFileShaMapping()
                    {
                        FileName = file.n,
                        FileSize = file.s,
                        Sha = file.sha,
                        IsOnLocal = false
                    };

                    ScanDataBaseManager.InserOneOneFiveFileShaMapping(entity);
                }
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

        public static List<string> InitLocalSha(bool earse = false)
        {
            List<string> needToCalculateSha = new List<string>();

            var filesIn115 = OneOneFiveService.Get115FilesModel();
            var localFiles = GetAllLocalAvs();

            if (filesIn115 != null && filesIn115.Any())
            {
                if (earse)
                {
                    ScanDataBaseManager.TruncateLocalShaMapping();
                }

                foreach (var localFile in localFiles)
                {
                    var matchedRecord = filesIn115.FirstOrDefault(x => x.n.Equals(localFile.Name, StringComparison.OrdinalIgnoreCase) && x.s == localFile.Length);

                    if (matchedRecord != null && !string.IsNullOrEmpty(matchedRecord.sha))
                    {
                        LocalShaMapping temp = new LocalShaMapping
                        {
                            FilePath = localFile.Name,
                            FileFolder = Path.GetPathRoot(localFile.FullName),
                            FileSize = localFile.Length,
                            Sha1 = matchedRecord.sha
                        };

                        ScanDataBaseManager.InsertLocalShaMapping(temp);
                    }
                    else
                    {
                        needToCalculateSha.Add(localFile.FullName);
                    }
                }
            }

            return needToCalculateSha;
        }

        public static void Match115AndMoveLocalFile()
        {
            var filesIn115 = OneOneFiveService.Get115FilesModel();

            foreach (var drive in Environment.GetLogicalDrives())
            {
                Console.WriteLine($"处理 {drive}");

                List<string> files = new List<string>();
                List<string> toFin = new List<string>();
                List<string> to115 = new List<string>();

                var fin = drive + FinFolder;
                var up = drive + UpFolder;

                if (Directory.Exists(fin))
                {
                    files.AddRange(new DirectoryInfo(fin).GetFiles().Select(y => y.FullName).ToList());
                }

                if (Directory.Exists(up))
                {
                    files.AddRange(new DirectoryInfo(up).GetFiles().Select(y => y.FullName).ToList());
                }

                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);
                    if (filesIn115.Exists(x => x.n.Equals(Path.GetFileName(file), StringComparison.OrdinalIgnoreCase) && x.s == new FileInfo(file).Length))
                    {
                        var path = Path.GetDirectoryName(file) + "\\";
                        if (path == up)
                        {
                            toFin.Add(file);
                        }
                    }
                    else
                    {
                        var path = Path.GetDirectoryName(file) + "\\";
                        if (path == fin)
                        {
                            to115.Add(file);
                        }
                    }
                }

                if (toFin.Count > 0)
                {
                    if (!Directory.Exists(fin))
                    {
                        Directory.CreateDirectory(fin);
                    }

                    Console.WriteLine($"移动 {toFin.Count} 到 FIN");

                    FileUtility.TransferFileUsingSystem(toFin, fin, true, true);
                }

                if (to115.Count > 0)
                {
                    if (!Directory.Exists(up))
                    {
                        Directory.CreateDirectory(up);
                    }

                    Console.WriteLine($"移动 {to115.Count} 到 UP115");

                    FileUtility.TransferFileUsingSystem(to115, up, true, true);
                }
            }
        }

        public static void SyncLocalAnd115FileStatus(string folder = "1834397846621504875", bool update115Maping = false)
        {
            var oneOneFiles = Get115FilesModel(folder);
            var localFiles = GetAllLocalAvs();
            var localShaMapping = ScanDataBaseManager.GetAllLocalShaMapping();

            if (oneOneFiles != null && oneOneFiles.Any())
            {
                if (update115Maping)
                {
                    Insert115FileSha(oneOneFiles);
                }

                //刷新本地sha，更新本地是否保存
                foreach (var oneOneFive in oneOneFiles)
                {
                    var matchedMapping = localShaMapping.FirstOrDefault(x => x.Sha1 == oneOneFive.sha);
                    
                    if (matchedMapping != null)
                    {
                        ScanDataBaseManager.UpdateOneOneFiveFileShaMapping(matchedMapping.Sha1, true);
                    }
                }
            }
        }

        public static List<FileItemModel> Get115HasButLocal()
        {
            List<FileItemModel> extraFiles = new List<FileItemModel>();

            var localFiles = GetAllLocalAvs();
            var oneOneFiveFiles = Get115FilesModel();

            foreach (var oneOneFiveFile in oneOneFiveFiles)
            {
                if (localFiles.FirstOrDefault(x => x.Length == oneOneFiveFile.s && x.Name.Equals(oneOneFiveFile.n, StringComparison.OrdinalIgnoreCase)) == null)
                {
                    //if (localFiles.FirstOrDefault(x => x.Name.Equals(oneOneFiveFile.n, StringComparison.OrdinalIgnoreCase)) != null)
                    //{
                        extraFiles.Add(oneOneFiveFile);
                    //}
                }
            }

            return extraFiles;
        }

        public static List<FileInfo> GetFileToBeDeletedBySize(long gb = 1)
        {
            var files = GetAllLocalAvs();

            var delete = files.Where(x => x.Length <= gb * 1024 * 1024 * 1024).ToList();

            return delete;
        }

        public static List<LocalAndRemoteFiles> GetLocalAndRemoteFiles(bool includeUpFolder = true, FileSearchScope scope = FileSearchScope.Both)
        {
            List<LocalAndRemoteFiles> ret = new List<LocalAndRemoteFiles>();

            if (scope.HasFlag(FileSearchScope.Local))
            {
                var localFiles = GetAllLocalAvs(includeUpFolder);

                foreach (var file in localFiles)
                {
                    ret.Add(new LocalAndRemoteFiles()
                    {
                        FileExtension = Path.GetExtension(file.FullName),
                        FileLocation = Path.GetDirectoryName(file.FullName),
                        FileName = file.Name,
                        FileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FullName),
                        FileSize = file.Length,
                        FileSizeStr = FileSize.GetAutoSizeString(file.Length, 1),
                        IsChinese = file.Name.Contains("-C."),
                        IsLocal = true,
                        FileAvId = file.Name.Split('-').Length >= 3 ? file.Name.Split('-')[0] + "-" + file.Name.Split('-')[1] : ""
                    });
                    ;
                }
            }

            if (scope.HasFlag(FileSearchScope.Remote))
            {
                var remoteFiles = Get115FilesModel();

                foreach (var file in remoteFiles)
                {
                    ret.Add(new LocalAndRemoteFiles()
                    {
                        FileExtension = "." + file.ico,
                        FileLocation = file.cid,
                        FileName = file.n,
                        FileNameWithoutExtension = file.n.Replace("." + file.ico, ""),
                        FileSize = file.s,
                        FileSizeStr = FileSize.GetAutoSizeString(file.s, 1),
                        IsChinese = file.n.Contains("-C."),
                        IsLocal = false,
                        FileAvId = file.n.Split('-').Length >= 3 ? file.n.Split('-')[0] + "-" + file.n.Split('-')[1] : "",
                        PickCode = file.pc
                    });
                }
            }

            return ret;
        }

        public static string GetM3U8(string pc)
        {
            var cc = Get115Cookie();
            var url = "https://v.anxia.com/site/api/video/m3u8/" + pc + ".m3u8";
            var m3u8 = "";

            var htmlRet = HtmlManager.GetHtmlWebClient("https://webapi.115.com", url, cc);
            if (htmlRet.Success)
            {
                if (!string.IsNullOrEmpty(htmlRet.Content))
                {
                    m3u8 = htmlRet.Content.Substring(htmlRet.Content.IndexOf("http"));
                }
            }

            return m3u8;
        }

        public static void Rename(List<FileItemModel> files, string toFolder, string notFoundFolder, CookieContainer cc)
        {
            var avs = JavDataBaseManager.GetAllAV();
            List<string> allPrefix = new List<string>();

            foreach (var name in avs.Select(x => x.ID).ToList())
            {
                var tempPrefix = name.Split('-')[0];
                if (!allPrefix.Contains(tempPrefix))
                {
                    allPrefix.Add(tempPrefix);
                }
            }

            allPrefix = allPrefix.OrderByDescending(x => x.Length).ToList();

            foreach (var file in files)
            {
                List<RenameModel> tempRet = new List<RenameModel>();
                List<AV> possibleAv = new List<AV>();
                var fileNameWithoutFormat = file.n.Replace("." + file.ico, "").ToLower();

                foreach (var prefix in allPrefix)
                {
                    var pattern = prefix + "{1}-?\\d{1,7}";
                    var matches = Regex.Matches(fileNameWithoutFormat, pattern, RegexOptions.IgnoreCase);

                    foreach (System.Text.RegularExpressions.Match m in matches)
                    {
                        var possibleAvId = m.Groups[0].Value;

                        if (!possibleAvId.Contains("-"))
                        {
                            bool isFirst = true;
                            StringBuilder sb = new StringBuilder();

                            foreach (var c in possibleAvId)
                            {
                                if (c >= '0' && c <= '9')
                                {
                                    if (isFirst)
                                    {
                                        sb.Append("-");
                                        isFirst = false;
                                    }
                                }
                                sb.Append(c);
                            }
                            possibleAvId = sb.ToString();
                        }

                        var tempAv = JavDataBaseManager.GetAllAV(possibleAvId);

                        if (tempAv != null && tempAv.Count > 0)
                        {
                            possibleAv.AddRange(tempAv);
                        }
                        else
                        {
                            var prefixPart = possibleAvId.Split('-')[0];
                            var numberPart = possibleAvId.Split('-')[1];

                            while (numberPart.StartsWith("0"))
                            {
                                numberPart = numberPart.Substring(1);
                                possibleAvId = prefixPart + "-" + numberPart;
                                tempAv = JavDataBaseManager.GetAllAV(possibleAvId);
                                if (tempAv != null && tempAv.Count > 0)
                                {
                                    possibleAv.AddRange(tempAv);
                                }
                            }
                        }
                    }
                }

                if (possibleAv != null && possibleAv.Count >= 1)
                {
                    var rename = possibleAv.OrderByDescending(x => x.Name.Length).Take(1).FirstOrDefault();
                    var chinese = (fileNameWithoutFormat.EndsWith("-c") || fileNameWithoutFormat.EndsWith("-ch") || fileNameWithoutFormat.EndsWith("ch")) ? "-C" : "";

                    var tempName = rename.ID + "-" + rename.Name + chinese + "." + file.ico;

                    Rename(file.fid, tempName, cc);
                    //TODO 查询目标文件夹有没有相同SHA，如果有删除当前
                    //TODO 查询目标文件夹有没有相同名称，如果有旧的名称加上-1，当前文件用之前同名文件最大的-X + 1
                    Move(file.fid, toFolder, cc);
                }
                else
                {
                    Move(file.fid, notFoundFolder, cc);
                }
            }
        }
    }

    [Flags]
    public enum FileSearchScope
    { 
        Local = 1,
        Remote = 2,
        Both = 4
    }
}
