using System;

namespace MPCollab
{
    interface ITwoCursorsHandler : IDisposable
    {
        string ClientIP { get; }
        bool ConnectionEstablished { get; }
        bool IsConnectionAlive { get; }
        DTOext ReceivedClipboard { get; }
        object ThreadLock4Field { get; }
        bool PasteField { get; set; }
        void HandleMouseMove();
        void HandleMouseClick(Enums.MButtons mb);
        void HandlePaste();
        void MakeConnection();
        void StartServer();
        void StopServer();
    }
}
