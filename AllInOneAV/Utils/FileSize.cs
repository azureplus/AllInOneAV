using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class FileSize
    {
        private const double KBCount = 1024;
        private const double MBCount = KBCount * 1024;
        private const double GBCount = MBCount * 1024;
        private const double TBCount = GBCount * 1024;

        public static string GetAutoSizeString(double size, int roundCount)
        {
            if (KBCount > size)
            {
                return Math.Round(size, roundCount) + "B";
            }
            else if (MBCount > size)
            {
                return Math.Round(size / KBCount, roundCount) + "KB";
            }
            else if (GBCount > size)
            {
                return Math.Round(size / MBCount, roundCount) + "MB";
            }
            else if (TBCount > size)
            {
                return Math.Round(size / GBCount, roundCount) + "GB";
            }
            else
            {
                return Math.Round(size / TBCount, roundCount) + "TB";
            }
        }

        public static double GetByteFromStr(string content)
        {
            double ret = 0d;
            content = content.ToLower();

            if (content.EndsWith("kb"))
            {
                double.TryParse(content.Replace("kb", ""), out ret);

                return ret * KBCount;
            }

            if (content.EndsWith("mb"))
            {
                double.TryParse(content.Replace("mb", ""), out ret);

                return ret * MBCount;
            }

            if (content.EndsWith("gb"))
            {
                double.TryParse(content.Replace("gb", ""), out ret);

                return ret * GBCount;
            }

            if (content.EndsWith("tb"))
            {
                double.TryParse(content.Replace("tb", ""), out ret);

                return ret * TBCount;
            }

            return ret;
        }
    }
}
