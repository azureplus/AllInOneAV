using DataBaseManager.MangaDataBaseHelper;
using HtmlAgilityPack;
using Model.MangaModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace Service
{
    public class MangaService
    {
        public static void InitHanhanCategory(string htmlContent)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlContent);

            var categoryPath = "//div[@class='filter-item clearfix']";

            var categoryNodes = document.DocumentNode.SelectNodes(categoryPath);

            if (categoryNodes != null && categoryNodes.Count > 0)
            {
                foreach (var node in categoryNodes)
                {
                    var rootNode = node.ChildNodes.FindFirst("label");

                    if (rootNode != null)
                    {
                        MangaCategory temp = new MangaCategory();
                        temp.SourceType = MangaCategorySourceType.憨憨漫画;
                        temp.RootCategory = rootNode.InnerHtml;

                        var categoryNode = node.ChildNodes.FindFirst("ul").ChildNodes;

                        foreach (var subNode in categoryNode)
                        {
                            var aTag = subNode.ChildNodes.FindFirst("a");

                            if (aTag != null)
                            {
                                var url = aTag.Attributes["href"].Value.Trim();
                                var name = aTag.InnerHtml.Trim();

                                if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(name))
                                {
                                    url = url.Substring(url.IndexOf("/list/") + "/list/".Length);

                                    temp.Url = string.IsNullOrEmpty(url) ? "" : url.Substring(0, url.LastIndexOf("/"));
                                    temp.Category = name;

                                    MangaDatabaseHelper.InsertMangaCategory(temp);
                                }
                            }
                        }
                    }
                }
            }
        }

        //下载漫画
        public static void TestDownload(string name, string folder)
        {
            Console.WriteLine($"正在处理{name}");

            var prefix = "http://www.5ikanhm.top";
            var html = HtmlManager.GetHtmlContentViaUrl("http://www.5ikanhm.top/book/423");

            if (html.Success)
            {
                Console.WriteLine($"获取内容成功");

                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }

                Thread.Sleep(100);

                var comicFolder = folder + "\\" + name + "\\";
                Directory.CreateDirectory(comicFolder);

                Console.WriteLine($"新建文件夹成功");

                List<string> chapters = new List<string>();

                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html.Content);

                string xpath = "//ul[@id='detail-list-select']//a";

                HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes(xpath);

                foreach (var node in nodes)
                {
                    chapters.Add(prefix + node.Attributes["href"].Value);
                }

                foreach (var chapter in chapters)
                {
                    Console.WriteLine($"处理章节{chapter}");

                    var chtml = HtmlManager.GetHtmlContentViaUrl(chapter, agent: "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1");

                    if (chtml.Success)
                    {
                        Console.WriteLine($"处理章节内容成功");
                        htmlDocument.LoadHtml(chtml.Content);

                        string cNameXpath = "//p[@class='view-fix-top-bar-title']";
                        string cXpath = "//div[@id='cp_img']//img";

                        HtmlNode cNameNode = htmlDocument.DocumentNode.SelectSingleNode(cNameXpath);
                        HtmlNodeCollection cImgNodes = htmlDocument.DocumentNode.SelectNodes(cXpath);

                        var chaperFolder = comicFolder + cNameNode.InnerText + "\\";
                        Directory.CreateDirectory(chaperFolder);

                        int i = 1;

                        foreach (var img in cImgNodes)
                        {
                            DownloadHelper.DownloadFile(img.Attributes["data-original"].Value, chaperFolder + i + ".jpg");

                            Console.WriteLine($"处理章节{cNameNode.InnerText}, 第 {i++} 张图片");
                        }
                    }
                }
            }
        }
    }
}
