using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using HtmlAgilityPack;
using Model.Common;
using Model.JavModels;
using Model.ScanModels;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Utils;
using Publisher = Model.JavModels.Publisher;

namespace Service
{
    public class JavLibraryHelper
    {
        private static CookieContainer cc = null;
        private static readonly string ImgFolder = JavINIClass.IniReadValue("Jav", "imgFolder");
        private static readonly string Mode = JavINIClass.IniReadValue("Jav", "cookieMode");
        private static readonly string UserAgent = JavINIClass.IniReadValue("Html", "UserAgent");

        public static void GetJavCookie(bool showConsole = true)
        {
            cc = new CookieContainer();

            if (Mode == "None")
            {

            }
            else if (Mode == "Process")
            {
                GetJavCookieChromeProcess();
            }
            else if (Mode == "Chrome")
            {
                GetJavCookieChromeDrive(false);
            }
        }

        public static void GetJavCookieChromeProcess()
        {
            Broswer.OpenBrowserUrl("http://www.javlibrary.com/cn/");
            Thread.Sleep(8 * 1000);
            Broswer.Refresh_click();
            Thread.Sleep(15 * 1000);
            Broswer.Refresh_click();
            Thread.Sleep(5 * 1000);

            var cookies = new ChromeCookieReader().ReadChromeCookies(".javlibrary.com");

            foreach (var c in cookies)
            {
                System.Net.Cookie coo = new System.Net.Cookie();

                if (c.Name.Contains("cf"))
                {
                    coo.Name = c.Name;
                    coo.Value = c.Value;
                    coo.Domain = "www.javlibrary.com";

                    cc.Add(coo);
                }
            }
        }

        public static void GetJavCookieChromeDrive(bool showConsole = true)
        {
            ChromeOptions options = new ChromeOptions();
            //"test-type", "--ignore-certificate-errors","window-size=1920,1080", "--disable-extensions", "--start-maximized", chromeUA, "--headless"
            var chromeUA = "--useragent=" + string.Format(UserAgent, HtmlManager.GetChromeVersion());
            options.AddArguments("--disable-gpu", "--no-sandbox", "window-size=1,1", "log-level=3", "blink-settings=imagesEnabled=false", "--disable-extensions", "--ignore-certificate-errors");
            List<OpenQA.Selenium.Cookie> ret = new List<OpenQA.Selenium.Cookie>();
            IWebDriver driver = null;

            if (showConsole)
            {
                driver = new ChromeDriver("c:\\setting\\", options);
            }
            else
            {
                var driverService = ChromeDriverService.CreateDefaultService("c:\\setting\\");
                driverService.HideCommandPromptWindow = true;

                driver = new ChromeDriver(driverService, options);
            }

            try
            {
                driver.Navigate().GoToUrl("http://www.javlibrary.com/cn/");

                Thread.Sleep(15 * 1000);

                ret = driver.Manage().Cookies.AllCookies.Where(x => x.Domain == ".javlibrary.com").ToList();

                Console.WriteLine("((((((((((((((((((((((((((更新Cookie)))))))))))))))))))))))))))");
            }
            catch (Exception ee)
            {

            }
            finally
            {
                driver.Quit();
            }


            cc = new CookieContainer();

            foreach (var r in ret)
            {
                cc.Add(new System.Net.Cookie(r.Name, r.Value, r.Path, r.Domain));
            }
        }

        private static Dictionary<string, string> GetJavCategory()
        {
            Dictionary<string, string> genreDic = new Dictionary<string, string>();

            int times = 1;
            int maxTimes = 3;

            //最大重试3次
            while (times <= maxTimes && (cc == null || cc.Count < 3))
            {
                GetJavCookie();
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

        private static Dictionary<string, List<string>> FillInCategory(Dictionary<string, string> category)
        {
            Dictionary<string, List<string>> ret = new Dictionary<string, List<string>>();

            int times = 1;
            int maxTimes = 3;

            GetJavCookie();

            if (times < 4 && cc != null)
            {
                Parallel.ForEach(category, new ParallelOptions { MaxDegreeOfParallelism = 20 }, c =>
                {
                    string genreSubUrl = c.Key + "&mode=&page=";
                    List<string> allPages = new List<string>();
                    int lastPage = 1;

                    var genreDetailhtmlRes = JavCookieContanierHelper(c.Key);

                    if (genreDetailhtmlRes.Success)
                    {
                        HtmlDocument detailHtmlDocument = new HtmlDocument();
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

        private static List<string> FillInCertainUrl(List<string> list, bool asc = true, int limit = -1)
        {
            List<string> ret = new List<string>();

            Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 10 }, url =>
            {
                string genreSubUrl = url + "&page=";
                List<string> allPages = new List<string>();
                int lastPage = 1;

                var genreDetailhtmlRes = JavCookieContanierHelper(url);

                if (genreDetailhtmlRes.Success)
                {
                    HtmlDocument detailHtmlDocument = new HtmlDocument();
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

                    if (asc == false)
                    {
                        allPages.Reverse();
                    }

                    if (limit > 0)
                    {
                        allPages = allPages.Take(limit).ToList();
                    }

                    ret.AddRange(allPages);
                }
            });

            return ret;
        }

        private static Dictionary<string, string> GetAllListUrl(Dictionary<string, List<string>> allScan)
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

        private static void ScanAllUrl(Dictionary<string, string> allListUrl)
        {
            int index = 0;
            Parallel.ForEach(allListUrl, new ParallelOptions { MaxDegreeOfParallelism = 5 }, url =>
            {
                index++;
                ScanCategoryPageUrl(url.Key, url.Value, index, allListUrl.Count);
            });

            //WriteScanFile(allUrl);
        }

        private static void ScanCategoryPageUrl(string url, string cate, int current, int total, List<string> scans = null)
        {
            var htmlRes = JavCookieContanierHelper(url);

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

                                    if (scans != null)
                                    {
                                        scans.Add(avUrl);
                                    }
                                }
                            }
                        }
                    }

                    Console.WriteLine(cate + " " + url + " 扫描了 " + unScanCount + " 未扫描, 进度" + current + " / " + total);
                }
            }
        }

        private static List<AV> ScanAndReturnAv(string url)
        {
            List<AV> ret = new List<AV>();
            var htmlRes = JavCookieContanierHelper(url);

            if (htmlRes.Success)
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(htmlRes.Content);

                var videoPath = "//div[@class='video']";

                var videoNodes = htmlDocument.DocumentNode.SelectNodes(videoPath);

                if (videoNodes != null)
                {
                    foreach (var node in videoNodes)
                    {
                        var urlAndTitle = node.ChildNodes[0];
                        if (urlAndTitle != null && urlAndTitle.ChildNodes.Count >= 3)
                        {
                            var id = urlAndTitle.ChildNodes[0].InnerText.Trim();
                            var name = FileUtility.ReplaceInvalidChar(urlAndTitle.ChildNodes[2].InnerText.Trim());
                            var pic = urlAndTitle.ChildNodes[1].Attributes["src"].Value;

                            if (!pic.StartsWith("http"))
                            {
                                pic = "http:" + pic;
                            }

                            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(pic) && !string.IsNullOrEmpty(name))
                            {
                                

                                Console.WriteLine("AV:" + JsonConvert.SerializeObject(new RefreshModel
                                    {
                                        Id = id,
                                        Name = name,
                                        Url = pic
                                    })
                                );
                            }
                        }
                    }
                }
            }

            return ret;
        }

        private static void ScanCategoryPageUrlSingleThread(Dictionary<string, string> urls)
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
                        GetJavCookie();
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

        private static void ScanAvList()
        {
            List<ScanURL> avs = JavDataBaseManager.GetScanURL().Where(x => x.IsDownload == false).ToList();

            Parallel.ForEach(avs, new ParallelOptions { MaxDegreeOfParallelism = 100 }, av =>
            {
                ScanEachAv(av, string.Format("进度 {0}/{1}", avs.IndexOf(av) + 1, avs.Count));
            });
        }

        private static void ScanAvList(List<ScanURL> urls, bool force = false)
        {
            List<ScanURL> avs = urls;

            Parallel.ForEach(avs, new ParallelOptions { MaxDegreeOfParallelism = 100 }, av =>
            {
                ScanEachAv(av, string.Format("进度 {0}/{1}", avs.IndexOf(av) + 1, avs.Count), force);
            });
        }

        private static void ScanDailyAvList(List<string> urls)
        {
            List<string> ret = new List<string>();

            Parallel.ForEach(urls, new ParallelOptions { MaxDegreeOfParallelism = 100 }, url =>
            {
                ScanEachAv(new ScanURL { URL = url }, string.Format("进度 {0}/{1}", urls.IndexOf(url) + 1, urls.Count));
            });
        }

        private static void ScanEachAv(ScanURL url, string status, bool force = false)
        {
            AV av = new AV();

            if (!JavDataBaseManager.HasAv(url.URL) || force)
            {
                var htmlRes = JavCookieContanierHelper(url.URL);

                if (htmlRes.Success)
                {
                    HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                    htmlDocument.LoadHtml(htmlRes.Content);

                    av = GenerateAVModel(htmlRes.Content, url.URL);

                    if (!force)
                    {
                        JavDataBaseManager.InsertAV(av);

                        Console.WriteLine("线程 " + Thread.CurrentThread.ManagedThreadId.ToString() + " => 插入AV => " + av.ID + " - " + av.Name + " " + status);
                        JavDataBaseManager.UpdateScanURL(url.URL);

                        Console.WriteLine("AV:" + JsonConvert.SerializeObject(new RefreshModel
                            {
                                Id = av.ID,
                                Name = av.Name,
                                Url = av.PictureURL
                            })
                        );
                    }

                    string result = "";
                    if (!File.Exists(ImgFolder + av.ID + av.Name + ".jpg"))
                    {
                        result = DownloadHelper.DownloadHttps(av.PictureURL, ImgFolder + av.ID + av.Name + ".jpg", "");

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
                Console.WriteLine("已存在 => " + url.URL + " " + status);
                JavDataBaseManager.UpdateScanURL(url.URL);
            }
        }

        private static void ScanEachAvSingleThread()
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
                            GetJavCookie();
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
                        if (!File.Exists(ImgFolder + av.ID + av.Name + ".jpg"))
                        {
                            result = DownloadHelper.DownloadHttps(av.PictureURL, ImgFolder + av.ID + av.Name + ".jpg", "");

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

        private static AV GenerateAVModel(string html, string avUrl)
        {
            AV av = new AV();

            HtmlDocument htmlDocument = new HtmlDocument();
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
            DateTime rDate = new DateTime(2050, 1, 1);
            if (release != null && !string.IsNullOrEmpty(release.InnerText))
            {
                DateTime.TryParse(release.InnerText.Trim(), out rDate);

                if (rDate <= DateTime.MinValue)
                {
                    rDate = new DateTime(2050, 1, 1);
                }
            }
            
            av.ReleaseDate = rDate;

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

        private static Utils.HtmlResponse JavCookieContanierHelper(string url)
        {
            var htmlRes = HtmlManager.GetHtmlWebClientWithRenewCC("http://www.javlibrary.com/cn/", url, cc);

            return htmlRes;
        }

        private static void RefreshCookie(int mintutes)
        {
            while (true)
            {
                Thread.Sleep(1000 * 60 * (mintutes - 5));

                GetJavCookie(false);

                Console.WriteLine("*********************更新Cookie*********************");
            }
        }

        private static List<MissingCheckModel> DoAllRelatedJav(string table, string content)
        {
            List<MissingCheckModel> ret = new List<MissingCheckModel>();
            var avs = JavDataBaseManager.GetAllAV();

            switch (table)
            {
                case "category":
                    avs = avs.Where(x => x.Category.Contains(content)).ToList();
                    break;
                case "actress":
                    avs = avs.Where(x => x.Actress.Contains(content)).ToList();
                    break;
                case "prefix":
                    avs = avs.Where(x => x.ID.StartsWith(content.ToUpper() + "-")).ToList();
                    break;
            }

            foreach (var av in avs)
            {
                MissingCheckModel mcm = new MissingCheckModel
                {
                    Av = av,
                    Fi = new List<FileInfo>(),
                    IsMatch = false,
                    Seeds = new List<SeedMagnetSearchModel>()
                };

                ret.Add(mcm);
            }

            return ret.OrderBy(x => x.Av.ID).ToList();
        }

        private static void FaviScan(List<string> urls)
        {
            ScanDataBaseManager.DeleteFaviScan();

            Parallel.ForEach(urls, new ParallelOptions { MaxDegreeOfParallelism = 10 }, url =>
            {
                var htmlRes = JavCookieContanierHelper(url);

                if (htmlRes.Success)
                {
                    HtmlDocument htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(htmlRes.Content);

                    var titlePath = "//div[@class='boxtitle']";

                    var titleNode = htmlDocument.DocumentNode.SelectSingleNode(titlePath);

                    if (titleNode != null)
                    {
                        var textArray = titleNode.InnerHtml.Trim().Split(' ');

                        var category = "";
                        var name = textArray[0].Replace("&quot;", "");

                        if (textArray[1] == "所执导的影片")
                        {
                            category = "director";
                        }
                        else if (textArray[1] == "识别码搜寻结果")
                        {
                            category = "prfix";
                        }
                        else if (textArray[1] == "相关的影片")
                        {
                            category = "category";
                        }
                        else
                        {
                            category = "actress";
                        }

                        FaviScan fs = new FaviScan
                        {
                            Url = url,
                            Name = name,
                            Category = category
                        };

                        Console.WriteLine("扫描 " + url + " 分类 " + category + " 名称 " + name);
                        ScanDataBaseManager.InsertFaviScan(fs);
                    }
                }
            });
        }

        public static void DoDownloadOnly(bool showConsole = true)
        {
            GetJavCookie(showConsole);

            Task.Run(() => RefreshCookie(60));

            ScanAvList();
        }

        public static void DoFullScan(bool showConsole = true)
        {
            //获取Cookie
            GetJavCookie(showConsole);

            Task.Run(() => RefreshCookie(60));

            //获取分类
            var genres = GetJavCategory();
            //获取所谓分类下面的所有页数,以便达到全站扫描
            var allScan = FillInCategory(genres);
            //重新组装所有列表页的去重url
            var allListUrl = GetAllListUrl(allScan);
            //对所有列表页尽享扫描
            ScanAllUrl(allListUrl);
            //处理每一个单独av
            ScanAvList();
        }

        public static void DoDailyUpdate(int page = 200, bool showConsole = true)
        {
            Dictionary<string, string> updatePages = new Dictionary<string, string>();
            List<string> urls = new List<string>();

            GetJavCookie(showConsole);

            var htmlRes = HtmlManager.GetHtmlWebClientWithRenewCC("http://www.javlibrary.com/cn/", "http://www.javlibrary.com/cn/vl_update.php?&mode=&page=1", cc);

            for (int i = 1; i <= page; i++)
            {
                updatePages.Add("http://www.javlibrary.com/cn/vl_update.php?&mode=&page=" + i, "更新");
            }

            int index = 0;
            Parallel.ForEach(updatePages, new ParallelOptions { MaxDegreeOfParallelism = 100 }, url =>
            {
                index++;
                ScanCategoryPageUrl(url.Key, url.Value, index, page, urls);
            });

            ScanDailyAvList(urls);
        }

        public static void DoDailyRefresh(int page = 200, bool showConsole = true)
        {
            Dictionary<string, string> updatePages = new Dictionary<string, string>();
            List<string> urls = new List<string>();

            GetJavCookie(showConsole);

            for (int i = page; i >= 1; i--)
            {
                updatePages.Add("http://www.javlibrary.com/cn/vl_update.php?&mode=&page=" + i, "更新");
            }

            int index = 0;
            Parallel.ForEach(updatePages, new ParallelOptions { MaxDegreeOfParallelism = 100 }, url =>
            {
                index++;
                ScanAndReturnAv(url.Key);
            });
        }

        public static void DoCertainCategory(Dictionary<string, string> dic, bool showConsole = true)
        {
            GetJavCookie(showConsole);
            var allScan = FillInCategory(dic);
            var allListUrl = GetAllListUrl(allScan);
            ScanAllUrl(allListUrl);
            ScanAvList();
        }

        public static void DoCombineScan(List<string> startingPages, bool isAsc, int pageSize, bool showConsole = true)
        {
            GetJavCookie(showConsole);
        }

        public static void DoFullScanSingleThread(bool showConsole = true)
        {
            //获取Cookie
            GetJavCookie(showConsole);
            //获取分类
            var genres = GetJavCategory();
            //获取所谓分类下面的所有页数,以便达到全站扫描
            var allScan = FillInCategory(genres);
            //重新组装所有列表页的去重url
            var allListUrl = GetAllListUrl(allScan);
            //完成全列表页扫描,并且写入扫描记录
            ScanCategoryPageUrlSingleThread(allListUrl);
            //下载所有未完成的下载
            ScanEachAvSingleThread();
        }

        public static void DoListSearch(List<string> urls, bool asc = true, int limit = 0, bool showConsole = true)
        {
            GetJavCookie(showConsole);

            var allList = FillInCertainUrl(urls, asc, limit);

            Parallel.ForEach(allList, new ParallelOptions { MaxDegreeOfParallelism = 12 }, url =>
            {
                ScanAndReturnAv(url);
            });

            Thread.Sleep(3 * 1000);
        }

        public static void DoFaviScan(List<string> urls, bool showConsole = true)
        {
            GetJavCookie(showConsole);

            FaviScan(urls);
        }

        public async static Task<List<MissingCheckModel>> GetAllRelatedJav(string table, string content)
        {
            return await Task.Run(() => DoAllRelatedJav(table, content));
        }

        public static AV GetAvFromUrl(string url)
        {
            AV ret = new AV();

            GetJavCookie(false);

            var htmlRes = JavCookieContanierHelper(url);

            if (htmlRes.Success)
            {
                ret = GenerateAVModel(htmlRes.Content, url);
            }

            return ret;
        }
    }

    public class BtsowClubHelper
    {
        private static readonly string UserAgent = JavINIClass.IniReadValue("Html", "UserAgent");

        public static CookieContainer GetBtsowCookie()
        {
            CookieContainer cc = new CookieContainer();
            ChromeOptions options = new ChromeOptions();
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            //"test-type", "--ignore-certificate-errors","window-size=1920,1080", "--disable-extensions", "--start-maximized", chromeUA, "--headless"
            var chromeUA = "--useragent=" + string.Format(UserAgent, HtmlManager.GetChromeVersion());
            options.AddArguments("--disable-gpu", "--no-sandbox", "log-level=3", "blink-settings=imagesEnabled=false", "--disable-extensions", "--ignore-certificate-errors");
            List<OpenQA.Selenium.Cookie> ret = new List<OpenQA.Selenium.Cookie>();
            IWebDriver driver = new ChromeDriver(Environment.CurrentDirectory, options);

            try
            {
                driver.Navigate().GoToUrl("https://btsow.club/tags");

                Thread.Sleep(3 * 1000);

                driver.Navigate().Refresh();

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
