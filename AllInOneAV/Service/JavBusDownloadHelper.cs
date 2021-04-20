using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using HtmlAgilityPack;
using Model.JavModels;
using Model.ScanModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utils;

namespace Service
{
    public class JavBusDownloadHelper
    {
        private static string MappingFile = JavINIClass.IniReadValue("Jav", "map");

        public static CookieContainer GetJavBusCookie()
        {
            var index = "https://www.javbus.com";
            var result = HtmlManager.GetCookies(index);
            CookieContainer cc = new CookieContainer();
            cc.Add(result);

            return cc;
        }

        public static void UpdateJavBusCategory(CookieContainer cc)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            List<string> busCategory = new List<string>();
            var url = "https://www.javbus.com/genre";

            var mapping = JavBusDownloadHelper.GetJavCategoryMapping(MappingFile);
            List<string> javLibrary = GetJavLibraryCategory();

            var content = HtmlManager.GetHtmlContentViaUrl(url, "utf-8", false, cc);

            if (content.Success)
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(content.Content);

                string xpath = "//a[@class='col-lg-2 col-md-2 col-sm-3 col-xs-6 text-center']";

                HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes(xpath);

                foreach (var node in nodes)
                {
                    busCategory.Add(FileUtility.GetJianTiStr(node.InnerText.Trim()));
                }
            }

            var copyBus = string.Join("\r\n", busCategory);
            var copyLib = string.Join("\r\n", javLibrary);

            for (int i = 0; i < busCategory.Count; i++)
            {
                if (javLibrary.Contains(busCategory[i]))
                {
                    if (!ret.ContainsKey(busCategory[i]))
                    {
                        ret.Add(busCategory[i], javLibrary.FirstOrDefault(x => x == busCategory[i]));
                    }
                }
                else
                {
                    if (mapping.ContainsKey(busCategory[i]))
                    {
                        ret.Add(busCategory[i], mapping[busCategory[i]]);
                    }
                }
            }

            if (ret.Count > 0)
            {
                JavDataBaseManager.DeleteJavBusCategory();

                foreach (var map in ret)
                {
                    JavDataBaseManager.InsertJavBusCategory(map.Key, map.Value);
                }
            }
        }

        public static Dictionary<string, string> GetJavCategoryMapping(string file)
        {
            return FileUtility.GetJavBusToJavLibraryCategoryMapping(file);
        }

        public static List<string> GetJavLibraryCategory()
        {
            return JavDataBaseManager.GetCategories().Select(x => x.Name).ToList();
        }

        public static Dictionary<string, bool> CategoryNotMatch(List<string> javBus, List<string> javLibrary)
        {
            Dictionary<string, bool> ret = new Dictionary<string, bool>();

            foreach (var cate in javBus)
            {
                if (!javLibrary.Contains(cate))
                {
                    if (!ret.ContainsKey(cate))
                    {
                        ret.Add(cate, false);
                    }
                }
                else
                {
                    if (!ret.ContainsKey(cate))
                    {
                        ret.Add(cate, true);
                    }
                }
            }

            return ret;
        }

        public static List<JavBusSearchListModel> GetJavBusSearchListModel(string key, CookieContainer cc)
        {
            List<JavBusSearchListModel> ret = new List<JavBusSearchListModel>();

            var searchUrl = string.Format(@"https://www.javbus.com/search/{0}&type=&parent=ce", key);

            var listHtml = HtmlManager.GetHtmlContentViaUrl(searchUrl, "utf-8", false, cc);

            if (listHtml.Success)
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(listHtml.Content);

                var listXpath = "//div[@class='item']";
                HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes(listXpath);

                foreach (var item in nodes)
                {
                    var regUrl = "<a class=\"movie-box\" href=\"(.*?)\">";
                    var regImg = "<img src=\"(.*?)\" title=\"(.*?)\">";
                    var content = item.InnerHtml;

                    var mImg = Regex.Match(content, regImg);
                    var mUrl = Regex.Match(content, regUrl);

                    var img = mImg.Groups[1].Value;
                    var title = mImg.Groups[2].Value;
                    var url = mUrl.Groups[1].Value;

                    ret.Add(new JavBusSearchListModel
                    {
                        Img = img,
                        Title = title,
                        Url = url
                    });
                }
            }

            return ret;
        }

        public static AV GetJavBusSearchDetail(string url, CookieContainer cc, Dictionary<string, string> mapping)
        {
            AV av = new AV();

            var listHtml = HtmlManager.GetHtmlContentViaUrl(url, "utf-8", false, cc);

            if (listHtml.Success)
            {
                var titleTemplate = "<h3>(.*?)</h3>";
                var imgTemplate = "<a class=\"bigImage\" href=\"(.*?)\">";
                var idTemplate = "<span style=\"color:#CC0000;\">(.*?)</span>";
                var dateTemplate = "<p><span class=\"header\">發行日期:</span>(.*?)</p>";
                var directorTemplate = "<p><span class=\"header\">導演:</span> <a href=\"(.*?)\">(.*?)</a></p>";
                var lengthTemplate = "<p><span class=\"header\">長度:</span>(.*?)分鐘</p>";
                var actressTemplate = "<div class=\"star-name\"><a href=\"(.*?)\" title=\"(.*?)\">(.*?)</a></div>";
                var companyTemplate = "<p><span class=\"header\">製作商:</span> <a href=\"(.*?)\">(.*?)</a>";
                var publisherTemplate = "<p><span class=\"header\">發行商:</span> <a href=\"(.*?)\">(.*?)</a>";
                var categotyTemplate = "<span class=\"genre\"><a href=\"(.*?)\">(.*?)</a></span>";

                var mTitle = Regex.Match(listHtml.Content, titleTemplate);
                var mId = Regex.Match(listHtml.Content, idTemplate);
                var mImg = Regex.Match(listHtml.Content, imgTemplate);
                var mDate = Regex.Match(listHtml.Content, dateTemplate);
                var mLength = Regex.Match(listHtml.Content, lengthTemplate);

                var mDirector = Regex.Matches(listHtml.Content, directorTemplate);
                var mActress = Regex.Matches(listHtml.Content, actressTemplate);
                var mCompany = Regex.Matches(listHtml.Content, companyTemplate);
                var mPublisher = Regex.Matches(listHtml.Content, publisherTemplate);
                var mCategory = Regex.Matches(listHtml.Content, categotyTemplate);

                var id = mId.Groups[1];
                var title = mTitle.Groups[1].ToString().Replace(id.ToString(), "").Trim();
                var img = mImg.Groups[1];
                var date = mDate.Groups[1];
                var length = mLength.Groups[1];

                var director = "";
                var actress = "";
                var company = "";
                var publisher = "";
                var category = "";

                foreach (System.Text.RegularExpressions.Match d in mDirector)
                {
                    director += d.Groups[2] + ",";
                }

                foreach (System.Text.RegularExpressions.Match d in mActress)
                {
                    var act = d.Groups[3].ToString();

                    actress += act + ",";
                }

                foreach (System.Text.RegularExpressions.Match d in mCompany)
                {
                    company += d.Groups[2] + ",";
                }

                foreach (System.Text.RegularExpressions.Match d in mPublisher)
                {
                    publisher += d.Groups[2] + ",";
                }

                foreach (System.Text.RegularExpressions.Match d in mCategory)
                {
                    category += d.Groups[2] + ",";
                }

                DateTime parse = new DateTime(2050, 1, 1);
                av.Name = title;
                av.ID = id.ToString();
                av.PictureURL = img.ToString();
                av.Publisher = publisher;
                DateTime.TryParse(date.ToString(), out parse);
                av.ReleaseDate = parse;
                av.Director = director;
                av.Actress = actress;
                av.Company = company;
                av.Category = category;
                av.AvLength = int.Parse(length.ToString());

                return av;
            }

            return null;
        }

        public static BusLibMatchResultModel GetMatchJavDetail(AV javBusRecord)
        {
            BusLibMatchResultModel ret = new BusLibMatchResultModel
            {
                Matches = new List<AV>(),
                IsMatch = false
            };

            javBusRecord.Name = FileUtility.ReplaceInvalidChar(javBusRecord.Name);
            ret.Matches.Add(javBusRecord);
            ret.IsMatch = false;

            return ret;
        }

        public static AV GetCloseLibAVModel(AV javBusRecord, Dictionary<string, string> mapping)
        {
            StringBuilder actSb = new StringBuilder();
            StringBuilder dirSb = new StringBuilder();
            StringBuilder comSb = new StringBuilder();
            StringBuilder pubSb = new StringBuilder();
            StringBuilder catSb = new StringBuilder();

            var actArray = javBusRecord.Actress.Split(',');
            var dirArray = javBusRecord.Director.Split(',');
            var comArray = javBusRecord.Company.Split(',');
            var pubArray = javBusRecord.Publisher.Split(',');
            var catArray = javBusRecord.Category.Split(',');

            foreach (var act in actArray)
            {
                if (!string.IsNullOrEmpty(act))
                {
                    if (act.Contains("（"))
                    {
                        var temp = act.Replace("（", "、").Replace("）", "");
                        var tempArr = temp.Split('、');
                        bool find = false;

                        foreach (var split in tempArr)
                        {
                            if (JavDataBaseManager.HasActressByName(split))
                            {
                                actSb.Append(split + ",");
                                find = true;
                                break;
                            }
                        }

                        if (!find)
                        {
                            actSb.Append("[" + temp + "]");
                        }
                    }
                    else
                    {
                        if (JavDataBaseManager.HasActressByName(act))
                        {
                            actSb.Append(act + ",");
                        }
                        else
                        {
                            actSb.Append("[" + act + "]");
                        }
                    }
                }
            }

            foreach (var dir in dirArray)
            {
                if (!string.IsNullOrEmpty(dir))
                {
                    if (JavDataBaseManager.HasDirectorByName(dir))
                    {
                        dirSb.Append(dir + ",");
                    }
                    else
                    {
                        dirSb.Append("[" + dir + "],");
                    }
                }
            }

            foreach (var com in comArray)
            {
                if (!string.IsNullOrEmpty(com))
                {
                    if (JavDataBaseManager.HasCompanyByName(com))
                    {
                        comSb.Append(com + ",");
                    }
                    else
                    {
                        comSb.Append("[" + com + "],");
                    }
                }
            }

            foreach (var pub in pubArray)
            {
                if (!string.IsNullOrEmpty(pub))
                {
                    if (!string.IsNullOrEmpty(pub))
                    {
                        if (JavDataBaseManager.HasCompanyByName(pub))
                        {
                            pubSb.Append(pub + ",");
                        }
                        else
                        {
                            pubSb.Append("[" + pub + "],");
                        }
                    }
                }
            }

            foreach (var cat in catArray)
            {
                if (!string.IsNullOrEmpty(cat))
                {
                    var jianti = FileUtility.GetJianTiStr(cat);

                    if (mapping.ContainsKey(jianti))
                    {
                        var mapStr = mapping[jianti];

                        if (JavDataBaseManager.HasCategoryByName(mapStr))
                        {
                            catSb.Append(mapStr + ",");
                        }
                    }
                    else
                    {
                        catSb.Append("[" + jianti + "],");
                    }
                }
            }

            javBusRecord.Actress = actSb.ToString();
            javBusRecord.Director = dirSb.ToString();
            javBusRecord.Company = comSb.ToString();
            javBusRecord.Publisher = pubSb.ToString();
            javBusRecord.Category = catSb.ToString();

            return javBusRecord;
        }

        public static List<RefreshModel> GetJavbusAVList(string url, int page, bool onlyMag = true)
        {
            List<RefreshModel> ret = new List<RefreshModel>();

            var cc = HtmlManager.GetCookies("https://www.javbus.com");

            if (onlyMag)
            {
                cc.Add(new Cookie()
                {
                    Domain = "www.javbus.com",
                    Name = "existmag",
                    Value = "mag"
                });
            }
            else
            {
                cc.Add(new Cookie()
                {
                    Domain = "www.javbus.com",
                    Name = "existmag",
                    Value = "all"
                });
            }

            var c = new CookieContainer();
            c.Add(cc);

            for (int i = 1; i <= page; i++)
            {
                var htmlResult = HtmlManager.GetHtmlContentViaUrl(url + "/" + i, "utf-8", true, c);

                if (htmlResult.Success)
                {
                    HtmlDocument htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(htmlResult.Content);

                    var itemPath = "//div[@class='item']";

                    var itemNodes = htmlDocument.DocumentNode.SelectNodes(itemPath);

                    foreach (var item in itemNodes)
                    {
                        if (!item.InnerHtml.Contains("avatar-box"))
                        {
                            RefreshModel temp = new RefreshModel();

                            var itemUrl = item.ChildNodes[1].Attributes["href"].Value;
                            var id = itemUrl.Substring(itemUrl.LastIndexOf("/") + 1);
                            var name = item.ChildNodes[1].ChildNodes[1].ChildNodes[1].Attributes["title"].Value;
                            var pic = item.ChildNodes[1].ChildNodes[1].ChildNodes[1].Attributes["src"].Value;

                            temp.Id = id;
                            temp.Name = name;
                            temp.Url = pic.Replace("https://pics.javbus.com/thumb/", "https://pics.javbus.com/cover/").Replace(".jpg", "_b.jpg");

                            ret.Add(temp);
                        }
                    }
                }
            }

            return ret;
        }

        public static void AvatorMatch()
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
            var cc = GetJavBusCookie();

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
    }
}
