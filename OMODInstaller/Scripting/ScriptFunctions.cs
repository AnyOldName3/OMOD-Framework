﻿using System;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

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
            return Array.Exists<string>(files, new Predicate<string>(path.ToLower().Equals));
        }

        private void CheckPathSafty(string path)
        {
            if(!Program.IsSafeFileName(path)) throw new Exception("Illegal file name: "+path);
        }

        private void CheckPluginSafty(string path)
        {
            permissions.Assert();
            if (!Program.IsSafeFileName(path)) throw new ScriptingException("Illegal file name: " + path);
            if (!(testMode ? ExistsIn(path, pluginList) : File.Exists(Plugins + path))) throw new Exception("File " + path + " not found");
        }

        private void CheckDataSafty(string path)
        {
            permissions.Assert();
            if (!Program.IsSafeFileName(path)) throw new ScriptingException("Illegal file name: " + path);
            if (!(testMode ? ExistsIn(path, dataFileList) : File.Exists(DataFiles + path))) throw new Exception("File " + path + " not found");
        }

        private void CheckFolderSafty(string path)
        {
            if (!Program.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
        }

        private void CheckPluginFolderSafty(string path)
        {
            permissions.Assert();
            if (path.EndsWith("\\") || path.EndsWith("/")) path = path.Remove(path.Length - 1);
            if (!Program.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
            if (!(testMode ? ExistsIn(path, pluginFolderList) : Directory.Exists(Plugins + path))) throw new ScriptingException("Folder " + path + " not found");
        }

        private void CheckDataFolderSafty(string path)
        {
            permissions.Assert();
            if (path.EndsWith("\\") || path.EndsWith("/")) path = path.Remove(path.Length - 1);
            if (!Program.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
            if (!(testMode ? ExistsIn(path, dataFolderList) : Directory.Exists(DataFiles + path))) throw new Exception("Folder " + path + " not found");
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

        public bool DialogYesNo(string msg)
        {
            throw new NotImplementedException();
        }

        public bool DialogYesNo(string msg, string title)
        {
            throw new NotImplementedException();
        }

        public void DisplayImage(string path)
        {
            throw new NotImplementedException();
        }

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
            throw new NotImplementedException();
        }

        public string[] GetActiveOmodNames()
        {
            throw new NotImplementedException();
        }

        public byte[] GetDataFileFromBSA(string file)
        {
            throw new NotImplementedException();
        }

        public byte[] GetDataFileFromBSA(string bsa, string file)
        {
            throw new NotImplementedException();
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            throw new NotImplementedException();
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            throw new NotImplementedException();
        }

        public bool GetDisplayWarnings()
        {
            throw new NotImplementedException();
        }

        public string[] GetExistingEspNames()
        {
            throw new NotImplementedException();
        }

        public Version GetOBGEVersion()
        {
            throw new NotImplementedException();
        }

        public Version GetOblivionVersion()
        {
            throw new NotImplementedException();
        }

        public Version GetOBMMVersion()
        {
            throw new NotImplementedException();
        }

        public Version GetOBSEPluginVersion(string plugin)
        {
            throw new NotImplementedException();
        }

        public Version GetOBSEVersion()
        {
            throw new NotImplementedException();
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            throw new NotImplementedException();
        }

        public string[] GetPlugins(string path, string pattern, bool recurse)
        {
            throw new NotImplementedException();
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
