using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace CassConsoleApp
{
    public class Program
    {
        /// <summary>
        /// Первый аргумент расположение кассеты, параметры превью находятся в finfo-файле
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            string bin_directory = @"D:\Home\bin";
            // Директория с внешними запускаемыми программами или C:\Home\bin или D:\Home\bin
            if (Directory.Exists(@"C:\Home\bin")) bin_directory = @"C:\Home\bin";

            Console.WriteLine("Usage: CassConsoleApp preview|compress|collect config-file");

            string command = args.Length > 0 ? args[0] : "collect";
            command = "preview";

            string second_arg = args.Length > 1 ? args[1] : @"C:\Home\CoreSites\config.xml";

            var parts = second_arg.Split('\\', '/');
            string filename = parts == null ? @"C:\Home\CoreSites\config.xml" : parts[parts.Length - 1];
            string[] cassnames = new string[0]; 
            if (filename.ToLower() == "config.xml")
            {
                XElement xconfig = XElement.Load(second_arg);
                cassnames = xconfig.Elements("LoadCassette").Select(x => x.Value).ToArray();
            }
            else
            {
                cassnames = new string[] { second_arg }; 
            }

            // =============== Развертка collect ===============
            if (command == "collect")
            {
                // Определяем множество фог-файлов, входящих в сборку config
                string?[] fog_ids = cassnames.SelectMany(casspath =>
                {
                    // кассетный фог
                    string fog0 = casspath + "/meta/" + casspath.Split('/', '\\').Last() + "_current.fog";
                    // читаем fog0, ищем другие фоги
                    XElement datab = XElement.Load(fog0);
                    return Enumerable.Repeat(fog0, 1).Concat(datab.Elements()
                        .Select(e =>
                        {
                            if (e.Name.LocalName != "document") return (string?)null;
                            string? uri = null;
                            string? documentinfo = null;
                            string? documenttype = null;
                            bool isfog = false;
                            foreach (XElement xel in e.Elements())
                            {
                                if (xel.Name.LocalName == "uri") uri = xel.Value;
                                else if (xel.Name.LocalName == "docmetainfo") documentinfo = xel.Value;
                                else if (xel.Name.LocalName == "iisstore")
                                {
                                    uri = xel.Attributes()
                                        .FirstOrDefault(a => a.Name.LocalName == "uri")?.Value;
                                    documenttype = xel.Attributes()
                                        .FirstOrDefault(a => a.Name.LocalName == "documenttype")?.Value;
                                }
                            }
                            if (uri == null) return null;
                            if (documenttype == null && documentinfo == null) return null;
                            if (documentinfo != null && documentinfo.Contains("documenttype:application/fog;")) isfog = true;
                            else // поищу в iisstore
                            {
                                if (documenttype == "application/fog") isfog = true; 
                            }
                            if (isfog)
                            {
                                // надо преобразовать uri в имя файла
                                string fname = casspath + @"\originals\" + uri.Substring(uri.Length - 10) + ".fog";
                                //return e.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
                                return fname;
                            }
                            return null;
                        })
                        .Where(id => id != null)
                        );
                }).ToArray();


                foreach (string? f_id in fog_ids)
                {
                    if (f_id == null) continue;

                    string xdocroot0 = @"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' 
xmlns='http://fogid.net/o/'></rdf:RDF>";
                    XElement xsbor = XElement.Parse(xdocroot0);

                    OneFog fog = new OneFog(f_id);
                    var attributes = fog.FogAttributes();
                    // Добавлю атрибуты uri, owner, prefix, counter
                    if (attributes.ContainsKey("uri")) xsbor.Add(new XAttribute("uri", attributes["uri"]));
                    if (attributes.ContainsKey("owner")) xsbor.Add(new XAttribute("owner", attributes["owner"]));
                    if (attributes.ContainsKey("prefix")) xsbor.Add(new XAttribute("prefix", attributes["prefix"]));
                    if (attributes.ContainsKey("counter")) xsbor.Add(new XAttribute("counter", attributes["counter"]));
                    xsbor.Add(new XAttribute("version", "fogid-2024"));

                    // Теперь добавим преобразованные записи
                    foreach (XElement rec in fog.Records())
                    {
                        string localname = rec.Name.LocalName;
                        string? id = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
                        xsbor.Add(new XElement(XName.Get(localname, "http://fogid.net/o/"),
                            new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id),
                            rec.Elements().Select(el =>
                            {
                                string pred = el.Name.LocalName;
                                var att = el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource");
                                if (att == null)
                                {
                                    return new XElement(XName.Get(pred, "http://fogid.net/o/"), new XText(el.Value));
                                }
                                else
                                {
                                    return new XElement(XName.Get(pred, "http://fogid.net/o/"),
                                        new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", att.Value));
                                }
                            })));
                    }
                    fog.Close();
                    Console.WriteLine($"Запись документа " + f_id + ".xml");
                    xsbor.Save(f_id + ".xml");
                }


                // Выход из collect
                return;
            }
        



            // =============== конец развертки ===============

            // ============ Начало коррекции превьюшек ============
            foreach (string casspath in cassnames)
            {
                string cass = casspath.Split('/', '\\').Last();
                string cassfog = casspath + "/meta/" + cass + "_current.fog";

                // Типоразмеры
                XElement finfo = XElement.Parse(
    @"<?xml version='1.0' encoding='utf-8'?>
<finfo>
  <image>
    <small previewBase='200' qualityLevel='90' />
    <medium previewBase='480' qualityLevel='90' />
    <normal previewBase='900' qualityLevel='90' />
  </image>
  <video>
    <medium videoBitrate='400K' audioBitrate='22050' rate='10' framesize='384x288' previewBase='600' />
  </video>
</finfo>");
                // Warning: пока нет чтения finfo из кассеты или из параметров команды 
                OneFog fog = new OneFog(cassfog);
                // Кажется, выделения атрибутов можно не делать, поскольку в FogAttributes они сформированы 
                Dictionary<string, string> attributes = new Dictionary<string, string>();
                foreach (var pair in fog.FogAttributes())
                {
                    Console.WriteLine($"Fog attribute: {pair.Key}=>{pair.Value}");
                    attributes.Add(pair.Key, pair.Value);
                }

                Console.WriteLine("===========<<< начало вычисления превьюшек =============");

                foreach (XElement rec in fog.Records())
                {
                    string localname = rec.Name.LocalName;
                    string? id = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
                    int width = 1440, height = 1080;
                    if (id == null) { continue; }
                    string? documenttype = null;

                    // Сначала выявим uri и infos - массив атрибутов в docmetainfo 
                    string? uri = rec.Element(XName.Get("uri", "http://fogid.net/o/"))?.Value;
                    string? docmetainfo = rec.Element(XName.Get("docmetainfo", "http://fogid.net/o/"))?.Value;
                    string[] infos = docmetainfo != null ? docmetainfo.Split(';') : new string[0];

                    // ============== command = "compress" ===============
                    if (command == "compress")
                    {
                        if (localname == "photo-doc" && uri != null)
                        {
                            Console.WriteLine("photo-doc1: " + uri);
                            // Если есть tif как оригинал, его надо преобразовать в .jpg
                            string source = casspath + @"\originals\" + uri.Substring(uri.Length - 10) + ".tif";
                            string? target = null;
                            if (System.IO.File.Exists(source))
                            {
                                target = casspath + @"\originals\" + uri.Substring(uri.Length - 10) + ".jpg";
                                Process proc1 = new Process();
                                proc1.StartInfo.FileName = bin_directory + @"\magick.exe";
                                proc1.StartInfo.WorkingDirectory = bin_directory;
                                proc1.StartInfo.ArgumentList.Add(source);
                                proc1.StartInfo.ArgumentList.Add(target);
                                proc1.Start();
                                try
                                {
                                    proc1.WaitForExit();
                                    Console.WriteLine(target + " <- " + " OK");
                                }
                                finally
                                {
                                    proc1.Dispose();
                                }
                            }
                            if (target != null && System.IO.File.Exists(target))
                            {
                                System.IO.File.Delete(source);
                            }
                        }

                        continue;
                    }
                    // ============== end of "compress" =============
                    
                    // Работаем в масштабах:
                    string[] frame_sizes = new string[] { "small", "medium", "normal" };

                    // Если localname это photo-doc - надо для каждого типоразмера вычислять коэффициент
                    // масштабирования и помещать команду вычисления превьюшки в поток команд.  
                    if (localname == "photo-doc" && uri != null)
                    {
                        Console.WriteLine("photo-doc: " + uri);

                        foreach (string info in infos)
                        {
                            if (info.StartsWith("width:"))
                                width = Int32.Parse(info.Substring("width:".Length));
                            else if (info.StartsWith("height:"))
                                height = Int32.Parse(info.Substring("height:".Length));
                            else if (info.StartsWith("documenttype:"))
                                documenttype = info.Substring("documenttype:".Length);
                        }
                        string? ext = documenttype?.Substring(documenttype.LastIndexOf('/') + 1)?.ToLower();
                        if (ext == "jpg" || ext == "jpeg") ext = "jpg";
                        else if (ext == "tif" || ext == "tiff") ext = "tif";
                        // Документ-оригинал:
                        string original = casspath + "\\originals" + uri.Substring(uri.Length - 10) + "." + ext;
                        // Оригинал может быть .jpg, надо проверить наличие файла
                        if (ext == "tif" && !System.IO.File.Exists(original))
                            original = casspath + "\\originals" + uri.Substring(uri.Length - 10) + ".jpg";

                        // Вычислим фактор "увеличения" для каждого типоразмера
                        int more = width > height ? width : height;

                        Dictionary<string, double> factors = new Dictionary<string, double>();
                        string? pBs = finfo.Element("image")?.Element("small")?
                            .Attribute("previewBase")?.Value;
                        factors.Add("small", pBs != null ? (double)Int32.Parse(pBs) / (double)more : -1);
                        string? pBm = finfo.Element("image")?.Element("medium")?
                            .Attribute("previewBase")?.Value;
                        factors.Add("medium", pBm != null ? (double)Int32.Parse(pBm) / (double)more : -1);
                        string? pBn = finfo.Element("image")?.Element("normal")?
                            .Attribute("previewBase")?.Value;
                        factors.Add("normal", pBn != null ? (double)Int32.Parse(pBn) / (double)more : -1);

                        foreach (string fsize in frame_sizes)
                        {
                            // используем uri для вычисления позиций 
                            // <uri>iiss://t2@iis.nsk.su/0001/0001/0003</uri>
                            // Берем casspath, к этому мы добавляем последние 10 символов и
                            // добавляем файловое расширение. Где его взять - непонятно. Попробую
                            // взять в списке атрибутов documenttype 

                            // Превьюшки:
                            string target = casspath + "\\documents\\" + fsize + uri.Substring(uri.Length - 10) + ".jpg";

                            if (factors[fsize] < 0) continue;
                            // Вычислим целую и дробную части resize-фактора

                            double factor1000 = factors[fsize] * 1000;
                            int ifactor = (int)(factor1000 / 10);
                            int rfactor = (int)(factor1000) % 10;

                            //Process process = Process.Start(magic_path + "\\magick.exe");
                            Process process = new Process();
                            process.StartInfo.FileName = bin_directory + @"\magick.exe";
                            process.StartInfo.WorkingDirectory = bin_directory;
                            //process.StartInfo.WorkingDirectory = @"D:\Home\data\tmp";
                            string ars = $"\"{original}\" \"-resize {ifactor}.{rfactor}%\" \"{fsize}\"";

                            process.StartInfo.ArgumentList.Add(original);
                            process.StartInfo.ArgumentList.Add("-resize");
                            process.StartInfo.ArgumentList.Add(ifactor + "." + rfactor + "%");
                            process.StartInfo.ArgumentList.Add(target);
                            process.Start();
                            try
                            {
                                process.WaitForExit();
                                Console.WriteLine(target + " <- " + original + "   " + fsize + " " + ext + " OK");
                            }
                            finally
                            {
                                process.Dispose();
                            }
                        }
                    }
                    else if (localname == "video-doc" && uri != null) // ================ Обработка видео! =================
                    {
                        Console.WriteLine("video-doc: " + uri);

                        foreach (string info in infos)
                        {
                            if (info.StartsWith("width:"))
                                width = Int32.Parse(info.Substring("width:".Length));
                            else if (info.StartsWith("height:"))
                                height = Int32.Parse(info.Substring("height:".Length));
                            else if (info.StartsWith("documenttype:"))
                                documenttype = info.Substring("documenttype:".Length);
                        }
                        string? ext = documenttype?.Substring(documenttype.LastIndexOf('/') + 1);

                        if (ext == "x-msvideo") ext = "avi";

                        // Документ-оригинал:
                        string original = casspath + "\\originals" + uri.Substring(uri.Length - 10) + "." + ext;

                        Console.WriteLine("Файл: " + original);

                        // =============== Вычисление ширины и высоты через обращение к MediaInfo ===
                        XElement? xoutput = null;
                        using Process process1 = new Process();
                        {
                            process1.StartInfo.FileName = bin_directory + @"\MediaInfo.exe";
                            process1.StartInfo.WorkingDirectory = bin_directory;
                            process1.StartInfo.ArgumentList.Add("--Output=XML");
                            process1.StartInfo.ArgumentList.Add(original);
                            process1.StartInfo.UseShellExecute = false;
                            process1.StartInfo.RedirectStandardOutput = true;
                            process1.StartInfo.RedirectStandardError = true;

                            //process1.OutputDataReceived += DataReceivedEventHandler; //обработчик события при получении очередной строки с данными
                            //process1.ErrorDataReceived += ErrorReceivedEventHandler; //обработчик события при получении ошибки

                            process1.Start(); //запускаем процесс
                                              //process1.BeginOutputReadLine(); //начинаем считывать данные из потока 
                                              //process1.BeginErrorReadLine(); //начинаем считывать данные об ошибках 

                            //Console.WriteLine(process1.StandardOutput.ReadToEnd());
                            string output = process1.StandardOutput.ReadToEnd();
                            xoutput = XElement.Parse(output);
                            process1.WaitForExit(); //ожидаем окончания работы приложения, чтобы очистить буфер
                                                    //process1.Close(); //завершает процесс
                        };
                        // Оприходуем width и height
                        var swidth = xoutput.Element("File")?
                            .Elements("track").FirstOrDefault(tr => tr.Attribute("type")?.Value == "Video")?
                            .Element("Width")?.Value?.Replace(" ", "");
                        if (swidth != null) width = Int32.Parse(swidth.Substring(0, swidth.Length - "pixels".Length));
                        var sheight = xoutput.Element("File")?
                            .Elements("track").FirstOrDefault(tr => tr.Attribute("type")?.Value == "Video")?
                            .Element("Height")?.Value?.Replace(" ", "");
                        if (sheight != null) height = Int32.Parse(sheight.Substring(0, sheight.Length - "pixels".Length));

                        // =============== конец вычисления ширины и высоты через обращение к MediaInfo ===


                        // Вычислим фактор "увеличения" для каждого типоразмера
                        int more = width > height ? width : height;

                        Console.WriteLine($"width: {width}, height: {height}, more: {more}");

                        Dictionary<string, double> factors = new Dictionary<string, double>();
                        string? pBs = finfo.Element("video")?.Element("small")?
                            .Attribute("previewBase")?.Value;
                        factors.Add("small", pBs != null ? (double)Int32.Parse(pBs) / (double)more : -1);
                        string? pBm = finfo.Element("video")?.Element("medium")?
                            .Attribute("previewBase")?.Value;
                        factors.Add("medium", pBm != null ? (double)Int32.Parse(pBm) / (double)more : -1);
                        string? pBn = finfo.Element("video")?.Element("normal")?
                            .Attribute("previewBase")?.Value;
                        factors.Add("normal", pBn != null ? (double)Int32.Parse(pBn) / (double)more : -1);

                        foreach (string fsize in frame_sizes)
                        {
                            // используем uri для вычисления позиций 
                            // <uri>iiss://t2@iis.nsk.su/0001/0001/0003</uri>
                            // Берем casspath, к этому мы добавляем последние 10 символов и
                            // добавляем файловое расширение. Где его взять - непонятно. Попробую
                            // взять в списке атрибутов documenttype 

                            // Превьюшки:
                            string target = casspath + "\\documents\\" + fsize + uri.Substring(uri.Length - 10) + ".mp4";

                            if (factors[fsize] < 0) continue;
                            // Вычислим целую и дробную части resize-фактора

                            double factor1000 = factors[fsize] * 1000;
                            int ifactor = (int)(factor1000 / 10);
                            int rfactor = (int)(factor1000) % 10;

                            // ======= Внешний ffmpeg вычисления превьюшки
                            Process process2 = new Process();
                            process2.StartInfo.FileName = bin_directory + @"\ffmpeg.exe";
                            process2.StartInfo.WorkingDirectory = bin_directory;
                            //string ars = $"\"{original}\" \"-resize {ifactor}.{rfactor}%\" \"{fsize}\"";

                            process2.StartInfo.ArgumentList.Add("-i");
                            process2.StartInfo.ArgumentList.Add(original);
                            process2.StartInfo.ArgumentList.Add("-y");
                            //process2.StartInfo.ArgumentList.Add("-vcodec");
                            //process2.StartInfo.ArgumentList.Add("libx264");
                            //process2.StartInfo.ArgumentList.Add("-b:4050K");
                            //process2.StartInfo.ArgumentList.Add("-ar");
                            //process2.StartInfo.ArgumentList.Add("22050");
                            process2.StartInfo.ArgumentList.Add("-s");

                            double factor = factors[fsize];
                            int w = (int)(width * factor); if (w % 2 == 1) w += 1;
                            int h = (int)(height * factor); if (h % 2 == 1) h += 1;
                            process2.StartInfo.ArgumentList.Add(w + "x" + h);
                            process2.StartInfo.ArgumentList.Add(target);
                            process2.Start();
                            try
                            {
                                process2.WaitForExit();
                                Console.WriteLine(fsize + " " + ext + " OK");
                            }
                            finally
                            {
                                process2.Dispose();
                            }
                        }
                    }   // =============== конец обработки видео

                }
                fog.Close();

                Console.WriteLine("=====================>>>===========================");

                // ======================== А это - канонически правильная копия fog2 ===========================
                bool makeFog2 = true;
                if (makeFog2)
                {
                    string xdocroot = @"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' 
xmlns='http://fogid.net/o/'></rdf:RDF>";
                    OneFog fog2 = new OneFog(cassfog);

                    XElement xdoc = XElement.Parse(xdocroot);
                    // Добавлю атрибуты uri, owner, prefix, counter

                    if (attributes.ContainsKey("uri")) xdoc.Add(new XAttribute("uri", attributes["uri"]));
                    if (attributes.ContainsKey("owner")) xdoc.Add(new XAttribute("owner", attributes["owner"]));
                    if (attributes.ContainsKey("prefix")) xdoc.Add(new XAttribute("prefix", attributes["prefix"]));
                    if (attributes.ContainsKey("counter")) xdoc.Add(new XAttribute("counter", attributes["counter"]));
                    xdoc.Add(new XAttribute("version", "fogid-2024"));

                    // Теперь добавим преобразованные записи
                    foreach (XElement rec in fog2.Records())
                    {
                        string localname = rec.Name.LocalName;
                        string? id = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
                        xdoc.Add(new XElement(XName.Get(localname, "http://fogid.net/o/"),
                            new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id),
                            rec.Elements().Select(el =>
                            {
                                string pred = el.Name.LocalName;
                                var att = el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource");
                                if (att == null)
                                {
                                    return new XElement(XName.Get(pred, "http://fogid.net/o/"), new XText(el.Value));
                                }
                                else
                                {
                                    return new XElement(XName.Get(pred, "http://fogid.net/o/"),
                                        new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", att.Value));
                                }
                            })));
                    }
                    fog2.Close();
                    //Console.WriteLine(xdoc.ToString());
                    System.IO.File.Move(cassfog, cassfog + ".xml", true);
                    Console.WriteLine($"Запись документа {cassfog}");
                    xdoc.Save(cassfog);
                }

                //xdoc.Save(cassname); // ============ Это сохранение fog-файла

                //convert.Close();
                //writer.Close();
            }
        }

        static void DataReceivedEventHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Console.WriteLine($"Внешний процесс вернул данные: {e.Data}");
            }

        }
        static void ErrorReceivedEventHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Console.WriteLine($"Внешний процесс вернул ошибку: {e.Data}");
            }
        }

    }




}

