using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper_1080.Helper
{
    public static class local
    {
        public static void ProgressRun(string exeFileName,string cmdStr, bool showWindow = false)
        {
            var process = new Process();

            process.StartInfo.FileName = exeFileName;
            process.StartInfo.Arguments = cmdStr;
            process.StartInfo.UseShellExecute = false;
            if (!showWindow)
            {
                process.StartInfo.CreateNoWindow = true;
            }

            process.Start();

            process.WaitForExit();
        }
    }
}
