using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using tar_cs;

namespace tar_cs
{
    internal class TarHeader : ITarHeader
    {
        private readonly byte[] buffer = new byte[512];
        private long headerChecksum;

        public TarHeader()
        {
            // Default values
            Mode = 511; // 0777 dec
            UserId = 61; // 101 dec
            GroupId = 61; // 101 dec
        }

        private string fileName;
        private string linkName;
        protected readonly DateTime TheEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
        public EntryType EntryType { get; set; }
        private static byte[] spaces = Encoding.ASCII.GetBytes("        ");

        public virtual string FileName
        {
            get
            {
                return fileName.Replace("\0",string.Empty);
            } 
            set
            {
                if(value.Length > 100)
                {
                    throw new TarException("A file name can not be more than 100 chars long");
                }
                fileName = value;
            }
        }
        public int Mode { get; set; }

        public string ModeString
        {
            get { return Convert.ToString(Mode, 8).PadLeft(7, '0'); }
        }

        public int UserId { get; set; }
        public virtual string UserName
        {
            get { return UserId.ToString(); }
            set { UserId = Int32.Parse(value); }
        }

        public string UserIdString
        {
            get { return Convert.ToString(UserId, 8).PadLeft(7, '0'); }
        }

        public int GroupId { get; set; }
        public virtual string GroupName
        {
            get { return GroupId.ToString(); }
            set { GroupId = Int32.Parse(value); }
        }

        public string GroupIdString
        {
            get { return Convert.ToString(GroupId, 8).PadLeft(7, '0'); }
        }

        public long SizeInBytes { get; set; }

        public string SizeString
        {
            get { return Convert.ToString(SizeInBytes, 8).PadLeft(11, '0'); }
        }

        public DateTime LastModification { get; set; }

        public string LastModificationString
        {
            get
            {
                return Convert.ToString((long)(LastModification - TheEpoch).TotalSeconds, 8).PadLeft(11, '0');
            }
        }

        public string HeaderChecksumString
        {
            get { return Convert.ToString(headerChecksum, 8).PadLeft(6, '0'); }
        }

        public string LinkName
        {
            get
            {
                return (linkName ?? string.Empty).Replace("\0",string.Empty);
            } 
            set
            {
                if(value.Length > 100)
                {
                    throw new TarException("A link name can not be more than 100 chars long");
                }

                linkName = value;
            }
        }

        public virtual int HeaderSize
        {
            get { return 512; }
        }

        public byte[] GetBytes()
        {
            return buffer;
        }

        public virtual bool UpdateHeaderFromBytes()
        {
            fileName = Encoding.ASCII.GetString(buffer, 0, 100);
            // thanks to Shasha Alperocivh. Trimming nulls.
            Mode = Convert.ToInt32(Encoding.ASCII.GetString(buffer, 100, 7).Trim(), 8);
            UserId = Convert.ToInt32(Encoding.ASCII.GetString(buffer, 108, 7).Trim(), 8);
            GroupId = Convert.ToInt32(Encoding.ASCII.GetString(buffer, 116, 7).Trim(), 8);

            EntryType = (EntryType)buffer[156];

            LinkName = Encoding.ASCII.GetString(buffer, 157, 100);

            if((buffer[124] & 0x80) == 0x80) // if size in binary
            {
                long sizeBigEndian = BitConverter.ToInt64(buffer,0x80);
                SizeInBytes = IPAddress.NetworkToHostOrder(sizeBigEndian);
            }
            else
            {
                SizeInBytes = Convert.ToInt64(Encoding.ASCII.GetString(buffer, 124, 11), 8);
            }
            long unixTimeStamp = Convert.ToInt64(Encoding.ASCII.GetString(buffer,136,11),8);
            LastModification = TheEpoch.AddSeconds(unixTimeStamp);

            var storedChecksum = Convert.ToInt32(Encoding.ASCII.GetString(buffer,148,6));
            RecalculateChecksum(buffer);
            if (storedChecksum == headerChecksum)
            {
                return true;
            }

            RecalculateAltChecksum(buffer);
            return storedChecksum == headerChecksum;
        }

        private void RecalculateAltChecksum(byte[] buf)
        {
            spaces.CopyTo(buf, 148);
            headerChecksum = 0;
            foreach(byte b in buf)
            {
                if((b & 0x80) == 0x80)
                {
                    headerChecksum -= b ^ 0x80;
                }
                else
                {
                    headerChecksum += b;
                }
            }
        }

        public virtual byte[] GetHeaderValue()
        {
            // Clean old values
            Array.Clear(buffer,0, buffer.Length);

            if (string.IsNullOrEmpty(fileName)) throw new TarException("FileName can not be empty.");
            if (fileName.Length >= 100)
            {
                var actualLength = fileName.Length;
                var thisAsUsTar = this as UsTarHeader;
                var namePrefix = string.Empty;
                if (thisAsUsTar != null)
                {
                    namePrefix = thisAsUsTar.namePrefix;
                }

                throw new TarException(@"File name is too long.
Full file name: " + FileName + @"
100-byte file name: " + fileName + @"
Name prefix: " + namePrefix + @"
100-byte file name length: " + fileName.Length + @"
Full file name length: " + FileName.Length);
            }
                

            // Fill header
            Encoding.ASCII.GetBytes(fileName.PadRight(100, '\0')).CopyTo(buffer, 0);
            Encoding.ASCII.GetBytes(ModeString).CopyTo(buffer, 100);
            Encoding.ASCII.GetBytes(UserIdString).CopyTo(buffer, 108);
            Encoding.ASCII.GetBytes(GroupIdString).CopyTo(buffer, 116);
            Encoding.ASCII.GetBytes(SizeString).CopyTo(buffer, 124);
            Encoding.ASCII.GetBytes(LastModificationString).CopyTo(buffer, 136);

//            buffer[156] = 20;
            buffer[156] = ((byte) EntryType);

            if (LinkName != null)
            {
                Encoding.ASCII.GetBytes(LinkName.PadRight(100, '\0')).CopyTo(buffer, 157);
            }

            RecalculateChecksum(buffer);

            // Write checksum
            Encoding.ASCII.GetBytes(HeaderChecksumString).CopyTo(buffer, 148);

            return buffer;
        }

        protected virtual void RecalculateChecksum(byte[] buf)
        {
            // Set default value for checksum. That is 8 spaces.
            spaces.CopyTo(buf, 148);

            // Calculate checksum
            headerChecksum = 0;
            foreach (byte b in buf)
            {
                headerChecksum += b;
            }
        }
    }
}