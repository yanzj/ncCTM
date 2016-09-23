﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CTM.Core.Util
{
    public class ProcessHelper
    {
        /// <summary>
        /// 进程多重启动管理
        /// </summary>
        /// <param name="currentProcess"></param>
        public static void RepetitionStartManage(Process currentProcess)
        {
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);

            if (processes?.Length == 0) return;

            foreach (var p in processes)
            {
                if (p.Id != currentProcess.Id && (p.StartTime - currentProcess.StartTime).TotalMilliseconds <= 0)
                {
                    p.Kill();

                    p.WaitForExit();
                }
            }
        }

        ///   <summary>
        ///   启动外部应用程序
        ///   </summary>
        ///   <param   name="fileName">应用程序名称</param>
        ///   <param   name="workDirectory">应用程序工作目录</param>
        ///   <param   name="args">命令行参数</param>
        ///   <param   name="windowStyle">窗口风格</param>
        public static void StartExternalProgram(string fileName, string workDirectory, string args, ProcessWindowStyle windowStyle)
        {
            try
            {
                Process myprocess = new Process();
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = fileName;
                startInfo.Arguments = args;
                startInfo.WindowStyle = windowStyle;
                startInfo.WorkingDirectory = workDirectory;
                startInfo.UseShellExecute = true;

                myprocess.StartInfo = startInfo;
                myprocess.Start();
                RepetitionStartManage(myprocess);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}