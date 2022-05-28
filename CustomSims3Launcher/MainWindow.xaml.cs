using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using Microsoft.Win32;

namespace CustomSims3Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public event EventHandler<GameRunningEvent> appEvent;
        public bool IsInstalled = false;
        public string InstallationLocation = "";
        public bool SkipLauncher { get; set; }
        private bool gameRunning = false;
        CancellationTokenSource cts = new CancellationTokenSource();

        public MainWindow()
        {
            if (!checkInstalled("The Sims™ 3"))
            {
                MessageBox.Show(
                    "The sims 3 is NOT installed. This is a launcher! " +
                    "Only launches the game for Alder Lake (12th CPUs). It does NOT install the game for you!",
                    "sims 3",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Application.Current.Shutdown();
            }
            else
            {
                IsInstalled = true;
                string registryKey = @"SOFTWARE\WOW6432Node\Sims\The Sims 3";
                RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey);
                if (key != null)
                {
                    Object o = key.GetValue("Install Dir").ToString();
                    if (o != null)
                    {
                        InstallationLocation = o.ToString();
                        Trace.WriteLine("Sims 3 installation Dir: {0}", InstallationLocation);
                    }

                    key.Close();
                }
            }
            WPFUI.Appearance.Background.Apply(
                  this,                                // Window class
                  WPFUI.Appearance.BackgroundType.Mica // Background type
            );
            InitializeComponent();
            skipLauncherCheck.DataContext = this;
            installedLabel.Content = IsInstalled ? "Game is installed" : "";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!gameRunning)
            {
                if (SkipLauncher)
                {
             
                    LaunchGame(@"TS3W.exe");
                    ThreadPool.QueueUserWorkItem(new WaitCallback(setAffinity), cts.Token);

                }
                else
                {
                    LaunchGame( @"Sims3LauncherW.exe");
                    ThreadPool.QueueUserWorkItem(new WaitCallback(setAffinity), cts.Token);

                }
            }
          
        }

        
        private void OnSomethingChanged(object sender, GameRunningEvent e)
        {
            cts.Cancel();
        }

        private void setAffinity(Object? obj)
        {
            CancellationToken token = (CancellationToken)obj;

            bool gotProcess = false;
            Process SimsProcess = new Process();

            while (!gotProcess)
            {
                if (token.IsCancellationRequested)
                {
                    Trace.WriteLine("cancellation has been requested...");
                    break;
                }
                Process[] processes = Process.GetProcessesByName("TS3W");
                if (processes.Length != 0)
                {
                    
                    SimsProcess = processes[0];
                    gotProcess = true;
                }
            }
            if (gotProcess)
            {
                try
                {
                    var affinity = SimsProcess.ProcessorAffinity;
                    SimsProcess.ProcessorAffinity = (IntPtr) 0x0001;
                    Thread.Sleep(5000);
                    SimsProcess.ProcessorAffinity = affinity;
                    gameRunning = true;

                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
          
            }

        }


        private void LaunchGame(string filname)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WorkingDirectory = InstallationLocation + @"\Game\Bin\",
                FileName = InstallationLocation + @"\Game\Bin\" + filname
            };
            
            Process.Start(startInfo);
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        public static bool checkInstalled(string c_name)
        {
            string displayName;

            string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey);
            if (key != null)
            {
                foreach (RegistryKey subkey in key.GetSubKeyNames().Select(keyName => key.OpenSubKey(keyName)))
                {
                    displayName = subkey.GetValue("DisplayName") as string;
                    if (displayName != null && displayName.Contains(c_name))
                    {
                        return true;
                    }
                }

                key.Close();
            }

            registryKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            key = Registry.LocalMachine.OpenSubKey(registryKey);
            if (key != null)
            {
                foreach (RegistryKey subkey in key.GetSubKeyNames().Select(keyName => key.OpenSubKey(keyName)))
                {
                    displayName = subkey.GetValue("DisplayName") as string;
                    if (displayName != null && displayName.Contains(c_name))
                    {
                        return true;
                    }
                }

                key.Close();
            }

            return false;
        }
    }
}

public class GameRunningEvent{
    public String name {get; set;}
}