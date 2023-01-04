using System;
using System.Security.Cryptography;
using System.Text;

namespace ZModLauncher;

public class DropboxContentHasher : HashAlgorithm
{
    public const int BLOCK_SIZE = 4 * 1024 * 1024;

    private const string HEX_DIGITS = "0123456789abcdef";
    private readonly SHA256 blockHasher;
    private readonly SHA256 overallHasher;
    private int blockPos;

    public DropboxContentHasher() : this(SHA256.Create(), SHA256.Create(), 0) { }

    public DropboxContentHasher(SHA256 overallHasher, SHA256 blockHasher, int blockPos)
    {
        this.overallHasher = overallHasher;
        this.blockHasher = blockHasher;
        this.blockPos = blockPos;
    }

    public override int HashSize => overallHasher.HashSize;

    protected override void HashCore(byte[] input, int offset, int len)
    {
        int inputEnd = offset + len;
        while (offset < inputEnd)
        {
            if (blockPos == BLOCK_SIZE)
            {
                FinishBlock();
            }
            int spaceInBlock = BLOCK_SIZE - blockPos;
            int inputPartEnd = Math.Min(inputEnd, offset + spaceInBlock);
            int inputPartLength = inputPartEnd - offset;
            blockHasher.TransformBlock(input, offset, inputPartLength, input, offset);
            blockPos += inputPartLength;
            offset += inputPartLength;
        }
    }

    protected override byte[] HashFinal()
    {
        if (blockPos > 0)
        {
            FinishBlock();
        }
        overallHasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return overallHasher.Hash;
    }

    public override void Initialize()
    {
        blockHasher.Initialize();
        overallHasher.Initialize();
        blockPos = 0;
    }

    private void FinishBlock()
    {
        blockHasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        byte[] blockHash = blockHasher.Hash;
        blockHasher.Initialize();
        overallHasher.TransformBlock(blockHash, 0, blockHash.Length, blockHash, 0);
        blockPos = 0;
    }

    /// <summary>
    ///     A convenience method to convert a byte array into a hexadecimal string.
    /// </summary>
    public static string ToHex(byte[] data)
    {
        var r = new StringBuilder();
        foreach (byte b in data)
        {
            r.Append(HEX_DIGITS[b >> 4]);
            r.Append(HEX_DIGITS[b & 0xF]);
        }
        return r.ToString();
    }
}