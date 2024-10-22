using System.Net;
using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;

namespace Luxlib.Robot
{
    public class DataInterfaceResponse
    {
        public bool IsSuccesful { get; set; } = false;
        public HttpStatusCode HttpStatusCode { get; set; }
        public dynamic JsonResponse { get; set; }
        public string Location { get; set; } = "";
        public string SubGroup { get; set; } = "";
    }
}
