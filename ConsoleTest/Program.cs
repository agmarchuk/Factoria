using System.Collections.Generic;
using System.Linq;
using FactographData;
using ConsoleTest;
using static System.Net.Mime.MediaTypeNames;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Start ConsoleTest");

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

// Создаю словарь id -> RRecord
var dic = persons
    .Concat(fotos)
    .Concat(reflections)
    .ToDictionary(r => r.Id);

// Создаю словарь обратных отношений: id -> RRecord[] - множество записей,
// ссылающихся на данный идентификатор
var dic_inverse = persons
    .Concat(fotos)
    .Concat(reflections)
    .SelectMany(r =>
    {
        var pairs = r.Props
            .Where(p => p is RLink)
            .Select(p => new Tuple<string, RRecord>(((RLink)p).Resource, r));
        return pairs;
    })
    .GroupBy(pa => pa.Item1, pa => pa.Item2)
    .ToDictionary(paa => paa.Key, paa => paa.ToArray())
    ;
//foreach (var x in dic_inverse)
//{
//    Console.WriteLine(x.Key + " ");
//    foreach (var r in x.Value) Console.Write(" " + r.Id);
//    Console.WriteLine();
//}

// Теперь определим функцию GetRecord c выходом в виде RRecord
Func<string, RRecord> getRecord = id =>
{
    return dic[id];
};

// Попробуем
RRecord rec1 = getRecord("p" + 66);
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

var rbuilder = new RecBuilder(getRecord, null);
var item = rbuilder.ToRec("p66", shablon, null);