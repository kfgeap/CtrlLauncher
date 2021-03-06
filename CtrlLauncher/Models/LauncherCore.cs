﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CtrlLauncher.Models
{
    public class LauncherCore : BindableBase
    {
        public string DataDirectoryPath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data");

        public string CountFilePath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "count.dat");

        public string SettingsFilePath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.yml");

        public Settings Settings { get; private set; }

        public ObservableCollection<AppInfo> Apps { get; } = new ObservableCollection<AppInfo>();

        private List<CountData> countData = new List<CountData>();

        public async Task LoadSettingsAsync()
        {
            if (File.Exists(SettingsFilePath))
                Settings = await Settings.LoadAsync(SettingsFilePath);
            else
            {
                Settings = new Settings();
                await SaveSettingsAsync();
            }
        }

        public async Task SaveSettingsAsync() => await Settings.SaveAsync(SettingsFilePath);

        public async Task LoadAppsAsync()
        {
            Apps.Clear();

            // カウントの読み込み
            countData.Clear();
            if (File.Exists(CountFilePath)) await Task.Run(() =>
            {
                try
                {
                    using (var fs = new FileStream(CountFilePath, FileMode.Open))
                    using (var decomp = new GZipStream(fs, CompressionMode.Decompress))
                        countData.AddRange(XDocument.Load(decomp).Root.Elements().Select(n => new CountData(n)));
                }
                catch { }
            });

            // アプリケーションの読み込み
            if (Directory.Exists(DataDirectoryPath))
            {
                var dirs = Directory.GetDirectories(DataDirectoryPath);
                foreach (var dir in dirs)
                {
                    try
                    {
                        var yamlPath = new[] { "spec.yml", "spec.yaml" }.Select(p => Path.Combine(dir, p)).FirstOrDefault(File.Exists);
                        if (!string.IsNullOrEmpty(yamlPath))
                        {
                            var spec = await AppSpec.LoadAsync(yamlPath);
                            Apps.Add(new AppInfo(this, spec, dir));
                        }
                    }
                    catch { }
                }
            }
        }

        public int GetCount(AppInfo info)
        {
            var data = countData.FirstOrDefault(d => d.DirectoryNameHash == Path.GetFileName(info.Path).GetHashCode() && d.Title == info.AppSpec.Title);
            if (data != null)
                return data.Count;
            return 0;
        }

        public void SetCount(AppInfo info, int value)
        {
            var data = countData.FirstOrDefault(d => d.DirectoryNameHash == Path.GetFileName(info.Path).GetHashCode() && d.Title == info.AppSpec.Title);
            if (data == null)
                countData.Add(new CountData(info) { Count = value });
            else
                data.Count = value;

            saveCountData();
        }

        public async Task ExportCountDataAsync(string path)
        {
            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                foreach (var item in countData.Where(d => d.Count > 0).OrderByDescending(d => d.Count))
                {
                    await sw.WriteLineAsync(item.Title + " " + item.Count.ToString());
                }
            }
        }

        public async Task ExportApplicationIdAsync(string path)
        {
            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                await sw.WriteLineAsync(Apps.Select(a => a.AppSpec.Id.Replace(" ", "").Replace("　", "").Replace("\t", "")).OrderBy(s => s).Aggregate((str, s) => $"{str} {s}"));
            }
        }

        public void ClearCount()
        {
            countData.Clear();
            saveCountData();
        }

        private void saveCountData()
        {
            var doc = new XDocument(new XElement("CountData"));
            foreach (var item in countData)
                doc.Root.Add(item.ToXElement());

            try
            {
                using (var fs = new FileStream(CountFilePath, FileMode.Create))
                using (var comp = new GZipStream(fs, CompressionMode.Compress))
                    doc.Save(comp);
            }
            catch { }
        }

        private class CountData
        {
            public int DirectoryNameHash { get; }

            public string Title { get; }

            public int Count { get; set; }

            public CountData(AppInfo info)
            {
                DirectoryNameHash = Path.GetFileName(info.Path).GetHashCode();
                Title = info.AppSpec.Title;
            }

            public CountData(XElement e)
            {
                DirectoryNameHash = int.Parse(e.Attribute("Hash").Value);
                Title = e.Attribute("Title").Value;
                Count = int.Parse(e.Attribute("Count").Value);
            }

            public XElement ToXElement() => new XElement("Data",
                    new XAttribute("Hash", DirectoryNameHash),
                    new XAttribute("Title", Title ?? ""),
                    new XAttribute("Count", Count));
        }
    }
}
