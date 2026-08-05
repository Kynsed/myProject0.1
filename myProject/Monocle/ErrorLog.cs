using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Monocle
{
    public static class ErrorLog
    {
        public const string Filename = "error_log.txt";
        public const string Marker = "==========================================";

        public static void Write(Exception e)
        {
            Write(e.ToString());
        }

        public static void Write(string str)
        {
            StringBuilder builder = new StringBuilder();

            // make sure the directory exists
            if (Path.IsPathRooted(Filename))
            {
                string dir = Path.GetDirectoryName(Filename);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }

            // get the previous contents
            string content = "";
            if (File.Exists(Filename))
            {
                StreamReader reader = new StreamReader(Filename);
                content = reader.ReadToEnd();
                reader.Close();

                if (!content.Contains(Marker))
                    content = "";
            }

            // header
            if (Engine.Instance != null)
                builder.Append(Engine.Instance.Title);
            else
                builder.Append("Monocle Engine");
            builder.AppendLine(" Error Log");
            builder.AppendLine(Marker);
            builder.AppendLine();

            // version
            if (Engine.Instance != null && Engine.Instance.Version != null)
            {
                builder.Append("Ver ");
                builder.AppendLine(Engine.Instance.Version.ToString());
            }

            // datetime and the error
            builder.AppendLine(DateTime.Now.ToString());
            builder.AppendLine(str);

            // append the previous log
            if (content != "")
            {
                int at = content.IndexOf(Marker) + Marker.Length;
                string after = content.Substring(at);
                builder.AppendLine(after);
            }

            // write it out
            StreamWriter writer = new StreamWriter(Filename, false);
            writer.Write(builder.ToString());
            writer.Close();
        }

        public static void Open()
        {
            if (File.Exists(Filename))
                Process.Start(Filename);
        }
    }
}
