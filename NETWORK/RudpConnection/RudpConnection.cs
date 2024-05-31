using _UTIL_;
using System;
using System.Net;

namespace _RUDP_
{
    public partial class RudpConnection : IDisposable
    {
        public readonly RudpSocket socket;
        public readonly IPEndPoint endPoint;
        public IPEndPoint localEnd, publicEnd;

        public readonly ThreadSafe<bool> disposed = new();

        public readonly ThreadSafe<double>
            lastSend = new(),
            lastReceive = new();

        public readonly RudpChannel
            channel_files,
            channel_states,
            channel_flux,
            channel_audio,
            channel_eve;

        public bool keepAlive;

        public override string ToString() => $"conn({socket.endIP_LAN}->{endPoint})";

        //----------------------------------------------------------------------------------------------------------

        public RudpConnection(in RudpSocket socket, in IPEndPoint endPoint)
        {
            this.socket = socket;
            this.endPoint = endPoint;

            channel_files = new(this, RudpHeaderM.Files);
            channel_states = new(this, RudpHeaderM.States);
            channel_flux = new(this, RudpHeaderM.Flux);
            channel_audio = new(this, RudpHeaderM.Audio);
            channel_eve = new(this, RudpHeaderM.Eve);

            keepAlive = true;
        }

        //----------------------------------------------------------------------------------------------------------

        public void OnNetworkPush()
        {
            channel_files.Push();
            channel_states.Push();

            if (keepAlive)
            {
                double time = Util_net.TotalMilliseconds;
                if (time > lastSend.Value + 5000)
                {
                    lastSend.Value = time;
                    socket.Send(Util_net.EMPTY_BUFFER);
                }
            }
        }

        public void Send(in byte[] buffer, in ushort offset, in ushort length)
        {
            lastSend.Value = Util_net.TotalMilliseconds;
            socket.SendTo(buffer, offset, length, endPoint);
        }

        //----------------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            if (disposed.Value)
                return;
            disposed.Value = true;

            channel_files.Dispose();
            channel_states.Dispose();
            channel_flux.Dispose();
            channel_audio.Dispose();
            channel_eve.Dispose();
        }
    }
}