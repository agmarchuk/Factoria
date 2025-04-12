using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace Factograph.Docs
{
    public partial class Program
    {
        public static void Main0(string[] args)
        {
            Console.WriteLine("usage: CassCorrect [-tiff] [-ещечтото] config-file");

            // Директория с внешними запускаемыми программами
            string ext_bin_directory = @"D:\Home\bin\";
            var path_variants = new string[] { @"D:\Home\bin\", @"C:\Home\bin\", System.AppContext.BaseDirectory + "../../../ext_bin/" };
            foreach (var variant in path_variants)
            {
                ext_bin_directory = variant;
                if (File.Exists(ext_bin_directory + "ffmpeg.exe")) break;
            }

            // Параметры
            bool to_convert_tiff = false;

            string config_or_cass = System.AppContext.BaseDirectory + "../../../config.xml";
            string[] cassnames = new string[0];

            foreach (string arg in args)
            {
                if (arg == "-tiff")
                {
                    to_convert_tiff = true;
                }
                else if (arg[0] != '-') // конфигуратор или кассета
                {
                    config_or_cass = arg;
                }
            }
            to_convert_tiff = true; // Буду конвертировать все встретившиеся тиффы
            //to_convert_tiff = false; //  Не буду конвертировать тиффы

            var parts = config_or_cass.Split('\\', '/');
            string filename = parts[parts.Length - 1];
            if (filename.ToLower() == "config.xml")
            {
                XElement xconfig = XElement.Load(config_or_cass);
                cassnames = xconfig.Elements("LoadCassette").Select(x => x.Value).ToArray();
            }
            else
            {
                cassnames = new string[] { config_or_cass };
            }

            foreach (string casspath in cassnames)
            {
                // Типоразмеры дефолтно
                XElement finfo = XElement.Parse(
    @"<?xml version='1.0' encoding='utf-8'?>
<finfo>
  <image>
    <small previewBase='180' qualityLevel='90' />
    <medium previewBase='480' qualityLevel='90' />
    <normal previewBase='900' qualityLevel='90' />
  </image>
  <video>
    <medium videoBitrate='400K' audioBitrate='22050' rate='10' framesize='384x288' previewBase='600' />
  </video>
</finfo>");
                // Если есть, читаем finfo из файла
                if (File.Exists(casspath + "/cassette.finfo"))
                {
                    //finfo = XElement.Load(casspath + "/cassette.finfo"); // пока не читаем!
                }

                // кассетный фог
                string fog0 = casspath + "/meta/" + casspath.Split('/', '\\').Last() + "_current.fog";
                // читаем fog0, ищем все фоги кассеты
                XElement datab = XElement.Load(fog0);
                string[] fog_ids = AllFogsInCass(casspath, fog0, datab);

                // ============== Перебираем нужные фоги кассеты
                foreach (string f_id in fog_ids)
                {
                    // Управляющие переменные
                    bool iscurrent_fog = f_id.EndsWith("_current.fog");
                    bool ismodern_fog_version = false; // Если уже новая версия фога
                    bool to_collect_fog = false; // Собирать новый fog-документ
                    bool to_save_fog = true; // Сохранять новый fog-документ

                    // Заглядываем в fog, получаем версию, готовим чтение атрибутов
                    OneFog fog = new OneFog(f_id);
                    var attributes = fog.FogAttributes();
                    string fog_version = attributes.ContainsKey("version") ? attributes["version"] : "";
                    if (fog_version == "fogid-2024")
                    {
                        ismodern_fog_version = true;
                        to_save_fog = false;
                    }

                    // Если версия правильная и не iscurrent_fog, то его не обрабатываем поскольку фог корректировать не надо
                    if (ismodern_fog_version && !iscurrent_fog) continue;

                    // ==== Начинаем формировать новый фог в xsbor

                    // Формируем заготовку 
                    string xdocroot0 = @"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'
xmlns='http://fogid.net/o/'>
</rdf:RDF>";
                    XElement xsbor = XElement.Parse(xdocroot0);

                    // Добавлю атрибуты uri, owner, prefix, counter, если есть
                    if (attributes.ContainsKey("uri")) xsbor.Add(new XAttribute("uri", attributes["uri"]));
                    if (attributes.ContainsKey("owner")) xsbor.Add(new XAttribute("owner", attributes["owner"]));
                    if (attributes.ContainsKey("prefix")) xsbor.Add(new XAttribute("prefix", attributes["prefix"]));
                    if (attributes.ContainsKey("counter")) xsbor.Add(new XAttribute("counter", attributes["counter"]));
                    // добавлю версию
                    xsbor.Add(new XAttribute("version", "fogid-2024"));

                    // Теперь сканируем записи, если надо, делаем преобразование или превьюшки. Преобразования влекут вычисления и фиксацию метаинформации
                    foreach (XElement rec in fog.Records())
                    {
                        // Возможная замена
                        string? docmetainfo_next = null;
                        XElement? fileStore = null;
                        XElement? recNew = null;

                        string? id = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
                        XName nm = rec.Name;
                        string? uri = null;
                        Func<string, string> doc9path = (ur) => ur == null ? "" : ur.Substring(ur.Length - 9); // Добавок к пути к документам 
                        string[] infos = new string[0];
                        string mimetype = "unknown";
                        int width = -1, height = -1; // Чтобы что-то "ругнулось" в случае отсутствия

                        // ===== Есть 4 варианта, которые влекут обработку: фото, видео, файл, иначе.  

                        if      (nm.LocalName == "photo-doc" && nm.NamespaceName == "http://fogid.net/o/")
                        {
                            // Если новая версия, то нет метаинформации, если старая - то есть, ее надо извлечь
                            // То же самое про конвертацию форрмата и изготовление превьюшек
                            if (ismodern_fog_version) 
                            { 
                                xsbor.Add(new XElement(rec)); // Сохраняем без изменений
                            }
                            else
                            {
                                Console.Write("photo-doc (old version) ");

                                // Поскольку это старый формат, в любом случае из записи изымается поле docmetainfo, запись обновляется.
                                recNew = new XElement(rec.Name, new XAttribute(rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")),
                                    rec
                                    .Elements()
                                    .Where(x => x.Name.LocalName != "docmetainfo" || x.Name.NamespaceName != "http://fogid.net/o/")
                                    .Select(x => new XElement(x))
                                    );
                                xsbor.Add(recNew);

                                // Если это не главный фог, то работать с метаинформацией не надо, просто пропускаем
                                if (!iscurrent_fog) { continue; } 

                                Extract_uri_infos_mimetype_width_height_fromrec(rec, out uri, out infos, ref mimetype, ref width, ref height);
                                Possible_converttiff_calculatepreviews(to_convert_tiff, casspath, finfo, ref to_save_fog, ref docmetainfo_next, doc9path(uri), infos, mimetype, ref width, ref height);
                                

                                // Изготовливается запись FileStore в которую помещается
                                // uri и модифицированная docmetainfo. Идентификатор изготавливается как модификация записи документа
                                fileStore = new XElement("{http://fogid.net/o/}FileStore",
                                    new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id + "_"),
                                    new XElement("{http://fogid.net/o/}forDocument", new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", id)),
                                    new XElement("{http://fogid.net/o/}uri", uri),
                                    new XElement("{http://fogid.net/o/}docmetainfo", docmetainfo_next)
                                    );
                                // Сохраняем новый вариант записи и элемент fileStore 
                                xsbor.Add(fileStore);
                            }
                        }
                        else if (nm.LocalName == "video-doc" && nm.NamespaceName == "http://fogid.net/o/")
                        {
                            if (ismodern_fog_version)
                            {
                                xsbor.Add(new XElement(rec)); // Сохраняем без изменений
                            }
                            else
                            {
                                // Поскольку это старый формат, в любом случае из записи изымается поле docmetainfo, запись обновляется.
                                recNew = new XElement(rec.Name, new XAttribute(rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")),
                                    rec
                                    .Elements()
                                    .Where(x => x.Name.LocalName != "docmetainfo" || x.Name.NamespaceName != "http://fogid.net/o/")
                                    .Select(x => new XElement(x))
                                    );
                                xsbor.Add(recNew);

                                // Если это не главный фог, то работать с метаинформацией не надо, просто пропускаем
                                if (!iscurrent_fog) { continue; }

                                Extract_uri_infos_mimetype_width_height_fromrec(rec, out uri, out infos, ref mimetype, ref width, ref height);
                                using Process process1 = PreviewsCalculations(ext_bin_directory, casspath, finfo, doc9path, infos, ref width, ref height);
                                // Изготовливается запись FileStore в которую помещается
                                // uri и модифицированная docmetainfo. Идентификатор изготавливается как модификация записи документа
                                fileStore = new XElement("{http://fogid.net/o/}FileStore",
                                    new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id + "_"),
                                    new XElement("{http://fogid.net/o/}forDocument", new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", id)),
                                    new XElement("{http://fogid.net/o/}uri", uri),
                                    new XElement("{http://fogid.net/o/}docmetainfo", docmetainfo_next)
                                    );
                                xsbor.Add(fileStore);
                            }

                        }
                        else if (nm.LocalName == "FileStore" && nm.NamespaceName == "http://fogid.net/o/")
                        {
                            // FileStore это новая конструкция. Она допустима лишь в current фогах. В ней есть ссылка на документ (в данном
                            // преобразователе она не используется, но ее необходимо сохранить). Есть uri и docmetainfo. Следующее действие выявит
                            // uri, поля infos, размеры и mime-тип
                            Extract_uri_infos_mimetype_width_height_fromrec(rec, out uri, out infos, ref mimetype, ref width, ref height);

                            // Possible_converttiff_calculatepreviews(to_convert_tiff, casspath, finfo, ref to_save_fog, ref docmetainfo_next, doc9path(uri), infos, mimetype, ref width, ref height);

                            // Простой вариант преобразования - ничего не делать
                            xsbor.Add(new XElement(rec));
                        }
                        else // не фото,  не видео, не файл
                        {
                            xsbor.Add(new XElement(rec)); // Сохраняем без изменений
                        }
                    }
                    fog.Close();

                    if (to_save_fog)
                    {
                        Console.WriteLine($"Saving of document " + f_id);
                        xsbor.Save(f_id + ".tmp");
                        System.IO.File.Move(f_id, f_id + ".old");
                        System.IO.File.Move(f_id + ".tmp", f_id);
                    }
                }
                // ========== конец перебора фогов кассеты
            }
        }

        private static Process PreviewsCalculations(string ext_bin_directory, string casspath, XElement finfo, Func<string, string> doc9path, string[] infos, ref int width, ref int height)
        {
            // Вычисление превьюшек
            string documenttype = "video/mp4"; // дефолтный тип видео

            // Выборка имеющейся информации из docmetainfo
            foreach (string info in infos)
            {
                if (info.StartsWith("width:"))
                    width = Int32.Parse(info.Substring("width:".Length));
                else if (info.StartsWith("height:"))
                    height = Int32.Parse(info.Substring("height:".Length));
                else if (info.StartsWith("documenttype:"))
                    documenttype = info.Substring("documenttype:".Length);
            }
            // Вычисление екстеншина
            string? ext = documenttype?.Substring(documenttype.LastIndexOf('/') + 1);
            if (ext == "x-msvideo") ext = "avi"; //TODO: надо другие типы видео также изымать, хотя можно "положиться" на ffmpeg

            // Документ-оригинал: (наверное возможно  по-другому)
            string original = casspath + "/originals/" + doc9path + "." + ext;
            if (ext == "mpeg" && !File.Exists(original))
            {
                ext = "mpg";
                original = casspath + "/originals/" + doc9path + "." + ext;
            }

            // =============== Вычисление ширины и высоты через обращение к MediaInfo ===
            XElement? xoutput = null;
            Process process1 = new Process();
            {
                process1.StartInfo.FileName = ext_bin_directory + @"MediaInfo.exe";
                process1.StartInfo.WorkingDirectory = ext_bin_directory;
                process1.StartInfo.ArgumentList.Add("--Output=XML");
                process1.StartInfo.ArgumentList.Add(original);
                process1.StartInfo.UseShellExecute = false;
                process1.StartInfo.RedirectStandardOutput = true;
                process1.StartInfo.RedirectStandardError = true;

                process1.Start(); //запускаем процесс

                string output = process1.StandardOutput.ReadToEnd();
                xoutput = XElement.Parse(output);
                process1.WaitForExit(); //ожидаем окончания работы приложения, чтобы очистить буфер
            }
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



            // получим массив элементов вида: <medium ... previewBase='600' /> 
            var vi = finfo.Element("video");
            XElement[] xframes = vi != null ? vi.Elements().ToArray() : Array.Empty<XElement>();

            // Пройдемся по элементам массива
            foreach (XElement xfr in xframes)
            {
                // Проверяем наличие превьюшки
                string sz = xfr.Name.LocalName;
                if (File.Exists(casspath + "/documents/medium/" + doc9path + ".mp4")) continue;

                // Делаем преобразование 
                Console.WriteLine($"video-doc {sz} w={width} h={height}");

            }



            //// Вычислим фактор "увеличения" для каждого типоразмера
            //int more = width > height ? width : height;

            //Dictionary<string, double> factors = new Dictionary<string, double>();
            //string? pBs = finfo.Element("video")?.Element("small")?
            //    .Attribute("previewBase")?.Value;
            //factors.Add("small", pBs != null ? (double)Int32.Parse(pBs) / (double)more : -1);
            //string? pBm = finfo.Element("video")?.Element("medium")?
            //    .Attribute("previewBase")?.Value;
            //factors.Add("medium", pBm != null ? (double)Int32.Parse(pBm) / (double)more : -1);
            //string? pBn = finfo.Element("video")?.Element("normal")?
            //    .Attribute("previewBase")?.Value;
            //factors.Add("normal", pBn != null ? (double)Int32.Parse(pBn) / (double)more : -1);

            //foreach (var fsize in xframes)
            //{
            //    // Превьюшки:
            //    string target = casspath + "\\documents\\" + fsize.Name + uri.Substring(uri.Length - 10) + ".mp4";

            //    // Не вычисляем если уже есть
            //    if (File.Exists(target)) continue;

            //    if (factors[fsize] < 0) continue;
            //    // Вычислим целую и дробную части resize-фактора

            //    double factor1000 = factors[fsize] * 1000;
            //    int ifactor = (int)(factor1000 / 10);
            //    int rfactor = (int)(factor1000) % 10;

            //    // ======= Внешний ffmpeg вычисления превьюшки
            //    Process process2 = new Process();
            //    process2.StartInfo.FileName = ext_bin_directory + @"ffmpeg.exe";
            //    process2.StartInfo.WorkingDirectory = ext_bin_directory;
            //    //string ars = $"\"{original}\" \"-resize {ifactor}.{rfactor}%\" \"{fsize}\"";

            //    process2.StartInfo.ArgumentList.Add("-i");
            //    process2.StartInfo.ArgumentList.Add(original);
            //    process2.StartInfo.ArgumentList.Add("-y");
            //    process2.StartInfo.ArgumentList.Add("-s");

            //    double factor = factors[fsize];
            //    int w = (int)(width * factor); if (w % 2 == 1) w += 1;
            //    int h = (int)(height * factor); if (h % 2 == 1) h += 1;
            //    process2.StartInfo.ArgumentList.Add(w + "x" + h);
            //    process2.StartInfo.ArgumentList.Add(target);
            //    process2.Start();
            //    try
            //    {
            //        process2.WaitForExit();
            //        Console.WriteLine("Preview video-doc: " + uri);
            //        Console.WriteLine($"width: {width}, height: {height}, more: {more}");
            //        Console.WriteLine(fsize + " " + ext + " OK");
            //    }
            //    finally
            //    {
            //        process2.Dispose();
            //    }
            //}
            return process1;
        }

        private static void Possible_converttiff_calculatepreviews(bool to_convert_tiff, string casspath, XElement finfo, ref bool to_save_fog, ref string? docmetainfo_next, string doc9, string[] infos, string mimetype, ref int width, ref int height)
        {
            // Возможно, нужно выполнить трансформацию файла документа
            string? fileToDelete = null;
            System.Drawing.Bitmap? bitmap = null;

            // Читаем битмап если: -tiff и документный тип image/tiff и расширение .tif или
            // нет какой-нибудь первьюшки
            bool nsm = !File.Exists(casspath + "/documents/small/" + doc9 + ".jpg");
            bool nme = !File.Exists(casspath + "/documents/medium/" + doc9 + ".jpg");
            bool nno = !File.Exists(casspath + "/documents/normal/" + doc9 + ".jpg");
            if ((to_convert_tiff && mimetype == "image/tiff") || nsm || nme || nno)
            {
                string ext = mimetype == "image/tiff" ? ".tif" :
                    (mimetype == "image/jpeg" ? ".jpg" : "." + mimetype.Substring(mimetype.IndexOf('/') + 1));
                bool tifftransformed = false;
                try
                {
                    // Делаем битмап файл-документа! Заодно, скорректируем ширину и высоту
                    bitmap = new System.Drawing.Bitmap(casspath + "/originals/" + doc9 + ext);
                    width = bitmap.Width;
                    height = bitmap.Height;
                    Console.Write("bitmap based on " + (casspath + "/originals/" + doc9 + ext) + " ");

                    // Будем сохранять. Сначала сохраним оригинал для специфического случая преобразования тиффов
                    if (mimetype == "image/tiff")
                    {
                        bitmap.Save(casspath + "/originals/" + doc9 + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        tifftransformed = true;
                        Console.Write("tifftransformed ");
                        //File.Delete(casspath + "/originals/" + doc9path + ext);
                        fileToDelete = casspath + "/originals/" + doc9 + ext;
                    }

                    // Опеределим вычисление превьюшки и сохрания ее
                    Action<string, int, int, string> calculatePreview = (string sz, int width, int height, string doc9) =>
                    {
                        int previewBase = Int32.Parse(finfo.Element("image")?.Element(sz)?.Attribute("previewBase")?.Value ?? "200");
                        double factor = width >= height ? (double)previewBase / (double)width : (double)previewBase / (double)height;
                        System.Drawing.Bitmap bitmap2 = new System.Drawing.Bitmap(bitmap, (int)((double)width * factor), (int)((double)height * factor));
                        bitmap2.Save(casspath + "/documents/" + sz + "/" + doc9 + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        Console.Write($"Preview {sz}/{doc9}.jpg calculated; ");
                    };
                    //  вычислим превьюшки если надо
                    if (nsm) calculatePreview("small", width, height, doc9);
                    if (nme) calculatePreview("medium", width, height, doc9);
                    if (nno) calculatePreview("normal", width, height, doc9);
                }
                catch (Exception) { }

                // Корректируем docmetainfo
                if (tifftransformed)
                {
                    docmetainfo_next = infos.Select(pa =>
                    {
                        if (pa.StartsWith("documenttype:")) return "documenttype:image/jpeg";
                        return pa;
                    })
                    .Aggregate((sum, s) => sum + ";" + s);
                    to_save_fog = true;
                }
                else
                {
                    docmetainfo_next = infos
                        .Aggregate((sum, s) => sum + ";" + s);
                    to_save_fog = true;
                }
            }
            if (bitmap != null) bitmap.Dispose();
            if (fileToDelete != null) { File.Delete(fileToDelete); }
            Console.WriteLine();
        }

        private static void Extract_uri_infos_mimetype_width_height_fromrec(XElement rec, out string? uri, out string[] infos, ref string mimetype, ref int width, ref int height)
        {
            uri = rec.Element(XName.Get("uri", "http://fogid.net/o/"))?.Value;
            string? docmetainfo = rec.Element(XName.Get("docmetainfo", "http://fogid.net/o/"))?.Value;
            infos = docmetainfo != null ? docmetainfo.Split(';').Select(s => s.ToLower()).ToArray() : new string[0];

            foreach (string info in infos)
            {
                if (info.StartsWith("width:"))
                    width = Int32.Parse(info.Substring("width:".Length));
                else if (info.StartsWith("height:"))
                    height = Int32.Parse(info.Substring("height:".Length));
                else if (info.StartsWith("documenttype:"))
                    mimetype = info.Substring("documenttype:".Length);
            }
        }

        private static string[] AllFogsInCass(string casspath, string fog0, XElement datab)
        {
            string[] fog_ids = Enumerable.Repeat(fog0, 1).Concat(datab.Elements()
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
                        string fname = casspath + @"/originals/" + uri.Substring(uri.Length - 9) + ".fog";
                        //return e.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
                        return fname;
                    }
                    return null;
                })
                .Where(id => id != null)
                ).Cast<string>().ToArray();
            return fog_ids;
        }
    }
}
