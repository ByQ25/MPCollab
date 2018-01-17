using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;

namespace MPCollab
{
    class DTOHandlerImpl : IDTOHandler
    {
        // Fields:
        private int timeWin;
        private bool disposed;
        private Stopwatch stoper;
        private BinaryReader bReader;
        private BinaryWriter bWriter;
        private BinaryFormatter bFormatter;

        // Constructors:
        public DTOHandlerImpl(BinaryReader bReader, BinaryWriter bWriter, int timeWin)
        {
            this.timeWin = timeWin;
            this.disposed = false;
            this.bReader = bReader;
            this.bWriter = bWriter;
            this.bFormatter = new BinaryFormatter();
            this.stoper = new Stopwatch();
        }

        public DTOHandlerImpl(BinaryReader bReader, BinaryWriter bWriter)
            : this(bReader, bWriter, 17) { }

        // Methods:
        public void SendDTO(object dto)
        {
            stoper.Reset();
            stoper.Start();
            // Sending BinaryDTO via stream from TCPClientSocket:
            if (dto is DTO) bFormatter.Serialize(bWriter.BaseStream, (DTO)dto);
            else if (dto is DTOext) bFormatter.Serialize(bWriter.BaseStream, (DTOext)dto);
            else throw new DTOHandlerException("Not supported DTO type.");
            bWriter.Flush();
            // TODO: We should add a try{} catch{} here.
            if (!bReader.ReadBoolean()) throw new DTOHandlerException("False acknowledgement received from the server.");
            stoper.Stop();
            int dt = Convert.ToInt32(stoper.ElapsedMilliseconds);
            Thread.Sleep(dt < timeWin + 1 ? timeWin - dt : 0);
        }

        public object UnpackDTO()
        {
            if (bReader != null)
            {
                // Receiving binary serialized DTO via stream from TCPClientSocket:
                object dto = bFormatter.Deserialize(bReader.BaseStream);

                // Sending acknowledgement:
                bWriter.Write(true);

                return dto;
            }
            return null;
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
                    if (bReader != null)
                    {
                        bReader.Close();
                        bReader.Dispose();
                        bReader = null;
                    }
                    if (bWriter != null)
                    {
                        bWriter.Close();
                        bWriter.Dispose();
                        bWriter = null;
                    }
                }
            }
            this.disposed = true;
        }

        [Serializable]
        public class DTOHandlerException : ApplicationException
        {
            public DTOHandlerException(string msg) : base(msg) { }
        }
    }
}
