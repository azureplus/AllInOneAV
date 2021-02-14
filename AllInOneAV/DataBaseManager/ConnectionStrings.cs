
using Utils;

namespace DataBaseManager
{
    public class ConnectionStrings
    {
        public static string AvManager
        {
            get 
            {
                return JavINIClass.IniReadValue("AvManager", "db");
            }
        }

        public static string Scan
        {
            get
            {
                return JavINIClass.IniReadValue("Scan", "db");
            }
        }

        public static string Jav
        {
            get
            {
                return JavINIClass.IniReadValue("Jav", "db");
            }
        }

        public static string Manga
        {
            get
            {
                return JavINIClass.IniReadValue("Manga", "db");
            }
        }
    }
}
