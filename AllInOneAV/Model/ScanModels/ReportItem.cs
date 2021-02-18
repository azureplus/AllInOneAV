using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.ScanModels
{
    public class Report
    {
        public int ReportId { get; set; }
        public DateTime ReportDate { get; set; }
        public DateTime EndDate { get; set; }
        public int IsFinish { get; set; }
        public int TotalCount { get; set; }
        public int TotalExist { get; set; }
        public decimal TotalExistSize { get; set; }
        public int LessThenOneGiga { get; set; }
        public int OneGigaToTwo { get; set; }
        public int TwoGigaToFour { get; set; }
        public int FourGigaToSix { get; set; }
        public int GreaterThenSixGiga { get; set; }
        public int H265Count { get; set; }
        public int ChineseCount { get; set; }
        public string Extension { get; set; }
        public Dictionary<string, int> ExtensionModel { get; set; }
        public string ExtensionJson
        {
            get 
            {
                return JsonConvert.SerializeObject(this.ExtensionModel);
            }
        }
    }

    public class ReportItem
    {
        public int ReportItemId { get; set; }
        public string ItemName { get; set; }
        public int ExistCount { get; set; }
        public int TotalCount { get; set; }
        public double TotalSize { get; set; }
        public int ReportId { get; set; }
        public int ReportType { get; set; }
    }

    public enum ReportType
    { 
        Actress = 1,
        Category = 2,
        Director = 3,
        Company = 4,
        Publisher = 5,
        Prefix = 6,
        Date = 7,
    }
}
