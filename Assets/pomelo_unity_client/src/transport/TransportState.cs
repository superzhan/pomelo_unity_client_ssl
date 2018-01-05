using System;

namespace Pomelo.DotNetClient
{

    public enum TransportState
    {
        readHead = 1,		// on read head
        readBody = 2,		// on read body
        closed = 3			// connection closed, will ignore all the message and wait for clean up
    }

    /// <summary>
    /// 通信协议类型
    /// </summary>
    public enum TransportType
    {
       TCP,
        SSL
    }

    public class StateObject
    {
        public const int BufferSize = 1024;
        internal byte[] buffer = new byte[BufferSize];
    }
}