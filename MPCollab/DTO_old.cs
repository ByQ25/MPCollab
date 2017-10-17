using Newtonsoft.Json;

namespace MPCollab
{
    public struct DTO_old
    {
        // Fields:
        private int diffX, diffY;
        private byte clickLPM, clickPPM;

        // Constructors:
        [JsonConstructor]
        public DTO_old(int diffX, int diffY, byte clickLPM, byte clickPPM)
        {
            this.diffX = diffX;
            this.diffY = diffY;
            this.clickLPM = clickLPM;
            this.clickPPM = clickPPM;
        }
        public DTO_old(int diffX, int diffY) : this(diffX, diffY, 0, 0) { }

        // Properties:
        public int DiffX { get { return diffX; } }
        public int DiffY { get { return diffY; } }
        public byte LPMClicked { get { return clickLPM; } }
        public byte PPMClicked { get { return clickPPM; } }

        public string ReturnJSONString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
