using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ExampleWebWithExternalReferences.Startup))]
namespace ExampleWebWithExternalReferences
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
