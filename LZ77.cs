using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LZ77
{
    class LZ77
    {
        int sizeBuffer = Convert.ToInt32(Math.Pow(2, 8));
        int sizeMask = Convert.ToInt32(Math.Pow(2, 5));
        string message;
        string writePath = @"fileLab1Result.bin";
        string readPath = "fileLab1.txt";
        string buffer = "";

        public LZ77()
        {
            message = ReadFile(readPath);
        }

        string ReadFile(string filename)
        {
            using (StreamReader sr = new StreamReader(filename, System.Text.Encoding.Default))
            {
                return sr.ReadToEnd();
            }
        }

        int findInBuffer(string substring)
        {
            return buffer.IndexOf(substring);
        }

        void addToBuffer(string str, int sizeBuff)
        {
            buffer += str;
            if (buffer.Length > sizeBuff)
                buffer = buffer.Substring(buffer.Length - sizeBuff, sizeBuff);
        }

        void FromTripletToByte(int item, ref List<byte> listBytes, ref int counterBytes, ref int capacityByte, int sizeBits)
        {
            byte mask = 255;
            if (sizeBits > 8)
            {
                listBytes[counterBytes] = (byte)(listBytes[counterBytes]|(item&mask));
                listBytes.Add(0);
                listBytes[counterBytes+1] = (byte)(listBytes[counterBytes+1] | (item >> 8));
                counterBytes++;
               // listBytes.Add(0);
            }
            else
            {
                listBytes[counterBytes] = (byte)(listBytes[counterBytes]|item);
                counterBytes++;
                listBytes.Add(0);
            }
        }

        public byte[] MessageToBits(List<(int, int, char)> triplet)
        {
            List<byte> listBytes = new List<byte>() { 0 };
            int counterBytes = 0;
            int capacityByte = 0;
            for (int i = 0; i < triplet.Count; i++)
            {
                FromTripletToByte(triplet[i].Item1,ref listBytes, ref counterBytes, ref capacityByte, sizeBuffer);
                FromTripletToByte(triplet[i].Item2, ref listBytes, ref counterBytes, ref capacityByte, sizeMask);
                FromTripletToByte(Encoding.GetEncoding(866).GetBytes(triplet[i].Item3+" ")[0] , ref listBytes, ref counterBytes, ref capacityByte, 8);
            }
            return listBytes.ToArray();
        }

        public void Encode()
        {
            List<(int, int, char)> triplet = new List<(int, int, char)>();
            int indexSubstring = 0;
            string substring;
            for (int i = 0; i < message.Length; i++)
            {
                for (int j = sizeMask; j > 0; j--)
                {
                    try { substring = message.Substring(i, j); }
                    catch (ArgumentOutOfRangeException)
                    {
                        continue;
                    }
                    indexSubstring = findInBuffer(substring);
                    if (indexSubstring != -1)
                    {
                        if (i == message.Length - 1)
                        {
                            addToBuffer(substring + '\0', sizeBuffer);
                            triplet.Add((indexSubstring, j, '\0'));
                        }
                        else
                        {
                            addToBuffer(substring + message[i + j], sizeBuffer);
                            triplet.Add((indexSubstring, j, message[i + j]));
                        }
                        i += j;
                        break;
                    }
                    else if (j == 1)
                    {
                        addToBuffer(message[i].ToString(), sizeBuffer);
                        triplet.Add((0, 0, message[i]));
                        break;
                    };
                }
            }
            buffer = "";
            WriteFile(MessageToBits(triplet));
        }

        public void Decode()
        {
            (int, int, char) triple;
            string substring;
            string substringBuffer;
            string deMessage = "";
            string enMessage = ReadFile(writePath);
            for (int i = 0; i < enMessage.Length; i += 3)
            {
                substring = enMessage.Substring(i, 3);
                triple = ((int)Char.GetNumericValue(substring[0]), (int)Char.GetNumericValue(substring[1]), substring[2]);
                if (triple.Item2 == 0)
                {
                    addToBuffer(triple.Item3.ToString(), sizeBuffer);
                    deMessage += triple.Item3.ToString();
                }
                else
                {
                    substringBuffer = buffer.Substring(triple.Item1, triple.Item2) + triple.Item3;
                    addToBuffer(substringBuffer, sizeBuffer);
                    deMessage += substringBuffer;
                }
            }
            Console.WriteLine(deMessage);
        }



        public void WriteFile(byte[] arrayBytes)
        {
            using (BinaryWriter sw = new BinaryWriter(File.Open(writePath, FileMode.Create)))
            {
                sw.Write(ReversePow(sizeBuffer));
                sw.Write(ReversePow(sizeMask));
                foreach (byte temp in arrayBytes)
                    sw.Write(temp);
            }
        }

        public void unpack()
        {
            int sizeBuffer;
            int sizeMask;
            List<byte> list = new List<byte>();
            using (BinaryReader br = new BinaryReader(File.Open(writePath, FileMode.Open)))
            {
                sizeBuffer = br.ReadInt32();
                sizeMask = br.ReadInt32();
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    list.Add(br.ReadByte());
                }
            }
            Console.WriteLine(DecompressionMessage(new int[3] { sizeBuffer,sizeMask,8}, ref list));
        }

        byte ConcatMask(int countBits)
        {
            byte num = 0;
            for (int i = 0; i < countBits; i++)
                num += (byte)Math.Pow(2, i);
            return num;
        }

        int ReversePow(int number)
        {
            int counter = 0;
            while (number >= 2)
            {
                number = number / 2;
                counter++;
            }
            return counter;
        }

        void FindMessage((int,int,char) triplet,ref string resultMessage, int sizeBuff)
        {
            string substringBuffer;
            if (triplet.Item2 == 0)
            {
                addToBuffer(triplet.Item3.ToString(), sizeBuff);
                resultMessage += triplet.Item3.ToString();
            }
            else
            {
                substringBuffer = buffer.Substring(triplet.Item1, triplet.Item2) + triplet.Item3;
                addToBuffer(substringBuffer, sizeBuff);
                resultMessage += substringBuffer;
            }
        }

        string DecompressionMessage(int[] sizesBuffandMask, ref List<byte> messageFromFile)
        {
            int tempSize;
            int counterTriplet = 0;
            (int, int, char) tempTriplet = (0, 0, ' ');
            int bufferLength = Convert.ToInt32(Math.Pow(2, sizesBuffandMask[0]));
            string resultString = "";
            byte mask = 255, concat = 0, lessByte=0;
            int concatingItem = 0, nextByte;
            for (int i = 0; i < messageFromFile.Count; i++)
            {
                tempSize = sizesBuffandMask[counterTriplet];
                if (tempSize > 8)
                {
                    concat = (byte)(messageFromFile[i] & mask);
                    nextByte = messageFromFile[i+1] & (mask>>tempSize-8);
                    concatingItem = (nextByte<<8) | concat;
                    messageFromFile[i + 1] = (byte)(messageFromFile[i + 1] >> (tempSize - 8));
                    if (counterTriplet == 0)
                        tempTriplet.Item1 = concatingItem;
                    if (counterTriplet == 1)
                        tempTriplet.Item2 = concatingItem;
                    if (counterTriplet == 2)
                        tempTriplet.Item3 = (char)concatingItem ;
                    counterTriplet++;
                    if (counterTriplet == 3)
                    {
                        FindMessage(tempTriplet,ref resultString, bufferLength);
                        counterTriplet = 0;
                    }                       
                }
                else
                {
                    lessByte = (byte)(messageFromFile[i] & (mask >> (8 - tempSize)));
                    if (counterTriplet == 0)
                        tempTriplet.Item1 = lessByte;
                    if (counterTriplet == 1)
                        tempTriplet.Item2 = lessByte;
                    if (counterTriplet == 2)
                        tempTriplet.Item3 = Encoding.GetEncoding(866).GetChars(new byte[] { lessByte,0})[0];
                    counterTriplet++;
                    if (counterTriplet == 3)
                    {
                        FindMessage(tempTriplet,  ref resultString, bufferLength);
                        counterTriplet = 0;
                    }
                        
                }
            }
            return resultString;
        }
    }
}
