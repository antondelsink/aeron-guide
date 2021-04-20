using System.Runtime.InteropServices;

namespace NAeron
{
    [StructLayout(LayoutKind.Explicit, Size = SizeOfCncMetaDataLayout)]
    public struct CncMetaDataLayout
    {
        [FieldOffset(00)] public int AeronCncVersion;
        [FieldOffset(04)] public int ToDriverBufferLength;
        [FieldOffset(08)] public int ToClientBufferLength;
        [FieldOffset(12)] public int CountersMetadataBufferLength;
        [FieldOffset(16)] public int CountersValuesBufferLength;
        [FieldOffset(20)] public int ErrorLogBufferLength;
        [FieldOffset(24)] public long ClientLivenessTimeout;
        [FieldOffset(32)] public long DriverStartTimestamp;
        [FieldOffset(40)] public long DriverPID;

        public const int SizeOfCncMetaDataLayout = 48;
    }
}