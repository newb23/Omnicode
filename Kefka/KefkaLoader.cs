using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Enums;
using ff14bot.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using ProtoBuf;
using TreeSharp;
using Action = TreeSharp.Action;

namespace CombatRoutineLoader
{
    public class CombatRoutineLoader : CombatRoutine
    {
        private const string ProjectName = "Kefka";
        private const int ProjectId = 19;
        private const string ProjectMainType = "Kefka.Kefka";
        private const string ProjectAssemblyName = "Kefka.dll";
        private static readonly Color logColor = Color.FromRgb(255, 77, 172);
        public override bool WantButton => true;
        public override float PullRange => 25;

        public override ClassJobType[] Class
        {
            get
            {
                switch (Core.Me.CurrentJob)
                {
                    case ClassJobType.Marauder:
                        return new[] { ClassJobType.Marauder };

                    case ClassJobType.Warrior:
                        return new[] { ClassJobType.Warrior };

                    case ClassJobType.Gladiator:
                        return new[] { ClassJobType.Gladiator };

                    case ClassJobType.Paladin:
                        return new[] { ClassJobType.Paladin };

                    case ClassJobType.Pugilist:
                        return new[] { ClassJobType.Pugilist };

                    case ClassJobType.Monk:
                        return new[] { ClassJobType.Monk };

                    case ClassJobType.Lancer:
                        return new[] { ClassJobType.Lancer };

                    case ClassJobType.Dragoon:
                        return new[] { ClassJobType.Dragoon };

                    case ClassJobType.Archer:
                        return new[] { ClassJobType.Archer };

                    case ClassJobType.Bard:
                        return new[] { ClassJobType.Bard };

                    case ClassJobType.Thaumaturge:
                        return new[] { ClassJobType.Thaumaturge };

                    case ClassJobType.BlackMage:
                        return new[] { ClassJobType.BlackMage };

                    case ClassJobType.Arcanist:
                        return new[] { ClassJobType.Arcanist };

                    case ClassJobType.Summoner:
                        return new[] { ClassJobType.Summoner };

                    case ClassJobType.Rogue:
                        return new[] { ClassJobType.Rogue };

                    case ClassJobType.Ninja:
                        return new[] { ClassJobType.Ninja };

                    case ClassJobType.Machinist:
                        return new[] { ClassJobType.Machinist };

                    case ClassJobType.DarkKnight:
                        return new[] { ClassJobType.DarkKnight };

                    case ClassJobType.Conjurer:
                        return new[] { ClassJobType.Conjurer };

                    case ClassJobType.WhiteMage:
                        return new[] { ClassJobType.WhiteMage };

                    case ClassJobType.Astrologian:
                        return new[] { ClassJobType.Astrologian };

                    case ClassJobType.Scholar:
                        return new[] { ClassJobType.Scholar };

                    default:
                        return new[]
                        {
                            ClassJobType.Marauder, ClassJobType.Warrior,
                            ClassJobType.Gladiator, ClassJobType.Paladin,
                            ClassJobType.Pugilist, ClassJobType.Monk,
                            ClassJobType.Lancer, ClassJobType.Dragoon,
                            ClassJobType.Archer, ClassJobType.Bard,
                            ClassJobType.Thaumaturge, ClassJobType.BlackMage,
                            ClassJobType.Arcanist, ClassJobType.Summoner,
                            ClassJobType.Rogue, ClassJobType.Ninja,
                            ClassJobType.Machinist, ClassJobType.DarkKnight,
                            ClassJobType.Conjurer, ClassJobType.WhiteMage,
                            ClassJobType.Astrologian, ClassJobType.Scholar
                        };
                }
            }
        }

        private static readonly object locker = new object();
        private static readonly string projectAssembly = Path.Combine(Environment.CurrentDirectory, $@"Routines\{ProjectName}\{ProjectAssemblyName}");
        private static readonly string greyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");
        private static readonly string KefkaUIAssembly = Path.Combine(Environment.CurrentDirectory, $@"Routines\{ProjectName}\KefkaUI.Metro.dll");
        private static readonly string kefkaUIIconsAssembly = Path.Combine(Environment.CurrentDirectory, $@"Routines\{ProjectName}\KefkaUI.Metro.IconPacks.dll");
        private static readonly string versionPath = Path.Combine(Environment.CurrentDirectory, $@"Routines\{ProjectName}\version.txt");
        private static readonly string baseDir = Path.Combine(Environment.CurrentDirectory, $@"Routines\{ProjectName}");
        private static readonly string projectTypeFolder = Path.Combine(Environment.CurrentDirectory, @"Routines");
        private static volatile bool updaterStarted, updaterFinished, loaded;

        public CombatRoutineLoader()
        {
            if (updaterStarted) { return; }

            updaterStarted = true;
            Task.Factory.StartNew(AutoUpdate);
        }

        private static object Product { get; set; }

        private static PropertyInfo CombatProp { get; set; }

        private static PropertyInfo HealProp { get; set; }

        private static PropertyInfo PullProp { get; set; }

        private static PropertyInfo PreCombatProp { get; set; }

        private static PropertyInfo CombatBuffProp { get; set; }

        private static PropertyInfo PullBuffProp { get; set; }

        private static PropertyInfo RestProp { get; set; }

        private static MethodInfo PulseFunc { get; set; }

        private static MethodInfo ButtonFunc { get; set; }

        private static MethodInfo InitFunc { get; set; }

        private static MethodInfo ShutDownFunc { get; set; }

        public override string Name => ProjectName;

        public override void OnButtonPress()
        {
            if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
            if (Product != null) { ButtonFunc.Invoke(Product, null); }
        }

        public override void Pulse()
        {
            if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
            if (Product != null) { PulseFunc.Invoke(Product, null); }
        }

        public override void ShutDown()
        {
            if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
            if (Product != null) { ShutDownFunc.Invoke(Product, null); }
        }

        public override Composite CombatBehavior
        {
            get
            {
                if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)CombatProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite HealBehavior
        {
            get
            {
                if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)HealProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite PullBehavior
        {
            get
            {
                if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)PullProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite PreCombatBuffBehavior
        {
            get
            {
                if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)PreCombatProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite CombatBuffBehavior
        {
            get
            {
                if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)CombatBuffProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite PullBuffBehavior
        {
            get
            {
                if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)PullBuffProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite RestBehavior
        {
            get
            {
                if (!loaded && Product == null && updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)RestProp?.GetValue(Product, null); }
                return new Action();
            }
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

            ResolveEventHandler kefkaUIHandler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != "KefkaUI.Metro" ? null : Assembly.LoadFrom(KefkaUIAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += kefkaUIHandler;

            ResolveEventHandler kefkaUIIconsHandler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != "KefkaUI.Metro.IconPacks" ? null : Assembly.LoadFrom(kefkaUIIconsAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += kefkaUIIconsHandler;
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
                if (Product != null)
                {
                    return;
                }
                Product = Load();
                loaded = true;
                if (Product == null)
                {
                    return;
                }

                CombatProp = Product.GetType().GetProperty("CombatBehavior");
                HealProp = Product.GetType().GetProperty("HealBehavior");
                PullProp = Product.GetType().GetProperty("PullBehavior");
                PreCombatProp = Product.GetType().GetProperty("PreCombatBuffBehavior");
                PullBuffProp = Product.GetType().GetProperty("PullBuffBehavior");
                CombatBuffProp = Product.GetType().GetProperty("CombatBuffBehavior");
                RestProp = Product.GetType().GetProperty("RestBehavior");
                PulseFunc = Product.GetType().GetMethod("Pulse");
                ShutDownFunc = Product.GetType().GetMethod("ShutDown");
                ButtonFunc = Product.GetType().GetMethod("OnButtonPress");
                InitFunc = Product.GetType().GetMethod("OnInitialize", new[] { typeof(int) });
                if (InitFunc != null)
                {
#if RB_CN
                Log($"{ProjectName}: CN loaded.");
                InitFunc.Invoke(Product, new[] {(object)3});
#elif RB_64
                Log($"{ProjectName}: 64 loaded.");
                InitFunc.Invoke(Product, new[] {(object)2});
#else
                    Log($"{ProjectName}: 32 loaded.");
                    InitFunc.Invoke(Product, new[] { (object)1 });
#endif
                }
            }
        }

        private static void Log(string message)
        {
            message = "[Auto-Updater][" + ProjectName + "] " + message;
            Logging.Write(logColor, message);
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

            if (Directory.Exists(baseDir + @"\.svn"))
            {
                Log("Found SVN folder. Updating...");
                using (var webClient = new WebClient())
                {
                    var downloadeddGithubArchive = webClient.DownloadData("https://github.com/newb23/Omnicode/blob/master/Kefka.zip?raw=true");

                    Log("Extracting new files.");
                    if (!Extract(downloadeddGithubArchive, projectTypeFolder))
                    {
                        Log("Could not extract new files.");
                        updaterFinished = true;
                        return;
                    }

                    stopwatch.Stop();
                    Log($"Update complete in {stopwatch.ElapsedMilliseconds} ms.");
                    updaterFinished = true;
                    LoadProduct();
                }
            }

            string local = GetLocalVersion();

            var message = new VersionMessage { LocalVersion = local, ProductId = ProjectId };
            var responseMessage = GetLatestVersion(message).Result;
            string latest = responseMessage.LatestVersion;

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