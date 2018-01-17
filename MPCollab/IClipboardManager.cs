using System;

namespace MPCollab
{
    interface IClipboardManager
    {
        [STAThread]
        void CopyClipboard();
        [STAThread]
        void PasteClipboard();
        [STAThread]
        DTOext ExportClipboardToDTOext(bool paste);
        [STAThread]
        void ImportDTOext(DTOext ext);
    }
}
