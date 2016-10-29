using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using ProtoBuf;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ATB
{
    public class BotBaseLoader : BotBase
    {
        // Change this settings to reflect your project!
        private const string ProjectName = "ATB";

        private const int ProjectId = 12;
        private const string ProjectMainType = "ATB.ATB";
        private const string ProjectAssemblyName = "ATB.dll";
        private static readonly Color LogColor = Colors.LawnGreen;
        public override PulseFlags PulseFlags => PulseFlags.All;
        public override bool IsAutonomous => false;
        public override bool WantButton => true;
        public override bool RequiresProfile => false;

        // Don't touch anything else below from here!
        private static readonly object locker = new object();

        private static readonly string projectAssembly = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}\{ProjectAssemblyName}");
        private static readonly string greyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");
        private static readonly string versionPath = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}\version.txt");
        private static readonly string baseDir = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}");
        private static readonly string projectTypeFolder = Path.Combine(Environment.CurrentDirectory, @"BotBases");
        private static volatile bool updaterStarted, updaterFinished, loaded;

        public BotBaseLoader()
        {
            if (updaterStarted) { return; }

            updaterStarted = true;
            Task.Factory.StartNew(AutoUpdate);
        }

        private static object Product { get; set; }

        private static MethodInfo StartFunc { get; set; }

        private static MethodInfo StopFunc { get; set; }

        private static MethodInfo ButtonFunc { get; set; }

        private static MethodInfo RootFunc { get; set; }

        public override string Name => ProjectName;

        public override Composite Root
        {
            get
            {
                if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
                return Product != null ? (Composite)RootFunc.Invoke(Product, null) : new Action();
            }
        }

        public override void OnButtonPress()
        {
            if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
            if (Product != null) { ButtonFunc.Invoke(Product, null); }
        }

        public override void Start()
        {
            if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
            if (Product != null) { StartFunc.Invoke(Product, null); }
        }

        public override void Stop()
        {
            if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
            if (Product != null) { StopFunc.Invoke(Product, null); }
        }

        public static void RedirectAssembly()
        {
            ResolveEventHandler handler = (sender, args) =>
            {
                string name = Assembly.GetEntryAssembly().GetName().Name;
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != name ? null : Assembly.GetEntryAssembly();
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;

            ResolveEventHandler greyMagicHandler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != "GreyMagic" ? null : Assembly.LoadFrom(greyMagicAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += greyMagicHandler;
        }

        private static Assembly LoadAssembly(string path)
        {
            if (!File.Exists(path)) { return null; }

            Assembly assembly = null;
            try { assembly = Assembly.LoadFrom(path); }
            catch (Exception e) { Logging.WriteException(e); }

            return assembly;
        }

        private static object Load()
        {
            RedirectAssembly();

            var assembly = LoadAssembly(projectAssembly);
            if (assembly == null) { return null; }

            Type baseType;
            try { baseType = assembly.GetType(ProjectMainType); }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            object bb;
            try { bb = Activator.CreateInstance(baseType); }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            if (bb != null) { Log(ProjectName + " was loaded successfully."); }
            else { Log("Could not load " + ProjectName + ". This can be due to a new version of Rebornbuddy being released. An update should be ready soon."); }

            return bb;
        }

        private static void LoadProduct()
        {
            lock (locker)
            {
                if (Product != null) { return; }
                Product = Load();
                loaded = true;
                if (Product == null) { return; }

                StartFunc = Product.GetType().GetMethod("Start");
                StopFunc = Product.GetType().GetMethod("Stop");
                ButtonFunc = Product.GetType().GetMethod("OnButtonPress");
                RootFunc = Product.GetType().GetMethod("GetRoot");
            }
        }

        private static void Log(string message)
        {
            message = "[Auto-Updater][" + ProjectName + "] " + message;
            Logging.Write(LogColor, message);
        }

        private static string GetLocalVersion()
        {
            if (!File.Exists(versionPath)) { return null; }
            try
            {
                string version = File.ReadAllText(versionPath);
                return version;
            }
            catch { return null; }
        }

        private static void AutoUpdate()
        {
            var stopwatch = Stopwatch.StartNew();
            string local = GetLocalVersion();

            var message = new VersionMessage { LocalVersion = local, ProductId = ProjectId };
            var responseMessage = GetLatestVersion(message).Result;
            string latest = responseMessage?.LatestVersion;

            if (local == latest || latest == null)
            {
                updaterFinished = true;
                LoadProduct();
                return;
            }

            Log($"Updating to version {latest}.");
            var bytes = responseMessage.Data;
            if (bytes == null || bytes.Length == 0) { return; }

            if (!Clean(baseDir))
            {
                Log("Could not clean directory for update.");
                updaterFinished = true;
                return;
            }

            Log("Extracting new files.");
            if (!Extract(bytes, projectTypeFolder))
            {
                Log("Could not extract new files.");
                updaterFinished = true;
                return;
            }

            if (File.Exists(versionPath)) { File.Delete(versionPath); }
            try { File.WriteAllText(versionPath, latest); }
            catch (Exception e) { Log(e.ToString()); }

            stopwatch.Stop();
            Log($"Update complete in {stopwatch.ElapsedMilliseconds} ms.");
            updaterFinished = true;
            LoadProduct();
        }

        private static bool Clean(string directory)
        {
            foreach (var file in new DirectoryInfo(directory).GetFiles())
            {
                try { file.Delete(); }
                catch { return false; }
            }

            foreach (var dir in new DirectoryInfo(directory).GetDirectories())
            {
                try { dir.Delete(true); }
                catch { return false; }
            }

            return true;
        }

        private static bool Extract(byte[] files, string directory)
        {
            using (var stream = new MemoryStream(files))
            {
                var zip = new FastZip();

                try { zip.ExtractZip(stream, directory, FastZip.Overwrite.Always, null, null, null, false, true); }
                catch (Exception e)
                {
                    Log(e.ToString());
                    return false;
                }
            }

            return true;
        }

        private static async Task<VersionMessage> GetLatestVersion(VersionMessage message)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
                client.BaseAddress = new Uri("http://siune.net");

                var bytes = ToBytes(message);
                var content = new ByteArrayContent(bytes);

                HttpResponseMessage response;
                try { response = await client.PostAsync("/api/products/version", content); }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }

                byte[] responseMessageBytes;
                try { responseMessageBytes = await response.Content.ReadAsByteArrayAsync(); }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }

                var responseMessage = FromBytes<VersionMessage>(responseMessageBytes);
                return responseMessage;
            }
        }

        private static T FromBytes<T>(byte[] data)
        {
            var obj = default(T);
            using (var stream = new MemoryStream(data))
            {
                try { obj = Serializer.Deserialize<T>(stream); }
                catch (Exception e) { Console.WriteLine(e); }
            }

            return obj;
        }

        private static byte[] ToBytes<T>(T obj)
        {
            byte[] data;
            using (var stream = new MemoryStream())
            {
                try { Serializer.Serialize(stream, obj); }
                catch (Exception e) { Console.WriteLine(e); }

                data = stream.ToArray();
            }

            return data;
        }

        [ProtoContract]
        private class VersionMessage
        {
            [ProtoMember(1)]
            public int ProductId { get; set; }

            [ProtoMember(2)]
            public string LocalVersion { get; set; }

            [ProtoMember(3)]
            public string LatestVersion { get; set; }

            [ProtoMember(4)]
            public byte[] Data { get; set; } = new byte[0];
        }
    }
}