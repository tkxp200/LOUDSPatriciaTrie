using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace TrieUtil;

public class BitVector
{
    // max n = 2^32
    // lg(n)^2 = 1024 : ushort
    // lg(n)/2 = 16

    // const int BIG_BLOCK_SPLIT_SIZE = 8;
    // const int SMALL_BLOCK_SPLIT_SIZE = 4;
    const int BIG_BLOCK_SPLIT_SIZE = 1024;
    const int SMALL_BLOCK_SPLIT_SIZE = 16;

    const int SMALL_BLOCK_SIZE = BIG_BLOCK_SPLIT_SIZE / SMALL_BLOCK_SPLIT_SIZE;

    const int MAX_OUTPUT_BITSIZE = 1024;
    const int MAX_INDEX = 15;

    private readonly List<ushort> _bitArray;
    private readonly uint _size;
    private readonly int[] _bigBlock;
    private readonly short[,] _smallBlock;

    public BitVector(List<ushort> bitArray)
    {
        _bitArray = bitArray;
        _size = (uint)_bitArray.Count * SMALL_BLOCK_SPLIT_SIZE;
        int bigBlockSize = bitArray.Count / SMALL_BLOCK_SIZE;
        _bigBlock = new int[bigBlockSize + 1]; // bigBlock[0] = 0
        _smallBlock = new short[bigBlockSize, SMALL_BLOCK_SIZE + 1]; //smallBlock[*, 0] = 0
        Build();
    }


    private int Rank1(int position)
    {
        if ((uint)position > _size) return -1;

        int count = 0;
        int bigBlockIndex = position / BIG_BLOCK_SPLIT_SIZE;
        count += _bigBlock[bigBlockIndex];
        int smallBlockIndex = position % BIG_BLOCK_SPLIT_SIZE / SMALL_BLOCK_SPLIT_SIZE;
        count += _smallBlock[bigBlockIndex, smallBlockIndex];
        uint mask = (uint)(1 << (position % SMALL_BLOCK_SPLIT_SIZE)) - 1;
        count += BitOperations.PopCount(_bitArray[position / SMALL_BLOCK_SPLIT_SIZE] & mask);
        return count;
    }

    public int Rank0(int position)
    {
        return position - Rank1(position);
    }

    public int Select1(int count)
    {
        if ((uint)count > _size) return -1;
        if (count == 0) return -1;

        int position = 0;
        int remain = count;

        int left = -1;
        int right = _bigBlock.Length;
        while (right - left > 1)
        {
            int mid = (left + right) / 2;

            if (_bigBlock[mid] < remain) left = mid;
            else right = mid;
        }
        int bigIndex = left;
        position += bigIndex * BIG_BLOCK_SPLIT_SIZE;
        remain -= _bigBlock[bigIndex];

        left = -1;
        right = _smallBlock.GetLength(1);
        while (right - left > 1)
        {
            int mid = (left + right) / 2;

            if (_smallBlock[bigIndex, mid] < remain) left = mid;
            else right = mid;
        }
        int smallIndex = left;
        position += smallIndex * SMALL_BLOCK_SPLIT_SIZE;
        remain -= _smallBlock[bigIndex, smallIndex];

        var mask = _bitArray[position / SMALL_BLOCK_SPLIT_SIZE];
        for (remain--; remain > 0; remain--)
        {
            mask &= (ushort)(mask - 1);
        }

        return position + BitOperations.TrailingZeroCount(mask) + 1;
    }

    public bool Get(int position)
    {
        var index = position / SMALL_BLOCK_SPLIT_SIZE;
        var bitPos = position % SMALL_BLOCK_SPLIT_SIZE;
        return (_bitArray[index] & (1 << bitPos)) != 0;
    }

    private void Build()
    {
        int count = 0;
        int smallBlockIndex = 0;
        int bigBlockIndex = 1;
        var span = CollectionsMarshal.AsSpan(_bitArray);
        for (int i = 0; i < span.Length; i++)
        {
            smallBlockIndex++;
            count += BitOperations.PopCount(span[i]);
            _smallBlock[bigBlockIndex - 1, smallBlockIndex] = (short)count;
            if (smallBlockIndex == SMALL_BLOCK_SIZE)
            {
                _bigBlock[bigBlockIndex] = _bigBlock[bigBlockIndex - 1] + count;
                bigBlockIndex++;
                smallBlockIndex = 0;
                count = 0;
            }
        }
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"BigBlockSize: {_bigBlock.Length}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"SmallBlockSize: {_smallBlock.GetLength(0)},{_smallBlock.GetLength(1)}");
        builder.AppendLine();

        if (_size > MAX_OUTPUT_BITSIZE) return builder.ToString();

        builder.AppendLine(CultureInfo.InvariantCulture, $"BitVector Count: {_bitArray.Count}");

        builder.AppendLine("BitVector:");
        for (int i = 0; i < MathF.Min(_bitArray.Count, MAX_OUTPUT_BITSIZE); i++)
        {
            builder.Append(Convert.ToString(_bitArray[i], 2).PadLeft(MAX_INDEX + 1, '0').Reverse().ToArray());
            builder.AppendLine();
        }
        builder.AppendLine();
        builder.AppendLine();

        builder.AppendLine("BigBlock:");
        builder.AppendLine(string.Join(",", _bigBlock));
        builder.AppendLine();

        builder.AppendLine("smallBlock:");
        for (int i = 0; i < _smallBlock.GetLength(0); i++)
        {
            var rowArray = Enumerable.Range(0, _smallBlock.GetLength(1))
                                    .Select(j => _smallBlock[i, j]);
            builder.AppendLine(string.Join(",", rowArray));
        }
        builder.AppendLine();

        return builder.ToString();
    }
}

public class BitVectorBuilder
{
    const int MAX_OUTPUT_BITSIZE = 128;
    const int MAX_INDEX = 15;
    const int BIG_BLOCK_SPLIT_SIZE = 1024;
    const int SMALL_BLOCK_SPLIT_SIZE = 16;
    const ushort MAX_VALUE = ushort.MaxValue;

    const int SMALL_BLOCK_SIZE = BIG_BLOCK_SPLIT_SIZE / SMALL_BLOCK_SPLIT_SIZE; //64

    private readonly List<ushort> bitVectorList;
    private int bitPos;
    private int listIndex = -1;
    private int capacity;
    private ushort bitVector = MAX_VALUE;

    public BitVectorBuilder()
    {
        capacity = SMALL_BLOCK_SIZE;
        bitVectorList = new(capacity);

        AddRoot();
    }

    private void AddRoot()
    {
        Add(false);
        Add(true);
    }

    public void Add(bool value)
    {
        if (!value) bitVector &= (ushort)~(1 << bitPos);

        bitPos++;

        if (bitPos > MAX_INDEX)
        {
            listIndex++;
            if (listIndex >= capacity)
            {
                capacity += SMALL_BLOCK_SIZE;
                bitVectorList.Capacity = capacity;
            }

            bitVectorList.Add(bitVector);
            bitVector = MAX_VALUE;
            bitPos = 0;
        }
    }

    public BitVector Build()
    {
        if (bitPos != 0) bitVectorList.Add(bitVector);

        FillToCapacity();

        return new BitVector(bitVectorList);
    }

    public void FillToCapacity()
    {
        while (bitVectorList.Count < capacity)
        {
            bitVectorList.Add(MAX_VALUE);
        }
    }

    public override string ToString()
    {

        var builder = new StringBuilder();
        if (bitVectorList.Count > MAX_OUTPUT_BITSIZE)
            builder.AppendLine("The BitVector is too large. Only a portion will be displayed.\n");

        builder.AppendLine(CultureInfo.InvariantCulture, $"BitVector Count: {bitVectorList.Count}");

        builder.AppendLine("BitVector:");
        for (int i = 0; i < MathF.Min(bitVectorList.Count, MAX_OUTPUT_BITSIZE); i++)
        {
            builder.AppendLine(Convert.ToString(bitVectorList[i], 2).PadLeft(16, '0'));
        }
        if (bitPos != 0) builder.AppendLine(Convert.ToString(bitVector, 2).PadLeft(16, '0'));

        return builder.ToString();
    }

}
