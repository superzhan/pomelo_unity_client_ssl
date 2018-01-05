using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Pomelo.DotNetClient
{
  
    public class TransporterTCP : TransporterBase
    {
        //private TransportQueue<byte[]> _receiveQueue = new TransportQueue<byte[]>();
       // private System.Object _lock = new System.Object();

        public TransporterTCP()
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

            //connect 
            socket.BeginConnect(ie, new AsyncCallback((result) =>
                {
                    try
                    {
                        this.socket.EndConnect(result);
                       // this.protocol = new Protocol(this, this.socket);
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


        public override void send(byte[] buffer)
        {
            if (this.transportState != TransportState.closed)
            {
                //string str = "";
                //foreach (byte code in buffer)
                //{
                //    str += code.ToString();
                //}
                //Console.WriteLine("send:" + buffer.Length + " " + str.Length + "  " + str);
                this.asyncSend = socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(sendCallback), socket);

                this.onSending = true;
            }
        }

        private void sendCallback(IAsyncResult asyncSend)
        {
            //UnityEngine.Debug.Log("sendCallback " + this.transportState);
            if (this.transportState == TransportState.closed) return;
            socket.EndSend(asyncSend);
            this.onSending = false;
        }

    

        internal override void close()
        {
            this.transportState = TransportState.closed;
            NetWorkChanged(NetWorkState.CLOSED);
          

            if(socket !=null)
            {
                try
                {
                    this.socket.Shutdown(SocketShutdown.Both);
                    this.socket.Close();
                    this.socket = null;
                }
                catch (Exception)
                {
                    //todo : 有待确定这里是否会出现异常，这里是参考之前官方github上pull request。emptyMsg
                }
            }

        }

        public override void receive()
        {
            //Console.WriteLine("receive state : {0}, {1}", this.transportState, socket.Available);
            this.asyncReceive = socket.BeginReceive(stateObject.buffer, 0, stateObject.buffer.Length, SocketFlags.None, new AsyncCallback(endReceive), stateObject);
            this.onReceiving = true;
        }
        private void endReceive(IAsyncResult asyncReceive)
        {
            if (this.transportState == TransportState.closed)
                return;
            StateObject state = (StateObject)asyncReceive.AsyncState;
            Socket socket = this.socket;

            try
            {
                int length = socket.EndReceive(asyncReceive);

                this.onReceiving = false;

                if (length > 0)
                {
                    processBytes(state.buffer, 0, length);
                    //Receive next message
                    if (this.transportState != TransportState.closed) receive();
                }
                else
                {
                    if (this.onDisconnect != null) this.onDisconnect();
                }

            }
            catch (System.Net.Sockets.SocketException)
            {
                if (this.onDisconnect != null)
                    this.onDisconnect();
            }
        }


    }
}