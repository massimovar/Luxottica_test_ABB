using System;
using Luxlib.Robot;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Xml;
using WebSocketSharp;
using UAManagedCore;
using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;

namespace Luxottica_feasibility_test
{
    internal class AbbOmnicoreRobot
    {

        private const string _defaultUsername = "Default User"; // Default username (same used by RobotStudio)
        private const string _defaultPassword = "robotics"; // Default password (same used by RobotStudio)

        public event EventHandler<EventArgs> SignalChangedFromOmnicoreRobot;
        public event EventHandler<EventArgs> ValueChangedFromOmnicoreRobot;
        public event EventHandler ErrorData;

        #region Fields
        private Cookie _coockies = new Cookie();
        private Cookie _session = new Cookie();
        private WebSocket _websocket = null;
        #endregion

        #region Properties
        // public ConnectionStatesEnum ConnectionState { get; private set; }
        public DataInterface DataInterface { get; private set; }
        public string ControllerName { get; private set; }
        public string IpAddress { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        #endregion

        #region Ctor
        public AbbOmnicoreRobot(string controllerName, string ipAddress, string username = _defaultUsername, string password = _defaultPassword)
        {
            ControllerName = controllerName;
            IpAddress = ipAddress;
            Username = username;
            Password = password;
        }
        #endregion

        public bool Connect()
        {
            // ConnectionState = ConnectionStatesEnum.CONNECTING;

            // Init interface with default credentials
            DataInterface = new DataInterface(Username, Password);

            // Event subscription
            DataInterface.OnNewDataSent += DataInterface_OnNewDataSent;
            DataInterface.OnNewDataSubscribed += DataInterface_OnNewDataSubscribed;
            DataInterface.ErrorRequest += DataInterface_ErrorRequest;

            // Return
            return true;
        }

        private void DataInterface_ErrorRequest(object sender, EventArgs e)
        {
            ErrorData?.Invoke(sender, e);
        }

        public void Disconnect()
        {
            // Dispose controller
            if (DataInterface != null)
            {
                DataInterface = null;
                // ConnectionState = ConnectionStatesEnum.OFFLINE;
            }
        }

        #region Private Methods
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

        private void DataInterface_OnNewDataSent(object sender, HttpWebResponse e)
        {
            // Get coockies
            string[] cookies = GetAbbCookie(e);
            if (cookies[0] != null)
            {
                _coockies = new Cookie(cookies[0], cookies[1]);
                _session = new Cookie(cookies[2], cookies[3]);
            }
        }

        private void DataInterface_OnNewDataSubscribed(object sender, HttpWebResponse e)
        {
            // Get coockies
            string[] cookies = GetAbbCookie(e);
            if (cookies[0] != null)
            {
                _coockies = new Cookie(cookies[0], cookies[1]);
                _session = new Cookie(cookies[2], cookies[3]);
            }

            string location = e.Headers["Location"];
            Uri uri = new Uri(location);
            try
            {
                //ConnectWebsocket(new Uri(location), _coockies, _session);
                ConnectWebsocket(uri, _coockies, _session);
            }
            catch (Exception ex)
            {
                Log.Error($@"ConnectWebsocket with uri = {uri.ToString()} FAILED for ({ex.Message}).");
            }
        }

        private void ConnectWebsocket(Uri url, Cookie abbCookie, Cookie httpSessionCookie)
        {
            _websocket = new WebSocket(url.AbsoluteUri, "rws_subscription"); // create websocket using rws_subscription protocol

            _websocket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true; // If the server certificate is valid.
            };

            _websocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Default | SslProtocols.Tls12;

            _websocket.SetCookie(new WebSocketSharp.Net.Cookie(abbCookie.Name, abbCookie.Value));
            _websocket.SetCookie(new WebSocketSharp.Net.Cookie(httpSessionCookie.Name, httpSessionCookie.Value));

            // define handles
            _websocket.OnOpen += new EventHandler(WsOpened);
            _websocket.OnError += new EventHandler<WebSocketSharp.ErrorEventArgs>(WsError);
            _websocket.SetCredentials(Username, Password, false);

            _websocket.OnMessage += new EventHandler<MessageEventArgs>(WsMessageReceived);

            _websocket.OnClose += new EventHandler<CloseEventArgs>(WsClosed);

            // do the web socket connect, if anything goes wrong is an exception thrown
            _websocket.Connect();
            Log.Info($@"Try websocket open.");
        }

        private void WsOpened(object sender, EventArgs e)
        {
            Log.Info($@"Websocket opened.");
        }

        private void WsClosed(object sender, CloseEventArgs e)
        {
            Log.Info($@"Websocket closed.");
            //Console.WriteLine("Websocket closed");
            //m_connected = false;
        }

        private void WsError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Log.Error($@"Websocket error: {e.Exception.Message}");
            //Console.WriteLine("Websocket error");
        }

        private Stream MakeStream(string str)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            //writer.Dispose();
            return stream;
        }

        /// <summary>
        /// This method sends inbound events on the message queue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WsMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.IsBinary == true)
            {

            }
            else if (e.IsText == true)
            {
                var value = DeserializeResource(MakeStream(e.Data.ToString()));
            }
        }

        /// <summary>
        /// This method handles the resources deserialization.
        /// </summary>
        /// <param name="Xml"></param>
        /// <returns></returns>
        public string DeserializeResource(Stream Xml)
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
                        case "rap-value-ev":
                            XmlNode rnv = node.SelectSingleNode("ns:a[@rel='self']", nsmgr);
                            string rapidHref = null;
                            if (rnv != null)
                            {
                                XmlAttribute rlink = rnv.Attributes["href"];
                                rapidHref = rlink.Value.ToString();
                            }
                            //notify change
                            ValueChangedFromOmnicoreRobot?.Invoke(this, new EventArgs());
                            break;
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

                            Description = lvalue;

                            // Notify change
                            SignalChangedFromOmnicoreRobot?.Invoke(this, new EventArgs());
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
                            Description = elogCode + " " + elogTimeStamp + " " + elogDescription;
                            break;
                        case "elog-message-ev":
                            XmlNode elogNode = node.SelectSingleNode("ns:a[@rel='self']", nsmgr);
                            string elogHref = null;
                            string seqNo = null;
                            if (elogNode != null)
                            {
                                XmlAttribute rlink = elogNode.Attributes["href"];
                                elogHref = rlink.Value.ToString();
                            }
                            XmlNode elogSeqNo = node.SelectSingleNode("//ns:span[@class='seqnum']", nsmgr);
                            if (elogSeqNo != null)
                            {
                                seqNo = elogSeqNo.InnerText.ToString();
                            }
                            Console.WriteLine("link: " + elogHref);
                            break;
                        default:
                            break;
                    }
                }
            }
            return Description;
        }
        #endregion
    }
}
