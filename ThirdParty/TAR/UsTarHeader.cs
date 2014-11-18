using System;
using System.Net;
using System.Text;

namespace tar_cs
{
    /// <summary>
    /// UsTar header implementation.
    /// </summary>
    internal class UsTarHeader : TarHeader
    {
        private const string magic = "ustar\0";
        private const string version = "  ";
        private string groupName;

        public string namePrefix = string.Empty;
        private string userName;

        public override string UserName
        {
            get { return userName.Replace("\0",string.Empty); }
            set
            {
                if (value.Length > 32)
                {
                    throw new TarException("user name can not be longer than 32 chars");
                }
                userName = value;
            }
        }

        public override string GroupName
        {
            get { return groupName.Replace("\0",string.Empty); }
            set
            {
                if (value.Length > 32)
                {
                    throw new TarException("group name can not be longer than 32 chars");
                }
                groupName = value;
            }
        }

        public override string FileName
        {
            get { return namePrefix.Replace("\0", string.Empty) + base.FileName.Replace("\0", string.Empty); }
            set
            {
                if (value.Length >= 100)
                {
                    if (value.Length > 255)
                    {
                        throw new TarException("UsTar fileName can not be longer thatn 255 chars");
                    }
                    int position = value.Length - 99;

                    // Find first path separator in the remaining 100 chars of the file name
                    while (!IsPathSeparator(value[position]))
                    {
                        ++position;
                        if (position == value.Length)
                        {
                            break;
                        }
                    }
                    if (position == value.Length)
                        position = value.Length - 99;
                    namePrefix = value.Substring(0, position);
                    base.FileName = value.Substring(position, value.Length - position);
                }
                else
                {
                    base.FileName = value;
                }
            }
        }

        public override bool UpdateHeaderFromBytes()
        {
            byte[] bytes = GetBytes();
            UserName = Encoding.ASCII.GetString(bytes, 265, 32);
            GroupName = Encoding.ASCII.GetString(bytes, 297, 32);
            namePrefix = Encoding.ASCII.GetString(bytes, 345, 155);
            return base.UpdateHeaderFromBytes();
        }

        internal static bool IsPathSeparator(char ch)
        {
            return (ch == '\\' || ch == '/' || ch == '|'); // All the path separators I ever met.
        }

        public override byte[] GetHeaderValue()
        {
            byte[] header = base.GetHeaderValue();

            Encoding.ASCII.GetBytes(magic).CopyTo(header, 257); // Mark header as ustar
            Encoding.ASCII.GetBytes(version).CopyTo(header, 263);
            Encoding.ASCII.GetBytes(UserName).CopyTo(header, 265);
            Encoding.ASCII.GetBytes(GroupName).CopyTo(header, 297);
            Encoding.ASCII.GetBytes(namePrefix).CopyTo(header, 345);

            if (SizeInBytes >= 0x1FFFFFFFF)
            {
                byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(SizeInBytes));
                SetMarker(AlignTo12(bytes)).CopyTo(header, 124);
            }

            RecalculateChecksum(header);
            Encoding.ASCII.GetBytes(HeaderChecksumString).CopyTo(header, 148);
            return header;
        }

        private static byte[] SetMarker(byte[] bytes)
        {
            bytes[0] |= 0x80;
            return bytes;
        }

        private static byte[] AlignTo12(byte[] bytes)
        {
            var retVal = new byte[12];
            bytes.CopyTo(retVal, 12 - bytes.Length);
            return retVal;
        }
    }
}