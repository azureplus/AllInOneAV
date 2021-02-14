using DataBaseManager.Common;
using Model.JavModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace DataBaseManager.JavDataBaseHelper
{
    public class JavDataBaseManager
    {
        private static string con;
        private static SqlConnection mycon;

        static JavDataBaseManager()
        {
            con = string.Format("Server={0};Database={1};User=sa;password=19880118Qs123!", JavINIClass.IniReadValue("Jav", "server"), JavINIClass.IniReadValue("Jav", "db"));
            mycon = new SqlConnection(con);
        }

        public static List<Category> GetCategories()
        {
            var sql = @"SELECT * FROM Category";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Category>();
        }

        public static List<Actress> GetActress()
        {
            var sql = @"SELECT * FROM Actress";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Actress>();
        }

        public static List<Company> GetCompany()
        {
            var sql = @"SELECT * FROM Company";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Company>();
        }

        public static List<Publisher> GetPublisher()
        {
            var sql = @"SELECT * FROM Publisher";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Publisher>();
        }

        public static List<Director> GetDirector()
        {
            var sql = @"SELECT * FROM Director";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Director>();
        }

        public static int InsertCategory(Category category)
        {
            var sql = @"INSERT INTO Category (Name, URL, CreateTime) VALUES (@name, @url, @createTime)";

            SqlParameter[] paras ={
                        new SqlParameter("@name",SqlDbType.NVarChar,100),
                        new SqlParameter("@url",SqlDbType.NVarChar,200),
                        new SqlParameter("@createTime",SqlDbType.DateTime)
                    };

            paras[0].Value = category.Name ?? "";
            paras[1].Value = category.Url ?? "";
            paras[2].Value = DateTime.Now;

            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql, paras);
        }

        public static int InsertScanURL(ScanURL s)
        {
            var sql = @"INSERT INTO ScanURL (Category, URL, ID, Title, IsDownload, CreateTime) VALUES (@category, @url, @id, @title, @isDownload, @createTime)";

            SqlParameter[] paras ={
                        new SqlParameter("@category",SqlDbType.NVarChar,100),
                        new SqlParameter("@url",SqlDbType.NVarChar,200),
                        new SqlParameter("@id",SqlDbType.NVarChar,500),
                        new SqlParameter("@title",SqlDbType.NVarChar,500),
                        new SqlParameter("@isDownload",SqlDbType.Int),
                        new SqlParameter("@createTime",SqlDbType.DateTime)
                    };

            paras[0].Value = s.Category;
            paras[1].Value = s.URL;
            paras[2].Value = s.ID;
            paras[3].Value = s.Title;
            paras[4].Value = s.IsDownload;
            paras[5].Value = DateTime.Now;

            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql, paras);
        }

        public static bool HasScan(ScanURL s)
        {
            var sql = @"SELECT * FROM ScanURL WHERE Url = @URL AND IsDownload = 1";

            SqlParameter[] paras ={
                        new SqlParameter("@URL",SqlDbType.NVarChar,500),
                    };

            paras[0].Value = s.URL;

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql, paras).ToList<ScanURL>().Count > 0 ? true : false;
        }

        public static List<ScanURL> GetScanURL()
        {
            var sql = @"SELECT * FROM ScanURL";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<ScanURL>();
        }

        public static int InsertAV(AV av)
        {
            var sql = @"INSERT INTO AV (ID, Name, Company, Director, Publisher, Category, Actress, ReleaseDate, AvLength, CreateTime, PictureURL, URL) 
                        VALUES (@id, @name, @company, @director, @publisher, @category, @actress, @releaseDate, @avLength, @createTime, @pictureURL, @url)";

            SqlParameter[] paras ={
                        new SqlParameter("@id",SqlDbType.NVarChar,100),
                        new SqlParameter("@name",SqlDbType.NVarChar,300),
                        new SqlParameter("@company",SqlDbType.NVarChar,200),
                        new SqlParameter("@director",SqlDbType.NVarChar,200),
                        new SqlParameter("@publisher",SqlDbType.NVarChar,200),
                        new SqlParameter("@category",SqlDbType.NVarChar,600),
                        new SqlParameter("@actress",SqlDbType.NVarChar,4000),
                        new SqlParameter("@releaseDate",SqlDbType.DateTime),
                        new SqlParameter("@avLength",SqlDbType.Int),
                        new SqlParameter("@CreateTime",SqlDbType.DateTime),
                        new SqlParameter("@pictureURL",SqlDbType.NVarChar,1000),
                        new SqlParameter("@url",SqlDbType.NVarChar,1000)
                    };

            paras[0].Value = av.ID;
            paras[1].Value = av.Name;
            paras[2].Value = av.Company;
            paras[3].Value = av.Director;
            paras[4].Value = av.Publisher;
            paras[5].Value = av.Category;
            paras[6].Value = av.Actress;
            paras[7].Value = av.ReleaseDate;
            paras[8].Value = av.AvLength;
            paras[9].Value = DateTime.Now;
            paras[10].Value = av.PictureURL;
            paras[11].Value = av.URL;

            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql, paras);
        }

        public static bool HasAv(string url)
        {
            var sql = @"SELECT * FROM AV WHERE Url = '" + url + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<AV>().Count > 0 ? true : false;
        }

        public static bool HasAv(string id, string name)
        {
            var sql = @"SELECT * FROM AV WHERE Id = '" + id + "' AND Name = N'" + FileUtility.ReplaceInvalidChar(name) + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<AV>().Count > 0 ? true : false;
        }

        public static List<AV> GetAllAV()
        {
            var sql = @"SELECT * FROM AV";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<AV>();
        }

        public static List<AV> GetAllAV(string id)
        {
            var sql = @"SELECT * FROM AV WHERE ID = '" + id + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<AV>();
        }

        public static AV GetAV(int avid)
        {
            var sql = @"SELECT * FROM AV WHERE AvID = " + avid;

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToModel<AV>();
        }

        public static bool HasCompany(string url)
        {
            var sql = @"SELECT * FROM Company WHERE URL = '" + url + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Company>().Count > 0 ? true : false;
        }

        public static int InsertCompany(Company c)
        {
            var sql = @"INSERT INTO Company (Name, URL, CreateTime) VALUES (@name, @url, @createTime)";

            SqlParameter[] paras ={
                        new SqlParameter("@name",SqlDbType.NVarChar,100),
                        new SqlParameter("@url",SqlDbType.NVarChar,300),
                        new SqlParameter("@CreateTime",SqlDbType.DateTime)
                    };

            paras[0].Value = c.Name;
            paras[1].Value = c.URL;
            paras[2].Value = DateTime.Now;

            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql, paras);
        }

        public static bool HasDirector(string url)
        {
            var sql = @"SELECT * FROM Director WHERE URL = '" + url + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Director>().Count > 0 ? true : false;
        }

        public static int InsertDirector(Director c)
        {
            var sql = @"INSERT INTO Director (Name, URL, CreateTime) VALUES (@name, @url, @createTime)";

            SqlParameter[] paras ={
                        new SqlParameter("@name",SqlDbType.NVarChar,100),
                        new SqlParameter("@url",SqlDbType.NVarChar,300),
                        new SqlParameter("@CreateTime",SqlDbType.DateTime)
                    };

            paras[0].Value = c.Name;
            paras[1].Value = c.URL;
            paras[2].Value = DateTime.Now;

            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql, paras);
        }

        public static bool HasPublisher(string url)
        {
            var sql = @"SELECT * FROM Publisher WHERE URL = '" + url + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Publisher>().Count > 0 ? true : false;
        }

        public static int InsertPublisher(Publisher c)
        {
            var sql = @"INSERT INTO Publisher (Name, URL, CreateTime) VALUES (@name, @url, @createTime)";

            SqlParameter[] paras ={
                        new SqlParameter("@name",SqlDbType.NVarChar,100),
                        new SqlParameter("@url",SqlDbType.NVarChar,300),
                        new SqlParameter("@CreateTime",SqlDbType.DateTime)
                    };

            paras[0].Value = c.Name;
            paras[1].Value = c.URL;
            paras[2].Value = DateTime.Now;

            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql, paras);
        }

        public static bool HasActress(string url)
        {
            var sql = @"SELECT * FROM Actress WHERE URL = '" + url + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Actress>().Count > 0 ? true : false;
        }

        public static int InsertActress(Actress c)
        {
            var sql = @"INSERT INTO Actress (Name, URL, CreateTime) VALUES (@name, @url, @createTime)";

            SqlParameter[] paras ={
                        new SqlParameter("@name",SqlDbType.NVarChar,100),
                        new SqlParameter("@url",SqlDbType.NVarChar,300),
                        new SqlParameter("@CreateTime",SqlDbType.DateTime)
                    };

            paras[0].Value = c.Name;
            paras[1].Value = c.URL;
            paras[2].Value = DateTime.Now;

            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql, paras);
        }

        public static int UpdateScanURL(string url)
        {
            var sql = string.Format("update scanurl set isdownload = 1 where url = '{0}'", url);
            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql);
        }

        public static int DeleteJavBusCategory()
        {
            var sql = @"TRUNCATE TABLE JavBusCategoryMapping";

            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql);
        }

        public static int InsertJavBusCategory(string javBusCategory, string javLibCategory)
        {
            var sql = @"INSERT INTO JavBusCategoryMapping (JavBusCategory, JavLibCategory) VALUES ('" + javBusCategory + "', '" + javLibCategory + "')";

            return SqlHelper.ExecuteNonQuery(con, CommandType.Text, sql);
        }

        public static Dictionary<string, string> GetJavBusCategoryMapping()
        {
            var sql = @"SELECT JavBusCategory, JavLibCategory FROM JavBusCategoryMapping";

            return SqlHelper.ExecuteDataset(con, CommandType.Text, sql).Tables[0].ToDictionary<string, string>("JavBusCategory", "JavLibCategory");
        }

        public static bool HasActressByName(string name)
        {
            var sql = @"SELECT * FROM Actress WHERE NAME = '" + name + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Actress>().Count > 0 ? true : false;
        }

        public static bool HasDirectorByName(string name)
        {
            var sql = @"SELECT * FROM Director WHERE NAME = '" + name + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Director>().Count > 0 ? true : false;
        }

        public static bool HasCompanyByName(string name)
        {
            var sql = @"SELECT * FROM Company WHERE NAME = '" + name + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Company>().Count > 0 ? true : false;
        }

        public static bool HasCategoryByName(string name)
        {
            var sql = @"SELECT * FROM Category WHERE NAME = '" + name + "'";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Category>().Count > 0 ? true : false;
        }

        public static List<CommonModel> GetSimilarContent(string table, string content)
        {
            var sql = @"SELECT Name FROM " + table + " WHERE NAME LIKE ('%" + content + "%')";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<CommonModel>();
        }

        public static List<CommonModel> GetSimilarContent(string table)
        {
            var sql = @"SELECT Name FROM " + table;

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<CommonModel>();
        }

        public static List<Actress> GetAllValidMap(string table)
        {
            var sql = @"SELECT Name, Url FROM " + table + " WHERE Url <> ''";

            return SqlHelper.ExecuteDataTable(con, CommandType.Text, sql).ToList<Actress>();
        }
    }
}
