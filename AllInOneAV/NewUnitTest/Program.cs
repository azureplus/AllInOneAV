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
            //OneOneFiveService.Match115AndMoveLocalFile();

            //移除115同名文件（保留文件体积大的）
            //var deleteSize = OneOneFiveService.RemoveDuplicated115Files();
            //Console.WriteLine($"共删除 {deleteSize} ");

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
    }
}
