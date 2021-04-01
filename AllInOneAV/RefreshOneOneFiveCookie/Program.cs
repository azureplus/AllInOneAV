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

            var cookieData = new ChromeCookieReader().ReadCookies("115.com");

            var json = JsonConvert.SerializeObject(cookieData.Where(x => !x.Value.Contains(",")).Distinct());
   
            ScanDataBaseManager.InsertOneOneFiveCookie(new OneOneFiveCookieModel
            {
                OneOneFiveCookie = json
            });
        }
    }
}
