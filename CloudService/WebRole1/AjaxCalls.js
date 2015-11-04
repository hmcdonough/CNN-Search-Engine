//calls getQuerySuggestions, passing it json data to get results
function search() {
    if ($('#url').val() != "") {
        $.ajax({
            type: "POST",
            url: "Crawler.asmx/SearchTrie",
            data: JSON.stringify({ word: $('#url').val() }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: OnSuccess,
            failure: function (response) {
                alert(response.d);
            }
        });
    } else {
        $("#list").empty();
        $("ul").empty();
        $("#titles").empty();
    }
};

//sets up the resulting list on the html page
function OnSuccess(response) {
    var results = response.d.split(",");
    $("#list").empty();
    if (results.length > 0) {
        for (var index = 0; index < results.length; index++) {
            var li = document.createElement("li")
            li.innerHTML = results[index].replace(/\"/g, "");
            document.getElementById('list').appendChild(li);
        }
    }
}


function getRamAndCpu() {
    $.ajax({
        type: "POST",
        url: "Crawler.asmx/GetPerformance",
        data: "{}", //JSON.stringify({ word: $('#box').val() })
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            var s = response.d.split(',');
            document.getElementById("machineCounters").innerHTML =
                ("Ram Available: " + s[0] + "<br />CPU Usage: " + s[1]);
        },
        failure: function (response) {
            alert(response.d);
        }
    });
};

function getState() {

    $.ajax({
        type: "POST",
        url: "Crawler.asmx/GetX",
        data: JSON.stringify({ x: "state" }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            document.getElementById("state").innerHTML = "state: " +
                response.d;
        },
        failure: function (response) {
            alert(response.d);
        }
    });
};

function getTotal() {

    $.ajax({
        type: "POST",
        url: "Crawler.asmx/GetX",
        data: JSON.stringify({ x: "total" }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            document.getElementById("urlsCrawled").innerHTML = "Total Crawled: " +
                response.d;
        },
        failure: function (response) {
            alert(response.d);
        }
    });
};

function getAccepted() {

    $.ajax({
        type: "POST",
        url: "Crawler.asmx/GetX",
        data: JSON.stringify({ x: "accepted" }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            document.getElementById("sizeOfIndex").innerHTML = "Entities in Index: " +
                response.d;
        },
        failure: function (response) {
            alert(response.d);
        }
    });
};

function getNum() {

    $.ajax({
        type: "POST",
        url: "Crawler.asmx/GetX",
        data: JSON.stringify({ x: "numTitles" }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            document.getElementById("numTitles").innerHTML = "Number of Titles in Index: " +
                response.d;
        },
        failure: function (response) {
            alert(response.d);
        }
    });
};

function getLast() {

    $.ajax({
        type: "POST",
        url: "Crawler.asmx/GetX",
        data: JSON.stringify({ x: "lastTitle" }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            document.getElementById("lastTitle").innerHTML = "Last Title Added: " +
                response.d;
        },
        failure: function (response) {
            alert(response.d);
        }
    });
};

function getErrors() {
    $.ajax({
        type: "POST",
        url: "Crawler.asmx/GetErrors",
        data: "{}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d != "") {
                document.getElementById("errors").innerHTML = response.d
            }
        },
        failure: function (response) {
            alert(response.d);
        }
    });
};

function getURLs() {
    document.getElementById("lastTen").innerHTML = "Last Ten URLS: <br />";
    $.ajax({
        type: "POST",
        url: "Crawler.asmx/GetURLs",
        data: "{}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d != "") {
                var s = response.d.split(',');
                s.forEach(function (entry) {
                    document.getElementById("lastTen").innerHTML += entry + "<br />";
                });
            }
        },
        failure: function (response) {
            alert(response.d);
        }
    });
};

function getQueue() {
    $.ajax({
        type: "POST",
        url: "Crawler.asmx/GetX",
        data: JSON.stringify({ x: "queue" }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            document.getElementById("sizeOfQueue").innerHTML = "Queue Size:" + response.d;
        },
        failure: function (response) {
            alert(response.d);
        }
    });
};

$(document).ready(function () {
    $("#start").click(function () {
        $.ajax({
            type: "POST",
            url: "Crawler.asmx/StartCrawling",
            data: "{}", //JSON.stringify({ word: $('#box').val() })
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                document.getElementById("inputResults").innerHTML =
                    (response.d);
            },
            failure: function (response) {
                alert(response.d);
            }
        });
    });
});

$(document).ready(function () {
    $("#stop").click(function () {
        $.ajax({
            type: "POST",
            url: "Crawler.asmx/StopCrawling",
            data: "{}", //JSON.stringify({ word: $('#box').val() })
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                document.getElementById("inputResults").innerHTML =
                    (response.d);
            },
            failure: function (response) {
                alert(response.d);
            }
        });
    });
});

$(document).ready(function () {
    $("#clear").click(function () {
        $.ajax({
            type: "POST",
            url: "Crawler.asmx/ClearIndex",
            data: "{}", //JSON.stringify({ word: $('#box').val() })
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                document.getElementById("inputResults").innerHTML =
                    (response.d);
            },
            failure: function (response) {
                alert(response.d);
            }
        });
    });
});

$(document).ready(function () {
    $("#findbutton").click(function () {
        $("stats").empty();
        $("titles").empty();
        var t = document.getElementById('table');
        if (t != null) {
            t.parentNode.removeChild(t);
        }
        $.ajax({
            url: 'http://ec2-52-10-224-180.us-west-2.compute.amazonaws.com/getPlayer.php',
            data: { name: $('#url').val() },
            dataType: 'jsonp',
            jsonp: 'callback',
            jsonpCallback: 'makeTable',
            success: function () {
            }
        });
        $.ajax({
            type: "POST",
            url: "Crawler.asmx/GetUrlsForTitle",
            data: JSON.stringify({ title: $('#url').val() }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                var html = "Titles Found: <br /><br />";
                if (response.d != null) {
                    for (var i = 0; i < response.d.length ; i++) {
                        html += response.d[i].Item3 + "<br />" + decodeURIComponent(response.d[i].Item1) + "<br />" + response.d[i].Item4.substring(0, 20) + "<br /><br />";
                    }
                } else {
                    html = "no titles found with those keywords";
                }
                document.getElementById("titles").innerHTML = html;
            },
            failure: function (response) {
                alert(response.d);
            }
        });
    });
});

function makeTable(data) { //callback for jsonp
    var stats = data.split(",");
    if (stats.length > 2) {
        var table = document.createElement('table');
        table.id = "table";
        var tr = document.createElement('tr');

        var tdImage = document.createElement('td');
        var img = new Image(65, 90);
        var name = stats[0].split(" ");
        img.src = "http://www.nba.com/media/playerfile/" + name[0].toLowerCase() + "_" + name[1].toLowerCase() + ".jpg";
        tdImage.appendChild(img);

        //var tdName = document.createElement('td');
        //tdName.innerHTML = stats[0] + " " + stats[1];

        tr.appendChild(tdImage);
        //tr.appendChild(tdName);

        for (var i = 0; i < stats.length; i++) {
            var td = document.createElement('td');
            td.innerHTML = stats[i];
            tr.appendChild(td);
        }

        var trHeaders = makeHeaders();
        table.appendChild(trHeaders);
        table.appendChild(tr);
        document.getElementById("stats").appendChild(table);
    } else {
        document.getElementById("stats").innerHTML = "no stats found";
    }
}

function makeHeaders() {
    var tr = document.createElement('tr');
    var headers = ["Image", "Name", "Games Played", "Field Goal Percentage", "Three Point Percentage", "Free Throw Percentage", "Points Per Game"];
    headers.forEach(function (a) {
        var td = document.createElement("td");
        td.innerHTML = a;
        tr.appendChild(td);
    });
    return tr;
}

var performance = setInterval(function () { getRamAndCpu() }, 3000);
var state = setInterval(function () { getState() }, 3000);
var total = setInterval(function () { getTotal() }, 3000);
var accepted = setInterval(function () { getAccepted() }, 3000);
var errors = setInterval(function () { getErrors() }, 10000);
var queue = setInterval(function () { getQueue() }, 3000);
var urls = setInterval(function () { getURLs() }, 10000);
var num = setInterval(function () { getNum() }, 3000);
var last = setInterval(function () { getLast() }, 3000);
