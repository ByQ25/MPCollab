using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCollab
{
    interface ITwoCursorsHandler : IDisposable
    {
        string ClientIP { get; }
        bool ConnectionEstablished { get; }
        DTOext ReceivedClipboard { get; }
        object ThreadLock4Field { get; }
        bool PasteField { get; set; }
        void HandleMouseMove();
        void HandleMouseClick(TwoCursorsHandler.MButtons mb);
        void HandlePaste();
        void MakeConnection();
        void StartServer();
        void StopServer();
    }
}
