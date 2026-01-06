using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using MahApps.Metro.Controls;

struct GameInfo
{
    public string Name;
    public string VersionLink;
    public string ZipLink;
    public string RootPath;
    public string VersionFile;
    public string GameZip;
    public string GameExe;
}

enum GameStatus
{
    Empty,
    Updating,
    Ready,
    Broken,
}

namespace JaroClient
{
    public partial class MainWindow : MetroWindow
    {
        bool _hasRun = false;

        string _rootParentPath;
        string _clientPath;
        string _clientExe;

        string _clientKeyNet = "https://raw.githubusercontent.com/imDevDonut/JaroClient/refs/heads/main/ClientKey.Devour";
        string _clientKeyLocal;

        string _updaterNet = "https://github.com/imDevDonut/JaroClient/releases/download/Launcher/ClientUpdater.zip";
        string _updaterZip;

        string _updaterFolder;
        string _updaterExe;


        GameStatus _clientStatus;
        internal GameStatus ClientStatus
        {
            get => _clientStatus;

            set
            {
                _clientStatus = value;

                switch (_clientStatus)
                {
                    case GameStatus.Empty:
                        break;

                    case GameStatus.Updating:
                        StartingScreen_StatusText.Text = "Actualizando Cliente...";
                        StartingScreen_ProgressBar.Visibility = Visibility.Visible;
                        break;

                    case GameStatus.Ready:
                        
                        // Go to Main Screen Here
                        break;

                    case GameStatus.Broken:
                        Close();
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            _rootParentPath = Path.GetDirectoryName(Directory.GetCurrentDirectory());

            _clientPath = Directory.GetCurrentDirectory();
            _clientExe = Path.Combine(_clientPath, "Launcher.exe");

            _clientKeyLocal = Path.Combine(_clientPath, "ClientKey.devour");

            _updaterFolder = Path.Combine(_rootParentPath, "Update");
            _updaterZip = Path.Combine(_rootParentPath, "ClientUpdater.zip");
            _updaterExe = Path.Combine(_updaterFolder, "Updater.exe");
        }

        void Window_Loaded(object sender, EventArgs e)
        {
            if (_hasRun) return;
            _hasRun = true;
            CheckForClientUpdates();

            if (Directory.Exists(_updaterFolder))
            {
                Directory.Delete(_updaterFolder, true);
                MessageBox.Show("El cliente se ha actualizado correctamente.");
            }
        }

        void CheckForClientUpdates()
        {
            if (ClientStatus != GameStatus.Broken && File.Exists(_clientKeyLocal))
            {
                string localKey = File.ReadAllText(_clientKeyLocal);

                try
                {
                    WebClient webClient = new();
                    string onlineKey = webClient.DownloadString(_clientKeyNet);

                    if (onlineKey != localKey)
                    {
                        ClientStatus = GameStatus.Updating;
                        RequestUpdate(onlineKey);
                    }
                    else
                    {
                        ClientStatus = GameStatus.Ready;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ha ocurrido un problema\n\n{ex}");
                    ClientStatus = GameStatus.Broken;
                }
            }
            else
            {
                RequestUpdate();
            }
        }

        void RequestUpdate(string onlineKey = null)
        {
            try
            {
                WebClient webClient = new();
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;

                ClientStatus = GameStatus.Updating;

                if (string.IsNullOrEmpty(onlineKey))
                    onlineKey = webClient.DownloadString(_clientKeyNet);

                StartingScreen_ProgressBar.Minimum = 0;
                StartingScreen_ProgressBar.Minimum = 100;
                StartingScreen_ProgressBar.Value = 0;

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(OnClientUpdateDownload);
                webClient.DownloadFileAsync(new Uri(_updaterNet), _updaterZip, onlineKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar cliente.\n\n{ex}");
                ClientStatus = GameStatus.Broken;
            }
        }

        void OnClientUpdateDownload(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                //CreateShortcut(shortcutName: "Jaro Client", targetPath: _clientExe, workingDirectory: _rootParentPath);

                ZipFile.ExtractToDirectory(_updaterZip, _rootParentPath, true);
                File.Delete(_updaterZip);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al instalar cliente.\n\n{ex}");
                ClientStatus = GameStatus.Broken;
                return;
            }

            ProcessStartInfo startInfo = new(_updaterExe);
            startInfo.WorkingDirectory = _updaterFolder;
            Process.Start(startInfo);
            Close();
        }

        void CreateShortcut(string shortcutName, string targetPath, string workingDirectory, string iconPath = null)
        {
            string shortcutPath = Path.Combine(_rootParentPath, $"{shortcutName}.Ink");

            if (File.Exists(shortcutPath))
                return;
        }

        void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            StartingScreen_ProgressBar.Value = e.ProgressPercentage;
        }
    }
}
