using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace CassConsoleApp
{
    public class PhotoSource
    {
        public static void GetSource(string imageSource)
        {
            //Uri _source = new Uri(imageSource);
            //try
            //{
            //    _source = new Uri(imageSource);
            //}
            //catch (Exception exc)
            //{
            //    Console.WriteLine(exc.Message + " Не загрузился файл " + imageSource);
            //    return;
            //}

            //Image im = Image.FromFile(imageSource);
            //Bitmap bi = new Bitmap(im);
            //if (bi != null)
            //{
            //    var w = bi.Width;
            //    var h = bi.Height;
            //    var qq = bi.PropertyIdList;
            //}

            //System.Drawing.Bitmap bm = new System.Drawing.Bitmap(imageSource);
            //System.Drawing.Imaging.Metafile mf = new System.Drawing.Imaging.Metafile(imageSource);

            string ext = imageSource.Substring(imageSource.LastIndexOf('.')).ToLower();
            if (ext == ".jpg")
            {
                FileStream Foto = File.Open(imageSource, FileMode.Open, FileAccess.Read); // открыли файл по адресу s для чтения
                System.Drawing.Image im = System.Drawing.Bitmap.FromStream(Foto);
                string t1="", t2 = "", t3 = "", t4 = ""; // = DateTime.MinValue.ToShortTimeString();
                try
                {
                    var pr1 = im.GetPropertyItem(306); // TagDateTime
                    var pr2 = im.GetPropertyItem(36867); // TagExifDTOrig
                    t1 = Encoding.UTF8.GetString(pr1.Value);
                    t2 = Encoding.UTF8.GetString(pr2.Value);
                }
                catch (Exception) { Console.WriteLine($"no exif for {imageSource}"); }
                var w = im.Width;
                var h = im.Height;
                Console.WriteLine($"{imageSource} {w} {h} {t1} {t2}");
            }
            else 
            { 
            }

            //var fh = File.OpenHandle(imageSource);
            //var t = File.GetLastWriteTime(fh);
            //var ima = System.Drawing.Image.FromFile(imageSource);
            //var w = im.Width;
            //var h = im.Height;
            //Console.WriteLine($"{imageSource} {w} {h} {t}");
            //fh.Close();

            //using (FileStream fs = new FileStream(imageSource, FileMode.Create))
            //{
            //    var im = System.Drawing.Image.FromStream(fs);
            //    var w = im.Width;
            //    var h = im.Height;
            //    Console.WriteLine($"{w} {h} {t}");
            //}


            //var fa = File.GetAttributes(imageSource);



        }
    }
}
