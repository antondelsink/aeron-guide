namespace NAeron
{
    public struct NAeronCounterMetaData
    {
        public int RecordState;
        public int TypeID;
        public long FreeForUseDeadline;
        public string Label;

        public NAeronCounterMetaData(int recordState, int typeID, long freeForUseDeadline, string label)
        {
            RecordState = recordState;
            TypeID = typeID;
            FreeForUseDeadline = freeForUseDeadline;
            Label = label;
        }
        public override string ToString()
        {
            return $"RecordState: {RecordState}, TypeID: {TypeID}, FreeForUseDeadLine: {FreeForUseDeadline}, Label: {Label}.";
        }
    }
}