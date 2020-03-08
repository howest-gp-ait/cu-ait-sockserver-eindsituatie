using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using SockServerLib;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Windows.Threading;

namespace SockServerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket listener;
        int numberOfClients = 1;
        bool verderdoen = true;
        string activeFolder;
        string baseFolder;

        public MainWindow()
        {
            InitializeComponent();
            DoStartup();
            FillDictionary();
            btnStart.Visibility = Visibility.Visible;
            btnStop.Visibility = Visibility.Hidden;
        }
        public static void DoEvents()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
        private void DoStartup()
        {
            cmbIPs.ItemsSource = Helper.GetActiveIP4s();
            DataTable dt = Helper.ReadConfigFile();
            txtPort.Text = dt.Rows[0]["Port"].ToString();
            lblWorkingFolder.Content = dt.Rows[0]["Folder"].ToString();
            activeFolder = dt.Rows[0]["Folder"].ToString();
            baseFolder = dt.Rows[0]["Folder"].ToString();
            try
            {
                cmbIPs.SelectedItem = dt.Rows[0]["IP"].ToString();
            }
            catch
            {
                cmbIPs.SelectedItem = "127.0.0.1";
            }
        }

        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            string ip = cmbIPs.SelectedItem.ToString();
            string workingFolder = lblWorkingFolder.Content.ToString();
            activeFolder = workingFolder;
            baseFolder = workingFolder;

            int poort = 0;
            int.TryParse(txtPort.Text, out poort);
            if (poort == 0) poort = 49200;
            if (poort < 49152) poort = 49200;
            if (poort > 65535) poort = 49200;
            txtPort.Text = poort.ToString();
            Helper.UpdateConfigFile(ip, poort, workingFolder);
            System.Windows.MessageBox.Show("Configuration saved", "Configuration saved to config.xml", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSelectWorkingFolder_Click(object sender, RoutedEventArgs e)
        {
            // referentie naar System.Windows.Forms nodig voor onderstaande functionaliteit !
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.SelectedPath = lblWorkingFolder.Content.ToString();
            System.Windows.Forms.DialogResult result = fbd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                lblWorkingFolder.Content = fbd.SelectedPath;
                activeFolder = fbd.SelectedPath;
                baseFolder = fbd.SelectedPath;
            }
        }

        private void FillDictionary()
        {
            tbkDictionary.Text = "";
            tbkDictionary.Text += "DIRALL" + Environment.NewLine;
            tbkDictionary.Text += "DIRFILES" + Environment.NewLine;
            tbkDictionary.Text += "DIRFOLDERS" + Environment.NewLine;
            tbkDictionary.Text += "CURRENTDIR" + Environment.NewLine;
            tbkDictionary.Text += "CHANGEDIR <existing foldername>" + Environment.NewLine;
            tbkDictionary.Text += "CHANGEDIR UP" + Environment.NewLine;
            tbkDictionary.Text += "CHANGEDIR ROOT" + Environment.NewLine;
            tbkDictionary.Text += "MAKEDIR <non existing foldername>" + Environment.NewLine;
            tbkDictionary.Text += "REMOVEDIR <remove existing empty folder>" + Environment.NewLine;
            tbkDictionary.Text += "REMOVEDIRALL <remove existing folder>" + Environment.NewLine;
            tbkDictionary.Text += "RENAMEDIR <existing folder>,<new name>" + Environment.NewLine;
            tbkDictionary.Text += "CONTENTFILE <existing file>" + Environment.NewLine;
            tbkDictionary.Text += "REMOVEFILE <remove existing file>" + Environment.NewLine;
            tbkDictionary.Text += "RENAMEFILE <rename existing file>,<new name>" + Environment.NewLine;

        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.Visibility = Visibility.Hidden;
            btnStop.Visibility = Visibility.Visible;
            grpConfig.IsEnabled = false;
            tbkInfo.Text = "";
            activeFolder = baseFolder;
            ExecuteServer();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            btnStart.Visibility = Visibility.Visible;
            btnStop.Visibility = Visibility.Hidden;
            grpConfig.IsEnabled = true;
            verderdoen = false;
            try
            {
                listener.Close();
            }
            catch
            { }
            listener = null;
            tbkInfo.Text = $"Socket stopped at : {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} \n" + tbkInfo.Text;

        }
        public void ExecuteServer()
        {
            IPAddress ipAddr = IPAddress.Parse(cmbIPs.SelectedItem.ToString());
            tbkInfo.Text = $"I will listen to IP : {ipAddr.ToString()} \n" + tbkInfo.Text;
            IPEndPoint serverEndPoint = new IPEndPoint(ipAddr, int.Parse(txtPort.Text));
            tbkInfo.Text = $"My endpoint will be : {serverEndPoint.ToString()} \n" + tbkInfo.Text;
            listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(serverEndPoint);
                listener.Listen(numberOfClients);
                tbkInfo.Text = $"Socket started at : {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} \n" + tbkInfo.Text;
                tbkInfo.Text = $"My maximum capacity : {numberOfClients} \n" + tbkInfo.Text;

                verderdoen = true;
                while (verderdoen)
                {
                    DoEvents();
                    if (listener.Poll(1000000, SelectMode.SelectRead))
                    {
                        Socket clientSocket = listener.Accept();

                        byte[] clientRequest = new Byte[1024];
                        string instructie = null;

                        while (true)
                        {

                            int numByte = clientSocket.Receive(clientRequest);
                            instructie += Encoding.ASCII.GetString(clientRequest, 0, numByte);
                            if (instructie.IndexOf("##EOM") > -1)
                                break;
                        }
                        tbkInfo.Text = $"\n=================================\n" + tbkInfo.Text;
                        tbkInfo.Text = $"request = {instructie}\n" + tbkInfo.Text;
                        instructie = instructie.ToUpper();
                        instructie = instructie.Replace("##EOM", "").Trim();
                        byte[] clientResponse = Encoding.ASCII.GetBytes("");
                        string result;
                        if (instructie.Length < 5)
                        {
                            result = $"{instructie} is a unknown instruction\n";
                        }
                        else
                        {
                            result = $"{ExecuteCommand(instructie)}\n";
                        }
                        clientResponse = Encoding.ASCII.GetBytes(result);

                        clientSocket.Send(clientResponse);
                        clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                        tbkInfo.Text = $"response = {result}" + tbkInfo.Text;
                    }
                }
            }
            catch (Exception e)
            {
                if (verderdoen)
                {
                    tbkInfo.Text = $"Error : {e.Message} \n" + tbkInfo.Text;
                }
            }
        }

        private string ExecuteCommand(string command)
        {
            if (command == "HELLO")
            {
                return "<cf>" + activeFolder;
            }
            else if (command == "GOODBYE")
            {
                activeFolder = baseFolder;
                return "HAVE A NICE DAY";
            }
            else if (command == "DIRALL")
            {
                return DIRALL();
            }
            else if (command == "DIRFILES")
            {
                return DIRFILES();
            }
            else if (command == "DIRFOLDERS")
            {
                return DIRFOLDERS();
            }
            else if (command == "CURRENTDIR")
            {
                return CURRENTDIR();
            }
            else if (command.IndexOf("CHANGEDIR") > -1)
            {
                if (command == "CHANGEDIR UP")
                {
                    return CHANGEDIR_UP();
                }
                else if (command == "CHANGEDIR ROOT")
                {
                    return CHANGEDIR_ROOT();
                }
                else
                {
                    string[] delen = command.Split('|');
                    return CHANGEDIR(delen[1]);
                }
            }
            else if (command.IndexOf("MAKEDIR") > -1)
            {
                string[] delen = command.Split('|');
                return MAKEDIR(delen[1]);
            }
            else if (command.IndexOf("REMOVEDIR|") > -1)
            {
                string[] delen = command.Split('|');
                return REMOVEDIR(delen[1]);
            }
            else if (command.IndexOf("REMOVEDIRALL") > -1)
            {
                string[] delen = command.Split('|');
                return REMOVEDIRALL(delen[1]);
            }
            else if (command.IndexOf("RENAMEDIR") > -1)
            {
                string[] delen = command.Split('|');
                return RENAMEDIR(delen[1], delen[2]);
            }
            else if (command.IndexOf("REMOVEFILE|") > -1)
            {
                string[] delen = command.Split('|');
                return REMOVEFILE(delen[1]);
            }
            else if (command.IndexOf("RENAMEFILE") > -1)
            {
                string[] delen = command.Split('|');
                return RENAMEFILE(delen[1], delen[2]);
            }
            else if (command.IndexOf("CONTENTFILE") > -1)
            {
                string[] delen = command.Split('|');
                return CONTENTFILE(delen[1]);
            }
            else
            {
                return "UNKNOWN INSTRUCTION";
            }
        }

        private string DIRALL()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Subfolders & files in current folder : \n");
            DirectoryInfo basedir = new DirectoryInfo(activeFolder);
            foreach (DirectoryInfo di in basedir.GetDirectories())
            {
                sb.AppendLine($"<dir>\t{di.Name}");
            }
            foreach (FileInfo fi in basedir.GetFiles())
            {
                sb.AppendLine($"\t{fi.Name}");
            }
            return sb.ToString();
        }
        private string DIRFILES()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Files in current folder : \n");
            DirectoryInfo basedir = new DirectoryInfo(activeFolder);
            foreach (FileInfo fi in basedir.GetFiles())
            {
                sb.AppendLine($"\t{fi.Name}");
            }
            return sb.ToString();
        }
        private string DIRFOLDERS()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Subfolders in current folder : \n");

            DirectoryInfo basedir = new DirectoryInfo(activeFolder);
            foreach (DirectoryInfo di in basedir.GetDirectories())
            {
                sb.AppendLine($"<dir>\t{di.Name}");
            }
            return sb.ToString();
        }
        private string CURRENTDIR()
        {
            return activeFolder;
        }
        private string CHANGEDIR(string subfolder)
        {
            if (Directory.Exists(activeFolder + "\\" + subfolder))
            {
                activeFolder = activeFolder + "\\" + subfolder;
                return "<cf>" + activeFolder;
            }
            else
                return $"Error : folder unchanged\nCurrent folder is still : {activeFolder}";
        }
        private string CHANGEDIR_UP()
        {
            if (activeFolder == baseFolder)
            {
                return "<cf>" + activeFolder;
            }
            DirectoryInfo di = new DirectoryInfo(activeFolder);
            activeFolder = di.Parent.FullName;
            return "<cf>" + activeFolder;

        }
        private string CHANGEDIR_ROOT()
        {
            activeFolder = baseFolder;
            return "<cf>" + activeFolder;

        }
        private string MAKEDIR(string subfolder)
        {
            if (!Directory.Exists(activeFolder + "\\" + subfolder))
            {
                try
                {
                    Directory.CreateDirectory(activeFolder + "\\" + subfolder);
                }
                catch (Exception fout)
                {
                    return $"Error : {fout.Message}\nCurrent folder is still : {activeFolder}";

                }
                activeFolder = activeFolder + "\\" + subfolder;
                return "<cf>" + activeFolder;
            }
            else
                return $"Error : folder allready exists\nCurrent folder is still : {activeFolder}";
        }
        private string REMOVEDIR(string folder)
        {
            if (Directory.Exists(activeFolder + "\\" + folder))
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(activeFolder + "\\" + folder);
                    int aantalMappen = di.GetDirectories().Count();
                    int aantalBestanden = di.GetFiles().Count();
                    if (aantalBestanden > 0 || aantalMappen > 0)
                    {
                        return $"Error : folder NOT empty";

                    }
                    Directory.Delete(activeFolder + "\\" + folder);
                }
                catch (Exception fout)
                {
                    return $"Error : {fout.Message}";

                }
                return "<cf>" + activeFolder;
            }
            else
                return $"Error : folder does not exists";
        }
        private string REMOVEDIRALL(string folder)
        {
            if (Directory.Exists(activeFolder + "\\" + folder))
            {
                try
                {
                    Directory.Delete(activeFolder + "\\" + folder, true);
                }
                catch (Exception fout)
                {
                    return $"Error : {fout.Message}";

                }
                return "<cf>" + activeFolder;
            }
            else
                return $"Error : folder does not exists";
        }
        private string RENAMEDIR(string oldfoldername, string newfoldername)
        {
            if (Directory.Exists(activeFolder + "\\" + oldfoldername))
            {
                try
                {
                    Directory.Move(activeFolder + "\\" + oldfoldername, activeFolder + "\\" + newfoldername);
                    return "<cf>" + activeFolder;
                }
                catch (Exception fout)
                {
                    return $"Error : {fout.Message}";
                }
            }
            else
                return $"Error : folder does not exists";
        }
        private string RENAMEFILE(string oldfilename, string newfilename)
        {
            if (File.Exists(activeFolder + "\\" + oldfilename))
            {
                try
                {
                    File.Move(activeFolder + "\\" + oldfilename, activeFolder + "\\" + newfilename);
                    return "<cf>" + activeFolder;
                }
                catch (Exception fout)
                {
                    return $"Error : {fout.Message}";
                }
            }
            else
                return $"Error : file does not exists";
        }
        private string REMOVEFILE(string filename)
        {
            if (File.Exists(activeFolder + "\\" + filename))
            {
                try
                {
                    File.Delete(activeFolder + "\\" + filename);
                }
                catch (Exception fout)
                {
                    return $"Error : {fout.Message}";

                }
                return "<cf>" + activeFolder;
            }
            else
                return $"Error : file does not exists";
        }
        private string CONTENTFILE(string filename)
        {
            if (File.Exists(activeFolder + "\\" + filename))
            {
                try
                {
                    byte[] filebytes = File.ReadAllBytes(activeFolder + "\\" + filename);
                    return Encoding.ASCII.GetString(filebytes);
                }
                catch (Exception fout)
                {
                    return $"Error : {fout.Message}";

                }
            }
            else
                return $"Error : file does not exists";
        }
    }
}
