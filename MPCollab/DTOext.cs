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
        private bool paste;

        public DTOext(MemoryStream audio, string text, StringCollection fileDropList, BitmapSource image, bool paste)
        {
            this.audio = audio;
            this.text = text;
            this.fileDropList = fileDropList;
            this.image = image;
            this.paste = paste;
        }
        public DTOext(MemoryStream audio):this(audio,"",new StringCollection(),null,false) { }
        public DTOext(string text) : this(new MemoryStream(), text, new StringCollection(), null, false) { }
        public DTOext(StringCollection fileDropList) : this(new MemoryStream(), "", fileDropList, null,false) { }
        public DTOext(BitmapSource image) : this(new MemoryStream(), "", new StringCollection(), image, false) { }
        private DTOext(SerializationInfo info, StreamingContext context)
        {
            this.audio = (MemoryStream)info.GetValue("Audio", typeof(MemoryStream));
            this.text = info.GetString("Text");
            this.fileDropList = (StringCollection)info.GetValue("FileDropList", typeof(StringCollection));
            this.image = new BitmapImage();
            this.paste = info.GetBoolean("Paste");
            this.image = Base64ToImage(info.GetString("Image"));
        }

        public MemoryStream Audio { get { return audio; } }
        public string Text { get { return text; } }
        public StringCollection FileDropList { get { return fileDropList; } }
        public BitmapSource Image { get { return image; } }
        public bool Paste { get { return paste; } }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Audio", audio, typeof(MemoryStream));
            info.AddValue("Text", text);
            info.AddValue("FileDropList", fileDropList, typeof(StringCollection));
            //info.AddValue("Image", image, typeof(BitmapSource));
            info.AddValue("Image", ImageToBase64(image));
            info.AddValue("Paste", paste);
        }
        

        string ImageToBase64(BitmapSource bitmap)
        {
            var encoder = new PngBitmapEncoder();
            var frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        BitmapSource Base64ToImage(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using (var stream = new MemoryStream(bytes))
            {
                return BitmapFrame.Create(stream);
            }
        }
    }
}
