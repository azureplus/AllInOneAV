﻿using DataBaseManager.ScanDataBaseHelper;
using HtmlAgilityPack;
using Model.Common;
using MonoTorrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace Service
{
    public class MagService
    {
        private static readonly string sukebei = JavINIClass.IniReadValue("Mag", "sukebei");

        public static List<SeedMagnetSearchModel> SearchBtsow(string id)
        {
            List<SeedMagnetSearchModel> ret = new List<SeedMagnetSearchModel>();

            try
            {
                var serachContent = "https://btsow.space/search/" + id;
                var htmlRet = HtmlManager.GetHtmlWebClient("https://btsow.space", serachContent, null, true);

                if (htmlRet.Success)
                {
                    HtmlDocument htmlDocument = new HtmlDocument();
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
                        var subHtmlRet = HtmlManager.GetHtmlContentViaUrl(r.Url);

                        if (subHtmlRet.Success)
                        {
                            htmlDocument = new HtmlDocument();
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

            return ret.OrderByDescending(x => x.Size).ToList();
        }

        public static List<SeedMagnetSearchModel> SearchSukebei(string id, CookieContainer cc = null)
        {
            //if (cc == null)
            //{
            //    var c = HtmlManager.GetCookies("https://sukebei.nyaa.si/");
            //    cc = new CookieContainer();
            //    cc.Add(c);
            //}

            List<SeedMagnetSearchModel> ret = new List<SeedMagnetSearchModel>();

            try
            {
                HtmlResponse htmlRet = new HtmlResponse() { Success = false };

                if (sukebei == "pro")
                {
                    var serachContent = "https://sukebei.nyaa.pro/search/c_0_0_k_" + id;
                    htmlRet = HtmlManager.GetHtmlWebClient("https://sukebei.nyaa.pro", serachContent, cc);
                }
                else if (sukebei == "si")
                {
                    var serachContent = "https://sukebei.nyaa.si?f=0&c=0_0&q=" + id;
                    htmlRet = HtmlManager.GetHtmlWebClient("https://sukebei.nyaa.si", serachContent, cc);
                }

                if (htmlRet.Success)
                {
                    HtmlDocument htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(htmlRet.Content);

                    string xpath = "//tr";

                    HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes(xpath);

                    foreach (var node in nodes.Skip(1))
                    {
                        var text = FileUtility.ReplaceInvalidChar(node.ChildNodes[3].InnerText.Trim());
                        var a = node.ChildNodes[5].OuterHtml;
                        var size = node.ChildNodes[7].InnerText.Trim();
                        var date = node.ChildNodes[9].OuterHtml.Trim().Replace("<td class=\"text-center\" data-timestamp=\"", "").Replace("\"></td>", "");
                        //var complete = node.ChildNodes[15].InnerText.Trim();

                        var url = a.Substring(a.IndexOf("<a href=\"magnet:?xt") + 9);
                        url = url.Substring(0, url.IndexOf("\""));

                        int seconds = 0;

                        int.TryParse(date, out seconds);

                        DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
                        DateTime dt = startTime.AddSeconds(seconds);

                        SeedMagnetSearchModel temp = new SeedMagnetSearchModel
                        {
                            Title = text,
                            Size = FileUtility.GetFileSizeFromString(size),
                            Date = dt,
                            Url = "",
                            //CompleteCount = int.Parse(complete),
                            MagUrl = url,
                            Source = SearchSeedSiteEnum.Sukebei
                        };

                        ret.Add(temp);
                    }
                }
            }
            catch (Exception ee)
            {

            }

            return ret.Where(x => x.Size >= 0).OrderByDescending(x => x.CompleteCount).ThenByDescending(x => x.Size).ToList();
        }

        public static async Task<Torrent> GetTorrentInfo(string magurl, string webUrl, string saveFolder, string fileName)
        {
            Torrent ret = null;
            var torrent = SaveMagnetToTorrent(magurl, webUrl, saveFolder, fileName);

            if (!string.IsNullOrEmpty(torrent) && File.Exists(torrent))
            {
                ret = await Torrent.LoadAsync(torrent);
            }

            return ret;
        }

        public static string SaveMagnetToTorrent(string magurl, string webUrl, string saveFolder, string fileName)
        {
            string ret = (saveFolder.EndsWith("\\") || saveFolder.EndsWith("/")) ? saveFolder + fileName : saveFolder + "\\" + fileName;

            if (string.IsNullOrEmpty(magurl) || (!magurl.StartsWith("magnet:?xt=") && magurl.Length != 40))
            {
                return ret;
            }

            try
            {
                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                    Thread.Sleep(50);
                }
                else
                {
                    if (File.Exists(ret))
                    {
                        File.Delete(ret);
                        Thread.Sleep(50);
                    }
                }

                File.Create(ret).Close();


                if (magurl.Length == 40)
                {
                    DownloadHelper.DownloadFile(webUrl + magurl + ".torrent", ret);
                }
                else
                {
                    var hash = magurl.Substring(magurl.IndexOf("btih:") + "btih:".Length);
                    hash = hash.Substring(0, hash.IndexOf("&"));

                    DownloadHelper.DownloadFile(webUrl + hash + ".torrent", ret);
                }
            }
            catch (Exception ee)
            {
                ret = "";
            }

            return ret;
        }
    }
}
