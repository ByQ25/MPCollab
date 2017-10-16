using Newtonsoft.Json;

namespace MPCollab
{
    public struct DTO
    {
        // Fields:
        private int diffX, diffY;
        private bool clickLPM, clickPPM;

        // Constructors:
        [JsonConstructor]
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

        public string ReturnJSONString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
