using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Numerics;
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
        // Параметры
        static bool to_convert_tiff = false;
        static bool to_compress_video = true;


        static string ext_bin_directory = @"D:\Home\bin\";

        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            Console.WriteLine("usage: CassCorr [-tiff] [-compress] config-file");
            // Директория с внешними запускаемыми программами
            var path_variants = new string[] { @"D:\Home\bin\", @"C:\Home\bin\", System.AppContext.BaseDirectory + "../../../ext_bin/" };
            foreach (var variant in path_variants)
            {
                ext_bin_directory = variant;
                if (File.Exists(ext_bin_directory + "ffmpeg.exe")) break;
            }


            string config_or_cass = System.AppContext.BaseDirectory + "../../../config.xml";
            string[] cassnames = new string[0];

            foreach (string arg in args)
            {
                if (arg == "-tiff")
                {
                    to_convert_tiff = true;
                }
                else if (arg == "-compress")
                {
                    to_compress_video = true;
                }
                else if (arg[0] != '-') // конфигуратор или кассета
                {
                    config_or_cass = arg;
                }
            }
            // == Силовая устанвока параметров
            //to_convert_tiff = true; // Буду конвертировать все встретившиеся тиффы
            //to_convert_tiff = false; //  Не буду конвертировать тиффы
            to_compress_video = true;

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
                    //finfo = XElement.Load(casspath + "/cassette.finfo"); // пока не читаем! TODO:
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

                    // Заглядываем в fog, получаем версию, готовим чтение атрибутов
                    OneFog fog = new OneFog(f_id);
                    var attributes = fog.FogAttributes();
                    string fog_version = attributes.ContainsKey("version") ? attributes["version"] : "";
                    if (fog_version == "fogid-2024")
                    {
                        ismodern_fog_version = true;
                        to_save_fog = false; // Если будет изменение, то признак установится в true
                    }

                    // Отдельно (?) вычисляем дату фога
                    DateTime fog_date = new FileInfo(f_id).LastWriteTime;

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
                            // ==== Здесь поработаем с файлом, который находится по uri
                            if ((new XName[] { XNms.Photo, XNms.Video, XNms.Audio, XNms.Document }).Contains(rec.Name))
                            {
                                string? ur = rec.Element(XName.Get("uri", XNms.fog))?.Value; 
                                if (ur != null && !uries.Contains(ur))
                                {
                                    bool originalchanged = ProcessFile(casspath, rec, ur, null, fog_date, finfo); // Без трансформации 
                                    if (originalchanged) to_save_fog = true;
                                    uries.Add(ur);
                                }
                            }
                            else {  }
                            xsbor.Add(new XElement(rec)); // Сохраняем без изменений
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
                                ConvertDocument(casspath, xsbor, uries, rec, fog_date, finfo);
                            }
                            else
                            {
                                xsbor.Add(new XElement(rec)); // Сохраняем без изменений
                            }
                        }
                    }
                    else { Console.WriteLine($"Assert: strange variant {ismodern_fog_version} {iscurrent_fog}"); }

                    fog.Close();

                    if (to_save_fog)
                    {
                        Console.WriteLine($"Saving of fog {f_id} ");
                        xsbor.Save(f_id + ".tmp");
                        if (File.Exists(f_id + ".old")) File.Delete(f_id + ".old");
                        System.IO.File.Move(f_id, f_id + ".old");
                        System.IO.File.Move(f_id + ".tmp", f_id);
                    }
                }
            }
        }
        // Процедура конвертирования записи документа. На вход подаются константы имен, путь к кассете, собирательный xsbor,
        // словарь мультимедиа файлов данной кассеты и, главное, анализируемая и преобразуемая запись.
        // В результате, в xsbor будет помещена копия rec, возможно с изъятием docmetainfo и если в словаре uri нет, то  
        // словарь пополнится элементом rec под именем uri, а в xsbor добавится еще и новый FileStore.
        // Результат true если оригинал преобразован
        private static bool ConvertDocument(string casspath, XElement xsbor, HashSet<string> uries, XElement rec, 
            DateTime fog_dt, XElement finfo)
        {
            bool originalchanged = false;
            // Добываем uri и  docmetainfo
            string? ur = rec.Element(XNms.Uri)?.Value;
            string? transform = rec.Element("{http://fogid.net/o/}transform")?.Value;
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
                        .Where(x => x.Name != "{http://fogid.net/o/}transform")
                        .Select(x => new XElement(x)));
                    xsbor.Add(new XElement(rec1)); // Сохраняем измененный
                   
                    if (!uries.Contains(ur))
                    {
                        originalchanged = ProcessFile(casspath, rec, ur, transform, fog_dt, finfo); 
                        uries.Add(ur);
                    }
                        
                }
            }
            return originalchanged;
        }
        // Результат true если оригинал изменен
        private static bool ProcessFile(string casspath, XElement rec, string ur, string? transform, 
            DateTime fog_dt, XElement finfo)
        {
            bool originaltransformed = false; // Если будет true, то в имени надо убрать .new 
            // Находим файл документа, проверяем что там за документ
            string last9 = ur.Substring(ur.Length - 9);
            var pth = new DirectoryInfo(casspath + "/originals/" + last9.Substring(0, 4));
            var ff = pth.GetFiles(last9.Substring(5) + "*");
            var files = ff.Where(f => !f.Name.EndsWith(".old") && !f.Name.EndsWith(".neworiginal.jpg"));
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
            int dur_mils = -1; // продолжительность в миллисекундах
            double file_size = 0.0;
            DateTime encoded_date = DateTime.Now;

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
                else if (ext == ".mts") mime = "video/mp2t";
                else if (ext == ".wmv") { mime = "video/x-ms-wmv"; }
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
                Console.WriteLine($"[err: unknown mime type for {ext} in {rec.Name}] ");
            }

            // == Поработаем над преобразованиями имиджа и вычисления превьюшек (только для битмапов) ==
            if (mime.StartsWith("image/") && mime != "image/svg+xml")
            {
                System.Drawing.Bitmap? bitmap = null;
                // Когда преобразование нужно делать?
                // 1) есть transform или
                // 2) дата имиджа больше даты фог-файла (ее еще надо иметь) или
                bool image_changed = fog_dt < fileinfo.LastWriteTime; 
                // 3) нет какой-нибудь превьюшки
                bool nsm = !File.Exists(casspath + "/documents/small/" + last9 + ".jpg");
                bool nme = !File.Exists(casspath + "/documents/medium/" + last9 + ".jpg");
                bool nno = !File.Exists(casspath + "/documents/normal/" + last9 + ".jpg");
                // 4) mime == "image/tiff"
                if (transform != null || image_changed || (nsm|nme|nno) ||
                    (to_convert_tiff && mime == "image/tiff"))
                {
                    string doc9 = ur.Substring(ur.Length - 9);
                    string oldname = casspath + "/originals/" + doc9 + ext;
                    string newname = casspath + "/originals/" + doc9 + ".neworiginal.jpg";
                    try
                    {
                        // Делаем битмап файл-документа! Заодно, скорректируем ширину и высоту
                        //bitmap = new System.Drawing.Bitmap(casspath + "/originals/" + doc9 + ext);
                        string fname = casspath + "/originals/" + doc9 + ext;
                        //var image = System.Drawing.Image.FromFile(fname);
                        //bitmap = new Bitmap(image);
                        bitmap = new Bitmap(fname);
                        width = bitmap.Width;
                        height = bitmap.Height;
                        Console.Write("b ");

                        // Возможно, надо повернуть
                        if (transform == "r") bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        else if (transform == "rr") bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        else if (transform == "rrr") bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);

                        // Будем вычислять и сохранять превьюшки
                        Action<string> mk_preview = (string sz) =>
                        {
                            int previewBase = Int32.Parse(finfo.Element("image")?.Element(sz)?.Attribute("previewBase")?.Value ?? "200");
                            // на фактор надо множить размеры фотки
                            double factor = width >= height ? (double)previewBase / (double)width : (double)previewBase / (double)height;
                            System.Drawing.Bitmap bm = new Bitmap(bitmap,
                                new Size((int)((double)width * factor), (int)((double)height * factor)));
                            bm.Save(casspath + "/documents/" + sz + "/" + doc9 + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            Console.Write(sz[0] + " ");
                        };

                        if (nsm) mk_preview("small");
                        if (nme) mk_preview("medium");
                        if (nno) mk_preview("normal");

                        // Будем сохранять оригинал при преобразовании и для специфического случая преобразования тиффа
                        if (image_changed || transform != null || mime == "image/tiff")
                        {
                            bitmap.Save(newname, System.Drawing.Imaging.ImageFormat.Jpeg);
                            originaltransformed = true;
                            Console.Write("ot ");
                            mk_preview("small");
                            mk_preview("medium");
                            mk_preview("normal");
                        }


                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine($"[Err: {ex.Message}] file {oldname} not changed"); 
                    }
                    
                    // Разрушим то, что должно быть разрушено
                    if (bitmap != null) bitmap.Dispose();
                    int pos = newname.LastIndexOf(".neworiginal.jpg");
                    if (originaltransformed) 
                    { 
                        File.Delete(oldname);
                        File.Move(newname, newname.Substring(0, pos) + ".jpg");
                    }
                    else if (pos >= 0)
                    {
                        File.Delete(newname);
                    }
                    Console.WriteLine();
                }
            }
            // == А теперь поработаем над преобразованиями видео и вычисления превьюшек ==
            else if (mime.StartsWith("video/"))
            {
                string doc9 = ur.Substring(ur.Length - 9);
                string oldname = casspath + "/originals/" + doc9 + ext;
                string newname = casspath + "/originals/" + doc9 + ".neworiginal.mp4";

                // Когда преобразование нужно делать?
                // 1) to_compress_video == true, тогда проверим, что оригинал сжат плохо
                // 2) дата имиджа больше даты фог-файла (ее еще надо иметь) или
                bool video_changed = fog_dt < fileinfo.LastWriteTime; 
                // 3) нет какой-нибудь нужной превьюшки
                XElement? fv = finfo.Element("video");
                string[] p_v = fv==null ? new string[0] : fv.Elements().Select(el => el.Name.LocalName).ToArray();
                bool nsm = p_v.Any(s =>s == "small") && !File.Exists(casspath + "/documents/small/" + last9 + ".mp4");
                bool nme = p_v.Any(s => s == "medium") && !File.Exists(casspath + "/documents/medium/" + last9 + ".mp4");
                bool nno = p_v.Any(s => s == "normal") && !File.Exists(casspath + "/documents/normal/" + last9 + ".mp4");              

                // Для указанных случаев вычисляем метаинформацию
                if (to_compress_video || video_changed || (nsm | nme | nno))
                {
                    // =============== Вычисление ширины и высоты через обращение к MediaInfo ===
                    XElement? xoutput = null;
                    using Process process1 = new Process();
                    {
                        process1.StartInfo.FileName = ext_bin_directory + @"MediaInfo.exe";
                        process1.StartInfo.WorkingDirectory = ext_bin_directory;
                        process1.StartInfo.ArgumentList.Add("--Output=XML");
                        process1.StartInfo.ArgumentList.Add(oldname);
                        process1.StartInfo.UseShellExecute = false;
                        process1.StartInfo.RedirectStandardOutput = true;
                        process1.StartInfo.RedirectStandardError = true;

                        process1.Start(); //запускаем процесс

                        string output = process1.StandardOutput.ReadToEnd();
                        xoutput = XElement.Parse(output);
                        process1.WaitForExit(); //ожидаем окончания работы приложения, чтобы очистить буфер
                    }
                    // В элементе xoutput (имя: Mediainfo/File) обработаем элементы: 
                    //<track type="General">
                    //  <Complete_name>C:\Home\FactographProjects\Cassette_20211014/originals/0001/0005.mp4</Complete_name>
                    //  <Format>MPEG-4</Format>
                    //  <Format_profile>Base Media / Version 2</Format_profile>
                    //  <Codec_ID>mp42</Codec_ID>
                    //  <File_size>68.9 MiB</File_size>
                    //  <Duration>12s 864ms</Duration>
                    //  <Overall_bit_rate>44.9 Mbps</Overall_bit_rate>
                    //  <Encoded_date>UTC 2021-01-01 08:38:56</Encoded_date>
                    //  <Tagged_date>UTC 2021-01-01 08:38:56</Tagged_date>
                    //</track>
                    //<track type="Video">
                    //  <ID>1</ID>
                    //  <Format>AVC</Format>
                    //  <Format_Info>Advanced Video Codec</Format_Info>
                    //  <Format_profile>Baseline@L4.0</Format_profile>
                    //  <Format_settings__CABAC>No</Format_settings__CABAC>
                    //  <Format_settings__ReFrames>2 frames</Format_settings__ReFrames>
                    //  <Codec_ID>avc1</Codec_ID>
                    //  <Codec_ID_Info>Advanced Video Coding</Codec_ID_Info>
                    //  <Duration>12s 800ms</Duration>
                    //  <Bit_rate_mode>Variable</Bit_rate_mode>
                    //  <Bit_rate>19.2 Mbps</Bit_rate>
                    //  <Width>1 920 pixels</Width>
                    //  <Height>1 080 pixels</Height>
                    //  <Display_aspect_ratio>16:9</Display_aspect_ratio>
                    //  <Frame_rate_mode>Constant</Frame_rate_mode>
                    //  <Frame_rate>30.000 fps</Frame_rate>
                    //  <Original_frame_rate>60.000 fps</Original_frame_rate>
                    //  <Color_space>YUV</Color_space>
                    //  <Chroma_subsampling>4:2:0</Chroma_subsampling>
                    //  <Bit_depth>8 bits</Bit_depth>
                    //  <Scan_type>Progressive</Scan_type>
                    //  <Bits__Pixel_Frame_>0.309</Bits__Pixel_Frame_>
                    //  <Stream_size>29.3 MiB (43%)</Stream_size>
                    //  <Encoded_date>UTC 2021-01-01 08:38:56</Encoded_date>
                    //  <Tagged_date>UTC 2021-01-01 08:38:56</Tagged_date>
                    //</track>                    

                    // Из General берем File_size, Duration, Encoded_date
                    // Из Mediainfo/File/Track type="Video" берем Width, Height
                    XElement? meta_gen = xoutput.Element("File")?
                        .Elements("track").FirstOrDefault(tr => tr.Attribute("type")?.Value == "General");
                    XElement? meta_vid = xoutput.Element("File")?
                        .Elements("track").FirstOrDefault(tr => tr.Attribute("type")?.Value == "Video");
                    if (meta_gen != null && meta_vid != null)
                    {
                        // Оприходуем width и height
                        var sw = meta_vid.Element("Width")?.Value?.Replace(" ", "");
                        if (sw != null) width = Int32.Parse(sw.Substring(0, sw.Length - "pixels".Length));
                        var sh = meta_vid.Element("Height")?.Value?.Replace(" ", "");
                        if (sh != null) height = Int32.Parse(sh[..^"pixels".Length]);
                        // теперь вычислим Duration
                        var sd = meta_gen.Element("Duration")?.Value?.Replace(" ", "");
                        if (sd != null)
                        {
                            dur_mils = 0; int h = 0, mn = 0, s = 0, ms = 0;
                            int pos = sd.IndexOf("h");
                            if (pos != -1)
                            {
                                h = Int32.Parse(sd.Substring(0, pos));
                                sd = sd.Substring(pos + 1);
                            }
                            pos = sd.IndexOf("mn");
                            if (pos != -1)
                            {
                                mn = Int32.Parse(sd.Substring(0, pos));
                                sd = sd.Substring(pos + 2);
                            }
                            pos = sd.IndexOf("s");
                            if (pos != -1)
                            {
                                s = Int32.Parse(sd.Substring(0, pos));
                                sd = sd.Substring(pos + 1);
                            }
                            pos = sd.IndexOf("ms");
                            if (pos != -1)
                            {
                                ms = Int32.Parse(sd.Substring(0, pos));
                                //sd = sd.Substring(pos + 2);
                            }
                            dur_mils = ms + (s + mn * 60 + h * 3600) * 1000;
                        }
                        // теперь вычислим Stream_size, Encoded_date
                        var ss = meta_gen.Element("File_size")?.Value?.Replace(" ", "");
                        if (ss != null)
                        {
                            int pos = ss.IndexOf("MiB");
                            if (pos != -1)
                            {
                                file_size = Double.Parse(ss.Substring(0, pos));
                            }
                            else 
                            {
                                pos = ss.IndexOf("GiB");
                                if (pos != -1)
                                {
                                    file_size = Double.Parse(ss.Substring(0, pos)) * 1000;
                                }
                                else
                                {
                                    file_size = size;
                                }
                            }
                            
                        }
                        var ed = meta_gen.Element("Encoded_date")?.Value; // можно и из meta_vid
                        if (ed != null)
                        {
                            encoded_date = DateTime.Parse(ed.Substring(4));
                        }
                        // =============== конец вычислений через обращение к MediaInfo ===
                    }
                }

                // преобразование оригинала возможно в случае, когда вычисденный bit-rate больше 4000 (можно будет уточнять)
                var bitrate = file_size * 8000000.0 / dur_mils * 1000.0; 
                if (to_compress_video && bitrate > 4000000.0) //TODO: порог может быть другим или записанным в finfo
                {
                    using Process process2 = new Process();
                    {
                        process2.StartInfo.FileName = ext_bin_directory + @"ffmpeg.exe";
                        process2.StartInfo.WorkingDirectory = ext_bin_directory;
                        process2.StartInfo.ArgumentList.Add("-i");
                        process2.StartInfo.ArgumentList.Add(oldname);
                        process2.StartInfo.ArgumentList.Add("-y");
                        
                        process2.StartInfo.ArgumentList.Add("-c:v");
                        process2.StartInfo.ArgumentList.Add("libx264");

                        process2.StartInfo.ArgumentList.Add("-c:v");
                        process2.StartInfo.ArgumentList.Add("libx264");
                        process2.StartInfo.ArgumentList.Add("-c:a");
                        process2.StartInfo.ArgumentList.Add(ext==".mts"? "libmp3lame" : "aac");

                        process2.StartInfo.ArgumentList.Add(newname);
                        
                        process2.Start(); //запускаем процесс

                        //string output = process2.StandardOutput.ReadToEnd();
                        //xoutput = XElement.Parse(output);
                        process2.WaitForExit(); //ожидаем окончания работы приложения, чтобы очистить буфер
                    }

                }
                // Процедура вычисления превьюшек
                Action<string, string, double> Mk_video_preview = (oname, nname, fact) =>
                {
                    using Process process3 = new Process();
                    {
                        process3.StartInfo.FileName = ext_bin_directory + @"ffmpeg.exe";
                        process3.StartInfo.WorkingDirectory = ext_bin_directory;
                        process3.StartInfo.ArgumentList.Add("-i");
                        process3.StartInfo.ArgumentList.Add(oname);
                        process3.StartInfo.ArgumentList.Add("-y");

                        process3.StartInfo.ArgumentList.Add("-c:v");
                        process3.StartInfo.ArgumentList.Add("libx264");
                        process3.StartInfo.ArgumentList.Add("-c:a");
                        process3.StartInfo.ArgumentList.Add(ext == ".mts" ? "libmp3lame" : "aac");

                        process3.StartInfo.ArgumentList.Add("-s");
                        int w = (int)(width * fact); if (w % 2 == 1) w += 1;
                        int h = (int)(height * fact); if (h % 2 == 1) h += 1;
                        process3.StartInfo.ArgumentList.Add(w + "x" + h);

                        process3.StartInfo.ArgumentList.Add(nname);

                        process3.Start(); //запускаем процесс

                        process3.WaitForExit(); //ожидаем окончания работы приложения, чтобы очистить буфер
                    }
                };
                // Делаем превьюшки для тех, которые нужны, но их нет
                int more = width > height ? width : height;
                if (nsm)
                {
                    string? pBs = finfo.Element("video")?.Element("small")?.Attribute("previewBase")?.Value;
                    double factor = pBs != null ? (double)Int32.Parse(pBs) / (double)more : -1;
                    Mk_video_preview(oldname, casspath + "/documents/small/" + last9 + ".mp4", factor);
                }
                if (nme)
                {
                    var a = finfo.Element("video");
                    var b = finfo.Element("video")?.Element("medium");
                    var c = finfo.Element("video")?.Element("medium")?.Attribute("previewBase");
                    var pBs = finfo.Element("video")?.Element("medium")?.Attribute("previewBase")?.Value;
                    //string? pBs = finfo.Element("video")?.Element("meduim")?.Attribute("previewBase")?.Value;
                    double factor = pBs != null ? (double)Int32.Parse(pBs) / (double)more : -1;
                    Mk_video_preview(oldname, casspath + "/documents/medium/" + last9 + ".mp4", factor);
                }
                if (nno)
                {
                    string? pBs = finfo.Element("video")?.Element("normal")?.Attribute("previewBase")?.Value;
                    double factor = pBs != null ? (double)Int32.Parse(pBs) / (double)more : -1;
                    Mk_video_preview(oldname, casspath + "/documents/normal/" + last9 + ".mp4", factor);
                }

                if (originaltransformed)
                {
                    File.Delete(oldname);
                    int pos = newname.LastIndexOf(".new");
                    File.Move(newname, newname.Substring(0, pos));
                }
                //Console.WriteLine();
            }
            return originaltransformed;
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


    }
}