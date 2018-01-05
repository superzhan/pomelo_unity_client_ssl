using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;


namespace Pomelo.DotNetClient
{
    public class TransporterBase  {


        protected Socket socket;

        public const int HeadLength = 4;

        protected Action<byte[]> messageProcesser;


        //Used for get message
        protected StateObject stateObject = new StateObject();
        protected TransportState transportState;
        protected IAsyncResult asyncReceive;
        protected IAsyncResult asyncSend;
        protected bool onSending = false;
        protected bool onReceiving = false;
        protected byte[] headBuffer = new byte[4];
        protected byte[] buffer;
        protected int bufferOffset = 0;
        protected int pkgLength = 0;
        internal Action onDisconnect = null;


       
        // The net work state changed event
        public  Action<NetWorkState> NetWorkStateChangedEvent;
        protected NetWorkState netWorkState = NetWorkState.CLOSED;   //current network state
        protected ManualResetEvent timeoutEvent = new ManualResetEvent(false);
        protected int timeoutMSec = 8000;    //connect timeout count in millisecond


        public void SetMessageProcesser(Action<byte[]> processer)
        {
            this.messageProcesser = processer;
        }

        public virtual void  Init(string host, int port, Action<bool> initSuccessCallback = null)
        {}

        public virtual void send(byte[] buffer){}

        public virtual void receive() {}

        internal virtual void close(){}




        public void start()
        {
            this.receive();
        }


        internal void processBytes(byte[] bytes, int offset, int limit)
        {
            if (this.transportState == TransportState.readHead)
            {
                readHead(bytes, offset, limit);
            }
            else if (this.transportState == TransportState.readBody)
            {
                readBody(bytes, offset, limit);
            }
        }

        protected bool readHead(byte[] bytes, int offset, int limit)
        {
            int length = limit - offset;
            int headNum = HeadLength - bufferOffset;

            if (length >= headNum)
            {
                //Write head buffer
                writeBytes(bytes, offset, headNum, bufferOffset, headBuffer);
                //Get package length
                pkgLength = (headBuffer[1] << 16) + (headBuffer[2] << 8) + headBuffer[3];

                //Init message buffer
                buffer = new byte[HeadLength + pkgLength];
                writeBytes(headBuffer, 0, HeadLength, buffer);
                offset += headNum;
                bufferOffset = HeadLength;
                this.transportState = TransportState.readBody;

                if (offset <= limit) processBytes(bytes, offset, limit);
                return true;
            }
            else
            {
                writeBytes(bytes, offset, length, bufferOffset, headBuffer);
                bufferOffset += length;
                return false;
            }
        }

        protected void readBody(byte[] bytes, int offset, int limit)
        {
            int length = pkgLength + HeadLength - bufferOffset;
            if ((offset + length) <= limit)
            {
                writeBytes(bytes, offset, length, bufferOffset, buffer);
                offset += length;

                //Invoke the protocol api to handle the message
                this.messageProcesser.Invoke(buffer);
                this.bufferOffset = 0;
                this.pkgLength = 0;

                if (this.transportState != TransportState.closed)
                    this.transportState = TransportState.readHead;
                if (offset < limit)
                    processBytes(bytes, offset, limit);
            }
            else
            {
                writeBytes(bytes, offset, limit - offset, bufferOffset, buffer);
                bufferOffset += limit - offset;
                this.transportState = TransportState.readBody;
            }
        }

        protected void writeBytes(byte[] source, int start, int length, byte[] target)
        {
            writeBytes(source, start, length, 0, target);
        }

        private void writeBytes(byte[] source, int start, int length, int offset, byte[] target)
        {
            for (int i = 0; i < length; i++)
            {
                target[offset + i] = source[start + i];
            }
        }

        protected void print(byte[] bytes, int offset, int length)
        {
            for (int i = offset; i < length; i++)
                Console.Write(Convert.ToString(bytes[i], 16) + " ");
            Console.WriteLine();
        }

        protected void NetWorkChanged(NetWorkState state)
        {
            netWorkState = state;
            if (NetWorkStateChangedEvent != null)
            {
                NetWorkStateChangedEvent(state);
            }
        }
    
 
    }
}
