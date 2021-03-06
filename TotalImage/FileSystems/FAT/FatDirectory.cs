using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TotalImage.FileSystems.FAT
{
    /*
     * FAT12/FAT16/FAT32 directory class. Implements directory entry enumeration.
     */
    public class FatDirectory : Directory
    {
        DirectoryEntry entry;

        public FatDirectory(Fat12 fat, DirectoryEntry entry, Directory parent) : base(fat, parent)
        {
            this.entry = entry;
        }

        public override string Name
        {
            get => Helper.TrimFileName(entry.name);
            set => throw new NotImplementedException();
        }

        public override FileAttributes Attributes
        {
            get => (FileAttributes)entry.attr;
            set => throw new NotImplementedException();
        }

        public override DateTime? LastAccessTime
        {
            get => Helper.FatToDateTime(entry.lstAccDate);
            set => throw new NotImplementedException();
        }

        public override DateTime? LastWriteTime
        {
            get => Helper.FatToDateTime(entry.wrtDate, entry.wrtTime);
            set => throw new NotImplementedException();
        }

        public override DateTime? CreationTime
        {
            get => Helper.FatToDateTime(entry.crtDate, entry.crtTime, entry.crtTimeTenth);
            set => throw new NotImplementedException();
        }

        public override long Length
        {
            get => entry.fileSize;
            set => throw new NotImplementedException();
        }

        public override Directory CreateSubdirectory(string path)
        {
            throw new NotImplementedException();
        }

        public override void Delete()
        {
            /* When deleting a directory, the first character of the name needs to be changed to 0xE5.
            * The directory's directory entry can then be reused, and its clusters are marked as free until they're
            * overwritten. The same must then be done for all files and subdirectories inside.
            * This code is untested until this class is hooked up to the UI... */

            byte[] bytes = Encoding.ASCII.GetBytes(entry.name);
            bytes[0] = 0xE5;
            entry.name = Encoding.ASCII.GetString(bytes);

            //And then mark all clusters in the chain as free, and do the same for all files and subdirectories inside.
        }

        public override IEnumerable<FileSystemObject> EnumerateFileSystemObjects()
        {
            var fat = FileSystem as Fat12;
            var dataAreaOffset = (uint)(fat.BiosParameterBlock.BytesPerLogicalSector + (fat.BiosParameterBlock.BytesPerLogicalSector *
                fat.BiosParameterBlock.LogicalSectorsPerFAT * fat.BiosParameterBlock.NumberOfFATs) + (fat.BiosParameterBlock.RootDirectoryEntries << 5));
            var fat1Offset = fat.BiosParameterBlock.BytesPerLogicalSector;
            var stream = fat.GetStream();

            using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                uint cluster = ((uint)entry.fstClusHI << 16) | entry.fstClusLO;

                do
                {
                    uint clusterOffset = (cluster - 2) * fat.BiosParameterBlock.LogicalSectorsPerCluster * fat.BiosParameterBlock.BytesPerLogicalSector;

                    //No. of entries that fit in one cluster = BPS * SPC / 32 bytes per entry
                    for (int i = 0; i < (fat.BiosParameterBlock.LogicalSectorsPerCluster * fat.BiosParameterBlock.BytesPerLogicalSector / 32); i++)
                    {
                        stream.Seek(dataAreaOffset + clusterOffset + (i * 32), SeekOrigin.Begin);
                        byte firstByte = reader.ReadByte();

                        /* 0x00/0xF6 = no more entries after this one, stop
                         * 0xE5/0x05 = deleted entry, skip for now */
                        if (firstByte == 0x00 || firstByte == 0xF6) break;
                        else if (firstByte == 0xE5 || firstByte == 0x05) continue;

                        stream.Seek(-0x01, SeekOrigin.Current);
                        var entry = DirectoryEntry.Parse(reader.ReadBytes(32));

                        //Skip LFN and volume label entries for now
                        if (Convert.ToBoolean(entry.attr & 0x08))
                        {
                            continue;
                        }

                        //Folder entry
                        if (Convert.ToBoolean(entry.attr & 0x10))
                        {
                            yield return new FatDirectory(fat, entry, this);
                        }
                        else
                        {
                            yield return new FatFile(fat, entry, this);
                        }

                    }
                    if (cluster % 2 == 0)
                    {
                        stream.Seek(fat1Offset + (ushort)(cluster * 1.5), SeekOrigin.Begin);
                        ushort lower8 = reader.ReadByte();
                        ushort upper4 = (ushort)((reader.ReadByte() & 0x0F) << 8);
                        cluster = (ushort)(upper4 + lower8);
                    }
                    else
                    {
                        stream.Seek(fat1Offset + (ushort)Math.Floor(cluster * 1.5), SeekOrigin.Begin);
                        ushort lower4 = (ushort)((reader.ReadByte() & 0xF0) >> 4);
                        ushort upper8 = (ushort)(reader.ReadByte() << 4);
                        cluster = (ushort)(upper8 + lower4);
                    }
                }
                while (cluster <= 0x0FEF);
            }
        }

        public override void MoveTo(string path)
        {
            throw new NotImplementedException();
        }

        //Checks if an entry with the specified name already exists in this directory
        public bool EntryExists(string fullname)
        {
            if (string.IsNullOrEmpty(fullname))
                throw new ArgumentNullException(nameof(fullname), "fullname cannot be null!");

            string name = fullname.Substring(0, fullname.IndexOf('.')).PadRight(8, ' ');
            string ext = fullname.Substring(fullname.IndexOf('.'), fullname.Length - 1).PadRight(3, ' ');

            return false; //Bogus, needs to actually check all the entries which aren't in this class yet...
        }
    }
}