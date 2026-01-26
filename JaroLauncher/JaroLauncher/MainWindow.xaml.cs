using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using MahApps.Metro.Controls;

namespace JaroLauncher
{
    enum UpdaterStatus
    {
        Preparing,
        Downloading,
        Ready,
        Broken,
    }

    public partial class MainWindow : MetroWindow
    {
        bool _hasRun = false;

        string _rootParentPath;
        string _updaterFolder;

        string _clientKeyNet = "https://raw.githubusercontent.com/imDevDonut/JaroClient/refs/heads/main/Keys/key_Client.devour";
        string _clientKeyLocal;

        string _clientNet = "https://github.com/imDevDonut/JaroClient/releases/download/Launcher/Launcher.zip";
        string _clientZip;

        string _clientFolder;
        string _clientExe;

        UpdaterStatus _status;
        UpdaterStatus CurrentStatus
        {
            get => _status;

            set
            {
                _status = value;

                switch (_status)
                {
                    case UpdaterStatus.Preparing:
                        break;

                    case UpdaterStatus.Downloading:
                        break;

                    case UpdaterStatus.Ready:
                        Close();
                        // Close, delete and launch client again
                        break;

                    case UpdaterStatus.Broken:
                        Close();
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            _rootParentPath = Path.GetDirectoryName(Directory.GetCurrentDirectory());
            _updaterFolder = Directory.GetCurrentDirectory();

            _clientFolder = Path.Combine(_rootParentPath, "Launcher");
            _clientKeyLocal = Path.Combine(_clientFolder, "ClientKey.devour");
            _clientZip = Path.Combine(_rootParentPath, "Launcher.zip");
            _clientExe = Path.Combine(_clientFolder, "Launcher.exe");

        }

        void OnLoad(object sender, EventArgs e)
        {
            if (_hasRun) return;
            _hasRun = true;

            RequestUpdate();
        }

        void RequestUpdate()
        {
            try
            {
                WebClient webClient = new();
                string onlineKey = webClient.DownloadString(_clientKeyNet);
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;

                CurrentStatus = UpdaterStatus.Downloading;

                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = 100;
                ProgressBar.Value = 100;

                webClient.DownloadFileCompleted += new(DownloadLauncherCompletedCallback);
                webClient.DownloadFileAsync(new Uri(_clientNet), _clientZip, onlineKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar cliente.\n\n{ex}");
                CurrentStatus = UpdaterStatus.Broken;
            }
        }

        void DownloadLauncherCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                ZipFile.ExtractToDirectory(_clientZip, _rootParentPath, true);
                File.Delete(_clientZip);

                string onlineKey = e.UserState as string;
                File.WriteAllText(_clientKeyLocal, onlineKey);

                ProcessStartInfo startInfo = new(_clientExe);
                startInfo.WorkingDirectory = _clientFolder;
                Process.Start(startInfo);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al instalar cliente.\n\n{ex}");
                CurrentStatus = UpdaterStatus.Broken;
                return;
            }

            CurrentStatus = UpdaterStatus.Ready;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_status == UpdaterStatus.Downloading)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }

        void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }
    }
}