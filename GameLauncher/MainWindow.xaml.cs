using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Timer = System.Windows.Forms.Timer;

namespace GameLauncher
{
    public enum LauncherStatus
    {
        Ready,
        Failed,
        DownloadingGame,
        DownloadingUpdate
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int fixedUpdate = 5000;//milliseconds
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;
        private WebClient webClient;
        private Timer timer1;
        private bool msgShowed = false;
        private LauncherStatus launcherStatus;
        internal LauncherStatus LauncherStatus
        {
            get => launcherStatus;
            set
            {
                launcherStatus = value;
                switch (launcherStatus)
                {
                    case LauncherStatus.Ready:
                        StatusText.Text = "Ready (100%)";
                        pbStatus.Value = 100;
                        PlayButton.IsEnabled = true;
                        PlayButton.Content = ButtonBackgroundState(true);
                        PlayButton.Foreground = Brushes.White;
                        PlayButton.Content = "START";
                        break;
                    case LauncherStatus.Failed:
                        StatusText.Text = "Failed";
                        PlayButton.IsEnabled = true;
                        PlayButton.Content = ButtonBackgroundState(true);
                        PlayButton.Foreground = Brushes.White;
                        PlayButton.Content = "FAILED - RETRY";
                        break;
                    case LauncherStatus.DownloadingGame:
                        StatusText.Text = "Downloading (0%)";
                        PlayButton.IsEnabled = false;
                        PlayButton.Content = ButtonBackgroundState(false);
                        PlayButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9C9C9C"));
                        PlayButton.Content = "DOWNLOADING";
                        break;
                    case LauncherStatus.DownloadingUpdate:
                        StatusText.Text = "Updating (0%)";
                        PlayButton.IsEnabled = false;
                        PlayButton.Content = ButtonBackgroundState(false);
                        PlayButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9C9C9C"));
                        PlayButton.Content = "UPDATING";
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Build", "velaverse_multiplayer.exe");
            webClient = new WebClient();

            timer1 = new Timer();
            Start();
        }
        private void Start()
        {
            //InitTimer();
            pbStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FFB6"));
            pbStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#082C3A"));


        }
        private void Update()
        {
            /*if (IsConnectedToInternet() == false)
            {
                webClient.CancelAsync();
                LauncherStatus = LauncherStatus.Failed;
                if (msgShowed == false)
                {
                    SetBooleanTrue(ref msgShowed);
                    MessageBox.Show("No internet connection\nPlease try again later.");
                }
                pbStatus.Value = 100;
                Close();
            }*/
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdate();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && LauncherStatus == LauncherStatus.Ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe, File.ReadAllText(versionFile));
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                Process.Start(startInfo);

                Close();
            }
            else if (LauncherStatus == LauncherStatus.Failed)
            {
                CheckForUpdate();
            }
        }
        private Image ButtonBackgroundState(bool _isEnable)
        {
            Image img = new Image();
            img.Stretch = Stretch.Fill;
            BitmapImage bitimg;
            switch (_isEnable)
            {
                case true:
                    bitimg = new BitmapImage();
                    bitimg.BeginInit();
                    bitimg.UriSource = new Uri("pack://application:,,,/images/DefaultEnable.png", UriKind.RelativeOrAbsolute);
                    bitimg.EndInit();
                    break;
                case false:
                    bitimg = new BitmapImage();
                    bitimg.BeginInit();
                    bitimg.UriSource = new Uri("pack://application:,,,/images/DefaultDisable.png", UriKind.RelativeOrAbsolute);
                    bitimg.EndInit();
                    break;
            }
            img.Source = bitimg;
            return img;
        }
        private void CheckForUpdate()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://fileserver.velaverse.io/Version.txt"));
                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        LauncherStatus = LauncherStatus.Ready;
                    }
                }
                catch (Exception ex)
                {
                    LauncherStatus = LauncherStatus.Failed;
                    MessageBox.Show($"Error checking for game updates:{ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool isUpdate, Version onlineVersion)
        {
            try
            {
                using (webClient)
                {
                    if (isUpdate)
                    {
                        LauncherStatus = LauncherStatus.DownloadingUpdate;
                    }
                    else
                    {
                        LauncherStatus = LauncherStatus.DownloadingGame;
                        onlineVersion = new Version(webClient.DownloadString("https://fileserver.velaverse.io/Version.txt"));
                    }
                    webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                    webClient.DownloadFileAsync(new Uri("https://fileserver.velaverse.io/Build.zip"), gameZip, onlineVersion);
                }

            }
            catch (Exception ex)
            {
                LauncherStatus = LauncherStatus.Failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object? sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();

                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                LauncherStatus = LauncherStatus.Ready;
            }
            catch (Exception ex)
            {
                LauncherStatus = LauncherStatus.Failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }
        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            pbStatus.Value = int.Parse(Math.Truncate(percentage).ToString());
            switch (LauncherStatus)
            {
                case LauncherStatus.DownloadingGame:
                    StatusText.Text = $"Downloading ({pbStatus.Value}%)";
                    break;
                case LauncherStatus.DownloadingUpdate:
                    StatusText.Text = $"Updating ({pbStatus.Value}%)";
                    break;
            }
        }
        private bool IsConnectedToInternet()
        {
            string host = "192.168.1.31";
            bool result;
            Ping ping = new Ping();
            PingReply pingReply = ping.Send(host, 8000);
            if (pingReply.Status == IPStatus.Success)
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }

        public void InitTimer()
        {
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = fixedUpdate; // in miliseconds
            timer1.Start();
        }
        private void SetBooleanTrue(ref bool _bool)
        {
            if (_bool == false)
            {
                _bool = true;
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            Update();
        }

    }
    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);
        private short major;
        private short minor;
        private short subMinor;

        public Version(short major, short minor, short subMinor)
        {
            this.major = major;
            this.minor = minor;
            this.subMinor = subMinor;
        }
        internal Version(string version)
        {
            string[] versionStrings = version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }
        internal bool IsDifferentThan(Version otherVersion)
        {
            if (major != otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
