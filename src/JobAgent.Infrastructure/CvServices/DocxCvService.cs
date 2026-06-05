using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JobAgent.Application.Cv.Interfaces;

namespace JobAgent.Infrastructure.CvServices;

public class DocxCvService : ICvDocxService
{
    public string ReadText(string docxPath)
    {
        using var doc = WordprocessingDocument.Open(docxPath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var para in body.Elements<Paragraph>())
        {
            sb.AppendLine(para.InnerText);
        }
        return sb.ToString();
    }

    public void WriteText(string docxPath, string outputPath, string tailoredContent)
    {
        File.Copy(docxPath, outputPath, overwrite: true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null)
            return;

        // Replace the body content with tailored content
        body.RemoveAllChildren<Paragraph>();

        foreach (var line in tailoredContent.Split('\n'))
        {
            var para = new Paragraph(new Run(new Text(line)));
            body.AppendChild(para);
        }

        doc.MainDocumentPart!.Document.Save();
    }
}
