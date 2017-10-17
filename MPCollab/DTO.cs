using Newtonsoft.Json;
using System;

namespace MPCollab
{
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

        public static DTO DeserializeDTOObject(int diffX, int diffY, bool clickLPM, bool clickPPM)
        {
            return new DTO(diffX, diffY, clickLPM, clickPPM);
        }

        public static DTO DeserializeDTOObject(string diffXS, string diffYS, string clickLPMS, string clickPPMS)
        {
            int diffX = Convert.ToInt32(diffXS);
            int diffY = Convert.ToInt32(diffYS);
            bool clickLPM = Convert.ToBoolean(clickLPMS);
            bool clickPPM = Convert.ToBoolean(clickPPMS);
            return new DTO(diffX, diffY, clickLPM, clickPPM);
        }

        public static DTO DeserializeDTOObject(string input)
        {
            string[] args = input.Split(';');
            int diffX = Convert.ToInt32(args[0]);
            int diffY = Convert.ToInt32(args[1]);
            bool clickLPM = Convert.ToBoolean(args[2]);
            bool clickPPM = Convert.ToBoolean(args[3]);
            return new DTO(diffX, diffY, clickLPM, clickPPM);
        }

        public string SerializePSONString()
        {
            return string.Format("{0};{1};{2};{3}", diffX, diffY, clickLPM, clickPPM);
        }
    }
}
