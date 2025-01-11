
var xhttp;
var thisPageName = "?";
function HTTPrequest(requestUri, params)//, handler)
{
    if (document.all) {
        xhttp = new ActiveXObject("Msxml2.XMLHTTP");
    } else {
        // Mozilla - based browser 
        xhttp = new window.XMLHttpRequest();
    }
    //if (handler != null) xhttp.onreadystatechange = handler;
    xhttp.open("POST", requestUri, false);
    xhttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
    //    xhttp.setRequestHeader("Content-length", params.length);
    //    xhttp.setRequestHeader("Connection", "close");
    //responseXML contains an XMLDOM object
    xhttp.send(params);
}
function stringReplace(search, replace, sourceString) {
    return sourceString.split(search).join(replace);
}
function AddRow(sender, itemId, externPropId, subTypeId) {
  //  window.alert(itemId);
    var divForTable = sender.parentNode;
    if (divForTable.getElementsByTagName("TABLE").length == 0) {
        //window.alert(xhttp.responseText);
        HTTPrequest(thisPageName, "id=" + itemId +
         "&externPropId=" + encodeURIComponent(externPropId) +
          "&editSubTypeId=" + encodeURIComponent(subTypeId) + 
          "&AJAX=yes&c=AddTable&d=" + encodeURIComponent(new Date()));
      //  var divs = stringReplace("/>", "></div>", stringReplace("<html>", "", stringReplace("<body>", "", stringReplace("<div></div></body></html>", "", xhttp.responseText))));
        divForTable.innerHTML += xhttp.responseText;
       // window.alert(xhttp.responseText);
    }
    else {
        HTTPrequest(thisPageName, "id=" + encodeURIComponent(itemId) + "&externPropId=" + encodeURIComponent(externPropId) + "&editSubTypeId=" + encodeURIComponent(subTypeId) + "&AJAX=yes&c=addRow&d=" + encodeURIComponent(new Date()));
       
        var table = divForTable.getElementsByTagName("TABLE")[0];
        var newRow = table.insertRow(1);
        newRow.setAttribute("align", "center");
        var newRowTemp = document.createElement("DIV");
        var divs =stringReplace("/>", "></div>",  stringReplace("<html>", "", stringReplace("<body>", "", stringReplace("<div></div></body></html>", "", xhttp.responseText))));
        newRowTemp.innerHTML = divs;       
        var tds = newRowTemp.children[0].children;
        for (var i = 0; i < tds.length; i++) {
           var newCell = newRow.insertCell(-1);
           newCell.innerHTML = tds[i].innerHTML;
           if (i == tds.length - 1)
               newCell.setAttribute("externpropid", tds[i].getAttribute("externpropid"));
        }
      //  var tbody = div_for_Table.getElementsByTagName("TABLE")[0].children[0];
      //  tbody.appendChild(newRow);
        //div_for_Table.innerHTML = div_for_Table.innerHTML.replace(/<\/tbody>/g, newRow);
    }
}
//function GetRow(sender, itemId, externPropId, eid) {
//    HTTPrequest("Ursul.aspx", "id=" + encodeURIComponent(itemId) + "&externPropId=" + encodeURIComponent(externPropId) + "&editSubTypeId=" + encodeURIComponent(subTypeId) + "&AJAX=yes&c=GetRow&d=" + encodeURIComponent(new Date()));

//}
//function DeleteRowCancel(sender, eid, externPropId) {
//    var conteiner = sender.parentNode.parentNode;
//    conteiner.removeChild(sender.parentNode);
//    conteiner.innerHTML += "<span class='textbutton' onclick=\"DeleteRowConfirmed(this, '" + eid + "', '" + externPropId + "')\">удалить ряд</span>";    
//}
//function DeleteRow(sender, eid, externPropId) {
//    var conteiner = sender.parentNode;
//    conteiner.removeChild(sender);
//    conteiner.innerHTML += "<span><span class='textbutton' onclick=\"DeleteRowConfirmed(this, '" + eid + "', '" + externPropId + "')\">ok</span>"
//    +  " deleting "
//    + "<span class='textbutton' onclick=\"DeleteRowCancel(this, '" + eid + "', '" + externPropId + "')\">cancel</span></span>";
//    
// }
    function DeleteRowConfirmed(sender, eid, externPropId) {
        HTTPrequest(thisPageName, "externPropId=" + externPropId + "&eid=" + encodeURIComponent(eid) + "&AJAX=" + encodeURIComponent("yes") + "&c=" + encodeURIComponent("DELETERow") + "&d=" + encodeURIComponent(new Date()));
        var table = sender.parentNode.parentNode.parentNode.parentNode;
        var tbody = sender.parentNode.parentNode.parentNode;
        var tr = sender.parentNode.parentNode;
     //   window.alert(tr.innerHTML);
        tbody.removeChild(tr);
       // window.alert(table.innerHTML);
         if (table.getElementsByTagName("TR").length == 1) {
           var tableContainer = table.parentNode.parentNode;
           tableContainer.removeChild(table.parentNode);
       }
       if (externPropId == '') {
        window.location =""; 
       }
    }
    function EditeCell(sender, propId, eid) {
        var value = sender.getAttribute('DIVVALUE');
      //  window.alert('f' + sender.innerHTML + 'f');
        HTTPrequest(thisPageName, "eid=" + encodeURIComponent(eid) + "&editPropId=" + encodeURIComponent(propId) + "&editPropValue=" + encodeURIComponent(value) + "&AJAX=" + encodeURIComponent("yes") + "&c=" + encodeURIComponent("cell") + "&d=" + encodeURIComponent(new Date()));
        var inputParent = sender.parentNode;
        sender.parentNode.innerHTML = xhttp.responseText;
        inputParent.children[0].focus();
    }
    function ComboBoxChanged(sender, propId, eid) {
        var value = sender.options[sender.selectedIndex].getAttribute('OPTIONVALUE');
         HTTPrequest(thisPageName, "eid=" + encodeURIComponent(eid) + "&editPropId=" + encodeURIComponent(propId) + "&editPropValue=" + encodeURIComponent(value) + "&AJAX=" + encodeURIComponent("yes") + "&c=" + encodeURIComponent("cellEnd") + "&d=" + encodeURIComponent(new Date()));
        InsertOKinTableRow(sender.parentNode.parentNode, propId, eid);
        sender.parentNode.innerHTML = xhttp.responseText;
    }
    function EditeEndCellKeyUp(sender, propId, eid) {
        if (event.keyCode != 13) return;
        EditeEndCell(sender, propId, eid);
    }
    function EditeEndCell(sender, propId, eid) {
        var value = sender.value;
        HTTPrequest(thisPageName, "eid=" + encodeURIComponent(eid) + "&editPropId=" + encodeURIComponent(propId) + "&editPropValue=" + encodeURIComponent(value) + "&AJAX=" + encodeURIComponent("yes") + "&c=" + encodeURIComponent("cellEnd") + "&d=" + encodeURIComponent(new Date()));
        InsertOKinTableRow(sender.parentNode.parentNode, propId, eid);
        sender.parentNode.innerHTML = xhttp.responseText;
    }
    function InsertOKinTableRow(tr, propId, eid) {
        var tds = tr.children;
        var div = tds[tds.length - 1];
        if (div.getElementsByTagName("SPAN").length == 0)
            div.innerHTML += " <span class='textbutton' onclick=\"TableChanged(this, '" + eid + "', 'true')\">ok</span>";
    }
    function CellObjGetSearchPanel(sender, propId, eid, mainItemId) {
        var searchString = sender.value;
        if (sender.nodeName == "DIV") {
            searchString = sender.parentNode.children[0].innerHTML;
         } // else it's input
        var targetType;
        var selectOrDiv = sender.parentNode.parentNode.children[0];
        if (selectOrDiv.nodeName == "DIV")
            targetType = selectOrDiv.getAttribute("targettype");
        else {
            var options = selectOrDiv.getElementsByTagName("OPTION");
            for (var j = 0; j < options.length; j++)
                if (options[j].selected) {
                    targetType = options[j].value;
                    break;
                }
            }
            var tergetTypeText = "";            
             if(targetType != null)  tergetTypeText = "&targetype=" +  encodeURIComponent(targetType);
             HTTPrequest(thisPageName, "id=" + mainItemId + "&eid=" + encodeURIComponent(eid) + "&ss=" + encodeURIComponent(searchString) + "&editPropId=" + encodeURIComponent(propId) + tergetTypeText + "&AJAX=" + encodeURIComponent("yes") + "&c=" + encodeURIComponent("cellObjSearchPanelEnd") + "&d=" + encodeURIComponent(new Date()));
             sender.parentNode.parentNode.innerHTML = xhttp.responseText;
    }
    function CellObjInputSearch(sender, propId, eid) {
        if (event.keyCode != 13) return;
        CellObjGetSearchPanel(sender, propId, eid);
    }
    function CellObjSearchResSelect(sender, propId, eid, targetId, mainItemId) {
        var targetIdText = "";
      //  window.alert("I'm here");
         if (targetId != '')
            targetIdText = "&targetId=" + targetId;
         HTTPrequest(thisPageName, "id=" + mainItemId + "&eid=" + eid + targetIdText + "&editPropId=" + propId + "&AJAX=yes&c=cellObjView&d=" + new Date());
        InsertOKinTableRow(sender.parentNode.parentNode.parentNode, propId, eid);
        sender.parentNode.parentNode.innerHTML = xhttp.responseText;        
    }
    function CellObjSearchNewSelect(sender, propId, eid, targetType, mainItemId) {
        targetName = sender.parentNode.parentNode.parentNode.getElementsByTagName("INPUT")[0].value;
        //window.alert(targetName
        HTTPrequest(thisPageName, "id=" + mainItemId + "&eid=" + encodeURIComponent(eid) + "&targetType=" + encodeURIComponent(targetType) + "&targetName=" + encodeURIComponent(targetName) + "&editPropId=" + encodeURIComponent(propId) + "&AJAX=" + encodeURIComponent("yes") + "&c=" + encodeURIComponent("cellObjNewView") + "&d=" + encodeURIComponent(new Date()));
        InsertOKinTableRow(sender.parentNode.parentNode.parentNode.parentNode, propId, eid);
        sender.parentNode.parentNode.parentNode.innerHTML = xhttp.responseText;
    }
    function TableChanged(sender, eid, isOkClicked) {               
         var command;
        var isNew = "false";
        var deleting = false;
        var paramsQuery = "";
//        if (isOkClicked == "true") {
            command = "&ask="+ encodeURIComponent("ok");
//        }
//        else {
//            command = "&ask="+ encodeURIComponent("cancel");
//            isNew = sender.parentNode.getAttribute("isNew");
//            if (isNew == "true") {
//                paramsQuery = "&deleteNEWItem="+ encodeURIComponent("true");
//                deleting = true;
//            }
//        }
        var tr = sender.parentNode.parentNode;
        var trContent = tr.children;
        for (var i = 0; i < trContent.length - 1; i++) {
            if (deleting || !(isOkClicked == "true")) continue;
            var input = trContent[i].children[0];
            if (input == null) continue;
            if (input.getAttribute("propInputType") == "text") {
                paramsQuery += "&" + input.getAttribute("propId") + "=";
                paramsQuery +=  encodeURIComponent(input.getAttribute("DIVVALUE"));
            }
            else if (input.nodeName == "SELECT") {
                paramsQuery += "&" + input.name + "=";
                var options = input.getElementsByTagName("OPTION");
                for (var j = 0; j < options.length; j++) {
                    if (options[j].selected) {
                        paramsQuery +=  encodeURIComponent(options[j].value);
                        break;
                    }
                }
            }
            //        else if (input.nodeName == "TEXTAREA") {
            //        }
            else if (input.getAttribute("propInputType") == "id" && input.getAttribute("propValue") != null) {
                paramsQuery += "&" + input.getAttribute("propId") + "=";
                if (input.getAttribute("propValue") != "NEW")
                    paramsQuery += encodeURIComponent(input.getAttribute("propValue"));
                else {
                    paramsQuery += encodeURIComponent("NEW") + "&TargetType." + input.getAttribute("propId") + "=" + encodeURIComponent(input.getAttribute("targetType"))
                             + "&propTargetName." + input.getAttribute("propId") + "=" + encodeURIComponent(input.getAttribute("propTargetName"));
                }
            }
//            else if (input.nodeName == "A") {
//                deleting = true;
//                paramsQuery = "&deleteItem=true"
//            }
            if (input.getAttribute("style") != null) {
                input.removeAttribute("style");
//            var buts = input.getElementsByTagName("BUTTON");
//                if (buts.length != 0)
//                    input.removeChild(buts[buts.length - 1]);
            }
        }
        //window.alert(paramsQuery);
        HTTPrequest(thisPageName, "editPropId=" + encodeURIComponent(sender.parentNode.getAttribute("externPropId")) + "&eid=" + encodeURIComponent(eid) + paramsQuery + command + "&AJAX=" + encodeURIComponent("yes") + "&c=" + encodeURIComponent("tableEndEdit") + "&d=" + encodeURIComponent(new Date()));
        trContent[trContent.length - 1].removeChild(sender);


          var newRowTemp = document.createElement("DIV");
        var divs = stringReplace("/>", "></div>",  stringReplace("<html>", "", stringReplace("<body>", "", stringReplace("<div></div></body></html>", "", xhttp.responseText))));
        newRowTemp.innerHTML = divs;
    //   window.alert(newRowTemp.innerHTML);
        var tds = newRowTemp.children[0].children;
        for (i = 0; i < tr.cells.length; i++) {
            tr.cells[i].innerHTML = tds[i].innerHTML;
        }
        
       // xhttp.responseText;
    }

    function CancelCellText(sender, realvalue) {
        var container = sender.parentNode;
        container.InnerHTML = realvalue;
       // container.removeChild(sender);
    }
    function HidingTable(sender, propId) {
        var hider = sender.parentNode.parentNode.children[0];
        var element = sender.parentNode.parentNode.children[1];
        if (element.style.display == "none") {
          //  window.alert(hider.innerHTML);
          hider.innerHTML = stringReplace("+ ", "", hider.innerHTML);
           element.style.display = "block";
       } else {
       if(element.getElementsByTagName("TABLE").length != 0)
       hider.innerHTML = "+ " + hider.innerHTML;
            element.style.display = "none";
        }
        HTTPrequest(thisPageName, "editPropId=" + propId + "&AJAX=yes&c=hiding&d=" + encodeURIComponent(new Date()));
    }