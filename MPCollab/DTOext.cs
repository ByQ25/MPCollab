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
            this.image = SourceFromBitmap((System.Drawing.Bitmap)info.GetValue("Image", typeof(System.Drawing.Bitmap)));
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
            info.AddValue("Image", BitmapFromSource(image),typeof(System.Drawing.Bitmap));
            info.AddValue("Paste", paste);
        }
        private System.Drawing.Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            if (bitmapsource != null)
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    BitmapEncoder enc = new BmpBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                    enc.Save(outStream);
                    bitmap = new System.Drawing.Bitmap(outStream);
                }
            }
            else
            {
                bitmap = new System.Drawing.Bitmap(1, 1);
            }
            
            return bitmap;
        }
        private BitmapSource SourceFromBitmap(System.Drawing.Bitmap bitmap)
        {
            byte[] array;
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                array = stream.ToArray();
            }

            var img = new BitmapImage();
            using (MemoryStream outStream = new MemoryStream(array))
            {
                img.BeginInit();
                img.StreamSource = outStream;
                img.EndInit();
            }
            return img;
        }
    }
}
