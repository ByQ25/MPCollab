using System;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Threading;


namespace MPCollab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // TODO: Let's consider an option of installing dll embedding package: Install-Package Costura.Fody
    public partial class MainWindow : Window, IDisposable
    {
        private DTOext clipboard;
        private Thread connMaker, pasteChecker;
        private XElement leftCompIP, rightCompIP;
        private XDocument confFile;
        private DispatcherTimer mainTimer, edgeCheckerTimer;
        private TwoCursorsHandler TCH;
        private ClipboardManagerImpl clipboardManager;
        private NativeMethods.Win32Point w32MousePos;
        private object pasteThreadlock;
        private bool disposed, hostOrClient; // true - host, false - client
        private const int timeWin = 17;
        private const string confPath = "config.xml";
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        public const int VK_LCONTROL = 0xA2; //Left Control key code
        public const int V = 0x56;

        public MainWindow()
        {
            InitializeComponent();

            // Checking the config file integrity and processing it
            if (File.Exists(confPath))
            {
                try
                {
                    confFile = XDocument.Load(confPath);
                    leftCompIP = (from ips in confFile.Descendants("IPs").Descendants("IP") select ips).ElementAt(0);
                    rightCompIP = (from ips in confFile.Descendants("IPs").Descendants("IP") select ips).ElementAt(1);
                    leftCompIPTB.Text = leftCompIP.Value;
                    rightCompIPTB.Text = rightCompIP.Value;
                }
                catch
                {
                    CreateNewConfigXmlDoc(true);
                    MessageBox.Show("Plik konfiguracyjny był uszkodzony i został nadpisany przez prawidłową, lecz pustą wersję.",
                        "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else CreateNewConfigXmlDoc(true);

            // Adding shortcut and binding commands
            RoutedCommand newCmd = new RoutedCommand();
            newCmd.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCmd, ControlSExecuted));

            newCmd = new RoutedCommand();
            newCmd.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.C, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCmd, ControlCExecuted));

            newCmd = new RoutedCommand();
            newCmd.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.V, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCmd, ControlVExecuted));

            disposed = false;
            mainTimer = new DispatcherTimer();
            mainTimer.Interval = new TimeSpan(0,0,1);
            mainTimer.Tick += mainTimer_Tick;

            edgeCheckerTimer = new DispatcherTimer();
            edgeCheckerTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            edgeCheckerTimer.Tick += edgeCheckerTimer_Tick;

            w32MousePos = new NativeMethods.Win32Point();
            clipboardManager = new ClipboardManagerImpl(new DataObject());
            pasteThreadlock = new object();

            try { localIPLabel.Content = GetLocalIPAddress(); }
            catch (ApplicationException) { }
            RestoreAppToInitialState("Serwer został uruchomiony.", false);
        }

        // Additional methods:
        private static string GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            throw new ApplicationException("Local IP Address Not Found!");
        }

        private void ServerSideProcedure()
        {
            // Stopping client side:
            if (connMaker != null && connMaker.IsAlive && !hostOrClient)
            {
                mainTimer.Stop();
                connMaker.Abort();
                while (connMaker.IsAlive) Thread.Sleep(1000);
                connMaker = null;
                TCH = null;
                GC.Collect();
            }

            hostOrClient = true;
            if (TCH == null) TCH = new TwoCursorsHandler(localIPLabel.Content.ToString(), timeWin, hostOrClient);
            connMaker = new Thread(TCH.MakeConnection);
            connMaker.IsBackground = true;
            connMaker.Start();

            pasteChecker = new Thread(CheckPaste);
            pasteChecker.Start();
            mainTimer.Start();
        }

        private void ClientSideProcedure(byte computerTag)
        {
            // Stopping server side:
            if (connMaker != null && connMaker.IsAlive && hostOrClient)
            {
                mainTimer.Stop();
                connMaker.Abort();
                while (connMaker.IsAlive) Thread.Sleep(1000);
                connMaker = null;
                TCH = null;
                GC.Collect();
            }

            if (TCH == null)
            {
                // Locking mouse cursor in the window:
                Point lockingRect = this.PointToScreen(new Point(0, 0));
                System.Drawing.Rectangle r = new System.Drawing.Rectangle(
                    (int)lockingRect.X,
                    (int)lockingRect.Y,
                    (int)(lockingRect.X + this.Width - 8 * SystemParameters.BorderWidth),
                    (int)(lockingRect.Y + this.Height - 1.5 * SystemParameters.WindowCaptionHeight));
                NativeMethods.ClipCursor(ref r);

                hostOrClient = false;
                DisableWindowControls();
                List<Komputer> comps = new List<Komputer>();
                comps.Add((Komputer)vb2.Child);
                string ip = "";
                switch (computerTag)
                {
                    case 0:
                        ip = leftCompIPTB.Text;
                        comps.Add((Komputer)vb1.Child);
                        break;
                    case 1:
                        ip = rightCompIPTB.Text;
                        comps.Add((Komputer)vb3.Child);
                        break;
                }
                TCH = new TwoCursorsHandler(ip, timeWin, hostOrClient);
                connMaker = new Thread(TCH.MakeConnection);
                connMaker.IsBackground = true;
                connMaker.Start();
                mainTimer.Start();
                StartBlinking(comps);
                this.bottomLabel.Content = "Trwa próba połączenia z hostem.";
            }
        }

        private void DisableWindowControls()
        {
            this.leftCompIPTB.IsEnabled = false;
            this.rightCompIPTB.IsEnabled = false;
            this.buttonClientLeft.IsEnabled = false;
            this.buttonHost.IsEnabled = false;
            this.buttonClientRight.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.None;
        }

        private void EnableWindowControls()
        {
            this.leftCompIPTB.IsEnabled = true;
            this.rightCompIPTB.IsEnabled = true;
            if (ValidateTextBoxIP(this.leftCompIPTB, true))
                buttonClientLeft.IsEnabled = true;
            this.buttonHost.IsEnabled = true;
            if (ValidateTextBoxIP(this.rightCompIPTB, true))
                buttonClientRight.IsEnabled = true; ;
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void RestoreAppToInitialState(string message, bool doWait)
        {
            if (doWait) Thread.Sleep(1000);
            if (TCH != null)
            {
                TCH.StopServer();
                TCH.Dispose();
                TCH = null;
            }
            disposed = false;
            hostOrClient = true;
            NativeMethods.ClipCursor(IntPtr.Zero);
            EnableWindowControls();
            Komputer[] comps = { (Komputer)vb1.Child, (Komputer)vb2.Child, (Komputer)vb3.Child };
            StopBlinking(comps);
            bottomLabel.Content = message;
            if ((bool)this.edgesConnCB.IsChecked) edgeCheckerTimer.Start();
            if (localIPLabel.Content.ToString() != null) ServerSideProcedure();
        }

        private void CreateNewConfigXmlDoc(bool save)
        {
            confFile = new XDocument(
                    new XElement("Configs",
                        new XElement("IPs",
                            new XElement("IP", ""),
                            new XElement("IP", ""))));
            if (save) confFile.Save(confPath);
        }

        private bool ValidateTextBoxIP(TextBox tb, bool visualsOn)
        {
            try { IPAddress.Parse(tb.Text); }
            catch
            {
                if (visualsOn)
                {
                    tb.BorderBrush = System.Windows.Media.Brushes.Crimson;
                    tb.ToolTip = "Podano niepoprawny adres IP.";
                }
                return false;
            }
            if (visualsOn)
            {
                tb.BorderBrush = System.Windows.Media.Brushes.ForestGreen;
                tb.ToolTip = "Podany adres IP jest poprawny.";
            }
            return true;
        }

        private void ProcessTBIPChange(TextBox tb, Button but, XElement ipNode)
        {
            if (ValidateTextBoxIP(tb, true))
            {
                but.IsEnabled = true;
                ipNode.Value = tb.Text;
                confFile.Save(confPath);
            }
            else but.IsEnabled = false;
        }

        private void CheckPaste()
        {
            if (TCH.PasteField && hostOrClient)
            {
                clipboardManager.CopyClipboard();
                clipboard = TCH.ReceivedClipboard;
                clipboardManager.ImportDTOext(clipboard);

                NativeMethods.keybd_event(VK_LCONTROL, 0, KEYEVENTF_EXTENDEDKEY, (IntPtr)0);
                NativeMethods.keybd_event(V, 0, KEYEVENTF_EXTENDEDKEY, (IntPtr)0);
                NativeMethods.keybd_event(V, 0, KEYEVENTF_KEYUP, (IntPtr)0);
                NativeMethods.keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, (IntPtr)0);
                lock (pasteThreadlock) { TCH.PasteField = false; }
            }
        }

        private void StartBlinking(List<Komputer> comps)
        {
            foreach (Komputer comp in comps)
                comp.Start();
        }

        private void StopBlinking(Komputer[] comps)
        {
            foreach (Komputer comp in comps)
                comp.Stop();
        }

        // Events handling:
        private void MainWin_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Multiply: ServerSideProcedure(); break;
                case Key.OemMinus: ClientSideProcedure(0); break;
                case Key.OemPlus: ClientSideProcedure(1); break;
                case Key.Escape:
                    bottomLabel.Content = "Połączenie zosało zakończone.";
                    RestoreAppToInitialState("Serwer został uruchomiony.", true);
                    break;
            }
        }

        private void buttonHost_Click(object sender, RoutedEventArgs e)
        {
            ServerSideProcedure();
        }

        private void buttonClientLeft_Click(object sender, RoutedEventArgs e)
        {
            ClientSideProcedure(0);
        }

        private void buttonClientRight_Click(object sender, RoutedEventArgs e)
        {
            ClientSideProcedure(1);
        }

        private void leftCompIPTB_LostFocus(object sender, RoutedEventArgs e)
        {
            ProcessTBIPChange(leftCompIPTB, buttonClientLeft, leftCompIP);
        }

        private void rightCompIPTB_LostFocus(object sender, RoutedEventArgs e)
        {
            ProcessTBIPChange(rightCompIPTB, buttonClientRight, rightCompIP);
        }

        private void MainWin_MouseMove(object sender, MouseEventArgs e)
        {
            if (TCH != null) TCH.HandleMouseMove();
        }

        private void MainWin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TCH != null) TCH.HandleMouseClick(TwoCursorsHandler.MButtons.LMB);
        }

        private void MainWin_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TCH != null) TCH.HandleMouseClick(TwoCursorsHandler.MButtons.RMB);
        }

        private void mainTimer_Tick(object sender, EventArgs e)
        {
            if (connMaker != null && !connMaker.IsAlive)
            {
                if (TCH.ConnectionEstablished)
                {
                    bottomLabel.Content = "Połączenie zosało nawiązane.";
                    if (hostOrClient)
                    {
                        string clientIP = TCH.ClientIP.Split(':')[0];
                        List<Komputer> comps = new List<Komputer>();
                        comps.Add((Komputer)vb2.Child);
                        if (clientIP == leftCompIP.Value)
                            comps.Add((Komputer)vb1.Child);
                        else if (clientIP == rightCompIP.Value)
                            comps.Add((Komputer)vb3.Child);
                        StartBlinking(comps);
                        TCH.StartServer();
                    }
                    mainTimer.Stop();
                }
                else RestoreAppToInitialState("Serwer nie odpowiada.", false);
                connMaker = null;
            }
        }

        private void edgeCheckerTimer_Tick(object sender, EventArgs e)
        {
            NativeMethods.GetCursorPos(ref w32MousePos);
            if (w32MousePos.X == 0 && buttonClientLeft.IsEnabled)
            {
                ClientSideProcedure(0);
                edgeCheckerTimer.Stop();
            }
            else if (w32MousePos.X == SystemParameters.PrimaryScreenWidth - 1 && buttonClientRight.IsEnabled)
            {
                ClientSideProcedure(1);
                edgeCheckerTimer.Stop();
            }
        }

        private void edgesConnCB_Click(object sender, RoutedEventArgs e)
        {
            switch (edgesConnCB.IsChecked)
            {
                case false: edgeCheckerTimer.Stop(); break;
                case true: edgeCheckerTimer.Start(); break;
            }
        }

        private void ControlCExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (TCH != null && !hostOrClient) TCH.HandleCopy();
        }

        private void ControlVExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (TCH != null && !hostOrClient) TCH.HandlePaste();
        }

        private void ControlSExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("skrót");
        }

        // IDisposable implementation:
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing) TCH.Dispose();
            this.disposed = true;
        }
    }
}
