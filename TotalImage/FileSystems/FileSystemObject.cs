using System;
using System.Collections.Generic;

namespace TotalImage.FileSystems
{
    public abstract class FileSystemObject
    {
        public abstract string Name { get; set; }
        public abstract string FullName { get; set; }

        public abstract FileAttributes Attributes { get; set; }

        public abstract DateTime LastAccessTime { get; set; }
        public abstract DateTime CreationTime { get; set; }

        public abstract long Length { get; set; }

        public abstract void Delete();
        public abstract void MoveTo(string path);

        public bool IsReadOnly
        {
            get => (Attributes & FileAttributes.ReadOnly) > 0;
            set => Attributes = Attributes | (value ? FileAttributes.ReadOnly : 0);
        }
    }
}