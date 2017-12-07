using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Specialized;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization;


namespace MPCollab
{
    [Serializable]
    public struct DTOext : ISerializable
    {
        //Fields:
        private MemoryStream audio;
        private string text;
        private StringCollection fileDropList;
        private BitmapSource image;

        public DTOext(MemoryStream audio, string text, StringCollection fileDropList, BitmapSource image)
        {
            this.audio = audio;
            this.text = text;
            this.fileDropList = fileDropList;
            this.image = image;
        }
        public DTOext(MemoryStream audio):this(audio,"",new StringCollection(),null) { }
        public DTOext(string text) : this(new MemoryStream(), text, new StringCollection(), null) { }
        public DTOext(StringCollection fileDropList) : this(new MemoryStream(), "", fileDropList, null) { }
        public DTOext(BitmapSource image) : this(new MemoryStream(), "", new StringCollection(), image) { }
        private DTOext(SerializationInfo info, StreamingContext context) : this((MemoryStream)info.GetValue("Audio", Type.GetType("MemoryStream")), info.GetString("Text"), (StringCollection)info.GetValue("FileDropList", Type.GetType("StringCollection")), (BitmapSource)info.GetValue("Image", Type.GetType("BitmapSource"))) { }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Audio", audio,Type.GetType("Memorystream"));
            info.AddValue("Text", text);
            info.AddValue("FileDropList", fileDropList, Type.GetType("StringCollection"));
            info.AddValue("Image", image, Type.GetType("BitmapSource"));
            
        }
    }
}
