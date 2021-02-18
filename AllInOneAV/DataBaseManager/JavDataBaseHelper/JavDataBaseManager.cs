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
    public class JavDataBaseManager : DapperHelper
    {
        public static List<Category> GetCategories()
        {
            var sql = @"SELECT * FROM Category";

            return Query<Category>(ConnectionStrings.Jav, sql);
        }

        public static List<Actress> GetActress()
        {
            var sql = @"SELECT * FROM Actress";

            return Query<Actress>(ConnectionStrings.Jav, sql);
        }

        public static List<Company> GetCompany()
        {
            var sql = @"SELECT * FROM Company";

            return Query<Company>(ConnectionStrings.Jav, sql);
        }

        public static List<Publisher> GetPublisher()
        {
            var sql = @"SELECT * FROM Publisher";

            return Query<Publisher>(ConnectionStrings.Jav, sql);
        }

        public static List<Director> GetDirector()
        {
            var sql = @"SELECT * FROM Director";

            return Query<Director>(ConnectionStrings.Jav, sql);
        }

        public static int InsertCategory(Category category)
        {
            var sql = @"INSERT INTO Category (Name, URL, CreateTime) VALUES (@Name, @Url, GETDATE())";

            return Execute(ConnectionStrings.Jav, sql, category);
        }

        public static int InsertScanURL(ScanURL entity)
        {
            var sql = @"INSERT INTO ScanURL (Category, URL, ID, Title, IsDownload, CreateTime) VALUES (@Category, @URL, @ID, @Title, @IsDownload, GETDATE())";

            return Execute(ConnectionStrings.Jav, sql, entity);
        }

        public static bool HasScan(ScanURL entity)
        {
            var sql = @"SELECT * FROM ScanURL WHERE Url = @URL AND IsDownload = 1";

            return Query<ScanURL>(ConnectionStrings.Jav, sql, entity).Count > 0;
        }

        public static List<ScanURL> GetScanURL()
        {
            var sql = @"SELECT * FROM ScanURL";

            return Query<ScanURL>(ConnectionStrings.Jav, sql);
        }

        public static int InsertAV(AV av)
        {
            var sql = @"INSERT INTO AV (ID, Name, Company, Director, Publisher, Category, Actress, ReleaseDate, AvLength, CreateTime, PictureURL, URL) 
                        VALUES (@ID, @Name, @Company, @Director, @Publisher, @Category, @Actress, @ReleaseDate, @AvLength, GETDATE(), @PictureURL, @URL)";

            return Execute(ConnectionStrings.Jav, sql, av);
        }

        public static bool HasAv(string url)
        {
            var sql = @"SELECT * FROM AV WHERE Url = @url";

            return Query<AV>(ConnectionStrings.Jav, sql, new { url }).Count > 0;
        }

        public static bool HasAv(string id, string name)
        {
            var sql = @"SELECT * FROM AV WHERE Id = @id AND Name = @name";

            return Query<AV>(ConnectionStrings.Jav, sql, new { id, name }).Count > 0;
        }

        public static List<AV> GetAllAV()
        {
            var sql = @"SELECT * FROM AV";

            return Query<AV>(ConnectionStrings.Jav, sql);
        }

        public static List<AV> GetAllAV(string id)
        {
            var sql = @"SELECT * FROM AV WHERE ID = @id";

            return Query<AV>(ConnectionStrings.Jav, sql, new { id });
        }

        public static AV GetAV(int avid)
        {
            var sql = @"SELECT TOP 1 * FROM AV WHERE AvID = @avid";

            return QuerySingle<AV>(ConnectionStrings.Jav, sql, new { avid });
        }

        public static bool HasCompany(string url)
        {
            var sql = @"SELECT * FROM Company WHERE URL = @url";

            return Query<Company>(ConnectionStrings.Jav, sql, new { url }).Count > 0;
        }

        public static int InsertCompany(Company entity)
        {
            var sql = @"INSERT INTO Company (Name, URL, CreateTime) VALUES (@Name, @URL, GETDATE())";

            return Execute(ConnectionStrings.Jav, sql, entity);
        }

        public static bool HasDirector(string url)
        {
            var sql = @"SELECT * FROM Director WHERE URL = @url";

            return Query<Director>(ConnectionStrings.Jav, sql, new { url }).Count > 0;
        }

        public static int InsertDirector(Director entity)
        {
            var sql = @"INSERT INTO Director (Name, URL, CreateTime) VALUES (@Name, @URL, GETDATE())";

            return Execute(ConnectionStrings.Jav, sql, entity);
        }

        public static bool HasPublisher(string url)
        {
            var sql = @"SELECT * FROM Publisher WHERE URL = @url";

            return Query<Publisher>(ConnectionStrings.Jav, sql, new { url }).Count > 0;
        }

        public static int InsertPublisher(Publisher entity)
        {
            var sql = @"INSERT INTO Publisher (Name, URL, CreateTime) VALUES (@Name, @URL, GETDATE())";

            return Execute(ConnectionStrings.Jav, sql, entity);
        }

        public static bool HasActress(string url)
        {
            var sql = @"SELECT * FROM Actress WHERE URL = @url";

            return Query<Actress>(ConnectionStrings.Jav, sql, new { url }).Count > 0;
        }

        public static int InsertActress(Actress entity)
        {
            var sql = @"INSERT INTO Actress (Name, URL, CreateTime) VALUES (@Name, @URL, GETDATE())";

            return Execute(ConnectionStrings.Jav, sql, entity);
        }

        public static int UpdateScanURL(string url)
        {
            var sql = "update scanurl set isdownload = 1 where url = @url";
            return Execute(ConnectionStrings.Jav, sql, new { url });
        }

        public static int DeleteJavBusCategory()
        {
            var sql = @"TRUNCATE TABLE JavBusCategoryMapping";

            return Execute(ConnectionStrings.Jav, sql);
        }

        public static int InsertJavBusCategory(string javBusCategory, string javLibCategory)
        {
            var sql = @"INSERT INTO JavBusCategoryMapping (JavBusCategory, JavLibCategory) VALUES (@javBusCategory, @javLibCategory)";

            return Execute(ConnectionStrings.Jav, sql, new { javBusCategory, javLibCategory });
        }

        public static Dictionary<string, string> GetJavBusCategoryMapping()
        {
            var sql = @"SELECT JavBusCategory, JavLibCategory FROM JavBusCategoryMapping";

            return SqlHelper.ExecuteDataset(ConnectionStrings.Jav, CommandType.Text, sql).Tables[0].ToDictionary<string, string>("JavBusCategory", "JavLibCategory");
        }

        public static bool HasActressByName(string name)
        {
            var sql = @"SELECT * FROM Actress WHERE NAME = @name";

            return Query<Actress>(ConnectionStrings.Jav, sql, new { name }).Count > 0;
        }

        public static bool HasDirectorByName(string name)
        {
            var sql = @"SELECT * FROM Director WHERE NAME = @name";

            return Query<Director>(ConnectionStrings.Jav, sql, new { name }).Count > 0;
        }

        public static bool HasCompanyByName(string name)
        {
            var sql = @"SELECT * FROM Company WHERE NAME = @name";

            return Query<Company>(ConnectionStrings.Jav, sql, new { name }).Count > 0;
        }

        public static bool HasCategoryByName(string name)
        {
            var sql = @"SELECT * FROM Category WHERE NAME = @name";

            return Query<Category>(ConnectionStrings.Jav, sql, new { name }).Count > 0;
        }

        public static List<CommonModel> GetSimilarContent(string table, string content)
        {
            var sql = @"SELECT Name FROM " + table + " WHERE NAME LIKE ('%" + content + "%')";

            return Query<CommonModel>(ConnectionStrings.Jav, sql);
        }

        public static List<CommonModel> GetSimilarContent(string table)
        {
            var sql = @"SELECT Name FROM " + table;

            return Query<CommonModel>(ConnectionStrings.Jav, sql);
        }

        public static List<Actress> GetAllValidMap(string table)
        {
            var sql = @"SELECT Name, Url FROM " + table + " WHERE Url <> ''";

            return Query<Actress>(ConnectionStrings.Jav, sql);
        }
    }
}
