using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using HtmlAgilityPack;
using Microsoft.Win32.TaskScheduler;
using Model.JavModels;
using Model.ScanModels;
using Newtonsoft.Json;
using Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace NewUnitTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var files = Directory.GetFiles(@"R:\testrename").OrderBy(x => x.Length);

            Console.ReadKey();
        }

        public static void Rename()
        {
            string src = "";
            string desc = "";

            while (string.IsNullOrWhiteSpace(src))
            {
                Console.WriteLine("请输入需要重命名的文件夹...");
                src = Console.ReadLine();
            }

            if (Directory.Exists(src))
            {
                Console.WriteLine("正在初始化数据...");

                var ret = RenameService.PrepareRename(src, 500).OrderBy(x => x.Key).ThenBy(x => x.Key.Length).ToDictionary(x => x.Key, x => x.Value);

                Console.WriteLine($"共找到 {ret.Count} 个文件需要重命名...");

                while (string.IsNullOrWhiteSpace(desc))
                {
                    Console.WriteLine("请输入重命名到的文件夹...");
                    desc = Console.ReadLine();
                }

                if (!Directory.Exists(desc))
                {
                    Directory.CreateDirectory(desc);
                }

                foreach (var f in ret)
                {
                    var finalFile = "";

                    if (f.Value.Count == 1)
                    {
                        finalFile = f.Value.FirstOrDefault().MoveFile;
                    }

                    if (f.Value.Count > 1)
                    {
                        int index = 1;
                        int choose = 0;

                        Console.WriteLine($"文件 {f.Key} 有多个匹配...");

                        foreach (var l in f.Value)
                        {
                            Console.WriteLine($"\t{index++}. {l.MoveFile}");
                        }

                        while (choose <= 0)
                        {
                            Console.WriteLine("输入序号选择...");
                            int.TryParse(Console.ReadLine(), out choose);
                        }

                        finalFile = f.Value[choose - 1].MoveFile;
                    }

                    FileUtility.FileRenameUsingSystem(f.Key, src + "\\" + finalFile);
                    FileUtility.TransferFileUsingSystem(new List<string>() { src + "\\" + finalFile }, desc + "\\" + finalFile, true, false);
                }
            }
            else
            {
                Console.WriteLine("无效文件夹...");
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        public static List<string> CheckH265()
        {
            List<string> badFiles = new List<string>();

            System.Threading.Tasks.Task.Run(() =>
            {
                Parallel.ForEach(Environment.GetLogicalDrives(), dir =>
                {
                    badFiles.AddRange(IsH265($"{dir}fin\\").Result.Item2);
                });
            }).Wait();

            return badFiles;
        }

        public static void TestRemoveFolder(string sourceFolder, string descFolder, int fileSizeLimit)
        {
            descFolder = (sourceFolder.EndsWith("\\") || sourceFolder.EndsWith("/")) ? sourceFolder + "movefiles\\" : sourceFolder + "\\movefiles\\";

            var ret = RenameService.RemoveSubFolder(sourceFolder: sourceFolder, descFolder: descFolder, fileSizeLimit: fileSizeLimit);
        }

        public static void TestRename(string sourceFolder, int fileSizeLimit)
        {
            var descFolder = (sourceFolder.EndsWith("\\") || sourceFolder.EndsWith("/")) ? sourceFolder + "tempFile\\" : sourceFolder + "\\tempFin\\";

            var ret = RenameService.PrepareRename(sourceFolder, fileSizeLimit);
        }

        public static void ReFormatName(string folder)
        {
            if (Directory.Exists(folder))
            {
                foreach (var file in new DirectoryInfo(folder).GetFiles())
                {
                    if (AVFileHelper.IsReformated(file))
                    {
                        var ret = AVFileHelper.ParseAvFile(file.FullName);
                        var has = JavDataBaseManager.HasAv(ret.AvId, ret.AvName);

                        Console.WriteLine(file + (has ? " 数据库存在" : " 数据库不存在"));
                    }
                    else
                    {
                        var reName = AVFileHelper.GetAvName(file);
                        file.MoveTo(reName);
                        AVFileHelper.ParseAvFile(reName);
                    }
                }
            }
        }

        public static void DeleteErrorFile(string log)
        {
            double deleteSize = 0;
            int count = 0;

            if (File.Exists(log))
            {
                StreamReader sr = new StreamReader(log);

                while (!sr.EndOfStream)
                {
                    var text = sr.ReadLine();

                    var deleteFile = text.Substring(text.IndexOf("文件 ") + "文件 ".Length);

                    if (File.Exists(deleteFile))
                    {
                        deleteSize += new FileInfo(deleteFile).Length;
                        count++;

                        File.Delete(deleteFile);
                    }
                }
            }

            Console.WriteLine("删除 " + count + " 个文件, 总大小 " + FileSize.GetAutoSizeString(deleteSize, 1));
        }

        public async static Task<ValueTuple<int, List<string>>> IsH265(string folder)
        {
            var start = DateTime.Now;
            var ffmpeg = @"c:\setting\ffmpeg.exe";
            int h265Count = 0;
            ValueTuple<int, List<string>> ret = new ValueTuple<int, List<string>>();
            List<string> badFiles = new List<string>();

            if (Directory.Exists(folder))
            {
                var files = new DirectoryInfo(folder).GetFiles();

                System.Threading.Tasks.Task.Run(() =>
                {
                    Parallel.ForEach(files, f =>
                    {
                        var temp = DateTime.Now;

                        var result = FileUtility.IsH265(f.FullName, ffmpeg).Result;

                        if (result.Item1)
                        {
                            h265Count++;
                        }

                        if (!string.IsNullOrEmpty(result.Item2))
                        {
                            badFiles.Add(f.FullName);
                        }

                        Console.WriteLine(f.FullName + " -> " + (result.Item1 ? "是H265" : "不是H265") + " 耗时 " + (DateTime.Now - temp).TotalSeconds + " 秒");
                    });
                }).Wait();

                //foreach (var f in files)
                //{
                    
                //}
            }

            ret.Item1 = h265Count;
            ret.Item2 = badFiles;
            Console.WriteLine("总耗时 " + (DateTime.Now - start).TotalSeconds + " 秒, 共有" + h265Count + " 部H265");

            return ret;
        }

        public static void Match115(List<string> drivers, bool overRide = false, bool writeJson = true)
        {
            Dictionary<string, bool> SearchResult = new Dictionary<string, bool>();
            var cc = OneOneFiveService.Get115Cookie();

            foreach (var d in drivers)
            {
                var files = new DirectoryInfo(d + @"\fin\").GetFiles();
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
                            result = OneOneFiveService.Get115SearchResultInFinFolder(cc, possibleSha.Sha1);
                        }
                        else
                        {
                            result = true;
                            possibleSha.IsExist = 1;

                            ScanDataBaseManager.InsertShaMapping(possibleSha);
                        }
                    }

                    if(possibleSha == null || overRide)
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

        public static void BackUpJav()
        {
            var dateStr = DateTime.Today.ToString("yyyyMMdd");
            var extension = ".json";
            var folder = @"G:\Github\AllInOneAV\Scripts\\";
            var rawFolder = folder + "dataRaw\\";
            var zipFileFoler = folder + "dataZip\\";
            var zipFile = zipFileFoler + dateStr + ".zip";
            var avFile = rawFolder + "av" + dateStr + extension;
            var actressFile = rawFolder + "actress" + dateStr + extension;
            var directorFile = rawFolder + "director" + dateStr + extension;
            var companyFile = rawFolder + "company" + dateStr + extension;
            var publisherFile = rawFolder + "publisher" + dateStr + extension;

            StreamWriter sw = null;

            var avs = JavDataBaseManager.GetAllAV();
            var actress = JavDataBaseManager.GetActress();
            var director = JavDataBaseManager.GetDirector();
            var company = JavDataBaseManager.GetCompany();
            var publisher = JavDataBaseManager.GetPublisher();

            if (!Directory.Exists(rawFolder))
            {
                Directory.CreateDirectory(rawFolder);
            }

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (!Directory.Exists(zipFileFoler))
            {
                Directory.CreateDirectory(zipFileFoler);
            }

            foreach (var file in new DirectoryInfo(rawFolder).GetFiles())
            {
                file.Delete();
                Thread.Sleep(50);
            }

            if (!File.Exists(avFile))
            {
                File.Create(avFile).Close();

                sw = new StreamWriter(avFile);
                sw.WriteLine(JsonConvert.SerializeObject(avs));
                sw.Close();
            }

            if (!File.Exists(actressFile))
            {
                File.Create(actressFile).Close();

                sw = new StreamWriter(actressFile);
                sw.WriteLine(JsonConvert.SerializeObject(actress));
                sw.Close();
            }

            if (!File.Exists(directorFile))
            {
                File.Create(directorFile).Close();

                sw = new StreamWriter(directorFile);
                sw.WriteLine(JsonConvert.SerializeObject(director));
                sw.Close();
            }

            if (!File.Exists(companyFile))
            {
                File.Create(companyFile).Close();

                sw = new StreamWriter(companyFile);
                sw.WriteLine(JsonConvert.SerializeObject(company));
                sw.Close();
            }

            if (!File.Exists(publisherFile))
            {
                File.Create(publisherFile).Close();

                sw = new StreamWriter(publisherFile);
                sw.WriteLine(JsonConvert.SerializeObject(publisher));
                sw.Close();
            }
        }

        public static void GetTaskNextRunTime(string taskName)
        {
            TaskService ts = new TaskService();
            var task = ts.FindTask("ScanJavJob");

            task.Run();

        }

        public static string GetNextRunTimeString(Microsoft.Win32.TaskScheduler.Task t)
        {
            if (t.State == TaskState.Disabled || t.NextRunTime < DateTime.Now)
                return string.Empty;
            return t.NextRunTime.ToString("G");
        }

        public static void TestMove()
        {
            var folder = "c:\\setting\\testmove";

            List<string> tos = new List<string>();
            var files = Directory.GetFiles(folder).ToList();

            FileUtility.TransferFileUsingSystem(files, "c:\\setting\\testmove\\move2\\", false);
        }

        public static void TestRename()
        {
            var folder = "c:\\setting\\testmove";

            List<string> tos = new List<string>();
            var files = Directory.GetFiles(folder).ToList();

            foreach (var f in files)
            {
                var newFileName = Path.GetDirectoryName(f) + Path.DirectorySeparatorChar + "123" + Path.GetExtension(f);
                FileUtility.FileRenameUsingSystem(f, newFileName);
            }
        }

        public static void MatchAvator()
        {
            var avatorFolderPrefix = @"C:\Setting\演员头像\";
            var actress = JavDataBaseManager.GetActress();

            int found = 0;
            int notFound = 0;

            foreach (var act in actress)
            {
                bool has = false;
                var realFolder = avatorFolderPrefix + act.Name[0] + @"\";

                if (Directory.Exists(realFolder))
                {
                    var avators = Directory.GetFiles(realFolder);

                    foreach (var avator in avators)
                    {
                        if (avator.Contains(act.Name))
                        {
                            Console.WriteLine(act.Name + avator);
                            found++;
                            has = true;
                            break;
                        }
                    }
                }

                if (!has)
                {
                    notFound++;
                }
            }

            Console.WriteLine("找到 " + found + " 未找到 " + notFound);
        }

        public static void CheckAvatorMatch()
        {
            Dictionary<string, List<string>> matchRecord = new Dictionary<string, List<string>>();
            List<string> avators = new List<string>();
            var folder = @"G:\Github\AllInOneAV\Setting\avator";
            var avs = JavDataBaseManager.GetActress();

            foreach (var f in Directory.GetDirectories(folder))
            {
                foreach (var a in Directory.GetFiles(f))
                {
                    if (!avators.Contains(a))
                    {
                        avators.Add(a);
                    }
                }
            }

            foreach (var a in avs)
            {
                foreach (var m in avators.OrderByDescending(x => x.Length))
                {
                    if (m.Contains(a.Name))
                    {
                        if (!matchRecord.ContainsKey(a.Name))
                        {
                            matchRecord.Add(a.Name, new List<string>() { m.Replace(@"G:\Github\AllInOneAV\Setting\", @"\Imgs\").Replace(@"\", "/") });
                            break;
                        }
                    }
                }
            }

            foreach (var m in matchRecord)
            {
                ScanDataBaseManager.UpdateFaviAvator(m.Key, m.Value.FirstOrDefault());
            }

            Console.ReadKey();
        }

        public static void DownloadActreeAvator()
        {
            int index = 1;
            bool contiune = true;
            var folderPrefix = @"G:\Github\AllInOneAV\Setting\avator\";
            var url = "https://www.javbus.com/actresses/";
            var cc = JavBusDownloadHelper.GetJavBusCookie();

            while (contiune)
            {
                var content = HtmlManager.GetHtmlContentViaUrl(url + index++, "utf-8", false, cc);

                if (content.Success)
                {
                    HtmlDocument htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(content.Content);

                    string xpath = "//a[@class='avatar-box text-center']";
                    string imgPath = "/img";

                    HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes(xpath);

                    foreach (var node in nodes)
                    {
                        var img = node.ChildNodes[1].ChildNodes[1];

                        var src = img.Attributes["src"].Value;
                        var title = img.Attributes["title"].Value;

                        if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(title))
                        {
                            var tempFolder = folderPrefix + title[0] + "\\";
                            if (!Directory.Exists(tempFolder))
                            {
                                Directory.CreateDirectory(tempFolder);
                            }

                            DownloadHelper.DownloadFile(src, tempFolder + title + ".jpg");
                            Console.WriteLine($"下载第 {index - 1} 页，{title} 的头像");
                        }
                    }
                }
                else
                {
                    contiune = false;
                }
            }
        }
    }
}
