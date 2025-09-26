namespace Antlr.Runtime
{
    using System.Collections.Generic;

    using StringBuilder = System.Text.StringBuilder;

    /** <summary>
     *  A stripped-down version of org.antlr.misc.BitSet that is just
     *  good enough to handle runtime requirements such as FOLLOW sets
     *  for automatic error recovery.
     *  </summary>
     */
    [Serializable]
    public sealed class BitSet : ICloneable
    {
        private const int BITS = 64;    // number of bits / long
        private const int LOG_BITS = 6; // 2^6 == 64

        /** <summary>
         *  We will often need to do a mod operator (i mod nbits).  Its
         *  turns out that, for powers of two, this mod operation is
         *  same as (i &amp; (nbits-1)).  Since mod is slow, we use a
         *  precomputed mod mask to do the mod instead.
         *  </summary>
         */
        private const int MOD_MASK = BITS - 1;

        /** <summary>The actual data bits</summary> */
        private ulong[] _bits;

        /** <summary>Construct a bitset of size one word (64 bits)</summary> */
        public BitSet()
            : this(BITS)
        {
        }

        /** <summary>Construction from a static array of longs</summary> */
        public BitSet(ulong[] bits)
        {
            _bits = bits;
        }

        /** <summary>Construction from a list of integers</summary> */
        public BitSet(IEnumerable<int> items)
            : this()
        {
            foreach (var i in items)
                Add(i);
        }

        /** <summary>Construct a bitset given the size</summary>
         *  <param name="nbits">The size of the bitset in bits</param>
         */
        public BitSet(int nbits)
        {
            _bits = new ulong[((nbits - 1) >> LOG_BITS) + 1];
        }

        public static BitSet Of(int el)
        {
            var s = new BitSet(el + 1);
            s.Add(el);
            return s;
        }

        public static BitSet Of(int a, int b)
        {
            var s = new BitSet(Math.Max(a, b) + 1);
            s.Add(a);
            s.Add(b);
            return s;
        }

        public static BitSet Of(int a, int b, int c)
        {
            var s = new BitSet();
            s.Add(a);
            s.Add(b);
            s.Add(c);
            return s;
        }

        public static BitSet Of(int a, int b, int c, int d)
        {
            var s = new BitSet();
            s.Add(a);
            s.Add(b);
            s.Add(c);
            s.Add(d);
            return s;
        }

        /** <summary>return this | a in a new set</summary> */
        public BitSet Or(BitSet a)
        {
            if (a == null) {
                return this;
            }
            var s = (BitSet) this.Clone();
            s.OrInPlace(a);
            return s;
        }

        /** <summary>or this element into this set (grow as necessary to accommodate)</summary> */
        public void Add(int el)
        {
            var n = WordNumber(el);
            if (n >= _bits.Length) {
                GrowToInclude(el);
            }
            _bits[n] |= BitMask(el);
        }

        /** <summary>Grows the set to a larger number of bits.</summary>
         *  <param name="bit">element that must fit in set</param>
         */
        public void GrowToInclude(int bit)
        {
            var newSize = Math.Max(_bits.Length << 1, NumWordsToHold(bit));
            SetSize(newSize);
        }

        public void OrInPlace(BitSet a)
        {
            if (a == null) {
                return;
            }
            // If this is smaller than a, grow this first
            if (a._bits.Length > _bits.Length) {
                SetSize(a._bits.Length);
            }
            var min = Math.Min(_bits.Length, a._bits.Length);
            for (var i = min - 1; i >= 0; i--) {
                _bits[i] |= a._bits[i];
            }
        }

        /** <summary>Sets the size of a set.</summary>
         *  <param name="nwords">how many words the new set should be</param>
         */
        private void SetSize(int nwords)
        {
            Array.Resize(ref _bits, nwords);
        }

        private static ulong BitMask(int bitNumber)
        {
            var bitPosition = bitNumber & MOD_MASK; // bitNumber mod BITS
            return 1UL << bitPosition;
        }

        public object Clone()
        {
            return new BitSet((ulong[]) _bits.Clone());
        }

        public int Size()
        {
            var deg = 0;
            for (var i = _bits.Length - 1; i >= 0; i--) {
                var word = _bits[i];
                if (word != 0L) {
                    for (var bit = BITS - 1; bit >= 0; bit--) {
                        if ((word & (1UL << bit)) != 0) {
                            deg++;
                        }
                    }
                }
            }
            return deg;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object other)
        {
            if (other == null || !(other is BitSet)) {
                return false;
            }

            var otherSet = (BitSet) other;

            var n = Math.Min(this._bits.Length, otherSet._bits.Length);

            // for any bits in common, compare
            for (var i = 0; i < n; i++) {
                if (this._bits[i] != otherSet._bits[i]) {
                    return false;
                }
            }

            // make sure any extra bits are off

            if (this._bits.Length > n) {
                for (var i = n + 1; i < this._bits.Length; i++) {
                    if (this._bits[i] != 0) {
                        return false;
                    }
                }
            }
            else if (otherSet._bits.Length > n) {
                for (var i = n + 1; i < otherSet._bits.Length; i++) {
                    if (otherSet._bits[i] != 0) {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool Member(int el)
        {
            if (el < 0) {
                return false;
            }
            var n = WordNumber(el);
            if (n >= _bits.Length)
                return false;
            return (_bits[n] & BitMask(el)) != 0;
        }

        // remove this element from this set
        public void Remove(int el)
        {
            var n = WordNumber(el);
            if (n < _bits.Length) {
                _bits[n] &= ~BitMask(el);
            }
        }

        public bool IsNil()
        {
            for (var i = _bits.Length - 1; i >= 0; i--) {
                if (_bits[i] != 0)
                    return false;
            }
            return true;
        }

        private static int NumWordsToHold(int el)
        {
            return (el >> LOG_BITS) + 1;
        }

        public int NumBits()
        {
            return _bits.Length << LOG_BITS; // num words * bits per word
        }

        /** <summary>return how much space is being used by the bits array not how many actually have member bits on.</summary> */
        public int LengthInLongWords()
        {
            return _bits.Length;
        }

        /**Is this contained within a? */
        /*
        public boolean subset(BitSet a) {
            if (a == null || !(a instanceof BitSet)) return false;
            return this.and(a).equals(this);
        }
        */

        public int[] ToArray()
        {
            var elems = new int[Size()];
            var en = 0;
            for (var i = 0; i < (_bits.Length << LOG_BITS); i++) {
                if (Member(i)) {
                    elems[en++] = i;
                }
            }
            return elems;
        }

        private static int WordNumber(int bit)
        {
            return bit >> LOG_BITS; // bit / BITS
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(string[] tokenNames)
        {
            var buf = new StringBuilder();
            var separator = ",";
            var havePrintedAnElement = false;
            buf.Append('{');

            for (var i = 0; i < (_bits.Length << LOG_BITS); i++) {
                if (Member(i)) {
                    if (i > 0 && havePrintedAnElement) {
                        buf.Append(separator);
                    }
                    if (tokenNames != null) {
                        buf.Append(tokenNames[i]);
                    }
                    else {
                        buf.Append(i);
                    }
                    havePrintedAnElement = true;
                }
            }
            buf.Append('}');
            return buf.ToString();
        }
    }
}
