﻿using Microsoft.AspNetCore.Components.Forms;

namespace Publicuem2
{
    public class Styles
    {
        public static readonly string Css =
@"a {

}
html {
    background: #FFF8CC;
    min-height: 100%;
    font-family: Helvetica;
    display: flex;
    flex-direction: column;
}
body {
    margin: 0;
    padding: 0 15px;
    display: flex;
    flex-direction: column;
    flex: auto;
}
h1 {
    margin-top: 0;
}
h1, p {
    color: #006064;
}
img {
    border: 0;
}
table.s, td.s, th.s {
    border: 1px solid;
}
table.s {
    border-collapse: collapse;
}
.header {
    width: 100%;
    min-width: 460px;
    max-width: 960px;
    margin: 0 auto 30px;
    padding: 30px 0 10px;
    display: flex;
    flex-wrap: wrap;
    justify-content: space-between;
    box-sizing: border-box;
}
td.s {
    font-size: 1.2rem;
}
th.s {
    //border: solid 1px grey;
    background-color: #f0f0f0;
    padding: 4px 4px 4px 4px;
    font-weight: lighter;
    //font-size: smaller;
}
input {
    font-size: 1.2rem;
}
.logo {
    font-size: 1.5rem;
    color: #fff;
    text-decoration: none;
    margin: 5px 0 0 5px;
    justify-content: center;
    align-items: center;
    display: flex;
    flex: none;
    align-items: center;
    background: #839FFF;
    width: 130px;
    height: 50px;
}
.nav {
    margin: -5px 0 0 -5px;
    display: flex;
    flex-wrap: wrap;
}
.nav-item {
    background: #BDC7FF;
    width: 130px;
    height: 50px;
    font-size: 1.5rem;
    color: #fff;
    text-decoration: none;
    display: flex;
    margin: 5px 0 0 5px;
    justify-content: center;
    align-items: center;
}
.sqr {
    height: 300px;
    width: 300px;
    background: #FFDB89;
}
.main {
    width: 100%;
    min-width: 460px;
    max-width: 960px;
    margin: auto;
    flex: auto;
    box-sizing: border-box;
}
.box {
    font-size: 1.25rem;
    line-height: 1.5;
    margin: 0 0 40px -50px;
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
}
.box-base {
    margin-left: 50px;
    flex: 1 0 430px;
}
.box-side {
    margin-left: 50px;
    font: none;
}
.box-img {
    max-width: 100%;
    height: auto;
}
.content {
    margin-bottom: 30px;
    display: flex;
    flex-wrap: wrap;
}
.banners {
    flex: 1 1 200px;
}
.banner {
    background: #FFDB89;
    width: 100%;
    min-width: 100px;
    min-height: 200px;
    font-size: 3rem;
    color: #fff;
    margin: 0 0 30px 0;
    display: flex;
    justify-content: center;
    align-items: center;
}
.posts {
    margin: 0 0 30px 30px;
    flex: 1 1 200px;
}
.comments {
    margin: 0 0 30px 30px;
    flex: 1 1 200px;
}
.comment {
    display: flex;
}
.comment-side {
    padding-right: 20px;
    flex: none;
}
.comment-base {
    flex: auto;
}
.comment-avatar {
    background: #FFA985;
    width: 50px;
    height: 50px;
}
.footer {
    background: #FF3366;
    width: 100%;
    max-width: 960px;
    min-width: 460px;
    color: #fff;
    margin: auto;
    padding: 15px;
    box-sizing: border-box;
}
@media screen and (max-width: 800px) {
    .banners {
        margin-left: -30px;
        display: flex;
        flex-basis: 100%;
    }
    .banner {
        margin-left: 30px;
    }
    .posts {
        margin-left: 0;
    }
}
@media screen and (max-width: 600px) {
    .content {
        display: block;
    }
    .banners {
        margin: 0;
        display: block;
    }
    .banner {
        margin-left: 0;
    }
    .posts {
        margin: 0;
    }
}
";            
    }
}
