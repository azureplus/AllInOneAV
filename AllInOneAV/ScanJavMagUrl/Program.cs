using DataBaseManager.ScanDataBaseHelper;
using Model.Common;
using Model.ScanModels;
using Newtonsoft.Json;
using Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace ScanJavMagUrl
{
    class Program
    {
        static List<RefreshModel> models = new List<RefreshModel>();
        static bool IsFinish = true;
        static ScanJob model = null;

        static void Main(string[] args)
        {
            string arg = "";
            int jobId = 0;

            model = ScanDataBaseManager.GetFirstScanJob();

            if (args.Length == 0)
            {
                arg = " refresh " + 15;

                ScanDataBaseManager.DeleteRemoteScanMag();
            }
            else if (args.Length == 1)
            {
                if (model != null)
                {
                    if (model.Website == "jav")
                    {
                        var parameter = JsonConvert.DeserializeObject<ScanParameter>(model.ScanParameter);
                        parameter.ScanJobId = model.ScanJobId;

                        if (parameter != null && parameter.StartingPage != null && parameter.StartingPage.Count > 0)
                        {
                            arg = string.Format("dolist {0} {1} {2}", string.Join(",", parameter.StartingPage), parameter.IsAsc, parameter.PageSize);
                            jobId = parameter.ScanJobId;

                            ScanDataBaseManager.SetScanJobFinish(jobId, -1);

                            DoJob(arg, jobId);
                        }
                    }

                    if (model.Website == "bus")
                    {
                        var parameter = JsonConvert.DeserializeObject<ScanParameter>(model.ScanParameter);
                        parameter.ScanJobId = model.ScanJobId;

                        if (parameter != null && parameter.StartingPage != null && parameter.StartingPage.Count > 0)
                        {
                            jobId = parameter.ScanJobId;
                            ScanDataBaseManager.SetScanJobFinish(jobId, -1, parameter.PageSize * 30);

                            DoJob(arg, jobId, parameter);
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            while (IsFinish)
            {
                
            }

            ScanDataBaseManager.SetScanJobFinish(jobId, 1, models.Count);

            new RestClient("https://api.day.app").Get("4z4uANLXpe8BXT3wAZVe9F/下载种子文件完成");
        }

        async static void DoJob(string arg, int jobId, ScanParameter parameter = null)
        {
            if (parameter == null)
            {
                await StartJavRefresh("", arg, OutputJavRefresh);
            }
            else
            {
                models = JavBusDownloadHelper.GetJavbusAVList(parameter.StartingPage.FirstOrDefault(), parameter.PageSize, parameter.IsAsc);
            }

            ScanDataBaseManager.SetScanJobFinish(jobId, -1, models.Count);

            await Task.Run(() => UpdateRefreshUi(jobId));

            IsFinish = false;
        }

        private async static Task StartJavRefresh(string exe, string arg, DataReceivedEventHandler output)
        {
            exe = "E:\\Github\\AllInOneAV\\AllInOneAV\\BatchJavScanerAndMacthMagUrl\\bin\\Debug\\BatchJavScanerAndMacthMagUrl.exe";

            using (var p = new Process())
            {
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = arg;

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;

                p.OutputDataReceived += output;

                p.Start();
                p.BeginOutputReadLine();

                await p.WaitForExitAsync();
            }
        }

        private static void OutputJavRefresh(object sendProcess, DataReceivedEventArgs output)
        {
            if (!string.IsNullOrEmpty(output.Data) && output.Data.StartsWith("AV:"))
            {
                var jsonStr = output.Data.Replace("AV:", "");

                RefreshModel rm = JsonConvert.DeserializeObject<RefreshModel>(jsonStr);

                Console.WriteLine("扫描 --> " + rm.Name);

                models.Add(rm);
            }
        }

        private static void UpdateRefreshUi(int jobId = 0)
        {
            Random ran = new Random();
            int count = 1;
            string sukebei = JavINIClass.IniReadValue("Mag", "sukebei");

            Parallel.ForEach(models, new ParallelOptions { MaxDegreeOfParallelism = 10 }, rm =>
            {
                RemoteScanMag entity = new RemoteScanMag();
                entity.JobId = jobId;

                Console.Write("处理 --> " + rm.Name + " " + count++ + "/" + models.Count);

                var token = ScanDataBaseManager.GetToken();

                var htmlResult = HtmlManager.GetHtmlContentViaUrl($"http://www.cainqs.com:8087/avapi/EverythingSearch?token={token.Token}&content=" + rm.Id);

                Model.ScanModels.EverythingResult searchResult = new Model.ScanModels.EverythingResult();
                List<MyFileInfo> matchFiles = new List<MyFileInfo>();

                if (htmlResult.Success)
                {
                    searchResult = JsonConvert.DeserializeObject<Model.ScanModels.EverythingResult>(htmlResult.Content);

                    if (searchResult != null && searchResult.results != null)
                    {
                        foreach (var result in searchResult.results)
                        {
                            var temp = new MyFileInfo();

                            if (result.location == "本地")
                            {
                                temp.Length = long.Parse(result.size);
                                temp.FullName = result.path + "\\" + result.name;
                            }
                            else
                            {
                                temp.Length = long.Parse(result.size);
                                temp.FullName = "网盘" + long.Parse(result.size);
                            }

                            matchFiles.Add(temp);
                        }
                    }
                }

                List<SeedMagnetSearchModel> list = new List<SeedMagnetSearchModel>();

                if (sukebei == "pro" || sukebei == "si")
                {
                    list = MagService.SearchSukebei(id: rm.Id, web: sukebei);
                }
                else
                {
                    list = MagService.SearchJavBus(rm.Id, null);
                }

                if (list != null && list.Count > 0)
                {
                    if (matchFiles.Count > 0)
                    {
                        var biggestFile = matchFiles.FirstOrDefault(x => x.Length == matchFiles.Max(y => y.Length));
                        entity.SearchStatus = 2;
                        entity.MatchFile = biggestFile.FullName;
                    }
                    else
                    {
                        entity.SearchStatus = 1;
                    }

                    foreach (var seed in list)
                    {
                        entity.AvId = rm.Id;
                        entity.AvName = FileUtility.ReplaceInvalidChar(rm.Name);
                        entity.AvUrl = rm.Url;
                        entity.MagDate = seed.Date;
                        entity.MagSize = seed.Size;
                        entity.MagTitle = FileUtility.ReplaceInvalidChar(seed.Title);
                        entity.MagUrl = seed.MagUrl;

                        try
                        {
                            if (entity.MagTitle.Contains(rm.Id) || entity.MagTitle.Contains(rm.Id.Replace("-", "")))
                            {
                                ScanDataBaseManager.InsertRemoteScanMag(entity);
                            }
                        }
                        catch (Exception ee)
                        {
                            entity.MatchFile = "";
                            entity.SearchStatus = 1;
                            ScanDataBaseManager.InsertRemoteScanMag(entity);
                        }
                    }
                }
                else
                {
                    Console.Write(" 没搜到");
                    entity.SearchStatus = 0;
                }

                Console.WriteLine();
            });
        }
    }

    public static class ProcessExtensions
    {
        public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(tcs.SetCanceled);
            }

            return tcs.Task;
        }
    }
}
