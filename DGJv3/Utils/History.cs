using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DGJv3
{
    class History
    {
        public History()
        {
            if (!Directory.Exists(Utilities.SongsHistoryDirectoryPath))
            {
                Directory.CreateDirectory(Utilities.SongsHistoryDirectoryPath);
            }
        }
        public void Write(string text)
        {
            try
            {
                CheckHistoryFile();
                using (StreamWriter sw =File.AppendText(Utilities.SongsHistoryFilePath))
                {
                    sw.WriteLine(text);
                }
            }
            catch (Exception e)
            {
                DGJMain.SELF.Log(e.Message);
            }
        }
        public void Write(SongInfo songInfo, string userName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("【");
            sb.Append(userName);
            sb.Append("|");
            sb.Append(songInfo.Module.ModuleName);
            sb.Append("|");
            sb.Append(songInfo.Id);
            sb.Append("】");
            sb.Append(songInfo.Name);
            sb.Append("---");
            sb.Append(songInfo.SingersText);
            Write(sb.ToString());
        }
        void CheckHistoryFile()
        {
            if (!File.Exists(Utilities.SongsHistoryFilePath))
            {
                Create();
            }
            else
            {
                string FileDate = File.GetLastWriteTime(Utilities.SongsHistoryFilePath).ToString("yyyy-MM-dd");
                if (FileDate != DateTime.Now.ToString("yyyy-MM-dd"))
                {
                    File.Move(Utilities.SongsHistoryFilePath, Path.Combine(Utilities.SongsHistoryDirectoryPath, FileDate + ".txt"));
                    Create();
                }
            }
        }
        void Create()
        {
            using (StreamWriter sw = File.CreateText(Utilities.SongsHistoryFilePath))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd"));
                sw.WriteLine("--------");
                sw.Flush();
            }
        }
    }
}
