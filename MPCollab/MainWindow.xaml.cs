using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Xml.Linq;

namespace MPCollab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // TODO: Let's consider an option of installing dll embedding package: Install-Package Costura.Fody
    public partial class MainWindow : Window, IDisposable
    {
        private TwoCursorsHandler TCH;
        private XDocument confFile;
        private XElement leftCompIP, rightCompIP;
        private bool disposed, hostOrClient; // true - host, false - client
        private const int timeWin = 17;
        private const string confPath = "config.xml";

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

            // Validating IPs
            if (ValidateTextBoxIP(leftCompIPTB, true)) buttonClientLeft.IsEnabled = true;
            if (ValidateTextBoxIP(rightCompIPTB, true)) buttonClientRight.IsEnabled = true;

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

            try { this.localIPLabel.Content = GetLocalIPAddress(); }
            catch (ApplicationException) { }
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
            hostOrClient = true;
            if (TCH == null) TCH = new TwoCursorsHandler(localIPLabel.Content.ToString(), timeWin, hostOrClient);
            TCH.StartServer();
            bottomLabel.Content = "Połączenie zosało nawiązane.";
            TCH.OnPushClipboard += TCH.Paste;
            // TODO: Server should blink on computers that connected, not always left and center.
            StartBlinking((Komputer)vb1.Child,(Komputer)vb2.Child); 
        }

        private void ClientSideProcedure(byte computerTag)
        {
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
                switch (computerTag)
                {
                    case 0:
                        TCH = new TwoCursorsHandler(leftCompIPTB.Text, timeWin, hostOrClient);
                        StartBlinking((Komputer)vb1.Child, (Komputer)vb2.Child);
                        break;
                    case 1:
                        TCH = new TwoCursorsHandler(rightCompIPTB.Text, timeWin, hostOrClient);
                        StartBlinking((Komputer)vb2.Child, (Komputer)vb3.Child);
                        break;
                }
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
            this.buttonClientLeft.IsEnabled = true;
            this.buttonHost.IsEnabled = true;
            this.buttonClientRight.IsEnabled = true;
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void RestoreAppToInitialState()
        {
            if (TCH != null)
            {
                TCH.StopServer();
                TCH.Dispose();
                TCH = null;
            }
            NativeMethods.ClipCursor(IntPtr.Zero);
            EnableWindowControls();
            Komputer[] comps = { (Komputer)vb1.Child, (Komputer)vb2.Child, (Komputer)vb3.Child };
            StopBlinking(comps);
            bottomLabel.Content = "Połączenie zosało zakończone.";
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
            if (ValidateTextBoxIP(this.leftCompIPTB, true))
            {
                tb.IsEnabled = true;
                ipNode.Value = tb.Text;
                confFile.Save(confPath);
            }
            else but.IsEnabled = false;
        }

        // Events handling:
        private void MainWin_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.NumPad1: ServerSideProcedure(); break;
                case Key.NumPad2: ClientSideProcedure(0); break;
                case Key.NumPad3: ClientSideProcedure(1); break;
                case Key.Escape: RestoreAppToInitialState(); break;
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

        private void StartBlinking(Komputer com1, Komputer com2)
        {
            com1.Start();
            com2.Start();
        }

        private void StopBlinking(Komputer[] comps)
        {
            foreach (Komputer comp in comps)
                comp.Stop();
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
