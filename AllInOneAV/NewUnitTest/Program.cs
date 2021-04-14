using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using HtmlAgilityPack;
using Microsoft.Win32.TaskScheduler;
using Model.Common;
using Model.JavModels;
using Model.OneOneFive;
using Model.ScanModels;
using Model.WebModel;
using Newtonsoft.Json;
using Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace NewUnitTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = OneOneFiveService.RemoveDuplicated115Files();
            //Console.WriteLine(test);

            OneOneFiveService.Match115();

            //OneOneFiveService.Insert115FileSha();

            //OneOneFiveService.MatchLocalAndOneOneFive();

            //Get115ShaAndMatchLocal();

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

        public static void TestRemoveFolder(string sourceFolder, int fileSizeLimit)
        {
            var descFolder = (sourceFolder.EndsWith("\\") || sourceFolder.EndsWith("/")) ? sourceFolder + "movefiles\\" : sourceFolder + "\\movefiles\\";

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

        public static SystemTreeVM GetSystemTreeVM(bool excludeFiles = false, bool exculdeCDrive = true)
        {
            SystemTreeVM ret = new SystemTreeVM
            {
                text = "System",
                selectable = true,
                icon = "fa fa-terminal"
            };

            List<SystemTreeVM> subs = new List<SystemTreeVM>();
            ret.nodes = subs;

            var drives = Environment.GetLogicalDrives();

            if (exculdeCDrive)
            {
                drives = drives.Skip(1).ToArray();
            }

            foreach (var d in drives)
            {
                SystemTreeVM sub = new SystemTreeVM();
                ret.nodes.Add(sub);
 
                GetSystemTreeRecursively(sub, d);
            }

            return ret;
        }

        public static void GetSystemTreeRecursively(SystemTreeVM sub, string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                return;
            }

            List<SystemTreeVM> subs = new List<SystemTreeVM>();
            sub.nodes = subs;

            var folders = new DirectoryInfo(root).GetDirectories().Where(x => (x.Attributes & FileAttributes.System) == 0);
            var files = Directory.GetFiles(root);

            foreach (var fo in folders)
            {
                var tempNode = new SystemTreeVM()
                {
                    text = fo.Name,
                    selectable = true,
                    icon = "fa fa-folder",
                    selectedIcon = "fa fa-folder-open"
                };

                sub.nodes.Add(tempNode);

                GetSystemTreeRecursively(tempNode, fo.FullName);
            }

            foreach (var fi in files)
            {
                SystemTreeVM treeNode = new SystemTreeVM
                {
                    text = fi,
                    selectable = true,
                    icon = "fa fa-file",
                };

                sub.nodes.Add(treeNode);
            }
        }

        public static Dictionary<string, List<SeedMagnetSearchModel>> TestSearchJavBus(string drive)
        {
            var files = Directory.GetFiles(drive);
            Dictionary<string, List<SeedMagnetSearchModel>> result = new Dictionary<string, List<SeedMagnetSearchModel>>();

            foreach (var f in files)
            {
                if (f.Contains("-"))
                {
                    var file = Path.GetFileNameWithoutExtension(f);

                    var avid = file.Split('-')[0] + "-" + file.Split('-')[1];

                    var mag = MagService.SearchJavBus(avid);

                    if (mag != null && mag.Count > 0)
                    {
                        result.Add(f, mag);
                    }
                }
            }

            return result;
        }

        //下载漫画
        public static void TestDownload(string name, string folder)
        {
            Console.WriteLine($"正在处理{name}");

            var prefix = "http://www.5ikanhm.top";
            var html = HtmlManager.GetHtmlContentViaUrl("http://www.5ikanhm.top/book/423");

            if (html.Success)
            {
                Console.WriteLine($"获取内容成功");

                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }

                Thread.Sleep(100);

                var comicFolder = folder + "\\" + name + "\\";
                Directory.CreateDirectory(comicFolder);

                Console.WriteLine($"新建文件夹成功");

                List<string> chapters = new List<string>();

                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html.Content);

                string xpath = "//ul[@id='detail-list-select']//a";

                HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes(xpath);

                foreach (var node in nodes)
                {
                    chapters.Add(prefix + node.Attributes["href"].Value);
                }

                foreach(var chapter in chapters)
                {
                    Console.WriteLine($"处理章节{chapter}");

                    var chtml = HtmlManager.GetHtmlContentViaUrl(chapter, agent: "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1");

                    if (chtml.Success)
                    {
                        Console.WriteLine($"处理章节内容成功");
                        htmlDocument.LoadHtml(chtml.Content);

                        string cNameXpath = "//p[@class='view-fix-top-bar-title']";
                        string cXpath = "//div[@id='cp_img']//img";

                        HtmlNode cNameNode = htmlDocument.DocumentNode.SelectSingleNode(cNameXpath);
                        HtmlNodeCollection cImgNodes = htmlDocument.DocumentNode.SelectNodes(cXpath);

                        var chaperFolder = comicFolder + cNameNode.InnerText + "\\";
                        Directory.CreateDirectory(chaperFolder);

                        int i = 1;

                        foreach (var img in cImgNodes)
                        {
                            DownloadHelper.DownloadFile(img.Attributes["data-original"].Value, chaperFolder + i + ".jpg");

                            Console.WriteLine($"处理章节{cNameNode.InnerText}, 第 {i++} 张图片");
                        }
                    }
                }
            }
        }

        public static void Get115ShaAndMatchLocal()
        {
            var filesIn115 = OneOneFiveService.Get115FilesModel();

            foreach (var drive in Environment.GetLogicalDrives())
            {
                Console.WriteLine($"处理 {drive}");

                List<string> files = new List<string>();
                List<string> toFin = new List<string>();
                List<string> to115 = new List<string>();

                var fin = drive + "fin\\";
                var up = drive + "up115\\";

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
                    if (filesIn115.Exists(x => x.n == Path.GetFileName(file) && x.s == new FileInfo(file).Length))
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
    }
}
