namespace MPCollab
{
    interface IClipboardManager
    {
        void CopyClipboard();
        void PasteClipboard();
        DTOext ExportClipboardToDTOext(bool paste);
        void ImportDTOext(DTOext ext);
    }
}
