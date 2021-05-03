using Model.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class EverythingHelper
    {
        private List<FileInfo> files = new List<FileInfo>();
        public static string Extensions = "ext:3g2;3gp;3gp2;3gpp;amr;amv;asf;avi;bdmv;bik;d2v;divx;drc;dsa;dsm;dss;dsv;evo;f4v;flc;fli;flic;flv;hdmov;ifo;ivf;m1v;m2p;m2t;m2ts;m2v;m4b;m4p;m4v;mkv;mp2v;mp4;mp4v;mpe;mpeg;mpg;mpls;mpv2;mpv4;mov;mts;ogm;ogv;pss;pva;qt;ram;ratdvd;rm;rmm;rmvb;roq;rpm;smil;smk;swf;tp;tpr;ts;vob;vp6;webm;wm;wmp;wmv";

        public List<FileInfo> SearchFile(string content, EverythingSearchEnum type)
        {
            switch (type)
            {
                case EverythingSearchEnum.Video:
                    content = "ext:3g2;3gp;3gp2;3gpp;amr;amv;asf;avi;bdmv;bik;d2v;divx;drc;dsa;dsm;dss;dsv;evo;f4v;flc;fli;flic;flv;hdmov;ifo;ivf;m1v;m2p;m2t;m2ts;m2v;m4b;m4p;m4v;mkv;mp2v;mp4;mp4v;mpe;mpeg;mpg;mpls;mpv2;mpv4;mov;mts;ogm;ogv;pss;pva;qt;ram;ratdvd;rm;rmm;rmvb;roq;rpm;smil;smk;swf;tp;tpr;ts;vob;vp6;webm;wm;wmp;wmv " + content;
                    break;
            }

            files = new List<FileInfo>();

            SearchEverythign(content);

            return files;
        }

        private void SearchEverythign(string strArg)
        {
            Process p = new Process();//建立外部调用线程
            p.StartInfo.FileName = @"E:\AVWeb\exe\es.exe";//要调用外部程序的绝对路径
            p.StartInfo.Arguments = strArg;
            p.StartInfo.UseShellExecute = false;//不使用操作系统外壳程序启动线程(一定为FALSE,详细的请看MSDN)
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = false;//不创建进程窗口
            p.OutputDataReceived += new DataReceivedEventHandler(Output);
            p.Start();//启动线程
            p.BeginOutputReadLine();//开始异步读取
            p.WaitForExit();//阻塞等待进程结束
            p.Close();//关闭进程
            p.Dispose();//释放资源
        }

        private void Output(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                if (File.Exists(output.Data))
                {
                    files.Add(new FileInfo(output.Data));
                }
            }
        }
    }

    public class EverythingResult
    {
        public long Size { get; internal set; }
        public string FullPath { get; internal set; }
        public DateTime? DateCreated { get; internal set; }
        public DateTime? DateAccessed { get; internal set; }
        public DateTime? DateModified { get; internal set; }
        public DateTime? DateRecentlyChanged { get; internal set; }
        public DateTime? DateRun { get; internal set; }
        public uint RunCount { get; internal set; }
        public uint Attributes { get; internal set; }
    }

    public abstract class EverythingBase
    {
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern int Everything_SetSearch(string lpSearchString);
        [DllImport("Everything64.dll")]
        public static extern void Everything_SetMatchPath(bool bEnable);
        [DllImport("Everything64.dll")]
        public static extern void Everything_SetMatchCase(bool bEnable);
        [DllImport("Everything64.dll")]
        public static extern void Everything_SetMatchWholeWord(bool bEnable);
        [DllImport("Everything64.dll")]
        public static extern void Everything_SetRegex(bool bEnable);
        [DllImport("Everything64.dll")]
        public static extern void Everything_SetMax(uint dwMax);
        [DllImport("Everything64.dll")]
        public static extern void Everything_SetOffset(uint dwOffset);

        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetMatchPath();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetMatchCase();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetMatchWholeWord();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetRegex();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetMax();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetOffset();
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern string Everything_GetSearch();
        [DllImport("Everything64.dll")]
        public static extern int Everything_GetLastError();

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern bool Everything_Query(bool bWait);

        [DllImport("Everything64.dll")]
        public static extern void Everything_SortResultsByPath();

        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetNumFileResults();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetNumFolderResults();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetNumResults();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetTotFileResults();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetTotFolderResults();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetTotResults();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_IsVolumeResult(uint nIndex);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_IsFolderResult(uint nIndex);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_IsFileResult(uint nIndex);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultPath(uint nIndex);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultFileName(uint nIndex);

        [DllImport("Everything64.dll")]
        public static extern void Everything_Reset();
        /// <summary>
        ///  Resets the result list and search state, freeing any allocated memory by the library.
        /// </summary>
        /// <remarks>
        /// You should call Everything_CleanUp to free any memory allocated by the Everything SDK.
        /// Calling <see cref="Everything_SetSearch"/> frees the old search and allocates the new search string.
        /// Calling <see cref="Everything_Query"/> frees the old result list and allocates the new result list.
        /// </remarks>
        [DllImport("Everything64.dll")]
        public static extern void Everything_CleanUp();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetMajorVersion();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetMinorVersion();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetRevision();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetBuildNumber();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_Exit();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_IsDBLoaded();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_IsAdmin();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_IsAppData();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_RebuildDB();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_UpdateAllFolderIndexes();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_SaveDB();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_SaveRunHistory();
        [DllImport("Everything64.dll")]
        public static extern bool Everything_DeleteRunHistory();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetTargetMachine();

        // Everything 1.4
        [DllImport("Everything64.dll")]
        public static extern void Everything_SetSort(uint dwSortType);
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetSort();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetResultListSort();
        [DllImport("Everything64.dll")]
        public static extern void Everything_SetRequestFlags(uint dwRequestFlags);
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetRequestFlags();
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetResultListRequestFlags();
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultExtension(uint nIndex);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetResultSize(uint nIndex, out long lpFileSize);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetResultDateCreated(uint nIndex, out long lpFileTime);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetResultDateModified(uint nIndex, out long lpFileTime);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetResultDateAccessed(uint nIndex, out long lpFileTime);
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetResultAttributes(uint nIndex);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultFileListFileName(uint nIndex);
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetResultRunCount(uint nIndex);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetResultDateRun(uint nIndex, out long lpFileTime);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_GetResultDateRecentlyChanged(uint nIndex, out long lpFileTime);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultHighlightedFileName(uint nIndex);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultHighlightedPath(uint nIndex);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultHighlightedFullPathAndFileName(uint nIndex);
        [DllImport("Everything64.dll")]
        public static extern uint Everything_GetRunCountFromFileName(string lpFileName);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_SetRunCountFromFileName(string lpFileName, uint dwRunCount);
        [DllImport("Everything64.dll")]
        public static extern uint Everything_IncRunCountFromFileName(string lpFileName);

        [DllImport("Everything64.dll")]
        public static extern bool Everything_IsFileInfoIndexed(uint fileInfoType);
    }

    public class Everything : EverythingBase, IDisposable
    {
        /// <summary>
        /// Performs a search for the specified query and sorts the results.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="maxResults">The maximum number of results to return. If less that 0, all results are returned.</param>
        /// <param name="sort">Sort order of the results.</param>
        /// <param name="requestFlags">The fields to return.</param>
        /// <exception cref="EverythingException">Thrown if the search is unsuccessful.</exception>
        /// <returns>The results of the search.</returns>
        public IEnumerable<EverythingResult> Search(string query, int maxResults = -1, int offset = -1, Sort sort = Sort.NameAscending, RequestFlags requestFlags = RequestFlags.FullPathAndFileName)
        {
            Everything_SetSearch(query);
            Everything_SetSort((uint)sort);
            Everything_SetRequestFlags((uint)requestFlags);
            if (maxResults > -1)
                Everything_SetMax((uint)maxResults);
            if (offset > -1)
                Everything_SetOffset((uint)offset);

            bool success = Everything_Query(true);
            if (!success)
            {
                Error errorCode = (Error)Everything_GetLastError();
                throw new EverythingException(errorCode, errorCode.GetDescription());
            }

            const int fileAndPathSize = 260;
            StringBuilder fileAndPathBuffer = new StringBuilder(fileAndPathSize);

            uint numResults = Everything_GetNumResults();
            for (uint index = 0; index < numResults; index++)
            {
                fileAndPathBuffer.Clear();
                Everything_GetResultFullPathName(index, fileAndPathBuffer, fileAndPathSize);

                Everything_GetResultSize(index, out long size);
                Everything_GetResultDateCreated(index, out long dateCreated);
                Everything_GetResultDateAccessed(index, out long dateAccessed);
                Everything_GetResultDateModified(index, out long dateModified);
                Everything_GetResultDateRecentlyChanged(index, out long dateRecentlyChanged);
                Everything_GetResultDateRun(index, out long dateRun);

                yield return new EverythingResult
                {
                    Size = size,
                    FullPath = fileAndPathBuffer.ToString(),
                    DateCreated = dateCreated > 0 ? DateTime.FromFileTime(dateCreated) : (DateTime?)null,
                    DateAccessed = dateAccessed > 0 ? DateTime.FromFileTime(dateAccessed) : (DateTime?)null,
                    DateModified = dateModified > 0 ? DateTime.FromFileTime(dateModified) : (DateTime?)null,
                    DateRecentlyChanged = dateRecentlyChanged > 0 ? DateTime.FromFileTime(dateRecentlyChanged) : (DateTime?)null,
                    DateRun = dateRun > 0 ? DateTime.FromFileTime(dateRun) : (DateTime?)null,
                    RunCount = Everything_GetResultRunCount(index),
                    Attributes = Everything_GetResultAttributes(index)
                };
            }
        }

        /// <summary>
        /// Increments the run counter for the specified result and returns the new run count.
        /// </summary>
        /// <param name="result">The search result to increase the run counter for.</param>
        /// <returns>The new run count.</returns>
        public uint IncrementRunCount(EverythingResult result)
        {
            return Everything_IncRunCountFromFileName(result.FullPath);
        }

        public void Dispose()
        {
            Everything_CleanUp();
        }
    }

    public class EverythingSearchOptions
    {
        private EverythingSearcher _searcher;

        internal string Query { get; set; }
        internal uint? MaxResults { get; set; }
        internal uint? Offset { get; set; }
        internal Sort Sort { get; set; }
        internal RequestFlags Flags { get; set; }

        internal EverythingSearchOptions(EverythingSearcher searcher)
        {
            _searcher = searcher;

            Sort = Sort.NameAscending;
            Flags = RequestFlags.FullPathAndFileName;
        }

        internal EverythingSearchOptions SetQuery(string query)
        {
            Query = query;
            return this;
        }

        public EverythingSearchOptions WithResultLimit(uint max)
        {
            MaxResults = max;
            return this;
        }

        public EverythingSearchOptions WithOffset(uint offset)
        {
            Offset = offset;
            return this;
        }

        public EverythingSearchOptions OrderBy(Sort sort)
        {
            Sort = sort;
            return this;
        }

        public EverythingSearchOptions GetFields(RequestFlags requestedFields)
        {
            Flags = requestedFields;
            return this;
        }

        public IEnumerable<EverythingEntry> Execute()
        {
            return _searcher.Execute(this);
        }
    }

    public class EverythingSearcher : EverythingBase, IDisposable
    {
        public EverythingSearchOptions SearchFor(string query)
        {
            return new EverythingSearchOptions(this).SetQuery(query);
        }

        internal IEnumerable<EverythingEntry> Execute(EverythingSearchOptions options)
        {
            /*
             * Set options
             */
            Everything_SetSearch(options.Query);
            Everything_SetSort((uint)options.Sort);
            Everything_SetRequestFlags((uint)options.Flags);
            if (options.Offset.HasValue)
                Everything_SetOffset(options.Offset.Value);
            if (options.MaxResults.HasValue)
                Everything_SetMax(options.MaxResults.Value);

            /*
             * Perform the search
             */
            bool success = Everything_Query(true);
            if (!success)
            {
                Error errorCode = (Error)Everything_GetLastError();
                throw new EverythingException(errorCode, errorCode.GetDescription());
            }

            /*
             * Get results
             */
            const int fileAndPathSize = 260;
            StringBuilder fileAndPathBuffer = new StringBuilder(fileAndPathSize);
            uint numResults = Everything_GetNumResults();
            bool areAttributesIndexed = Everything_IsFileInfoIndexed((uint)FileInfoType.Attributes);

            for (uint index = 0; index < numResults; index++)
            {
                fileAndPathBuffer.Clear();
                Everything_GetResultFullPathName(index, fileAndPathBuffer, fileAndPathSize);
                Everything_GetResultSize(index, out long size);
                Everything_GetResultDateCreated(index, out long dateCreated);
                Everything_GetResultDateAccessed(index, out long dateAccessed);
                Everything_GetResultDateModified(index, out long dateModified);
                Everything_GetResultDateRecentlyChanged(index, out long dateRecentlyChanged);
                Everything_GetResultDateRun(index, out long dateRun);

                yield return new EverythingEntry
                {
                    Size = size,
                    FullPath = fileAndPathBuffer.ToString(),
                    DateCreated = dateCreated > 0 ? DateTime.FromFileTime(dateCreated) : (DateTime?)null,
                    DateAccessed = dateAccessed > 0 ? DateTime.FromFileTime(dateAccessed) : (DateTime?)null,
                    DateModified = dateModified > 0 ? DateTime.FromFileTime(dateModified) : (DateTime?)null,
                    DateRecentlyChanged = dateRecentlyChanged > 0 ? DateTime.FromFileTime(dateRecentlyChanged) : (DateTime?)null,
                    DateRun = dateRun > 0 ? DateTime.FromFileTime(dateRun) : (DateTime?)null,
                    RunCount = Everything_GetResultRunCount(index),
                    Attributes = areAttributesIndexed ? Everything_GetResultAttributes(index) : (uint?)null,
                    Type = Everything_IsFileResult(index) ? EntryType.File : Everything_IsVolumeResult(index) ? EntryType.Volume : EntryType.Folder
                };
            }
        }

        /// <summary>
        /// Increments the run count for the specified result and returns the new run count.
        /// </summary>
        /// <param name="result">The search result to increase the run counter for.</param>
        /// <returns>The new run count.</returns>
        public uint IncrementRunCount(EverythingEntry result)
        {
            return Everything_IncRunCountFromFileName(result.FullPath);
        }

        /// <summary>
        /// Increments the run count for the specified path and returns the new run count.
        /// </summary>
        /// <returns>The new run count.</returns>
        public uint IncrementRunCount(string path)
        {
            return Everything_IncRunCountFromFileName(path);
        }

        /// <summary>
        /// Gets the run count for the given path.
        /// </summary>
        public uint GetRunCountForFile(string path)
        {
            return Everything_GetRunCountFromFileName(path);
        }

        public void Dispose()
        {
            Everything_CleanUp();
        }
    }

    public class EverythingEntry
    {
        public long Size { get; internal set; }
        public string FullPath { get; internal set; }
        public DateTime? DateCreated { get; internal set; }
        public DateTime? DateAccessed { get; internal set; }
        public DateTime? DateModified { get; internal set; }
        public DateTime? DateRecentlyChanged { get; internal set; }
        public DateTime? DateRun { get; internal set; }
        public uint RunCount { get; internal set; }
        public uint? Attributes { get; internal set; }
        public EntryType Type { get; internal set; }
    }

    public static class ErrorEnumExtensions
    {
        internal static string GetDescription(this Error error)
        {
            return error.GetType()
                .GetMember(error.ToString())
                .FirstOrDefault()
                ?.GetCustomAttribute<DescriptionAttribute>()?
                .Description;
        }
    }

    public class EverythingException : Exception
    {
        public Error ErrorCode { get; }

        public EverythingException(Error errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    public enum Sort
    {
        NameAscending = 1,
        NameDescending = 2,
        PathAscending = 3,
        PathDescending = 4,
        SizeAscending = 5,
        SizeDescending = 6,
        ExtensionAscending = 7,
        ExtensionDescending = 8,
        TypeNameAscending = 9,
        TypeNameDescending = 10,
        DateCreatedAscending = 11,
        DateCreatedDescending = 12,
        DateModifiedAscending = 13,
        DateModifiedDescending = 14,
        AttributesAscending = 15,
        AttributesDescending = 16,
        FileListFileNameAscending = 17,
        FileListFileNameDescending = 18,
        RunCountAscending = 19,
        RunCountDescending = 20,
        DateRecentlyChangedAscending = 21,
        DateRecentlyChangedDescending = 22,
        DateAccessedAscending = 23,
        DateAccessedDescending = 24,
        DateRunAscending = 25,
        DateRunDescending = 26
    }

    [Flags]
    public enum RequestFlags
    {
        [Obsolete]
        FileName = 0x1,
        [Obsolete]
        Path = 0x2,
        FullPathAndFileName = 0x4,
        [Obsolete]
        Extension = 0x8,
        Size = 0x10,
        DateCreated = 0x20,
        DateModified = 0x40,
        DateAccessed = 0x80,
        Attributes = 0x100,
        [Obsolete]
        FileListFileName = 0x200,
        RunCount = 0x400,
        DateRun = 0x800,
        DateRecentlyChanged = 0x1000,
        [Obsolete]
        HighlightedFileName = 0x2000,
        [Obsolete]
        HighlightedPath = 0x4000,
        [Obsolete]
        HighlightedFullPathAndFileName = 0x8000,
    }

    enum FileInfoType : uint
    {
        FileSize = 1,
        FFolderSize = 2,
        DateCreated = 3,
        DateModified = 4,
        DateAccessed = 5,
        Attributes = 6
    }

    public enum Error
    {
        Ok = 0,

        [Description("Failed to allocate memory for the search query.")]
        Memory = 1,

        [Description("IPC is not available. Make sure Everything is running.")]
        Ipc = 2,

        [Description("Failed to register the search query window class.")]
        RegisterClassEx = 3,

        [Description("Failed to create the search query window.")]
        CreateWindow = 4,

        [Description("Failed to create the search query thread.")]
        CreateThread = 5,

        [Description("Failed to register the search query window class.")]
        InvalidIndex = 6,

        [Description("Call Everything_SetReplyWindow before calling Everything_Query with bWait set to FALSE.")]
        InvalidCall = 7
    }

    public enum EntryType
    {
        File,
        Folder,
        Volume
    }
}
