﻿using System;
using System.IO;
using UnityEngine;

namespace _RUDP_
{
    [Serializable]
    public partial class EveClient : IDisposable
    {
        public interface IUser
        {
            void OnEveOperation(in EveCodes code, in bool success, in BinaryReader reader);
        }

        enum HeaderI : byte
        {
            version,
            _last_
        }

        public const byte
            VERSION = 1,
            HEADER_LENGTH = (byte)HeaderI._last_;

        public static readonly bool
            logEvePaquets = true;

        [SerializeField] EveCodes armedCode;
        [SerializeField] IUser user;

        public readonly RudpSocket socket;
        public readonly RudpConnection eveConn;
        public readonly BinaryReader socketReader;

        readonly byte[] eveBuffer;
        readonly MemoryStream eveStream;
        readonly BinaryWriter eveWriter;
        public byte[] GetPaquetBuffer() => eveBuffer[..(int)eveStream.Position];
        public override string ToString() => $"{nameof(EveClient)} {eveConn}";

        //----------------------------------------------------------------------------------------------------------

        public EveClient(in RudpConnection eveConn)
        {
            this.eveConn = eveConn;
            socket = eveConn.socket;
            socketReader = eveConn.socket.recPaquetReader;

            eveBuffer = new byte[Util_rudp.PAQUET_SIZE];
            eveStream = new(eveBuffer);
            eveWriter = new(eveStream, RudpSocket.UTF8, false);

            for (HeaderI code = 0; code < HeaderI._last_; code++)
                switch (code)
                {
                    case HeaderI.version:
                        eveWriter.Write(VERSION);
                        break;
                    default:
                        eveWriter.Write((byte)0);
                        break;
                }
        }

        //----------------------------------------------------------------------------------------------------------

        public void StartOperation(in IUser user, in EveCodes code)
        {
            lock (this)
            {
                this.user = user;
                armedCode = code;

                lock (eveStream)
                {
                    eveStream.Position = HEADER_LENGTH;
                    eveWriter.Write((byte)code);

                    switch (code)
                    {
                        case EveCodes.GetPublicEnd:
                            break;
                        case EveCodes.ListHosts:
                            lock (hostsLock)
                            {
                                hostsOffset = 0;
                                hostsList.Clear();
                                eveWriter.Write(hostsOffset);
                            }
                            break;
                        case EveCodes.AddHost:
                            OnWriteRequest_AddHost();
                            break;
                        case EveCodes.JoinHost:
                            break;
                        case EveCodes.Test:
                            break;
                    }
                }
            }
        }

        public void Push(in bool dontwait = false)
        {
            lock (eveStream)
                if (eveStream.Position > HEADER_LENGTH)
                    lock (eveConn.lastSend)
                        if (dontwait || Util.TotalMilliseconds > eveConn.lastSend._value + 1000)
                            eveConn.Send(eveBuffer, 0, (ushort)eveStream.Position);
        }

        //----------------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            eveStream.Dispose();
            eveWriter.Dispose();
        }
    }
}