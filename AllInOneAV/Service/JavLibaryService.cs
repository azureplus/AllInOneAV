﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DataBaseManager.JavDataBaseHelper;
using HtmlAgilityPack;
using Model.Common;
using Model.JavModels;
using Model.ScanModels;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Utils;

namespace Service
{
    public class JavLibraryHelper
    {
        private static LockModel lockModel = new LockModel();

        private static string imgFolder = JavINIClass.IniReadValue("Jav", "imgFolder");
        private static string UserAgent = JavINIClass.IniReadValue("Html", "UserAgent");

        public static CookieContainer GetJavCookie(bool showConsole = true)
        {
            CookieContainer cc = new CookieContainer();
            ChromeOptions options = new ChromeOptions();
            //"test-type", "--ignore-certificate-errors","window-size=1920,1080", "--disable-extensions", "--start-maximized", chromeUA, "--headless"
            var chromeUA = "--useragent=" + string.Format(UserAgent, HtmlManager.GetChromeVersion());
            options.AddArguments("--disable-gpu", "--no-sandbox", "window-size=1,1", "log-level=3", "blink-settings=imagesEnabled=false", "--disable-extensions", "--ignore-certificate-errors");
            List<OpenQA.Selenium.Cookie> ret = new List<OpenQA.Selenium.Cookie>();
            IWebDriver driver = null;

            if (showConsole)
            {
                driver = new ChromeDriver(Environment.CurrentDirectory, options);
            }
            else
            {
                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;

                driver = new ChromeDriver(driverService, options);
            }

            try
            {
                driver.Navigate().GoToUrl("http://www.javlibrary.com/cn/");

                var cf = driver.Manage().Cookies.AllCookies.Where(x => x.Domain == ".javlibrary.com").FirstOrDefault(x => x.Name == "cf_clearance");

                if (cf == null)
                {
                    Thread.Sleep(12 * 1000);
                }

                ret = driver.Manage().Cookies.AllCookies.Where(x => x.Domain == ".javlibrary.com").ToList();
            }
            catch (Exception ee)
            {

            }
            finally
            {
                driver.Quit();
            }

            foreach (var r in ret)
            {
                cc.Add(new System.Net.Cookie(r.Name, r.Value, r.Path, r.Domain));
            }

            if (ret != null && ret.Count >= 3)
            {
                return cc;
            }
            else
            {
                return null;
            }
        }

        public static Dictionary<string, string> GetJavCategory(CookieContainer cc)
        {
            Dictionary<string, string> genreDic = new Dictionary<string, string>();

            int times = 1;
            int maxTimes = 3;

            //最大重试3次
            while (times <= maxTimes && (cc == null || cc.Count < 3))
            {
                cc = GetJavCookie();
                times++;

                if (cc != null && cc.Count >= 3)
                {
                    break;
                }
            }

            if (times < 4 && cc != null)
            {
                //获取分类, 不会过期也不需要多线程
                var htmlRes = HtmlManager.GetHtmlWebClient("http://www.javlibrary.com/cn/", "http://www.javlibrary.com/cn/genres.php", cc, false);

                if (htmlRes.Success)
                {
                    HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                    htmlDocument.LoadHtml(htmlRes.Content);

                    var genrePath = "//div[@class='genreitem']";

                    var genreNodes = htmlDocument.DocumentNode.SelectNodes(genrePath);

                    foreach (var node in genreNodes)
                    {
                        var aTagHref = "http://www.javlibrary.com/cn/" + node.ChildNodes[0].Attributes["href"].Value.Trim();
                        var aTagTitle = node.ChildNodes[0].InnerText.Trim();

                        if (!JavDataBaseManager.HasCategoryByName(aTagTitle))
                        {
                            JavDataBaseManager.InsertCategory(new Category
                            {
                                Name = aTagTitle,
                                Url = aTagHref
                            });
                        }

                        if (!genreDic.ContainsKey(aTagHref))
                        {
                            genreDic.Add(aTagHref, aTagTitle);

                            Console.WriteLine("加入分类 " + aTagHref);
                        }
                    }
                }
            }

            return genreDic;
        }

        public static Dictionary<string, List<string>> FillInCategory(Dictionary<string, string> category, CookieContainer cc)
        {
            Dictionary<string, List<string>> ret = new Dictionary<string, List<string>>();

            int times = 1;
            int maxTimes = 3;

            //最大重试3次
            while (times <= maxTimes && (cc == null || cc.Count < 3))
            {
                cc = GetJavCookie();
                times++;

                if (cc != null && cc.Count >= 3)
                {
                    break;
                }
            }

            if (times < 4 && cc != null)
            {
                Parallel.ForEach(category, new ParallelOptions { MaxDegreeOfParallelism = 20 }, c =>
                {
                    string genreSubUrl = c.Key + "&mode=&page=";
                    List<string> allPages = new List<string>();
                    int lastPage = 1;

                    var genreDetailhtmlRes = JavCookieContanierHelper(cc, c.Key);

                    if (genreDetailhtmlRes.Success)
                    {
                        HtmlAgilityPack.HtmlDocument detailHtmlDocument = new HtmlAgilityPack.HtmlDocument();
                        detailHtmlDocument.LoadHtml(genreDetailhtmlRes.Content);

                        var lastPagePath = "//a[@class='page last']";

                        var lastPageNode = detailHtmlDocument.DocumentNode.SelectSingleNode(lastPagePath);

                        if (lastPageNode != null)
                        {
                            var pageStr = lastPageNode.Attributes["href"].Value.Trim();

                            if (!string.IsNullOrEmpty(pageStr))
                            {
                                pageStr = pageStr.Substring(pageStr.LastIndexOf("=") + 1);

                                int.TryParse(pageStr, out lastPage);
                            }
                        }

                        for (int i = 1; i <= lastPage; i++)
                        {
                            allPages.Add(genreSubUrl + i);
                        }

                        lock (ret)
                        {
                            if (ret.ContainsKey(c.Value))
                            {
                                ret[c.Value].AddRange(allPages);
                            }
                            else
                            {
                                ret.Add(c.Value, allPages);
                            }
                        }

                        Console.WriteLine("线程 " + Thread.CurrentThread.ManagedThreadId.ToString() + " => 添加分类 " + c.Value + " 的所有页码, 一共有 " + lastPage + " 页");
                    }
                });
            }

            return ret;
        }

        public static Dictionary<string, string> GetAllListUrl(Dictionary<string, List<string>> allScan)
        {
            Dictionary<string, string> allListUrl = new Dictionary<string, string>();

            foreach (var scan in allScan)
            {
                foreach (var subScan in scan.Value)
                {
                    if (!allListUrl.ContainsKey(subScan))
                    {
                        allListUrl.Add(subScan, scan.Key);
                    }
                }
            }

            return allListUrl;
        }

        public static void ScanAllUrl(Dictionary<string, string> allListUrl, CookieContainer cc)
        {
            int index = 0;
            Parallel.ForEach(allListUrl, new ParallelOptions { MaxDegreeOfParallelism = 20 }, url =>
            {
                index++;
                ScanCategoryPageUrl(url.Key, url.Value, cc, index, allListUrl.Count);
            });

            //WriteScanFile(allUrl);
        }

        public static void ScanCategoryPageUrl(string url, string cate, CookieContainer cc, int current, int total)
        {
            var htmlRes = JavCookieContanierHelper(cc, url);

            if (htmlRes.Success)
            {
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(htmlRes.Content);

                var videoPath = "//div[@class='video']";

                var videoNodes = htmlDocument.DocumentNode.SelectNodes(videoPath);

                if (videoNodes != null)
                {
                    int unScanCount = 0;

                    foreach (var node in videoNodes)
                    {
                        var urlAndTitle = node.ChildNodes[0];
                        if (urlAndTitle != null && urlAndTitle.ChildNodes.Count >= 3)
                        {
                            var id = urlAndTitle.ChildNodes[0].InnerText.Trim();
                            var name = FileUtility.ReplaceInvalidChar(urlAndTitle.ChildNodes[2].InnerText.Trim());
                            var avUrl = urlAndTitle.Attributes["href"].Value.Trim().Replace("./", "http://www.javlibrary.com/cn/");

                            if (!string.IsNullOrEmpty(avUrl) && !string.IsNullOrEmpty(name) && !string.IsNullOrWhiteSpace(id))
                            {
                                ScanURL scan = new ScanURL
                                {
                                    Category = url,
                                    ID = id,
                                    IsDownload = false,
                                    Title = name,
                                    URL = avUrl
                                };

                                if (!JavDataBaseManager.HasScan(scan))
                                {
                                    unScanCount++;
                                    JavDataBaseManager.InsertScanURL(scan);
                                }
                            }
                        }
                    }

                    Console.WriteLine(cate + " " + url + " 扫描了 " + unScanCount + " 未扫描, 进度" + current + " / " + total);
                }
            }
        }
        
        //按照类型处理每一个列表页
        public static void ScanCategoryPageUrlSingleThread(Dictionary<string, string> urls, CookieContainer cc)
        {
            int index = 1;

            foreach (var url in urls)
            {
                int retry = 1;
                var htmlRes = new Utils.HtmlResponse();

                //如果取不到cookie最多重试5次
                while (retry <= 5)
                {
                    htmlRes = HtmlManager.GetHtmlWebClientWithRenewCC("http://www.javlibrary.com/cn/", url.Key, cc);

                    if (htmlRes.IsExpire)
                    {
                        cc = GetJavCookie();
                        retry++;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                if (htmlRes.Success)
                {
                    HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                    htmlDocument.LoadHtml(htmlRes.Content);

                    var videoPath = "//div[@class='video']";
                    var videoNodes = htmlDocument.DocumentNode.SelectNodes(videoPath);

                    if (videoNodes != null)
                    {
                        int unScanCount = 0;
                        foreach (var node in videoNodes)
                        {
                            var urlAndTitle = node.ChildNodes[0];
                            if (urlAndTitle != null && urlAndTitle.ChildNodes.Count >= 3)
                            {
                                var id = urlAndTitle.ChildNodes[0].InnerText.Trim();
                                var name = FileUtility.ReplaceInvalidChar(urlAndTitle.ChildNodes[2].InnerText.Trim());
                                var avUrl = urlAndTitle.Attributes["href"].Value.Trim().Replace("./", "http://www.javlibrary.com/cn/");

                                if (!string.IsNullOrEmpty(avUrl) && !string.IsNullOrEmpty(name) && !string.IsNullOrWhiteSpace(id))
                                {
                                    ScanURL scan = new ScanURL
                                    {
                                        Category = url.Value,
                                        ID = id,
                                        IsDownload = false,
                                        Title = name,
                                        URL = avUrl
                                    };

                                    if (!JavDataBaseManager.HasScan(scan))
                                    {
                                        unScanCount++;
                                        JavDataBaseManager.InsertScanURL(scan);
                                    }
                                }
                            }
                        }

                        Console.WriteLine(url.Value + " 第 " + index + " / " + urls.Count + " 页, 加入" + unScanCount + " 条未扫描AV");

                        index++;
                    }
                }
                else
                {
                    Console.WriteLine("获取列表页 " + url.Key + " 内容失败");
                }
            }
        }

        public static void ScanAvList(CookieContainer cc)
        {
            List<ScanURL> avs = JavDataBaseManager.GetScanURL().Where(x => x.IsDownload == false).ToList();

            Parallel.ForEach(avs, new ParallelOptions { MaxDegreeOfParallelism = 100 }, av =>
            {
                ScanEachAv(av, cc, string.Format("进度 {0}/{1}", avs.IndexOf(av) + 1, avs.Count));
            });
        }

        public static void ScanEachAv(ScanURL url, CookieContainer cc, string status)
        {
            AV av = new AV();

            if (!JavDataBaseManager.HasAv(url.URL))
            {
                var htmlRes = JavCookieContanierHelper(cc, url.URL);

                if (htmlRes.Success)
                {
                    HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                    htmlDocument.LoadHtml(htmlRes.Content);

                    av = GenerateAVModel(htmlRes.Content, url.URL);

                    JavDataBaseManager.InsertAV(av);
                    Console.WriteLine("线程 " + Thread.CurrentThread.ManagedThreadId.ToString() + " => 插入AV => " + av.ID + " - " + av.Name + " " + status);
                    JavDataBaseManager.UpdateScanURL(url.URL);

                    string result = "";
                    if (!File.Exists(imgFolder + av.ID + av.Name + ".jpg"))
                    {
                        result = DownloadHelper.DownloadHttps(av.PictureURL, imgFolder + av.ID + av.Name + ".jpg", "");

                        if (string.IsNullOrEmpty(result))
                        {
                            Console.WriteLine("线程 " + Thread.CurrentThread.ManagedThreadId.ToString() + " => 下载AV图片成功 => " + av.ID + " - " + av.Name);
                        }
                        else
                        {
                            Console.WriteLine(result);
                        }
                    }
                    else
                    {
                        Console.WriteLine("已存在图片不下载");
                    }
                }
            }
            else
            {
                Console.WriteLine("已存在 => " + url + " " + status);
                JavDataBaseManager.UpdateScanURL(url.URL);
            }
        }

        public static void ScanEachAvSingleThread(CookieContainer cc)
        {
            List<ScanURL> urls = JavDataBaseManager.GetScanURL().Where(x => x.IsDownload == false).ToList();
            int index = 0;

            foreach (var url in urls)
            {
                int retry = 1;
                //二次确认
                if (!JavDataBaseManager.HasAv(url.URL))
                {
                    var htmlRes = new Utils.HtmlResponse();

                    //最多重试5次
                    while (retry <= 5)
                    {
                        htmlRes = HtmlManager.GetHtmlWebClientWithRenewCC("http://www.javlibrary.com/cn/", url.URL, cc);

                        if (htmlRes.IsExpire)
                        {
                            cc = GetJavCookie();
                            retry++;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (htmlRes.Success)
                    {
                        index++;
                        HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                        htmlDocument.LoadHtml(htmlRes.Content);

                        var av = GenerateAVModel(htmlRes.Content, url.URL);

                        JavDataBaseManager.InsertAV(av);
                        Console.WriteLine("线程 " + Thread.CurrentThread.ManagedThreadId.ToString() + " => 插入AV => " + av.ID + " - " + av.Name);
                        JavDataBaseManager.UpdateScanURL(url.URL);

                        string result = "";
                        if (!File.Exists(imgFolder + av.ID + av.Name + ".jpg"))
                        {
                            result = DownloadHelper.DownloadHttps(av.PictureURL, imgFolder + av.ID + av.Name + ".jpg", "");

                            if (string.IsNullOrEmpty(result))
                            {
                                Console.WriteLine("线程 " + Thread.CurrentThread.ManagedThreadId.ToString() + " => 下载AV图片成功 => " + av.ID + " - " + av.Name);
                            }
                            else
                            {
                                Console.WriteLine(result);
                            }
                        }
                        else
                        {
                            Console.WriteLine("已存在图片不下载");
                        }

                        Console.WriteLine("完成" + index + " / " + urls.Count);
                    }
                }
                else
                {
                    JavDataBaseManager.UpdateScanURL(url.URL);
                    Console.WriteLine("详情页已下载 => " + url.URL + " 完成" + index + " / " + urls.Count);
                }
            }
        }

        public static AV GenerateAVModel(string html, string avUrl)
        {
            AV av = new AV();

            HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(html);

            var titlePath = "//h3[@class='post-title text']";
            var picPath = "//img[@id='video_jacket_img']";

            var releasdPath = "//div[@id='video_date']//td[@class='text']";
            var lengthPath = "//div[@id='video_length']//span[@class='text']";

            var dirPath = "//span[@class='director']//a";
            var comPath = "//span[@class='maker']//a";
            var pubPath = "//span[@class='label']//a";

            var catPath = "//span[@class='genre']//a";
            var staPath = "//span[@class='star']//a";


            var titleNode = htmlDocument.DocumentNode.SelectSingleNode(titlePath);
            var title = titleNode.InnerText.Trim();
            var id = title.Substring(0, title.IndexOf(" "));
            title = FileUtility.ReplaceInvalidChar(title.Substring(title.IndexOf(" ") + 1));
            var picUrl = htmlDocument.DocumentNode.SelectSingleNode(picPath);

            av.URL = avUrl;
            av.PictureURL = picUrl.Attributes["src"].Value;

            av.PictureURL = av.PictureURL.StartsWith("http") ? av.PictureURL : "http:" + av.PictureURL;

            av.Name = title;
            av.ID = id;

            var release = htmlDocument.DocumentNode.SelectSingleNode(releasdPath);
            if (release != null && !string.IsNullOrEmpty(release.InnerText))
            {
                av.ReleaseDate = DateTime.Parse(release.InnerText.Trim());
            }

            var length = htmlDocument.DocumentNode.SelectSingleNode(lengthPath);
            if (length != null && !string.IsNullOrEmpty(length.InnerText))
            {
                av.AvLength = int.Parse(length.InnerText.Trim());
            }

            var dirNode = htmlDocument.DocumentNode.SelectNodes(dirPath);
            if (dirNode != null)
            {
                foreach (var dir in dirNode)
                {
                    var name = dir.InnerHtml.Trim();
                    var url = "http://www.javlibrary.com/cn/" + dir.Attributes["href"].Value;

                    Director d = new Director
                    {
                        CreateTime = DateTime.Now,
                        Name = name,
                        URL = url
                    };

                    if (!JavDataBaseManager.HasDirector(d.URL))
                    {
                        JavDataBaseManager.InsertDirector(d);
                    }

                    av.Director += name + ",";
                }
            }

            var comNode = htmlDocument.DocumentNode.SelectNodes(comPath);
            if (comNode != null)
            {
                foreach (var com in comNode)
                {
                    var name = com.InnerHtml.Trim();
                    var url = "http://www.javlibrary.com/cn/" + com.Attributes["href"].Value;

                    Company c = new Company
                    {
                        CreateTime = DateTime.Now,
                        Name = name,
                        URL = url
                    };

                    if (!JavDataBaseManager.HasCompany(c.URL))
                    {
                        JavDataBaseManager.InsertCompany(c);
                    }

                    av.Company += name + ",";
                }
            }

            var pubNode = htmlDocument.DocumentNode.SelectNodes(pubPath);
            if (pubNode != null)
            {
                foreach (var pub in pubNode)
                {
                    var name = pub.InnerHtml.Trim();
                    var url = "http://www.javlibrary.com/cn/" + pub.Attributes["href"].Value;

                    Publisher p = new Publisher
                    {
                        CreateTime = DateTime.Now,
                        Name = name,
                        URL = url
                    };

                    if (!JavDataBaseManager.HasPublisher(p.URL))
                    {
                        JavDataBaseManager.InsertPublisher(p);
                    }

                    av.Publisher += name + ",";
                }
            }

            var catNodes = htmlDocument.DocumentNode.SelectNodes(catPath);
            if (catNodes != null)
            {
                foreach (var cat in catNodes)
                {
                    var name = cat.InnerHtml.Trim();
                    var url = "http://www.javlibrary.com/cn/" + cat.Attributes["href"].Value;

                    av.Category += name + ",";
                }
            }

            var starNodes = htmlDocument.DocumentNode.SelectNodes(staPath);
            if (starNodes != null)
            {
                foreach (var star in starNodes)
                {
                    var name = star.InnerHtml.Trim();
                    var url = "http://www.javlibrary.com/cn/" + star.Attributes["href"].Value;

                    Actress a = new Actress
                    {
                        CreateTime = DateTime.Now,
                        Name = name,
                        URL = url
                    };

                    if (!JavDataBaseManager.HasActress(a.URL))
                    {
                        JavDataBaseManager.InsertActress(a);
                    }

                    av.Actress += name + ",";
                }
            }

            return av;
        }

        public static void WriteScanFile(List<string> allUrl)
        {
            var jsonStr = JsonConvert.SerializeObject(allUrl.Distinct());

            var file = "c:\\setting\\scan" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".json";
            File.Create(file).Close();

            StreamWriter sw = new StreamWriter(file);
            sw.WriteLine(jsonStr);
            sw.Close();
        }

        public static void WriteDownloadFile(List<AV> allAv)
        {
            var jsonStr = JsonConvert.SerializeObject(allAv);

            var file = "c:\\setting\\download" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".json";
            File.Create(file).Close();

            StreamWriter sw = new StreamWriter(file);
            sw.WriteLine(jsonStr);
            sw.Close();
        }

        public static Utils.HtmlResponse JavCookieContanierHelper(CookieContainer cc, string url)
        {
            var htmlRes = new Utils.HtmlResponse();

            while (true)
            {
                htmlRes = HtmlManager.GetHtmlWebClientWithRenewCC("http://www.javlibrary.com/cn/", url, cc);

                if (htmlRes.IsExpire)
                {
                    lockModel.CanRun = false;

                    lock (lockModel)
                    {
                        if (lockModel.CanRun == false)
                        {
                            cc = GetJavCookie();
                        }
                    }
                }
                else
                {
                    if (lockModel.CanRun == false)
                    {
                        lock (lockModel)
                        {
                            lockModel.CanRun = true;
                        }
                    }
                    break;
                }
            }

            return htmlRes;
        }

        public static void DoFullScan(bool showConsole = true)
        {
            //获取Cookie
            var cc = GetJavCookie(showConsole);
            //获取分类
            var genres = GetJavCategory(cc);
            //获取所谓分类下面的所有页数,以便达到全站扫描
            var allScan = FillInCategory(genres, cc);
            //重新组装所有列表页的去重url
            var allListUrl = GetAllListUrl(allScan);
            //对所有列表页尽享扫描
            ScanAllUrl(allListUrl, cc);
            //处理每一个单独av
            ScanAvList(cc);
        }

        public static void DoDailyUpdate(bool showConsole = true)
        {
            Dictionary<string, string> updatePages = new Dictionary<string, string>();

            var cc = GetJavCookie(showConsole);

            for (int i = 1; i <= 200; i++)
            {
                updatePages.Add("http://www.javlibrary.com/cn/vl_update.php?&mode=&page=" + i, "更新");
            }

            int index = 0;
            Parallel.ForEach(updatePages, new ParallelOptions { MaxDegreeOfParallelism = 100 }, url =>
            {
                index++;
                ScanCategoryPageUrl(url.Key, url.Value, cc, index, 200);
            });

            ScanAvList(cc);
        }

        public static void DoCertainCategory(Dictionary<string, string> dic, bool showConsole = true)
        {
            var cc = GetJavCookie(showConsole);
            var allScan = FillInCategory(dic, cc);
            var allListUrl = GetAllListUrl(allScan);
            ScanAllUrl(allListUrl, cc);
            ScanAvList(cc);
        }

        public static void DoFullScanSingleThread(bool showConsole = true)
        {
            //获取Cookie
            var cc = GetJavCookie(showConsole);
            //获取分类
            var genres = GetJavCategory(cc);
            //获取所谓分类下面的所有页数,以便达到全站扫描
            var allScan = FillInCategory(genres, cc);
            //重新组装所有列表页的去重url
            var allListUrl = GetAllListUrl(allScan);
            //完成全列表页扫描,并且写入扫描记录
            ScanAllUrl(allListUrl, cc);
            //下载所有未完成的下载
            ScanEachAvSingleThread(cc);
        }
    }

    public class BtsowClubHelper
    {
        private static string UserAgent = JavINIClass.IniReadValue("Html", "UserAgent");

        public static CookieContainer GetBtsowCookie()
        {
            CookieContainer cc = new CookieContainer();
            ChromeOptions options = new ChromeOptions();
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            //"test-type", "--ignore-certificate-errors","window-size=1920,1080", "--disable-extensions", "--start-maximized", chromeUA, "--headless"
            var chromeUA = "--useragent=" + string.Format(UserAgent, HtmlManager.GetChromeVersion());
            options.AddArguments("--disable-gpu", "--no-sandbox", "window-size=1,1", "log-level=3", "blink-settings=imagesEnabled=false", "--disable-extensions", "--ignore-certificate-errors", "--headless");
            List<OpenQA.Selenium.Cookie> ret = new List<OpenQA.Selenium.Cookie>();
            IWebDriver driver = new ChromeDriver(driverService, options);

            try
            {
                driver.Navigate().GoToUrl("https://btsow.club/tags");
  
                ret = driver.Manage().Cookies.AllCookies.Where(x => x.Domain == "btsow.club").ToList();
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.ToString());
            }
            finally
            {
                driver.Quit();
            }

            foreach (var r in ret)
            {
                cc.Add(new System.Net.Cookie(r.Name, r.Value, r.Path, r.Domain));
            }

            return cc;
        }

        public static List<SeedMagnetSearchModel> SerachListView(string content, CookieContainer cc)
        {
            List<SeedMagnetSearchModel> ret = new List<SeedMagnetSearchModel>();

            try
            {
                var serachContent = "https://btsow.club/search/" + content;
                var htmlRet = HtmlManager.GetHtmlWebClient("https://btsow.club", serachContent, null, true);

                if (htmlRet.Success)
                {
                    HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                    htmlDocument.LoadHtml(htmlRet.Content);

                    string xpath = "//div[@class='row']";

                    HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes(xpath);

                    foreach (var node in nodes.Take(nodes.Count - 1))
                    {
                        var text = node.ChildNodes[1].ChildNodes[1].InnerText.Trim();
                        var size = FileUtility.GetFileSizeFromString(node.ChildNodes[3].InnerText.Trim());
                        var date = node.ChildNodes[5].InnerText.Trim();
                        var a = node.ChildNodes[1].OuterHtml;
                        var url = a.Substring(a.IndexOf("\"") + 1);
                        url = url.Substring(0, url.IndexOf("\""));

                        SeedMagnetSearchModel temp = new SeedMagnetSearchModel
                        {
                            Title = text,
                            Size = size,
                            Date = DateTime.Parse(date),
                            Url = url,
                            Source = SearchSeedSiteEnum.Btsow
                        };

                        ret.Add(temp);
                    }

                    foreach (var r in ret)
                    {
                        var subHtmlRet = HtmlManager.GetHtmlWebClient("https://btsow.club", r.Url, cc, false);

                        if (subHtmlRet.Success)
                        {
                            htmlDocument = new HtmlAgilityPack.HtmlDocument();
                            htmlDocument.LoadHtml(subHtmlRet.Content);

                            xpath = "//textarea[@class='magnet-link hidden-xs']";

                            HtmlNode node = htmlDocument.DocumentNode.SelectSingleNode(xpath);

                            if (node != null)
                            {
                                r.MagUrl = node.InnerText;
                            }
                        }
                    }
                }
            }
            catch (Exception ee)
            {

            }

            return ret;
        }

        public static List<SeedMagnetSearchModel> SearchBtsow(string content)
        {
            List<SeedMagnetSearchModel> ret = new List<SeedMagnetSearchModel>();

            var cc = GetBtsowCookie();

            if (cc != null && cc.Count > 0)
            {
                ret = SerachListView(content, cc);
            }

            return ret;
        }
    }
}
