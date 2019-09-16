﻿using System;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace OMODExtractor
{
    public class OMODExtract
    {
        static void Main(String[] args)
        {
            if(args.Length == 2)
            {
                string source = args[0];
                string dest = args[1];
                Console.Write($"Extracting {source} using 7zip\n");
                var info = new ProcessStartInfo
                {
                    FileName = "7z.exe",
                    Arguments = $"x -bsp1 -y -o\"{dest}\" \"{source}\"",
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = new Process
                {
                    StartInfo = info
                };
                p.Start();
                try
                {
                    p.PriorityClass = ProcessPriorityClass.BelowNormal;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Console.Write($"Archive extracted\n");

                string outputDir = Path.Combine(Directory.GetCurrentDirectory(), dest);
                outputDir += "\\";
                string[] allOMODFiles = Directory.GetFiles(outputDir, "*.omod",SearchOption.TopDirectoryOnly);

                if(allOMODFiles.Length == 1)
                {
                    OMOD omod = new OMOD(allOMODFiles[0], outputDir);
                }

                //on exit:
                cleanup(outputDir + "temp\\");
            }
        }

        internal class OMOD
        {
            private string path;
            private string basedir;
            private string tempdir;
            private ZipFile ModFile;

            internal OMOD(string path_, string basedir_)
            {
                path = path_;
                ModFile = new ZipFile(path);
                basedir = basedir_;
                tempdir = basedir + "temp\\";
                Directory.CreateDirectory(tempdir);

                SaveConfig();
                SaveFile("readme");
                SaveFile("script");

                ExtractData();
            }

            internal void ExtractData()
            {
                string DataPath = GetDataFiles();
                Console.WriteLine(DataPath);
            }

            internal string GetDataFiles()
            {
                return ParseCompressedStream("data.crc", "data");
            }

            private string ParseCompressedStream(string fileList, string compressedStream)
            {
                string path;
                Stream FileList = ExtractWholeFile(fileList);
                if (FileList == null) return null;
                Stream CompressedStream = ExtractWholeFile(compressedStream);
                path = CompressionHandler.DecompressFiles(FileList, CompressedStream, CompressionHandler.CompressionType.SevenZip);
                FileList.Close();
                CompressedStream.Close();
                return path;
            }

            internal void SaveFile(string entry)
            {
                string result = null;
                string s = "";
                Stream st = ExtractWholeFile(entry, ref s);
                BinaryReader br = null;

                try
                {
                    br = new BinaryReader(st);
                    result = br.ReadString();
                }
                finally
                {
                    if (br != null) br.Close();
                    SaveToFile(result,basedir+entry+".txt");
                }
            }

            internal void SaveConfig()
            {
                string result = null;
                string s = "";
                Stream st = ExtractWholeFile("config", ref s);
                BinaryReader br = null;

                try
                {
                    br = new BinaryReader(st);
                    byte version = br.ReadByte();
                    string modName = br.ReadString();
                    int majorVersion = br.ReadInt32();
                    int minorVersion = br.ReadInt32();
                    string author = br.ReadString();
                    string email = br.ReadString();
                    string website = br.ReadString();
                    string description = br.ReadString();
                    DateTime creationTime;
                    CompressionHandler.CompressionType CompType;
                    int buildVersion;
                    if (version >= 2)
                    {
                        creationTime = DateTime.FromBinary(br.ReadInt64());
                    }
                    else
                    {
                        string sCreationTime = br.ReadString();
                        if (!DateTime.TryParseExact(sCreationTime, "dd/MM/yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out creationTime))
                        {
                            creationTime = new DateTime(2006, 1, 1);
                        }
                    }
                    if (description == "") description = "No description";
                    CompType = (CompressionHandler.CompressionType)br.ReadByte();
                    if (version >= 1)
                    {
                        buildVersion = br.ReadInt32();
                    }
                    else buildVersion = -1;

                    result = $"version: {version}\n" +
                        $"Modname: {modName}\n" +
                        $"Majorversion: {majorVersion}\n" +
                        $"Minorversion: {minorVersion}\n" +
                        $"Author: {author}\n" +
                        $"Email: {email}\n" +
                        $"Website: {website}\n" +
                        $"Description: {description}\n" +
                        $"Creationtime: {creationTime}\n" +
                        $"buildversion: {buildVersion}";
                }
                finally
                {
                    if (br != null) br.Close();
                    SaveToFile(result, basedir + "config.txt");
                }
            }

            private Stream ExtractWholeFile(string s)
            {
                string s2 = null;
                return ExtractWholeFile(s, ref s2);
            }

            private Stream ExtractWholeFile(string s, ref string path)
            {
                ZipEntry ze = ModFile.GetEntry(s);
                if (ze == null) return null;
                return ExtractWholeFile(ze, ref path);
            }

            private Stream ExtractWholeFile(ZipEntry ze, ref string path)
            {
                Stream file = ModFile.GetInputStream(ze);
                Stream TempStream;
                if (path != null || ze.Size > 67108864)
                {
                    TempStream = CreateTempFile(out path);
                }
                else
                {
                    TempStream = new MemoryStream((int)ze.Size);
                }
                byte[] buffer = new byte[4096];
                int i;
                while ((i = file.Read(buffer, 0, 4096)) > 0)
                {
                    TempStream.Write(buffer, 0, i);
                }
                TempStream.Position = 0;
                return TempStream;
            }

            internal FileStream CreateTempFile()
            {
                string s;
                return CreateTempFile(out s);
            }
            internal FileStream CreateTempFile(out string path)
            {
                int i = 0;
                for (i = 0; i < 32000; i++)
                {
                    if (!File.Exists(tempdir + "tmp" + i.ToString()))
                    {
                        path = tempdir + "tmp" + i.ToString();
                        return File.Create(path);
                    }
                }
                throw new Exception("Could not create temp file because directory is full");
            }
        }

        public static string CreateTempDirectory()
        {
            string tempdir = Directory.GetCurrentDirectory()+"\\temp\\";
            for (int i = 0; i < 32000; i++)
            {
                if (!Directory.Exists(tempdir + i.ToString()))
                {
                    Directory.CreateDirectory(tempdir + i.ToString() + "\\");
                    return tempdir + i.ToString() + "\\";
                }
            }
            throw new Exception("Could not create temp folder because directory is full");
        }

        public static void cleanup(string tempdir)
        {
            DirectoryInfo di = new DirectoryInfo(tempdir);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
            Directory.Delete(tempdir);
        }

        //for testing
        public static void SaveToFile(string contents, string dest)
        {
            if (File.Exists(dest)) File.Delete(dest);
            File.WriteAllText(dest, contents);
        }
    }
}
