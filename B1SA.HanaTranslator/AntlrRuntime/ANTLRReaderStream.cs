namespace Antlr.Runtime
{
    using TextReader = TextReader;

    /** <summary>
     *  Vacuum all input from a Reader and then treat it like a StringStream.
     *  Manage the buffer manually to avoid unnecessary data copying.
     *  </summary>
     *
     *  <remarks>
     *  If you need encoding, use ANTLRInputStream.
     *  </remarks>
     */
    [Serializable]
    public class ANTLRReaderStream : ANTLRStringStream
    {
        public const int ReadBufferSize = 1024;
        public const int InitialBufferSize = 1024;

        public ANTLRReaderStream(TextReader r)
            : this(r, InitialBufferSize, ReadBufferSize)
        {
        }

        public ANTLRReaderStream(TextReader r, int size)
            : this(r, size, ReadBufferSize)
        {
        }

        public ANTLRReaderStream(TextReader r, int size, int readChunkSize)
        {
            Load(r, size, readChunkSize);
        }

        public virtual void Load(TextReader r, int size, int readChunkSize)
        {
            if (r == null) {
                return;
            }
            if (size <= 0) {
                size = InitialBufferSize;
            }
            if (readChunkSize <= 0) {
                readChunkSize = ReadBufferSize;
            }
            // System.out.println("load "+size+" in chunks of "+readChunkSize);
            try {
                data = r.ReadToEnd().ToCharArray();
                base.n = data.Length;
            }
            finally {
                r.Dispose();
            }
        }
    }
}
