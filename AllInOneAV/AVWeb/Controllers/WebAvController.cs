using AVWeb.Filter;
using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using Microsoft.Ajax.Utilities;
using Microsoft.Win32.TaskScheduler;
using Model.JavModels;
using Model.OneOneFive;
using Model.ScanModels;
using Model.WebModel;
using Newtonsoft.Json;
using Service;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Utils;

namespace AVWeb.Controllers
{
    public class WebAvController : Controller
    {
        [Base]
        public ActionResult Index()
        {
            return View();
        }

        [Rights]
        public ActionResult Play()
        {
            return View();
        }

        [Rights]
        public ActionResult UploadSeeds()
        {
            return View();
        }

        public ActionResult PlayAv(string filePath)
        {
            var host = "http://www.cainqs.com:8087/avapi/playav?filename=" + filePath;
            ViewData.Add("path", host);

            return View();
        }

        [Rights]
        public ActionResult GetAv(string search, bool onlyExist = false, string searchType = "all", int page = 1, int pageSize = 20)
        {
            var scanResult = new List<ScanResult>();
            string scanCache = "scan";

            if (CacheTools.HasCache(scanCache))
            {
                scanResult = CacheTools.GetCache<List<ScanResult>>(scanCache);
            }
            else
            {
                scanResult = ScanDataBaseManager.GetMatchScanResult();
                CacheTools.CacheInsert(scanCache, scanResult, DateTime.Now.AddHours(3));
            }

            if (onlyExist)
            {
                scanResult = scanResult.Where(x => !string.IsNullOrEmpty(x.Location)).ToList();
            }

            var toBePlay = new List<ScanResult>();
            List<ScanResult> namePlay = new List<ScanResult>();
            List<ScanResult> actressPlay = new List<ScanResult>();
            List<ScanResult> categoryPlay = new List<ScanResult>();
            List<ScanResult> prefixPlay = new List<ScanResult>();
            List<ScanResult> direPlay = new List<ScanResult>();

            if (searchType == "all" || searchType == "id")
            {
                foreach (var r in scanResult)
                {
                    if (r.AvId == search.ToUpper())
                    {
                        toBePlay.Add(r);
                    }
                }
            }

            if (searchType == "all" || searchType == "actress")
            {
                foreach (var r in scanResult)
                {
                    foreach (var ac in r.ActressList)
                    {
                        if (ac.Contains(search))
                        {
                            toBePlay.Add(r);
                        }
                    }
                }
            }

            if (searchType == "all" || searchType == "category")
            {
                foreach (var r in scanResult)
                {
                    foreach (var ca in r.CategoryList)
                    {
                        if (ca.Contains(search))
                        {
                            toBePlay.Add(r);
                        }
                    }
                }
            }

            if (searchType == "all" || searchType == "prefix")
            {
                foreach (var r in scanResult)
                {
                    if (r.Prefix.Contains(search.ToUpper()))
                    {
                        toBePlay.Add(r);
                    }
                }
            }

            if (searchType == "all" || searchType == "director")
            {
                foreach (var r in scanResult)
                {
                    if (r.Director.Contains(search))
                    {
                        toBePlay.Add(r);
                    }
                }
            }

            var pageContent = toBePlay.OrderByDescending(x=>x.ReleaseDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewData.Add("avs", pageContent);
            ViewData.Add("search", search);
            ViewData.Add("count", (int)Math.Ceiling((decimal)toBePlay.Count / pageSize));
            ViewData.Add("size", pageSize);
            ViewData.Add("current", page);
            ViewData.Add("total", toBePlay.Count);

            return View();
        }

        [Rights]
        public ActionResult Av(int avId)
        {
            var av = JavDataBaseManager.GetAV(avId);
            var match = ScanDataBaseManager.GetMatchScanResult(avId);

            if (av == null)
            {
                av = new AV();
            }

            if (match == null)
            {
                match = new ScanResult();
            }

            ViewData.Add("av", av);
            ViewData.Add("match", match);

            return View();
        }

        [Base]
        public JsonResult GetComics(int page = 1, int pageSize = 50)
        {
            string message = "";
            bool success = false;
            FileInfo[] files = null;
            int totalCount = 0;
            int currentCount = 0;

            try
            {
                files = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\ComicDownload\\").GetFiles();
                totalCount = files.Count();
                files = files.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
                currentCount = files.Count();
                success = true;
            }
            catch (Exception ee)
            {
                message = ee.ToString();
            }

            return Json(new { success = success, message = message, data = files.Select(x=>x.Name).ToList(), totalCount = totalCount, currentCount = currentCount, page = page, pageSize = pageSize }, JsonRequestBehavior.AllowGet);
        }

        [Base]
        public JsonResult GetComic(string name)
        {
            string message = "文件未找到";
            bool success = false;
            FileInfo fi = null;
            string url = "";
            double size = 0;
            string sizeStr = "";

            var files = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\ComicDownload\\").GetFiles();
            fi = files.FirstOrDefault(x => x.Name == name);

            if (fi != null)
            {
                success = true;
                message = "";
                url = "http://www.cainqs.com:8087/comicdownload/" + fi.Name;
                size = fi.Length;
                sizeStr = FileSize.GetAutoSizeString(fi.Length, 1);
            }

            return Json(new { success = success, message = message, url = url, size = size, sizeStr = sizeStr }, JsonRequestBehavior.AllowGet);
        }

        [Base]
        public String Comic()
        {
            var template = "<a href=\"{0}\" booksize=\"{1}\" bookdate=\"{2}\">{3}</a><br>";
            var html = "<html><head><title>Index list.</title></head><body>{0}</body></html>";

            var files = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\ComicDownload\\").GetFiles();
            StringBuilder sb = new StringBuilder();

            foreach (var file in files)
            {
                sb.Append(string.Format(template, "http://www.cainqs.com:8087/comicdownload/" + HttpUtility.UrlEncode(file.Name, Encoding.UTF8), file.Length, file.CreationTimeUtc.ToFileTimeUtc(), file.Name));
            }


            return string.Format(html,sb.ToString());
        }

        [Rights]
        public ActionResult ShowMag(ShowMagType type = ShowMagType.All, int jobId = 0)
        {
            var data = ScanDataBaseManager.GetAllMagByJob(jobId).Where(x => !string.IsNullOrEmpty(x.AvId)).GroupBy(x => x.AvId).ToDictionary(x => x.Key, x=>x.ToList());
            Dictionary<ShowMagKey, List<RemoteScanMag>> ret = new Dictionary<ShowMagKey, List<RemoteScanMag>>();

            foreach (var d in data)
            {
                if (d.Value.Count > 0)
                {
                    var key = new ShowMagKey();
                    key.Key = d.Key;
                    key.Type |= ShowMagType.All;

                    //没有已存在文件
                    if (d.Value.FirstOrDefault().SearchStatus == 1)
                    {
                        d.Value.ForEach(x => x.ClassStr = "card bg-primary");
                        key.Type |= ShowMagType.OnlyNotExist;
                    }

                    //有已存在文件
                    if (d.Value.FirstOrDefault().SearchStatus == 2)
                    {
                        d.Value.ForEach(x => x.ClassStr = "card bg-success");
                        key.Type |= ShowMagType.OnlyExist;
                    }

                    if (d.Value.Exists(x => x.MagSize > 0) || (d.Value.Exists(x => x.MagTitle.ToUpper().Contains("HD")) || d.Value.Exists(x => x.MagTitle.ToUpper().Contains("FHD"))))
                    {
                        key.Type |= ShowMagType.HasMagSize;
                    }
                    else
                    {
                        key.Type |= ShowMagType.HasNoMagSize;
                    }

                    //TODO
                    if (!string.IsNullOrEmpty(d.Value.FirstOrDefault().MatchFile))
                    {
                        var count = d.Value.Count;

                        //扫描的时候文件在浏览的时候删除或者移动了位置，重新定位
                        if (!d.Value.FirstOrDefault().MatchFile.StartsWith("网盘") && d.Value.Exists(x => !System.IO.File.Exists(x.MatchFile)))
                        {
                            var newFiles = new EverythingHelper().SearchFile(d.Value.FirstOrDefault().AvId + "-" + d.Value.FirstOrDefault().AvName, Model.Common.EverythingSearchEnum.Video);

                            int i = 0;

                            foreach (var dTemp in d.Value)
                            {
                                if (i < newFiles.Count)
                                {
                                    dTemp.MatchFile = newFiles[i++].FullName;
                                }
                            }

                            count = newFiles.Count;
                        }

                        foreach (var temp in d.Value.Take(count))
                        {
                            if (!string.IsNullOrEmpty(temp.MatchFile) && !temp.MatchFile.StartsWith("网盘"))
                            {
                                temp.MatchFileSize = new FileInfo(temp.MatchFile).Length;
                            }

                            if (!string.IsNullOrEmpty(temp.MatchFile) && temp.MatchFile.StartsWith("网盘"))
                            {
                                temp.MatchFileSize = long.Parse(temp.MatchFile.Replace("网盘", ""));
                            }
                        }

                        if (d.Value.Max(x => x.MagSize >= x.MatchFileSize))
                        {
                            key.Type |= ShowMagType.GreaterThenExist;
                        }
                    }
                    else
                    {
                        if (d.Value.Max(x => x.MagSize > 0))
                        {
                            key.Type |= ShowMagType.GreaterThenNotExist;
                        }
                    }

                    if (d.Value.Exists(x => x.MagTitle.Contains(d.Key) || x.MagTitle.Contains(d.Key.Replace("-", ""))))
                    {
                        ret.Add(key, d.Value);
                    }
                }
            }

            ret = ret.Where(x => x.Key.Type.HasFlag(type)).ToDictionary(x => x.Key, x => x.Value);

            ViewData.Add("jobId", jobId);
            ViewData.Add("data", ret);        

            return View();
        }

        [Base]
        public ActionResult ShareFile(string name)
        {
            List<FileInfo> f = new List<FileInfo>();
            List<DirectoryInfo> d = new List<DirectoryInfo>();

            try
            {
                var dirs = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\Share\\" + name + "\\").GetDirectories();
                var files = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\Share\\" + name + "\\").GetFiles();

                foreach (var file in files)
                {
                    f.Add(file);
                }

                foreach (var dir in dirs)
                {
                    d.Add(dir);
                }

                ApplicationLog.Debug(name);

                ViewData.Add("name", name.ToUpper());
            }
            catch (Exception ee)
            {
                ApplicationLog.Error(ee.ToString());
            }

            ViewData.Add("file", f);
            ViewData.Add("dir", d);

            return View();
        }

        [Rights]
        public ActionResult ShareList()
        {
            var list = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\Share\\").GetDirectories();

            return View(list);
        }

        [Rights]
        public ActionResult ScanJav()
        {
            #region 按页面
            List<ScanMap> page = new List<ScanMap>();

            page.Add(new ScanMap()
            {
                Title = "新话题",
                Url = "http://www.javlibrary.com/cn/vl_update.php?mode="
            });

            page.Add(new ScanMap()
            {
                Title = "新加入",
                Url = "http://www.javlibrary.com/cn/vl_newentries.php?mode="
            });
            page.Add(new ScanMap()
            {
                Title = "最想要",
                Url = "http://www.javlibrary.com/cn/vl_mostwanted.php?mode="
            });
            page.Add(new ScanMap()
            {
                Title = "高评价",
                Url = "http://www.javlibrary.com/cn/vl_bestrated.php?mode="
            });
            page.Add(new ScanMap()
            {
                Title = "新发行",
                Url = "http://www.javlibrary.com/cn/vl_newrelease.php?mode="
            });

            ViewData.Add("page", page);
            #endregion

            #region 按演员
            var actress = JavDataBaseManager.GetActress();
            ViewData.Add("actress", actress);
            #endregion

            #region 按类型
            var cate = JavDataBaseManager.GetCategories();
            ViewData.Add("cate", cate);
            #endregion

            #region 按收藏
            var faviModel = ScanDataBaseManager.GetFaviScan();

            var favi = faviModel.GroupBy(x => x.Category).ToDictionary(x => x.Key, x => x.OrderBy(y=>y.FaviScanId).ToList());

            ViewData.Add("favi", favi);
            #endregion

            return View();
        }

        [Rights]
        public ActionResult ScanJobList(int pageSize = 20)
        {
            var model = ScanDataBaseManager.GetScanJob(pageSize);

            model.ForEach(x => x.CurrentItemCount = ScanDataBaseManager.GetScanJobItem(x.ScanJobId));

            return View(model);
        }

        [Rights]
        public JsonResult DeleteScanJob(int jobId)
        {
            var ret = 0;

            ret += ScanDataBaseManager.DeleteScanJob(jobId);
            ret += ScanDataBaseManager.DeleteRemoteMagScan(jobId);

            if (ret > 0)
            {
                return Json(new { success = "Success" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = "Fail" }, JsonRequestBehavior.AllowGet);
            }
        }

        [Base]
        public ActionResult GetValidateCode()
        {
            ValidateCode vCode = new ValidateCode();
            string code = vCode.CreateValidateCode(5);
            Session["ValidateCode"] = code;
            byte[] bytes = vCode.CreateValidateGraphic(code);
            return File(bytes, @"image/jpeg");
        }

        [Base]
        public ActionResult Login()
        {
            return View();
        }

        [Base]
        public ActionResult Logout()
        {
            var uName = CookieTools.GetCookie("uName").Value;
 
            if (!string.IsNullOrEmpty(uName))
            {
                CacheTools.CacheRemove(uName);

                CookieTools.RemoveCookie(uName);
                CookieTools.RemoveCookie("token");
            }

            return View("Index");
        }

        [Base]
        public ActionResult NoRights()
        {
            return View();
        }

        [Base]
        public ActionResult ShowChart()
        {
            var report = ScanDataBaseManager.GetReport();

            var items = ScanDataBaseManager.ReportItem(report.ReportId);

            string extension = "['后缀', 'total']";
            Dictionary<string, string> actress = new Dictionary<string, string>();
            Dictionary<string, string> category = new Dictionary<string, string>();
            Dictionary<string, string> director = new Dictionary<string, string>();
            Dictionary<string, string> company = new Dictionary<string, string>();
            Dictionary<string, string> publisher = new Dictionary<string, string>();
            Dictionary<string, string> prefix = new Dictionary<string, string>();
            Dictionary<string, string> date = new Dictionary<string, string>();

            Dictionary<string, string> actressRatio = new Dictionary<string, string>();
            Dictionary<string, string> categoryRatio = new Dictionary<string, string>();
            Dictionary<string, string> directorRatio = new Dictionary<string, string>();
            Dictionary<string, string> companyRatio = new Dictionary<string, string>();
            Dictionary<string, string> publisherRatio = new Dictionary<string, string>();
            Dictionary<string, string> prefixRatio = new Dictionary<string, string>();
            Dictionary<string, string> dateRatio = new Dictionary<string, string>();

            Dictionary<string, string> actressSize = new Dictionary<string, string>();
            Dictionary<string, string> categorySize = new Dictionary<string, string>();
            Dictionary<string, string> directorSize = new Dictionary<string, string>();
            Dictionary<string, string> companySize = new Dictionary<string, string>();
            Dictionary<string, string> publisherSize = new Dictionary<string, string>();
            Dictionary<string, string> prefixSize = new Dictionary<string, string>();
            Dictionary<string, string> dateSize = new Dictionary<string, string>();

            List<string> actressKey = new List<string>();
            List<int> actressValue = new List<int>();
            List<string> categoryKey = new List<string>();
            List<int> categoryValue = new List<int>();
            List<string> directorKey = new List<string>();
            List<int> directorValue = new List<int>();
            List<string> companyKey = new List<string>();
            List<int> companyValue = new List<int>();
            List<string> publisherKey = new List<string>();
            List<int> publisherValue = new List<int>();
            List<string> prefixKey = new List<string>();
            List<int> prefixValue = new List<int>();
            List<string> dateKey = new List<string>();
            List<int> dateValue = new List<int>();

            List<string> actressRatioKey = new List<string>();
            List<decimal> actressRatioValue = new List<decimal>();
            List<string> categoryRatioKey = new List<string>();
            List<decimal> categoryRatioValue = new List<decimal>();
            List<string> directorRatioKey = new List<string>();
            List<decimal> directorRatioValue = new List<decimal>();
            List<string> companyRatioKey = new List<string>();
            List<decimal> companyRatioValue = new List<decimal>();
            List<string> publisherRatioKey = new List<string>();
            List<decimal> publisherRatioValue = new List<decimal>();
            List<string> prefixRatioKey = new List<string>();
            List<decimal> prefixRatioValue = new List<decimal>();
            List<string> dateRatioKey = new List<string>();
            List<decimal> dateRatioValue = new List<decimal>();

            List<string> actressSizeKey = new List<string>();
            List<double> actressSizeValue = new List<double>();
            List<string> categorySizeKey = new List<string>();
            List<double> categorySizeValue = new List<double>();
            List<string> directorSizeKey = new List<string>();
            List<double> directorSizeValue = new List<double>();
            List<string> companySizeKey = new List<string>();
            List<double> companySizeValue = new List<double>();
            List<string> publisherSizeKey = new List<string>();
            List<double> publisherSizeValue = new List<double>();
            List<string> prefixSizeKey = new List<string>();
            List<double> prefixSizeValue = new List<double>();
            List<string> dateSizeKey = new List<string>();
            List<double> dateSizeValue = new List<double>();

            var extensionModel = JsonConvert.DeserializeObject<Dictionary<string, int>>(report.Extension);

            foreach (var e in extensionModel)
            {
                extension += string.Format(",['{1}',{0}]", e.Value, e.Key);
            }

            foreach (ReportType type in Enum.GetValues(typeof(ReportType)))
            {
                List<ReportItem> i = new List<ReportItem>();
                switch (type)
                {      
                    case ReportType.Actress:
                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            actressKey.Add("'" + temp.ItemName + "'");
                            actressValue.Add(temp.ExistCount);
                        }

                        i = items.Where(x => (ReportType)x.ReportType == type && x.TotalCount >= 100).OrderByDescending(x => (decimal)x.ExistCount / (decimal)x.TotalCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            actressRatioKey.Add("'" + temp.ItemName + "'");
                            actressRatioValue.Add(Math.Round(((decimal)temp.ExistCount / (decimal)temp.TotalCount) * 100, 1));
                        }

                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.TotalSize).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            actressSizeKey.Add("'" + temp.ItemName + "'");
                            actressSizeValue.Add(temp.TotalSize);
                        }

                        break;
                    case ReportType.Category:
                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            categoryKey.Add("'" + temp.ItemName + "'");
                            categoryValue.Add(temp.ExistCount);
                        }

                        i = items.Where(x => (ReportType)x.ReportType == type && x.TotalCount >= 100).OrderByDescending(x => (decimal)x.ExistCount / (decimal)x.TotalCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            categoryRatioKey.Add("'" + temp.ItemName + "'");
                            categoryRatioValue.Add(Math.Round(((decimal)temp.ExistCount / (decimal)temp.TotalCount) * 100, 1));
                        }

                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.TotalSize).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            categorySizeKey.Add("'" + temp.ItemName + "'");
                            categorySizeValue.Add(temp.TotalSize);
                        }

                        break;
                    case ReportType.Director:
                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            directorKey.Add("'" + temp.ItemName + "'");
                            directorValue.Add(temp.ExistCount);
                        }


                        i = items.Where(x => (ReportType)x.ReportType == type && x.TotalCount >= 100).OrderByDescending(x => (decimal)x.ExistCount / (decimal)x.TotalCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            directorRatioKey.Add("'" + temp.ItemName + "'");
                            directorRatioValue.Add(Math.Round(((decimal)temp.ExistCount / (decimal)temp.TotalCount) * 100, 1));
                        }

                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.TotalSize).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            directorSizeKey.Add("'" + temp.ItemName + "'");
                            directorSizeValue.Add(temp.TotalSize);
                        }

                        break;
                    case ReportType.Company:
                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            companyKey.Add("'" + temp.ItemName + "'");
                            companyValue.Add(temp.ExistCount);
                        }


                        i = items.Where(x => (ReportType)x.ReportType == type && x.TotalCount >= 100).OrderByDescending(x => (decimal)x.ExistCount / (decimal)x.TotalCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            companyRatioKey.Add("'" + temp.ItemName + "'");
                            companyRatioValue.Add(Math.Round(((decimal)temp.ExistCount / (decimal)temp.TotalCount) * 100, 1));
                        }

                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.TotalSize).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            companySizeKey.Add("'" + temp.ItemName + "'");
                            companySizeValue.Add(temp.TotalSize);
                        }

                        break;
                    case ReportType.Publisher:
                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            publisherKey.Add("'" + temp.ItemName + "'");
                            publisherValue.Add(temp.ExistCount);
                        }


                        i = items.Where(x => (ReportType)x.ReportType == type && x.TotalCount >= 100).OrderByDescending(x => (decimal)x.ExistCount / (decimal)x.TotalCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            publisherRatioKey.Add("'" + temp.ItemName + "'");
                            publisherRatioValue.Add(Math.Round(((decimal)temp.ExistCount / (decimal)temp.TotalCount) * 100, 1));
                        }

                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.TotalSize).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            publisherSizeKey.Add("'" + temp.ItemName + "'");
                            publisherSizeValue.Add(temp.TotalSize);
                        }

                        break;
                    case ReportType.Prefix:
                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            prefixKey.Add("'" + temp.ItemName + "'");
                            prefixValue.Add(temp.ExistCount);
                        }


                        i = items.Where(x => (ReportType)x.ReportType == type && x.TotalCount >= 100).OrderByDescending(x => (decimal)x.ExistCount / (decimal)x.TotalCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            prefixRatioKey.Add("'" + temp.ItemName + "'");
                            prefixRatioValue.Add(Math.Round(((decimal)temp.ExistCount / (decimal)temp.TotalCount) * 100, 1));
                        }

                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.TotalSize).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            prefixSizeKey.Add("'" + temp.ItemName + "'");
                            prefixSizeValue.Add(temp.TotalSize);
                        }

                        break;
                    case ReportType.Date:
                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.ExistCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            dateKey.Add("'" + temp.ItemName + "'");
                            dateValue.Add(temp.ExistCount);
                        }


                        i = items.Where(x => (ReportType)x.ReportType == type && x.TotalCount >= 100).OrderByDescending(x => (decimal)x.ExistCount / (decimal)x.TotalCount).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            dateRatioKey.Add("'" + temp.ItemName + "'");
                            dateRatioValue.Add(Math.Round(((decimal)temp.ExistCount / (decimal)temp.TotalCount) * 100, 1));
                        }

                        i = items.Where(x => (ReportType)x.ReportType == type).OrderByDescending(x => x.TotalSize).Take(20).ToList();

                        foreach (var temp in i)
                        {
                            dateSizeKey.Add("'" + temp.ItemName + "'");
                            dateSizeValue.Add(temp.TotalSize);
                        }

                        break;
                }
            }

            actress.Add("[" + string.Join(",", actressKey)  +"]", "[" + string.Join(",", actressValue) + "]");
            category.Add("[" + string.Join(",", categoryKey) + "]", "[" + string.Join(",", categoryValue) + "]");
            director.Add("[" + string.Join(",",  directorKey) + "]", "[" + string.Join(",", directorValue) + "]");
            company.Add("[" + string.Join(",", companyKey) + "]", "[" + string.Join(",", companyValue) + "]");
            publisher.Add("[" + string.Join(",", publisherKey) + "]", "[" + string.Join(",", publisherValue) + "]");
            date.Add("[" + string.Join(",", dateKey) + "]", "[" + string.Join(",", dateValue) + "]");
            prefix.Add("[" + string.Join(",",  prefixKey) + "]", "[" + string.Join(",", prefixValue) + "]");

            actressRatio.Add("[" + string.Join(",", actressRatioKey) + "]", "[" + string.Join(",", actressRatioValue) + "]");
            categoryRatio.Add("[" + string.Join(",", categoryRatioKey) + "]", "[" + string.Join(",", categoryRatioValue) + "]");
            directorRatio.Add("[" + string.Join(",", directorRatioKey) + "]", "[" + string.Join(",", directorRatioValue) + "]");
            companyRatio.Add("[" + string.Join(",", companyRatioKey) + "]", "[" + string.Join(",", companyRatioValue) + "]");
            publisherRatio.Add("[" + string.Join(",", publisherRatioKey) + "]", "[" + string.Join(",", publisherRatioValue) + "]");
            dateRatio.Add("[" + string.Join(",", dateRatioKey) + "]", "[" + string.Join(",", dateRatioValue) + "]");
            prefixRatio.Add("[" + string.Join(",", prefixRatioKey) + "]", "[" + string.Join(",", prefixRatioValue) + "]");

            actressSize.Add("[" + string.Join(",", actressSizeKey) + "]", "[" + string.Join(",", actressSizeValue) + "]");
            categorySize.Add("[" + string.Join(",", categorySizeKey) + "]", "[" + string.Join(",", categorySizeValue) + "]");
            directorSize.Add("[" + string.Join(",", directorSizeKey) + "]", "[" + string.Join(",", directorSizeValue) + "]");
            companySize.Add("[" + string.Join(",", companySizeKey) + "]", "[" + string.Join(",", companySizeValue) + "]");
            publisherSize.Add("[" + string.Join(",", publisherSizeKey) + "]", "[" + string.Join(",", publisherSizeValue) + "]");
            dateSize.Add("[" + string.Join(",", dateSizeKey) + "]", "[" + string.Join(",", dateSizeValue) + "]");
            prefixSize.Add("[" + string.Join(",", prefixSizeKey) + "]", "[" + string.Join(",", prefixSizeValue) + "]");

            ViewData.Add("countString", string.Format("['总数', 'total'],['总共',{0}],['存在',{1}]", report.TotalCount, report.TotalExist));
            ViewData.Add("chineseString", string.Format("['中文', 'total'],['总共',{0}],['中文',{1}]", report.TotalCount, report.ChineseCount));
            ViewData.Add("sizeString", string.Format("['大小', 'total'],['[0,1GB)',{0}], ['[1GB,2GB)',{1}], ['[2GB,4GB)',{2}], ['[4GB,6GB)',{3}], ['[6GB,∞)',{4}]", report.LessThenOneGiga, report.OneGigaToTwo, report.TwoGigaToFour, report.FourGigaToSix, report.GreaterThenSixGiga));
            ViewData.Add("extensionString", extension);

            ViewData.Add("actressString", actress);
            ViewData.Add("categoryString", category);
            ViewData.Add("directorString", director);
            ViewData.Add("companyString", company);
            ViewData.Add("publisherString", publisher);
            ViewData.Add("dateString", date);
            ViewData.Add("prefixString", prefix);

            ViewData.Add("actressRatioString", actressRatio);
            ViewData.Add("categoryRatioString", categoryRatio);
            ViewData.Add("directorRatioString", directorRatio);
            ViewData.Add("companyRatioString", companyRatio);
            ViewData.Add("publisherRatioString", publisherRatio);
            ViewData.Add("dateRatioString", dateRatio);
            ViewData.Add("prefixRatioString", prefixRatio);

            ViewData.Add("actressSizeString", actressSize);
            ViewData.Add("categorySizeString", categorySize);
            ViewData.Add("directorSizeString", directorSize);
            ViewData.Add("companySizeString", companySize);
            ViewData.Add("publisherSizeString", publisherSize);
            ViewData.Add("dateSizeString", dateSize);
            ViewData.Add("prefixSizeString", prefixSize);

            return View();
        }

        [Rights]
        public ActionResult Setting() 
        {
            string cookieMode = JavINIClass.IniReadValue("Jav", "cookieMode"); 
            string sukebei = JavINIClass.IniReadValue("Mag", "sukebei");

            ViewData.Add("cookieMode", cookieMode);
            ViewData.Add("sukebei", sukebei);

            return View();
        }

        [Rights]
        public JsonResult SaveIniSetting(string cookieMode, string magUrl)
        {
            bool ret = true;
            try
            {
                JavINIClass.IniWriteValue("Jav", "cookieMode", cookieMode);
                JavINIClass.IniWriteValue("Mag", "sukebei", magUrl);
            } catch (Exception ee)
            {
                ApplicationLog.Debug(ee.ToString());
                ret = false;
            }

            if (ret)
            {
                return Json(new { success = "success" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = "fail" }, JsonRequestBehavior.AllowGet);
        }

        [Base]
        public ActionResult Catelog(int limitGiga = 0, int page = 1, int pageSize = 200)
        {
            var scanResult = new List<ScanResult>();
            var toBe = new List<ScanResult>();
            string scanCache = "scan";

            if (CacheTools.HasCache(scanCache))
            {
                scanResult = CacheTools.GetCache<List<ScanResult>>(scanCache);
            }
            else
            {
                scanResult = ScanDataBaseManager.GetMatchScanResult();
                CacheTools.CacheInsert(scanCache, scanResult, DateTime.Now.AddHours(3));
            }

            scanResult = scanResult.Where(x => !string.IsNullOrEmpty(x.Location)).ToList();

            if (limitGiga > 0)
            {
                double limit = (double)limitGiga * 1024 * 1024 * 1024;
                ApplicationLog.Debug("catelog -> limitGiga: " + limitGiga + " limit: " + limit);
                ApplicationLog.Debug("catelog -> beforeCount: " + scanResult.Count);

                foreach (var s in scanResult)
                {
                    if (new FileInfo(s.AvFilePath).Length >= limit)
                    {
                        toBe.Add(s);
                    }
                }

                ApplicationLog.Debug("catelog -> afterCount: " + toBe.Count);
            }
            else
            {
                toBe = scanResult;
            }

            var pageContent = toBe.OrderByDescending(x => x.ReleaseDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewData.Add("avs", pageContent);
            ViewData.Add("count", (int)Math.Ceiling((decimal)toBe.Count / pageSize));
            ViewData.Add("size", pageSize);
            ViewData.Add("current", page);
            ViewData.Add("total", toBe.Count);
            ViewData.Add("limit", limitGiga);

            return View();
        }

        [Rights]
        public JsonResult SaveJavSetting(string category, string url, string name)
        {
            var ret = 0;

            try
            {
                FaviScan fs = new FaviScan();

                fs.Category = category;
                fs.Url = url;
                fs.Name = name;

                ret = ScanDataBaseManager.InsertFaviScan(fs);
            }
            catch (Exception ee)
            {
                ApplicationLog.Debug(ee.ToString());
            }

            if (ret > 0)
            {
                return Json(new { success = "success" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = "fail" }, JsonRequestBehavior.AllowGet);
        }

        [Base, HttpGet]
        public string GetWebViewLog(string where)
        {
            StringBuilder sb = new StringBuilder();

            string table = (@"<table class='table table-striped'>
                                    <thead>
                                      <tr>
                                        <th scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>控制器</th>
                                        <th scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>方法</th>
                                        <th scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>IP</th>
                                        <th scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>客户端</th>
                                        <th scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>登录</th>
                                        <th scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>时间</th>
                                        <th scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>参数</th>
                                      </tr>
                                    </thead>
                                    <tbody>
                                      {0}
                                    </tbody>
                                  </table>");

            var logs = ScanDataBaseManager.GetWebViewLog(where);

            foreach (var log in logs)
            {
                sb.Append(string.Format(@"<tr>
                                            <td scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>{0}</td>
                                            <td scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>{1}</td>
                                            <td scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>{2}</td>
                                            <td scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>{3}</td>
                                            <td scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>{4}</td>
                                            <td scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>{5}</td>
                                            <td scope='col' style='word-wrap:break-word;overflow:hidden;text-overflow:ellipsis;width:100px'>{6}</td>
                                        </tr>", log.Controller, log.Action, log.IPAddress, log.UserAgent, log.IsLogin == 1 ? "True" : "False", log.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"), log.Parameter));
            }

            return string.Format(table, sb.ToString());
        }

        [Base, HttpGet]
        public JsonResult SaveWish(int id, string avId, string file)
        {
            int ret = 0;

            var ip = Request.UserHostAddress;

            WishList entity = new WishList()
            {
                AvId = avId,
                IPAddress = ip,
                FilePath = file,
                Id = id
            };

            ret = ScanDataBaseManager.InsertWishList(entity);

            if (ret > 0)
            {
                return Json(new { success = "success" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = "fail" }, JsonRequestBehavior.AllowGet);
        }

        [Base, HttpPost]
        public ActionResult Login(string token = "")
        {
            string uName = Request.Form["userName"];
            string uPwd = Request.Form["userPassword"];
            string uValidate = Request.Form["validate"];
            string serviceCode = Session["ValidateCode"] as string;//服务器端验证码

            if (!string.IsNullOrEmpty(uName) && !string.IsNullOrEmpty(uPwd) && uValidate.Equals(serviceCode))
            {
                try
                {
                    if (ScanDataBaseManager.IsUser(uName, uPwd))
                    {
                        Guid guid = Guid.NewGuid();

                        CookieTools.AddCookie("token", guid.ToString(), DateTime.Now.AddDays(1), "");
                        CookieTools.AddCookie("uName", uName, DateTime.Now.AddDays(1), "");

                        CacheTools.CacheInsert(uName, guid.ToString(), DateTime.Now.AddDays(1));

                        TempData["LoginState"] = 1;

                        return Redirect("Index");
                    }
                }
                catch (Exception e)
                {

                }
            }

            return View("Login");
        }

        [Rights, HttpPost]
        public JsonResult PostScanJob(string jobName, string scanParameter, string website = "jav")
        {
            var jobId = ScanDataBaseManager.InsertScanJob(jobName, scanParameter, website);

            return Json(new { msg = "success", jobId = jobId });
        }

        [Rights, HttpGet]
        public JsonResult PostBusScanJob(string page, int size, string website = "bus", string jobName = "PostBus")
        {
            ScanParameter sp = new ScanParameter();

            sp.IsAsc = true;
            sp.StartingPage = new List<string>() { { page } };
            sp.Website = "bus";
            sp.PageSize = size;

            var jobId = ScanDataBaseManager.InsertScanJob(jobName, JsonConvert.SerializeObject(sp), website);

            return Json(new { msg = "success", jobId = jobId }, JsonRequestBehavior.AllowGet);
        }

        [Rights, HttpPost]
        public JsonResult Add115Task(string mag)
        {
            CookieContainer cc = new CookieContainer();
            bool ret = false;
            string msg = "";

            foreach (var t in JsonConvert.DeserializeObject<List<CookieItem>>(ScanDataBaseManager.GetOneOneFiveCookie().OneOneFiveCookie))
            {

                if (t.Name == "PHPSESSID" || t.Name == "UID" || t.Name == "CID" || t.Name == "SEID" || t.Name == "115_lang")
                {
                    Cookie c = new Cookie(t.Name, t.Value, "/", "115.com");
                    cc.Add(c);
                }
            }

            var split = mag.Split(new string[] { "magnet:?" }, StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x));

            Dictionary<string, string> param = new Dictionary<string, string>();

            if (split.Count() <= 1)
            {
                param.Add("url", mag);
            }
            else
            {
                int index = 0;
                foreach (var s in split)
                {
                    param.Add(string.Format("url[{0}]", index), "magnet:?" + s);

                    index++;
                }
            }

            param.Add("sign", "");
            param.Add("uid" , "340200422");
            param.Add("time", DateTime.Now.ToFileTimeUtc() + "");

            var returnStr = "";

            if (split.Count() <= 1)
            {
                returnStr = HtmlManager.Post("https://115.com/web/lixian/?ct=lixian&ac=add_task_url", param, cc);
            }
            else
            {
                returnStr = HtmlManager.Post("https://115.com/web/lixian/?ct=lixian&ac=add_task_urls", param, cc);
            }

            if (!string.IsNullOrEmpty(returnStr))
            {
                var data = Newtonsoft.Json.Linq.JObject.Parse(returnStr);

                bool.TryParse(data.Property("state").Value.ToString(), out ret);

                if (ret == false)
                {
                    msg = data.Property("error_msg").Value.ToString();
                }
            }

            if (string.IsNullOrEmpty(msg))
            { 
                msg = "下载成功";
            }

            return Json(new { status = ret, msg = msg}, JsonRequestBehavior.AllowGet);
        }

        [Rights]
        public ActionResult RematchAndGenerateReport()
        {
            var p = new Process
            {
                StartInfo =
                {
                    FileName = @"E:\Github\AllInOneAV\AllInOneAV\GenerateReport\bin\Debug\GenerateReport.exe",
                    UseShellExecute = false,
                    Arguments = " new",
                    CreateNoWindow = false,
                }
            };
            p.Start();

            return View();
        }

        [Rights]
        [ValidateInput(false)]
        public ActionResult RemoveSubFolder()
        {
            var svm = SystemService.GetSystemTreeVM();

            var json = JsonConvert.SerializeObject(svm, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            ViewData.Add("data", HttpUtility.UrlDecode(json));

            return View(svm);
        }

        [Rights]
        public ActionResult RemoveSubFolderList(string folder, string toFolder, int limit)
        {
            var model = RenameService.RemoveSubFolder(sourceFolder: folder, descFolder: toFolder, fileSizeLimit: limit);

            model = model.OrderBy(x => x.DescFile).ToList();

            return View(model);
        }

        [Rights, HttpPost]
        public JsonResult RemoveSubFolder(List<RemoveSubModel> data)
        {
            List<string> froms = new List<string>();
            List<string> tos = new List<string>();
            int count = 0;
            double size = 0;

            foreach (var d in data)
            {
                if (!string.IsNullOrEmpty(d.SrcFile) && !string.IsNullOrEmpty(d.DescFile) && System.IO.File.Exists(d.SrcFile))
                {
                    froms.Add(d.SrcFile);
                    tos.Add(d.DescFile);
                }
            }

            try
            {
                if (froms.Count == tos.Count)
                {
                    for (int i = 0; i < froms.Count; i++)
                    {
                        System.IO.File.Move(froms[i], tos[i]);
                        count++;
                        size += new FileInfo(tos[i]).Length;
                    }
                }
            }
            catch (Exception ee)
            {
                return Json(new { success = false, msg = "移动失败" });
            }

            return Json(new { success = true, msg = string.Format("移动成功, 共移动{0}个文件，总大小{1}", count, FileSize.GetAutoSizeString(size, 1)) });
        }

        [Rights]
        public ActionResult BatchRename()
        {
            return View();
        }

        [Rights]
        public ActionResult BatchRenameList(string folder, int limit)
        {
            var model = RenameService.PrepareRename(folder, limit);

            return View(model);
        }

        public ActionResult TestSocket()
        {
            return View();
        }

        public ActionResult RunJob(string router)
        {
            ViewData.Add("router", router);

            return View();
        }

        [Rights]
        public ActionResult MoveBackToLocal(int page, int pagesize, bool force, int limit = 2)
        {
            List<FileItemModel> models = new List<FileItemModel>();
            List<AV> avdb = new List<AV>();

            if (force)
            {
                models = OneOneFiveService.Get115FilesModel();
                avdb = JavDataBaseManager.GetAllAV();

                RedisService.DeleteHash("webav", "avs");
                RedisService.DeleteHash("webav", "moveback");

                RedisService.SetHash("webav", "avs", JsonConvert.SerializeObject(avdb));
                RedisService.SetHash("webav", "moveback", JsonConvert.SerializeObject(models));
            }
            else
            {
                avdb = JsonConvert.DeserializeObject<List<AV>>(RedisService.GetHash("webav", "avs"));
                models = JsonConvert.DeserializeObject<List<FileItemModel>>(RedisService.GetHash("webav", "moveback"));
            }

            var targetModel = models.Where(x => x.s >= limit * 1024 * 1024 * 1024).Skip((page - 1) * pagesize).Take(pagesize).ToList();

            List<MoveBackToLocal> avs = new List<MoveBackToLocal>();

            foreach (var m in targetModel)
            {
                avs.Add(new MoveBackToLocal()
                {
                    AvId = m.n.Split('-')[0] + "-" + m.n.Split('-')[1],
                    AvName = m.n,
                    AvSize = m.s,
                    AvSizeStr = FileSize.GetAutoSizeString(m.s, 1),
                    Fid = m.fid,
                    AvPic = avdb.FirstOrDefault(x => (x.ID + "-" + x.Name).Equals(m.n.Replace("." + m.ico, "").Replace("-C", ""), StringComparison.OrdinalIgnoreCase)) == null ? "" : avdb.FirstOrDefault(x => (x.ID + "-" + x.Name).Equals(m.n.Replace("." + m.ico, "").Replace("-C", ""), StringComparison.OrdinalIgnoreCase)).PictureURL
                });
            }

            ViewData.Add("avs", avs);
            ViewData.Add("size", pagesize);
            ViewData.Add("count", (models.Count / pagesize) + 1);
            ViewData.Add("current", page);

            return View();
        }

        [Base, HttpGet]
        public JsonResult SaveBack(string fid)
        {
            var ret = OneOneFiveService.Copy(fid, "2092826214403000069", OneOneFiveService.Get115Cookie());
            if (ret)
            {
                return Json(new { success = "success" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = "fail" }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}