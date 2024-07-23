using System;
using System.IO;

namespace _RUDP_
{
    public enum RudpHeaderI : byte
    {
        Version,
        Mask,
        ID,
        Attempt,
        _last_,
    }

    enum RudpHeaderB : byte
    {
        direct,
        reliable,
        ack,
        compressed,
        _last_,
    }

    [Flags]
    public enum RudpHeaderM : byte
    {
        Direct = 1 << RudpHeaderB.direct,
        Reliable = 1 << RudpHeaderB.reliable,
        Ack = 1 << RudpHeaderB.ack,
        Compressed = 1 << RudpHeaderB.compressed,

        Files = Reliable | Direct,
        Audio = Direct,
        States = Reliable,
        Flux = 0,
    }

    public readonly struct RudpHeader
    {
        public const byte HEADER_length = (byte)RudpHeaderI._last_;

        public readonly RudpHeaderM mask;
        public readonly byte version;
        public readonly byte id, attempt;
        public override string ToString() => $"{{{nameof(version)}:{version}, {nameof(mask)}:{{{mask}}}, {nameof(id)}:{id}, {nameof(attempt)}:{attempt}}}";

        //----------------------------------------------------------------------------------------------------------

        public static void Prefixe(in BinaryWriter writer) => ((RudpHeader)default).Write(writer);

        //----------------------------------------------------------------------------------------------------------

        public RudpHeader(in byte id, in RudpHeaderM mask, in byte attempt) : this(Util_rudp.VERSION, mask, id, attempt)
        {
        }

        RudpHeader(in byte version, in RudpHeaderM mask, in byte id, in byte attempt)
        {
            this.version = version;
            this.mask = mask;
            this.id = id;
            this.attempt = attempt;
        }

        //----------------------------------------------------------------------------------------------------------

        public static RudpHeader FromBuffer(in byte[] buffer) => new(buffer[0], (RudpHeaderM)buffer[1], buffer[2], buffer[3]);
        public static RudpHeader FromReader(in BinaryReader reader) => new(reader.ReadByte(), (RudpHeaderM)reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

        public void Write(in byte[] buffer)
        {
            buffer[(int)RudpHeaderI.Version] = version;
            buffer[(int)RudpHeaderI.Mask] = (byte)mask;
            buffer[(int)RudpHeaderI.ID] = id;
            buffer[(int)RudpHeaderI.Attempt] = attempt;
        }

        public void Write(in BinaryWriter writer)
        {
            writer.Write(version);
            writer.Write((byte)mask);
            writer.Write(id);
            writer.Write(attempt);
        }

        public static void Write(in byte[] buffer, in RudpHeaderM mask, in byte id, in byte attempt)
        {
            buffer[(int)RudpHeaderI.Version] = Util_rudp.VERSION;
            buffer[(int)RudpHeaderI.Mask] = (byte)mask;
            buffer[(int)RudpHeaderI.ID] = id;
            buffer[(int)RudpHeaderI.Attempt] = attempt;
        }
    }
}