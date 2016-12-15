﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ude;

namespace EncodingConverter
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Recursively transcodes text files matching file mask parameter from its original encoding to target encoding parameter");
                Console.WriteLine("Usage: EncodingConverter <file mask> <target encoding>");
                return;
            }

            var fileMask = args[0];

            Encoding targetEncoding;
            try
            {
                targetEncoding = Encoding.GetEncoding(args[1]);
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"[!] Encoding '{args[1]}' not recognized");
                return;
            }
            
            CheckEncoding(".", fileMask, targetEncoding);

            Console.ReadKey();
        }

        private static void CheckEncoding(string rootDirectory, string fileMask, Encoding targetEncoding)
        {
            foreach (string file in GetFilesSimple(rootDirectory, fileMask, SearchOption.AllDirectories))
            {
                var fi = new FileInfo(file);

                var encoding = DetectWithUDE(fi, targetEncoding);
                if (encoding == null)
                    continue;

                // transcode
            }
        }

        private static Encoding DetectWithUDE(FileInfo file, Encoding targetEncoding)
        {
            var detector = new CharsetDetector();
            using (var stream = file.OpenRead())
                detector.Feed(stream);
            detector.DataEnd();

            if (targetEncoding.WebName.Equals(detector.Charset, StringComparison.InvariantCultureIgnoreCase))
                return null;

            if (string.IsNullOrEmpty(detector.Charset))
            {
                Console.WriteLine($"[-] Charset undetected in file '{file.Name}'- skipped!");
                return null;
            }

            if (detector.Confidence < 0.5)
            {
                Console.WriteLine($"[-] Charset detected as '{detector.Charset}' with confidence {Math.Round(detector.Confidence * 100)}% in file '{file.Name}' - skipped!");
                return null;
            }

            Console.WriteLine($"[+] Charset detected as '{detector.Charset}' with confidence {Math.Round(detector.Confidence * 100)}% in file '{file.Name}'");

            Encoding detectedEncoding;
            try
            {
                detectedEncoding = Encoding.GetEncoding(detector.Charset);
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"[!] Encoding '{detector.Charset}' not recognized by .NET Framework");
                return null;
            }

            return detectedEncoding;
        }

        private static IEnumerable<string> GetFilesSimple(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }
    }
}