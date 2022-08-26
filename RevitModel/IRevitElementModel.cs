namespace RevitTimasBIMTools.RevitModel
{
    public interface IRevitElementModel
    {
        string SymbolName { get; }
        string Description { get; set; }
    }
}