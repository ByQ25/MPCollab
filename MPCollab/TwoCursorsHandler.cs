using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

namespace MPCollab
{
    class TwoCursorsHandler : IDisposable
    {
        private Point mCursor2Pos, screenCenter;
        private Stopwatch stoper1, stoper2;
        private TcpListener serverSocket;
        private TcpClient clientSocket;
        private BinaryReader bReader, bReaderExt;
        private BinaryWriter bWriter, bWriterExt;
        private BinaryFormatter bFormatter;
        private Thread curSwitcher, serverRunner, pasteChecker;
        private DTO currentDiffs;
        private ClipboardManagerImpl clipboard;
        private int timeWin;
        private bool disposed, runServer, switchCursors, hostOrClient, clickLMB, clickRMB, paste;
        private object threadLock1, threadLock2, threadLock3, threadlock4;
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

        public TwoCursorsHandler(string ip, int timeWin, bool hostOrClient)
        {
            this.timeWin = timeWin;
            this.disposed = false;
            this.runServer = false;
            this.switchCursors = false;
            this.hostOrClient = hostOrClient;
            this.clickLMB = false;
            this.clickRMB = false;
            this.paste = false;
            this.threadLock1 = new object();
            this.threadLock2 = new object();
            this.threadLock3 = new object();
            this.threadlock4 = new object();
            this.stoper1 = new Stopwatch();
            this.stoper2 = new Stopwatch();
            this.clipboard = new ClipboardManagerImpl(new DataObject());
            //mCursor2Pos = PointToScreen(Mouse.GetPosition(this));
            mCursor2Pos = GetMousePosition();
            screenCenter = new Point((int)SystemParameters.PrimaryScreenWidth / 2, (int)SystemParameters.PrimaryScreenHeight / 2);
            //mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            this.bFormatter = new BinaryFormatter();
            if (hostOrClient)
            {
                this.serverSocket = new TcpListener(IPAddress.Parse(ip), 6656);
                this.serverSocket.Start();
                this.clientSocket = serverSocket.AcceptTcpClient();
            }
            else
            {
                this.clientSocket = new TcpClient(ip, 6656);
                NativeMethods.SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
            }
            this.bReader = new BinaryReader(clientSocket.GetStream());
            this.bWriter = new BinaryWriter(clientSocket.GetStream());
            this.bReaderExt = new BinaryReader(clientSocket.GetStream());
            this.bWriterExt = new BinaryWriter(clientSocket.GetStream());
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
                // Sending JSON via stream in TCPClientSocket:
                try { SendDTO(bReader, bWriter, currentDiffs); }
                catch { }
                NativeMethods.SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
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

        // TODO We should probably delete HandleCopy method
        public void HandleCopy()
        {
            //if(clientSocket != null && clientSocket.Connected && !hostOrClient)
            //{
            //    SendClipboard(bReaderExt, bWriterExt, clipboard.ExportClipboardToDTOext(false));
            //}
        }
        public void HandlePaste()
        {
            if (clientSocket != null && clientSocket.Connected && !hostOrClient)
            {
                SendClipboard(bReaderExt, bWriterExt, clipboard.ExportClipboardToDTOext(true));
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
                pasteChecker = new Thread(Paste);
                pasteChecker.IsBackground = true;
                pasteChecker.Start();
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
                    switch (bReader.ReadByte())
                    {
                        case 0: UnpackDTO(); break;
                        case 1: UnpackClipboard(); break;
                    }
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

        private void Paste()
        {
            if (this.paste && hostOrClient)
            {
                NativeMethods.keybd_event(VK_LCONTROL, 0, KEYEVENTF_EXTENDEDKEY, 0);
                NativeMethods.keybd_event(V, 0, KEYEVENTF_EXTENDEDKEY, 0);
                NativeMethods.keybd_event(V, 0, KEYEVENTF_KEYUP, 0);
                NativeMethods.keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, 0);
                lock (threadlock4) { this.paste = false; }
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
                    lock (threadLock1) { secondCursorPos = mCursor2Pos; }
                    NativeMethods.SetCursorPos((int)mCursor2Pos.X, (int)mCursor2Pos.Y);
                    if (clickLMB)
                    {
                        NativeMethods.mouse_event(MOUSEEVENT_K_LEFTDOWN, (int)mCursor2Pos.X, (int)mCursor2Pos.Y, 0, (IntPtr)0);
                        NativeMethods.mouse_event(MOUSEEVENT_K_LEFTUP, (int)mCursor2Pos.X, (int)mCursor2Pos.Y, 0, (IntPtr)0);
                        lock (threadLock3) { this.clickLMB = false; }
                    }
                    if (clickRMB)
                    {
                        NativeMethods.mouse_event(MOUSEEVENT_K_RIGHTDOWN, (int)mCursor2Pos.X, (int)mCursor2Pos.Y, 0, (IntPtr)0);
                        NativeMethods.mouse_event(MOUSEEVENT_K_RIGHTUP, (int)mCursor2Pos.X, (int)mCursor2Pos.Y, 0, (IntPtr)0);
                        lock (threadLock3) { this.clickRMB = false; }
                    }
                    Thread.Sleep(timeWin);
                    NativeMethods.SetCursorPos((int)tmpMousePos.X, (int)tmpMousePos.Y);
                    Thread.Sleep(timeWin);
                }
            }
        }

        private void SendDTO(BinaryReader bReader, BinaryWriter bWriter, DTO dto)
        {
            stoper2.Reset();
            stoper2.Start();
            // Sending BinaryDTO via stream from TCPClientSocket:
            bWriter.Write((byte)0); // DTO type info tag
            bFormatter.Serialize(bWriter.BaseStream, dto);
            bWriter.Flush();
            if (!bReader.ReadBoolean()) throw new TCHException("False acknowledgement received from the server.");
            stoper2.Stop();
            int dt = Convert.ToInt32(stoper2.ElapsedMilliseconds);
            Thread.Sleep(dt < timeWin + 1 ? timeWin - dt : 0);
        }

        // TODO: SendDTO and SendClipboard shold be merged into one method. They are basically the same.
        private void SendClipboard(BinaryReader bReaderExt, BinaryWriter bWriterExt, DTOext ext)
        {
            stoper2.Reset();
            stoper2.Start();
            // Sending BinaryDTOext via stream from TCPClientSocket:
            bWriterExt.Write((byte)1); // DTOext type info tag
            bFormatter.Serialize(bWriterExt.BaseStream, ext);
            bWriterExt.Flush();
            if (!bReaderExt.ReadBoolean()) throw new TCHException("False acknowledgement received from the server.");
            stoper2.Stop();
            int dt = Convert.ToInt32(stoper2.ElapsedMilliseconds);
            Thread.Sleep(dt < timeWin + 1 ? timeWin - dt : 0);
        }

        private void UnpackDTO()
        {
            if (bReader != null)
            {
                // Receiving binary serialized DTO via stream from TCPClientSocket:
                currentDiffs = (DTO)bFormatter.Deserialize(bReader.BaseStream);

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

        private void UnpackClipboard()
        {
            if (bReaderExt != null)
            {
                clipboard.CopyClipboard();
                DTOext tmp = (DTOext)bFormatter.Deserialize(bReaderExt.BaseStream);
                clipboard.ImportDTOext(tmp);

                if (tmp.Paste)
                    lock(threadlock4) { this.paste = true; }
                
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
                    clientSocket = null;
                    if (serverSocket != null)
                    {
                        serverSocket.Stop();
                        serverSocket = null;
                    }
                    bReader.Close();
                    bReader.Dispose();
                    bReader = null;
                    bWriter.Close();
                    bWriter.Dispose();
                    bWriter = null;
                    bReaderExt.Close();
                    bReaderExt.Dispose();
                    bReaderExt = null;
                    bWriterExt.Close();
                    bWriterExt.Dispose();
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
        public enum MButtons{ LMB, MMB, RMB };

        [Serializable]
        public class TCHException : ApplicationException
        {
            public TCHException(string msg) : base(msg) { } 
        }
    }
}
