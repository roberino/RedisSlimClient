namespace RedisSlimClient.Serialization
{
    internal enum ResponseType : byte
    {
        Unknown,

        //For Simple Strings the first byte of the reply is "+"
        StringType = (byte) '+',

        //For Errors the first byte of the reply is "-"
        ErrorType = (byte) '-',

        //For Integers the first byte of the reply is ":"
        IntType = (byte) ':',

        //For Bulk Strings the first byte of the reply is "$"
        BulkStringType = (byte) '$',

        //For Arrays the first byte of the reply is "*"
        ArrayType = (byte) '*'
    }
}