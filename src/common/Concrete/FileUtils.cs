// MIT License
// 
// Copyright (c) 2017 Mark Zuber
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using ZubeNet.Common.Native;

namespace ZubeNet.Common.Concrete
{
    /// <summary>
    ///     A collection of file-based utility functions.
    /// </summary>
    public class FileUtils : IFileUtils
    {
        private readonly IProcessUtils _processUtils;
        private readonly IXmlUtils _xmlUtils;

        public FileUtils(IProcessUtils processUtils, IXmlUtils xmlUtils)
        {
            _processUtils = processUtils;
            _xmlUtils = xmlUtils;
        }

        /// <summary>
        ///     Recursively copies all files and directories from source to target.  Target directories will be created
        ///     if they don't already exist.
        /// </summary>
        /// <param name="source">The full path of the source to copy from.</param>
        /// <param name="target">The full path of the target to copy to.  This wil be created if it doesn't exist.</param>
        /// <param name="recurse">True if should recurse into subdirs.</param>
        public void CopyAll(DirectoryInfo source, DirectoryInfo target, bool recurse)
        {
            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it’s new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            if (recurse)
            {
                // Copy each subdirectory using recursion.
                foreach (DirectoryInfo sourceSubDir in source.GetDirectories())
                {
                    var nextTargetSubDir = target.CreateSubdirectory(sourceSubDir.Name);
                    CopyAll(sourceSubDir, nextTargetSubDir, true);
                }
            }
        }

        /// <summary>
        ///     Recursively moves all files and directories from source to target.  Target directories will be created
        ///     if they don't already exist.
        /// </summary>
        /// <param name="source">The full path of the source to copy from.</param>
        /// <param name="target">The full path of the target to copy to.  This wil be created if it doesn't exist.</param>
        /// <param name="recurse">True if should recurse into subdirs.</param>
        public void MoveAll(DirectoryInfo source, DirectoryInfo target, bool recurse)
        {
            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it’s new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.MoveTo(Path.Combine(target.ToString(), fi.Name));
            }

            if (recurse)
            {
                // Copy each subdirectory using recursion.
                foreach (DirectoryInfo sourceSubDir in source.GetDirectories())
                {
                    var nextTargetSubDir = target.CreateSubdirectory(sourceSubDir.Name);
                    MoveAll(sourceSubDir, nextTargetSubDir, true);
                }
            }
        }

        /// <summary>
        ///     Copies files from source to destination if file does not exist in destination. Will not copy changed files, only
        ///     new files.
        ///     Uses xcopy for copying. Has been tested via console app, unit testing attempted but MSTest will not properly call
        ///     xcopy.
        /// </summary>
        /// <param name="srcFilePath">The full path of the source to copy from.</param>
        /// <param name="filter">Filter to select files copied. Pass String.Empty for *.*</param>
        /// <param name="destination">The full path of the destination to copy to</param>
        public void CopyOnlyNewFiles(string srcFilePath, string filter, string destination)
        {
            CopyOnlyNewFiles(srcFilePath, filter, destination, null);
        }

        /// <summary>
        ///     Copies files from source to destination if file does not exist in destination. Will not copy changed files, only
        ///     new files.
        ///     Uses xcopy for copying. Has been tested via console app, unit testing attempted but MSTest will not properly call
        ///     xcopy.
        /// </summary>
        /// <param name="srcFilePath">The full path of the source to copy from.</param>
        /// <param name="filter">Filter to select files copied. Pass String.Empty for *.*</param>
        /// <param name="destination">The full path of the destination to copy to</param>
        /// <param name="cancelRequestedEvent">An event that signifies the operation should be cancelled</param>
        public void CopyOnlyNewFiles(string srcFilePath, string filter, string destination, ManualResetEvent cancelRequestedEvent)
        {
            if (!Directory.Exists(srcFilePath))
            {
                throw new ArgumentException($"Invalid Source Directory: {srcFilePath}");
            }

            if (!Directory.Exists(destination))
            {
                throw new ArgumentException($"Invalid Destination Directory: {destination}");
            }

            string source = Path.Combine(srcFilePath, filter);
            string excludeListFilePath = Path.GetTempFileName();
            bool isNewFile = false;
            try
            {
                using (var excludeList = new StreamWriter(excludeListFilePath))
                {
                    var sourceFiles = new DirectoryInfo(srcFilePath).EnumerateFiles();
                    foreach (FileInfo file in sourceFiles)
                    {
                        if (cancelRequestedEvent != null && cancelRequestedEvent.WaitOne(0))
                        {
                            throw new OperationCanceledException();
                        }

                        string destPath = Path.Combine(destination, file.Name);
                        if (File.Exists(destPath))
                        {
                            if ((new FileInfo(destPath)).Length == file.Length)
                            {
                                excludeList.WriteLine(file.FullName);
                            }
                            else
                            {
                                isNewFile = true;
                            }
                        }
                        else
                        {
                            isNewFile = true;
                        }
                    }
                }

                if (isNewFile)
                {
                    string xcopyCopyArgs = $"/D /c xcopy /iheckdyq /EXCLUDE:{excludeListFilePath} {source.EncloseQuotes()} {destination.EncloseQuotes()}";
                    _processUtils.RunProcess("cmd.exe", xcopyCopyArgs, null, null, true);
                }
            }
            finally
            {
                File.Delete(excludeListFilePath);
            }
        }

        public void CopyFile(string srcFilePath, string destination)
        {
            File.Copy(srcFilePath, destination);
        }

        public void CopyFile(string srcFilePath, string destination, bool overwrite)
        {
            File.Copy(srcFilePath, destination, overwrite);
        }

        public void MoveFile(string srcFilePath, string destination)
        {
            File.Move(srcFilePath, destination);
        }

        /// <summary>
        ///     Sets a directory and its files attributes to Normal (removes the ReadOnly bit).
        ///     Option to do this recursively.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="recurse"></param>
        public void SetFileAttributesNormal(DirectoryInfo dir, bool recurse)
        {
            dir.Attributes = FileAttributes.Normal;

            foreach (var fi in dir.GetFiles())
            {
                fi.Attributes = FileAttributes.Normal;
            }

            if (recurse)
            {
                foreach (var subDir in dir.GetDirectories())
                {
                    SetFileAttributesNormal(subDir, true);
                }
            }
        }

        public void WriteBytesToFile(string filePath, byte[] contents)
        {
            File.WriteAllBytes(filePath, contents);
        }

        /// <summary>
        ///     Writes contents to a file named by filePath.
        /// </summary>
        /// <param name="filePath">The path to the file to write to.</param>
        /// <param name="contents">The contents of the file.</param>
        public void WriteTextToFile(string filePath, string contents)
        {
            using (var sw = new StreamWriter(filePath))
            {
                sw.WriteLine(contents);
            }
        }

        /// <summary>
        ///     Appends contents to a file named by filePath.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="contents"></param>
        public void AppendTextToFile(string filePath, string contents)
        {
            File.AppendAllText(filePath, contents);
        }

        /// <summary>
        ///     Writes contents to a temp file and returns the path to the temp file.
        /// </summary>
        /// <param name="contents">The string contents to be written to the file.</param>
        /// <returns>The path to the temp file that was created.</returns>
        public string WriteTextToTempFile(string contents)
        {
            string filePath = Path.GetTempFileName();
            using (var sw = new StreamWriter(filePath))
            {
                sw.WriteLine(contents);
            }

            return filePath;
        }

        /// <summary>
        ///     Reads the contents from a file as a string.
        /// </summary>
        /// <param name="filePath">The path to the file to be read.</param>
        /// <returns>The contents of the file as a string.</returns>
        public string GetTextFromFile(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public byte[] GetBytesFromFile(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        /// <summary>
        ///     Reads the contents from a file as an XDocument.
        /// </summary>
        /// <param name="filePath">The path to the file to be read.</param>
        /// <returns>The contents of the file as an XDocument.</returns>
        public XDocument GetXmlFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"GetXmlFromFile: The file {filePath} was not found");
            }

            // Sanity check - In production it was seen that sometimes xml output can be empty
            if (new FileInfo(filePath).Length == 0)
            {
                throw new InvalidDataException($"GetXmlFromFile: The file {filePath} was found to be empty");
            }

            try
            {
                return XDocument.Load(filePath);
            }
            catch (XmlException)
            {
                // be resiliant against badly-formed XML that can come from
                // -a sysconfig -xml
                // one example is 0xFFFF being in the XML, which is a bad XML char
                string xmlData = GetTextFromFile(filePath);
                {
                    xmlData = xmlData.Replace("&#xFFFF;", string.Empty);
                }

                return XDocument.Parse(xmlData);
            }
        }

        /// <summary>
        ///     Reads the contents from a file as an XDocument, and validates the Xpath expression exists
        ///     or throws XmlException
        /// </summary>
        /// <param name="filePath">The path to the file to be read.</param>
        /// <param name="validateXpathExpression">XPath expression to validate in XML.</param>
        /// <returns>The contents of the file as an XDocument.</returns>
        public XDocument GetXmlFromFile(string filePath, string validateXpathExpression)
        {
            var doc = GetXmlFromFile(filePath);

            if (!_xmlUtils.XPathExists(doc, validateXpathExpression))
            {
                throw new XmlException($"valiateXpathExpression not found: {validateXpathExpression}");
            }

            return doc;
        }

        /// <summary>
        ///     Adds an ACL entry on the specified directory for the specified account.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="account"></param>
        /// <param name="rights"></param>
        /// <param name="controlType"></param>
        public void AddDirectorySecurity(string path, string account, FileSystemRights rights, AccessControlType controlType)
        {
            // Create a new DirectoryInfo object.
            var dirInfo = new DirectoryInfo(path);

            // Get a DirectorySecurity object that represents the
            // current security settings.
            var dirSecurity = dirInfo.GetAccessControl();

            // Add the FileSystemAccessRule to the security settings.
            dirSecurity.AddAccessRule(
                new FileSystemAccessRule(account, rights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, controlType));

            // Set the new access settings.
            dirInfo.SetAccessControl(dirSecurity);
        }

        /// <summary>
        ///     Removes an ACL entry on the specified directory for the specified account.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="account"></param>
        /// <param name="rights"></param>
        /// <param name="controlType"></param>
        public void RemoveDirectorySecurity(string path, string account, FileSystemRights rights, AccessControlType controlType)
        {
            // Create a new DirectoryInfo object.
            var dirInfo = new DirectoryInfo(path);

            // Get a DirectorySecurity object that represents the
            // current security settings.
            var dirSecurity = dirInfo.GetAccessControl();

            // Add the FileSystemAccessRule to the security settings.
            dirSecurity.RemoveAccessRule(new FileSystemAccessRule(account, rights, controlType));

            // Set the new access settings.
            dirInfo.SetAccessControl(dirSecurity);
        }

        /// <summary>
        ///     Gets a temporary working directory
        ///     <para />
        ///     Caller must remove directory after they are done
        /// </summary>
        /// <returns>Temporary working directory</returns>
        public string GetTempWorkDir()
        {
            string workDir = GetTempWorkDirName();
            Directory.CreateDirectory(workDir);
            return workDir;
        }

        /// <summary>
        ///     Creates all directories and subdirectories in the specified path.
        /// </summary>
        /// <param name="path">The directory path to create.</param>
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <summary>
        ///     Returns the absolute path for the specified path string, expanding
        ///     any environment variables.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain absolute path information.</param>
        /// <returns>The fully qualified location of path, such as "C:\MyFile.txt".</returns>
        public string GetFullPathWithEnvironmentVariables(string path)
        {
            var expanded = Environment.ExpandEnvironmentVariables(path);
            return Path.GetFullPath(expanded);
        }

        /// <summary>
        ///     Deletes the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file to delete.</param>
        public void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }

        /// <summary>
        ///     Deletes the specified directory.
        /// </summary>
        /// <param name="directoryPath">The path to the directory to delete.</param>
        public void DeleteDirectory(string directoryPath)
        {
            try
            {
                Directory.Delete(directoryPath);
            }
            catch (DirectoryNotFoundException)
            {
                // this is okay
            }
        }

        /// <summary>
        ///     Deletes the specified directory.
        /// </summary>
        /// <param name="directoryPath">The path to the directory to delete.</param>
        /// <param name="recurse">true to delete sub file and directories; false otherwise.</param>
        public void DeleteDirectory(string directoryPath, bool recurse)
        {
            try
            {
                Directory.Delete(directoryPath, recurse);
            }
            catch (DirectoryNotFoundException)
            {
                // this is okay
            }
        }

        /// <summary>
        ///     Deletes all files and subdirectories in the specified directory, but not itself.
        ///     The specified directory must exist.
        /// </summary>
        public void CleanDirectory(string directoryPath)
        {
            foreach (var file in Directory.EnumerateFiles(directoryPath))
            {
                File.Delete(file);
            }

            foreach (var dir in Directory.EnumerateDirectories(directoryPath))
            {
                Directory.Delete(dir, true);
            }
        }

        /// <summary>
        ///     Copies a file.
        /// </summary>
        /// <param name="source">The source file.</param>
        /// <param name="destination">The destination file.</param>
        public void Copy(string source, string destination)
        {
            File.Copy(source, destination);
        }

        /// <summary>
        ///     Copies a file.
        /// </summary>
        /// <param name="source">The source file.</param>
        /// <param name="destination">The destination file.</param>
        /// <param name="overwrite">true to overwrite if the file exists; false otherwise.</param>
        public void Copy(string source, string destination, bool overwrite)
        {
            File.Copy(source, destination);
        }

        public object Deserialize(Stream stream, ProgressChangedEventHandler callback)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var cs = new ReadProgressStream(stream))
            {
                cs.ProgressChanged += callback;

                const int DefaultBufferSize = 4096;
                int onePercentSize = (int)Math.Ceiling(stream.Length / 100.0);

                using (var bs = new BufferedStream(cs, onePercentSize > DefaultBufferSize ? DefaultBufferSize : onePercentSize))
                {
                    var formatter = new BinaryFormatter();
                    return formatter.Deserialize(bs);
                }
            }
        }

        /// <summary>
        ///     Writes matching text from an input file to an output file
        /// </summary>
        /// <param name="inputFile">Input file</param>
        /// <param name="outputFile">Output file</param>
        /// <param name="tokensToFind">Strings to match (logical OR)</param>
        public void FindStr(string inputFile, string outputFile, string[] tokensToFind)
        {
            using (var sr = new StreamReader(inputFile))
            {
                using (var sw = new StreamWriter(outputFile))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        // determine if any of the tokensToFind are in this line
                        bool matchFound = tokensToFind.Any(find => line.Contains(find));

                        if (matchFound)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }

        public void CreateInternetShortcutFile(string shortcutFilePath, string url)
        {
            var sb = new StringBuilder();
            sb.Append("[InternetShortcut]").AppendLine();
            sb.AppendFormat(CultureInfo.InvariantCulture, "URL={0}", url);
            string fileContents = sb.ToString();
            WriteTextToFile(shortcutFilePath, fileContents);
        }

        public string CalculateFileHashSha1(string filePath)
        {
            using (var sha = SHA1.Create())
            {
                using (var fs = new FileStream(filePath, FileMode.Open))
                {
                    sha.ComputeHash(fs);
                }

                return BytesToString(sha.Hash);
            }
        }

        public void RobocopyMirror(string sourcePath, string targetPath)
        {
            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException("sourcePath not found: " + sourcePath);
            }

            string args = $"{sourcePath.EncloseQuotes()} {targetPath.EncloseQuotes()} /MIR";

            _processUtils.RunProcess(
                "robocopy.exe",
                args,
                null,
                null,
                false,
                new List<int>
                {
                    0,
                    1,
                    2,
                    3
                });
        }

        public void RemoveReadOnlyFlag(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return;
            }

            RemoveReadOnlyFlag(new DirectoryInfo(directoryPath));
        }

        public IEnumerable<string> EnumerateFiles(string path)
        {
            return EnumerateFiles(path, "*");
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            return EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }

        public long GetFileSize(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        public long GetDirectorySize(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            return directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fileInfo => fileInfo.Length);
        }

        public Stream OpenReadableStream(string filePath)
        {
            return new FileStream(filePath, FileMode.Open);
        }

        public Stream OpenWritableStream(string filePath)
        {
            return new FileStream(filePath, FileMode.OpenOrCreate);
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        /// <inheritdoc />
        public string DosPathToNtPath(string dosPath)
        {
            if (dosPath == null)
            {
                throw new ArgumentNullException(nameof(dosPath));
            }

            NativeStructs.UNICODE_STRING path = new NativeStructs.UNICODE_STRING(dosPath.Length + 4);

            try
            {
                if (!NativeMethods.RtlDosPathNameToNtPathName_U(dosPath, ref path, null, IntPtr.Zero))
                {
                    throw new ArgumentException($"Invalid DOS-style path '{dosPath}'.", nameof(dosPath));
                }

                return path.ToString();
            }
            finally
            {
                path.Dispose();
            }
        }

        /// <inheritdoc />
        public string CombinePathsKeepingSeparator(params string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            string result = Path.Combine(paths);

            string last = paths.LastOrDefault();

            if (last != null && EndsInSeparator(last))
            {
                return $"{result}{Path.DirectorySeparatorChar}";
            }

            return result;
        }

        private static string GetTempWorkDirName()
        {
            // get system temp path
            string tempPath = Path.GetTempPath();

            // create a random dir name off this path
            string tempDirName = Path.GetRandomFileName();

            // ensure this path doesn't exist
            while (Directory.Exists(Path.Combine(tempPath, tempDirName)))
            {
                tempDirName = Path.GetRandomFileName();
            }

            // create temp path
            return Path.Combine(tempPath, tempDirName);
        }

        private string BytesToString(IEnumerable<byte> bytes)
        {
            var str = new StringBuilder();

            foreach (byte t in bytes)
            {
                str.AppendFormat(CultureInfo.InvariantCulture, "{0:X2}", t);
            }

            return str.ToString();
        }

        /// <summary>
        ///     Determines if a path ends in a path separator character.
        /// </summary>
        /// <param name="path">Absolute or relative path in question.</param>
        /// <returns>True if the path ends in a separator.</returns>
        private static bool EndsInSeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                   path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

        private bool IsLocalFile(string fileNameAndPath)
        {
            return fileNameAndPath.Length > 2 && fileNameAndPath[1] == ':' && fileNameAndPath[2] == '\\';
        }

        private void RemoveReadOnlyFlag(DirectoryInfo directoryInfo)
        {
            directoryInfo.Attributes &= ~FileAttributes.ReadOnly;

            foreach (FileInfo fi in directoryInfo.GetFiles())
            {
                fi.Attributes &= ~FileAttributes.ReadOnly;
            }

            foreach (DirectoryInfo sourceSubDir in directoryInfo.GetDirectories())
            {
                RemoveReadOnlyFlag(sourceSubDir);
            }
        }
    }
}