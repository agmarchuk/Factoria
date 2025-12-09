
using System.Xml.Linq;

namespace CUtils
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start CUtils");
            if (args.Length == 0 && false) 
            {
                Console.WriteLine("usage: CUtils command filein fileout [options]");
                Console.WriteLine("  filein - input; fileout - output;");
                Console.WriteLine("  command:");
                Console.WriteLine("    -fog");
                return;
            }
            //string command = args[0], filein = args[1], fileout = args[2];
            string command = "-fog",
                filein = "C:\\Home\\FactographProjects\\syp_cassettes\\SypCassete\\meta\\SypCassete_current.fog", 
                fileout = "fogout.fog";
            //command = "-last";
            command = "-log";

            if (command == "-fog")
            {
                var fog = new Factograph.Docs.OneFog(filein);
                int cnt = 0;
                foreach (var r in fog.Records())
                {
                    if (cnt % 100 == 0) Console.WriteLine(
                        r.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value);
                    cnt++;
                }
            }
            else if (command == "-log")
            {
                filein = "C:\\Home\\FactographDatabases\\Xu2_orig\\db_log.xml";
                System.IO.FileStream stream = File.OpenRead(filein);
                var xelem = XElement.Load(stream);

                // Формируем заготовку 
                string xdocroot0 = @"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'
xmlns='http://fogid.net/o/'>
</rdf:RDF>";
                XElement xsbor = XElement.Parse(xdocroot0);
                Dictionary<string, string> keyTimePairs = new Dictionary<string, string>();
                foreach (var x in xelem.Elements())
                {
                    string? sender = x.Attribute("sender")?.Value; if (sender != "pavl") continue;
                    XName xname = x.Name;
                    var about = x.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about");
                    string? id = about != null ? about.Value : x.Attribute("id")?.Value;
                    var mT_att = x.Attribute("mT");
                    string mT = mT_att != null ? mT_att.Value : "1900";
                    
                    if (id != null && keyTimePairs.TryGetValue(id, out var time))
                    {
                        if (string.Compare(time, mT) <= 0)
                        {
                            keyTimePairs[id] = mT;
                        }
                        else
                        {
                            keyTimePairs.Add(id, mT);
                        }
                    } else if (id != null)
                    {
                        keyTimePairs.Add(id, mT);
                    }
                }
                foreach (var x in xelem.Elements())
                {
                    string? sender = x.Attribute("sender")?.Value; if (sender != "pavl") continue;
                    XName xname = x.Name;
                    var about = x.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about");
                    string? id = about != null ? about.Value : x.Attribute("id")?.Value;
                    var mT_att = x.Attribute("mT");
                    string mT = mT_att != null ? mT_att.Value : "1900";
                    
                    if (id != null && keyTimePairs.TryGetValue(id, out var time))
                    {
                        if (string.Compare(time, mT) == 0)
                        {
                            xsbor.Add(new XElement(xname, 
                                about==null? new XAttribute("id", id) : about,
                                //mT != null ? new XAttribute("mT", mT) : null
                                x.Elements().Select(el =>
                                {
                                    XName ename = el.Name;
                                    XAttribute? resource = el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource");
                                    if (resource != null) 
                                    {
                                        return new XElement(ename, resource);
                                    }
                                    else
                                    {
                                        XAttribute? lang = el.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                                        return new XElement(ename,
                                            lang ?? null,
                                            el.Value);
                                    }
                                }),
                                null
                                ));
                        }
                    }
                }

                fileout = "C:\\Home\\FactographDatabases\\Xu2_orig\\xdb.xml";
                xsbor.Save(fileout);
            }
            else if (command == "-last")
            {
                int count = 0;
                string[] lines = new string[8];
                System.IO.TextReader reader = new System.IO.StreamReader(filein);
                string? line = "";
                while ((line = reader.ReadLine()) != null) { lines[count % 8] = line; count++; }
                Console.WriteLine($"count={count}");
                for (int i = 0; i < 8; i++)
                {
                    Console.WriteLine(lines[(count + i) % 8]);
                }
                reader.Close();
            }
            else if (command == "")
            {

            }
            else
            {
                Console.WriteLine("Err: no available command");
            }


        }
    }
}
