namespace Tests.Fixtures;

public static class PdfAssertions
{
    public static void AssertValidPdf(byte[] pdfBytes)
    {
        Assert.That(pdfBytes, Is.Not.Null.And.Not.Empty);
        Assert.That(pdfBytes[..4], Is.EqualTo("%PDF"u8.ToArray()));
    }
}
