namespace DocuFlow.Domain.Enums;

public enum DocumentCategory
{
    General = 0,             // prebuilt-layout
    Invoice = 1,             // prebuilt-invoice
    Receipt = 2,             // prebuilt-receipt
    Identity = 3,            // prebuilt-idDocument
    TextExtraction = 4,      // prebuilt-read (OCR & Handwriting)
    Summary = 5              // prebuilt-read + LLM (Future)
}
