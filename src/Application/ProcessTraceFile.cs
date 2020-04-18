﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NormaliseTrace.Application
{
    public class ProcessTraceFile
    {
        private readonly IReaderStrategy _reader;
        private readonly int             _numColumns;
        private readonly List<double>    _delta;

        public ProcessTraceFile(IReaderStrategy reader, int numColumns, List<double> delta)
        {
            _reader     = reader;
            _numColumns = numColumns;
            _delta      = delta;
        }

        public void Process(string outputFolder, IEnumerable<string> directoryAndSearchPatterns)
        {
            if(_delta.Count < _numColumns)
            {
                Console.WriteLine($"Invalid. Delta columns = {_delta.Count} and options.columns = {_numColumns}.");
                Console.WriteLine("Too many columns specified. Cannot continue.");
                return;
            }

            Console.WriteLine("Reading trace files:");
            if (directoryAndSearchPatterns == null)
                throw new ArgumentNullException(nameof(directoryAndSearchPatterns));

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            foreach (var directoryAndSearchPattern in directoryAndSearchPatterns)
            {
                if (File.Exists(directoryAndSearchPattern))
                {
                    // This is not a directory search pattern, but a single file to read
                    var file  = directoryAndSearchPattern;
                    var input = _reader.ReadInput(file);
                    if (input.success)
                        WriteTraceOutput(file, input.data);
                }
                else
                {
                    var path          = Path.GetDirectoryName(directoryAndSearchPattern);
                    var searchPattern = Path.GetFileName(directoryAndSearchPattern);
                    var files         = Directory.EnumerateFiles(path, searchPattern);
                    ReadFiles(files);
                }
            }
        }

        public void ReadFiles(IEnumerable<string> files)
        {
            if (files == null)
                throw new ArgumentNullException(nameof(files));

            foreach (var file in files)
            {
                var input = _reader.ReadInput(file);
                if (input.success)
                    WriteTraceOutput(file, input.data);
            }
        }

        private void WriteTraceOutput(string inputFile, List<List<int>> inputData)
        {
            var folder     = Path.GetDirectoryName(inputFile);
            var filename   = Path.GetFileName(inputFile);
            var outputFile = Path.Combine(folder, filename);

            Console.WriteLine($"Writing: {outputFile}");

            using var stream = new StreamWriter(outputFile, false);
            foreach (var row in inputData)
            {
                if (row.Count < _numColumns)
                {
                    Console.WriteLine($"Invalid. File columns = {row.Count} and options.columns = {_numColumns}.");
                    Console.WriteLine("Too many columns specified, or too few in file. Skipping file.");
                    return;
                }

                var adjusted = _delta.Take(_numColumns)
                    .Select((delta, index) => (int) Math.Round(delta * row[index]))
                    .ToList();

                stream.WriteLine(string.Join(',', adjusted));
            }
            stream.Close();
        }
    }
}
