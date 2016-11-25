/*
FCCLI v0.1

FCCLI or Folder Compare CLI is a commandline tool made to
compare files between two folders, having same name.

Comparision is done by computing MD5, SHA1, SHA256 and SHA512
and any files that fail the comparision are logged on the
console as output with proper colors.

author  : Abhinav Dabral
last mod: 25/11/2016
email   : abhinavdabral@live.com
git     : https://github.com/abhinavdabral/fccli.git
website : https://github.com/abhinavdabral/fccli
license : MIT

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace fccli
{
    enum ChecksumType
    {
        MD5, SHA1, SHA256, SHA512
    }

    struct ChecksumResult
    {
        public string MD5;
        public string SHA1;
        public string SHA256;
        public string SHA512;
        public bool success;

        public ChecksumResult(string SHA1, string SHA256, string SHA512, string MD5)
        {
            this.SHA1 = SHA1;
            this.SHA256 = SHA256;
            this.SHA512 = SHA512;
            this.MD5 = MD5;
            success = false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var CR = (ChecksumResult)obj;

            return MD5.Equals(CR.MD5) &&
                SHA1.Equals(CR.SHA1) &&
                SHA256.Equals(CR.SHA256) &&
                SHA512.Equals(CR.SHA512);
        }

        public static bool operator ==(ChecksumResult c1, ChecksumResult c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(ChecksumResult c1, ChecksumResult c2)
        {
            return !c1.Equals(c2);
        }

        public override string ToString()
        {
            return $"MD5: {MD5}\nSHA1: {SHA1}\nSHA256: {SHA256}\nSHA512: {SHA512}";
        }
    }

    class FCCLI
    {
        static ConsoleColor DefaultFGConsoleColor = Console.ForegroundColor;
        static ConsoleColor DefaultBGConsoleColor =  Console.BackgroundColor;

        static void Main(string[] args)
        {
            string SourceDir = null;
            string DestDir = null;
            try {
                string CurrentDirectory = System.IO.Directory.GetCurrentDirectory();
                if (args.Length <= 0 || args[0].Trim().Length <= 0)
                {
                    Console.WriteLine("No source or destination path specified. Please make sure that if your path contains spaces, enclose the entire path within double-quotes like \"<path>\"\n\nPlease use the FCCLI tool as follows:\n\tfccli <source directory> <destination directory>\n\nOr if you want to compare the current directory with another one, use :\n\tfccli <destination directory>");
                    return;
                }

                SourceDir   = (args.Length >= 2 && Directory.Exists(args[1].Trim())) ? args[1].Trim() : CurrentDirectory;
                DestDir     = (args.Length >= 1 && Directory.Exists(args[0].Trim())) ? args[0].Trim() : null;

                if (DestDir == null)
                {
                    Console.WriteLine("Destination path specified cannot be found. Please make sure that if your path contains spaces, enclose the entire path within double-quotes like \"<path>\"\n\nPlease use the FCCLI tool as follows:\n\tfccli <source directory> <destination directory>\n\nOr if you want to compare the current directory with another one, use :\n\tfccli <destination directory>");
                    return;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.InnerException.Message);
                return;
            }

            DirectoryInfo DISource = new DirectoryInfo(SourceDir);
            DirectoryInfo DIDestination = new DirectoryInfo(DestDir);

            List<string> SourceFileNames = new List<string>();
            List<string> DestinationFileNames = new List<string>();
            List<string> CommonFiles = new List<string>();

            int mismatchCount = 0;
            int doneCount = 0;

            foreach (FileInfo fi in DISource.GetFiles())
                SourceFileNames.Add(fi.Name);

            foreach (FileInfo fi in DIDestination.GetFiles())
                DestinationFileNames.Add(fi.Name);

            foreach(string filename in SourceFileNames)
                if (DestinationFileNames.IndexOf(filename) >= 0) CommonFiles.Add(filename);

            if(CommonFiles.Count<=0)
            {
                Console.WriteLine("No files with common names were found between the two directories.");
                return;
            }

            Stopwatch benchmark = new Stopwatch();
            benchmark.Start();

            string format = string.Empty;   // Current status console output.

            foreach (string filename in CommonFiles)
            {
                FileInfo FISource = new FileInfo(Path.Combine(SourceDir + "\\", filename));
                FileInfo FIDest = new FileInfo(Path.Combine(DestDir + "\\", filename));

                doneCount++;

                if (format.Length > 0)
                {
                    // Cleaning the last status before replacing it with a new one.
                    Console.Write(new string(' ', format.Length));
                    if (format.Length <= Console.WindowWidth) Console.Write("\r");
                    for (int i = 0; i < format.Length / Console.WindowWidth; i++)
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                }
                
                // Setting status string to be displayed.
                format = $"Comparing {doneCount}/{CommonFiles.Count} : {filename}";
                Console.Write(format);

                // Resetting cursor location.
                if (format.Length <= Console.WindowWidth) Console.Write("\r");
                else for (int i = 0; i < format.Length / Console.WindowWidth; i++)
                        Console.SetCursorPosition(0, Console.CursorTop - 1);

                ChecksumResult CRSource = new ChecksumResult();
                ChecksumResult CRDest = new ChecksumResult();

                Thread SourceThread = new Thread(new ThreadStart(() => { CRSource = GetChecksum(FISource); }));
                Thread DestThread   = new Thread(new ThreadStart(() => { CRDest = GetChecksum(FIDest); }));

                SourceThread.Start();
                DestThread.Start();

                while (SourceThread.IsAlive || DestThread.IsAlive) Thread.Sleep(50);

                //For debugging purposes; You can choose to display the Checksum details of a file.
                //Console.WriteLine(CRSource);
                //Console.WriteLine(CRDest);

                if (CRSource!=CRDest && CRSource.success == true && CRDest.success==true)
                {
                    mismatchCount++;
                    Console.Write(filename);

                    if (FISource.LastWriteTimeUtc > FIDest.LastWriteTimeUtc)        ColorConsoleWrite(" (S) ", ConsoleColor.Cyan); // Source is newer
                    else if (FISource.LastWriteTimeUtc < FIDest.LastWriteTimeUtc)   ColorConsoleWrite(" (D) ", ConsoleColor.Yellow); // Destination file is newer
                    else                                                            ColorConsoleWrite(" (C) ", ConsoleColor.Red); // Both files are same but one of them could be corrupted

                    if (!CRSource.MD5.Equals(CRDest.MD5))       ColorConsoleWrite("[MD5] ", ConsoleColor.DarkCyan);     // MD5 checksum comparision failed.
                    if (!CRSource.SHA1.Equals(CRDest.SHA1))     ColorConsoleWrite("[SHA1] ", ConsoleColor.DarkYellow);  // SHA1 checksum comparision failed.
                    if (!CRSource.SHA256.Equals(CRDest.SHA256)) ColorConsoleWrite("[SHA256] ", ConsoleColor.Green);     // SHA256 checksum comparision failed.
                    if (!CRSource.SHA512.Equals(CRDest.SHA512)) ColorConsoleWrite("[SHA512]", ConsoleColor.Magenta);    // SHA512 checksum comparision failed.

                    Console.WriteLine();
                }
            }

            if (format.Length > 0)
            {
                // Cleaning the last status before replacing it with a new one.
                Console.Write(new string(' ', format.Length));
                if (format.Length <= Console.WindowWidth) Console.Write("\r");
                for (int i = 0; i < format.Length / Console.WindowWidth; i++)
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
            }

            benchmark.Stop(); // Stopping Stopwatch

            Console.WriteLine($"\n\n{CommonFiles.Count} files with common names compared successfully in {benchmark.ElapsedMilliseconds/1000} seconds!\n");

            benchmark.Reset(); // Resetting Stopwatch

            if (mismatchCount <= 0) // If no checksum comparisions failed.
                Console.WriteLine("All common files are identical at source and destination locations.");
            else
            {
                // Printing the Legend after the batch process is completed.

                Console.WriteLine("Legend :");
                ColorConsoleWrite("\t(S)     ", ConsoleColor.Cyan);         Console.WriteLine(" : Source file appears to be newer and probably contains the latest changes.");
                ColorConsoleWrite("\t(D)     ", ConsoleColor.Yellow);       Console.WriteLine(" : Destination file appears to be newer and probably contains the latest changes.");
                ColorConsoleWrite("\t(C)     ", ConsoleColor.Red);          Console.WriteLine(" : One of the files could be corrupted.");
                ColorConsoleWrite("\t[MD5]   ", ConsoleColor.DarkCyan);     Console.WriteLine(" : MD5 checksum comparision failed.");
                ColorConsoleWrite("\t[SHA1]  ", ConsoleColor.DarkYellow);   Console.WriteLine(" : SHA1 checksum comparision failed.");
                ColorConsoleWrite("\t[SHA256]", ConsoleColor.Green);        Console.WriteLine(" : SHA256 checksum comparision failed.");
                ColorConsoleWrite("\t[SHA512]", ConsoleColor.Magenta);      Console.WriteLine(" : SHA512 checksum comparision failed.");
            }            
        }

        static ChecksumResult GetChecksum(FileInfo fi)
        {

            ChecksumResult CR = new ChecksumResult();
            try
            {
                using (Stream sourceFileStream = File.Open(fi.FullName, FileMode.Open))
                using (var sha1Stream = new SHA1Managed())
                    CR.SHA1 = BitConverter.ToString(sha1Stream.ComputeHash(new BufferedStream(sourceFileStream))).Replace("-", "");

                using (Stream sourceFileStream = File.Open(fi.FullName, FileMode.Open))
                using (var sha256Stream = new SHA256Managed())
                    CR.SHA256 = BitConverter.ToString(sha256Stream.ComputeHash(new BufferedStream(sourceFileStream))).Replace("-", "");

                using (Stream sourceFileStream = File.Open(fi.FullName, FileMode.Open))
                using (var sha512Stream = new SHA512Managed())
                    CR.SHA512 = BitConverter.ToString(sha512Stream.ComputeHash(new BufferedStream(sourceFileStream))).Replace("-", "");

                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(fi.FullName))
                    CR.MD5 = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");

                CR.success = true;
            }
            catch(Exception e)
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nError:");
                Console.ResetColor();
                Console.Write(e.InnerException.Message);
                Console.WriteLine();
            }

            //Console.WriteLine($"Check complete for {fi.FullName}");
            return CR;
        }

        static void ColorConsoleWrite(string str, ConsoleColor fg)
        {
            Console.ResetColor();
            Console.ForegroundColor = fg;
            Console.Write(str);
            Console.ResetColor();
        }
    }
}
