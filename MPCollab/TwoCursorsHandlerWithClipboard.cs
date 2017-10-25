using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Diagnostics;
using System.Threading;

namespace MPCollab
{
    //Na razie na to nie patrz xD
    class TwoCursorsHandlerWithClipboard
    {
        private Point mCursor2Pos, screenCenter;
        private Stopwatch stoper1, stoper2, stoper3, stoper4;
        private TcpListener serverSocket, serverSocketClipboard;
        private TcpClient clientSocket, clientSocketClipboard;
        private BinaryReader bReader, bReaderExt;
        private BinaryWriter bWriter, bWriterExt;
        private Thread curSwitcher, serverRunner;
        private DTO currentDiffs;
        private DTOext clipboard;
        private int timeWin;
        private bool disposed, runServer, switchCursors, hostOrClient, clickLMB, clickRMB, copy, paste;
        private object threadLock1, threadLock2, threadLock3, threadLock4, threadLock5;
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
        public const int X = 0x58;

        const uint CF_UNICODETEXT = 13; 
        const uint CF_BITMAP = 2;
        const uint CF_TEXT = 1;
        const uint CF_HDROP = 15;

        public TwoCursorsHandlerWithClipboard(string ip, int timeWin, bool hostOrClient)
        {
            this.timeWin = timeWin;
            this.disposed = false;
            this.runServer = false;
            this.switchCursors = false;
            this.hostOrClient = hostOrClient;
            this.clickLMB = false;
            this.clickRMB = false;
            this.copy = false;
            this.paste = false;
            this.threadLock1 = new object();
            this.threadLock2 = new object();
            this.threadLock3 = new object();
            this.threadLock4 = new object();
            this.threadLock5 = new object();
            this.stoper1 = new Stopwatch();
            this.stoper2 = new Stopwatch();
            //mCursor2Pos = PointToScreen(Mouse.GetPosition(this));
            mCursor2Pos = GetMousePosition();
            screenCenter = new Point((int)SystemParameters.PrimaryScreenWidth / 2, (int)SystemParameters.PrimaryScreenHeight / 2);
            //mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            if (hostOrClient)
            {
                this.serverSocket = new TcpListener(IPAddress.Parse(ip), 6656);
                this.serverSocketClipboard = new TcpListener(IPAddress.Parse(ip), 6657);
                this.serverSocket.Start();
                this.clientSocket = serverSocket.AcceptTcpClient();
                this.clientSocketClipboard = serverSocketClipboard.AcceptTcpClient();
                this.bReader = new BinaryReader(clientSocket.GetStream());
                this.bWriter = new BinaryWriter(clientSocket.GetStream());
                this.bReaderExt = new BinaryReader(clientSocketClipboard.GetStream());
                this.bWriterExt = new BinaryWriter(clientSocketClipboard.GetStream());
            }
            else
            {
                this.clientSocket = new TcpClient(ip, 6656);
                this.clientSocketClipboard = new TcpClient(ip, 6657);
                this.bReader = new BinaryReader(clientSocket.GetStream());
                this.bWriter = new BinaryWriter(clientSocket.GetStream());
                this.bReaderExt = new BinaryReader(clientSocketClipboard.GetStream());
                this.bWriterExt = new BinaryWriter(clientSocketClipboard.GetStream());
                SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
            }
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetClipboardData(uint uFormat);
        [DllImport("user32.dll")]
        static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool CloseClipboard();
        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

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
                // Sending JSON via stream in TCPClientSocket:
                try { SendDTO(bReader, bWriter, currentDiffs); }
                catch { }
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

        public void HandleCopyPaste(Options o)
        {

            if (clientSocketClipboard != null && clientSocketClipboard.Connected && !hostOrClient)
            {
                switch (o)
                {
                    case Options.COPY:
                        SendDTOext(bReaderExt, bWriterExt, new DTOext(true, false, 0, ""));
                        break;
                    case Options.PASTE:
                        SendDTOext(bReaderExt, bWriterExt, new DTOext(false, true, 0, ""));
                        break;
                    default:
                        break;
                }
            }
        }

        // Entry point for GUI or other calling class:
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
                    UnpackDTO();
                    UnpackDTOExt();
                }
                catch { StopServer(); }
                stoper1.Stop();
                dt = Convert.ToInt32(stoper1.ElapsedMilliseconds);
                Thread.Sleep(dt < timeWin + 1 ? timeWin - dt : 0);
            }
        }

        public void StopServer()
        {
            lock (threadLock1) { this.switchCursors = false; }
            lock (threadLock2) { this.runServer = false; }
            if (curSwitcher != null && curSwitcher.IsAlive) curSwitcher.Join();
            if (serverRunner != null && serverRunner.IsAlive) serverRunner.Abort();
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
                    if (copy)
                    {

                    }
                    if (paste)
                    {

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
            // Sending PSON via stream from TCPClientSocket:
            bWriter.Write(dto.SerializePSONString());
            if (!bReader.ReadBoolean()) throw new TCHException("False acknowledgement received from the server.");
            stoper2.Stop();
            int dt = Convert.ToInt32(stoper2.ElapsedMilliseconds);
            Thread.Sleep(dt < timeWin + 1 ? timeWin - dt : 0);
        }
        private void SendDTOext(BinaryReader bReader, BinaryWriter bWriter, DTOext dto)
        {
            stoper4.Reset();
            stoper4.Start();
            // Sending PSON via stream from TCPClientSocket:
            bWriter.Write(dto.SerializePSONString());
            if (!bReader.ReadBoolean()) throw new TCHException("False acknowledgement received from the server.");
            stoper4.Stop();
            int dt = Convert.ToInt32(stoper2.ElapsedMilliseconds);
            Thread.Sleep(dt < timeWin + 1 ? timeWin - dt : 0);
        }

        private void UnpackDTO()
        {
            if (bReader != null)
            {
                // Receiving JSON via stream from TCPClientSocket:
                string tmp = "";
                tmp = bReader.ReadString();
                currentDiffs = tmp != "" ? DTO.DeserializeDTOObject(tmp) : new DTO(0, 0);

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
                bWriter.Write(true);
            }
        }

        private void UnpackDTOExt()
        {
            if (bReaderExt != null)
            {
                // Receiving JSON via stream from TCPClientSocket:
                string tmp = "";
                tmp = bReaderExt.ReadString();
                clipboard = tmp != "" ? DTOext.DeserializeDTOextObject(tmp) : new DTOext(0,"");

                

                // Copy/paste handling:
                if (clipboard.Copied)
                    lock (threadLock5) { this.copy = true; }
                if (clipboard.Pasted)
                    lock (threadLock5) { this.paste = true; }

                // Sending acknowledgement:
                bWriterExt.Write(true);
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
                    clientSocketClipboard.Close();
                    clientSocket = null;
                    clientSocketClipboard = null;
                    if (serverSocket != null)
                    {
                        serverSocket.Stop();
                        serverSocket = null;
                    }
                    if (serverSocketClipboard != null)
                    {
                        serverSocketClipboard.Stop();
                        serverSocketClipboard = null;
                    }
                    bReader.Close();
                    bReaderExt.Close();
                    bReader.Dispose();
                    bReaderExt.Dispose();
                    bReader = null;
                    bReaderExt = null;
                    bWriter.Close();
                    bWriterExt.Close();
                    bWriter.Dispose();
                    bWriterExt.Dispose();
                    bWriter = null;
                    bWriterExt = null;

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
        public enum MButtons { LMB, MMB, RMB };
        public enum Options { COPY, PASTE};

        [Serializable]
        public class TCHException : ApplicationException
        {
            public TCHException(string msg) : base(msg) { }
        }


    }
}
