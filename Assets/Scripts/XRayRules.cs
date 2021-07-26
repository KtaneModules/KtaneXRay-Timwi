namespace XRay
{
    public sealed class XRayRules
    {
        public SymbolInfo[] Columns { get; private set; }
        public SymbolInfo[] Rows { get; private set; }
        public SymbolInfo[] Table3x3 { get; private set; }
        public int[] NumbersInTable { get; private set; }

        public XRayRules(SymbolInfo[] columns, SymbolInfo[] rows, SymbolInfo[] t3x3, int[] numbersInTable)
        {
            Columns = columns;
            Rows = rows;
            Table3x3 = t3x3;
            NumbersInTable = numbersInTable;
        }
    }
}
