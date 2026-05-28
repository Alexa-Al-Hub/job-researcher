namespace JobAgent.Application.Interfaces;

public interface ICvDocxService
{
    string ReadText(string docxPath);
    void WriteText(string docxPath, string outputPath, string tailoredContent);
}
