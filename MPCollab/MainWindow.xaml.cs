using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MPCollab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point mCursor2Pos, screenCenter;
        private DispatcherTimer dTimer;
        private TwoCursorsHandler TCH;
        private DTO currentDiffs;
        private bool hostOrClient; // true - host, false - client
        private static int timeWin = 17;

        public MainWindow()
        {
            InitializeComponent();

            dTimer = new DispatcherTimer();
            dTimer.Interval = new TimeSpan(0, 0, 0, 0, timeWin);
            dTimer.Tick += dTimer_Tick;

            //Dodanie skrótu i przypisanej metody
            RoutedCommand newCmd = new RoutedCommand();
            newCmd.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCmd, ControlSExecuted));

            //mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try { this.textBox.Text = GetLocalIPAddress(); }
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
            TCH = new TwoCursorsHandler(textBox.Text, timeWin, hostOrClient);
            MessageBox.Show("Połączenie nawiązane.");
            dTimer.Start();
        }

        private void ClientSideProcedure()
        {
            hostOrClient = false;
            Mouse.OverrideCursor = Cursors.None;
            TCH = new TwoCursorsHandler(textBox.Text, timeWin, hostOrClient);
        }

        private void RestoreAppToInitialState()
        {
            dTimer.Stop();
            TCH.Dispose();
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        // Events handling:
        private void dTimer_Tick(object sender, EventArgs e)
        {
            dTimer.Stop();
            TCH.UpdateCursorsPositions();
            dTimer.Start();
        }

        private void MainWin_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.NumPad1: ServerSideProcedure(); break;
                case Key.NumPad2: ClientSideProcedure(); break;
                case Key.Escape: RestoreAppToInitialState(); break;
            }
        }

        private void buttonHost_Click(object sender, RoutedEventArgs e)
        {
            ServerSideProcedure();
        }

        private void buttonClient_Click(object sender, RoutedEventArgs e)
        {
            ClientSideProcedure();
        }

        private void MainWin_MouseMove(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MainWin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MainWin_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ControlSExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Wow, działa");
        }
    }
}
