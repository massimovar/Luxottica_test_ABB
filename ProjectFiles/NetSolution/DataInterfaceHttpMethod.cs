using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;
﻿namespace Luxlib.Robot
{
    public static class DataInterfaceHttpMethod
    {
        public static string Get() { return "GET"; }
        public static string Post() { return "POST"; }
        public static string Put() { return "PUT"; }
    }
}
