﻿using Factograph.Data;
using System.Xml.Linq;

namespace Soran1957
{
    public class SborSoran1957
    {
        private static string[] typs = new string[0];
        public static HtmlResult CreateHtml(Factograph.Data.IFDataService db, HttpRequest request)
        {
            var id = request.Query["id"];
            string? searchstring = request.Query["searchstring"];
            string? typ = request.Query["typ"];
            bool bywords = string.IsNullOrEmpty(request.Query["bywords"]) ? false : true;
            if (typs.Length == 0) typs = db.ontology.DescendantsAndSelf("http://fogid.net/o/sys-obj").ToArray();

            RRecord[] searchvariants = new RRecord[0];
            if (!string.IsNullOrEmpty(searchstring))
            {
                searchvariants = db.SearchRRecords(searchstring, false).ToArray();
            }
            RRecord? record = null;
            XElement? xportrait = null;
            if (!string.IsNullOrEmpty(id))
            {
                record = db.GetRRecord(id, true);
                if (record != null) { xportrait = BuildXPortrait(record, db); }
            }

            string options = typs.Select(t => "<option value='" + t + "' " + (t == typ ? "selected " : "") + ">" + db.ontology.LabelOfOnto(t) + "</option>")
                .Aggregate((s, el) => s + " " + el);
            string chked = bywords ? "checked" : "";
            var variants = new XElement("div", searchvariants.Select(variant => new XElement("div",
                        db.ontology.LabelOfOnto(variant.Tp),
                        new XText(" "),
                        new XElement("a", new XAttribute("href", "?id=" + variant.Id), variant.GetName())
                        )));


            // Поработаю с направлением поиска 
            string? sdir = null;

            string html =
$@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <link rel='stylesheet' href='PublicuemCommon/soran1957styles/zh.css' type='text/css' />
    <link rel='stylesheet' type='text/css' href='PublicuemCommon/Styles.css' />
    <script language='javascript' type='text/JScript' src='PublicuemCommon/PublicuemScript.js'>.</script>
</head>
<body>
    <table width='100%' style='table-layout:fixed;'>
        <col width='150' />
        <col width='100%' />
        <col width='410' />
        <tr valign='top'>
            <td />
            <td>
                <div id='logo' style='' width='500' height='99'>
                    <a href=''>
                        <img src='PublicuemCommon/soran1957styles/img/logo2.gif' width='391' height='61' vspace='12' alt='СО РАН с 1957 года' border='0' />
                    </a>
                </div>
            </td>
            <td>
                <div id='login' style=''>
                    <table cellpadding='0' cellspacing='0' border='0' width='100%'>
                        <tr>
                            <td class='text_arial_small'>имя: </td>
                            <td width='40%'><input type='text' class='form_login' /></td>
                            <td class='text_arial_small'>пароль: </td>
                            <td width='40%'><input type='password' class='form_login' /></td>
                            <td><input type='image' width='19' height='19' src='PublicuemCommon/soran1957styles/img/btn_login.gif' /></td>
                        </tr>
                        <tr>
                            <td />
                            <td colspan='4' class='text_arial_small'><a>регистрация</a> | <a>забыли пароль?</a></td>
                        </tr>
                    </table>
                </div>
            </td>
        </tr>
        <tr valign='top'>
            <td rowspan='2'>
                <div id='menu' style=''>
                    <a href='?id=w20070417_7_1744'>
                        <img src='PublicuemCommon/soran1957styles/img/i_home.gif' width='10' height='10' hspace='4' border='0' alt='Главная страница' />
                    </a>
                    <a href='mailto:pavl@iis.nsk.su?subject=soran1957.ru%20 w20070417_7_1744'>
                        <img src='PublicuemCommon/soran1957styles/img/i_mail.gif' width='10' height='10' hspace='11' border='0' alt='Написать письмо' />
                    </a>
                    <br />
                    <hr noshade='1' size='1' color='#cccccc' />
                    <div class='text_menu'>
                        <div class='p_menu'>
                            <a href='?id=w20070417_7_1744'> <b>Начало</b> </a>
                            <hr noshade='1' size='3' color='#cccccc' />
                            <a href='?id=w20070417_3_6186'><b>СО РАН</b></a>
                            <hr noshade='1' size='3' color='#cccccc' />
                            <a href='?id=w20070417_4_1010'><b>Первичные фотоматериалы</b></a>
                            <hr noshade='1' size='3' color='#cccccc' />
                            <a href='?id=newspaper_cassetteId'><b>Архив газет 'За науку в Сибири'</b></a>
                            <hr noshade='1' size='3' color='#cccccc' />
                            <a href='?id=w20070417_3_4658'><b>О проекте</b></a>
                            <hr noshade='1' size='3' color='#cccccc' />
                            <a href='?id=usefullLinks'><b>Полезные ссылки</b></a>
                        </div>
                    </div>
                    <hr noshade='1' size='3' color='#cccccc' />
                </div>
            </td>
            <td>
                <div id='search' style=''>
                    <form action=''>
                        <input type='hidden' name='p' value='typed-item-list' />
                        <input type='hidden' name='ch' value='search-form' />
                        <table cellpadding='0' cellspacing='0' border='0'>
                            <tr>
                                <td class='search_option'><a href='?sdirection=photo-doc'>Фотографии</a></td>
                                <td class='search_option_s'><a>Персоны</a></td>
                                <td class='search_option'><a href='?sdirection=org-sys'>Организации</a></td>
                            </tr>
                        </table>
                        <table cellpadding='0' cellspacing='0' border='0' width='100%' height='55'>
                            <tr>
                                <td width='100%' class='search_form'><input name='searchstring' type='text' class='form_search' /></td>
                                <td class='search_form'>
                                    <input type='image' width='63' height='19' src='PublicuemCommon/soran1957styles/img/btn_search.gif' style='margin-right:5px;' />
                                </td>
                            </tr>
                            <tr valign='top'>
                                <td colspan='2' class='search_form'>
                                    <input type='checkbox' id='search_all' disabled='disabled' style='border:0px solid #036;' />
                                    <label for='search_all'>искать по отдельным словам</label> | <a>расширенный поиск</a>
                                </td>
                            </tr>
                        </table>
                    </form>
                </div>
            </td>
            <td />
        </tr>
        <tr valign='top'>
            <td>
                <div>
                    <div><span style='text-align:center;' /></div>
                    <div>
                        <div ID='ListPanel'>
                            <div style='float:left;width:126px;height:170px;font-size:10px;'>
                                <table>
                                    <tr>
                                        <td>
                                            <a href='?id=PA_folders01-20_0001_0025'>
                                                <img style='border:0;' src='docs?s=small&amp;u=iiss://PA_folders01-20@iis.nsk.su/0001/0001/0025' />
                                            </a>
                                        </td>
                                        <tr>
                                            <td style='text-align:center;'><span style='text-align:center;'><span>1962</span>, <span>Новосибирск</span></span></td>
                                        </tr>
                                    </tr>
                                </table>
                            </div>
                            <div style='float:left;width:126px;height:170px;font-size:10px;'>
                                <table>
                                    <tr>
                                        <td>
                                            <a href='?id=krai_100616111436_2771_0'>
                                                <img style='border:0;' src='docs?s=small&amp;u=iiss://PA_folders01-20@iis.nsk.su/0001/0001/0496' />
                                            </a>
                                        </td>
                                        <tr>
                                            <td style='text-align:center;'>
                                                <span style='text-align:center;'><span>2005сен</span>, <span>Москва</span></span>
                                            </td>
                                        </tr>
                                    </tr>
                                </table>
                            </div>
                        </div>
                    </div>
                    <div><hr /></div>
                </div>
            </td>
            <td>
                <div>
                    <div />
                    <div />
                    <div />
                    <div />
                    <img src='docs?s=small&amp;u=iiss://PA_folders24-59@iis.nsk.su/0001/0004/0392' align='right' />
Проект посвящен истории Сибирского отделения Российской Академии Наук. Здесь можно найти уникальные фотографии, информацию о людях и организациях и статьи, касающиеся разных периодов жизни Сибирского отделения.
                    <h2>Коллекции сайта</h2>
                    <ul>
                        <li><a href='?id=PA_Users_furs_634395045531406250_2577'>Первопоселенцы Золотой долины</a></li>
                        <li><a href='?id=w20070417_3_7246'>Новосибирский Академгородок осенью</a></li>
                        <li><a href='?id=w20071113_9_8252'>Досуг в Новосибирском Академгородке</a></li>
                        <li><a href='?id=w20071113_9_11681'>Президенты АН и Председатели СО РАН </a></li>
                        <li><a href='?id=w20070417_3_3211'>Отцы-основатели</a></li>
                        <li><a href='?id=w20071113_9_3091'>Ветераны Великой Отечественной войны</a></li>
                        <li><a href='?id=w20070417_7_1735'>Лики науки</a></li>
                        <li><a href='?id=w20070417_3_7152'>Новосибирский Академгородок зимой</a></li>
                        <li><a href='?id=w20071113_9_12201'>Виды Новосибирского Академгородка</a></li>
                        <li><a href='?id=w20070417_3_7263'>Панорамные виды Новосибирского Академгородка</a></li>
                        <li><a href='?id=fog_pavlovskaya200812256983'>Гости Новосибирского Академгородка (люди науки)</a></li>
                        <li><a href='?id=fog_pavlovskaya200812256981'>Гости Новосибирского Академгородка (официальные лица)</a></li>
                        <li><a href='?id=w20070417_2_1046'>Гости Новосибирского Академгородка</a></li>
                        <li><a href='?id=w20070417_3_7197'>Строительство Новосибирского Академгородка</a></li>
                        <li><a href='?id=w20071113_9_9041'>Визит Н.С. Хрущева в Новосибирск (1959 г.)</a></li>
                        <li><a href='?id=krai_100616111436_1009'>Празднование 50-летия образования СО РАН</a></li>
                        <li><a href='?id=PA_Users_pavl_634395045572656250_4593'>Проект 'Сеть Интернет Академгородка'</a></li>
                        <li><a href='?id=PA_Users_svet_635182170969905234_19526'>Операция 'Дунай' глазами новосибирского студента</a></li>
                        <li><a href='?id=PA_Users_pavl_634395045572656250_12631'>Академгородок глазами французских журналистов</a></li>
                        <li><a href='?id=w20071030_1_26317'>Визит Шарля де Голля в Новосибирский Академгородок </a></li>
                        <li><a href='?id=w20071113_9_19474'>Фестиваль авторской песни (1968 г.)</a></li>
                        <li><a href='?id=w20070417_5_12843'>Фестиваль авторской песни 'Под интегралом' 40 лет спустя</a></li>
                        <li><a href='?id=fog_pavlovskaya200812254337'>Карнавал НГУ 1967 г.</a></li>
                        <li><a href='?id=fog_pavlovskaya200812254885'>Делегация космонавтов 'Союз-Аполлон' в Академгородке</a></li>
                        <li><a href='?id=pavl_100531115859_9984'>КВН НГУ (1985-1995)</a></li>
                        <li><a href='?id=fog_pavlovskaya200812254396'>КВН НГУ (1968-1970)</a></li>
                        <li><a href='?id=piu_200809053371'>Празднование масленицы (1967)</a></li>
                    </ul>
                </div>
            </td>
        </tr>
        <tr valign='top'>
            <td colspan='3'><hr /></td>
        </tr>
        <tr valign='top'>
            <td />
            <td colspan='2'>(c) Фотоархив Сибирского отделения Российской академии наук</td>
        </tr>
    </table>
</body>
</html>";
            return new HtmlResult(html);
        }

        private static XElement? BuildXPortrait(RRecord record, IFDataService db)
        {
            return new XElement("div", "BuildXPortrait result");
        }
    }
}
