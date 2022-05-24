using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibFoster
{
    /// <summary>
    /// Fostrian Data Format
    /// </summary>
    public static class Fostrian
    {
        #region Tools

        /// <summary>
        /// Seeks and searches <paramref name="search"/> on <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream"><see cref="System.IO.Stream"/></param>
        /// <param name="search"><see cref="byte"/> to search on.</param>
        /// <param name="startPos">The starting position of the search.</param>
        /// <returns>A <see cref="long"/> as the position of the <paramref name="search"/> in <paramref name="stream"/>.</returns>
        private static long Search(this System.IO.Stream stream, byte search, long startPos = -1)
        {
#pragma warning disable CA2201 // Do not raise reserved exception types
            if (!stream.CanSeek) { throw new AccessViolationException("Cannot seek in stream."); }
            if (!stream.CanRead) { throw new AccessViolationException("Cannot read in stream."); }
#pragma warning restore CA2201 // Do not raise reserved exception types
            long streamPos = stream.Position;
            long _startPos = startPos < 0 ? streamPos : startPos;
            for (long i = _startPos; i < stream.Length; i++)
            {
                stream.Position = i;
                byte v = (byte)stream.ReadByte();
                if (search == v)
                {
                    stream.Position = streamPos;
                    return i;
                }
            }
            stream.Position = streamPos;
            return -1;
        }

        #endregion Tools

        /// <summary>
        /// Parses a <paramref ref="stream" /> that contains information.
        /// </summary>
        /// <param name="stream"><see ref="System.IO.Stream" /> that contains the raw information.</param>
        /// <param name="stopPoint">Point to stop. <c>-1</c> to do not stop at all.</param>
        public static FostrianNode Parse(Stream stream, long stopPoint = -1)
        {
            stream = stream ?? throw new ArgumentNullException(nameof(stream));
            var _Stop = stopPoint < 0 ? stream.Length : (stopPoint <= stream.Position ? stream.Position : stopPoint);
            if (_Stop <= stream.Length)
            {
                throw new FostrianException("End of stream reached prematurely.");
            }
            else
            {
                var encodingStartByte = (byte)stream.ReadByte();
                var encodingEndByte = (byte)stream.ReadByte();
                var encodingByte = (byte)stream.ReadByte();
                var encoding = GetFostrianEncoding(encodingByte);
#pragma warning disable IDE0017 // Simplify object initialization
                FostrianNode rootNode = new FostrianNode() { IsRoot = true };
#pragma warning restore IDE0017 // Simplify object initialization

                // DO NOT ADD THIS BELOW TO THE CONSTRUCTOR, STUFF MIGHT BREAK

                rootNode.Encoding = encoding;
                rootNode.StartByte = encodingStartByte;
                rootNode.EndByte = encodingEndByte;

                while (stream.Position != _Stop)
                {
                    var streamPos = stream.Position;
                    if (_Stop <= stream.Position)
                    {
                        throw new FostrianException("End of stream reached prematurely.");
                    }
                    var node = new FostrianNode() { IsRoot = false, Parent = rootNode };
                    var endPoint = Search(stream, encodingEndByte);
                    stream.Position = streamPos;
                    byte[] DataBytes = new byte[endPoint - streamPos - 1];
                    stream.Read(DataBytes, 0, DataBytes.Length);
                    var sizeBytes = new byte[sizeof(int)];
                    stream.Read(sizeBytes, 0, sizeBytes.Length);
                    var nodesize = BitConverter.ToInt32(sizeBytes, 0);
                    node.Data = DataBytes;
                    rootNode.Values.Add(node);
                    if (nodesize > 0)
                    {
                        ParseRecursive(stream, encoding, node, nodesize, _Stop);
                    }
                }
                return rootNode;
            }
        }

        /// <summary>
        /// Loads a file from <paramref name="fileName"/> and parses it.
        /// </summary>
        /// <param name="fileName">Path of the file on local drive.</param>
        /// <returns><see cref="FostrianNode"/></returns>
        public static FostrianNode Parse(string fileName)
        {
            using (var fileStream = new System.IO.FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return Parse(fileStream);
            }
        }

        /// <summary>
        /// Exception thrown on Fostrian format errors.
        /// </summary>
        public class FostrianException : Exception
        {
            /// <summary>
            /// Exception thrown on Fostrian format errors.
            /// </summary>
            public FostrianException(string message) : base(message)
            {
            }
        }

        private static void ParseRecursive(Stream stream, System.Text.Encoding encoding, FostrianNode rootnode, int count, long _Stop)
        {
            if (rootnode == null) { throw new ArgumentNullException(nameof(rootnode)); }
            if (stream == null) { throw new ArgumentNullException(nameof(stream)); }
            if (count <= 0) { throw new ArgumentOutOfRangeException(nameof(count)); }
            var encodingEndByte = (byte)3;
            for (int i = 0; i < count; i++)
            {
                var streamPos = stream.Position;
                if (_Stop <= stream.Position)
                {
                    throw new FostrianException("End of stream reached prematurely.");
                }
                var node = new FostrianNode() { IsRoot = false, Parent = rootnode };
                var endPoint = Search(stream, encodingEndByte);
                stream.Position = streamPos;
                byte[] DataBytes = new byte[endPoint - streamPos - 1];
                stream.Read(DataBytes, 0, DataBytes.Length);
                var sizeBytes = new byte[sizeof(int)];
                stream.Read(sizeBytes, 0, sizeBytes.Length);
                var nodesize = BitConverter.ToInt32(sizeBytes, 0);
                node.Data = DataBytes;
                rootnode.Values.Add(node);
                if (nodesize > 0)
                {
                    ParseRecursive(stream, encoding, node, nodesize, _Stop);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="byte"/> representative of <paramref name="Encoding"/>.
        /// </summary>
        /// <param name="Encoding">The <see cref="System.Text.Encoding"/> that used for the information.</param>
        /// <returns>A <see cref="byte"/>.</returns>
        public static byte GetFostrianCode(this System.Text.Encoding Encoding)
        {
            if (Encoding == Encoding.Unicode)
            {
                return 0x03;
            }
            else if (Encoding == Encoding.BigEndianUnicode)
            {
                return 0x04;
            }
            else if (Encoding == Encoding.UTF32)
            {
                return 0x05;
            }
            else if (Encoding == Encoding.Default)
            {
                return 0x01;
            }
            else if (Encoding == Encoding.UTF8)
            {
                return 0x02;
            }
            else if (Encoding == Encoding.ASCII)
            {
                return 0x00;
            }
            else
            {
                throw new NotImplementedException("This encoding format is not implemented yet in this Fostrian library.");
            }
        }

        /// <summary>
        /// Converts <paramref name="value"/> back to <see cref="System.Text.Encoding"/>.
        /// </summary>
        /// <param name="value"><see cref="byte"/></param>
        /// <returns><see cref="System.Text.Encoding"/></returns>
        public static System.Text.Encoding GetFostrianEncoding(byte value)
        {
            switch (value)
            {
                default:
                    throw new FostrianException("Cannot get encoding for ID " + value + ".");
                case 0x01:
                    return System.Text.Encoding.Default;

                case 0x00:
                    return System.Text.Encoding.ASCII;

                case 0x02:
                    return System.Text.Encoding.UTF8;

                case 0x03:
                    return System.Text.Encoding.Unicode;

                case 0x04:
                    return System.Text.Encoding.BigEndianUnicode;

                case 0x05:
                    return System.Text.Encoding.UTF32;
            }
        }

        /// <summary>
        /// Recreates <paramref name="node"/> and writes into <paramref name="CopyTo"/>.
        /// </summary>
        /// <param name="node">The <see cref="FostrianNode"/>.</param>
        /// <param name="CopyTo"><see cref="System.IO.Stream"/> that allows seek and write operations.</param>
        public static void Recreate(this FostrianNode node, Stream CopyTo)
        {
            if (node == null) { throw new ArgumentNullException(nameof(node)); }
            if (CopyTo == null) { throw new ArgumentNullException(nameof(CopyTo)); }
#pragma warning disable CA2201 // Do not raise reserved exception types
            if (!CopyTo.CanSeek) { throw new Exception("Cannot seek in stream."); }
            if (!CopyTo.CanWrite) { throw new Exception("Cannot write to stream."); }
#pragma warning restore CA2201 // Do not raise reserved exception types
            if (node.IsRoot)
            {
                CopyTo.WriteByte(node.StartByte);
                CopyTo.WriteByte(node.EndByte);
                CopyTo.WriteByte(GetFostrianCode(node.Encoding));
            }
            CopyTo.WriteByte(node.StartByte);
            CopyTo.Write(node.Data, 0, node.Data.Length);
            CopyTo.WriteByte(node.EndByte);
            var size = BitConverter.GetBytes(node.Size);
            CopyTo.Write(size, 0, size.Length);
            for (int i = 0; i < node.Size; i++)
            {
                Recreate(node[i], CopyTo);
            }
        }

        /// <summary>
        /// Recreates <paramref name="node"/> and saves it into <paramref name="filePath"/>.
        /// </summary>
        /// <param name="node"><see cref="FostrianNode"/></param>
        /// <param name="filePath">Path of the file on drive.</param>
        public static void Recreate(this FostrianNode node, string filePath)
        {
            using (var fileStream = new System.IO.FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                Recreate(node, fileStream);
            }
        }

        /// <summary>
        /// Fostrian Node.
        /// </summary>
        public class FostrianNode
        {
            #region Constructor

            /// <summary>
            /// Creates a new <see cref="FostrianNode"/>.
            /// </summary>
            public FostrianNode()
            {
            }

            /// <summary>
            /// Creates a new <see cref="FostrianNode"/>.
            /// </summary>
            /// <param name="subnodes">The sub nodes that this node will contain.</param>
            public FostrianNode(IEnumerable<FostrianNode> subnodes) : this()
            {
                foreach (var node in subnodes)
                {
                    Values.Add(node);
                }
            }

            #endregion Constructor

            #region Complicated Properties

            /// <summary>
            /// Gets a sub <see cref="FostrianNode"/> at <paramref name="index"/>.
            /// </summary>
            /// <param name="index">Index of <see cref="FostrianNode"/>.</param>
            /// <returns><see cref="FostrianNode"/></returns>
            public FostrianNode this[int index] => Values[index];

            private Encoding _encoding;
            private byte _startByte;
            private byte _endByte;

            /// <summary>
            /// The default <see cref="System.Text.Encoding"/> of this node.
            /// <para>NOTE: The <c>set</c> only works for the root nodes and this property will always <c>get</c> the root node's <see cref="Encoding"/>.</para>
            /// </summary>
            public Encoding Encoding
            {
                get
                {
                    if (IsRoot)
                    {
                        return _encoding;
                    }
                    else
                    {
#pragma warning disable CA2201 // Do not raise reserved exception types
                        if (Parent is null) { throw new NullReferenceException("Parent node was null."); }
#pragma warning restore CA2201 // Do not raise reserved exception types
                        return Parent.Encoding;
                    }
                }
                set
                {
                    if (IsRoot)
                    {
                        _encoding = value;
                    }
                    else
                    {
                        // do absolutely nothing.
                    }
                }
            }

            /// <summary>
            /// <see cref="byte"/> used to determine the start of <seealso cref="Data"/>.
            /// <para>NOTE: The <c>set</c> only works for the root nodes and this property will always <c>get</c> the root node's <see cref="Encoding"/>.</para>
            /// </summary>
            public byte StartByte
            {
                get
                {
                    if (IsRoot)
                    {
                        return _startByte;
                    }
                    else
                    {
#pragma warning disable CA2201 // Do not raise reserved exception types
                        if (Parent is null) { throw new NullReferenceException("Parent node was null."); }
#pragma warning restore CA2201 // Do not raise reserved exception types
                        return Parent.StartByte;
                    }
                }
                set
                {
                    if (IsRoot)
                    {
                        _startByte = value;
                    }
                    else
                    {
                        // do absolutely nothing.
                    }
                }
            }

            /// <summary>
            /// <see cref="byte"/> used to determine the end of <seealso cref="Data"/>.
            /// <para>NOTE: The <c>set</c> only works for the root nodes and this property will always <c>get</c> the root node's <see cref="Encoding"/>.</para>
            /// </summary>
            public byte EndByte
            {
                get
                {
                    if (IsRoot)
                    {
                        return _endByte;
                    }
                    else
                    {
#pragma warning disable CA2201 // Do not raise reserved exception types
                        if (Parent is null) { throw new NullReferenceException("Parent node was null."); }
#pragma warning restore CA2201 // Do not raise reserved exception types
                        return Parent.EndByte;
                    }
                }
                set
                {
                    if (IsRoot)
                    {
                        _endByte = value;
                    }
                    else
                    {
                        // do absolutely nothing.
                    }
                }
            }

            #endregion Complicated Properties

            #region Add

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="int"/></param>
            public void Add(int input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="uint"/></param>
            public void Add(uint input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="ulong"/></param>
            public void Add(ulong input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="long"/></param>
            public void Add(long input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="short"/></param>
            public void Add(short input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="ushort"/></param>
            public void Add(ushort input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="float"/></param>
            public void Add(float input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="double"/></param>
            public void Add(double input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="bool"/></param>
            public void Add(bool input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="char"/></param>
            public void Add(char input) => Add(BitConverter.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/> with <paramref name="encoding"/>.
            /// </summary>
            /// <param name="input"><see cref="string"/></param>
            /// <param name="encoding"><see cref="System.Text.Encoding"/> to use, set null for the default value.</param>
            public void Add(string input, System.Text.Encoding encoding = null) => Add(encoding is null ? Encoding.GetBytes(input) : encoding.GetBytes(input));

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="byte"/> <seealso cref="Array"/>.</param>
            public void Add(byte[] input) => Values.Add(new FostrianNode() { IsRoot = false, Parent = this, Data = input });

            #endregion Add

            #region Data Conversions

            /// <summary>
            /// Gets the <see cref="Data"/> as a <seealso cref="string"/> by using <paramref name="encoding"/>.
            /// </summary>
            /// <param name="encoding"><see cref="System.Text.Encoding"/> to use when converting, use null for default.</param>
            /// <returns></returns>
            public string DataAsString(Encoding encoding = null) => encoding != null ? encoding.GetString(Data) : Encoding.GetString(Data);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="int"/>.
            /// </summary>
            public int DataAsInt32 => BitConverter.ToInt32(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="long"/>.
            /// </summary>
            public long DataAsInt64 => BitConverter.ToInt64(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="ulong"/>.
            /// </summary>
            public ulong DataAsUInt64 => BitConverter.ToUInt64(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="uint"/>.
            /// </summary>
            public uint DataAsUInt32 => BitConverter.ToUInt32(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="double"/>.
            /// </summary>
            public double DataAsDouble => BitConverter.ToDouble(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="float"/>.
            /// </summary>
            public float DataAsFloat => BitConverter.ToSingle(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="bool"/>.
            /// </summary>
            public bool DataAsBoolean => BitConverter.ToBoolean(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="char"/>.
            /// </summary>
            public char DataAsChar => BitConverter.ToChar(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="short"/>.
            /// </summary>
            public short DataAsInt16 => BitConverter.ToInt16(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="ushort"/>.
            /// </summary>
            public ushort DataAsUInt16 => BitConverter.ToUInt16(Data, 0);

            /// <summary>
            /// Converts the <see cref="Data"/> to <seealso cref="sbyte"/> <seealso cref="Array"/>.
            /// </summary>
            public sbyte[] DataAsSByte
            {
                get
                {
                    sbyte[] result = new sbyte[Data.Length];
                    for (int i = 0; i < Data.Length; i++)
                    {
                        result[i] = (sbyte)Data[i];
                    }
                    return result;
                }
            }

            #endregion Data Conversions

            #region Remove

            /// <summary>
            /// Removes <paramref name="node"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            public void Remove(FostrianNode node)
            {
                node.Parent = null;
                Values.Remove(node);
            }

            /// <summary>
            /// Removes <see cref="FostrianNode"/> at <paramref name="index"/>.
            /// </summary>
            /// <param name="index">Index of <see cref="FostrianNode"/>.</param>
            public void RemoveAt(int index)
            {
                var _node = Values[index];
                _node.Parent = null;
                Values.Remove(_node);
            }

            /// <summary>
            /// Removes all <see cref="FostrianNode"/>(s) that match the <paramref name="match"/>.
            /// </summary>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            public void RemoveAll(Predicate<FostrianNode> match)
            {
                for (int i = 0; i < Values.Count; i++)
                {
                    var node = Values[i];
                    if (match.Invoke(node))
                    {
                        Remove(node);
                    }
                }
            }

            /// <summary>
            /// Removes <see cref="FostrianNode"/> starting from <paramref name="index"/> and the next <paramref name="count"/> nodes.
            /// </summary>
            /// <param name="index">Starting Index.</param>
            /// <param name="count">Total Count</param>
            public void RemoveRange(int index, int count)
            {
                for (int i = index; i < index + count; i++)
                {
                    RemoveAt(i);
                }
            }

            #endregion Remove

            #region Insert

            /// <summary>
            /// Inserts <paramref name="node"/> into <paramref name="index"/>.
            /// </summary>
            /// <param name="index">Index of <paramref name="node"/>.</param>
            /// <param name="node"><see cref="FostrianNode"/></param>
            public void Insert(int index, FostrianNode node)
            {
                node.Parent = this;
                Values.Insert(index, node);
            }

            /// <summary>
            /// Inserts <paramref name="nodes"/> into <paramref name="index"/>.
            /// </summary>
            /// <param name="index">Starting index of <paramref name="nodes"/>.</param>
            /// <param name="nodes"><see cref="IEnumerable{T}"/> <seealso cref="FostrianNode"/></param>
            public void InsertRange(int index, IEnumerable<FostrianNode> nodes)
            {
                int _i = 0;
                foreach (FostrianNode node in nodes)
                {
                    node.Parent = this;
                    Values.Insert(index + _i, node);
                    _i++;
                }
            }

            #endregion Insert

            #region Find

            /// <summary>
            /// Finds a <see cref="FostrianNode"/> that matches the <paramref name="match"/>.
            /// </summary>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns><see cref="FostrianNode"/></returns>
            public FostrianNode Find(Predicate<FostrianNode> match) => Values.Find(match);

            /// <summary>
            /// Finds all <see cref="FostrianNode"/>(s) that match the <paramref name="match"/>.
            /// </summary>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/>"/></param>
            /// <returns><see cref="Array"/> of <seealso cref="FostrianNode"/>(s).</returns>
            public FostrianNode[] FindAll(Predicate<FostrianNode> match) => Values.FindAll(match).ToArray();

            #endregion Find

            #region Stuff

            /// <summary>
            /// Adds <paramref name="input"/>.
            /// </summary>
            /// <param name="input"><see cref="byte"/> <seealso cref="Array"/>.</param>
            public void AddRange(IEnumerable<byte[]> input)
            {
                foreach (byte[] bytes in input)
                {
                    Values.Add(new FostrianNode() { IsRoot = false, Parent = this, Data = bytes });
                }
            }

            /// <summary>
            /// removes all sub nodes in this node.
            /// </summary>
            public void Clear()
            {
                RemoveRange(0, Values.Count);
            }

            /// <summary>
            /// Checks if every <see cref="FostrianNode"/> matches <paramref name="match"/>.
            /// </summary>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns><c>true</c> if all nodes match, otherwise <c>false</c>.</returns>
            public bool TrueForAll(Predicate<FostrianNode> match) => Values.TrueForAll(match);

            /// <summary>
            /// Creates a read-only collection.
            /// </summary>
            /// <returns><see cref="IReadOnlyCollection{T}"/> <seealso cref="FostrianNode"/></returns>
            public IReadOnlyCollection<FostrianNode> AsReadOnly() => Values.AsReadOnly();

            /// <summary>
            /// Gets <see cref="FostrianNode"/>(s) starting from <paramref name="index"/> and the next <paramref name="count"/> nodes.
            /// </summary>
            /// <param name="index">Starting Index.</param>
            /// <param name="count">Number of <see cref="FostrianNode"/>(s).</param>
            /// <returns><see cref="Array"/> of <seealso cref="FostrianNode"/>.</returns>
            public FostrianNode[] GetRange(int index, int count) => Values.GetRange(index, count).ToArray();

            /// <summary>
            /// Checks if this node contains <paramref name="node"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <returns></returns>
            public bool Contains(FostrianNode node) => Values.Contains(node);

            /// <summary>
            /// Determines if any sub <see cref="FostrianNode"/>(s) match the <paramref name="match"/>.
            /// </summary>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns><c>true</c> if exists, otherwise <c>false</c>.</returns>
            public bool Exists(Predicate<FostrianNode> match) => Values.Exists(match);

            /// <summary>
            /// Finds the last <see cref="FostrianNode"/> that matches <paramref name="match"/>.
            /// </summary>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns><see cref="FostrianNode"/></returns>
            public FostrianNode FindLast(Predicate<FostrianNode> match) => Values.FindLast(match);

            #endregion Stuff

            #region BinarySearch

            /// <summary>
            /// Does a binary search for <paramref name="node"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <returns>Index of <paramref name="node"/>.</returns>
            public int BinarySearch(FostrianNode node) => Values.BinarySearch(node);

            /// <summary>
            /// Does a binary search for <paramref name="node"/> with <paramref name="comparer"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <param name="comparer"><see cref="IComparer{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns>Index of <paramref name="node"/>.</returns>
            public int BinarySearch(FostrianNode node, IComparer<FostrianNode> comparer) => Values.BinarySearch(node, comparer);

            /// <summary>
            /// Does a binary search for <paramref name="node"/> with <paramref name="comparer"/> starting from <paramref name="index"/> and within the next <paramref name="count"/>.
            /// </summary>
            /// <param name="index">Starting Index.</param>
            /// <param name="count">Count of how many <see cref="FostrianNode"/>(s) should be viewed.</param>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <param name="comparer"><see cref="IComparer{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns>Index of <paramref name="node"/>.</returns>
            public int BinarySearch(int index, int count, FostrianNode node, IComparer<FostrianNode> comparer) => Values.BinarySearch(index, count, node, comparer);

            #endregion BinarySearch

            #region FindIndex

            /// <summary>
            /// Finds index of a <see cref="FostrianNode"/> that matches <paramref name="match"/> by staring from <paramref name="startIndex"/> and within the next <paramref name="count"/>.
            /// </summary>
            /// <param name="startIndex">Starting Index.</param>
            /// <param name="count">Count of how many <see cref="FostrianNode"/>(s) should be viewed.</param>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns>Index of <see cref="FostrianNode"/>.</returns>
            public int FindIndex(int startIndex, int count, Predicate<FostrianNode> match) => Values.FindIndex(startIndex, count, match);

            /// <summary>
            /// Finds index of a <see cref="FostrianNode"/> that matches <paramref name="match"/> by staring from <paramref name="startIndex"/>.
            /// </summary>
            /// <param name="startIndex">Starting Index.</param>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns>Index of <see cref="FostrianNode"/>.</returns>
            public int FindIndex(int startIndex, Predicate<FostrianNode> match) => Values.FindIndex(startIndex, match);

            /// <summary>
            /// Finds index of a <see cref="FostrianNode"/> that matches <paramref name="match"/>.
            /// </summary>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns>Index of <see cref="FostrianNode"/>.</returns>
            public int FindIndex(Predicate<FostrianNode> match) => Values.FindIndex(match);

            #endregion FindIndex

            #region FindLastIndex

            /// <summary>
            /// Finds index of the last <see cref="FostrianNode"/> that matches <paramref name="match"/> by staring from <paramref name="startIndex"/> and within the next <paramref name="count"/>.
            /// </summary>
            /// <param name="startIndex">Starting Index.</param>
            /// <param name="count">Count of how many <see cref="FostrianNode"/>(s) should be viewed.</param>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns>Index of <see cref="FostrianNode"/>.</returns>
            public int FindLastIndex(int startIndex, int count, Predicate<FostrianNode> match) => Values.FindLastIndex(startIndex, count, match);

            /// <summary>
            /// Finds index of the last <see cref="FostrianNode"/> that matches <paramref name="match"/> by staring from <paramref name="startIndex"/>.
            /// </summary>
            /// <param name="startIndex">Starting Index.</param>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns>Index of <see cref="FostrianNode"/>.</returns>
            public int FindLastIndex(int startIndex, Predicate<FostrianNode> match) => Values.FindLastIndex(startIndex, match);

            /// <summary>
            /// Finds index of the last <see cref="FostrianNode"/> that matches <paramref name="match"/>.
            /// </summary>
            /// <param name="match"><see cref="Predicate{T}"/> <seealso cref="FostrianNode"/></param>
            /// <returns>Index of <see cref="FostrianNode"/>.</returns>
            public int FindLastIndex(Predicate<FostrianNode> match) => Values.FindLastIndex(match);

            #endregion FindLastIndex

            #region IndexOf

            /// <summary>
            /// Gets index of <paramref name="node"/> from <paramref name="index"/> and within the next <paramref name="count"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <param name="index">Starting Index.</param>
            /// <param name="count">Count of how many <see cref="FostrianNode"/>(s) should be viewed.</param>
            /// <returns>Index of <paramref name="node"/>.</returns>
            public int IndexOf(FostrianNode node, int index, int count) => Values.IndexOf(node, index, count);

            /// <summary>
            /// Gets index of <paramref name="node"/> from <paramref name="index"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <param name="index">Starting Index.</param>
            /// <returns>Index of <paramref name="node"/>.</returns>
            public int IndexOf(FostrianNode node, int index) => Values.IndexOf(node, index);

            /// <summary>
            /// Gets index of <paramref name="node"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <returns>Index of <paramref name="node"/>.</returns>
            public int IndexOf(FostrianNode node) => Values.IndexOf(node);

            #endregion IndexOf

            #region LastIndexOf

            /// <summary>
            /// Gets index of the last <paramref name="node"/> from <paramref name="index"/> and within the next <paramref name="count"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <param name="index">Starting Index.</param>
            /// <param name="count">Count of how many <see cref="FostrianNode"/>(s) should be viewed.</param>
            /// <returns>Index of <paramref name="node"/>.</returns>
            public int LastIndexOf(FostrianNode node, int index, int count) => Values.LastIndexOf(node, index, count);

            /// <summary>
            /// Gets index of the last <paramref name="node"/> from <paramref name="index"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <param name="index">Starting Index.</param>
            /// <returns>Index of <paramref name="node"/>.</returns>
            public int LastIndexOf(FostrianNode node, int index) => Values.LastIndexOf(node, index);

            /// <summary>
            /// Gets index of the last <paramref name="node"/>.
            /// </summary>
            /// <param name="node"><see cref="FostrianNode"/></param>
            /// <returns>Index of <paramref name="node"/>.</returns>
            public int LastIndexOf(FostrianNode node) => Values.LastIndexOf(node);

            #endregion LastIndexOf

            #region CopyTo

            /// <summary>
            /// Copies all the sub <see cref="FostrianNode"/>(s) into <paramref name="array"/> starting from <paramref name="index"/> and the next <paramref name="count"/> to <paramref name="arrayIndex"/>.
            /// </summary>
            /// <param name="index">Start Index.</param>
            /// <param name="array"><see cref="Array"/> <see cref="FostrianNode"/>.</param>
            /// <param name="arrayIndex">Start of the copied <see cref="FostrianNode"/>(s) at <paramref name="array"/>.</param>
            /// <param name="count">Count of how many <see cref="FostrianNode"/>(s) should be copied.</param>
            public void CopyTo(int index, FostrianNode[] array, int arrayIndex, int count) => Values.CopyTo(index, array, arrayIndex, count);

            /// <summary>
            /// Copies all the sub <see cref="FostrianNode"/>(s) into <paramref name="array"/> starting to <paramref name="arrayIndex"/>.
            /// </summary>
            /// <param name="array"><see cref="Array"/> <see cref="FostrianNode"/>.</param>
            /// <param name="arrayIndex">Start of the copied <see cref="FostrianNode"/>(s) at <paramref name="array"/>.</param>
            public void CopyTo(FostrianNode[] array, int arrayIndex) => Values.CopyTo(array, arrayIndex);

            /// <summary>
            /// Copies all the sub <see cref="FostrianNode"/>(s) into <paramref name="array"/>.
            /// </summary>
            /// <param name="array"><see cref="Array"/> <see cref="FostrianNode"/>.</param>
            public void CopyTo(FostrianNode[] array) => Values.CopyTo(array);

            #endregion CopyTo

            #region Other Properties

            /// <summary>
            /// Data of the node. Might contain data if the size of node is 0.
            /// </summary>
            public byte[] Data { get; set; }

            /// <summary>
            /// Determines if this is the root node or not.
            /// </summary>
            public bool IsRoot { get; internal set; }

            /// <summary>
            /// Size of the node.
            /// </summary>
            public int Size => Values.Count;

            /// <summary>
            /// Sub nodes of this node. Some of them are might be data.
            /// </summary>
            internal System.Collections.Generic.List<FostrianNode> Values { get; set; } = new System.Collections.Generic.List<FostrianNode>();

            /// <summary>
            /// Gets the parent node of this node.
            /// </summary>
            public FostrianNode Parent { get; set; }

            #endregion Other Properties
        }
    }
}