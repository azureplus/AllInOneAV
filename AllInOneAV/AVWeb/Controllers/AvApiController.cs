using AVWeb.Filter;
using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using log4net;
using Microsoft.Win32.TaskScheduler;
using Model.Common;
using Model.JavModels;
using Model.ScanModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using Utils;

namespace AVWeb.Controllers
{
    [RoutePrefix("avapi")]
    public class AVApiController : ApiController
    {
        [Base]
        /// <summary>
        /// 播放AV的流地址,目前测试阶段,传入从GetMatch接口获得的filePath
        /// </summary>
        /// <param name="filename">输入文件地址</param>
        /// <returns>异步视频流,HTML用video标签接收</returns>
        [Route("PlayAv")]
        [HttpGet]
        public HttpResponseMessage PlayAv(string filename)
        {
            filename = HttpUtility.UrlDecode(filename);
            if (Request.Headers.Range != null)
            {
                try
                {
                    Encoder stringEncoder = Encoding.UTF8.GetEncoder();
                    byte[] stringBytes = new byte[stringEncoder.GetByteCount(filename.ToCharArray(), 0, filename.Length, true)];
                    stringEncoder.GetBytes(filename.ToCharArray(), 0, filename.Length, stringBytes, 0, true);
                    MD5CryptoServiceProvider MD5Enc = new MD5CryptoServiceProvider();
                    string hash = BitConverter.ToString(MD5Enc.ComputeHash(stringBytes)).Replace("-", string.Empty);

                    HttpResponseMessage partialResponse = Request.CreateResponse(HttpStatusCode.PartialContent);
                    partialResponse.Headers.AcceptRanges.Add("bytes");
                    partialResponse.Headers.ETag = new EntityTagHeaderValue("\"" + hash + "\"");

                    var stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                    partialResponse.Content = new ByteRangeStreamContent(stream, Request.Headers.Range, new MediaTypeHeaderValue("video/mp4"));
                    return partialResponse;
                }
                catch (Exception ex)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable);
            }
        }

        [Base]
        /// <summary>
        /// 上传需要自动化下载的种子文件
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("PostSeedFiles")]
        public string PostSeedFiles()
        {
            return PostFiles(HttpContext.Current.Request.Files, "c:\\FileUpload\\Seeds\\", false, ".torrent");
        }

        [HttpGet]
        [Route("GetNextRunTime")]
        public NextRunModel GetNextRunTime(string token, string name = "ScanJavJob")
        {
            NextRunModel ret = new NextRunModel();
            var to = ScanDataBaseManager.GetToken().Token;

            if (to == token)
            {
                TaskService ts = new TaskService();
                var task = ts.FindTask("ScanJavJob");

                if (task != null)
                {
                    ret.NextRunTime = task.NextRunTime;
                    ret.NextRunCountMinutes = (int)Math.Ceiling((task.NextRunTime - DateTime.Now).TotalMinutes);
                }
            }

            return ret;
        }

        [HttpGet]
        [Route("SiriRunJob")]
        public TaskCommonModel SiriRunJob(string token, string jobName = "SiriRun", int page = 15, string website = "jav")
        {
            TaskCommonModel ret = new TaskCommonModel();
            var to = ScanDataBaseManager.GetToken().Token;

            if (to == token)
            {
                var parameter = new ScanParameter();

                if (website == "jav")
                {
                    parameter.IsAsc = true;
                    parameter.PageSize = page;
                    parameter.StartingPage = new List<string>() { "http://www.javlibrary.com/cn/vl_update.php?mode=" };
                }

                if (website == "bus")
                {
                    parameter.IsAsc = true;
                    parameter.PageSize = page;
                    parameter.StartingPage = new List<string>() { "https://www.javbus.com/page" };
                }

                var jobId = ScanDataBaseManager.InsertScanJob($"{jobName} {website} {page} 页", JsonConvert.SerializeObject(parameter), website);

                ret.Message = "建立成功";
            }
            else
            {
                ret.Message = "没有权限";
            }

            return ret;
        }

        [HttpGet]
        [Route("RunTask")]
        public TaskCommonModel RunTask(string token, string name = "ScanJavJob")
        {
            TaskCommonModel ret = new TaskCommonModel();
            var to = ScanDataBaseManager.GetToken().Token;

            if (to == token)
            {
                TaskService ts = new TaskService();
                var task = ts.FindTask(name);

                ret.Message = "程序没有执行";

                if (task != null && task.State == TaskState.Ready)
                {
                    task.Run();

                    ret.Message = "开始执行";
                }
            }
            else
            {
                ret.Message = "没有权限";
            }

            return ret;
        }

        [HttpGet]
        [Route("EverythingSearch")]
        public Model.ScanModels.EverythingResult EverythingSearch(string token, string content)
        {
            var to = ScanDataBaseManager.GetToken().Token;
            var retModel = new Model.ScanModels.EverythingResult();

            if (to == token)
            {
                var htmlModel = HtmlManager.GetHtmlContentViaUrl("http://localhost:8086/" + @"?s=&o=0&j=1&p=c&path_column=1&size_column=1&j=1&q=!c:\ " + EverythingHelper.Extensions + " " + content);

                if (htmlModel.Success)
                {
                    retModel = JsonConvert.DeserializeObject<Model.ScanModels.EverythingResult>(htmlModel.Content);

                    if (retModel != null && retModel.results != null)
                    {
                        retModel.results = retModel.results.OrderByDescending(x => double.Parse(x.size)).ToList();

                        foreach (var r in retModel.results)
                        {
                            r.sizeStr = FileSize.GetAutoSizeString(double.Parse(r.size), 1);
                            r.location = "本地";
                        }

                        return retModel;
                    }
                    else
                    {
                        retModel = new Model.ScanModels.EverythingResult();
                        retModel.results = new List<EverythingFileResult>();

                        var oneOneFiveFiles = ScanDataBaseManager.GetOneOneFiveShaMapping(content);

                        if (oneOneFiveFiles != null && oneOneFiveFiles.Any())
                        {
                            retModel.totalResults = oneOneFiveFiles.Count + "";

                            foreach (var file in oneOneFiveFiles)
                            {
                                EverythingFileResult temp = new EverythingFileResult();
                                temp.size = file.FileSize + "";
                                temp.sizeStr = FileSize.GetAutoSizeString(double.Parse(file.FileSize + ""), 1);
                                temp.location = "115";
                                temp.name = file.FileName;

                                retModel.results.Add(temp);
                            }
                        }
                    }
                }
            }

            return new Model.ScanModels.EverythingResult();
        }

        [HttpGet]
        [Route("GetReport")]
        public ReportVM GetReport(string token, int top = 5)
        {
            ReportVM ret = new ReportVM();
            var to = ScanDataBaseManager.GetToken().Token;

            if (to == token)
            {
                StringBuilder sb = new StringBuilder();
                var report = ScanDataBaseManager.GetReport();
                var items = ScanDataBaseManager.ReportItem(report.ReportId);

                ret.TotalCount = report.TotalExist;
                sb.AppendLine($"总Av数量: [{ret.TotalCount}]");
                ret.TotalSizeStr = FileSize.GetAutoSizeString((double)report.TotalExistSize, 1);
                sb.AppendLine($"总Av大小: [{ret.TotalSizeStr}]");
                ret.TotalSize = (double)report.TotalExistSize;
                ret.ChineseCount = report.ChineseCount;
                sb.AppendLine($"中文Av数量: [{ret.ChineseCount}]");
                ret.FileLessThan1G = report.LessThenOneGiga;
                sb.AppendLine($"文件小于1GB: [{ret.FileLessThan1G}]");
                ret.FileLargeThan1G = report.OneGigaToTwo;
                sb.AppendLine($"大于1GB小于2GB: [{ret.FileLargeThan1G}]");
                ret.FileLargeThan2G = report.TwoGigaToFour;
                sb.AppendLine($"大于2GB小于4GB: [{ret.FileLargeThan2G}]");
                ret.FileLargeThan4G = report.FourGigaToSix;
                sb.AppendLine($"大于4GB小于6GB: [{ret.FileLargeThan4G}]");
                ret.FileLargeThan6G = report.GreaterThenSixGiga;
                sb.AppendLine($"文件大于6GB: [{ret.FileLargeThan6G}]");

                var extensionModel = JsonConvert.DeserializeObject<Dictionary<string, int>>(report.Extension);

                ret.Formats = extensionModel;
                sb.AppendLine("后缀分布:");
                foreach (var ext in extensionModel)
                {
                    sb.AppendLine($"\t{ext.Key} : {ext.Value}");
                }

                foreach (ReportType type in Enum.GetValues(typeof(ReportType)))
                {
                    List<ReportItem> i = new List<ReportItem>();
                    switch (type)
                    {
                        case ReportType.Actress:
                            i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(top).ToList();

                            sb.AppendLine("女优TOP" + top);

                            foreach (var temp in i)
                            {
                                var name = temp.ItemName;
                                var count = temp.ExistCount;
                                var ratio = $"{temp.ExistCount} / {temp.TotalCount}";
                                var size = FileSize.GetAutoSizeString(temp.TotalSize, 1);

                                sb.AppendLine($"\t{name} -> 作品 {ratio}，总大小 {size}");
                            }

                            break;
                        case ReportType.Category:
                            i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(top).ToList();

                            sb.AppendLine("分类TOP" + top);

                            foreach (var temp in i)
                            {
                                var name = temp.ItemName;
                                var count = temp.ExistCount;
                                var ratio = $"{temp.ExistCount} / {temp.TotalCount}";
                                var size = FileSize.GetAutoSizeString(temp.TotalSize, 1);

                                sb.AppendLine($"\t{name} -> 作品 {ratio}，总大小 {size}");
                            }

                            break;
                        case ReportType.Prefix:
                            i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(top).ToList();

                            sb.AppendLine("番号TOP" + top);

                            foreach (var temp in i)
                            {
                                var name = temp.ItemName;
                                var count = temp.ExistCount;
                                var ratio = $"{temp.ExistCount} / {temp.TotalCount}";
                                var size = FileSize.GetAutoSizeString(temp.TotalSize, 1);

                                sb.AppendLine($"\t{name} -> 作品 {ratio}，总大小 {size}");
                            }

                            break;
                        case ReportType.Company:
                            i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(top).ToList();

                            sb.AppendLine("公司TOP" + top);

                            foreach (var temp in i)
                            {
                                var name = temp.ItemName;
                                var count = temp.ExistCount;
                                var ratio = $"{temp.ExistCount} / {temp.TotalCount}";
                                var size = FileSize.GetAutoSizeString(temp.TotalSize, 1);

                                sb.AppendLine($"\t{name} -> 作品 {ratio}，总大小 {size}");
                            }

                            break;
                        case ReportType.Date:
                            i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(top).ToList();

                            sb.AppendLine("日期TOP" + top);

                            foreach (var temp in i)
                            {
                                var name = temp.ItemName;
                                var count = temp.ExistCount;
                                var ratio = $"{temp.ExistCount} / {temp.TotalCount}";
                                var size = FileSize.GetAutoSizeString(temp.TotalSize, 1);

                                sb.AppendLine($"\t{name} -> 作品 {ratio}，总大小 {size}");
                            }

                            break;
                    }
                }

                ret.ShowContent = sb.ToString();

            }

            return ret;
        }


        [HttpPost]
        [Route("Save115Cookie")]
        public string Save115Cookie(string cookie)
        {
            List<CookieItem> items = new List<CookieItem>();

            CookieContainer cc = new CookieContainer();

            var cookieData = new ChromeCookieReader().ReadCookies(".115.com");

            if (cookieData != null)
            {
                items.AddRange(cookieData);
            }

            try
            {
                if (!string.IsNullOrEmpty(cookie))
                {
                    foreach (var item in cookie.Split(';'))
                    {
                        items.Add(new CookieItem()
                        {
                            Name = item.Split('=')[0].Trim(),
                            Value = item.Split('=')[1].Trim(),
                        });
                    }
                }

                if (items.Count > 0)
                {
                    ScanDataBaseManager.TruncateOneOneFiveCookie();

                    ScanDataBaseManager.InsertOneOneFiveCookie(new OneOneFiveCookieModel
                    {
                        OneOneFiveCookie = JsonConvert.SerializeObject(items)
                    });
                }
            }
            catch (Exception)
            {
                return "fail";
            }

            return "success";
        }
        #region 工具
        private string PostFiles(HttpFileCollection filelist, string folder, bool addDate, string ext)
        {
            StringBuilder sb = new StringBuilder();

            if (filelist != null && filelist.Count > 0)
            {
                for (int i = 0; i < filelist.Count; i++)
                {
                    HttpPostedFile file = filelist[i];
                    string fileName = file.FileName;

                    if (fileName.ToLower().Contains(ext))
                    {
                        if (addDate)
                        {
                            folder = folder + DateTime.Now.ToString("yyyy-MM-dd") + "\\";
                        }

                        DirectoryInfo di = new DirectoryInfo(folder);

                        if (!di.Exists)
                        {
                            di.Create();
                        }

                        try
                        {
                            file.SaveAs(folder + fileName);
                            sb.AppendLine("上传文件写入成功: " + (folder + fileName).Replace("\\", "/"));
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine("上传文件写入失败: " + fileName + Environment.NewLine + ex.ToString());
                        }
                    }
                    else
                    {
                        sb.AppendLine("传入格式不正确: " + fileName);
                    }
                }
            }
            else
            {
                sb.AppendLine("上传的文件信息不存在！");
            }

            return sb.ToString();
        }
        #endregion
    }
}
