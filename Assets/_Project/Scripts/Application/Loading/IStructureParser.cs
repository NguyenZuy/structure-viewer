namespace StructureViewer.Application.Loading
{
    public interface IStructureParser
    {
        // Never throws on bad input; problems are reported in ParseResult.Errors.
        ParseResult Parse(string json);
    }
}
