using DataBaseManager.Common;
using Model.FindModels;
using Model.JavModels;
using Model.OneOneFive;
using Model.ScanModels;
using Model.WebModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Utils;

namespace DataBaseManager.ScanDataBaseHelper
{
    public class ScanDataBaseManager : DapperHelper
    {
        public static int ClearMatch()
        {
            var sql = @"TRUNCATE TABLE Match";

            return Execute(ConnectionStrings.Scan, sql);
        }

        public static List<Match> GetAllMatch()
        {
            var sql = @"SELECT * FROM Match";

            return Query<Match>(ConnectionStrings.Scan, sql);
        }

        public static int SaveMatch(Match match)
        {
            var sql = @"INSERT INTO Match (AvID, MatchAVId, Name, Location, CreateTime, AvName) VALUES (@AvId, @MatchAVId, @Name, @Location, GETDATE(), @AvName)";

            return Execute(ConnectionStrings.Scan, sql, match);
        }

        public static int InsertViewHistory(string file)
        {
            var sql = "INSERT INTO ViewHistory (FileName) VALUES (@file)";

            return Execute(ConnectionStrings.Scan, sql, new { file });
        }

        public static int RemoveViewHistory(string file)
        {
            var sql = "DELETE FROM ViewHistory WHERE FileName = @file";

            return Execute(ConnectionStrings.Scan, sql, new { file });
        }

        public static bool ViewedFile(string file)
        {
            var sql = "SELECT TOP 1 * FROM ViewHistory WHERE FileName = @file";

            return QuerySingle<ViewHistory>(ConnectionStrings.Scan, sql, new { file }) != null;
        }

        public static List<PrefixModel> GetPrefix()
        {
            var sql = "SELECT * FROM Prefix";

            return Query<PrefixModel>(ConnectionStrings.Scan, sql);
        }

        public static int DeleteMagUrlById(string avid)
        {
            var sql = "DELETE FROM MagUrl WHERE AvId = @avid";

            return Execute(ConnectionStrings.Scan, sql, new { avid });
        }

        public static int InsertMagUrl(string avid, string magUrl, string magTitle, int isFound)
        {
            var sql = "INSERT INTO MagUrl (AvId, MagUrl, MagTitle, IsFound, CreateTime) VALUES (@avid, @magUrl, @magTitle, @isFoung, GETDATE())";

            return Execute(ConnectionStrings.Scan, sql, new { avid, magUrl, magTitle, isFound});
        }

        public static List<ScanResult> GetMatchScanResult()
        {
            var sql = @"SELECT m.MatchId AS Id, CASE WHEN m.Location IS NULL THEN a.AVID ELSE m.MatchAVId END AS MatchAvId, m.Location, m.Name AS FileName, a.PictureURL AS PicUrl, a.ID AS AvId, a.Company, a.Name AS AvName, a.Director, a.Publisher, a.Category, a.Actress, a.ReleaseDate FROM ScanAllAv.dbo.Match m RIGHT JOIN JavLibraryDownload.dbo.AV a ON m.AvID = a.ID";

            return Query<ScanResult>(ConnectionStrings.Scan, sql);
        }

        public static ScanResult GetMatchScanResult(int avId)
        {
            var sql = @"SELECT TOP 1 m.MatchId AS Id, m.Location, m.Name AS FileName, a.PictureURL AS PicUrl, a.ID AS AvId, a.Company, a.Name AS AvName, a.Director, a.Publisher, a.Category, a.Actress, a.ReleaseDate FROM ScanAllAv.dbo.Match m LEFT JOIN JavLibraryDownload.dbo.AV a ON m.AvID = a.ID WHERE m.MatchAvID = @avId";

            return QuerySingle<ScanResult>(ConnectionStrings.Scan, sql, new { avId });
        }

        public static int DeleteFaviScan()
        {
            var sql = "TRUNCATE TABLE FaviScan";

            return Execute(ConnectionStrings.Scan, sql);
        }

        public static int InsertFaviScan(FaviScan favi)
        {
            var sql = @"IF NOT EXISTS (SELECT * FROM FaviScan WHERE [Url] = @Url)
                            INSERT INTO FaviScan (Category, Url, Name) VALUES (@Category, @Url, @Name)";

            return Execute(ConnectionStrings.Scan, sql, favi);
        }

        public static List<FaviScan> GetFaviScan()
        {
            var sql = "SELECT * FROM FaviScan ORDER BY Category";

            return Query<FaviScan>(ConnectionStrings.Scan, sql);
        }

        public static AV GetMatchedAv(int id)
        {
            var sql = "SELECT TOP 1 av.* FROM JavLibraryDownload.dbo.AV av JOIN ScanAllAv.dbo.[Match] m ON av.AVID = m.MatchAVId WHERE m.MatchAVID = @id";

            return QuerySingle<AV>(ConnectionStrings.Scan, sql, new { id });
        }

        public static int InsertRemoteScanMag(RemoteScanMag entity)
        {
            var sql = @"INSERT INTO RemoteScanMag (AvId, AvUrl, AvName, MagTitle, MagUrl, MagSize, SearchStatus, MatchFile, CreateTime, MagDate, ScanJobId)
                            VALUES (@AvId, @AvUrl, @AvName, @MagTitle, @MagUrl, @MagSize, @SearchStatus, @MatchFile, GETDATE(), @MagDate, @JobId)";

            return Execute(ConnectionStrings.Scan, sql, entity);
        }

        public static TokenModel GetToken()
        {
            var sql = "SELECT TOP 1 * FROM Token";

            return QuerySingle<TokenModel>(ConnectionStrings.Scan, sql);
        }

        public static int InsertScanJob(string scanJobName, string scanParameter, string website)
        {
            var sql = "INSERT INTO ScanJob (ScanJobName, ScanParameter, CreateTime, EndTime, IsFinish, Website) VALUES (@scanJobName, @scanParameter, GETDATE(), GETDATE(), 0, @Website) SELECT @@IDENTITY";

            return QuerySingle<int>(ConnectionStrings.Scan, sql, new { scanJobName, scanParameter, website });
        }

        public static int DeleteRemoteScanMag()
        {
            var sql = "DELETE FROM RemoteScanMag WHERE ScanJobId = 0";

            return Execute(ConnectionStrings.Scan, sql);
        }

        public static ScanJob GetFirstScanJob()
        {
            var sql = "SELECT TOP 1 * FROM ScanJob WHERE IsFinish = 0 ORDER BY CreateTime ASC";

            return QuerySingle<ScanJob>(ConnectionStrings.Scan, sql);
        }

        public static List<ScanJob> GetScanJob(int count)
        {
            var sql = "SELECT TOP (@count) * FROM ScanJob ORDER BY EndTime DESC";

            return Query<ScanJob>(ConnectionStrings.Scan, sql, new { count });
        }

        public static int GetScanJobItem(int scanJobId)
        {
            var sql = "SELECT COUNT(DISTINCT(AvId)) FROM RemoteScanMag WHERE ScanJobID = @scanJobId";

            return QuerySingle<int>(ConnectionStrings.Scan, sql, new { scanJobId });
        }

        public static int DeleteScanJob(int jobId)
        {
            var sql = "DELETE FROM ScanJob WHERE ScanJobId = @jobId";

            return Execute(ConnectionStrings.Scan, sql, new { jobId });
        }

        public static int DeleteRemoteMagScan(int jobId)
        {
            var sql = "DELETE FROM RemoteScanMag WHERE ScanJobId = @jobId";

            return Execute(ConnectionStrings.Scan, sql, new { jobId });
        }

        public static int SetScanJobFinish(int scanJobId, int status, int totalItem = 0)
        {
            var sql = "UPDATE ScanJob SET IsFinish = @status, TotalItem = @totalItem, EndTime = GETDATE() WHERE ScanJobId = @scanJobId";

            return QuerySingle<int>(ConnectionStrings.Scan, sql, new { scanJobId, status, totalItem });
        }

        public static List<RemoteScanMag> GetAllMagByJob(int jobId)
        {
            var sql = "SELECT * FROM RemoteScanMag WHERE ScanJobId = @jobId";

            return Query<RemoteScanMag>(ConnectionStrings.Scan, sql, new { jobId });
        }

        public static int InsertOneOneFiveCookie(OneOneFiveCookieModel entity)
        {
            var sql = "INSERT INTO OneOneFiveCookie (OneOneFiveCookie) VALUES (@OneOneFiveCookie)";

            return Execute(ConnectionStrings.Scan, sql, entity);
        }

        public static int TruncateOneOneFiveCookie()
        {
            var sql = "TRUNCATE TABLE OneOneFiveCookie";

            return Execute(ConnectionStrings.Scan, sql);
        }

        public static OneOneFiveCookieModel GetOneOneFiveCookie()
        {
            var sql = "SELECT TOP 1 * FROM OneOneFiveCookie ORDER BY OneOneFiveCookieId DESC";

            return QuerySingle<OneOneFiveCookieModel>(ConnectionStrings.Scan, sql);
        }

        public static bool IsUser(string name, string pass)
        {
            var sql = string.Format("SELECT * FROM UserInfo WHERE UserName = @name AND UserPassword = @pass", name, pass);

            return QuerySingle<UserInfo>(ConnectionStrings.Scan, sql, new { name, pass }) != null;
        }

        public static int DeleteReportItem()
        {
            var sql = "TRUNCATE TABLE ReportItem";

            return Execute(ConnectionStrings.Scan, sql);
        }

        public static int BatchInserReportItem(List<ReportItem> items)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ReportItemId", typeof(int));
            dt.Columns.Add("ReportType", typeof(int));
            dt.Columns.Add("ItemName", typeof(string));
            dt.Columns.Add("ExistCount", typeof(int));
            dt.Columns.Add("TotalCount", typeof(int));
            dt.Columns.Add("TotalSize", typeof(double));
            dt.Columns.Add("ReportId", typeof(int));

            foreach(var item in items)
            {
                dt.Rows.Add(null, (int)item.ReportType, item.ItemName, item.ExistCount, item.TotalCount, item.TotalSize, item.ReportId);
            }

            using (SqlConnection conn = new SqlConnection(ConnectionStrings.Scan))
            {
                SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
                bulkCopy.DestinationTableName = "ReportItem";
                bulkCopy.BatchSize = dt.Rows.Count;
                conn.Open();
                if (dt != null && dt.Rows.Count != 0)
                {
                    bulkCopy.WriteToServer(dt);
                }
            }

            return items.Count;
        }

        public static int InsertReport(Report entity)
        {
            var sql = @"INSERT INTO Report (ReportDate,TotalCount,TotalExist,TotalExistSize,LessThenOneGiga,OneGigaToTwo,TwoGigaToFour,FourGigaToSix,GreaterThenSixGiga,Extension,H265Count,ChineseCount,IsFinish,EndDate) 
                            VALUES (GETDATE(), @TotalCount, @TotalExist, @TotalExistSize, @LessThenOneGiga, @OneGigaToTwo, @TwoGigaToFour, @FourGigaToSix, @GreaterThenSixGiga, @ExtensionJson, @H265Count, @ChineseCount, @IsFinish, GETDATE()) SELECT @@IDENTITY";

            return QuerySingle<int>(ConnectionStrings.Scan, sql, entity);
        }

        public static int UpdateReport(Report entity)
        {
            var sql = "UPDATE Report SET TotalExist = @TotalExist, TotalExistSize = @TotalExistSize, LessThenOneGiga = @LessThenOneGiga, OneGigaToTwo = @OneGigaToTwo, TwoGigaToFour = @TwoGigaToFour, FourGigaToSix = @FourGigaToSix, GreaterThenSixGiga = @GreaterThenSixGiga, Extension = @ExtensionJson, H265Count = @H265Count, ChineseCount = @ChineseCount WHERE ReportId = @ReportId";

            return Execute(ConnectionStrings.Scan, sql, entity);
        }

        public static int UpdateReportFinish(int id)
        {
            var sql = "UPDATE Report SET IsFinish = 1, EndDate = GETDATE() WHERE ReportID = @id";

            return Execute(ConnectionStrings.Scan, sql, new { id });
        }

        public static Report GetReport()
        {
            var sql = "SELECT TOP 1 * FROM Report WHERE IsFinish = 1 ORDER BY EndDate DESC";

            return QuerySingle<Report>(ConnectionStrings.Scan, sql);
        }

        public static List<ReportItem> ReportItem(int reportId)
        {
            var sql = "SELECT * FROM ReportItem WHERE ReportId = @reportId";

            return Query<ReportItem>(ConnectionStrings.Scan, sql, new { reportId });
        }

        public static int InsertWishList(WishList entity)
        {
            var sql = @"IF NOT EXISTS (SELECT * FROM WishList WHERE IPAddress = @IPAddress AND FilePath = @FilePath)
                            INSERT INTO WishList (IPAddress, Id, AvId, FilePath) VALUES (@IPAddress, @Id, @AvId, @FilePath)";

            return Execute(ConnectionStrings.Scan, sql, entity);
        }

        public static int InserWebViewLog(WebViewLog entity)
        {
            var sql = @"INSERT INTO WebViewLog (IPAddress, Controller, [Action], Parameter, UserAgent, IsLogin, CreateTime) 
                        VALUES (@IPAddress, @Controller, @Action, @Parameter, @UserAgent, @IsLogin, GETDATE())";

            return Execute(ConnectionStrings.Scan, sql, entity);
        }

        public static List<WishList> GetWishList(string where)
        {
            var sql = string.Format("SELECT * FROM WishList WHERE 1=1");

            return Query<WishList>(ConnectionStrings.Scan, sql, new { where });
        }

        public static List<WebViewLog> GetWebViewLog(string where)
        {
            var sql = string.Format("SELECT * FROM WebViewLog WHERE 1=1");

            return Query<WebViewLog>(ConnectionStrings.Scan, sql, new { where });
        }

        public static int UpdateFaviAvator(string name, string avator)
        {
            var sql = "UPDATE FaviScan SET Avator = @avator WHERE category = 'actress' AND Name = @name";

            return Execute(ConnectionStrings.Scan, sql, new { name, avator });
        }

        public static List<LocalShaMapping> GetAllLocalShaMapping()
        {
            var sql = "SELECT * FROM LocalShaMapping";

            return Query<LocalShaMapping>(ConnectionStrings.Scan, sql);
        }

        public static int InserOneOneFiveFileShaMapping(OneOneFiveFileShaMapping entity)
        {
            var sql = @"IF NOT EXISTS (SELECT * FROM OneOneFiveFileShaMapping WHERE Sha = @Sha)
                            INSERT INTO OneOneFiveFileShaMapping (FileName, Sha, FileSize, UpdateTime, IsOnLocal) VALUES (@FileName, @Sha, @FileSize, GETDATE(), @IsOnLocal)";

            return Execute(ConnectionStrings.Scan, sql, entity);
        }

        public static List<OneOneFiveFileShaMapping> GetOneOneFiveShaMapping(string content)
        {
            var sql = "SELECT * FROM OneOneFiveFilesShaMapping WHERE FileName LIKE @content";

            return Query<OneOneFiveFileShaMapping>(ConnectionStrings.Scan, sql, new { content });
        }

        public static int UpdateOneOneFiveFileShaMapping(string sha, bool isOnLocal)
        {
            var sql = "UPDATE OneOneFiveFileShaMapping SET IsOnLocal = @isOnLocal WHERE Sha = @sha";

            return Execute(ConnectionStrings.Scan, sql, new { isOnLocal, sha });
        }

        public static int TruncateOneOneFiveFileShaMapping()
        {
            var sql = "TRUNCATE TABLE OneOneFiveFileShaMapping";

            return Execute(ConnectionStrings.Scan, sql);
        }

        public static int TruncateLocalShaMapping()
        {
            var sql = "TRUNCATE TABLE LocalShaMapping";

            return Execute(ConnectionStrings.Scan, sql);
        }

        public static int InsertLocalShaMapping(LocalShaMapping entity)
        {
            var sql = "INSERT INTO LocalShaMapping (FilePath, Sha1, FileFolder, UpdateTime, FileSize) VALUES (@FilePath, @Sha1, @FileFolder, GETDATE(), @FileSize)";

            return Execute(ConnectionStrings.Scan, sql, entity);
        }
    }
}
