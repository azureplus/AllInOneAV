using DataBaseManager.JavDataBaseHelper;
using Model.Common;
using Model.JavModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace GenerateReport
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Service.ReportService.GenerateReport();
            }
            else
            {
                Service.ReportService.GenerateReportDataOnly();

                new RestClient("https://api.day.app").Get("4z4uANLXpe8BXT3wAZVe9F/生成报表完成");
            }
        }

        private static async void DoScanAsync()
        {
            await DoScanAndMatch();

            Service.ReportService.GenerateReportDataOnly();
        }


        private static async Task DoScanAndMatch()
        {
            var p = new Process
            {
                StartInfo =
                    {
                        FileName = @"G:\Github\AllInOneAV\AllInOneAV\ScanAllAndMatch\bin\Debug\ScanAllAndMatch.exe",
                        UseShellExecute = false,
                        Arguments = "",
                        CreateNoWindow = false,
                    }
            };
            p.Start();

            await p.WaitForExitAsync();
        }
    }
}
