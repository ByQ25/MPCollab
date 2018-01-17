using System;

namespace MPCollab
{
    interface IDTOHandler : IDisposable
    {
        void SendDTO(object dto);
        object UnpackDTO();
    }
}
