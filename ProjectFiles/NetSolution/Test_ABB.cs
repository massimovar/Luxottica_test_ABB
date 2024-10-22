#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.UI;
using FTOptix.System;
using FTOptix.Core;
using FTOptix.CoreBase;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using Adapters;
using System.Net.Http;
using Luxottica_feasibility_test;
using Luxlib.Robot;
using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;
using ABB.Robotics.Controllers.IOSystemDomain;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Text;
#endregion

public class Test_ABB : BaseNetLogic
{
    private readonly string _robotIpAddressOmnicore = "192.168.150.1";

    private readonly string _robotIpAddressIRC5 = "192.168.125.1";
    //private string _ioSignal = $"/rw/iosystem/signals/{network}/{unit}/{signal}";  // resource to get

    private static HttpWebCommunication _webcon;

    private const string _defaultUsername = "Default User"; // Default username (same used by RobotStudio)
    private const string _defaultPassword = "robotics"; // Default password (same used by RobotStudio)


    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void SDKScanNetwork()
    {
        var scanner = new NetworkScanner();
        scanner.Scan();
        ControllerInfoCollection controllers = scanner.Controllers;
        foreach (ControllerInfo controllerInfo in controllers)
        {
            Log.Info(controllerInfo.IPAddress.ToString());
            Log.Info(controllerInfo.Id);
            Log.Info(controllerInfo.Availability.ToString());
            Log.Info(controllerInfo.IsVirtual.ToString());
            Log.Info(controllerInfo.SystemName);
            Log.Info(controllerInfo.Version.ToString());
            Log.Info(controllerInfo.ControllerName);
        }
    }

    [ExportMethod]
    public void TestRWS(){
        AbbOmnicoreRobot abbOmnicoreRobot = new AbbOmnicoreRobot("R1", _robotIpAddressOmnicore);
        abbOmnicoreRobot.Connect();

        DataInterface dataInterface = new DataInterface(abbOmnicoreRobot.Username, abbOmnicoreRobot.Password);

        var network = "EtherNetIP";
        var device = "EN_Internal_Device";
        var signal = "VO_PPH1_PcStartMission";
        
        var res = dataInterface.SendRequest(dataInterface.Credentials, "Get", $"https://{_robotIpAddressOmnicore}/rw/iosystem/signals/{network}/{device}/{signal}");
        Log.Info(res.JsonResponse.ToString());
    }

    [ExportMethod]
    public void TestRWSIRC5()
    {
        AbbOmnicoreRobot abbOmnicoreRobot = new AbbOmnicoreRobot("R1", _robotIpAddressIRC5);
        abbOmnicoreRobot.Connect();

        DataInterface dataInterface = new DataInterface(abbOmnicoreRobot.Username, abbOmnicoreRobot.Password);

        var network = "DeviceNet"; 
        var device = "EN_Internal_Device";
        var signal = "Signal_Lamp";

        var res = dataInterface.SendRequest(dataInterface.Credentials, "Get", $"http://{_robotIpAddressIRC5}/rw/iosystem/signals/{network}/{device}/{signal}");
        Log.Info(res.JsonResponse.ToString());
    }

    //[ExportMethod]
    //public void RWSGetSignals() => PerformRequest("iosystem/signals");

    //private async void PerformRequest(string req)
    //{
    //    using HttpClient httpClient = new HttpClient();
    //    httpClient.BaseAddress = new Uri($"https://{_robotIpAddressOmnicore}/rw");
    //    httpClient.Timeout = new TimeSpan(1000);

    //    try
    //    {
    //        using HttpResponseMessage httpResponse = await httpClient.GetAsync(req);

    //        if (httpResponse.IsSuccessStatusCode)
    //        {
    //            string responseBody = await httpResponse.Content.ReadAsStringAsync();
    //            Log.Info( _robotIpAddressOmnicore + " " + req + " response: ", responseBody);
    //        }
    //        else
    //        {
    //            Log.Warning("Unsuccessful request to " + _robotIpAddressOmnicore + ": " + httpResponse.StatusCode);
    //        }
    //    }
    //    catch (System.Exception ex)
    //    {
    //        Log.Error("RWSGetSignals", ex.Message);
    //    }
    //}


    /*---------------------------------------------------------
    * ----- Sample Call Read a signal:
    * Omnicore:
    *  curl -u "Default User":robotics -H "accept: application/xhtml+xml;v=2.0"
    *  "https://localhost/rw/iosystem/signals/Local/DRV_1/DRV1K1"
    * IRC5:
    *  curl --digest -u "Default User":robotics
    *  "http://localhost/rw/iosystem/signals/Local/DRV_1/DRV1K1"
    * ----------------------------------------------------------
    */

    [ExportMethod]
    public void TestRWSIRC6()
    {
        Log.Info("hello");
    }

    [ExportMethod]
    public string ReadSignalIO()
    {
        var network = "DeviceNet";
        var device = "EN_Internal_Device";
        var signal = "Signal_Lamp";

        var _url = $"http://{_robotIpAddressIRC5}/rw/iosystem/signals/{network}/{device}/{signal}";

        string value = "NULL";
        HttpWebResponse response = null;
        try
        {
            _webcon = new HttpWebCommunication(true);
            response = _webcon.DoWebRequest("GET", _url, null);

            // A subscribe request shall return created
            if ((response.StatusCode != HttpStatusCode.OK) & (response.StatusCode != HttpStatusCode.NoContent))
            {
                // something was wrong
                Console.WriteLine("Error get resource {0} error {1}", _url, response.StatusCode);
            }

            value = DeserializeResource(response.GetResponseStream());
            // Close the http response stream (if forgotten, it might not be possible to send more requests) 
            response.GetResponseStream().Close();
            response.Close();
        }
        catch (Exception ex)
        {
            //DisposeHttpResponse(response);
            // ? not a communication error, reconnect will probably not solve the problem
            Console.WriteLine("Error get root:" + ex.ToString());
            Console.WriteLine(" ");
        }
        return value;
    }

    static string DeserializeResource(Stream Xml)
    {
        string Description = null;
        XmlDocument doc = new XmlDocument();
        doc.Load(Xml);

        // Create an XmlNamespaceManager for resolving namespaces.
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("ns", "http://www.w3.org/1999/xhtml");
        XmlNodeList nodes = doc.SelectNodes("//ns:html/ns:body/ns:div/ns:ul/ns:li", nsmgr);

        foreach (XmlNode node in nodes)
        {
            XmlAttribute type = node.Attributes["class"];
            if (type != null)
            {
                string classType = type.Value.ToString();
                XmlNode elog;
                switch (classType)
                {
                    case "rap-data":
                        elog = node.SelectSingleNode("//ns:span[@class='value']", nsmgr);
                        if (elog != null)
                        {
                            Description = elog.InnerText.ToString();
                        }
                        break;
                    case "rap-value":
                        elog = node.SelectSingleNode("//ns:span[@class='value']", nsmgr);
                        if (elog != null)
                        {
                            Description = elog.InnerText.ToString();
                        }
                        break;
                    case "ios-signal": //irc5
                        elog = node.SelectSingleNode("//ns:span[@class='lvalue']", nsmgr);
                        if (elog != null)
                        {
                            Description = elog.InnerText.ToString();
                        }
                        break;
                    case "ios-signal-li": // omnicore
                        elog = node.SelectSingleNode("//ns:span[@class='lvalue']", nsmgr);
                        if (elog != null)
                        {
                            Description = elog.InnerText.ToString();
                        }
                        break;
                    //case "rap-value-ev":
                    //    XmlNode rnv = node.SelectSingleNode("ns:a[@rel='self']", nsmgr);
                    //    string rapidHref = null;
                    //    if (rnv != null)
                    //    {
                    //        XmlAttribute rlink = rnv.Attributes["href"];
                    //        rapidHref = rlink.Value.ToString();
                    //    }

                    //    Console.WriteLine("link: " + rapidHref);

                    //    // get current rapid value

                    //    if (_bIRC5)
                    //    {
                    //        GetState(_host_irc5 + rapidHref);
                    //    }
                    //    else
                    //    {
                    //        GetState(_host_omni + rapidHref);
                    //    }
                    //    _bPrint = false;
                    //    break;
                    case "ios-signalstate-ev":
                        XmlNode nv = node.SelectSingleNode("ns:span[@class='lvalue']", nsmgr);
                        string lvalue = null; // logical value
                        string lstate = null; // logical state
                        string slink = null;  // signal url
                        if (nv != null)
                        {
                            lvalue = nv.InnerText.ToString();
                        }
                        nv = node.SelectSingleNode("ns:span[@class='lstate']", nsmgr);
                        if (nv != null)
                        {
                            lstate = nv.InnerText.ToString();
                        }

                        XmlNode href = node.SelectSingleNode("ns:a", nsmgr);
                        if (href != null)
                        {
                            XmlAttribute link = href.Attributes["href"];
                            if (link != null)
                            {
                                slink = link.Value.ToString();
                            }
                        }

                        Console.WriteLine("link: " + slink);
                        //_bPrint = true;
                        Description = lvalue;

                        Console.WriteLine("Message received -- new value: " + Description);
                        //SignalChanged.BeginInvoke(this, null,null,null);
                        //Program_SignalChanged(null, null);

                        break;
                    case "elog-message":
                        elog = node.SelectSingleNode("//ns:span[@class='desc']", nsmgr);
                        string elogDescription = null;
                        string elogCode = null;
                        string elogTimeStamp = null;
                        if (elog != null)
                        {
                            elogDescription = elog.InnerText.ToString();
                        }
                        elog = node.SelectSingleNode("//ns:span[@class='code']", nsmgr);
                        if (elog != null)
                        {
                            elogCode = elog.InnerText.ToString();
                        }
                        elog = node.SelectSingleNode("//ns:span[@class='tstamp']", nsmgr);
                        if (elog != null)
                        {
                            elogTimeStamp = elog.InnerText.ToString();
                        }
                        //_bPrint = true;
                        Description = elogCode + " " + elogTimeStamp + " " + elogDescription;
                        break;
                    //case "elog-message-ev":
                    //    XmlNode elogNode = node.SelectSingleNode("ns:a[@rel='self']", nsmgr);
                    //    string elogHref = null;
                    //    string seqNo = null;
                    //    if (elogNode != null)
                    //    {
                    //        XmlAttribute rlink = elogNode.Attributes["href"];
                    //        elogHref = rlink.Value.ToString();
                    //    }
                    //    XmlNode elogSeqNo = node.SelectSingleNode("//ns:span[@class='seqnum']", nsmgr);
                    //    if (elogSeqNo != null)
                    //    {
                    //        seqNo = elogSeqNo.InnerText.ToString();
                    //    }
                    //    Console.WriteLine("link: " + elogHref);

                    //    // get current log value

                    //    if (_bIRC5)
                    //    {
                    //        GetElogMessage(_host_irc5 + elogHref);
                    //    }
                    //    else
                    //    {
                    //        GetElogMessage(_host_omni + elogHref);
                    //    }
                    //    _bPrint = false;
                    //    break;
                    default:
                        break;
                }
            }
        }
        return Description;
    }

    public class HttpWebCommunication
    {
        private static bool _bIRC5 = true;

        private static string _ContentType = "";
        private static string _ContentType_omni = "application/x-www-form-urlencoded;v=2.0"; // use form data when sending update etc to controller
        private static string _ContentType_irc5 = "application/x-www-form-urlencoded"; // use form data when sending update etc to controller
        private static string _ContentTypeFile_irc5_omni = "text/plain;v=2.0"; // use form data when sending files, it's a plain text
        private static string _requestAccept_omni = "application/xhtml+xml;v=2.0";
        private static string _requestAccept_irc5 = "";


        NetworkCredential _credentials = new NetworkCredential(_defaultUsername, _defaultPassword);
        //---------------- Aggiunta gestione cookie
        CookieContainer _cookies = new CookieContainer();  // keep the cookies the same during multiple requests
        Cookie _abbCookie = null;
        Cookie _httpSessionCookie = null;

        public HttpWebCommunication(bool bIRC5)
        {
            _bIRC5 = bIRC5;
            if (_bIRC5)
            {
                _ContentType = _ContentType_irc5; // use form data when sending update etc to controller
            }
            else
            {
                _ContentType = _ContentType_omni;
            }
        }

        public HttpWebResponse DoWebRequest(string method, string url, string body)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.Credentials = _credentials;
            request.Method = method;
            request.Timeout = 4000;
            //------------------- aggiunta gestione cookie 
            request.CookieContainer = _cookies;
            request.PreAuthenticate = true;
            request.Proxy = null;

            if (_bIRC5 == false)
            {
                //------------------- omnicore--------------
                //------------------- aggiunta gestione proxy x omnicore
                request.Timeout = 10000;
                request.ServicePoint.Expect100Continue = false;
                if (request.Method != "PUT" && request.Method != "POST")
                {
                    request.Accept = _requestAccept_omni;
                }
                request.Proxy = new WebProxy();
                //-------------------
            }
            if (request.Method == "PUT" || request.Method == "POST")
            {
                ////use form data when sending update etc to controller

                //byte[] byteArray = Encoding.UTF8.GetBytes(body);

                //request.ContentType = _ContentType;
                //request.ContentLength = byteArray.Length;

                //Stream s = request.GetRequestStream();

                //s.Write(byteArray, 0, byteArray.Length);
                //s.Close();

                //////////////////////////////////////////////////////////////////
                ///
                byte[] byteArray = null;
                request.ContentType = _ContentType;

                //se arrivo codifica di file ho codificato con Convert.ToBase64String, devo usare Convert.FromBase64String e modificare il ContentType
                if (body != null && body != "" && IsBase64String(body))
                {
                    request.ContentType = _ContentTypeFile_irc5_omni;
                    byteArray = Convert.FromBase64String(body);
                    request.ContentLength = byteArray.Length;
                }
                else
                {
                    byteArray = Encoding.UTF8.GetBytes(body);
                    request.ContentLength = byteArray.Length;
                }

                // se il messaggio è meno di 4K scrivo direttamente, 
                if (request.ContentLength <= 4096)
                {
                    Stream s = request.GetRequestStream();
                    s.Write(byteArray, 0, byteArray.Length);
                    s.Close();
                }
                else //altrimenti devo creare un buffer e dire che mando a pacchetti
                {
                    request.AllowWriteStreamBuffering = true;
                    request.SendChunked = true;

                    MemoryStream stream = new MemoryStream();
                    stream.Write(byteArray, 0, byteArray.Length);
                    stream.Position = 0;

                    Stream s = request.GetRequestStream();
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        s.Write(buffer, 0, bytesRead);
                    }
                    stream.Close();
                    s.Close();
                }

            }

            HttpWebResponse response = null;

            //mio try
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception ex)
            {

                //
            }
            //-------------- gestione cookie
            if (response != null && _abbCookie == null)
            {
                // set the abb cookie
                _abbCookie = GetAbbCookie(response);
            }

            if (response != null && _httpSessionCookie == null)
            {
                _httpSessionCookie = GetHttpSessionCookie(response);
            }

            return response;

        }

        //-------------- gestione cookie

        // <summary>
        // Get the ABBCX cookie from the response
        // </summary>
        // <param name="response"></param>
        // <returns></returns>
        private Cookie GetAbbCookie(HttpWebResponse response)
        {
            string abbcookiestr = null;

            // get the abb cookie
            string cookiesText = (response as HttpWebResponse).Headers[HttpResponseHeader.SetCookie];
            string[] lines = cookiesText.Split(';');
            foreach (string line in lines)
            {
                string[] c = line.Split('=');
                if (c[0] == "ABBCX" || c[0] == " httponly,ABBCX")// " httponly,ABBCX" for newer version of Appweb
                {
                    abbcookiestr = c[1];
                    break;
                }
            }

            return new Cookie("ABBCX", abbcookiestr);
        }


        private Cookie GetHttpSessionCookie(HttpWebResponse response)
        {
            string httpcookiestr = null;
            // get the abb cookie
            string cookiesText = (response as HttpWebResponse).Headers[HttpResponseHeader.SetCookie];
            string[] lines = cookiesText.Split(';');
            foreach (string line in lines)
            {
                string[] c = line.Split('=');
                if (c[0] == "-http-session-")
                {
                    httpcookiestr = c[1];
                    break;
                }
            }
            return new Cookie("-http-session-", httpcookiestr);
        }


        public Cookie GetAbbCookie()
        {
            return _abbCookie;
        }

        public Cookie GetHttpSessionCookie()
        {
            return _httpSessionCookie;
        }

        public bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

        }

    }

    //static void GetRWService()
    //{
    //    var handler = new HttpClientHandler { Credentials = new NetworkCredential("Default User", "robotics") };
    //    handler.Proxy = null;   // disable the proxy, the controller is connected on same subnet as the PC 
    //    handler.UseProxy = false;
    //    // Send a request continue when complete
    //    using (HttpClient client = new HttpClient(handler))
    //    using (HttpResponseMessage response = await client.GetAsync(_address))
    //    using (HttpContent content = response.Content)
    //    {
    //        // Check that response was successful or throw exception
    //        response.EnsureSuccessStatusCode();
    //        // Get HTTP response from completed task.
    //        string result = await content.ReadAsStringAsync();
    //        // Deserialize the returned json string
    //        dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(result);
    //        // Display controller name, version and version name
    //        var service = obj._embedded._state[0]; // the first item in the json state response is the system name, robotware version and robotware version name
    //        Console.WriteLine("   service={0} name={1} version={2} versionname={3}", service._title, service.name, service.rwversion, service.rwversionname);
    //        // Display all installed options
    //        foreach (var option in obj._embedded._state[1].options) // the second state item is an array of installed options
    //        {
    //            Console.WriteLine("   option={0}", option.option);
    //        }
    //    }
    //}
}
