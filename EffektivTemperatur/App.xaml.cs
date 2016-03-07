using System.Net;

namespace EffektivTemperatur
{
    public partial class App
    {
        public App()
        {
            ServicePointManager.Expect100Continue = false;
        }
    }
}
