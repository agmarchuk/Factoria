using OAData.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FactographData
{
    public interface IFDataService
    {
        IOntology ontology { get; set; }
        void Init(string connectionstring);
        void Close();
        void Reload();
        // ============== Основные методы доступа к БД =============
        IEnumerable<XElement> SearchByName(string searchstring);
        IEnumerable<XElement> SearchByWords(string searchwords);
        XElement GetItemByIdBasic(string id, bool addinverse);
        XElement GetItemById(string id, XElement format);
        IEnumerable<XElement> GetAll();

        DAdapter GetAdapter();

        // ============== Загрузка базы данных ===============
        //void StartFillDb(Action<string> turlog);
        //void LoadXFlow(IEnumerable<XElement> xflow, Dictionary<string, string> orig_ids);
        //void FinishFillDb(Action<string> turlog);

        // ============== Редктирование ===============
        XElement UpdateItem(XElement item);
        XElement PutItem(XElement item);

        // ============== Работа с файлами и кассетами ================
        string CassDirPath(string uri);
        string GetFilePath(string u, string s);
        bool HasWritabeFogForUser(string? user);
        OAData.Adapters.CassInfo[] Cassettes { get; }

        // ============ Билдеры =============
        TRecordBuilder TBuilder { get; }

        // ============ Работа с RRecord - могут (должны) быть переопределены ===========
        RRecord? GetRRecord(string id, bool addinverse)
        {
            XElement xrec = GetItemByIdBasic(id, true);
            if (xrec != null && xrec.Attribute("id")!= null && xrec.Attribute("type") != null)
            {
                RRecord rr = new RRecord
                {
                    Id = xrec.Attribute("id").Value,
                    Tp = xrec.Attribute("type").Value,
                    Props = xrec.Elements()
                        .Select<XElement, RProperty?>(p =>
                        {
                            string? pred = p.Attribute("prop")?.Value;
                            if (pred == null) return null;
                            if (p.Name == "field")
                            {
                                XAttribute? la = p.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                                return new RField { Prop = pred, Value = p.Value, 
                                    Lang = (la == null ? "" : la.Value) };
                            }
                            else if (p.Name == "direct")
                            {
                                return new RLink { Prop = pred, Resource = p.Element("record").Attribute("id").Value };
                                //return new RDirect { Prop = pred, DRec = }
                            }
                            if (p.Name == "inverse")
                            {
                                return new RInverseLink { Prop = pred, Source = p.Element("record").Attribute("id").Value };
                            }
                            else return null;
                        })
                        .Where(ob => ob != null)
                        .Cast<RProperty>()
                        .ToArray()
                };
                return rr;
            };
            return null;
        }
        IEnumerable<RRecord> SearchRRecords(string sample, bool bywords)
        {
            var xrecs = bywords ? SearchByWords(sample) : SearchByName(sample);
            foreach (var xrec in xrecs)
            {
                RRecord rr = new RRecord
                {
                    Id = xrec.Attribute("id").Value,
                    Tp = xrec.Attribute("type").Value,
                    Props = xrec.Elements()
                        .Select<XElement, RProperty?>(p =>
                        {
                            string? pred = p.Attribute("prop")?.Value;
                            if (pred == null) return null;
                            if (p.Name == "field")
                            {
                                return new RField { Prop = pred, Value = p.Value, Lang = "ru" };
                            }
                            else if (p.Name == "direct")
                            {
                                return new RLink { Prop = pred, Resource = p.Element("record").Attribute("id").Value };
                                //return new RDirect { Prop = pred, DRec = }
                            }
                            if (p.Name == "inverse")
                            {
                                return new RInverseLink { Prop = pred, Source = p.Element("record").Attribute("id").Value };
                            }
                            else return null;
                        })
                        .Where(ob => ob != null)
                        .Cast<RProperty>()
                        .ToArray()
                };
                yield return rr;
            }
        }
    }
}
