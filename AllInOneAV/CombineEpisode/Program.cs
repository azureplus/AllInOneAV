﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CombineEpisode
{
    static class Program
    {
        /// <summary>
        /// 是否退出应用程序
        /// </summary>
        static bool glExitApp = false;


        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            //处理UI线程异常
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            //处理非线程异常
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());

            glExitApp = true;//标志应用程序可以退出
        }

        /// <summary>
         /// 处理未捕获异常
         /// </summary>
         /// <param name="sender"></param>
         /// <param name="e"></param>
         private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
         {
 
             while (true)
             {//循环处理，否则应用程序将会退出
                 if (glExitApp)
                 {//标志应用程序可以退出，否则程序退出后，进程仍然在运行
                     return;
                 }
             };
         }
 
         /// <summary>
         /// 处理UI主线程异常
         /// </summary>
         /// <param name="sender"></param>
         /// <param name="e"></param>
         private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
         {

         }
    }
}
