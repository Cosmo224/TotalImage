﻿using System;
using System.IO;
using System.Text;
using TotalImage.FileSystems;

namespace TotalImage.ImageFormats
{
    public class RawSector
    {
        private byte[] imageBytes;
        private BiosParameterBlock bpb;
        private FileStream fs;
      
        //Returns byte array of the image
        public byte[] GetImageBytes()
        {
            return imageBytes;
        }

        //Creates a new image from the selected preset - PORTED FROM 86BOX
        public void CreateImage(BiosParameterBlock bpb, string oemID, byte tracks)
        {
            uint imageSize = (ushort)(bpb.BytesPerLogicalSector * bpb.PhysicalSectorsPerTrack * bpb.NumberOfHeads * tracks);
            imageBytes = new byte[imageSize];
            Fat12 fat12 = new Fat12(imageBytes);
            fat12.Format(bpb, oemID, tracks);
            this.bpb = bpb;
        }

        //Creates a new image based on custom parameters
        public void CreateCustomImage()
        {
            /* Do custom parameter stuff here */
        }

        //Loads an image file
        public void LoadImage(string path)
        {
            //For larger images (HDD etc.) we probably won't read the entire file at once, but use the stream instead...
            imageBytes = File.ReadAllBytes(path);
            fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Fat12 fat12 = new Fat12(imageBytes);
            bpb = fat12.Parse();
            fat12.ReadRootDir(bpb);
        }

        //Closes and unlocks the file
        public void CloseImage()
        {
            fs.Flush();
            fs.Close();
        }

        //Lists the contents of the specified directory
        public void ListDirectory(FatDirEntry entry)
        {
            Fat12 fat12 = new Fat12(imageBytes);
            fat12.ListDir(bpb, entry);
        }

        //Lists the contents of the root directory
        public void ListRootDirectory()
        {
            Fat12 fat12 = new Fat12(imageBytes);
            fat12.ListRootDir(bpb);
        }
    }
}