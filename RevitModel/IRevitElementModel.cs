namespace RevitTimasBIMTools.RevitModel
{
    public interface IRevitElementModel
    {
        int IdInt { get; }
        string SymbolName { get; }
        string CategoryName { get; }
        string Description { get; set; }
    }
}