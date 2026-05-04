namespace SmartGrid.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var data = CSVLoader.LoadFirst100(@"C:\data\smartgrid.csv", @"C:\data\invalid.log");



        }
    }
}
