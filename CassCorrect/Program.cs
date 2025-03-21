using System;
using System.Xml.Linq;

namespace Factograph.Docs
{
    public class Program
    {
        public static void Main(string[] args)
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

                    // Если версия правильная и не iscurremt_fog и не _tiff, то его не обрабатываем поскольку фог корректировать не надо
                    if (ismodern_fog_version && iscurrent_fog && !to_convert_tiff) continue;

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

                    // Теперь сканируем записи, если надо, делаем преобразование или превьюшки
                    foreach (XElement rec in fog.Records())
                    {
                        // Возможная замена
                        string? docmetainfo_next = null;

                        string? id = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
                        XName nm = rec.Name;
                        string? uri = null;
                        string[] infos = new string[0];
                        string mimetype = "unknown";
                        //string? mimetype = null;
                        int width = -1, height = -1; // Чтобы что-то "ругнулось" в случае отсутствия

                        // Теперь мы исходим из того, что метаинформация уже зафиксиована в записи.
                        // Измениться может только documenttype
                        // 
                        // Сначала вычислим uri и infos documenttype, размеры, если фотка или видео  
                        if ((nm.LocalName == "photo-doc" || nm.LocalName == "video-doc") && nm.NamespaceName == "http://fogid.net/o/")
                        {
                            // Сначала выявим uri и infos - массив атрибутов в docmetainfo 
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
                        // Добавок к пути к документам 
                        string doc9path = uri == null ? "" : uri.Substring(uri.Length - 9);
                        
                        // ===== Будем преобразовывать, вычислять и корректировать. По-разному для фото и видео ====

                        if (nm.LocalName == "photo-doc" && nm.NamespaceName == "http://fogid.net/o/")
                        {
                            string? fileToDelete = null;
                            System.Drawing.Bitmap? bitmap = null;

                            // Читаем битмап если: -tiff и документный тип image/tiff и расширение .tif или
                            // нет какой-нибудь первьюшки
                            bool nsm = !File.Exists(casspath + "/documents/small/" + doc9path + ".jpg");
                            bool nme = !File.Exists(casspath + "/documents/medium/" + doc9path + ".jpg");
                            bool nno = !File.Exists(casspath + "/documents/normal/" + doc9path + ".jpg");
                            if ((to_convert_tiff && mimetype == "image/tiff") || nsm || nme || nno)
                            {
                                string ext = mimetype == "image/tiff" ? ".tif" :
                                    (mimetype == "image/jpeg" ? ".jpg" : "." + mimetype.Substring(mimetype.IndexOf('/')+1));
                                bool tifftransformed = false;
                                try
                                {
                                    bitmap = new System.Drawing.Bitmap(casspath + "/originals/" + doc9path + ext);
                                    width = bitmap.Width;
                                    height = bitmap.Height;

                                    // Сначала сохраним оригинал для специфического случая преобразования тиффов
                                    if (mimetype == "image/tiff")
                                    {
                                        bitmap.Save(casspath + "/originals/" + doc9path + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                                        tifftransformed = true;
                                        //File.Delete(casspath + "/originals/" + doc9path + ext);
                                        fileToDelete = casspath + "/originals/" + doc9path + ext;
                                    }
                                    // Вычислим превьюшки и сохраним их
                                    Action<string> calculatePreview = (string sz) =>
                                    {
                                        int previewBase = Int32.Parse(finfo.Element("image")?.Element(sz)?.Attribute("previewBase")?.Value ?? "200");
                                        double factor = width >= height ? (double)previewBase / (double)width : (double)previewBase / (double)height;
                                        System.Drawing.Bitmap bitmap2 = new System.Drawing.Bitmap(bitmap, (int)((double)width*factor), (int)((double)height * factor));
                                        bitmap2.Save(casspath + "/documents/" + sz + "/" + doc9path + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                                        Console.WriteLine($"Preview {sz}/{doc9path}.jpg calculated");
                                    };
                                    if (nsm) calculatePreview("small");
                                    if (nme) calculatePreview("medium");
                                    if (nno) calculatePreview("normal");
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
                            }
                            if (bitmap != null) bitmap.Dispose();
                            if (fileToDelete != null) { File.Delete(fileToDelete); }
                        }
                        else if (nm.NamespaceName == "http://fogid.net/o/" && nm.LocalName == "video-doc")
                        {  // Коррекция записи, вычисление превьюшек
                            Console.WriteLine("video-doc");
                        }

                        // Фиксация записи
                        var nels = rec.Elements().Select(el =>
                        {
                            string pred = el.Name.LocalName;
                            var att = el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource");
                            if (att != null)
                            {
                                return new XElement(XName.Get(pred, "http://fogid.net/o/"),
                                    new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", att.Value));
                            }
                            else if (pred == "docmetainfo" && docmetainfo_next != null)
                            {
                                return new XElement(el.Name, docmetainfo_next);
                            }
                            else
                            {
                                var xlang = el.Attribute(XName.Get("lang", "http://www.w3.org/XML/1998/namespace"));
                                return new XElement(XName.Get(pred, "http://fogid.net/o/"),
                                    (xlang == null ? null : new XAttribute(xlang)),
                                    new XText(el.Value));
                            }
                        });
                        if (id != null) xsbor.Add(new XElement(nm,
                                new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id),
                                nels));

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
