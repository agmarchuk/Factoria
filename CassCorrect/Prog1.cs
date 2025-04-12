using System;
using System.Diagnostics;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Factograph.Docs
{
    public partial class Program
    {
        public class XNms
        {
            public static XName rdfabout = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about";
            public static XName rdfresource = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource";
            public const string fogi = "{http://fogid.net/o/}";
            public const string fog = "http://fogid.net/o/";
            public static XName xmllang = "{http://www.w3.org/XML/1998/namespace}lang";
            // Константы
            public static XName Photo = XName.Get("photo-doc", "http://fogid.net/o/");
            public static XName Video = XName.Get("video-doc", "http://fogid.net/o/");
            public static XName Audio = XName.Get("audio-doc", "http://fogid.net/o/");
            public static XName Document = XName.Get("document", "http://fogid.net/o/");
            public static XName File = XName.Get("FileStore", "http://fogid.net/o/");
            public static XName Uri = XName.Get("uri", "http://fogid.net/o/");
            public static XName Docmetainfo = XName.Get("docmetainfo", "http://fogid.net/o/");
            public static XName Fordoc = XName.Get("forDocument", "http://fogid.net/o/");
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("usage: CassCorr [-tiff] [-ещечтото] config-file");
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
            //to_convert_tiff = true; // Буду конвертировать все встретившиеся тиффы
            to_convert_tiff = false; //  Не буду конвертировать тиффы

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

            // =====================  1 Цикл по кассетам ==================
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
                    bool to_save_fog = true; // Сохранять новый fog-документ

                    Console.Write($"fog {f_id.Substring(f_id.LastIndexOf('/'))} ");
                    // Заглядываем в fog, получаем версию, готовим чтение атрибутов
                    OneFog fog = new OneFog(f_id);
                    var attributes = fog.FogAttributes();
                    string fog_version = attributes.ContainsKey("version") ? attributes["version"] : "";
                    if (fog_version == "fogid-2024")
                    {
                        ismodern_fog_version = true;
                        to_save_fog = false; // Если будет изменение, то признак установится в true
                    }

                    // ============== Есть 4 варианта фога: ismodern_fog_version  iscurrent_fog ==================
                    // 1-й Если версия правильная и не iscurrent_fog, то его не обрабатываем поскольку фог корректировать не надо
                    if (ismodern_fog_version && !iscurrent_fog)
                    {
                        Console.WriteLine("unprocessed.");
                        continue; // !!!!!! ===>
                    }

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

                    // Словарь для хранения таблицы мультимедиа документов данного фога. Устанавливает 
                    // отношение uri-rec где rec - элемент FileStore. Типовой вариант, что один uri - одна запись. 
                    //Dictionary<string, XElement> mmfiles = new Dictionary<string, XElement>();

                    // Хеш-множество зафиксированных uri данного фога
                    HashSet<string> uries = new HashSet<string>();

                    // ============== Осталось 3 варианта фога  ==================
                    if (!ismodern_fog_version && !iscurrent_fog) // Просто копируются
                    {
                        to_save_fog = true; // Это преобразование старого в новое для неосновных фогов
                        foreach (XElement rec in fog.Records()) xsbor.Add(new XElement(rec)); // Сохраняем без изменений
                    }
                    else if (ismodern_fog_version && iscurrent_fog)
                    {
                        // Это уже окончательный вид фога, теперь его изменение происходит при изменении файлов или превьюшек
                        to_save_fog = false; // Если не будет изменений, то сохранять не надо (уже модерн)
                        foreach (XElement rec in fog.Records())
                        {
                            if (rec.Name == XNms.File)
                            {
                                string? uri = rec.Element(XNms.Uri)?.Value;
                                string? docmetainfo0 = rec.Element(XNms.Docmetainfo)?.Value; // Это для взятия начальных значени полей
                                string? docmetainfo = null; // А это для результата
                                // Я буду формировать поля docmetainfo, а потом формировать и вставляь его  в новую запись
                                var id_att = rec.Attribute(XNms.rdfabout);
                                if (uri != null && !uries.Contains(uri) && id_att != null) // дубли не допустимы
                                {
                                    // Находим файл документа, проверяем что там за документ
                                    FileInfo? fileinfo = GetFileInfo(casspath, uri);
                                    if (fileinfo != null)
                                    {
                                        string docmeta = "";
                                        // Будем формировать атрибуты (метаданные) документа
                                        string ext = fileinfo.Extension;
                                        DateTime writeTime = fileinfo.LastWriteTime; // Возможно, будет корректироваться позже
                                        int width = -1; int height = -1; // Будут вычислены позже
                                        long size = fileinfo.Length;
                                        string? originalname = null; 
                                        // Берем из предыдущего docmetainfo
                                        if (docmetainfo0 != null)
                                        {
                                            var opair = docmetainfo0.Split(';')
                                                .FirstOrDefault(pair => pair.StartsWith("originalname:"));
                                            if (opair != null) originalname = opair.Substring("originalname:".Length);
                                        }
                                        // Определим MIME тип документа
                                        string mime = "";
                                        if (ext == ".jpg" || ext == ".jpeg") mime = "image/jpeg";
                                        else if (ext == ".tif" || ext == ".tiff") mime = "image/tiff";
                                        else if (ext == ".png") mime = "image/png";
                                        else if (ext == ".gif") mime = "image/gif";
                                        else if (ext == ".bmp") mime = "image/bmp";
                                        else if (ext == ".svg") mime = "image/svg+xml";
                                        else if (ext == ".mp4") mime = "video/mp4";
                                        else if (ext == ".avi") mime = "video/x-msvideo";
                                        else if (ext == ".mpeg") mime = "video/mpeg";
                                        else if (ext == ".mp3") mime = "audio/mpeg";
                                        else if (ext == ".aac") mime = "audio/aac";
                                        else if (ext == ".wav") mime = "audio/wav";
                                        else if (ext == ".pdf") mime = "application/pdf";
                                        else if (ext == ".xml") mime = "application/xml";
                                        else if (ext == ".fog") mime = "application/fog";
                                        else
                                        {
                                            Console.WriteLine($"err: unknown mime type for {ext} in {rec.Name}");
                                        }

                                        docmeta = "";

                                        uries.Add(uri); // чтобы потом повторно не обрабатывать файл

                                        // Собираем метаинформацию
                                        docmeta =
                                            "documenttype:" + mime +
                                            ";writeTime:" + writeTime +
                                            ";width:" + width +
                                            ";height:" + height +
                                            ";size:" + size +
                                            ";originalname:" + (originalname ?? "") +
                                            ";";

                                        XElement rec1 = new XElement(XNms.File, new XAttribute(id_att),
                                            new XElement(XNms.Docmetainfo, docmeta),
                                            rec.Elements()
                                                .Where(el => el.Name != XNms.Docmetainfo)
                                                .Select(el => new XElement(el)));
                                        xsbor.Add(new XElement(rec1));
                                        to_save_fog = true;
                                    }
                                    else xsbor.Add(new XElement(rec));

                                }
                                else
                                {
                                    xsbor.Add(new XElement(rec));
                                }
                            }
                            else xsbor.Add(new XElement(rec)); // Сохраняем без изменений
                        }
                    }
                    else if (!ismodern_fog_version && iscurrent_fog)
                    {
                        // Перевод из старого формата в новый и отработка манипуляций с файлами, делается на документах,
                        // результаты элементы FileStore
                        to_save_fog = true; // Надо сделать модерн
                        foreach (XElement rec in fog.Records())
                        {
                            if ((new XName[] { XNms.Photo, XNms.Video, XNms.Audio, XNms.Document }).Contains(rec.Name))
                            {
                                //ConvertDocument(casspath, xsbor, mmfiles, rec);
                            }
                            else
                            {
                                xsbor.Add(new XElement(rec)); // Сохраняем без изменений
                            }
                        }
                        Console.Write("ft ");
                    }
                    else { Console.WriteLine($"Assert: strange variant {ismodern_fog_version} {iscurrent_fog}"); }

                    Console.WriteLine();
                    fog.Close();

                    if (to_save_fog)
                    {
                        Console.WriteLine($"Saving of document " + f_id);
                        xsbor.Save(f_id + ".tmp");
                        if (File.Exists(f_id + ".old")) File.Delete(f_id + ".old");
                        System.IO.File.Move(f_id, f_id + ".old");
                        System.IO.File.Move(f_id + ".tmp", f_id);
                    }
                }
            }
        }

        private static FileInfo? GetFileInfo(string casspath, string uri)
        {
            string last9 = uri.Substring(uri.Length - 9);
            var pth = new DirectoryInfo(casspath + "/originals/" + last9.Substring(0, 4));
            var ff = pth.GetFiles(last9.Substring(5) + ".*");
            var files = ff.Where(f => !f.Name.EndsWith(".old"));
            int fc = files.Count();
            if (fc != 1)
            {
                throw new Exception($"Err: {fc} file of {uri}");
            }
            return ff[0];
        }

        // Процедура конвертирования записи документа. На вход подаются константы имен, путь к кассете, собирательный xsbor,
        // словарь мультимедиа файлов данной кассеты и, главное, анализируемая и преобразуемая запись.
        // В результате, в xsbor будет помещена копия rec, возможно с изъятием docmetainfo и если в словаре uri нет, то  
        // словарь пополнится элементом rec под именем uri, а в xsbor добавится еще и новый FileStore.  
        private static void ConvertDocument(string casspath, XElement xsbor, Dictionary<string, XElement> mmfiles, XElement rec)
        {
            // Добываем uri и  docmetainfo
            string? ur = rec.Element(XNms.Uri)?.Value;
            string? dmi = rec.Element(XNms.Docmetainfo)?.Value;
            if (ur == null)
            {
                xsbor.Add(new XElement(rec)); // Сохраняем без изменений
            }
            else
            {
                XAttribute? id_att = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about");
                if (id_att != null)
                {
                    XElement rec1 = new XElement(rec.Name,
                        new XAttribute(id_att),
                        rec.Elements()
                        .Where(x => x.Name != XNms.Docmetainfo)
                        .Select(x => new XElement(x)));
                    xsbor.Add(new XElement(rec1)); // Сохраняем измененный
                    XElement felement = new XElement(XNms.File,
                        new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id_att.Value + "_"),
                        new XElement(XNms.Fordoc, new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", id_att.Value)),
                        new XElement(XNms.Uri, new XText(ur)),
                        dmi == null ? null : new XElement(XNms.Docmetainfo, new XText(dmi)));
                    xsbor.Add(felement);
                    // ==== Здесь поработаем с файлом, который находится по uri
                    ProcessFile(casspath, mmfiles, rec, ur, felement);
                }
            }
        }

        private static void ProcessFile(string casspath, Dictionary<string, XElement> mmfiles, XElement rec, string ur, XElement felement)
        {
            // Проверка однократности
            if (mmfiles.ContainsKey(ur))
            {
                Console.Write(" A ");
            }
            else // новый uri 
            {
                // Находим файл документа, проверяем что там за документ
                string last9 = ur.Substring(ur.Length - 9);
                var pth = new DirectoryInfo(casspath + "/originals/" + last9.Substring(0, 4));
                var ff = pth.GetFiles(last9.Substring(5) + "*");
                var files = ff.Where(f => !f.Name.EndsWith(".old"));
                int fc = files.Count();
                if (fc != 1)
                {
                    throw new Exception($"Err: {fc} file of {ur}");
                }
                // Определим extention
                var fileinfo = ff[0];
                string ext = fileinfo.Extension;

                // Будем формировать атрибуты (метаданные) документа
                DateTime writeTime = fileinfo.LastWriteTime; // Возможно, будет корректироваться позже
                int width = -1; int height = -1; // Будут вычислены позже
                long size = fileinfo.Length;

                // Определим MIME тип документа
                string mime = "";
                if (rec.Name == XNms.Photo)
                {
                    if (ext == ".jpg" || ext == ".jpeg") mime = "image/jpeg";
                    else if (ext == ".tif" || ext == ".tiff") mime = "image/tiff";
                    else if (ext == ".png") mime = "image/png";
                    else if (ext == ".gif") mime = "image/gif";
                    else if (ext == ".bmp") mime = "image/bmp";
                    else if (ext == ".svg") mime = "image/svg+xml";
                }
                else if (rec.Name == XNms.Video)
                {
                    if (ext == ".mp4") mime = "video/mp4";
                    else if (ext == ".avi") mime = "video/x-msvideo";
                    else if (ext == ".mpeg") mime = "video/mpeg";
                }
                else if (rec.Name == XNms.Audio)
                {
                    if (ext == ".mp3") mime = "audio/mpeg";
                    else if (ext == ".aac") mime = "audio/aac";
                    else if (ext == ".wav") mime = "audio/wav";
                }
                else if (rec.Name == XNms.Document)
                {
                    if (ext == ".pdf") mime = "application/pdf";
                    else if (ext == ".xml") mime = "application/xml";
                }
                else
                {
                    Console.WriteLine($"err: unknown mime type for {ext} in {rec.Name}");
                }
                Console.Write($"_");

                mmfiles.Add(ur, felement);
            }
        }
    }
}