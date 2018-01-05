
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace Pomelo.DotNetClient
{
    
    public class TransporterSSL : TransporterBase {

        private SslStream sslstream;
        private NetworkStream tcpStream;

        private bool tryAuthed = false;
        private bool authed = false;
        private string target_host;


        public TransporterSSL()
        {
            transportState = TransportState.readHead;
        }

        public override void  Init(string host, int port, Action<bool> initSuccessCallback = null)
        {

            timeoutEvent.Reset();
            NetWorkChanged(NetWorkState.CONNECTING);

            //explain ip address
            IPAddress ipAddress = null;
            try
            {
                IPAddress[] addresses = Dns.GetHostEntry(host).AddressList;
                foreach (var item in addresses)
                {
                    if (item.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = item;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                NetWorkChanged(NetWorkState.ERROR);
                return;
            }

            if (ipAddress == null)
            {
                throw new Exception("can not parse host : " + host);
            }

            //new socket
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ie = new IPEndPoint(ipAddress, port);
            this.target_host = ie.Address.ToString();

            //connect 
            socket.BeginConnect(ie, new AsyncCallback((result) =>
                {
                    try
                    {
                        this.socket.EndConnect(result);
                     

                        tcpStream = new NetworkStream(socket);

                        this.sslstream = new SslStream(
                            tcpStream,
                            false,
                            new RemoteCertificateValidationCallback(ValidateServerCertificate),
                            null
                        );

                     
                        NetWorkChanged(NetWorkState.CONNECTED);

                        if (initSuccessCallback != null)
                        {
                            initSuccessCallback(true);
                        }
                    }
                    catch (SocketException e)
                    {
                        if (netWorkState != NetWorkState.TIMEOUT)
                        {
                            NetWorkChanged(NetWorkState.ERROR);
                        }

                        if (initSuccessCallback != null)
                        {
                            initSuccessCallback(false);
                        }
                    }
                    finally
                    {
                        timeoutEvent.Set();
                    }
                }), this.socket);

            if (timeoutEvent.WaitOne(timeoutMSec, false))
            {
                if (netWorkState != NetWorkState.CONNECTED && netWorkState != NetWorkState.ERROR)
                {
                    NetWorkChanged(NetWorkState.TIMEOUT);

                    if (initSuccessCallback != null)
                    {
                        initSuccessCallback(false);
                    }
                }
            }

        }

        /// <summary>
        /// 证书验证
        /// </summary>
        /// <returns><c>true</c>, if server certificate was validated, <c>false</c> otherwise.</returns>
        /// <param name="sender">Sender.</param>
        /// <param name="certificate">Certificate.</param>
        /// <param name="chain">Chain.</param>
        /// <param name="sslPolicyErrors">Ssl policy errors.</param>
        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                authed = true;
                return true;
            }

            authed = true;
            return true;

//            if (chain.ChainElements.Count < 1)
//            {
//                Debug.LogError("certificate failed. empty chain!");
//                authed = false;
//                return false;
//            }
//
//            authed=true;
//            return true;
//
//            //check cert validity
//            bool cert_is_ok = false;
//            X509Certificate2 root = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
//            for (int i = 0; i < ca_thumbprints.Count; ++i)
//            {
//                if (root.Thumbprint == ca_thumbprints[i])
//                {
//                    cert_is_ok = true;
//                    break;
//                }
//            }
//            if (!cert_is_ok)
//            {
//                Debug.LogError("certificate failed. unknown cert printer: " + root.Thumbprint);
//                authed = false;
//                return false;
//            }
//
//            cert_is_ok = false;
//            //check host
//            for (int i = 0; i < target_hosts.Count; ++i)
//            {
//                if (root.Subject.Contains("CN=" + target_hosts[i]))
//                {
//                    cert_is_ok = true;
//                    break;
//                }
//            }
//            if (!cert_is_ok)
//            {
//                Debug.LogError("certificate failed. mismatch host: " + root.Subject);
//                authed = false;
//                return false;
//            }
//            authed = true;
//            return true;
            //Console.WriteLine("{0}", root.Thumbprint);
            //// Do not allow this client to communicate with unauthenticated servers.
            //X509Chain customChain = new X509Chain
            //{
            //    ChainPolicy = {
            //        VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority
            //    }
            //};
            //Boolean retValue = customChain.Build(chain.ChainElements[0].Certificate);
            //chain.Reset();
            //return retValue;
        }


        bool authorized()
        {
            if (null == sslstream)
            {
                return false;
            }

            if (tryAuthed == false)
            {
                try
                {
                   sslstream.AuthenticateAsClient(this.target_host);
                 
                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    Console.WriteLine("Authentication failed - closing the connection.");
                    sslstream.Close();
                    this.close();
                }
                finally
                {
                    tryAuthed = true;
                }
            }
             
            return authed;
        }



        public override void receive()
        {
            if (!this.authorized())
            {
                return;
            }
            this._receive();
        }

        private void _receive()
        {
            //Console.WriteLine("receive state : {0}, {1}", this.transportState, socket.Available);
            try
            {
                this.asyncReceive = sslstream.BeginRead(stateObject.buffer, 0, stateObject.buffer.Length, new AsyncCallback(endReceive), stateObject);
                this.onReceiving = true;
            }
            catch (Exception e)
            {
                this.close();
            }
        }

        private  void endReceive(IAsyncResult asyncReceive)
        {
            StateObject state = (StateObject)asyncReceive.AsyncState;

            try
            {
                this.onReceiving = false;
                int length = sslstream.EndRead(asyncReceive);
                if (length > 0)
                {
                    processBytes(state.buffer, 0, length);
          
                    //Receive next message
                    if (this.transportState != TransportState.closed) receive();
                }
                else
                {
                    this.close();
                }

            }
            catch (SocketException e)
            {
                this.close();
            }
        }

        public override void send(byte[] buffer)
        {
            if (this.transportState != TransportState.closed)
            {
                try
                {
                    this.asyncSend = sslstream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(sendCallback), stateObject);
                    onSending=true;
                }
                catch (Exception e)
                {
                 
                    this.close();
                }
            }
        }

        private  void sendCallback(IAsyncResult asyncSend)
        {
            try
            {
                sslstream.EndWrite(asyncSend);
                this.onSending = false;
            }
            catch (Exception e)
            {
              
            }
        }

        internal override void close()
        {

            this.transportState = TransportState.closed;
            NetWorkChanged(NetWorkState.CLOSED);

            if (this.sslstream != null)
            {
                this.sslstream.Close();
                this.sslstream = null;
            }

            if(this.tcpStream != null)
            {
                this.tcpStream.Close();
                this.tcpStream = null;
            }

            base.close();
        }


    }
}
