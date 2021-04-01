using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using Model.ScanModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utils;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanAllAndMatch
{
    public class Process
    {
        private static List<string> formats = JavINIClass.IniReadValue("Scan", "Format").Split(',').ToList();
        private static List<string> excludes = JavINIClass.IniReadValue("Scan", "Exclude").Split(',').ToList();

        public static void Start()
        {
            try
            {
                Console.WriteLine(string.Format("开始扫描 {0}", DateTime.Now.ToLongTimeString()));
                var drivers = Environment.GetLogicalDrives().ToList();

                List<FileInfo> fi = new List<FileInfo>();
                List<Match> temp = new List<Match>();
                List<Match> matches = new List<Match>();

                foreach (var driver in drivers)
                {
                    Console.WriteLine(string.Format("添加扫描驱动器: {0}", driver));

                    if (Directory.Exists(driver + "\\fin\\"))
                    {
                        if (!string.IsNullOrEmpty(FileUtility.GetFilesRecursive(driver + "\\fin\\", formats, excludes, fi, 100)))
                        {
                            Console.WriteLine("获取文件失败");
                        }
                    }

                    if (Directory.Exists(driver + "\\up115\\"))
                    {
                        if (!string.IsNullOrEmpty(FileUtility.GetFilesRecursive(driver + "\\up115\\", formats, excludes, fi, 100)))
                        {
                            Console.WriteLine("获取文件失败");
                        }
                    }
                }

                var avs = JavDataBaseManager.GetAllAV();
                avs.ForEach(x => x.ID = x.ID.ToUpper());
                var prefix = FileUtility.GetPrefix(avs);

                Console.WriteLine(string.Format("获取AV {0} 条, 前缀 {1} 条", avs.Count, prefix.Count));

                Parallel.ForEach(fi, new ParallelOptions { MaxDegreeOfParallelism = 10 }, file => 
                {
                    var split = file.Name.Split('-');
                     if (split.Length >= 3)
                     {
                         var id = split[0].ToUpper() + "-" + split[1].ToUpper();

                         var matchedAvs = avs.Where(x => x.ID == id).ToList();

                         if (matchedAvs != null)
                         {
                             Console.WriteLine(string.Format("{0} 找到 {1} 个匹配AV", file.FullName, matchedAvs.Count));

                             foreach (var av in matchedAvs)
                             {
                                 Match m = new Match
                                 {
                                     AvID = id,
                                     AvName = av.Name,
                                     Location = file.DirectoryName,
                                     Name = file.Name,
                                     MatchAVId = av.AvId
                                 };

                                 matches.Add(m);
                             }
                         }
                     }
                 });

                Console.WriteLine("清空匹配记录");
                ScanDataBaseManager.ClearMatch();

                foreach (var match in matches)
                {
                    try
                    {
                        ScanDataBaseManager.SaveMatch(match);
                    }
                    catch (Exception ee)
                    {

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.WriteLine(string.Format("扫描结束 {0}", DateTime.Now.ToLongTimeString()));

                new RestClient("https://api.day.app").Get("4z4uANLXpe8BXT3wAZVe9F/刷新匹配完成");
            }
        }
    }
}
