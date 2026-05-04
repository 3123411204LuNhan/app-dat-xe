using System.Threading.Tasks;

namespace RideHailingApi.Tools
{
    public class Program
    {
        public static async Task<int> Main(string[] args) => await MigrationHelper.Main(args);
    }
}
