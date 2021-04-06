using DataBaseManager.JavDataBaseHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{
    public class CommonService
    {
        public static void BackUpJav(string drive)
        {
            var dateStr = DateTime.Today.ToString("yyyyMMdd");
            var extension = ".json";
            var folder = drive + @"\Github\AllInOneAV\Scripts\\";
            var rawFolder = folder + "dataRaw\\";
            var zipFileFoler = folder + "dataZip\\";
            var zipFile = zipFileFoler + dateStr + ".zip";
            var avFile = rawFolder + "av" + dateStr + extension;
            var actressFile = rawFolder + "actress" + dateStr + extension;
            var directorFile = rawFolder + "director" + dateStr + extension;
            var companyFile = rawFolder + "company" + dateStr + extension;
            var publisherFile = rawFolder + "publisher" + dateStr + extension;

            StreamWriter sw = null;

            var avs = JavDataBaseManager.GetAllAV();
            var actress = JavDataBaseManager.GetActress();
            var director = JavDataBaseManager.GetDirector();
            var company = JavDataBaseManager.GetCompany();
            var publisher = JavDataBaseManager.GetPublisher();

            if (!Directory.Exists(rawFolder))
            {
                Directory.CreateDirectory(rawFolder);
            }

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (!Directory.Exists(zipFileFoler))
            {
                Directory.CreateDirectory(zipFileFoler);
            }

            foreach (var file in new DirectoryInfo(rawFolder).GetFiles())
            {
                file.Delete();
                Thread.Sleep(50);
            }

            if (!File.Exists(avFile))
            {
                File.Create(avFile).Close();

                sw = new StreamWriter(avFile);
                sw.WriteLine(JsonConvert.SerializeObject(avs));
                sw.Close();
            }

            if (!File.Exists(actressFile))
            {
                File.Create(actressFile).Close();

                sw = new StreamWriter(actressFile);
                sw.WriteLine(JsonConvert.SerializeObject(actress));
                sw.Close();
            }

            if (!File.Exists(directorFile))
            {
                File.Create(directorFile).Close();

                sw = new StreamWriter(directorFile);
                sw.WriteLine(JsonConvert.SerializeObject(director));
                sw.Close();
            }

            if (!File.Exists(companyFile))
            {
                File.Create(companyFile).Close();

                sw = new StreamWriter(companyFile);
                sw.WriteLine(JsonConvert.SerializeObject(company));
                sw.Close();
            }

            if (!File.Exists(publisherFile))
            {
                File.Create(publisherFile).Close();

                sw = new StreamWriter(publisherFile);
                sw.WriteLine(JsonConvert.SerializeObject(publisher));
                sw.Close();
            }
        }
    }
}
