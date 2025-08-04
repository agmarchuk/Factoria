using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseStaticFiles();

Factograph.Data.IFDataService db = new Factograph.Data.FDataService();
string[] tips = ["http://fogid.net/o/person", "http://fogid.net/o/org-sys", "http://fogid.net/o/collection",
    "http://fogid.net/o/document"];

Dictionary<string, Rec>? shablons = tips.Select(t => Rec.GetUniShablon(t, 2, null, db.ontology))
    .ToDictionary(rec => rec.Tp);


app.MapGet("/", () => Results.Redirect("/view/cassetterootcollection")); //"/view/syp2001-p-marchuk_a"));
app.MapGet("/room216", () => { db.Reload(); Results.Redirect("/"); }); //"/view/syp2001-p-marchuk_a"));

// ���� view - �������� � �������. ����� �������������� �������� �������� id - ������������� ��������. ����� ����� ���� ���������:
// ss (search string) - ��������� �����; tp - ��� ���������� ������, IsBullOrEmpty(tp) - ��� ����; bw (by words) - ����� "�� ������";
// idd - ������������� "��������" ������, ����. ������������� "������������" ���������.
app.MapGet("/view/{id?}", (HttpRequest request, string? id) =>
{
    string? ss = request.Query["ss"].FirstOrDefault();
    string? tp = request.Query["tp"].FirstOrDefault();
    string? sbw = request.Query["bw"].FirstOrDefault(); // ������� bywords "�������"
    bool bywords = sbw != null ? true : false;
    string? idd = request.Query["idd"].FirstOrDefault();

    // �����, �� ��������� ����� �������������� �������� ��� ��� XElement ��� ��� �����, ������ ����� ����� ���� �������������,
    // � ����� ����� ��������� � �����. �����:
    string selectsbor = BuildSelectResults(db, ss, tp, bywords); // ������ ������ ����������� ������
    string portrait = BuildPortrait(db, id, shablons); // ������ ��������

    string maket = $@"<!DOCTYPE html>
<html>
    <head> 
        <meta charset='utf-8'> 
        <link href='/css/site.css' rel='stylesheet' />
    </head>
    <body>

    <div class='container' style='width:100%; background-color:lime;'>
        <a href='/view'> <img src='/img/logossyp.png' style='height:60px;margin-left: 16px;margin-right:16px;'> </a>
        <div style='display: flex; flex-direction: column;font-size:large;align-items:center;'>
            <div style='font-size:x-large;font-weight:bold;color:white;'>������ ����� ���� �������������</div>
            <div>����� 2025 ����: 14-27 ����</div>
        </div>
    </div>

    <div style='margin-top:20px;'>
        <form method='get' action=''>
            <input name ='ss' value='{ss}' />
            <select name ='tp' >
                <option value='' ></option>
                <option value='http://fogid.net/o/person' {(tp == "http://fogid.net/o/person" ? "selected" : "")}>�������</option>
                <option value='http://fogid.net/o/org-sys' {(tp == "http://fogid.net/o/org-sys" ? "selected" : "")}>���.����.</option>
                <option value='http://fogid.net/o/collection' {(tp == "http://fogid.net/o/collection" ? "selected" : "")}>���������</option>
            </select>
            <span>�������<input type='checkbox' name='bw' {(bywords ? "checked" : "")}  /></span>
            <input type='submit' value='������' />    
        </form>
    </div>

    <div style='margin: 20px 0px 20px 0px'>
        {selectsbor}
    </div>

    <div> 
        {portrait}
    </div>

    <footer>
        <hr/>
    </footer>
    </body>
</html>";
    return Results.Content(maket, "text/html");
});

app.MapGet("/photo", (HttpRequest request) =>
{
    string? uri = request.Query["uri"].FirstOrDefault();
    string? sz = request.Query["size"].FirstOrDefault();
    string s = sz ?? "small";
    string path = db.GetFilePath(uri, s);
    if (!System.IO.File.Exists(path + ".jpg"))
    {
        s = s == "medium" ? "normal" : "medium";
        path = db.GetFilePath(uri, s);
    }
    if (string.IsNullOrEmpty(path))
    {
        return Results.Empty; //new EmptyResult();
    }
    //return PhysicalFile(path + ".jpg", "image/jpg");
    return Results.File(path + ".jpg", "image/jpeg");
});

app.MapGet("/video", (HttpRequest request) =>
{
    string? uri = request.Query["uri"].FirstOrDefault();
    if (uri == null) return Results.Empty;
    string path = db.GetFilePath(uri, "medium");
    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path + ".mp4"))
    {
        return Results.Empty; //new EmptyResult();
    }
    return Results.File(path + ".mp4", "video/mp4");
});

app.MapGet("/document", (HttpRequest request) =>
{
    string? uri = request.Query["uri"].FirstOrDefault();
    if (uri == null) return Results.Empty;
    string? path = db.GetOriginalPath(uri);
    if (path == null) return Results.NotFound();
    int pos1 = path.LastIndexOfAny(new char[] { '/', '\\' }); // ������ ���������� �����
    DirectoryInfo di = new DirectoryInfo(path.Substring(0, pos1));
    string filefirst = path.Substring(pos1 + 1);
    var files = di.GetFiles(filefirst + ".*");

    if (files.Length == 1)
    {
        string fname = files[0].Name;
        int lastpoint = fname.LastIndexOf('.');
        if (lastpoint == -1) return Results.Empty;
        string ext = fname.Substring(lastpoint + 1);
        return Results.File(files[0].FullName, $"application/{ext}");
    }
    else return Results.Empty; // �� ����� �������, ����� ���� �������� � ����������� ����
});

app.Run();

// ===================== ��������� =======================
static Dictionary<string, string> Doctipnames()
{
    return new Tuple<string, string>[] 
    {
        new Tuple<string, string>("http://fogid.net/o/photo-doc", "����"),
        new Tuple<string, string>("http://fogid.net/o/video-doc", "�����"),
        new Tuple<string, string>("http://fogid.net/o/audio-doc", "�����"),
        new Tuple<string, string>("http://fogid.net/o/document", "���."),
        new Tuple<string, string>("http://fogid.net/o/cassette", "����."),
        new Tuple<string, string>("http://fogid.net/o/collection", "����."),
        new Tuple<string, string>("http://fogid.net/o/person", "����."),
        new Tuple<string, string>("http://fogid.net/o/orh-sys", "���."),
        new Tuple<string, string>("http://fogid.net/o/city", "���."),
    }.ToDictionary(pa => pa.Item1, pa => pa.Item2);
}

static string BuildSelectResults(IFDataService db, string? ss, string? tp, bool bywords)
{
    return !string.IsNullOrEmpty(ss) ? new XElement("div",
        db.SearchRRecords(ss, bywords).Select(rec =>
        {
            string typ = rec.Tp;
            if (!string.IsNullOrEmpty(tp) && tp != typ) { return null; }
            var date = rec.GetField("http://fogid.net/o/from-date");
            return new XElement("div",
                new XElement("span", db.ontology.LabelOfOnto(tp)),
                " ",
                new XElement("a", new XAttribute("href", "/view/" + rec.Id), rec.GetName()),
                (date != null && date.Length > 3 ? new XElement("span", " " + date.Substring(0, 4)) : null),
                null);
        }),
        new XElement("div", "== ����� �������� =="))
        .ToString() : "";
}
//

//static string HTMLSquare(string url, string href, string name, string text, string bs)
//{
//    // �������� bs: contain - max ��������, auto, cover
//    return 
// @$"<div class='square' style='background: url({url}) center no-repeat; background-size:{bs}'>
//        <div style='height:92%;'></div>
//        <div style='background-color:white;'>
//            <a href='{href}'>{name}</a>  {text}
//        </div>
//    </div>";
//}
static string Kvad(string ttip, string iid, string name, string uri, string dates)
{
    string url ="";
    if (ttip == "http://fogid.net/o/collection" || ttip == "http://fogid.net/o/cassette") url = "/img/collection_m.jpg";
    else if (ttip == "http://fogid.net/o/photo-doc") url = $"/photo?uri={uri}";
    else if (ttip == "http://fogid.net/o/video-doc") url = "/img/video_m.jpg";
    else if (ttip == "http://fogid.net/o/audio-doc") url = "/img/audio_m.jpg";
    else if (ttip == "http://fogid.net/o/document") url = "/img/document_m.jpg";
    var dic = Doctipnames();
    string tword = dic.ContainsKey(ttip) ? dic[ttip] : ttip;
    return
@$"<div style='background-color:#CCFFCC; width:200px;height:200px;margin:10px 10px 10px 10px;'>
<div class='square' style='background: url({url}) center no-repeat; background-size:{(ttip == "http://fogid.net/o/photo-doc" ? "contain" : "auto")};margin:15px 20px 0px 10px;'>
    <div style='background-color:white;width:50px;align-self:end;'>{tword}</div>
    <div style='height:84%;'></div>
    <div style='background-color:white;'>
      <a href='/view/{iid}'>{name}</a>  {dates}
    </div>
</div></div>";
}
// 97FF97 ffffd8
static string BuildPortrait(Factograph.Data.IFDataService db, string id, Dictionary<string, Rec>? shablons)
{
    // ����������� ��������
    StringBuilder portrait = new StringBuilder();
    if (id != null)
    {
        var rrec = db.GetRRecord(id, true);
        while (rrec != null) // ���� ������ ������� ��� ������ �� break
        {
            string? subtype = shablons.Keys.FirstOrDefault(k => db.ontology.DescendantsAndSelf(k).Any(tt => tt == rrec.Tp));

            if (subtype == null) break;

            Factograph.Data.r.Rec? tree = Factograph.Data.r.Rec.Build(rrec, shablons[subtype], db.ontology,
                idd => db.GetRRecord(idd, false));
            string tip = tree.Tp;

            // ======== ���������: ���� ������� �������� (�������� idd), ���� ������� ��������� 
            // ������� ���������
            var member_in_collections = tree.GetInverse("http://fogid.net/o/collection-item")
                .Select(me =>
                {
                    var coll = me.GetDirect("http://fogid.net/o/in-collection");
                    if (coll == null) return null;
                    string? name = coll.GetText("http://fogid.net/o/name");
                    string[] idna = new string[] { coll.Id ?? "", name ?? "noname" };
                    return idna;
                })
                .Where(x => x != null)
                .Select(idna =>
                {
                    return $"<a href='/view/{idna[0]}'>{idna[1]}</a>";
                })
                .Aggregate("", (sum, s) => sum + " " + s);
            if (!string.IsNullOrEmpty(member_in_collections)) portrait.Append($"<div>!========== ���������: {member_in_collections}</div>");


            // ������� ����: ��� � �������������, ������� ���� - name, ����� ����, ��������
            portrait.Append($@"<div><span style='background-color:lime;'>{db.ontology.LabelOfOnto(tree.Tp)}</span> {tree.Id}</div>
");
            // ������ ����� ������� ��� ������ �����
            if (tip == "http://fogid.net/o/person")
            {
                // ����
                portrait.Append($@"<div style='margin:10px 0px 10px 0px;'><span style='font-size:large; font-weight:bold; '>{tree.GetText("http://fogid.net/o/name")}</span> {tree.GetDates()} {tree.GetText("http://fogid.net/o/description")}
</div>");
                // �������
                var particip = tree.GetInverse("http://fogid.net/o/participant")
                    .Select(p =>
                    {
                        var dir = p.GetDirect("http://fogid.net/o/in-org");
                        if (dir == null) return "";
                        string? r = p.GetText("http://fogid.net/o/role");
                        string role = r == null ? "��������" : r;
                        string? orgname = dir.GetText("http://fogid.net/o/name");
                        string? orgcat = dir.GetText("http://fogid.net/o/org-category");
                        string? oc = dir.GetStr("http://fogid.net/o/org-classification");
                        string? oo = oc == null ? null : db.ontology.EnumValue("http://fogid.net/o/org-classificator", oc, "ru");
                        string orgvid = orgcat != null ? orgcat : (oo == null && oc != null ? oc : "���.");
                        string? dtpart = p.GetDates();
                        string? dates = dtpart != null ? dtpart : dir.GetDates();
                        return $"<div>{role} <span style='background-color:lime;'>{orgvid}</span> <a href='/view/{dir.Id}'> {orgname} </a>  <span style='font-size:smaller;'>{dates ?? ""}</span></div>";
                    }).Aggregate("", (sum, s) => sum + " " + s);
                portrait.Append($@"<div>{particip}</div>");
                // ������ ���������
                var reflect = tree.GetInverse("http://fogid.net/o/reflected")
                    .Select(p =>
                    {
                        var dir = p.GetDirect("http://fogid.net/o/in-doc");
                        if (dir == null) return new string[] { "", "", "", "" };
                        string doctyp = dir.Tp;
                        string? docname = dir.GetText("http://fogid.net/o/name");
                        string? uri = dir.GetStr("http://fogid.net/o/uri");
                        string? dates = dir.GetDates();
                        return new string[] { doctyp, docname ?? "", uri ?? "", dates ?? "", dir.Id ?? "" };
                    })
                    .OrderBy(dud => dud[3])
                    .Select(dud =>
                    {
                        if (db.ontology.DescendantsAndSelf("http://fogid.net/o/document").Contains(dud[0]))
                        {
                            //string ttip = (Doctipnames())[dud[0]];
                            string name = dud[1];
                            if (string.IsNullOrEmpty(name)) name = "noname";
                            if (name.Length > 24) name = name.Substring(0, 24);
                            return Kvad(dud[0], dud[4], name, dud[2], dud[3]);
                        }
                        else return "";
                    })
                    .Aggregate("", (sum, s) => sum + " " + s);
                portrait.Append($@"<div class='container'>{reflect}</div>");
            }
            else if (tip == "http://fogid.net/o/org-sys")
            {
                portrait.Append(tip);
            }
            else if (tip == "http://fogid.net/o/collection" || tip == "http://fogid.net/o/cassette")
            {
                // ����� ����� ����������� ���� � �������� ���������: collection-member (� ��� �������)
                //// ������� ������ ���������
                //var member_in_collections = tree.GetInverse("http://fogid.net/o/collection-item")
                //    .Select(me =>
                //    {
                //        var coll = me.GetDirect("http://fogid.net/o/in-collection");
                //        if (coll == null) return null;
                //        string? name = coll.GetText("http://fogid.net/o/name");
                //        string[] idna = new string[] { coll.Id ?? "", name ?? "noname" };
                //        return idna;
                //    })
                //    .Where(x => x != null)
                //    .Select(idna =>
                //    {
                //        return $"<a href='/view/{idna[0]}'>{idna[1]}</a>";
                //    })
                //    .Aggregate("", (sum, s) => sum + " " + s);
                //if (!string.IsNullOrEmpty(member_in_collections)) portrait.Append($"<div>� ����������: {member_in_collections}</div>");

                // �������� ���� ���������
                var elements_of_collection = tree.GetInverse("http://fogid.net/o/in-collection")
                    .Select(me =>
                    {
                        var ci = me.GetDirect("http://fogid.net/o/collection-item");
                        if (ci == null) return null;
                        string? name = ci.GetText("http://fogid.net/o/name");
                        string? uri = ci.GetStr("http://fogid.net/o/uri");
                        string? dates = ci.GetDates();
                        string url = "";
                        string fill = "auto";
                        
                        string[] idtna = new string[] { ci.Id ?? "", ci.Tp, name ?? "noname", uri, dates ?? "" };
                        return idtna;
                    })
                    .Where(x => x != null)
                    .Select(idtna =>
                    {

                        return idtna == null ? "" :
                        //$"<div><a href='/view/{idtna[0]}'>{idtna[2]}</a></div>";
                        //HTMLSquare(idtna[3], $"/view/{idtna[0]}", idtna[2], "addition", idtna[4]);
                        Kvad(idtna[1], idtna[0], idtna[2], idtna[3], idtna[4]);
                    })
                    .Aggregate("", (sum, s) => sum + " " + s);
                portrait.Append($"<div class='container'>{elements_of_collection}</div>");


            }
            else if (db.ontology.DescendantsAndSelf("http://fogid.net/o/document").Contains(tip))
            {
                // ����� ����� ����������� ���� � ��������� �������� ���������: reflection, collection-member

                //string tip = tree.Tp; // ��� ����
                string? name = tree.GetText("http://fogid.net/o/name");
                string? date = tree.GetStr("http://fogid.net/o/from-date");
                string? uri = tree.GetStr("http://fogid.net/o/uri");
                string docview = "<div>" + (tip == "http://fogid.net/o/photo-doc" ?
                    $"<img src='/photo?uri={uri}&size=medium'/>" : 
                    (tip == "http://fogid.net/o/video-doc" ?
                        $"<video src='/video?uri={uri}' controls/>" :
                        (tip == "http://fogid.net/o/audio-doc" ?
                            $"<audio src='/audio?uri={uri}' controls/>" :
                            (tip == "http://fogid.net/o/document" ?
                                $"<a href='/document?uri={uri}'>������ ��������</a>" :
                    $"����� ������ ���� ������� ��������� {tree.Id} ���� {tip}"))));
                portrait.Append(docview);
                portrait.Append($"<div><span style='font-size:small;'>{name}</span> {date}</div>");


                // ���������
                var reflections = tree.GetInverse("http://fogid.net/o/in-doc")
                    .Select(x =>
                    {
                        var pers = x.GetDirect("http://fogid.net/o/reflected");
                        if (pers == null) return null;
                        string[] nada = new string[] { pers.Id, pers.GetText("http://fogid.net/o/name") ?? "noname" };
                        return nada;
                    })
                    .Where(na => na != null)
                    //.OrderBy(na => na[1] ?? "")
                    .Select(na =>
                    {
                        return $"<div><a href='/view/{na[0]}'>{na[1]}</a></div>";
                    })
                    .Aggregate("", (sum, te) => sum + te);

                portrait.Append($"<div>{reflections}</div>");
            }
            break;
        }
    }
    return portrait.ToString();
}

