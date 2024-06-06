﻿using _UTIL_;
using System;
using System.IO;
using UnityEngine;

namespace _RUDP_
{
    [Serializable]
    public class EveComm : IDisposable
    {
        public const byte
            VERSION = 1,
            HEADER_LENGTH = 2;

        public static readonly bool
            logEvePaquets = true;

        public readonly RudpConnection eveConn;

        readonly byte[] eveBuffer;
        readonly MemoryStream eveStream;
        readonly BinaryWriter eveWriter;

        [SerializeField] byte id, attempt;
        public readonly ThreadSafe<double> lastSend = new();
        [SerializeField] bool sendFlag;

        public Action onEveAck, onEvePaquet;

        public byte[] GetSubPaquet() => eveBuffer[..(int)eveStream.Position];
        public override string ToString() => $"{nameof(EveComm)} {eveConn}";

        //----------------------------------------------------------------------------------------------------------

        public EveComm(in RudpConnection eveConn)
        {
            this.eveConn = eveConn;

            eveBuffer = new byte[Util_rudp.PAQUET_SIZE];
            eveStream = new(eveBuffer);
            eveWriter = new(eveStream, RudpSocket.UTF8, false);
            eveWriter.Write(VERSION);
            eveWriter.Write(id);
        }

        //----------------------------------------------------------------------------------------------------------

        public void Push()
        {
            lock (eveStream)
                if (eveStream.Position > HEADER_LENGTH)
                    lock (this)
                    {
                        double time = Util.TotalMilliseconds;
                        lock (lastSend)
                            if (sendFlag || time > lastSend._value + 1000)
                            {
                                sendFlag = false;
                                lastSend._value = time;
                                eveConn.Send(eveBuffer, 0, (ushort)eveStream.Position);
                            }
                    }
        }

        public void WriteNewPaquet(in Action<BinaryWriter> onWriter)
        {
            lock (eveStream)
            {
                eveStream.Position = HEADER_LENGTH;
                eveBuffer[1] = ++id == 0 ? (byte)1 : id;
                onWriter(eveWriter);
                lock (this)
                    sendFlag = true;
            }
        }

        public bool TryAcceptEvePaquet()
        {
            lock (lastSend)
                Debug.Log($"Eve ping: {(Util.TotalMilliseconds - lastSend._value).MillisecondsLog()}".ToSubLog());

            byte version = eveConn.socket.recReader_u.ReadByte();
            byte id = eveConn.socket.recReader_u.ReadByte();

            if (id == 0)
            {
                onEvePaquet();
                return true;
            }

            lock (eveStream)
            {
                if (eveStream.Position <= HEADER_LENGTH || eveBuffer[1] != id)
                    return false;
                eveStream.Position = HEADER_LENGTH;
            }

            onEveAck();
            return true;
        }

        //----------------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            eveStream.Dispose();
            eveWriter.Dispose();
        }
    }
}