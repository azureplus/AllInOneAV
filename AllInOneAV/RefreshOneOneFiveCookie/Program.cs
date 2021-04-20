using DataBaseManager.ScanDataBaseHelper;
using Model.ScanModels;
using Newtonsoft.Json;
using Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RefreshOneOneFiveCookie
{
    class Program
    {
        static void Main(string[] args)
        {
            CookieContainer cc = new CookieContainer();

            var cookieData = new ChromeCookieReader().ReadCookies(".115.com");

            foreach (var item in cookieData.Where(x => !x.Value.Contains(",")).Distinct())
            {
                if (item.Name == "PHPSESSID" || item.Name == "UID" || item.Name == "CID" || item.Name == "SEID" || item.Name == "115_lang")
                {
                    Cookie c = new Cookie(item.Name, item.Value, "/", "115.com");
                    cc.Add(c);
                }
            }

            cookieData.AddRange(new ChromeCookieReader().ReadCookies("webapi.115.com"));

            var json = JsonConvert.SerializeObject(cookieData);

            ScanDataBaseManager.InsertOneOneFiveCookie(new OneOneFiveCookieModel
            {
                OneOneFiveCookie = json
            });
        }
    }
}
