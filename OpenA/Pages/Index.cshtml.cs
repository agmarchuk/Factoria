using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OpenA.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly Factograph.Data.IFDataService db;

        public readonly string[] tips = ["http://fogid.net/o/person", "http://fogid.net/o/org-sys", "http://fogid.net/o/collection",
        "http://fogid.net/o/document"];

        private Dictionary<string, Rec>? shablons = null;

        public string? id { get; set; }
        public string? ss { get; set; }
        public string? tp { get; set; }
        public string? idd { get; set; }

        public bool bywords = false;

        // Это будет результат поиска!
        public RRecord[] searchresults = new RRecord[0];

        public IndexModel(ILogger<IndexModel> logger, Factograph.Data.IFDataService db)
        {
            _logger = logger;
            this.db = db;
        }

        public void OnGet(string id, string idd)
        {
            // Здесь идет запуск построения портрета
        }
        public void OnPost()
        {
            // Здесь запрашивается поиск по имеющимся параметрам
            ss = this.Request.Form["ss"].FirstOrDefault();
            string? sv = this.Request.Form["sv"].FirstOrDefault();
            if (string.IsNullOrEmpty(sv)) tp = null;
            else tp = "http://fogid.net/o/" + sv;
            
            string? sbw = this.Request.Form["bw"].FirstOrDefault();
            bywords = sbw != null ? true : false;



            if (!string.IsNullOrEmpty(ss))
            {
                IEnumerable<RRecord> query = db.SearchRRecords(ss, bywords);
                // Если задан тип, то фильтруем
                if ( ! string.IsNullOrEmpty(tp))
                {
                    query = query.Where(r => r.Tp == tp);
                }
                // Получаем результат
                var sr = query
                    .Select(r => new Tuple<RRecord, string>(r, r.GetName())) // получаем поток пар запись-имя
                    .OrderBy(pa => pa.Item2) // сортируем по имени
                                             //.Select(pa => new Tuple<RRecord, string> (pa.Item1, pa.Item1.GetField("http://fogid.net/o/from-date") ?? "zz")) // Оставляем в потоке запись и дату
                    .ThenBy(pa => pa.Item1.GetField("http://fogid.net/o/from-date") ?? "zz") // Вторичная сортировка 
                    .Select(pa => pa.Item1)
                    .Take(100)
                    .ToArray();
                searchresults = sr;
            }
        }
    }
}
