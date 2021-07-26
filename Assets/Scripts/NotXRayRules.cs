namespace XRay
{
    public sealed class NotXRayRules
    {
        public SymbolInfo[][] Tables { get; private set; }
        public int[][] Mazes { get; private set; }

        public NotXRayRules(SymbolInfo[][] tables, int[][] mazes)
        {
            Tables = tables;
            Mazes = mazes;
        }
    }
}
