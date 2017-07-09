﻿using System;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        Point mCursor2Pos, screenCenter;
        DispatcherTimer dTimer;
        Socket mainSocket;
        DTO currentDiffs;

        public MainWindow()
        {
            InitializeComponent();

            dTimer = new DispatcherTimer();
            dTimer.Interval = new TimeSpan(0, 0, 0, 0, 17);
            dTimer.Tick += dTimer_Tick;

            //mCursor2Pos = PointToScreen(Mouse.GetPosition(this));
            mCursor2Pos = GetMousePosition();
            screenCenter = new Point((int)SystemParameters.PrimaryScreenWidth / 2, (int)SystemParameters.PrimaryScreenHeight / 2);

            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
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

        private void NamePlaceholder()
        {
            mainSocket.Bind(new IPEndPoint(IPAddress.Parse(textBox.Text), 6656));
            mainSocket.Listen(1);
            mainSocket.Accept();
            MessageBox.Show("Połączenie nawiązane.");
            SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
            Mouse.OverrideCursor = Cursors.None;
        }

        private void NamePlaceholder2()
        {
            mainSocket.Connect(textBox.Text, 6656);
            dTimer.Start();
        }

        // Events handling:
        private void dTimer_Tick(object sender, EventArgs e)
        {
            dTimer.Stop();
            byte[] bufferDTO = new byte[512];
            mainSocket.Receive(bufferDTO);
            currentDiffs = JsonConvert.DeserializeObject<DTO>(Convert.ToString(bufferDTO));
            mCursor2Pos.X += currentDiffs.DiffX;
            mCursor2Pos.Y += currentDiffs.DiffY;
            Point tmpMousePos = GetMousePosition();
            SetCursorPos((int)mCursor2Pos.X, (int)mCursor2Pos.Y);
            System.Threading.Thread.Sleep(17);
            SetCursorPos((int)tmpMousePos.X, (int)tmpMousePos.Y);
            dTimer.Start();
        }

        private void MainWin_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.NumPad1: NamePlaceholder(); break;
                case Key.NumPad2: NamePlaceholder2(); break;
                case Key.Escape: dTimer.Stop(); mainSocket.Disconnect(true); break;
            }
        }

        private void UpdateCursorPos(DTO diffsDTO)
        {
            Point mouseP = GetMousePosition();
            SetCursorPos((int)mouseP.X + diffsDTO.DiffX, (int)mouseP.Y + diffsDTO.DiffY);
        }

        private void buttonHost_Click(object sender, RoutedEventArgs e)
        {
            NamePlaceholder();
        }

        private void buttonClient_Click(object sender, RoutedEventArgs e)
        {
            NamePlaceholder2();
        }

        private void MainWin_MouseMove(object sender, MouseEventArgs e)
        {
            if (mainSocket.Connected)
            {
                Point mouseP = GetMousePosition();
                currentDiffs = new DTO((int)(mouseP.X - screenCenter.X), (int)(mouseP.Y - screenCenter.Y));
                mainSocket.Send(ASCIIEncoding.ASCII.GetBytes(currentDiffs.ReturnJSONString()));
            }
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
