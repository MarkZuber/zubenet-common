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

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.AccessControl;
using System.Threading;
using System.Xml.Linq;

namespace ZubeNet.Common
{
    public interface IFileUtils
    {
        void AddDirectorySecurity(string path, string account, FileSystemRights rights, AccessControlType controlType);
        string CalculateFileHashSha1(string filePath);
        void CopyAll(DirectoryInfo source, DirectoryInfo target, bool recurse);
        void MoveAll(DirectoryInfo source, DirectoryInfo target, bool recurse);
        void CopyOnlyNewFiles(string srcFilePath, string filter, string destination);

        void CopyOnlyNewFiles(string srcFilePath, string filter, string destination, ManualResetEvent cancelRequestedEvent);

        void CopyFile(string srcFilePath, string destination);
        void CopyFile(string srcFilePath, string destination, bool overwrite);
        void MoveFile(string srcFilePath, string destination);

        void CreateInternetShortcutFile(string shortcutFilePath, string url);
        object Deserialize(Stream stream, ProgressChangedEventHandler callback);
        void FindStr(string inputFile, string outputFile, string[] tokensToFind);
        string GetTempWorkDir();
        string GetTextFromFile(string filePath);
        byte[] GetBytesFromFile(string filePath);
        XDocument GetXmlFromFile(string filePath);
        XDocument GetXmlFromFile(string filePath, string validateXpathExpression);

        void RemoveDirectorySecurity(string path, string account, FileSystemRights rights, AccessControlType controlType);

        void SetFileAttributesNormal(DirectoryInfo dir, bool recurse);

        void WriteBytesToFile(string filePath, byte[] contents);
        void WriteTextToFile(string filePath, string contents);
        void AppendTextToFile(string filePath, string contents);
        string WriteTextToTempFile(string contents);
        void RobocopyMirror(string sourcePath, string targetPath);
        void RemoveReadOnlyFlag(string directoryPath);

        void CreateDirectory(string path);
        string GetFullPathWithEnvironmentVariables(string path);
        void DeleteFile(string filePath);
        void DeleteDirectory(string directoryPath);
        void DeleteDirectory(string directoryPath, bool recurse);
        void CleanDirectory(string directoryPath);
        void Copy(string source, string destination);
        void Copy(string source, string destination, bool overwrite);

        IEnumerable<string> EnumerateFiles(string path);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

        long GetFileSize(string filePath);

        long GetDirectorySize(string directoryPath);

        Stream OpenReadableStream(string filePath);
        Stream OpenWritableStream(string filePath);

        bool FileExists(string filePath);
        bool DirectoryExists(string directoryPath);
    }
}