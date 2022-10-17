namespace RevitTimasBIMTools.RevitModel
{
    public interface IElementModel
    {
        string SymbolName { get; }
        string Description { get; set; }
    }
}