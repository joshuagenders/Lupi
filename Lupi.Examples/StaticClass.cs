using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Lupi.Examples
{
    public static class StaticClass
    {
        public async static Task Init()
        {
            await Task.Delay(2);
        }

        public static async Task Teardown()
        {
            await Task.Delay(2);
        }

        public static ServiceProvider BuildServiceProvider() =>
            new ServiceCollection()
                .AddTransient<IInternalDependency, InternalDependency>()
                .BuildServiceProvider();
    }
}
