﻿using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using Microsoft.Ajax.Utilities;
using Model.JavModels;
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
    /// <summary>
    /// 
    /// </summary>
    public class WebAvController : Controller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        // GET: WebAv
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Test()
        {
            return View();
        }

        public ActionResult UploadSeeds()
        {
            return View();
        }

        public ActionResult GetUnmatched(bool includePlayed = true)
        {
            ViewData.Add("list", WebService.WebService.GetUnMatch(includePlayed).OrderByDescending(x => x.HasPlayed).ToList());

            return View();
        }

        public ActionResult PlayAv(string filePath)
        {
            var host = "http://www.cainqs.com:8087/avapi/playav?filename=" + filePath;
            ViewData.Add("path", host);

            return View();
        }

        public ActionResult GetAv(int page = 1, int pageSize = 20, string id = "", string category = "", string actress = "", string director = "", string company = "", string publisher = "", string releaseDate = "", string orderBy = " ReleaseDate ", string orderType = " DESC ")
        {
            List<AV> ret = new List<AV>();
            string orderStr = orderBy + orderType;
            string where = " 1 = 1 ";
            string pageStr = @" AND t.OnePage BETWEEN " + (((page - 1) * pageSize) + 1) + " AND " + page * pageSize; ;

            if (!string.IsNullOrEmpty(id))
            {
                where += string.Format(" AND ID = '{0}' ", id);
            }

            if (!string.IsNullOrEmpty(category))
            {
                where += string.Format(" AND Category LIKE '%{0}%' ", category);
            }

            if (!string.IsNullOrEmpty(actress))
            {
                where += string.Format(" AND Actress LIKE '%{0}%' ", actress);
            }

            if (!string.IsNullOrEmpty(director))
            {
                where += string.Format(" AND Director LIKE '%{0}%' ", director);
            }

            if (!string.IsNullOrEmpty(company))
            {
                where += string.Format(" AND Company LIKE '%{0}%' ", company);
            }

            if (!string.IsNullOrEmpty(publisher))
            {
                where += string.Format(" AND Publisher = '%{0}%' ", publisher);
            }

            if (!string.IsNullOrEmpty(releaseDate))
            {
                var date = DateTime.Parse(releaseDate);

                where += string.Format(" AND ReleaseDate = '{0}' ", date.ToString("yyyy-MM-dd") + " 00:00:00.000");
            }

            var items =  WebService.WebService.GetAv(orderBy, where, pageStr);

            ViewData.Add("avs", items);

            return View();
        }

        public ActionResult Av(int avId)
        {
            var av = JavDataBaseManager.GetAV(avId);
            var match = ScanDataBaseManager.GetMatchMapByAvId(avId);

            if (av == null)
            {
                av = new AV();
            }

            if (match == null)
            {
                match = new Model.ScanModels.MatchMap();
            }

            ViewData.Add("av", av);
            ViewData.Add("match", match);

            return View();
        }

        public ActionResult Actress()
        {
            return View();
        }

        public ActionResult Category()
        {
            return View();
        }

        public ActionResult UploadComics()
        {
            return View();
        }

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

        public ActionResult ShowMag()
        {
            var data = ScanDataBaseManager.GetAllMag().Where(x => !string.IsNullOrEmpty(x.AvId)).GroupBy(x => x.AvId).ToDictionary(x => x.Key, x=>x.ToList());

            ViewData.Add("data", data);        

            return View();
        }

        [HttpPost]
        public JsonResult Add115Task(string mag)
        {
            CookieContainer cc = new CookieContainer();
            bool ret = false;
            string msg = "";

            foreach (var t in JsonConvert.DeserializeObject<List<CookieItem>>(ScanDataBaseManager.GetOneOneFiveCookie().OneOneFiveCookie))
            {
                Cookie c = new Cookie(t.Name, t.Value, "/", "115.com");
                cc.Add(c);
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
    }
}