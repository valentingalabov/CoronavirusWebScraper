﻿import ApiGetFunction from "./getFunction.js";
google.charts.load('current', { 'packages': ['corechart'] });


document.getElementById("chart-select").addEventListener("change", draw, { passive: true });

let statistics = await ApiGetFunction("https://localhost:44305/api/analaysis");

let divToDraw = document.getElementById('charts');

let dateDiv = document.getElementById('date');
let h1 = document.createElement('h1');
h1.textContent = statistics.date;
dateDiv.appendChild(h1);

function draw() {
    let selectElement = document.getElementById("chart-select");
    let title = selectElement.options[selectElement.value].textContent;

    if (selectElement.value == 1) {
        google.charts.setOnLoadCallback(drawPieChart(title, statistics.active, statistics.hospitalized));
    } else if (selectElement.value == 2) {
        google.charts.setOnLoadCallback(drawPieChart(title, statistics.hospitalized, statistics.icu));
    } else if (selectElement.value == 3) {
        google.charts.setOnLoadCallback(drawPieChart(title, statistics.infected, statistics.totalTests));
    } else if (selectElement.value == 4) {
        google.charts.setOnLoadCallback(drawMedicalBarChart);
    }
    else {
        document.getElementById("charts").innerHTML = "";
    }
}

function drawPieChart(title, el1, el2) {
    let chartData = title.split("/");
    var data = google.visualization.arrayToDataTable([
        [title, 'брой'],
        [chartData[0], el1],
        [chartData[1], el2]
    ]);

    var options = {
        pieSliceText: 'label',
        title: title,
        pieStartAngle: 100,
        is3D: true
    };

    var chart = new google.visualization.PieChart(divToDraw);
    chart.draw(data, options);
}

function drawMedicalBarChart() {
    var data = google.visualization.arrayToDataTable([
        ['Тип', 'Брой', { role: 'style' }, { role: 'annotation' }],
        ['Доктори', statistics.totalMedicalAnalisys.doctor, '#b87333', 'Доктори'],
        ['Медицински сестри', statistics.totalMedicalAnalisys.nurces, 'silver', 'Медицински сестри'],
        ['Санитари', statistics.totalMedicalAnalisys.paramedics_1, 'gold', 'Санитари'],
        ['Фелдшери', statistics.totalMedicalAnalisys.paramedics_2, 'color: #e5e4e2', 'Фелдшери'],
        ['Друг мед. персонал', statistics.totalMedicalAnalisys.other, 'green', 'Друг мед. персонал']
    ]);

    var options = {
        title: "Потвърдени случаи за медицински персонал по тип",
        bar: { groupWidth: "95%" },
        legend: { position: 'none' },
    };
    var chart = new google.visualization.BarChart(divToDraw);
    chart.draw(data, options);
}

