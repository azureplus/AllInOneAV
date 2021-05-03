using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using Model.Common;
using Model.JavModels;
using Model.ScanModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Service
{
    public class ReportService
    {
        private static Dictionary<int, List<MyFileInfo>> GenerateExistingAVs(List<AV> avs)
        {
            Dictionary<int, List<MyFileInfo>> fileContainer = new Dictionary<int, List<MyFileInfo>>();
            var oneOneFiveFiles = OneOneFiveService.Get115FilesModel();
            List<OneOneFiveItemWIthMatchId> matches = new List<OneOneFiveItemWIthMatchId>();

            avs.ForEach(x => x.AvIdAndName = x.ID + "-" + x.Name);
            foreach (var file in oneOneFiveFiles)
            {
                if (file.n.Split('-').Length >= 3)
                {
                    file.AvId = file.n.Split('-')[0] + "-" + file.n.Split('-')[1];
                    file.AvName = file.n.Replace(file.AvId, "").Substring(1).Replace("." + file.ico, "").Replace("-C", "");
                }
            }

            foreach (var file in oneOneFiveFiles.Where(x => !string.IsNullOrEmpty(x.AvName)))
            {
                var matchList = avs.FirstOrDefault(x => x.Name == file.AvName);
                AV match = null;

                if (matchList != null)
                {
                    match = matchList;
                }

                if (match != null)
                {
                    matches.Add(new OneOneFiveItemWIthMatchId()
                    {
                        ico = file.ico,
                        n = file.n,
                        s = file.s,
                        MatchId = match.AvId
                    });
                }
            }

            foreach (var match in matches)
            {
                if (fileContainer.ContainsKey(match.MatchId))
                {
                    fileContainer[match.MatchId].Add(new MyFileInfo() { 
                        Extension = "." + match.ico,
                        FullName = match.n,
                        Length = match.s,
                        Name = match.n
                    });
                }
                else
                {
                    fileContainer.Add(match.MatchId, new List<MyFileInfo> { new MyFileInfo() {
                        Extension = "." + match.ico,
                        FullName = match.n,
                        Length = match.s,
                        Name = match.n
                    } });
                }
            }

            return fileContainer;
        }

        private static void GenerateBasicReportData(List<AV> avs, Dictionary<int, List<FileInfo>> matchFiles, ReportModel report)
        {
            report.TotalRecord = avs.Count;
            report.TotalAV = matchFiles.Count;
            string[] status = new string[] { "|", "/", "-", "\\", "|", "/", "-", "\\" };
            int index = 0;

            foreach (var av in matchFiles)
            {
                Console.Write(status[index++] + "\r");

                if (index == 8)
                {
                    index = 0;
                }

                if (av.Value.Count > 1)
                {
                    report.AvHasMoreThan1File++;
                }

                foreach (var file in av.Value)
                {
                    var ext = file.Extension.ToLower();
                    var avModel = avs.FirstOrDefault(x => x.AvId == av.Key);

                    report.TotalSizeLong += file.Length;
                    report.TotalFiles++;

                    if (file.Length >= (long)1 * 1024 * 1024 * 1024)
                    {
                        report.FileLargeThan1G++;
                    }

                    if (file.Length >= (long)2 * 1024 * 1024 * 1024)
                    {
                        report.FileLargeThan2G++;
                    }

                    if (file.Length >= (long)4 * 1024 * 1024 * 1024)
                    {
                        report.FileLargeThan4G++;
                    }

                    if (file.Length >= (long)6 * 1024 * 1024 * 1024)
                    {
                        report.FileLargeThan6G++;
                    }

                    if (report.Formats.ContainsKey(ext))
                    {
                        report.Formats[ext]++;
                    }
                    else
                    {
                        report.Formats.Add(ext, 1);
                    }

                    if (avModel != null)
                    {
                        var actressList = avModel.Actress.Split(',');
                        var categoryList = avModel.Category.Split(',');
                        var directorList = avModel.Director.Split(',');
                        var companyList = avModel.Company.Split(',');
                        var prefix = avModel.ID.Split('-')[0];
                        var date = avModel.ReleaseDate.Year;

                        //演员
                        foreach (var actress in actressList.Where(x => !string.IsNullOrEmpty(x)))
                        {
                            if (report.TopActress.ContainsKey(actress))
                            {
                                report.TopActress[actress]++;
                            }
                            else
                            {
                                report.TopActress.Add(actress, 1);
                            }

                            var actressRecord = report.ActressRatio.Find(x => x.Name == actress);
                            if (actressRecord == null)
                            {
                                var total = avs.Where(x => x.Actress.Contains(actress)).Count();
                                report.ActressRatio.Add(new ReportRatioModel
                                {
                                    Have = 1,
                                    Name = actress,
                                    Total = total,
                                    Ratio = (decimal)1 / (decimal)total
                                });
                            }
                            else
                            {
                                actressRecord.Have++;
                                actressRecord.Ratio = (decimal)actressRecord.Have / (decimal)actressRecord.Total;
                            }
                        }

                        //种类
                        foreach (var category in categoryList.Where(x => !string.IsNullOrEmpty(x)))
                        {
                            if (report.TopCategory.ContainsKey(category))
                            {
                                report.TopCategory[category]++;
                            }
                            else
                            {
                                report.TopCategory.Add(category, 1);
                            }

                            var categoryRecord = report.CategoryRatio.Find(x => x.Name == category);
                            if (categoryRecord == null)
                            {
                                var total = avs.Where(x => x.Category.Contains(category)).Count();
                                report.CategoryRatio.Add(new ReportRatioModel
                                {
                                    Have = 1,
                                    Name = category,
                                    Total = total,
                                    Ratio = (decimal)1 / (decimal)total
                                });
                            }
                            else
                            {
                                categoryRecord.Have++;
                                categoryRecord.Ratio = (decimal)categoryRecord.Have / (decimal)categoryRecord.Total;
                            }
                        }

                        //公司
                        foreach (var company in companyList.Where(x => !string.IsNullOrEmpty(x)))
                        {
                            if (report.TopCompany.ContainsKey(company))
                            {
                                report.TopCompany[company]++;
                            }
                            else
                            {
                                report.TopCompany.Add(company, 1);
                            }

                            var companyRecord = report.CompanyRatio.Find(x => x.Name == company);
                            if (companyRecord == null)
                            {
                                var total = avs.Where(x => x.Company.Contains(company)).Count();
                                report.CompanyRatio.Add(new ReportRatioModel
                                {
                                    Have = 1,
                                    Name = company,
                                    Total = total,
                                    Ratio = (decimal)1 / (decimal)total
                                });
                            }
                            else
                            {
                                companyRecord.Have++;
                                companyRecord.Ratio = (decimal)companyRecord.Have / (decimal)companyRecord.Total;
                            }
                        }

                        //导演
                        foreach (var director in directorList.Where(x => !string.IsNullOrEmpty(x)))
                        {
                            if (report.TopDirctor.ContainsKey(director))
                            {
                                report.TopDirctor[director]++;
                            }
                            else
                            {
                                report.TopDirctor.Add(director, 1);
                            }

                            var directorRecord = report.DirectorRatio.Find(x => x.Name == director);
                            if (directorRecord == null)
                            {
                                var total = avs.Where(x => x.Director.Contains(director)).Count();
                                report.DirectorRatio.Add(new ReportRatioModel
                                {
                                    Have = 1,
                                    Name = director,
                                    Total = total,
                                    Ratio = (decimal)1 / (decimal)total
                                });
                            }
                            else
                            {
                                directorRecord.Have++;
                                directorRecord.Ratio = (decimal)directorRecord.Have / (decimal)directorRecord.Total;
                            }
                        }

                        //系列
                        if (report.TopPrefix.ContainsKey(prefix))
                        {
                            report.TopPrefix[prefix]++;
                        }
                        else
                        {
                            report.TopPrefix.Add(prefix, 1);
                        }

                        var prefixRecord = report.PrefixRatio.Find(x => x.Name == prefix);
                        if (prefixRecord == null)
                        {
                            var total = avs.Where(x => x.ID.StartsWith(prefix + "-")).Count();
                            report.PrefixRatio.Add(new ReportRatioModel
                            {
                                Have = 1,
                                Name = prefix,
                                Total = total,
                                Ratio = (decimal)1 / (decimal)total
                            });
                        }
                        else
                        {
                            prefixRecord.Have++;
                            prefixRecord.Ratio = (decimal)prefixRecord.Have / (decimal)prefixRecord.Total;
                        }

                        //年份
                        if (report.TopDate.ContainsKey(date + ""))
                        {
                            report.TopDate[date + ""]++;
                        }
                        else
                        {
                            report.TopDate.Add(date + "", 1);
                        }

                        var dateRecord = report.YearRatio.Find(x => x.Name == date + "");
                        if (dateRecord == null)
                        {
                            var total = avs.Where(x => x.ReleaseDate.Year == date).Count();
                            report.YearRatio.Add(new ReportRatioModel
                            {
                                Have = 1,
                                Name = date + "",
                                Total = total,
                                Ratio = (decimal)1 / (decimal)total
                            });
                        }
                        else
                        {
                            dateRecord.Have++;
                            dateRecord.Ratio = (decimal)dateRecord.Have / (decimal)dateRecord.Total;
                        }
                    }
                }
            }
        }

        private static string PrintReport(ReportModel report)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("一共有 {0} 条AV信息", report.TotalRecord));
            sb.AppendLine(string.Format("本地拥有 {0} 部AV", report.TotalAV));
            sb.AppendLine("其中包含如下格式： ");
            report.Formats.OrderByDescending(x => x.Value).ToList().ForEach(x => sb.AppendLine("\t" + x.Key + " 有 " + x.Value + "部 "));
            sb.AppendLine();
            sb.AppendLine("大于1GB的有 " + report.FileLargeThan1G);
            sb.AppendLine("大于2GB的有 " + report.FileLargeThan2G);
            sb.AppendLine("大于4GB的有 " + report.FileLargeThan4G);
            sb.AppendLine("大于6GB的有 " + report.FileLargeThan6G);
            sb.AppendLine("一共有 " + report.TotalFiles + " 个文件，总大小 " + FileSize.GetAutoSizeString(report.TotalSizeLong, 2));
            sb.AppendLine("其中有 " + report.AvHasMoreThan1File + " 部AV有超过1张CD数");

            sb.AppendLine();
            sb.AppendLine("TOP " + report.Top + " 女优");
            foreach (var content in report.TopActress.OrderByDescending(x => x.Value).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Key + " 有 " + content.Value + " 部");
            }
            sb.AppendLine();

            sb.AppendLine("TOP " + report.Top + " 系列");
            foreach (var content in report.TopPrefix.OrderByDescending(x => x.Value).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Key + " 有 " + content.Value + " 部");
            }
            sb.AppendLine();

            sb.AppendLine("TOP " + report.Top + " 类型");
            foreach (var content in report.TopCategory.OrderByDescending(x => x.Value).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Key + " 有 " + content.Value + " 部");
            }
            sb.AppendLine();

            sb.AppendLine("TOP " + report.Top + " 导演");
            foreach (var content in report.TopDirctor.OrderByDescending(x => x.Value).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Key + " 有 " + content.Value + " 部");
            }
            sb.AppendLine();

            sb.AppendLine("TOP " + report.Top + " 公司");
            foreach (var content in report.TopCompany.OrderByDescending(x => x.Value).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Key + " 有 " + content.Value + " 部");
            }
            sb.AppendLine();

            sb.AppendLine("TOP " + report.Top + " 年份");
            foreach (var content in report.TopDate.OrderByDescending(x => x.Value).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Key + " 有 " + content.Value + " 部");
            }

            sb.AppendLine();
            sb.AppendLine("总数大于100的TOP " + report.Top + " 女优占比");
            foreach (var content in report.ActressRatio.Where(x=>x.Total >= 100).OrderByDescending(x => x.Ratio).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Name + " 拥有 " + content.Have + " / " + content.Total + " 占比 " + GetPercentage(content.Have, content.Total, 1) + "%");
            }
            sb.AppendLine();

            sb.AppendLine("总数大于100的TOP " + report.Top + " 系列占比");
            foreach (var content in report.PrefixRatio.Where(x => x.Total >= 100).OrderByDescending(x => x.Ratio).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Name + " 拥有 " + content.Have + " / " + content.Total + " 占比 " + GetPercentage(content.Have, content.Total, 1) + "%");
            }
            sb.AppendLine();

            sb.AppendLine("总数大于100的TOP " + report.Top + " 类型占比");
            foreach (var content in report.CategoryRatio.Where(x => x.Total >= 100).OrderByDescending(x => x.Ratio).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Name + " 拥有 " + content.Have + " / " + content.Total + " 占比 " + GetPercentage(content.Have, content.Total, 1) + "%");
            }
            sb.AppendLine();

            sb.AppendLine("总数大于100的TOP " + report.Top + " 导演占比");
            foreach (var content in report.DirectorRatio.Where(x => x.Total >= 100).OrderByDescending(x => x.Ratio).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Name + " 拥有 " + content.Have + " / " + content.Total + " 占比 " + GetPercentage(content.Have, content.Total, 1) + "%");
            }
            sb.AppendLine();

            sb.AppendLine("总数大于100的TOP " + report.Top + " 公司占比");
            foreach (var content in report.CompanyRatio.Where(x => x.Total >= 100).OrderByDescending(x => x.Ratio).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Name + " 拥有 " + content.Have + " / " + content.Total + " 占比 " + GetPercentage(content.Have, content.Total, 1) + "%");
            }
            sb.AppendLine();

            sb.AppendLine("总数大于100的TOP " + report.Top + " 年份占比");
            foreach (var content in report.YearRatio.Where(x => x.Total >= 100).OrderByDescending(x => x.Ratio).Take(report.Top))
            {
                sb.AppendLine("\t" + content.Name + " 拥有 " + content.Have + " / " + content.Total + " 占比 " + GetPercentage(content.Have, content.Total, 1) + "%");
            }

            return sb.ToString();
        }

        private static string GetPercentage(int divied, int divide, int round = 1)
        {
            if (divide == 0)
            {
                return "0";
            }
            else
            {
                var percentage = Math.Round(((decimal)divied / (decimal)divide) * 100, round);

                return percentage + "";
            }
        }

        public static void GenerateReportDataOnly()
        {
            List<ReportItem> items = new List<ReportItem>();
            ScanDataBaseManager.DeleteReportItem();
            var allAv = JavDataBaseManager.GetAllAV();
            var allMatch = GenerateExistingAVs(allAv);

            Report report = new Report();
            report.ExtensionModel = new Dictionary<string, int>();
            report.TotalCount = allAv.Count;

            var reportId = ScanDataBaseManager.InsertReport(report);
            report.ReportId = reportId;
            int process = 0;

            foreach(var av in allAv)
            { 
                ProcessReportType(av, allMatch, report, items);
                process++;
                Console.WriteLine(process + " / " + allAv.Count);
            };

            ScanDataBaseManager.BatchInserReportItem(items);
            ScanDataBaseManager.UpdateReport(report);
            ScanDataBaseManager.UpdateReportFinish(reportId);
        }

        private static void ProcessReportType(AV av, Dictionary<int, List<MyFileInfo>> existFiles, Report report, List<ReportItem> items)
        {
            int exist = 0;
            double existSize = 0d;

            if (existFiles.ContainsKey(av.AvId))
            {
                var file = existFiles[av.AvId];

                if (file.Count() > 0)
                {
                    var biggestFile = file.FirstOrDefault(x => x.Length == file.Max(y => y.Length));

                    exist = 1;
                    existSize = biggestFile.Length;

                    report.TotalExist += 1;
                    report.TotalExistSize += biggestFile.Length;

                    var extensionKey = biggestFile.Extension;

                    if (report.ExtensionModel.ContainsKey(extensionKey))
                    {
                        report.ExtensionModel[extensionKey] = report.ExtensionModel[extensionKey] + 1;
                    }
                    else
                    {
                        report.ExtensionModel.Add(extensionKey, 1);
                    }

                    if (biggestFile.Length < (long)1 * 1024 * 1024 * 1024)
                    {
                        report.LessThenOneGiga++;
                    }

                    if (biggestFile.Length >= (long)1 * 1024 * 1024 * 1024 && biggestFile.Length < (long)2 * 1024 * 1024 * 1024)
                    {
                        report.OneGigaToTwo++;
                    }

                    if (biggestFile.Length >= (long)2 * 1024 * 1024 * 1024 && biggestFile.Length < (long)4 * 1024 * 1024 * 1024)
                    {
                        report.TwoGigaToFour++;
                    }

                    if (biggestFile.Length >= (long)4 * 1024 * 1024 * 1024 && biggestFile.Length < (long)6 * 1024 * 1024 * 1024)
                    {
                        report.FourGigaToSix++;
                    }

                    if (biggestFile.Length >= (long)6 * 1024 * 1024 * 1024)
                    {
                        report.GreaterThenSixGiga++;
                    }

                    if (biggestFile.Name.Contains("-C" + biggestFile.Extension))
                    {
                        report.ChineseCount++;
                    }
                }
            }

            foreach (ReportType type in Enum.GetValues(typeof(ReportType)))
            {
                switch (type)
                {
                    case ReportType.Actress:
                        foreach (var itemName in av.Actress.Split(',').Where(x => !string.IsNullOrEmpty(x)))
                        {
                            ProcessReportItem(ReportType.Actress, itemName, exist, existSize, report.ReportId, items);
                        }
                        break;
                    case ReportType.Category:
                        foreach (var itemName in av.Category.Split(',').Where(x => !string.IsNullOrEmpty(x)))
                        {
                            ProcessReportItem(ReportType.Category, itemName, exist, existSize, report.ReportId, items);
                        }
                        break;
                    case ReportType.Company:
                        foreach (var itemName in av.Company.Split(',').Where(x => !string.IsNullOrEmpty(x)))
                        {
                            ProcessReportItem(ReportType.Company, itemName, exist, existSize, report.ReportId, items);
                        }
                        break;
                    case ReportType.Date:
                        ProcessReportItem(ReportType.Date, av.ReleaseDate.ToString("yyyy"), exist, existSize, report.ReportId, items);
                        break;
                    case ReportType.Director:
                        foreach (var itemName in av.Director.Split(',').Where(x => !string.IsNullOrEmpty(x)))
                        {
                            ProcessReportItem(ReportType.Director, itemName, exist, existSize, report.ReportId, items);
                        }
                        break;
                    case ReportType.Prefix:
                        var prefix = av.ID.Split('-').Length >= 2 ? av.ID.Split('-')[0] : "";
                        if (!string.IsNullOrEmpty(prefix))
                        {
                            ProcessReportItem(ReportType.Prefix, prefix, exist, existSize, report.ReportId, items);
                        }
                        break;
                    case ReportType.Publisher:
                        foreach (var itemName in av.Publisher.Split(',').Where(x => !string.IsNullOrEmpty(x)))
                        {
                            ProcessReportItem(ReportType.Publisher, itemName, exist, existSize, report.ReportId, items);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private static void ProcessReportItem(ReportType type, string itemName, int exist, double existSize, int reportId, List<ReportItem> items)
        {
            var tempItem = items.FirstOrDefault(x => x.ReportId == reportId && x.ItemName == itemName && (ReportType)x.ReportType == type);
            if (tempItem != null)
            {
                tempItem.TotalSize += existSize;
                tempItem.ExistCount += exist;
                tempItem.TotalCount += 1;
            }
            else
            {
                items.Add(new ReportItem()
                {
                    ExistCount = exist,
                    ItemName = itemName,
                    ReportType = (int)type,
                    TotalCount = 1,
                    TotalSize = existSize,
                    ReportId = reportId
                });
            }

            //ScanDataBaseManager.InsertReportItem(type, FileUtility.ReplaceInvalidChar(itemName), exist, existSize, reportId);
        }
    }
}
