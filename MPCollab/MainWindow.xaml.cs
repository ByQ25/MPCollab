using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
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
        private TcpListener serverSocket;
        private TcpClient clientSocket;
        private DTO currentDiffs;
        private BinaryReader bReader;
        private BinaryWriter bWriter;

        private bool hostOrClient; // true - host, false - client

        public MainWindow()
        {
            InitializeComponent();

            dTimer = new DispatcherTimer();
            dTimer.Interval = new TimeSpan(0, 0, 0, 0, 17);
            dTimer.Tick += dTimer_Tick;

            //mCursor2Pos = PointToScreen(Mouse.GetPosition(this));
            mCursor2Pos = GetMousePosition();
            screenCenter = new Point((int)SystemParameters.PrimaryScreenWidth / 2, (int)SystemParameters.PrimaryScreenHeight / 2);

            //Dodanie skrótu i przypisanej metody
            RoutedCommand newCmd = new RoutedCommand();
            newCmd.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCmd, ControlSExecuted));

            //mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try { this.textBox.Text = GetLocalIPAddress(); }
            catch (ApplicationException) { }
        }

        // Additional methods:
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        private static string GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            throw new ApplicationException("Local IP Address Not Found!");
        }

        private void ServerSideProcedure()
        {
            serverSocket = new TcpListener(IPAddress.Parse(textBox.Text), 6656);
            serverSocket.Start();
            clientSocket = serverSocket.AcceptTcpClient();
            bReader = new BinaryReader(clientSocket.GetStream());
            MessageBox.Show("Połączenie nawiązane.");
            hostOrClient = true;
            dTimer.Start();
        }

        private void ClientSideProcedure()
        {
            clientSocket = new TcpClient(textBox.Text, 6656);
            bWriter = new BinaryWriter(clientSocket.GetStream());
            SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
            Mouse.OverrideCursor = Cursors.None;
            hostOrClient = false;
        }

        private void RestoreAppToInitialState()
        {
            dTimer.Stop();
            clientSocket.Close();
            clientSocket = null;
            if (hostOrClient)
            {
                serverSocket.Stop();
                serverSocket = null;
                bReader.Close();
                bReader.Dispose();
                bReader = null;
            }
            else
            {
                bWriter.Close();
                bWriter.Dispose();
                bWriter = null;
            }
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        // Events handling:
        private void dTimer_Tick(object sender, EventArgs e)
        {
            dTimer.Stop();
            if (bReader != null)
            {
                // Receiving JSON via stream from TCPClientSocket:
                string tmp = "";
                try { tmp = bReader.ReadString(); }
                catch { RestoreAppToInitialState(); }
                currentDiffs = JsonConvert.DeserializeObject<DTO>(tmp);
                mCursor2Pos.X += currentDiffs.DiffX;
                mCursor2Pos.Y += currentDiffs.DiffY;
                Point tmpMousePos = GetMousePosition();
                SetCursorPos((int)mCursor2Pos.X, (int)mCursor2Pos.Y);
                System.Threading.Thread.Sleep(17);
                SetCursorPos((int)tmpMousePos.X, (int)tmpMousePos.Y);
            }
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
            if (clientSocket != null && clientSocket.Connected && !hostOrClient)
            {
                Point mouseP = GetMousePosition();
                currentDiffs = new DTO((int)(mouseP.X - screenCenter.X), (int)(mouseP.Y - screenCenter.Y));
                // Sending JSON via stream from TCPClientSocket:
                try { bWriter.Write(currentDiffs.ReturnJSONString()); }
                catch (IOException) { RestoreAppToInitialState(); }
                SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
                System.Threading.Thread.Sleep(17);
            }
        }
        private void ControlSExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Wow, działa");
        }
    }

    // Other structs and classes:
    public struct DTO
    {
        // Fields:
        private int diffX, diffY;
        private bool clickLPM, clickPPM;

        // Constructors:
        public DTO(int diffX, int diffY, bool clickLPM, bool clickPPM)
        {
            this.diffX = diffX;
            this.diffY = diffY;
            this.clickLPM = clickLPM;
            this.clickPPM = clickPPM;
        }
        public DTO(int diffX, int diffY) : this(diffX, diffY, false, false) { }

        // Properties:
        public int DiffX { get { return diffX; } }
        public int DiffY { get { return diffY; } }
        public bool LPMClicked { get { return clickLPM; } }
        public bool PPMClicked { get { return clickPPM; } }

        public string ReturnJSONString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
