using System.Net;
using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;

namespace Luxlib.Robot
{
    public class DataInterfaceFileResponse
    {
        public bool IsSuccesful { get; set; } = false;
        public HttpStatusCode HttpStatusCode { get; set; }
        public string TextResponse { get; set; }
    }
}
