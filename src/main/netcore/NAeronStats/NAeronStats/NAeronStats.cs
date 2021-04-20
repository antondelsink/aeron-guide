using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace NAeron
{
    public class NAeronStats : IDisposable
    {
        public const int CACHE_LINE_LENGTH = 64;

        public CncMetaDataLayout MetaData { get { return mdCNC; } }
        private CncMetaDataLayout mdCNC;

        private MemoryMappedFile mmf = null;

        private MemoryMappedViewAccessor vaCountersMetadata = null;
        private MemoryMappedViewAccessor vaCountersValues = null;

        public NAeronStats(string filename)
        {
            var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, FileOptions.RandomAccess);

            mmf = MemoryMappedFile.CreateFromFile(fs, mapName: null, fs.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);

            using (var vaCNCMD = mmf.CreateViewAccessor(offset: 0, CncMetaDataLayout.SizeOfCncMetaDataLayout, MemoryMappedFileAccess.Read))
            {
                vaCNCMD.Read(position: 0, out mdCNC);
            }

            var alignedEndOfCnCMetaData = Align(CncMetaDataLayout.SizeOfCncMetaDataLayout, (CACHE_LINE_LENGTH * 2));
            int offset_CountersMetaData = alignedEndOfCnCMetaData + mdCNC.ToDriverBufferLength + mdCNC.ToClientBufferLength;
            vaCountersMetadata = mmf.CreateViewAccessor(offset_CountersMetaData, mdCNC.CountersMetadataBufferLength, MemoryMappedFileAccess.Read);

            var offset_CountersValues = offset_CountersMetaData + mdCNC.CountersMetadataBufferLength;
            vaCountersValues = mmf.CreateViewAccessor(offset_CountersValues, mdCNC.CountersValuesBufferLength, MemoryMappedFileAccess.Read);
        }

        public long this[int index]
        {
            get
            {
                return GetCounterValue(index);
            }
        }

        public long GetCounterValue(int index)
        {
            var position_CounterValue = index * (64 * 2);
            return vaCountersValues.ReadInt64(position_CounterValue);
        }

        public IEnumerable<long> GetCounterValues()
        {
            int ixCounter = 0;
            foreach (var r in GetCounterMetaData())
            {
                var position_CounterValue = ixCounter * (64 * 2);

                yield return vaCountersValues.ReadInt64(position_CounterValue);

                ixCounter++;
            }
        }

        public IEnumerable<NAeronCounterMetaData> GetCounterMetaData()
        {
            int offset_Record = 0;
            while (offset_Record < vaCountersMetadata.Capacity)
            {
                int recordState = vaCountersMetadata.ReadInt32(offset_Record + 0);
                int typeID = vaCountersMetadata.ReadInt32(offset_Record + 4);
                long freeForUseDeadline = vaCountersMetadata.ReadInt64(offset_Record + 8);
                int labelLength = vaCountersMetadata.ReadInt32(offset_Record + 128);

                if (labelLength > 0)
                {
                    var arrayLabel = new byte[offset_Record + 380];
                    vaCountersMetadata.ReadArray<byte>(offset_Record + 132, arrayLabel, offset: 0, labelLength);
                    var label = Encoding.UTF8.GetString(arrayLabel, index: 0, labelLength);

                    yield return new NAeronCounterMetaData(recordState, typeID, freeForUseDeadline, label);
                }

                offset_Record += 512;
            }
        }

        public IEnumerable<(NAeronCounterMetaData, long)> GetStats()
        {
            int ix = 0;
            foreach (var c in GetCounterMetaData())
            {
                yield return (c, GetCounterValue(ix));
                ix++;
            }
        }

        public static int Align(int value, int alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }

        public void Dispose()
        {
            vaCountersMetadata?.Dispose();
            vaCountersValues?.Dispose();
            mmf?.Dispose();
        }
    }
}