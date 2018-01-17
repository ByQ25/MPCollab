using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Diagnostics;
using System.Threading;

namespace MPCollab
{
    class TwoCursorsHandlerImpl : ITwoCursorsHandler
    {
        private Point mHostCursor1Pos, mCursor2Pos, screenCenter;
        private Stopwatch stoper1;
        private TcpListener serverSocket;
        private TcpClient clientSocket;
        private Thread curSwitcher, serverRunner;
        private DTO currentDiffs;
        private DTOext receivedClipboard;
        private IDTOHandler DTOHandler;
        private IClipboardManager clipboard;
        private int timeWin;
        private string clientIP;
        private object[] threadLocks;
        private bool disposed, runServer, switchCursors, hostOrClient, clickLMB, clickRMB, paste, connEstablished;
        private const int MOUSEEVENT_K_LEFTDOWN = 0x02;
        private const int MOUSEEVENT_K_LEFTUP = 0x04;
        private const int MOUSEEVENT_K_RIGHTDOWN = 0x08;
        private const int MOUSEEVENT_K_RIGHTUP = 0x10;
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        public const int VK_LCONTROL = 0xA2; //Left Control key code
        public const int A = 0x41;
        public const int C = 0x43;
        public const int V = 0x56;
        public string ClientIP
        {
            get { return clientIP; }
        }
        public bool ConnectionEstablished
        {
            get { return connEstablished; }
        }
        public DTOext ReceivedClipboard
        {
            get { return receivedClipboard; }
        }
        public object ThreadLock4Field
        {
            get { return threadLocks[3]; }
        }
        public bool PasteField
        {
            get { return paste; }
            set { this.paste = value; }
        }

        public TwoCursorsHandlerImpl(string ip, int timeWin, bool hostOrClient)
        {
            this.timeWin = timeWin;
            this.disposed = false;
            this.runServer = false;
            this.switchCursors = false;
            this.hostOrClient = hostOrClient;
            this.clickLMB = false;
            this.clickRMB = false;
            this.paste = false;
            this.threadLocks = new object[4];
            this.threadLocks[0] = new object();
            this.threadLocks[1] = new object();
            this.threadLocks[2] = new object();
            this.threadLocks[3] = new object();
            this.stoper1 = new Stopwatch();
            this.clipboard = new ClipboardManagerImpl(new DataObject());
            mHostCursor1Pos = mCursor2Pos = GetMousePosition();
            screenCenter = new Point((int)SystemParameters.PrimaryScreenWidth / 2, (int)SystemParameters.PrimaryScreenHeight / 2);
            if (this.hostOrClient) this.serverSocket = new TcpListener(IPAddress.Parse(ip), 6656);
            else this.clientIP = ip;
        }

        public static Point GetMousePosition()
        {
            NativeMethods.Win32Point w32Mouse = new NativeMethods.Win32Point();
            NativeMethods.GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        } 

        public void HandleMouseMove()
        {
            if (clientSocket != null && clientSocket.Connected && !hostOrClient)
            {
                Point mouseP = GetMousePosition();
                currentDiffs = new DTO((int)(mouseP.X - screenCenter.X), (int)(mouseP.Y - screenCenter.Y));
                // Sending serialized object via stream in TCPClientSocket:
                try { DTOHandler.SendDTO(currentDiffs); }
                catch { }
                NativeMethods.SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
            }
        }

        public void HandleMouseClick(Enums.MButtons mb)
        {
            if (clientSocket != null && clientSocket.Connected && !hostOrClient)
                switch (mb)
                {
                    case Enums.MButtons.LMB: DTOHandler.SendDTO(new DTO(0, 0, true, false)); break;
                    case Enums.MButtons.RMB: DTOHandler.SendDTO(new DTO(0, 0, false, true)); break;
                }
        }

        public void HandlePaste()
        {
            if (clientSocket != null && clientSocket.Connected && !hostOrClient)
                DTOHandler.SendDTO(clipboard.ExportClipboardToDTOext(true));
        }

        // Entry point for GUI or other calling class:
        public void MakeConnection()
        {
            if (this.hostOrClient)
            {
                this.serverSocket.Start();
                while (!serverSocket.Pending()) Thread.Sleep(500);
                this.clientSocket = serverSocket.AcceptTcpClient();
                this.clientIP = clientSocket.Client.RemoteEndPoint.ToString();
                this.connEstablished = true;
            }
            else
            {
                // Creating a ClientSocket also connects to the specified IP. clientSocket.Connect()
                try { this.clientSocket = new TcpClient(clientIP, 6656); this.connEstablished = true; }
                catch { this.connEstablished = false; }
                NativeMethods.SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
            }
            if (this.connEstablished)
            {
                this.DTOHandler = new DTOHandlerImpl(
                    new BinaryReader(clientSocket.GetStream()),
                    new BinaryWriter(clientSocket.GetStream()));
            }
        }
        
        public void StartServer()
        {
            if (!this.runServer)
            {
                curSwitcher = new Thread(SwitchCursors);
                curSwitcher.IsBackground = true;
                curSwitcher.Start();
                serverRunner = new Thread(RunServer);
                serverRunner.IsBackground = true;
                serverRunner.Start();
            }
        }

        private void RunServer()
        {
            int dt;
            this.runServer = true;
            while (runServer)
            {
                stoper1.Reset();
                stoper1.Start();
                try
                {
                    object dto = DTOHandler.UnpackDTO();
                    if (dto is DTO) UpdateGuestCursorPosition((DTO)dto);
                    else if (dto is DTOext) UpdateGuestClipboard((DTOext)dto);
                }
                catch { StopServer(); }
                stoper1.Stop();
                dt = Convert.ToInt32(stoper1.ElapsedMilliseconds);
                Thread.Sleep(dt < timeWin + 1 ? timeWin - dt : 0);
            }
        }

        public void StopServer()
        {
            lock (threadLocks[0]) { this.switchCursors = false; }
            lock (threadLocks[1]) { this.runServer = false; }
            if (curSwitcher != null && curSwitcher.IsAlive) curSwitcher.Join();
            if (serverRunner != null && serverRunner.IsAlive) serverRunner.Abort();
        }

        public void Paste()
        {
            if (this.paste && hostOrClient)
            {
                NativeMethods.keybd_event(VK_LCONTROL, 0, KEYEVENTF_EXTENDEDKEY, (IntPtr)0);
                NativeMethods.keybd_event(V, 0, KEYEVENTF_EXTENDEDKEY, (IntPtr)0);
                NativeMethods.keybd_event(V, 0, KEYEVENTF_KEYUP, (IntPtr)0);
                NativeMethods.keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, (IntPtr)0);
                lock (threadLocks[3]) { this.paste = false; }
            }
        }

        private void SwitchCursors()
        {
            if (!this.switchCursors)
            {
                this.switchCursors = true;
                Point tmpMousePos;
                Point secondCursorPos;
                while (switchCursors)
                {
                    tmpMousePos = GetMousePosition();
                    if (ComputeDistance(mHostCursor1Pos, tmpMousePos) < 128.0)
                        mHostCursor1Pos = tmpMousePos;
                    lock (threadLocks[0]) { secondCursorPos = mCursor2Pos; }
                    if (tmpMousePos.X != secondCursorPos.X && tmpMousePos.Y != secondCursorPos.Y)
                        NativeMethods.SetCursorPos((int)mCursor2Pos.X, (int)mCursor2Pos.Y);
                    if (clickLMB)
                    {
                        NativeMethods.mouse_event(MOUSEEVENT_K_LEFTDOWN, (int)mCursor2Pos.X, (int)mCursor2Pos.Y, 0, (IntPtr)0);
                        NativeMethods.mouse_event(MOUSEEVENT_K_LEFTUP, (int)mCursor2Pos.X, (int)mCursor2Pos.Y, 0, (IntPtr)0);
                        lock (threadLocks[2]) { this.clickLMB = false; }
                    }
                    if (clickRMB)
                    {
                        NativeMethods.mouse_event(MOUSEEVENT_K_RIGHTDOWN, (int)mCursor2Pos.X, (int)mCursor2Pos.Y, 0, (IntPtr)0);
                        NativeMethods.mouse_event(MOUSEEVENT_K_RIGHTUP, (int)mCursor2Pos.X, (int)mCursor2Pos.Y, 0, (IntPtr)0);
                        lock (threadLocks[2]) { this.clickRMB = false; }
                    }
                    Thread.Sleep(timeWin);
                    NativeMethods.SetCursorPos((int)mHostCursor1Pos.X, (int)mHostCursor1Pos.Y);
                    Thread.Sleep(timeWin);
                }
            }
        }

        private void UpdateGuestCursorPosition(DTO dto)
        {
            currentDiffs = dto;

            // Updating second cursor position in critical section:
            lock (threadLocks[0])
            {
                mCursor2Pos.X += currentDiffs.DiffX;
                mCursor2Pos.Y += currentDiffs.DiffY;
            }

            // Mouse clicks handling:
            if (currentDiffs.LPMClicked)
                lock (threadLocks[2]) { this.clickLMB = true; }
            if (currentDiffs.PPMClicked)
                lock (threadLocks[2]) { this.clickRMB = true; }
        }

        private void UpdateGuestClipboard(DTOext ext)
        {
            receivedClipboard = ext;

            if (receivedClipboard.Paste)
                lock (threadLocks[3]) { this.paste = true; }
        }

        private double ComputeDistance(Point p1, Point p2)
        {
            double dX = p2.X - p1.X, dY = p2.Y - p1.Y;
            return Math.Sqrt(dX * dX + dY * dY);
        }

        // IDisposable implementation:
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (clientSocket != null)
                    {
                        clientSocket.Close();
                        clientSocket = null;
                    }
                    if (serverSocket != null)
                    {
                        serverSocket.Stop();
                        serverSocket = null;
                    }
                    if (DTOHandler != null)
                    {
                        DTOHandler.Dispose();
                        DTOHandler = null;
                    }
                    if (curSwitcher != null && curSwitcher.IsAlive)
                    {
                        curSwitcher.Abort();
                        curSwitcher = null;
                    }
                    if (serverRunner != null && serverRunner.IsAlive)
                    {
                        serverRunner.Abort();
                        serverRunner = null;
                    }
                }
            }
            this.disposed = true;
        }

        // Public area:
        [Serializable]
        public class TCHException : ApplicationException
        {
            public TCHException(string msg) : base(msg) { } 
        }
    }
}
