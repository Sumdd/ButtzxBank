using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ButtzxBank.Startup))]
namespace ButtzxBank
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
