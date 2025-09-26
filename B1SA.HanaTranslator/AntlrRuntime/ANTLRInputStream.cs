namespace Antlr.Runtime
{
    using Encoding = System.Text.Encoding;
    using Stream = Stream;
    using StreamReader = StreamReader;

    /** <summary>
     *  A kind of ReaderStream that pulls from an InputStream.
     *  Useful for reading from stdin and specifying file encodings etc...
     *  </summary>
     */
    [Serializable]
    public class ANTLRInputStream : ANTLRReaderStream
    {
        public ANTLRInputStream(Stream input)
            : this(input, null)
        {
        }

        public ANTLRInputStream(Stream input, int size)
            : this(input, size, null)
        {
        }

        public ANTLRInputStream(Stream input, Encoding encoding)
            : this(input, InitialBufferSize, encoding)
        {
        }

        public ANTLRInputStream(Stream input, int size, Encoding encoding)
            : this(input, size, ReadBufferSize, encoding)
        {
        }

        public ANTLRInputStream(Stream input, int size, int readBufferSize, Encoding encoding)
            : base(GetStreamReader(input, encoding), size, readBufferSize)
        {
        }

        private static StreamReader GetStreamReader(Stream input, Encoding encoding)
        {
            if (encoding != null)
                return new StreamReader(input, encoding);
            return new StreamReader(input);
        }
    }
}
