using System;
using System.IO;

namespace tar_cs
{
    public class TarWriter : LegacyTarWriter
    {
        public TarWriter(Stream writeStream) : base(writeStream)
        {
        }

        private void WriteHeader(string name, DateTime lastModificationTime, long count, string userName, string groupName, int mode, EntryType entryType)
        {
            var tarHeader = new UsTarHeader()
            {
                FileName = name,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userName.GetHashCode(),
                UserName = userName,
                GroupId = groupName.GetHashCode(),
                GroupName = groupName,
                Mode = mode,
                EntryType = entryType,
            };
            OutStream.Write(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }

        private void WriteHardLink(string name, DateTime lastModificationTime, string linkName, string userName, string groupName, int mode)
        {
            var tarHeader = new UsTarHeader()
            {
                FileName = name,
                LastModification = lastModificationTime,
                SizeInBytes = 0,
                UserId = userName.GetHashCode(),
                UserName = userName,
                GroupId = groupName.GetHashCode(),
                GroupName = groupName,
                Mode = mode,
                EntryType = tar_cs.EntryType.HardLink,
                LinkName = linkName,
            };
            OutStream.Write(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }

        public void WriteHardLink(string linkName, string fileName, string username, string groupname, int mode,
            DateTime lastModificationTime)
        {
            WriteHardLink(fileName,lastModificationTime, linkName ,username, groupname, mode);
        }

        public void WriteDirectoryEntry(string path, string username, string groupname, int mode, DateTime lastModificationTime)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (path[path.Length - 1] != '/')
            {
                path += '/';
            }

            var tarHeader = new UsTarHeader()
            {
                FileName = path,
                LastModification = lastModificationTime,
                SizeInBytes = 0,
                UserId = username.GetHashCode(),
                UserName = username,
                GroupId = groupname.GetHashCode(),
                GroupName = groupname,
                Mode = mode,
                EntryType = tar_cs.EntryType.Directory,
            };
            OutStream.Write(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }

        public void WriteFile(Stream stream, long dataSizeInBytes, string name, string username, string groupname, int mode, DateTime lastModificationTime)
        {
            WriteHeader(name, lastModificationTime, dataSizeInBytes, username, groupname, mode, EntryType.File);
            WriteContent(dataSizeInBytes, stream);
            AlignTo512(dataSizeInBytes, false);
        }
    }
}