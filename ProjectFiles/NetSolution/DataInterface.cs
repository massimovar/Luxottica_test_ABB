using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;
using WebSocketSharp;
using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;

namespace Luxlib.Robot
{
    public class DataInterface
    {
        #region members

        public event EventHandler ErrorRequest;
        private const string _defaultUsername = "Default User"; // Default username (same used by RobotStudio)
        private const string _defaultPassword = "robotics"; // Default password (same used by RobotStudio)

        private Cookie _coockies = new Cookie();
        private Cookie _session = new Cookie();

        private WebSocket websocket = null;

        #endregion members

        #region Fields
        CookieContainer _cookies = new CookieContainer();
        #endregion

        #region Properties
        public NetworkCredential Credentials { get; set; }
        #endregion

        #region Ctor
        public DataInterface(string username, string password)
        {
            Credentials = new NetworkCredential(username, password);
        }
        #endregion

        #region Methods
        /// <summary>
        /// sending http request to WS
        /// </summary>
        /// <param name="networkCredential"></param>
        /// <param name="httpmethod">request method (i.e. PUT, POST, etc)</param>
        /// <param name="node">http address to point to</param>
        /// <param name="action">content added to the request</param>
        /// <returns></returns>
        /// 
        public DataInterfaceResponse SendRequest(NetworkCredential networkCredential, string httpmethod, string node, string action = "")
        {
            //Log.Info($@"DataInterface - SendRequest: httpmethod = {httpmethod}, node = {node}, action = {action}.");

            // Init
            ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate pCertificate,
                X509Chain pChain, SslPolicyErrors pSSLPolicyErrors) { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            DataInterfaceResponse _dataInterfaceResponse = new DataInterfaceResponse();
            string exc = "";
            HttpWebResponse response = null;
            string responseText = "";
            HttpWebRequest request = null;

            try
            {
                // Setup request
                request = (HttpWebRequest)WebRequest.Create(new Uri(node));
                request.Credentials = new NetworkCredential(networkCredential.UserName, networkCredential.Password);
                request.Method = httpmethod;
                request.CookieContainer = _cookies;
                request.PreAuthenticate = true;
                request.Timeout = 5000;
                request.ServicePoint.Expect100Continue = false;
                request.Accept = "application/hal+json;v=2.0";
                request.Proxy = null;

                // Check request type
                if (request.Method == "PUT" || request.Method == "POST")
                {
                    request.ContentType = "application/x-www-form-urlencoded;v=2.0";
                    Stream s = request.GetRequestStream();
                    s.Write(Encoding.ASCII.GetBytes(action), 0, action.Length);
                    s.Close();
                }

                // Send request and handle the

                response = (HttpWebResponse)request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream(), ASCIIEncoding.ASCII))
                {
                    responseText += reader.ReadToEnd();
                }
                _dataInterfaceResponse.IsSuccesful = response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent;
                _dataInterfaceResponse.HttpStatusCode = response.StatusCode;
                _dataInterfaceResponse.JsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseText);

                // Close response
                response.Close();
            }
            catch (WebException ex)
            {
                //int a = ex.Message.IndexOf("(")+1;
                //int b = ex.Message.IndexOf(")");
                //string code = ex.Message.Substring(a, b-a);
                string _message = "";

                if (ex.Response != null)
                {
                    using (var sr = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        exc = sr.ReadToEnd();
                    }

                    dynamic JsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(exc);
                    _message = ResultDeserializer.GetErrorMessage(JsonResponse);
                }


                Log.Error($@"Error during Http data request {ex.Message}; msg: {_message}");
                ErrorRequest?.Invoke(this,new EventArgs());


                // Return
                return new DataInterfaceResponse() { IsSuccesful = false, JsonResponse=null};
            }

            // Notify successful request
            OnNewDataSent?.Invoke(this, response);

            // Return
            return _dataInterfaceResponse;
        }

        /// <summary>
        /// sending http request to WS
        /// </summary>
        /// <param name="networkCredential"></param>
        /// <param name="httpmethod">request method (i.e. PUT, POST, etc)</param>
        /// <param name="node">http address to point to</param>
        /// <param name="action">content added to the request</param>
        /// <returns></returns>
        /// 
        public DataInterfaceResponse SendSubscriptionRequest(NetworkCredential networkCredential, string httpmethod, string node, string action = "")
        {
            Log.Info($@"DataInterface - SendSubscriptionRequest: httpmethod = {httpmethod}, node = {node}, action = {action}.");

            // Init
            ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate pCertificate,
                X509Chain pChain, SslPolicyErrors pSSLPolicyErrors) { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            DataInterfaceResponse _dataInterfaceResponse = new DataInterfaceResponse();
            string exc = "";
            HttpWebResponse response = null;
            string responseText = "";
            HttpWebRequest request = null;

            try
            {
                // Setup request
                request = (HttpWebRequest)WebRequest.Create(new Uri(node));
                request.Credentials = new NetworkCredential(networkCredential.UserName, networkCredential.Password);
                request.Method = httpmethod;
                request.CookieContainer = _cookies;
                request.PreAuthenticate = true;
                request.Timeout = 5000;
                request.ServicePoint.Expect100Continue = false;
                request.Accept = "application/hal+json;v=2.0";
                //request.Accept = "application/xhtml+xml;v=2.0";              
                request.Proxy = null;

                // Check request type
                if (request.Method == "PUT" || request.Method == "POST")
                {
                    request.ContentType = "application/x-www-form-urlencoded;v=2.0";
                    Stream s = request.GetRequestStream();
                    s.Write(Encoding.ASCII.GetBytes(action), 0, action.Length);
                    s.Close();
                }

                // Send request and handle the response
                response = (HttpWebResponse)request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream(), ASCIIEncoding.ASCII))
                {
                    responseText += reader.ReadToEnd();
                }

                //---------------------------------------------------------------------//
                // double camera test
                string location = response.Headers["Location"];
                int first = location.IndexOf("/poll/") + "/poll/".Length;

                // Get coockies
                /*string[] cookies = GetAbbCookie(response);
                if (cookies[0] != null)
                {
                    _coockies = new Cookie(cookies[0], cookies[1]);
                    _session = new Cookie(cookies[2], cookies[3]);
                }*/

                // connect websocket with the received abb cookie 
                //ConnectWebsocket(new Uri(location), _coockies, _session);
                //return location.Substring(first, location.Length - first);
                string subgroup = location.Substring(first, location.Length - first);

                //---------------------------------------------------------------------//

                _dataInterfaceResponse.IsSuccesful = response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.Created;
                _dataInterfaceResponse.HttpStatusCode = response.StatusCode;
                //_dataInterfaceResponse.JsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseText);
                //_dataInterfaceResponse.JsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseText);
                _dataInterfaceResponse.Location = location;
                _dataInterfaceResponse.SubGroup = subgroup;

                // Close response
                response.Close();
            }
            catch (WebException ex)
            {
                Log.Error($@"Error during Http data request {ex.Message}.");
                if (ex.Response != null)
                    using (var sr = new StreamReader(ex.Response.GetResponseStream()))
                        exc = sr.ReadToEnd();

                // Return
                return new DataInterfaceResponse();
            }

            // Notify successful request
            OnNewDataSubscribed?.Invoke(this, response);

            // Return
            return _dataInterfaceResponse;
        }

        public DataInterfaceFileResponse SendFileRequest(NetworkCredential networkCredential, string httpmethod, string node, byte[] content = null)
        {
            // Init
            ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate pCertificate,
                X509Chain pChain, SslPolicyErrors pSSLPolicyErrors) { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            DataInterfaceFileResponse _dataInterfaceFileResponse = new DataInterfaceFileResponse();
            string exc = "";
            HttpWebResponse response = null;
            string responseText = "";
            HttpWebRequest request = null;

            try
            {
                // Setup request
                request = (HttpWebRequest)WebRequest.Create(new Uri(node));
                request.Credentials = new NetworkCredential(networkCredential.UserName, networkCredential.Password);
                request.Method = httpmethod;
                request.CookieContainer = _cookies;
                request.PreAuthenticate = true;
                request.Timeout = 8000;
                request.ServicePoint.Expect100Continue = false;
                request.Accept = "application/xhtml+xml;v=2.0";
                request.Proxy = new WebProxy();

                // Check request type
                if (request.Method == "PUT")
                {
                    request.ContentType = "application/octet-stream;v=2.0";
                    Stream s = request.GetRequestStream();
                    s.Write(content, 0, content.Length);
                    s.Close();
                }
                else if (request.Method == "POST")
                {
                    byte[] byteArray = content;

                    request.ContentType = "application/x-www-form-urlencoded;v=2.0";
                    request.ContentLength = byteArray.Length;

                    Stream s = request.GetRequestStream();
                    s.Write(byteArray, 0, byteArray.Length);
                    s.Close();
                }

                // Send request and handle the response
                response = (HttpWebResponse)request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.ASCII))
                {
                    responseText += reader.ReadToEnd();
                }
                _dataInterfaceFileResponse.IsSuccesful = response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent;
                _dataInterfaceFileResponse.HttpStatusCode = response.StatusCode;
                _dataInterfaceFileResponse.TextResponse = responseText;

                // Close response
                response.Close();
            }
            catch (WebException ex)
            {
                Log.Error($@"Error during Http file request {ex.Message}.");
                if (ex.Response != null)
                    using (var sr = new StreamReader(ex.Response.GetResponseStream()))
                        exc = sr.ReadToEnd();

                // Return
                return new DataInterfaceFileResponse();
            }

            // Notify successful request
            OnNewDataSent?.Invoke(this, response);

            // Return
            return _dataInterfaceFileResponse;
        }

        public async Task<DataInterfaceFileResponse> SendFileRequestAsync(NetworkCredential networkCredential, string httpmethod, string node, byte[] content = null)
        {
            // Init
            ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate pCertificate,
                X509Chain pChain, SslPolicyErrors pSSLPolicyErrors) { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            DataInterfaceFileResponse _dataInterfaceFileResponse = new DataInterfaceFileResponse();
            string exc = "";
            HttpWebResponse response = null;
            string responseText = "";
            HttpWebRequest request = null;

            try
            {
                // Setup request
                request = (HttpWebRequest)WebRequest.Create(new Uri(node));
                request.Credentials = new NetworkCredential(networkCredential.UserName, networkCredential.Password);
                request.Method = httpmethod;
                request.CookieContainer = _cookies;
                request.PreAuthenticate = true;
                request.Timeout = 8000;
                request.ServicePoint.Expect100Continue = false;
                request.Accept = "application/xhtml+xml;v=2.0";
                request.Proxy = new WebProxy();

                // Check request type
                if (request.Method == "PUT")
                {
                    request.ContentType = "application/octet-stream;v=2.0";
                    Stream s = request.GetRequestStream();
                    s.Write(content, 0, content.Length);
                    s.Close();
                }
                else if (request.Method == "POST")
                {
                    request.ContentType = "application/x-www-form-urlencoded;v=2.0";
                    Stream s = request.GetRequestStream();
                    s.Write(content, 0, content.Length);
                    s.Close();
                }

                // Send request and handle response
                response = (HttpWebResponse)await request.GetResponseAsync();
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.ASCII))
                {
                    responseText += reader.ReadToEnd();
                }
                _dataInterfaceFileResponse.IsSuccesful = response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent;
                _dataInterfaceFileResponse.HttpStatusCode = response.StatusCode;
                _dataInterfaceFileResponse.TextResponse = responseText;

                // Close response
                response.Close();
            }
            catch (WebException ex)
            {
                using (var sr = new StreamReader(ex.Response.GetResponseStream()))
                    exc = sr.ReadToEnd();

                // Return
                return new DataInterfaceFileResponse();
            }

            // Notify successful request
            OnNewDataSent?.Invoke(this, response);

            // Return
            return _dataInterfaceFileResponse;
        }

        public async Task<DataInterfaceResponse> SendRequestAsync(NetworkCredential networkCredential, string httpmethod, string node, string action = "")
        {
            // Init
            ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate pCertificate,
                X509Chain pChain, SslPolicyErrors pSSLPolicyErrors) { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            DataInterfaceResponse _dataInterfaceResponse = new DataInterfaceResponse();
            string exc = "";
            HttpWebResponse response = null;
            string responseText = "";
            HttpWebRequest request = null;

            try
            {
                // Setup request
                request = (HttpWebRequest)WebRequest.Create(new Uri(node));
                request.Credentials = new NetworkCredential(networkCredential.UserName, networkCredential.Password);
                request.Method = httpmethod;
                request.CookieContainer = _cookies;
                request.PreAuthenticate = true;
                request.Timeout = 5000;
                request.ServicePoint.Expect100Continue = false;
                request.Accept = "application/hal+json;v=2.0";
                request.Proxy = new WebProxy();

                // Check request type
                if (request.Method == "PUT" || request.Method == "POST")
                {
                    request.ContentType = "application/x-www-form-urlencoded;v=2.0";
                    Stream s = request.GetRequestStream();
                    s.Write(Encoding.ASCII.GetBytes(action), 0, action.Length);
                    s.Close();
                }

                // Send request and handle response
                response = (HttpWebResponse)await request.GetResponseAsync();
                using (var reader = new StreamReader(response.GetResponseStream(), ASCIIEncoding.ASCII))
                {
                    responseText += reader.ReadToEnd();
                }
                _dataInterfaceResponse.IsSuccesful = response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent;
                _dataInterfaceResponse.HttpStatusCode = response.StatusCode;
                _dataInterfaceResponse.JsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseText);

                // Close response
                response.Close();
            }
            catch (WebException ex)
            {
                using (var sr = new StreamReader(ex.Response.GetResponseStream()))
                    exc = sr.ReadToEnd();

                // Return
                return new DataInterfaceResponse();
            }

            // Notify successful request
            OnNewDataSent?.Invoke(this, response);

            // Return
            return _dataInterfaceResponse;
        }

        private void ConnectWebsocket(Uri url, Cookie abbCookie, Cookie httpSessionCookie)
        {
            //string wsUrl = "wss://" + url.Authority + "/poll"; // Authority, the port number must be included in the url
            /*if (_bIRC5)
            {
                websocket = new WebSocket(url.AbsoluteUri, "robapi2_subscription"); // create websocket using robapi2_subscription protocol
            }
            else
            {
                websocket = new WebSocket(url.AbsoluteUri, "rws_subscription"); // create websocket using rws_subscription protocol
            }*/

            websocket = new WebSocket(url.AbsoluteUri, "rws_subscription"); // create websocket using rws_subscription protocol

            websocket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true; // If the server certificate is valid.
            };

            websocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Default | SslProtocols.Tls12;

            websocket.SetCookie(new WebSocketSharp.Net.Cookie(abbCookie.Name, abbCookie.Value));
            websocket.SetCookie(new WebSocketSharp.Net.Cookie(httpSessionCookie.Name, httpSessionCookie.Value));

            // define handles
            /*
            websocket.OnOpen += new EventHandler(WsOpened);

            websocket.OnError += new EventHandler<WebSocketSharp.ErrorEventArgs>(WsError);

            websocket.SetCredentials(Username, Password, false);

            websocket.OnMessage += new EventHandler<MessageEventArgs>(WsMessageReceived);

            websocket.OnClose += new EventHandler<CloseEventArgs>(WsClosed);

            // do the web socket connect, if anything goes wrong is an exception thrown
            websocket.Connect();
            */
        }
        /// <summary>
        /// This method gets ABB cookie.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private string[] GetAbbCookie(HttpWebResponse response)
        {
            // Handle cookies
            string[] cookies = new string[4];
            int counter = 0;
            string cookiesText = response.Headers[HttpResponseHeader.SetCookie];

            if (cookiesText != null)
            {
                string[] lines = cookiesText.Split(';');
                foreach (string line in lines)
                {
                    string[] c = line.Split('=');

                    if (c[0] == "ABBCX" || c[0] == " httponly,ABBCX")
                    {
                        cookies[0] = "ABBCX";
                        cookies[1] = c[1];
                        counter++;
                    }

                    if (c[0] == "-http-session-")
                    {
                        cookies[2] = "-http-session-";
                        cookies[3] = c[1];
                        counter++;
                    }

                    if (counter == 2) break;
                }
            }
            return cookies;
        }

        #endregion

        #region Events
        public event EventHandler<HttpWebResponse> OnNewDataSent;
        public event EventHandler<HttpWebResponse> OnNewDataSubscribed;
        #endregion
    }
}
