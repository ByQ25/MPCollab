using System;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MPCollab
{
    class TwoCursorsHandler : IDisposable
    {
        private Point mCursor2Pos, screenCenter;
        private Stopwatch stoper1, stoper2;
        private TcpListener serverSocket;
        private TcpClient clientSocket;
        private BinaryReader bReader;
        private BinaryWriter bWriter;
        private Thread curSwitcher;
        private DTO currentDiffs;
        private bool disposed, runServer, switchCursors, hostOrClient, clickLMB, clickRMB;
        private int timeWin;
        private object threadLock1, threadLock2, threadLock3;
        private const int MOUSEEVENT_K_LEFTDOWN = 0x02;
        private const int MOUSEEVENT_K_LEFTUP = 0x04;
        private const int MOUSEEVENT_K_RIGHTDOWN = 0x08;
        private const int MOUSEEVENT_K_RIGHTUP = 0x10;

        public TwoCursorsHandler(string ip, int timeWin, bool hostOrClient)
        {
            this.disposed = false;
            this.runServer = false;
            this.switchCursors = false;
            this.hostOrClient = hostOrClient;
            this.clickLMB = false;
            this.clickRMB = false;
            this.timeWin = timeWin;
            this.threadLock1 = new object();
            this.threadLock2 = new object();
            this.threadLock3 = new object();
            this.stoper1 = new Stopwatch();
            this.stoper2 = new Stopwatch();
            //mCursor2Pos = PointToScreen(Mouse.GetPosition(this));
            mCursor2Pos = GetMousePosition();
            screenCenter = new Point((int)SystemParameters.PrimaryScreenWidth / 2, (int)SystemParameters.PrimaryScreenHeight / 2);
            //mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            if (hostOrClient)
            {
                this.serverSocket = new TcpListener(IPAddress.Parse(ip), 6656);
                this.serverSocket.Start();
                this.clientSocket = serverSocket.AcceptTcpClient();
                this.bReader = new BinaryReader(clientSocket.GetStream());
                this.bWriter = new BinaryWriter(clientSocket.GetStream());
            }
            else
            {
                this.clientSocket = new TcpClient(ip, 6656);
                this.bReader = new BinaryReader(clientSocket.GetStream());
                this.bWriter = new BinaryWriter(clientSocket.GetStream());
                SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

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

        public void HandleMouseMove()
        {
            if (clientSocket != null && clientSocket.Connected && !hostOrClient)
            {
                Point mouseP = GetMousePosition();
                currentDiffs = new DTO((int)(mouseP.X - screenCenter.X), (int)(mouseP.Y - screenCenter.Y));
                // Sending JSON via stream from TCPClientSocket:
                SendDTO(bReader, bWriter, currentDiffs);
                SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
            }
        }

        public void HandleMouseClick(MButtons mb)
        {
            if (clientSocket != null && clientSocket.Connected && !hostOrClient)
                switch (mb)
                {
                    case MButtons.LMB: SendDTO(bReader, bWriter, new DTO(0, 0, true, false)); break;
                    case MButtons.RMB: SendDTO(bReader, bWriter, new DTO(0, 0, false, true)); break;
                }
        }

        public void StartServer()
        {
            if (!this.runServer)
            {
                int dt;
                this.runServer = true;
                curSwitcher = new Thread(SwitchCursors);
                curSwitcher.IsBackground = true;
                curSwitcher.Start();
                while (runServer)
                {
                    stoper1.Reset();
                    stoper1.Start();
                    UnpackDTO();
                    stoper1.Stop();
                    dt = Convert.ToInt32(stoper1.ElapsedMilliseconds);
                    Thread.Sleep(dt < timeWin + 1 ? timeWin - dt : 0);
                }
            }
        }

        public void StopServer()
        {
            lock (threadLock1) { this.switchCursors = false; }
            lock (threadLock2) { this.runServer = false; }
            if (curSwitcher != null && curSwitcher.IsAlive) curSwitcher.Join();
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
                    lock (threadLock1) { secondCursorPos = mCursor2Pos; }
                    SetCursorPos((int)mCursor2Pos.X, (int)mCursor2Pos.Y);
                    if (clickLMB)
                    {
                        mouse_event(MOUSEEVENT_K_LEFTDOWN, (uint)mCursor2Pos.X, (uint)mCursor2Pos.Y, 0, 0);
                        mouse_event(MOUSEEVENT_K_LEFTUP, (uint)mCursor2Pos.X, (uint)mCursor2Pos.Y, 0, 0);
                        lock (threadLock3) { this.clickLMB = false; }
                    }
                    if (clickRMB)
                    {
                        mouse_event(MOUSEEVENT_K_RIGHTDOWN, (uint)mCursor2Pos.X, (uint)mCursor2Pos.Y, 0, 0);
                        mouse_event(MOUSEEVENT_K_RIGHTUP, (uint)mCursor2Pos.X, (uint)mCursor2Pos.Y, 0, 0);
                        lock (threadLock3) { this.clickRMB = false; }
                    }
                    Thread.Sleep(timeWin);
                    SetCursorPos((int)tmpMousePos.X, (int)tmpMousePos.Y);
                    Thread.Sleep(timeWin);
                }
            }
        }

        private void SendDTO(BinaryReader bReader, BinaryWriter bWriter, DTO dto)
        {
            stoper2.Reset();
            stoper2.Start();
            // Sending JSON via stream from TCPClientSocket:
            try { bWriter.Write(dto.ReturnJSONString()); }
            catch (IOException ex)
            {
                throw new TCHException(string.Format("Error occured while sending DTO.\n\nDetails:\n{0}", ex.Message));
            }
            try { if (!bReader.ReadBoolean()) throw new TCHException("False acknowledgement received from the server."); }
            catch (Exception ex)
            {
                throw new TCHException(string.Format("Error occured while receiving acknowledgement DTO.\n\nDetails:\n{0}", ex.Message));
            }
            stoper2.Stop();
            int dt = Convert.ToInt32(stoper2.ElapsedMilliseconds);
            Thread.Sleep(dt < timeWin + 1 ? timeWin - dt : 0);
        }

        private void UnpackDTO()
        {
            if (bReader != null)
            {
                // Receiving JSON via stream from TCPClientSocket:
                string tmp = "";
                try { tmp = bReader.ReadString(); }
                catch (Exception ex) {
                    throw new TCHException(string.Format("Error occured while sending DTO.\n\nDetails:\n{0}", ex.Message));
                }
                currentDiffs = tmp != "" ? JsonConvert.DeserializeObject<DTO>(tmp) : new DTO(0, 0);

                // Updating second cursor position in critical section:
                lock (threadLock1)
                {
                    mCursor2Pos.X += currentDiffs.DiffX;
                    mCursor2Pos.Y += currentDiffs.DiffY;
                }

                // Mouse clicks handling:
                if (currentDiffs.LPMClicked)
                    lock (threadLock3) { this.clickLMB = true; }
                if (currentDiffs.PPMClicked)
                    lock (threadLock3) { this.clickRMB = true; }

                // Sending acknowledgement:
                try { bWriter.Write(true); }
                catch (IOException ex) {
                    throw new TCHException(string.Format("Error occured while sending acknowledgement DTO.\n\nDetails:\n{0}", ex.Message));
                }
            }
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
                    clientSocket.Close();
                    clientSocket = null;
                    serverSocket.Stop();
                    serverSocket = null;
                    bReader.Close();
                    bReader.Dispose();
                    bReader = null;
                    bWriter.Close();
                    bWriter.Dispose();
                    bWriter = null;
                    if (curSwitcher != null && curSwitcher.IsAlive)
                    {
                        curSwitcher.Abort();
                        curSwitcher = null;
                    }
                }
            }
            this.disposed = true;
        }

        // Public area:
        public enum MButtons{ LMB, MMB, RMB };

        [Serializable]
        public class TCHException : ApplicationException
        {
            public TCHException(string msg) : base(msg) { } 
        }
    }
}
