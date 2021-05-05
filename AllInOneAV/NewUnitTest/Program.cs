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
using System.Windows.Forms;
using Utils;

namespace NewUnitTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //var ret = OneOneFiveService.CheckFolderSameShaRecursive("0", new List<string>() { "2068937774368408801" });

            //匹配115并移动本地文件    
            OneOneFiveService.Match115AndMoveLocalFile();

            //移除115同名文件（保留文件体积大的）
            //var deleteSize = OneOneFiveService.RemoveDuplicated115Files();
            //Console.WriteLine($"共删除 {deleteSize} ");

            //TestSearchEverything("ATID-462");

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
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

        public static void TestSearchEverything(string content)
        {
            var retModel = new Model.ScanModels.EverythingResult();
            var htmlModel = HtmlManager.GetHtmlContentViaUrl("http://localhost:8086/" + @"?s=&o=0&j=1&p=c&path_column=1&size_column=1&j=1&q=!c:\ " + EverythingHelper.Extensions + " " + content);

            if (htmlModel.Success)
            {
                retModel = JsonConvert.DeserializeObject<Model.ScanModels.EverythingResult>(htmlModel.Content);

                if (retModel != null && retModel.results != null && retModel.results.Count > 0)
                {
                    retModel.results = retModel.results.OrderByDescending(x => double.Parse(x.size)).ToList();

                    foreach (var r in retModel.results)
                    {
                        r.sizeStr = FileSize.GetAutoSizeString(double.Parse(r.size), 1);
                        r.location = "本地";
                    }
                }
                else
                {
                    retModel = new Model.ScanModels.EverythingResult
                    {
                        results = new List<EverythingFileResult>()
                    };

                    List<FileItemModel> oneOneFiveFiles = new List<FileItemModel>();

                    oneOneFiveFiles = OneOneFiveService.Get115SearchFileResult(OneOneFiveService.Get115Cookie(), content);

                    oneOneFiveFiles.AddRange(OneOneFiveService.Get115SearchFileResult(OneOneFiveService.Get115Cookie(), content, "2068937774368408801"));

                    if (oneOneFiveFiles != null && oneOneFiveFiles.Any())
                    {
                        var targetFile = oneOneFiveFiles.Where(x => x.n.ToLower().Contains(content.ToLower()) && !string.IsNullOrEmpty(x.fid)).ToList();
                        retModel.totalResults = targetFile.Count + "";

                        if (targetFile != null)
                        {
                            foreach (var file in targetFile)
                            {
                                EverythingFileResult temp = new EverythingFileResult
                                {
                                    size = file.s + "",
                                    sizeStr = FileSize.GetAutoSizeString(double.Parse(file.s + ""), 1),
                                    location = "115网盘",
                                    name = file.n
                                };

                                retModel.results.Add(temp);
                            }
                        }
                    }
                }
            }
        }
    }
}
