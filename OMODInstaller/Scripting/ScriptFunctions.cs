﻿using System;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OblivionModManager.Scripting
{
    internal class ScriptFunctions : IScriptFunctions
    {
        private readonly System.Security.PermissionSet permissions;
        private readonly ScriptReturnData srd;
        private readonly string DataFiles;
        private readonly string Plugins;
        private readonly string[] dataFileList;
        private readonly string[] pluginList;
        private readonly string[] dataFolderList;
        private readonly string[] pluginFolderList;
        private readonly bool testMode; //ignore

        internal ScriptFunctions(ScriptReturnData srd, string dataFilesPath, string pluginsPath)
        {
            this.srd = srd;
            DataFiles = dataFilesPath;
            Plugins = pluginsPath;

            permissions = new PermissionSet(PermissionState.None);
            List<string> paths = new List<string>(4);

            paths.Add(Program.CurrentDir);
            paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My games\\Oblivion"));
            if (dataFilesPath != null) paths.Add(dataFilesPath);
            if (pluginsPath != null) paths.Add(pluginsPath);

            permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, paths.ToArray()));
            permissions.AddPermission(new UIPermission(UIPermissionWindow.AllWindows));

            testMode = false;
        }

        internal ScriptFunctions(ScriptReturnData srd, string[] dataFiles, string[] plugins)
        {
            this.srd = srd;
            dataFileList = (string[])dataFiles.Clone();
            pluginList = (string[])plugins.Clone();

            //temp
            List<string> df = new List<string>();
            string dir;

            df.Add("");
            for (int i = 0; i < dataFileList.Length; i++)
            {
                dataFileList[i] = dataFileList[i].ToLower();
                dir = dataFileList[i];
                while (dir.Contains(@"\"))
                {
                    dir = Path.GetDirectoryName(dir);
                    if (dir != null && dir != "")
                    {
                        if (!df.Contains(dir)) df.Add(dir);
                    }
                    else break;
                }
            }
            dataFolderList = df.ToArray();

            df.Clear();
            df.Add("");
            for(int i = 0; i < pluginList.Length; i++)
            {
                pluginList[i] = pluginList[i].ToLower();
                dir = pluginList[i];
                while (dir.Contains(@"\"))
                {
                    dir = Path.GetDirectoryName(dir);
                    if (dir != null && dir != "")
                    {
                        if (!df.Contains(dir)) df.Add(dir);
                    }
                    else break;
                }
            }
            pluginFolderList = df.ToArray();

            string[] paths = new string[2];
            paths[0] = Program.CurrentDir;
            paths[1] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My games\\Oblivion");
            permissions = new PermissionSet(PermissionState.None);
            permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, paths));
            permissions.AddPermission(new UIPermission(UIPermissionWindow.AllWindows));
            testMode = true;
        }

        private bool ExistsIn(string path, string[] files)
        {
            if (files == null) return false;
            return Array.Exists(files, new Predicate<string>(path.ToLower().Equals));
        }

        private void CheckPathSafety(string path)
        {
            if(!Program.IsSafeFileName(path)) throw new Exception("Illegal file name: "+path);
        }

        private void CheckPluginSafety(string path)
        {
            permissions.Assert();
            if (!Program.IsSafeFileName(path)) throw new ScriptingException("Illegal file name: " + path);
            if (!(testMode ? ExistsIn(path, pluginList) : File.Exists(Plugins + path))) throw new Exception("File " + path + " not found");
        }

        private void CheckDataSafety(string path)
        {
            permissions.Assert();
            if (!Program.IsSafeFileName(path)) throw new ScriptingException("Illegal file name: " + path);
            if (!(testMode ? ExistsIn(path, dataFileList) : File.Exists(DataFiles + path))) throw new Exception("File " + path + " not found");
        }

        private void CheckFolderSafety(string path)
        {
            if (!Program.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
        }

        private void CheckPluginFolderSafety(string path)
        {
            permissions.Assert();
            if (path.EndsWith("\\") || path.EndsWith("/")) path = path.Remove(path.Length - 1);
            if (!Program.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
            if (!(testMode ? ExistsIn(path, pluginFolderList) : Directory.Exists(Plugins + path))) throw new ScriptingException("Folder " + path + " not found");
        }

        private void CheckDataFolderSafety(string path)
        {
            permissions.Assert();
            if (path.EndsWith("\\") || path.EndsWith("/")) path = path.Remove(path.Length - 1);
            if (!Program.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
            if (!(testMode ? ExistsIn(path, dataFolderList) : Directory.Exists(DataFiles + path))) throw new Exception("Folder " + path + " not found");
        }

        // Looks sick but is just used to see what files will be affected and only called when testmode is true
        // dunno if actually useful maybe delete later
        private string[] SimulateFSOutput(string[] fsList, string path, string pattern, bool recurse)
        {
            pattern = "^" + (pattern == "" ? ".*" : pattern.Replace("[", @"\[").Replace(@"\", "\\").Replace("^", @"\^").Replace("$", @"\$").
                Replace("|", @"\|").Replace("+", @"\+").Replace("(", @"\(").Replace(")", @"\)").
                Replace(".", @"\.").Replace("*", ".*").Replace("?", ".{0,1}")) + "$";
            return Array.FindAll(fsList, delegate (string value)
            {
                if ((path.Length > 0 && value.StartsWith(path.ToLower() + @"\")) || path.Length == 0)
                {
                    if (value == "" || (!recurse && Regex.Matches(value.Substring(path.Length), @"\\", RegexOptions.None).Count > 1)) return false;
                    if (Regex.IsMatch(value.Substring(value.LastIndexOf('\\') + 1), pattern)) return true;
                }
                return false;
            });
        }

        /// <summary>
        /// Returns all files within a folder that match the pattern
        /// </summary>
        /// <param name="path">Path to the folder</param>
        /// <param name="pattern">The regex pattern</param>
        /// <param name="recurse">To check for only the top-level directory or sub-directories</param>
        /// <returns></returns>
        private string[] GetFilePaths(string path, string pattern, bool recurse)
        {
            permissions.Assert();
            return Directory.GetFiles(path, (pattern != "" && pattern != null) ? pattern : "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Returns all directories within a folder that match the pattern
        /// </summary>
        /// <param name="path">Path to the folder</param>
        /// <param name="pattern">The regex pattern</param>
        /// <param name="recurse">To check for only the top-level directory or sub-directories</param>
        /// <returns></returns>
        private string[] GetDirectoryPaths(string path, string pattern, bool recurse)
        {
            permissions.Assert();
            return Directory.GetDirectories(path, (pattern != "" && pattern != null) ? pattern : "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Removes the rooted element of all paths in an array (rooted meaning C:dir, /dir or C:/dir)
        /// </summary>
        /// <param name="paths">Array of paths to strip</param>
        /// <param name="baseLength">The position where the new path starts</param>
        /// <returns></returns>
        private string[] StripPathList(string[] paths, int baseLength)
        {
            for (int i = 0; i < paths.Length; i++) if (Path.IsPathRooted(paths[i])) paths[i] = paths[i].Substring(baseLength);
            return paths;
        }

        public void CancelDataFileCopy(string file)
        {
            throw new NotImplementedException();
        }

        public void CancelDataFolderCopy(string folder)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string filename)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string filename, string comment, ConflictLevel level)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, ConflictLevel level)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, ConflictLevel level, bool regex)
        {
            throw new NotImplementedException();
        }

        public void ConslictsWith(string filename, string comment)
        {
            throw new NotImplementedException();
        }

        public void CopyDataFile(string from, string to)
        {
            throw new NotImplementedException();
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            throw new NotImplementedException();
        }

        public void CopyPlugin(string from, string to)
        {
            throw new NotImplementedException();
        }

        public Form CreateCustomDialog()
        {
            throw new NotImplementedException();
        }

        public bool DataFileExists(string path)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string filename)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string filename, string comment)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, bool regex)
        {
            throw new NotImplementedException();
        }

        public bool DialogYesNo(string msg) { return DialogYesNo(msg, "Question"); }

        public bool DialogYesNo(string msg, string title)
        {
            return MessageBox.Show(msg, title, System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes;
        }

        public void DisplayImage(string path) { DisplayImage(path, null); }

        public void DisplayImage(string path, string title)
        {
            throw new NotImplementedException();
        }

        public void DisplayText(string path)
        {
            throw new NotImplementedException();
        }

        public void DisplayText(string path, string title)
        {
            throw new NotImplementedException();
        }

        public void DontInstallAnyDataFiles()
        {
            throw new NotImplementedException();
        }

        public void DontInstallAnyPlugins()
        {
            throw new NotImplementedException();
        }

        public void DontInstallDataFile(string name)
        {
            throw new NotImplementedException();
        }

        public void DontInstallDataFolder(string folder, bool recurse)
        {
            throw new NotImplementedException();
        }

        public void DontInstallPlugin(string name)
        {
            throw new NotImplementedException();
        }

        public void EditINI(string section, string key, string value)
        {
            throw new NotImplementedException();
        }

        public void EditShader(byte package, string name, string path)
        {
            throw new NotImplementedException();
        }

        public void EditXMLLine(string file, int line, string value)
        {
            throw new NotImplementedException();
        }

        public void EditXMLReplace(string file, string find, string replace)
        {
            throw new NotImplementedException();
        }

        public void FatalError()
        {
            throw new NotImplementedException();
        }

        public void GenerateBSA(string file, string path, string prefix, int cRatio, int cLevel)
        {
            throw new NotImplementedException();
        }

        public void GenerateNewDataFile(string file, byte[] data)
        {
            throw new NotImplementedException();
        }

        public string[] GetActiveEspNames()
        {
            permissions.Assert();
            List<string> names = new List<string>();
            List<EspInfo> Esps = Program.Data.Esps;
            for (int i = 0; i < Esps.Count; i++) if (Esps[i].Active) names.Add(Program.Data.Esps[i].FileName);
            return names.ToArray();
        }

        public string[] GetActiveOmodNames()
        {
            throw new NotImplementedException();
        }

        public byte[] GetDataFileFromBSA(string file)
        {
            CheckPathSafety(file);
            permissions.Assert();
            return Classes.BSAArchive.GetFileFromBSA(file);
        }

        public byte[] GetDataFileFromBSA(string bsa, string file)
        {
            CheckPathSafety(file);
            permissions.Assert();
            return Classes.BSAArchive.GetFileFromBSA(bsa, file);
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            CheckDataFolderSafety(path);
            return testMode ? SimulateFSOutput(dataFileList, path, pattern, recurse)
                : StripPathList(GetFilePaths(DataFiles + path, pattern, recurse), DataFiles.Length);
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            CheckDataFolderSafety(path);
            return testMode ? SimulateFSOutput(dataFolderList, path, pattern, recurse)
                : StripPathList(GetDirectoryPaths(DataFiles + path, pattern, recurse), DataFiles.Length);
        }

        public bool GetDisplayWarnings() { return false; }

        public string[] GetExistingEspNames()
        {
            permissions.Assert();
            string[] names = new string[Program.Data.Esps.Count];
            for (int i = 0; i < names.Length; i++) names[i] = Program.Data.Esps[i].FileName;
            return names;
        }

        // TODO: OBMM had to be placed inside the oblivion folder, need to change that
        public Version GetOBGEVersion()
        {
            permissions.Assert();
            if (!File.Exists("data\\obse\\plugins\\obge.dll")) return null;
            else return new Version(FileVersionInfo.GetVersionInfo("data\\obse\\plugins\\obge.dll").FileVersion.Replace(", ", "."));
        }

        public Version GetOblivionVersion()
        {
            permissions.Assert();
            return new Version(FileVersionInfo.GetVersionInfo("oblivion.exe").FileVersion.Replace(", ", "."));
        }

        public Version GetOBMMVersion()
        {
            return new Version(Program.MajorVersion, Program.MinorVersion, Program.BuildNumber, 0);
        }

        public Version GetOBSEPluginVersion(string plugin)
        {
            plugin = Path.ChangeExtension(Path.Combine("data\\obse\\plugins", plugin), ".dll");
            CheckPathSafety(plugin);
            permissions.Assert();
            if (!File.Exists(plugin)) return null;
            else return new Version(FileVersionInfo.GetVersionInfo(plugin).FileVersion.Replace(", ", "."));
        }

        public Version GetOBSEVersion()
        {
            permissions.Assert();
            if (!File.Exists("obse_loader.exe")) return null;
            else return new Version(FileVersionInfo.GetVersionInfo("obse_loader.exe").FileVersion.Replace(", ", "."));
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            CheckPluginFolderSafety(path);
            return testMode ? SimulateFSOutput(pluginFolderList, path, pattern, recurse)
                : StripPathList(GetDirectoryPaths(Plugins + path, pattern, recurse), Plugins.Length);
        }

        public string[] GetPlugins(string path, string pattern, bool recurse)
        {
            CheckPluginFolderSafety(path);
            return testMode ? SimulateFSOutput(pluginList, path, pattern, recurse)
                : StripPathList(GetFilePaths(Plugins + path, pattern, recurse), Plugins.Length);
        }

        public string InputString()
        {
            throw new NotImplementedException();
        }

        public string InputString(string title)
        {
            throw new NotImplementedException();
        }

        public string InputString(string title, string initial)
        {
            throw new NotImplementedException();
        }

        public void InstallAllDataFiles()
        {
            throw new NotImplementedException();
        }

        public void InstallAllPlugins()
        {
            throw new NotImplementedException();
        }

        public void InstallDataFile(string name)
        {
            throw new NotImplementedException();
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            throw new NotImplementedException();
        }

        public void InstallPlugin(string name)
        {
            throw new NotImplementedException();
        }

        public bool IsSimulation()
        {
            throw new NotImplementedException();
        }

        public void LoadAfter(string plugin1, string plugin2)
        {
            throw new NotImplementedException();
        }

        public void LoadBefore(string plugin1, string plugin2)
        {
            throw new NotImplementedException();
        }

        public void LoadEarly(string plugin)
        {
            throw new NotImplementedException();
        }

        public void Message(string msg)
        {
            throw new NotImplementedException();
        }

        public void Message(string msg, string title)
        {
            throw new NotImplementedException();
        }

        public void PatchDataFile(string from, string to, bool create)
        {
            throw new NotImplementedException();
        }

        public void PatchPlugin(string from, string to, bool create)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadDataFile(string file)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadExistingDataFile(string file)
        {
            throw new NotImplementedException();
        }

        public string ReadINI(string section, string value)
        {
            throw new NotImplementedException();
        }

        public string ReadRendererInfo(string value)
        {
            throw new NotImplementedException();
        }

        public void RegisterBSA(string path)
        {
            throw new NotImplementedException();
        }

        public string[] Select(string[] items, string[] previews, string[] descs, string title, bool many)
        {
            throw new NotImplementedException();
        }

        public void SetDeactivationWarning(string plugin, DeactiveStatus warning)
        {
            throw new NotImplementedException();
        }

        public void SetGlobal(string file, string edid, string value)
        {
            throw new NotImplementedException();
        }

        public void SetGMST(string file, string edid, string value)
        {
            throw new NotImplementedException();
        }

        public void SetNewLoadOrder(string[] plugins)
        {
            throw new NotImplementedException();
        }

        public void SetPluginByte(string file, long offset, byte value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginFloat(string file, long offset, float value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginInt(string file, long offset, int value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginLong(string file, long offset, long value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginShort(string file, long offset, short value)
        {
            throw new NotImplementedException();
        }

        public void UncheckEsp(string plugin)
        {
            throw new NotImplementedException();
        }

        public void UnregisterBSA(string path)
        {
            throw new NotImplementedException();
        }
    }
}
