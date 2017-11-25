using System;
using System.Runtime.Serialization;

namespace MPCollab
{
    [Serializable]
    public struct DTO : ISerializable
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
        private DTO(SerializationInfo info, StreamingContext context) :
            this(info.GetInt32("DiffX"),
                info.GetInt32("DiffY"),
                info.GetBoolean("LPMClicked"),
                info.GetBoolean("PPMClicked")) { }

        // Properties:
        public int DiffX { get { return diffX; } }
        public int DiffY { get { return diffY; } }
        public bool LPMClicked { get { return clickLPM; } }
        public bool PPMClicked { get { return clickPPM; } }

        // ISerializable implementation:
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("DiffX", diffX);
            info.AddValue("DiffY", diffX);
            info.AddValue("LPMClicked", clickLPM);
            info.AddValue("PPMClicked", clickPPM);
        }
    }
}
