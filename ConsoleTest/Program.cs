using FactographData;
using ConsoleTest;

struct Person
{
    public int id;
    public string name;
    public int age;
}
partial class Program
{
    public static void Main()
    {
        Console.WriteLine("Start serialization tests");
        int npersons = 1_000_000;

        IEnumerable<Person> persons = Enumerable.Range(0, npersons)
            .Select(i => new Person { id = i, name = "" + i, age = 33 });

        FileStream fs = new FileStream(@"C:\Home\data\serial1.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);
        BinaryReader br = new BinaryReader(fs);
        BinaryWriter bw = new BinaryWriter(fs);

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Restart();
        Dictionary<int, long> keyOffsets = new Dictionary<int, long>();
        foreach (Person person in persons)
        {
            long offset = fs.Position;
            keyOffsets.Add(person.id, offset);

            bw.Write(person.id);
            bw.Write(person.name);
            bw.Write(person.age);
        }
        sw.Stop();
        bw.Flush();
        Console.WriteLine(sw.ElapsedMilliseconds + $" ms. for {npersons} records");
        // 22 ms. for 100_000 records

        int nprobes = 10_000;
        Random rnd = new Random();


        int key = npersons * 2 / 3;
        sw.Restart();
        for (int i = 0; i < nprobes; i++)
        {
            key = rnd.Next(npersons);
            long off = keyOffsets[key];
            fs.Position = off;
            int id = br.ReadInt32();
            string name = br.ReadString();
            int age = br.ReadInt32();
        }

        sw.Stop();
        Console.WriteLine(sw.ElapsedMilliseconds + $" ms. for {nprobes} access tests");
        // 33 ms. for 10_000 access tests

        // Для 100_000_000 записей (16 Гб ОЗУ, использовано 5.2 Гб):
        // 21 сек. for 100_000_000 records
        // 76 ms. for 10_000 access tests

    }
    public static void Main1()
    {
        // See https://aka.ms/new-console-template for more information
        Console.WriteLine("Start ConsoleTest");
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        // Сделаю генерацию данных "Фототека" средствами RRecord
        int npersons = 100_000;
        int nfotos = npersons * 4;
        int nreflections = npersons * 8;

        IEnumerable<RRecord> persons = Enumerable.Range(0, npersons)
            .Select(i => new RRecord()
            {
                Id = "p" + i,
                Tp = "http://fogid.net/o/person",
                Props = new RProperty[]
                {
            new RField() { Prop= "http://fogid.net/o/name", Value = "Иван" + i, Lang = "ru"},
                }
            });
        IEnumerable<RRecord> fotos = Enumerable.Range(0, nfotos)
            .Select(i => new RRecord()
            {
                Id = "f" + i,
                Tp = "http://fogid.net/o/photo-doc",
                Props = new RProperty[]
                {
            new RField() { Prop= "http://fogid.net/o/name", Value = "DSP" + i, Lang = "ru"},
                }
            });
        Random rnd = new Random();
        IEnumerable<RRecord> reflections = Enumerable.Range(0, nreflections)
            .Select(i => new RRecord()
            {
                Id = "r" + i,
                Tp = "http://fogid.net/o/reflection",
                Props = new RProperty[]
                {
            new RLink() { Prop= "http://fogid.net/o/reflected",
                Resource = "p" + rnd.Next(npersons)},
            new RLink() { Prop= "http://fogid.net/o/in-doc",
                Resource = "f" + rnd.Next(nfotos)},
                }
            });

        // Создаю словарь обратных отношений: id -> RRecord[] - множество записей,
        // ссылающихся на данный идентификатор
        sw.Restart();
        Dictionary<string, Tuple<string, string>[]> dic_inverse = new Dictionary<string, Tuple<string, string>[]>(); ;
        Action Mk_dic_inverse = () =>
        {
            dic_inverse = persons
            .Concat(fotos)
            .Concat(reflections)
            .SelectMany(r =>
            {
                var triples = r.Props
                    .Where(p => p is RLink)
                    .Select(p => new Tuple<string, string, RRecord>
                        (((RLink)p).Resource, p.Prop, r));
                return triples;
            })
            .GroupBy(tri => tri.Item1)
            .ToDictionary(paa => paa.Key, paa => paa.Select(tri =>
                new Tuple<string, string>(tri.Item2, tri.Item3.Id)).ToArray());
        };
        Mk_dic_inverse();
        // Формирование главного словаря с записями, в которых есть обратные ссылки
        Dictionary<string, RRecord> dic = new Dictionary<string, RRecord>();
        Action Mk_dic = () =>
        {
            dic = persons
                .Concat(fotos)
                .Concat(reflections)
                .Select<RRecord, RRecord>(rec =>
                {
                    string id = rec.Id;
                    if (!dic_inverse.ContainsKey(id)) return rec;
                    var inverse = dic_inverse[id];
                    RRecord result = new RRecord
                    {
                        Id = id,
                        Tp = rec.Tp,
                        Props =
                        rec.Props.Concat(inverse.Select(pair =>
                            new RInverseLink { Prop = pair.Item1, Source = pair.Item2 })
                        ).ToArray()
                    };
                    return result;
                })
                .ToDictionary(rr => rr.Id);
        };
        Mk_dic();
        sw.Stop();
        Console.WriteLine("Load ok. Duration=" + sw.ElapsedMilliseconds);

        // Теперь определим функцию GetRecord c выходом в виде RRecord
        Func<string, RRecord> getRecord = id =>
        {
            return dic[id];
        };

        // Попробуем
        RRecord rec1 = getRecord("p" + 6);
        Console.WriteLine($"{rec1.Id} {rec1.Tp} {rec1.GetName()}");

        // Перехожу в проектирование RecBuilder...

        // Теперь пробую конструирование значения или шаблона
        Rec rec = new Rec("p2938", "http://fogid.net/o/person",
            new Tex("http://fogit.net/o/name",
                new TextLan("Привет", "ru"),
                new TextLan("Hello", "en")),
            new Str("http://fogid.net/o/from-date", "2009-11-12")
            );

        Rec shablon = new Rec(null, "http://fogid.net/o/person",
          new Tex("http://fogid.net/o/name"),
          new Inv("http://fogid.net/o/reflected",
            new Rec(null, "http://fogid.net/o/reflection",
                new Dir("http://fogid.net/o/in-doc",
                    new Rec(null, "http://fogid.net/o/document",
                      new Tex("http://fogid.net/o/name"),
                      new Str("http://fogid.net/o/from-date")),
                    new Rec(null, "http://fogid.net/o/photo-doc",
                      new Tex("http://fogid.net/o/name"),
                      new Str("http://fogid.net/o/from-date"),
                      new Str("http://fogid.net/o/uri"),
                      new Str("http://fogid.net/o/docmetainfo"))))
          ));

        string id = "p66";
        var record = dic[id];
        Console.WriteLine($"{record.Id} {record.Tp}");
        foreach (var p in record.Props)
        {
            Console.WriteLine("   RProperty");
        }

        var rbuilder = new RecBuilder(id => dic[id]);
        var item = rbuilder.ToRec(dic["p66"], shablon);

        Console.WriteLine($"{item.Id} {item.Tp}");
        foreach (var p in item.Props)
        {
            if (p is Tex)
            {
                var texts = (Tex)p;
                Console.Write("t(");
                foreach (var t in texts.Values)
                {
                    Console.Write($" {t.Text}^^{t.Lang}");
                }
                Console.WriteLine(")");
            }
            else if (p is Str)
            {
                var str = (Str)p;
                Console.Write($"\"{str.Value}\"");
            }
            else if (p is Dir)
            {
                var dir = (Dir)p;
                Console.Write($"dir({dir.Resources[0]})");
            }
            else if (p is Inv)
            {
                var inv = (Inv)p;
                Console.Write($"inv({inv.Pred})[");
                foreach (var q in inv.Sources)
                {
                    Console.Write($"{q.Tp}");
                }
                Console.WriteLine("]");
            }
        }
        Console.WriteLine();

        // Проведу нагрузочное тестирование, буду изучать скорость выполнения построений 
        // Сначала создам базу данных
        //npersons = 100_000;
        //nfotos = npersons * 4;
        //nreflections = npersons * 8;
        //Mk_dic_inverse();
        //Mk_dic();

        sw.Restart();
        for (int i = 0; i < 1000; i++)
        {
            string code = "p" + rnd.Next(npersons);
            var ite = rbuilder.ToRec(dic[code], shablon);
            //Console.WriteLine(ite.ToString());
        }
        sw.Stop();
        Console.WriteLine("Load ok. Duration=" + sw.ElapsedMilliseconds);

    }

    public static void Main0()
    {
        // See https://aka.ms/new-console-template for more information
        Console.WriteLine("Start ConsoleTest");
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        // Сделаю генерацию данных "Фототека" средствами RRecord
        int npersons = 100;
        int nfotos = npersons * 4;
        int nreflections = npersons * 8;

        IEnumerable<RRecord> persons = Enumerable.Range(0, npersons)
            .Select(i => new RRecord()
            {
                Id = "p" + i,
                Tp = "http://fogid.net/o/person",
                Props = new RProperty[]
                {
            new RField() { Prop= "http://fogid.net/o/name", Value = "Иван" + i, Lang = "ru"},
                }
            });
        IEnumerable<RRecord> fotos = Enumerable.Range(0, nfotos)
            .Select(i => new RRecord()
            {
                Id = "f" + i,
                Tp = "http://fogid.net/o/photo-doc",
                Props = new RProperty[]
                {
            new RField() { Prop= "http://fogid.net/o/name", Value = "DSP" + i, Lang = "ru"},
                }
            });
        Random rnd = new Random();
        IEnumerable<RRecord> reflections = Enumerable.Range(0, nreflections)
            .Select(i => new RRecord()
            {
                Id = "r" + i,
                Tp = "http://fogid.net/o/reflection",
                Props = new RProperty[]
                {
            new RLink() { Prop= "http://fogid.net/o/reflected",
                Resource = "p" + rnd.Next(npersons)},
            new RLink() { Prop= "http://fogid.net/o/in-doc",
                Resource = "f" + rnd.Next(nfotos)},
                }
            });

        // Создаю словарь обратных отношений: id -> RRecord[] - множество записей,
        // ссылающихся на данный идентификатор
        sw.Restart();
        Dictionary<string, Tuple<string, string>[]> dic_inverse = new Dictionary<string, Tuple<string, string>[]>(); ;
        Action Mk_dic_inverse = () =>
        {
            dic_inverse = persons
            .Concat(fotos)
            .Concat(reflections)
            .SelectMany(r =>
            {
                var triples = r.Props
                    .Where(p => p is RLink)
                    .Select(p => new Tuple<string, string, RRecord>
                        (((RLink)p).Resource, p.Prop, r));
                return triples;
            })
            .GroupBy(tri => tri.Item1)
            .ToDictionary(paa => paa.Key, paa => paa.Select(tri =>
                new Tuple<string, string>(tri.Item2, tri.Item3.Id)).ToArray());
        };
        Mk_dic_inverse();
        // Формирование главного словаря с записями, в которых есть обратные ссылки
        Dictionary<string, RRecord> dic = new Dictionary<string, RRecord>();
        Action Mk_dic = () =>
        {
            dic = persons
                .Concat(fotos)
                .Concat(reflections)
                .Select<RRecord, RRecord>(rec =>
                {
                    string id = rec.Id;
                    if (!dic_inverse.ContainsKey(id)) return rec;
                    var inverse = dic_inverse[id];
                    RRecord result = new RRecord
                    {
                        Id = id,
                        Tp = rec.Tp,
                        Props =
                        rec.Props.Concat(inverse.Select(pair =>
                            new RInverseLink { Prop = pair.Item1, Source = pair.Item2 })
                        ).ToArray()
                    };
                    return result;
                })
                .ToDictionary(rr => rr.Id);
        };
        Mk_dic();
        sw.Stop();
        Console.WriteLine("Load ok. Duration=" + sw.ElapsedMilliseconds);

        // Теперь определим функцию GetRecord c выходом в виде RRecord
        Func<string, RRecord> getRecord = id =>
        {
            return dic[id];
        };

        // Попробуем
        RRecord rec1 = getRecord("p" + 6);
        Console.WriteLine($"{rec1.Id} {rec1.Tp} {rec1.GetName()}");

        // Перехожу в проектирование RecBuilder...

        // Теперь пробую конструирование значения или шаблона
        Rec rec = new Rec("p2938", "http://fogid.net/o/person",
            new Tex("http://fogit.net/o/name",
                new TextLan("Привет", "ru"),
                new TextLan("Hello", "en")),
            new Str("http://fogid.net/o/from-date", "2009-11-12")
            );

        Rec shablon = new Rec(null, "http://fogid.net/o/person",
          new Tex("http://fogid.net/o/name"),
          new Inv("http://fogid.net/o/reflected",
            new Rec(null, "http://fogid.net/o/reflection",
                new Dir("http://fogid.net/o/in-doc",
                    new Rec(null, "http://fogid.net/o/document",
                      new Tex("http://fogid.net/o/name"),
                      new Str("http://fogid.net/o/from-date")),
                    new Rec(null, "http://fogid.net/o/photo-doc",
                      new Tex("http://fogid.net/o/name"),
                      new Str("http://fogid.net/o/from-date"),
                      new Str("http://fogid.net/o/uri"),
                      new Str("http://fogid.net/o/docmetainfo"))))
          ));

        string id = "p66";
        var record = dic[id];
        Console.WriteLine($"{record.Id} {record.Tp}");
        foreach (var p in record.Props)
        {
            Console.WriteLine("   RProperty");
        }

        var rbuilder = new RecBuilder(id => dic[id]);
        var item = rbuilder.ToRec(dic["p66"], shablon);

        Console.WriteLine($"{item.Id} {item.Tp}");
        foreach (var p in item.Props)
        {
            if (p is Tex)
            {
                var texts = (Tex)p;
                Console.Write("t(");
                foreach (var t in texts.Values)
                {
                    Console.Write($" {t.Text}^^{t.Lang}");
                }
                Console.WriteLine(")");
            }
            else if (p is Str)
            {
                var str = (Str)p;
                Console.Write($"\"{str.Value}\"");
            }
            else if (p is Dir)
            {
                var dir = (Dir)p;
                Console.Write($"dir({dir.Resources[0]})");
            }
            else if (p is Inv)
            {
                var inv = (Inv)p;
                Console.Write($"inv({inv.Pred})[");
                foreach (var q in inv.Sources)
                {
                    Console.Write($"{q.Tp}");
                }
                Console.WriteLine("]");
            }
        }
        Console.WriteLine();

        // Проведу нагрузочное тестирование, буду изучать скорость выполнения построений 
        // Сначала создам базу данных
        //npersons = 100_000;
        //nfotos = npersons * 4;
        //nreflections = npersons * 8;
        //Mk_dic_inverse();
        //Mk_dic();

        sw.Restart();
        for (int i = 0; i < 1000; i++)
        {
            string code = "p" + rnd.Next(npersons);
            var ite = rbuilder.ToRec(dic[code], shablon);
            //Console.WriteLine(ite.ToString());
        }
        sw.Stop();
        Console.WriteLine("Load ok. Duration=" + sw.ElapsedMilliseconds);
    }

}