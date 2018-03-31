using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WeixinFirstDemo.Startup))]
namespace WeixinFirstDemo
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
