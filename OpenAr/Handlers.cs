namespace OpenAr
{
    public class Handlers
    {
        private static string IF(bool usl, string iftrue, string iffalse) 
        {
            if (usl) return iftrue; return iffalse; 
        }
        public static string Page(string? id, string? ss, string bw, string? sv ) => $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>Открытый архив СО</title>
    <link rel='stylesheet' type='text/css' href='/css/moo.css' />
    <link rel='shortcut icon' href='/css/favicon.ico' type='image/x-icon'>
</head>
<body>
    <div id='site'>
        <div class='pd'>
            <table cellpadding='0' cellspacing='0' border='0' width='100%'>
                <!--
                <tr>
                <td class='header'>
                &laquo;Открытый архив СО&nbsp;РАН как электронная система накопления, представления и&nbsp;хранения научного наследия&raquo; М-48
                </td>
                </tr>
                -->
                <tr>
                    <td class='block-top'>
                        <a href='/Index'><img src='/img/logo.png' class='logo' alt='Открытый архив СО РАН' /></a>
                        <div class='main-menu'>
                            <a href='' class='menu-item nov'>Фонды</a>
                            <span class='menu-sep'>|</span>
                            <!--
                            <a href='' class='menu-item nov'>Персоны</a>
                            <span class='menu-sep'>|</span>
                            <a href='' class='menu-item nov'>Организации</a>
                            <span class='menu-sep'>|</span>
                            <a href='' class='menu-item nov'>Полезные ссылки</a>
                            <span class='menu-sep'>|</span>
                            -->
                            <a href='About.cshtml' class='menu-item nov'>О проекте</a>
                            <span class='menu-sep'>|</span>
                            <a href='Participants.cshtml' class='menu-item nov'>Участники</a>
                            <span class='menu-sep'>|</span>
                            <a href='Contacts.cshtml' class='menu-item nov'>Контакты</a>
                        </div>
                    </td>
                </tr>
            </table>

            <!-- В таблице: поисковая панель, основная панель (RenderBody), правая панель -->
            <table cellpadding='0' cellspacing='0' border='0' width='100%'>
                <tr valign='top'>
                    <td class='block-content'>
                        <div id='wrap'>
                            <div class='fk-ie'>
                                <form method='post' action='/Index/'>
                                    <div class='bsearch-1'>
                                        <div class='bsearch-2'>
                                            <div class='bsearch-3'>
                                                <table cellpadding='0' cellspacing='0' border='0' width='100%'>
                                                    <tr>
                                                        <td class='s-input-1'>
                                                            <div class='search-form'>
                                                                <input name='ss' type='text' placeholder='поиск' title='введите текст для поиска' value='{ss}' />
                                                            </div>
                                                        </td>
                                                        <td class='s-input-1' style='width:50px;vertical-align:central;'>
                                                            <div class='search-form' style='width:20px; height:16px;vertical-align:central;'>
" +                                                                
(bw == "on" ? "<input name='bw' type='checkbox' checked style='width:20px; height:16px;vertical-align:central;' />" :
              "<input name='bw' type='checkbox' style='width:20px; height:16px;vertical-align:central;' />" +
                                                            $@"</div>
                                                        </td>
                                                        <td class='s-input-2' style=''>
                                                            <div class='search-form'>
                                                                <select name='sv' style='width:100%;'>
                                                                    <option value=''></option>
{IF(sv == "person", "<option selected value='person'>Персоны</option>", "<option value='person'>Персоны</option>")}
{IF(sv == "org-sys", "<option selected value='org-sys'>Персоны</option>", "<option value='org-sys'>Организации</option>")}
{IF(sv == "collection", "<option selected value='collection'>Коллекции</option>", "<option value='collection'>Коллекции</option>")}
{IF(sv == "document", "<option selected value='document'>Документы</option>", "<option value='document'>Документы</option>")}
{IF(sv == "city", "<option selected value='city'>Города</option>", "<option value='city'>Города</option>")}
{IF(sv == "country", "<option selected value='country'>Страны</option>", "<option value='country'>Страны</option>")}
                                                                </select>
                                                            </div>

                                                        </td>
                                                        <td>
                                                            <input type='submit' value='&nbsp; &nbsp;&nbsp;  найти' class='search-go' />
                                                        </td>
                                                        <td class='s-input-o'>
                                                            <a href='' class='small white'>
                                                                Расширенный<br /><img src='/img/search-ext-btn.png' class='ext-btn' alt='' />поиск<img src='/img/p.gif' class='ext-btn' alt='' />
                                                            </a>

                                                        </td>
                                                    </tr>
                                                </table>

                                            </div>
                                        </div>
                                    </div>
                                </form>
                                <br clear='all' />
                                ==========RenderBody()
                                <br clear='all' />




                            </div>
                        </div>
                    </td>

                    <td class='sep-content'><img src='/img/p.gif' width='40' height='1' alt='' /></td>
                    <td class='block-right'>
                        ==========RenderSection('rightpanel', false)
                    </td>
                </tr>
            </table>

            <table cellpadding='0' cellspacing='0' border='0' width='100%'>
                <tr valign='top'>
                    <td class='block-content'>
                        <div class='line-bottom'> </div>

                        <a href='#'><img src='/img/up_arrow.png' border='0' align='left' /></a>


                        <div class='copyright'>
                            &copy; 2013-2014 Институт Систем Информатики <br />
                            им. А.П. Ершова СО РАН<br />
                            <a href='mailto:oda@iis.nsk.su?subject=Open Digital Archive'><img src='/img/ico-mail.gif' class='ico-mail' alt='Написать письмо' />Написать письмо</a>
                        </div>

                        <div class='main-menu'>
                            <a href='Default.cshtml' class='menu-item nov'>Фонды</a>
                            <span class='menu-sep'>|</span>
                            <a href='About.cshtml' class='menu-item nov'>О проекте</a>
                            <span class='menu-sep'>|</span>
                            <a href='Participants.cshtml' class='menu-item nov'>Участники</a>
                            <span class='menu-sep'>|</span>
                            <a href='Contacts.cshtml' class='menu-item nov'>Контакты</a>
                        </div>
                    </td>
                </tr>
            </table>
        </div>
    </div>
</body>
</html>");
    }
}
