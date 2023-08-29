using System;
using System.IO;

class LZRW1KH
{
    const int BufferMaxSize = 32768; // if file size is bigger, it will fail #TODO 🔧🐛
    const int BufferMaxTwice = BufferMaxSize*2;
    const byte FLAG_Copied = 0x80;
    const byte FLAG_Compress = 0x40;
        
    short[] hashTable = new short[4096];    
    bool GetMatch(byte[] source, int x, int sourceSize, short[] hash,
                 out int matchSize, out int matchPos)
    {        
        int hashValue = (40543 * ((((source[x] << 4) ^ source[x + 1]) << 4) ^ source[x + 2]) >> 4) & 0xFFF;

        matchSize = 0;
        matchPos = -1;

        if (hash[hashValue] != -1 && x - hash[hashValue] < 4096)
        {
            matchPos = hash[hashValue];
            matchSize = 0;

            while (matchSize < 18 && source[x + matchSize] == source[matchPos + matchSize]
                   && x + matchSize < sourceSize)
            {
                matchSize++;
            }

            if (matchSize >= 3)
                return true;
        }
        
        hash[hashValue] = (short)x;
        return false;
    }
    int Compress(byte[] source, byte[] dest, int sourceSize)
    {
        Array.Fill(hashTable, (short)-1);

        dest[0] = FLAG_Compress;

        int x = 0;
        int y = 3;
        int z = 1;
        int bit = 0;
        int command = 0;

        while (x < sourceSize && y <= sourceSize)
        {       
            if (bit > 15)
            {
                dest[z] = (byte)(command >> 8);
                dest[z + 1] = (byte)command;
                z = y;
                bit = 0;
                y += 2;
            }

            int size = 1;
            while (size < 0xFFF && x + size < sourceSize && source[x] == source[x + size])
                size++;

            if (size >= 16)
            {
                dest[y] = 0;
                dest[y + 1] = (byte)(size - 16 >> 8);
                dest[y + 2] = (byte)(size - 16);
                dest[y + 3] = source[x];
                y += 4;
                x += size;
                command = (command << 1) + 1;
            }
            else
            {
                int matchPos;
                int matchSize;
                if (GetMatch(source, x, sourceSize, hashTable, out matchSize, out matchPos))
                {
                    int key = ((x - matchPos) << 4) + (matchSize - 3);
                    dest[y] = (byte)(key >> 8);
                    dest[y + 1] = (byte)key;
                    y += 2;
                    x += matchSize;
                    command = (command << 1) + 1;
                }
                else
                {
                    dest[y++] = source[x++];
                    command <<= 1;
                }
            }

            bit++;
        }

        command <<= (16 - bit);
        dest[z] = (byte)(command >> 8);
        dest[z + 1] = (byte)command;

        if (y > sourceSize)
        {
            Array.Copy(source, 0, dest, 1, sourceSize);
            dest[0] = FLAG_Copied;
            y = sourceSize + 1;
        }

        return y;
    }
    int Compress2(byte[] source, byte[] dest, int sourceBytes)
    {
        int srcOffset = 0;
        int destOffset = 0;

        while (srcOffset < sourceBytes)
        {
            int chunkSize = Math.Min(BufferMaxSize, sourceBytes - srcOffset);

            int compressedSize = CompressChunk(source, dest, srcOffset, chunkSize);

            Array.Copy(dest, 0, dest, destOffset, compressedSize);

            srcOffset += chunkSize;
            destOffset += compressedSize;
        }

        return destOffset;
    }
    int CompressChunk(byte[] source, byte[] dest, int srcOffset, int chunkSize)
    {
        Array.Fill(hashTable, (short)-1);

        dest[0] = FLAG_Compress;

        int x = srcOffset;
        int y = 3;
        int z = 1;
        int bit = 0;
        int command = 0;

        while (x < srcOffset + chunkSize && y <= chunkSize)
        {
            if (bit > 15)
            {
                dest[z] = (byte)(command >> 8);
                dest[z + 1] = (byte)command;
                z = y;
                bit = 0;
                y += 2;
            }

            int size = 1;
            while (size < 0xFFF && x + size < chunkSize && source[x] == source[x + size])
                size++;

            if (size >= 16)
            {
                dest[y] = 0;
                dest[y + 1] = (byte)(size - 16 >> 8);
                dest[y + 2] = (byte)(size - 16);
                dest[y + 3] = source[x];
                y += 4;
                x += size;
                command = (command << 1) + 1;
            }
            else
            {
                int matchPos;
                int matchSize;
                if (GetMatch(source, x, chunkSize, hashTable, out matchSize, out matchPos))
                {
                    int key = ((x - matchPos) << 4) + (matchSize - 3);
                    dest[y] = (byte)(key >> 8);
                    dest[y + 1] = (byte)key;
                    y += 2;
                    x += matchSize;
                    command = (command << 1) + 1;
                }
                else
                {
                    dest[y++] = source[x++];
                    command <<= 1;
                }
            }

            bit++;
        }

        command <<= (16 - bit);
        dest[z] = (byte)(command >> 8);
        dest[z + 1] = (byte)command;

        if (y > chunkSize)
        {
            Array.Copy(source, 0, dest, 1, chunkSize);
            dest[0] = FLAG_Copied;
            y = chunkSize + 1;
        }

        return y;
    }

    int Decompress(byte[] source, byte[] dest, int sourceSize)
    {
        if (sourceSize <= 1)
            return 0;

        if (source[0] == FLAG_Copied)
        {
            Array.Copy(source, 1, dest, 0, sourceSize - 1);
            return sourceSize - 1;
        }

        int x = 3;
        int y = 0;

        int command = (source[1] << 8) | source[2];
        int bit = 16;

        while (x < sourceSize)
        {

            if (bit == 0)
            {
                command = (source[x] << 8) | source[x + 1];
                x += 2;
                bit = 16;
            }

            if ((command & 0x8000) == 0)
            {
                dest[y++] = source[x++];
            }
            else
            {
                int pos = (source[x] << 4) | (source[x + 1] >> 4);
                if (pos == 0)
                {
                    int size = (source[x + 1] << 8) | source[x + 2] + 15;
                    for (int k = 0; k <= size; k++) 
                        dest[y + k] = source[x + 3];
                    x += 4;
                    y += size + 1;
                }
                else
                {
                    int size = (source[x + 1] & 0x0F) + 2;
                    for (int k = 0; k <= size; k++)
                        dest[y + k] = dest[y - pos + k];
                    x += 2;
                    y += size + 1;
                }
            }

            command <<= 1;
            bit--;
        }

        return y;
    }
    
    public byte[] ByteArrayDecompress(byte[] src)
    {
        LZRW1KH compressor = new LZRW1KH();

        byte[] buffer = new byte[BufferMaxSize];
        byte[] DSTBuf = new byte[BufferMaxSize];
        int bytesRead;
        
        List<byte> dst = new List<byte>();

        for (int offset = 0; offset < src.Length; offset += BufferMaxSize) 
        { 
            bytesRead = Math.Min(BufferMaxSize, src.Length - offset);

            Array.Copy(src, offset, buffer, 0, bytesRead);
            int decompressedSize = compressor.Decompress(buffer, DSTBuf, bytesRead);            
            dst.AddRange(DSTBuf.Take(decompressedSize));            
        }
        return dst.ToArray();
    }

    public byte[] SingleByteArrayDecompress(byte[] src)
    {
        const int BufferMaxSize = 32768;

        byte[] buffer = new byte[BufferMaxSize];
        List<byte> dst = new List<byte>();

        for (int offset = 0; offset < src.Length; offset += BufferMaxSize)
        {
            int bytesRead = Math.Min(BufferMaxSize, src.Length - offset);
            Array.Copy(src, offset, buffer, 0, bytesRead);

            if (bytesRead <= 1)
                continue;

            if (buffer[0] == FLAG_Copied)
            {
                dst.AddRange(buffer.Skip(1).Take(bytesRead - 1));
                continue;
            }

            int x = 3;
            int y = 0;
            int command = (buffer[1] << 8) | buffer[2];
            int bit = 16;

            while (x < bytesRead)
            {
                if (bit == 0)
                {
                    command = (buffer[x] << 8) | buffer[x + 1];
                    x += 2;
                    bit = 16;
                }

                if ((command & 0x8000) == 0)
                {
                    dst.Add(buffer[x++]);
                }
                else
                {
                    // Copied from Decompress method
                    int pos = (buffer[x] << 4) | (buffer[x + 1] >> 4);
                    if (pos == 0)
                    {
                        int size = (buffer[x + 1] << 8) | buffer[x + 2] + 15;
                        for (int k = 0; k <= size; k++)
                            dst.Add(buffer[x + 3]);
                        x += 4;
                        y += size + 1;
                    }
                    else
                    {
                        int size = (buffer[x + 1] & 0x0F) + 2;
                        for (int k = 0; k <= size; k++)
                            dst.Add(dst[dst.Count - pos + k]);
                        x += 2;
                        y += size + 1;
                    }
                }

                command <<= 1;
                bit--;
            }
        }

        return dst.ToArray();
    }
    public static void Main(string[] args)
    {
        LZRW1KH compressor = new LZRW1KH();

        if (args[0] == "compressOLD")
        {
            using (FileStream input = File.OpenRead(args[1]))
            using (FileStream output = File.Create(args[2]))
            {
                byte[] buffer = new byte[BufferMaxSize];
                byte[] DSTBuf = new byte[BufferMaxSize];

                int bytesRead;

                while ((bytesRead = input.Read(buffer, 0, BufferMaxSize)) > 0)                
                {
                    int compressedSize = compressor.Compress(buffer, DSTBuf, bytesRead);
                    output.Write(DSTBuf, 0, compressedSize);
                }                
            }
        }
        else if (args[0] == "compress")
        {
            int srcSize;
            
            using (FileStream input = File.OpenRead(args[1]))
            using (FileStream output = File.Create(args[2]))
            {
                byte[] SrcBuf = new byte[BufferMaxTwice];
                byte[] DSTBuf = new byte[BufferMaxTwice];   
                                
                while((srcSize = input.Read(SrcBuf, 0, SrcBuf.Length))>0)
                {                                        
                    int DSTSize = compressor.Compress(SrcBuf, DSTBuf, srcSize);
                    //output.Write(BitConverter.GetBytes((short)DSTSize), 0, sizeof(short));
                    output.Write(DSTBuf, 0, DSTSize);                    
                }
            }

        }
        else if (args[0] == "decompress")
        {
            byte[] entireFileAsBytesArray = File.ReadAllBytes(args[1]);
            byte[] entireDecompressedFileAsBytesArray = compressor.ByteArrayDecompress(entireFileAsBytesArray);
            File.WriteAllBytes(args[2], entireDecompressedFileAsBytesArray);
            
            //using the similar approach as original algorithm parameters
            /*using (FileStream input = File.OpenRead(args[1]))
            {
                using (FileStream output = File.Create(args[2]))
                {
                    
                    byte[] buffer = new byte[BufferMaxTwice];
                    byte[] DSTBuf = new byte[BufferMaxTwice];
                    int bytesRead;

                    while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        int decompressedSize = compressor.Decompress(buffer, DSTBuf, bytesRead);
                        
                        output.Write(DSTBuf, 0, decompressedSize);
                        /*for (int i = 0; i < decompressedSize; i++)
                        {
                            Console.WriteLine(compressor.destBuffer[i].ToString());
                        }*
                    }
                }
            }*/
        }
    }
}