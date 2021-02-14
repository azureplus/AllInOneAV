using DataBaseManager.Common;
using Model.JavModels;
using Model.MangaModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace DataBaseManager.MangaDataBaseHelper
{
    public class MangaDatabaseHelper : DapperHelper
    {
        public static int InsertMangaCategory(MangaCategory entity)
        {
            var sql = "INSERT INTO MangaCategory (SourceType, RootCategory, Category, Url) VALUES (@SourceType, @RootCategory, @Category, @Url )";

            return Execute(ConnectionStrings.Manga, sql, entity);
        }

        public static List<MangaCategory> GetMangaCategoryByType(MangaCategorySourceType type)
        {
            var sql = "SELECT RootCategory, Category, Url FROM MangaCategory WHERE SourceType = @type";

            return Query<MangaCategory>(ConnectionStrings.Manga, sql, new { type });
        }
    }
}
