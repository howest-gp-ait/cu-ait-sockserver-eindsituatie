using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Data;
using System.IO;

namespace SockServerLib
{
    public class Helper
    {
        public static List<string> GetActiveIP4s()
        {
            List<string> activeIps = new List<string>();
            activeIps.Add("127.0.0.1");
            string x = Dns.GetHostName();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    activeIps.Add(ip.ToString());
                }
            }
            return activeIps;
        }

        public static DataTable ReadConfigFile()
        {
            string XMLBestand = Directory.GetCurrentDirectory() + "/config.xml";
            if(!File.Exists(XMLBestand))
            {
                MakeConfigFile();
            }
            DataSet ds = new DataSet();
            ds.ReadXml(XMLBestand, XmlReadMode.ReadSchema);
            return ds.Tables[0];
        }
        public static void UpdateConfigFile(string ipNumber, int portNumber, string workingFolder)
        {
            string XMLBestand = Directory.GetCurrentDirectory() + "/config.xml";
            if (!File.Exists(XMLBestand))
            {
                MakeConfigFile();
            }
            DataSet ds = new DataSet();
            ds.ReadXml(XMLBestand, XmlReadMode.ReadSchema);
            ds.Tables[0].Rows[0][0] = ipNumber;
            ds.Tables[0].Rows[0][1] = portNumber;
            ds.Tables[0].Rows[0][2] = workingFolder;
            ds.WriteXml(XMLBestand, XmlWriteMode.WriteSchema);
        }
        private static void MakeConfigFile()
        {
            DataSet ds = new DataSet();
            DataTable dt = ds.Tables.Add();
            DataColumn dc;
            dc = new DataColumn();
            dc.ColumnName = "IP";
            dc.DataType = typeof(string);
            dt.Columns.Add(dc);
            dc = new DataColumn();
            dc.ColumnName = "Port";
            dc.DataType = typeof(int);
            dt.Columns.Add(dc);
            dc = new DataColumn();
            dc.ColumnName = "Folder";
            dc.DataType = typeof(string);
            dt.Columns.Add(dc);
            DataRow dr = dt.NewRow();
            dr[0] = "127.0.0.1";
            dr[1] = 49200;
            dr[2] = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dt.Rows.Add(dr);
            string  XMLBestand = Directory.GetCurrentDirectory() + "/config.xml";
            ds.WriteXml(XMLBestand, XmlWriteMode.WriteSchema);
        }
    }
}
