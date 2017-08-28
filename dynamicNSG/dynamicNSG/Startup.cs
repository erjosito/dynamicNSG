using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(dynamicNSG.Startup))]
namespace dynamicNSG
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
