using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MPCollab
{
    class ClipboardManagerImpl : IClipboardManager
    {
        DataObject clipboardTmp;

        public ClipboardManagerImpl(DataObject clipboardTmp)
        {
            this.clipboardTmp = clipboardTmp;
        }

        //For furture purposes
        public DataObject ClipboardTmp
        {
            get { return clipboardTmp; }
        }
        
        public void CopyClipboard()
        {
            if (Clipboard.ContainsAudio())
            {
                MemoryStream tmp = new MemoryStream();
                Clipboard.GetAudioStream().CopyToAsync(tmp);
                clipboardTmp.SetAudio(tmp); 
            }
            if (Clipboard.ContainsFileDropList())
            {
                clipboardTmp.SetFileDropList(Clipboard.GetFileDropList());
            }
            if (Clipboard.ContainsImage())
            {
                clipboardTmp.SetImage(Clipboard.GetImage());
            }
            if (Clipboard.ContainsText())
            {
                clipboardTmp.SetText(Clipboard.GetText());
            }
        }

        public void PasteClipboard()
        {
            if (clipboardTmp.ContainsAudio())
            {
                Clipboard.SetAudio(clipboardTmp.GetAudioStream());
            }
            if (clipboardTmp.ContainsFileDropList())
            {
                Clipboard.SetFileDropList(clipboardTmp.GetFileDropList());
            }
            if (clipboardTmp.ContainsImage())
            {
                Clipboard.SetImage(clipboardTmp.GetImage());
            }
            if (clipboardTmp.ContainsText())
            {
                Clipboard.SetText(clipboardTmp.GetText());
            }
        }
    }
}
