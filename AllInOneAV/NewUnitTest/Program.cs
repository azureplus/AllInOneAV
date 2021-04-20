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
            //var test = OneOneFiveService.RemoveDuplicated115Files();
            //Console.WriteLine(test);

            //OneOneFiveService.Match115AndMoveLocalFile();

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

            //JavBusDownloadHelper.AvatorMatch();

            //var extraFiles = OneOneFiveService.Get115HasButLocal();
            //OneOneFiveService.DeleteList(extraFiles, "1834397846621504875");

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
