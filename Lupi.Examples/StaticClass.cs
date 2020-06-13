using Autofac;
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

        public static ContainerBuilder Startup(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(GlobalState).Assembly);
            return builder;
        }
    }
}
