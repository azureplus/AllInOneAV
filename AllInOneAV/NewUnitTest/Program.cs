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
            Console.WriteLine(test);

            //OneOneFiveService.Match115();

            //OneOneFiveService.Insert115FileSha();

            //OneOneFiveService.MatchLocalAndOneOneFive();

            //OneOneFiveService.Match115AndMoveLocalFile();

            //TestFind115(@"d://up115");

            //var oneOneFilveFiles = OneOneFiveService.Get115FilesModel();
            //var localFileve = OneOneFiveService.GetAllLocalAvs();

            //var list = OneOneFiveService.InitLocalSha(true);

            //OneOneFiveService.SyncLocalAnd115FileStatus();

            //var list = OneOneFiveService.GetFileToBeDeletedBySize(2);
            //var deleteSize = FileSize.GetAutoSizeString(list.Sum(x => x.Length), 1);

            Console.ReadKey();
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

        public static void TestFind115(string folder)
        {
            if (Directory.Exists(folder))
            {
                var files = new DirectoryInfo(folder).GetFiles();

                var oneOneFileFiles = OneOneFiveService.Get115FilesModel();

                foreach (var file in files)
                {
                    var matchRecord = oneOneFileFiles.FirstOrDefault(x => x.n.Equals(file.Name, StringComparison.OrdinalIgnoreCase) && x.s == file.Length);

                    if (matchRecord != null)
                    {
                        Console.WriteLine($"找到 {file.FullName}");
                    }
                    else
                    { 
                        var matchName = oneOneFileFiles.FirstOrDefault(x => x.n.Equals(file.Name, StringComparison.OrdinalIgnoreCase));

                        if (matchName != null)
                        {
                            Console.WriteLine($"找到名称 {file.FullName}");
                        }
                        else
                        {
                            Console.WriteLine($"什么都没有找到 {file.FullName}");
                        }
                    }
                }
            }
        }
    }
}
