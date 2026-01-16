using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

class GameInfo
{
    public MetroProgressBar ProgressBar;
    public TextBlock StatusText;
    public Button GameButton;

    public string GameKeyNet = "";
    public string GameNet = "";

    public string GameZipPath = "";
    public string GameFolder = "";

    public string LocalKey = "";
    public string OnlineKey = "";

    public string GameExe = "";

    public GamesList GameInList;

    GameStatus _gameStatus;
    internal GameStatus GameStatus
    {
        get => _gameStatus;

        set
        {
            _gameStatus = value;

            switch (_gameStatus)
            {
                case GameStatus.Empty:
                    StatusText.Text = "Instalar";
                    ProgressBar.Visibility = Visibility.Collapsed;
                    GameButton.IsEnabled = true;
                    break;

                case GameStatus.Outdated:
                    StatusText.Text = "Actualizar";
                    ProgressBar.Visibility = Visibility.Collapsed;
                    GameButton.IsEnabled = true;
                    break;

                case GameStatus.Updating:
                    StatusText.Text = "Actualizando...";
                    ProgressBar.Visibility = Visibility.Visible;
                    GameButton.IsEnabled = false;
                    break;

                case GameStatus.Ready:
                    StatusText.Text = "Jugar";
                    ProgressBar.Visibility = Visibility.Collapsed;
                    GameButton.IsEnabled = true;
                    break;

                case GameStatus.Broken:
                    StatusText.Text = "Reparar";
                    ProgressBar.Visibility = Visibility.Collapsed;
                    GameButton.IsEnabled = true;
                    break;
            }
        }
    }

    public void Verify()
    {
        if (!File.Exists(LocalKey))
        {
            GameStatus = GameStatus.Empty;
            return;
        }

        if (!File.Exists(GameExe))
        {
            GameStatus = GameStatus.Broken;
            return;
        }

        string localKey = File.ReadAllText(LocalKey);

        try
        {
            WebClient webClient = new();
            string onlineKey = webClient.DownloadString(GameKeyNet);

            if (onlineKey != localKey)
                GameStatus = GameStatus.Outdated;
            else
                GameStatus = GameStatus.Ready;
        }
        catch (Exception ex)
        {
            GameStatus = GameStatus.Broken;
            MessageBox.Show($"{GameInList} necesita reparación.");
        }
    }
}

enum GameStatus
{
    Empty,
    Outdated,
    Updating,
    Ready,
    Broken,
}

enum GamesList
{
    JaroTCG,
}

namespace JaroClient
{
    public partial class MainWindow : MetroWindow
    {
        // Add Icon to this
        bool hasVerifiedClient = false;

        string _rootParentPath;
        string _gamesFolder;

        string _clientPath;
        string _clientExe;

        string _clientKeyNet = "https://raw.githubusercontent.com/imDevDonut/JaroClient/refs/heads/main/ClientKey.Devour";
        string _clientKeyLocal;

        string _updaterNet = "https://github.com/imDevDonut/JaroClient/releases/download/Launcher/ClientUpdater.zip";
        string _updaterZip;

        string _updaterFolder;
        string _updaterExe;

        // GAME INFO
        bool _forceClose = false;
        List<GameInfo> _allGamesInfo = new List<GameInfo>();
        GameInfo _gameInfo_JaroTCG;

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
            if (hasVerifiedClient) return;
            hasVerifiedClient = true;

            GamesHolder.Visibility = Visibility.Collapsed;

            if (Directory.Exists(_updaterFolder))
            {
                Directory.Delete(_updaterFolder, true);
            }

            CheckForClientUpdates();
        }

        void CheckForClientUpdates()
        {
            if (ClientStatus != GameStatus.Broken && File.Exists(_clientKeyLocal))
            {
                string localKey = File.ReadAllText(_clientKeyLocal);
                try
                {
                    using WebClient webClient = new();
                    string onlineKey = webClient.DownloadString(_clientKeyNet).Trim();

                    if (onlineKey != localKey)
                    {
                        RequestClientUpdate();
                    }
                    else
                    {
                        ClientStatus = GameStatus.Ready;
                        InitializeGames();
                        GamesHolder.Visibility = Visibility.Visible;
                    }
                }
                catch (WebException ex) when
                    ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    RequestClientUpdate();
                }
                catch (Exception ex)
                {
                    // ❌ Real error
                    MessageBox.Show($"Ha ocurrido un problema\n\n{ex}");
                    ClientStatus = GameStatus.Broken;
                }
            }
            else
            {
                RequestClientUpdate();
            }
        }

        void RequestClientUpdate()
        {
            try
            {
                WebClient webClient = new();
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;

                ClientStatus = GameStatus.Updating;

                StartingScreen_ProgressBar.Minimum = 0;
                StartingScreen_ProgressBar.Minimum = 100;
                StartingScreen_ProgressBar.Value = 0;

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(OnClientUpdateDownload);
                webClient.DownloadFileAsync(new Uri(_updaterNet), _updaterZip);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar cliente.\n\n{ex}");
                ClientStatus = GameStatus.Broken;
            }
        }

        void InitializeGames()
        {
            _gamesFolder = Path.Combine(_rootParentPath, "Games");
            Directory.CreateDirectory(_gamesFolder);

            // Jaro TCG

            _gameInfo_JaroTCG = new GameInfo
            {
                ProgressBar = ProgressBar_JaroTCG,
                StatusText = StatusText_JaroTCG,
                GameButton = GameButton_JaroTCG,
                GameKeyNet = "https://raw.githubusercontent.com/imDevDonut/JaroClient/refs/heads/main/key_JaroTCG",
                GameNet = "https://github.com/imDevDonut/JaroClient/releases/download/Launcher/JaroTCG.zip",

                GameFolder = Path.Combine(_gamesFolder, "Jaro TCG"),
                GameZipPath = Path.Combine(_gamesFolder, "JaroTCG.zip"),

                LocalKey = Path.Combine(_gamesFolder, "Jaro TCG", "GameKey.devour"),
                GameExe = Path.Combine(_gamesFolder, "Jaro TCG", "Jaro TCG.exe"),

                GameInList = GamesList.JaroTCG,
                GameStatus = GameStatus.Empty
            };

            _allGamesInfo.Add(_gameInfo_JaroTCG);

            foreach (var game in _allGamesInfo)
            {
                game.Verify();
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
            _forceClose = true;
            Close();
        }

        void CreateShortcut(string shortcutName, string targetPath, string workingDirectory, string iconPath = null)
        {
            string shortcutPath = Path.Combine(_rootParentPath, $"{shortcutName}.Ink");

            if (File.Exists(shortcutPath))
                return;
        }

        void Play_JaroTCG_Button_Click(object sender, RoutedEventArgs e)
        {
            UseGameButton(_gameInfo_JaroTCG);
        }

        void LaunchGame(GameInfo gameInfo)
        {
            gameInfo.Verify();

            switch (gameInfo.GameStatus)
            {
                case GameStatus.Ready:
                    ProcessStartInfo startInfo = new(gameInfo.GameExe);
                    startInfo.WorkingDirectory = gameInfo.GameFolder;
                    Process.Start(startInfo);
                    Close();
                    break;

                case GameStatus.Broken:
                    MessageBox.Show($"{gameInfo.GameInList} necesita repararse.");
                    break;
            }
        }

        void InstallGame(GameInfo gameInfo)
        {
            try
            {
                WebClient webClient = new();
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(OnGameDownloadCompleted);

                gameInfo.GameStatus = GameStatus.Updating;
                gameInfo.OnlineKey = webClient.DownloadString(gameInfo.GameKeyNet);

                Directory.CreateDirectory(gameInfo.GameFolder);

                StartingScreen_ProgressBar.Minimum = 0;
                StartingScreen_ProgressBar.Maximum = 100;
                StartingScreen_ProgressBar.Value = 0;

                webClient.DownloadFileAsync(new Uri(gameInfo.GameNet), gameInfo.GameZipPath, gameInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar cliente.\n\n{ex}");
                ClientStatus = GameStatus.Broken;
            }
        }

        void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.UserState is GameInfo game)
            {
                game.ProgressBar.Value = e.ProgressPercentage;
                return;
            }

            StartingScreen_ProgressBar.Value = e.ProgressPercentage;
        }

        void OnGameDownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.UserState is not GameInfo game)
                return;

            try
            {
                string onlineKey = game.OnlineKey;
                File.WriteAllText(game.LocalKey, onlineKey);

                ZipFile.ExtractToDirectory(game.GameZipPath, _gamesFolder, true);
                File.Delete(game.GameZipPath);

                game.Verify();
            }
            catch (Exception ex)
            {
                game.GameStatus = GameStatus.Broken;
                MessageBox.Show($"Error al instalar {game.GameInList}\n\n{ex}");
            }
        }

        void UseGameButton(GameInfo gameInfo)
        {
            switch (gameInfo.GameStatus)
            {
                case GameStatus.Empty or GameStatus.Broken or GameStatus.Outdated:
                    InstallGame(gameInfo);
                    break;

                case GameStatus.Ready:
                    LaunchGame(gameInfo);
                    break;
            }
        }

        bool IsAnyGameUpdating()
        {
            foreach (var gameInfo in _allGamesInfo)
            {
                if (gameInfo.GameStatus == GameStatus.Updating)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_forceClose && IsAnyGameUpdating())
            {
                MessageBox.Show($"Jaro TCG se está actualizando.");
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }
    }
}
