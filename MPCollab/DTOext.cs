using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCollab
{
    struct DTOext
    {
        //Fields:
        private bool copy, paste;
        private uint type; //text,base64Image,fileDrop
        private string clipboardData;


        //Constructors:
        public DTOext(bool copy, bool paste, uint type, string clipboardData)
        {
            this.copy = copy;
            this.paste = paste;
            this.type = type;
            this.clipboardData = clipboardData;
        }
        public DTOext(uint type, string clipboardData) : this(false, false, type, clipboardData) { }

        // Properties:
        public bool Copy { get { return copy; } }
        public bool Paste { get { return paste; } }
        public uint Type { get { return type; } }
        public string ClipboardData { get { return clipboardData; } }

        public string SerializePSONString()
        {
            return string.Format("{0};{1};{2};{3}", copy, paste, type, clipboardData); ;
        }

        public static DTOext DeserializeDTOextToObject(string copy, string paste, string type, string clipboardData)
        {
            return new DTOext(Convert.ToBoolean(copy), Convert.ToBoolean(paste), Convert.ToUInt32(type), clipboardData);
        }

        public static DTOext DeserializeDTOextToObject(string input)
        {
            string[] tmpT = input.Split(';');
            bool copy, paste;
            uint type;
            string clipboardData = "";
            copy = Convert.ToBoolean(tmpT[0]);
            paste = Convert.ToBoolean(tmpT[1]);
            type = Convert.ToUInt32(tmpT[2]);
            for (int i = 3; i < tmpT.Length; i++)
            {
                clipboardData += tmpT[i];
            }

            return new DTOext(copy, paste, type, clipboardData);
        }
    }
}
