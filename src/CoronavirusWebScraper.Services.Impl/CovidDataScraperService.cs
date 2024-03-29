﻿namespace CoronavirusWebScraper.Services.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using AngleSharp;
    using AngleSharp.Dom;
    using CoronavirusWebScraper.Data;
    using CoronavirusWebScraper.Data.Models;
    using CoronavirusWebScraper.Services.ServiceModels;
    using MongoDB.Bson;

    /// <summary>
    /// Data scraper implementation let you scrape data and add it to database.
    /// </summary>
    public class CovidDataScraperService : ICovidDataScraperService
    {
        private readonly IMongoRepository<CovidStatistics> repository;

        /// <summary>
        /// Constructor Implementation repository to store covid19 statistics data.
        /// </summary>
        /// <param name="repository">Mongodb repository.</param>
        public CovidDataScraperService(IMongoRepository<CovidStatistics> repository)
        {
            this.repository = repository;
        }

        /// <inheritdoc />
        public async Task ScrapeData()
        {
            var document = await this.FetchDocument();

            if (document != null)
            {
                await this.repository.InsertOneAsync(document);
            }
        }

        private async Task<CovidStatistics> FetchDocument()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);

            var document = await context.OpenAsync(Constants.CovidUrl);

            var covidStatisticUrl = Constants.CovidUrl + document.QuerySelector(".statistics-sub-header.nsi").GetAttribute("href");
            var statisticDocument = await context.OpenAsync(covidStatisticUrl);

            var statistics = document.QuerySelectorAll(".statistics-container > div > p").Select(x => x.TextContent).ToArray();
            var allTebles = statisticDocument.QuerySelectorAll(".table").ToArray();

            // Date
            var currentDateSpan = document.QuerySelector(".statistics-header-wrapper span").TextContent.Split(" ");
            var time = currentDateSpan[2];
            var date = currentDateSpan[5];
            var month = currentDateSpan[6];
            var year = currentDateSpan[7];
            var currDateAsString = string.Join(" ", date, month, year, time);
            var currDate = DateTime.Parse(currDateAsString);

            var dataDate = currDate.ToString(Constants.DateTimeFormatISO8601WithTimeZone);

            var dateScraped = DateTime.UtcNow.ToString(Constants.DateTimeFormatISO8601);

            // No scrape data if already scraped for current day
            var currentDayStatistics = await this.repository.FindOneAsync(filter => filter.Date == dataDate);

            if (currentDayStatistics != null)
            {
                return null;
            }

            // Tests
            var totalTestsByTypeTableRecords = allTebles[1].QuerySelectorAll("td").Select(x => x.TextContent).ToArray();

            var totalTests = this.IntParser(statistics[0]);
            var totalPcr = this.IntParser(totalTestsByTypeTableRecords[1]);
            var totalAntigen = this.IntParser(totalTestsByTypeTableRecords[4]);

            var totalTests24 = this.IntParser(statistics[2]);
            var totalPcr24 = this.IntParser(totalTestsByTypeTableRecords[2]);
            var totalAntigen24 = this.IntParser(totalTestsByTypeTableRecords[5]);

            // Confirmed
            var confirmedTestsByTypeTableRecords = allTebles[2].QuerySelectorAll("td").Select(x => x.TextContent).ToArray();
            var confirmedByRegionTableRecords = allTebles[3].QuerySelectorAll("td").SkipLast(3).Select(x => x.TextContent).ToArray();

            var totalConfirmed = this.IntParser(statistics[4]);
            var confirmedPcr = this.IntParser(confirmedTestsByTypeTableRecords[1]);
            var confirmedAntigen = this.IntParser(confirmedTestsByTypeTableRecords[4]);

            var confirmedPcr24 = this.IntParser(confirmedTestsByTypeTableRecords[2]);
            var confirmedAntigen24 = this.IntParser(confirmedTestsByTypeTableRecords[5]);
            var totalConfirmed24 = this.IntParser(confirmedTestsByTypeTableRecords[8]);

            // Active
            int active = this.IntParser(statistics[6]);

            var hospitalized = this.IntParser(statistics[12]);
            var intensiveCare = this.IntParser(statistics[14]);

            // Recovered
            var totalRecovered = this.IntParser(statistics[8]);
            var totalRecovered24 = this.IntParser(statistics[10]);

            // Deceased
            var deceased = this.IntParser(statistics[16]);
            var deceased24 = this.IntParser(statistics[18]);

            // Vaccinated
            var vaccinated = this.IntParser(statistics[20]);
            var vaccinated24 = this.IntParser(statistics[22]);

            var vaccinatedTableRecords = allTebles[5].QuerySelectorAll("tr").Last().QuerySelectorAll("td").Select(x => x.TextContent).ToArray();

            var comirnaty = this.IntParser(vaccinatedTableRecords[2]);
            var moderna = this.IntParser(vaccinatedTableRecords[3]);
            var astraZeneca = this.IntParser(vaccinatedTableRecords[4]);
            var janssen = this.IntParser(vaccinatedTableRecords[5]);
            var totalVaccinatedComplate = this.IntParser(vaccinatedTableRecords[6]);

            // MedicalTable
            var medicalTableRecords = allTebles[4].QuerySelectorAll("td").Select(x => x.TextContent).ToArray();

            // TotalByTypePrc
            var totalTestedPcrPercentage = this.DevideTwoIntiger(totalPcr, totalTests);
            var totalTestedAntigenPercentage = this.DevideTwoIntiger(totalAntigen, totalTests);

            // Last24hByTypePrc
            var last24PcrPercentage = this.DevideTwoIntiger(totalPcr24, totalTests24);
            var last24AntigenPercentage = this.DevideTwoIntiger(totalAntigen24, totalTests24);

            // TotalConfirmed/TotalTested
            var totalPerTestedPrcPercentage = this.DevideTwoIntiger(totalConfirmed, totalTests);

            // ConfirmedFor24h/TotalTestedFor24h
            var last24PerTestedPrcPercentage = this.DevideTwoIntiger(totalConfirmed24, totalTests24);

            // TotalConfirmedByTypePercentage
            var totalConfirmedPcrPercentage = this.DevideTwoIntiger(confirmedPcr, totalConfirmed);
            var totalConfirmedAntigenPercentage = this.DevideTwoIntiger(confirmedAntigen, totalConfirmed);

            var last24ConfirmedPcrPercentage = this.DevideTwoIntiger(confirmedPcr24, totalConfirmed24);
            var last24ConfirmedAntigenPercentage = this.DevideTwoIntiger(confirmedAntigen24, totalConfirmed24);

            // ActivePercentage
            var hospitalizedPerActive = this.DevideTwoIntiger(hospitalized, active);
            var icuPerHospitalized = this.DevideTwoIntiger(intensiveCare, hospitalized);

            // MedicalInformation
            var previousDate = currDate.AddDays(-1).ToString(Constants.DateTimeFormatISO8601WithTimeZone);

            var totalMedical = this.IntParser(medicalTableRecords[11]);

            // Check having information about medics for previous day.
            var medicalInformationForPreviousDay = this.repository
                        .FilterBy(filter => filter.Date == previousDate, projected
                        => projected.Overall.Confirmed.Medical)
                        .FirstOrDefault();

            // Get information about medicalConfirmed/totalConfirmed for 24h if have one.
            double medicalPrc = 0;
            if (medicalInformationForPreviousDay != null)
            {
                medicalPrc
                      = this.DevideTwoIntiger(totalMedical - medicalInformationForPreviousDay.Total, totalConfirmed24);
            }

            var covidStatistics = new CovidStatistics
            {
                Date = dataDate,
                ScrapedDate = dateScraped,
                Country = "BG",
                Overall = new Overall
                {
                    Tested = new Tested
                    {
                        Total = totalTests,
                        TotalByType = new TestedByType { PCR = totalPcr, Antigen = totalAntigen },
                        Last24 = totalTests24,
                        TotalByType24 = new TestedByType { PCR = totalPcr24, Antigen = totalAntigen24 },
                    },
                    Confirmed = new Confirmed
                    {
                        Total = totalConfirmed,
                        TotalByType = new TestedByType { PCR = confirmedPcr, Antigen = confirmedAntigen },
                        Last24 = totalConfirmed24,
                        TotalByType24 = new TestedByType { PCR = confirmedPcr24, Antigen = confirmedAntigen24 },
                        Medical = this.GetMedicalStatistics(totalMedical, medicalTableRecords, medicalInformationForPreviousDay),
                    },
                    Active = new Active
                    {
                        Curent = active,
                        CurrentByType = new ActiveTypes { Hospitalized = hospitalized, Icu = intensiveCare },
                    },
                    Recovered = new TotalAndLast
                    {
                        Total = totalRecovered,
                        Last = totalRecovered24,
                    },
                    Deceased = new TotalAndLast
                    {
                        Total = deceased,
                        Last = deceased24,
                    },
                    Vaccinated = new Vaccinated
                    {
                        Total = vaccinated,
                        Last = vaccinated24,
                        LastByType = new VaccineType
                        {
                            Comirnaty = comirnaty,
                            Moderna = moderna,
                            AstraZeneca = astraZeneca,
                            Janssen = janssen,
                        },
                        TotalCompleted = totalVaccinatedComplate,
                    },
                },
                Regions = this.GetAllRegionsData(allTebles),
                Stats = new Stats
                {
                    TestedPrc = new TestedPrc
                    {
                        TotalByTyprPrc = new PcrAntigenPrc { PCR = totalTestedPcrPercentage, Antigen = totalTestedAntigenPercentage },
                        LastByTypePrc = new PcrAntigenPrc { PCR = last24PcrPercentage, Antigen = last24AntigenPercentage },
                    },
                    ConfirmedPrc = new ConfirmedPrc
                    {
                        TotalPerTestedPrc = totalPerTestedPrcPercentage,
                        LastPerTestedPrc = last24PerTestedPrcPercentage,
                        TotalByTypePrc = new PcrAntigenPrc { PCR = totalConfirmedPcrPercentage, Antigen = totalConfirmedAntigenPercentage },
                        LastByTypePrc = new PcrAntigenPrc { PCR = last24ConfirmedPcrPercentage, Antigen = last24ConfirmedAntigenPercentage },
                    },
                    Active = new ActivePrc
                    {
                        HospotalizedPerActive = hospitalizedPerActive,
                        IcuPerHospitalized = icuPerHospitalized,
                    },
                },
            };

            if (medicalPrc != 0)
            {
                covidStatistics.Stats.ConfirmedPrc.MedicalPcr = medicalPrc;
            }

            var convertedRegions = Conversion.ConvertToRegionsServiceModel(covidStatistics.Regions);

            covidStatistics.ConditionResult =
                this.GetConditionResult(covidStatistics.Overall.Tested, covidStatistics.Overall.Confirmed, covidStatistics.Overall.Vaccinated, convertedRegions);

            return covidStatistics;
        }

        private BsonDocument GetConditionResult(Tested tested, Confirmed confirmed, Vaccinated vaccinated, IEnumerable<RegionsServiceModel> convertedRegions)
        {
            var condition = "approved";
            var sb = new StringBuilder();
            var conditionResult = new BsonDocument();

            if (tested.Total != tested.TotalByType.PCR + tested.TotalByType.Antigen)
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total tests must be equal to sum of antigen and  pcr total tests!");
                conditionResult.Add("tested/total", new BsonDocument
                {
                    { "expected", tested.Total },
                    { "actual", tested.TotalByType.PCR + tested.TotalByType.Antigen },
                });
            }

            if (tested.TotalByType24.PCR + tested.TotalByType24.Antigen != tested.Last24)
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total tests for last 24h must be equal to sum of antigen and pcr total tests for last 24h!");
                conditionResult.Add("tested/last", new BsonDocument
                {
                    { "expected", tested.Last24 },
                    { "actual", tested.TotalByType24.PCR + tested.TotalByType24.Antigen },
                });
            }

            if (confirmed.Total != confirmed.TotalByType.PCR + confirmed.TotalByType.Antigen)
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total confirmed tests is not equal to sum of total confirmed antigen and pcr tests!");
                conditionResult.Add("confirmed/total ", new BsonDocument
                {
                    { "expected", confirmed.Total },
                    { "actual", confirmed.TotalByType.PCR + confirmed.TotalByType.Antigen },
                });
            }

            if (confirmed.Last24 != confirmed.TotalByType24.PCR + confirmed.TotalByType24.Antigen)
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total confirmed tests for last 24h is not equal to sum of total confirmed antigen and pcr tests for last 24h!");
                conditionResult.Add("confirmed/last ", new BsonDocument
                {
                    { "expected", confirmed.Last24 },
                    { "actual", confirmed.TotalByType24.PCR + confirmed.TotalByType24.Antigen },
                });
            }

            if (vaccinated.Last != vaccinated.LastByType.AstraZeneca + vaccinated.LastByType.Comirnaty + vaccinated.LastByType.Moderna + vaccinated.LastByType.Janssen)
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total vaccinated for last 24h is not equal to sum of total vaccinated by vaccine type for last 24h!");
                conditionResult.Add("vaccinated/last ", new BsonDocument
                {
                    { "expected", vaccinated.Last },
                    { "actual", vaccinated.LastByType.AstraZeneca + vaccinated.LastByType.Comirnaty + vaccinated.LastByType.Moderna + vaccinated.LastByType.Janssen },
                });
            }

            if (confirmed.Total != convertedRegions.Sum(x => x.RegionStatistics.Confirmed.Total))
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total confirmed tests is not equal to sum of total confirmed tests for all regions!");
                conditionResult.Add("confirmed/total ", new BsonDocument
                {
                    { "expected", confirmed.Total },
                    { "actual", convertedRegions.Sum(x => x.RegionStatistics.Confirmed.Total) },
                });
            }

            if (confirmed.Last24 != convertedRegions.Sum(x => x.RegionStatistics.Confirmed.Last))
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total confirmed tests for last 24h is not equal to sum of total confirmed tests for all regions for last 24h!");
                conditionResult.Add("confirmed/last ", new BsonDocument
                {
                    { "expected", confirmed.Last24 },
                    { "actual", convertedRegions.Sum(x => x.RegionStatistics.Confirmed.Last) },
                });
            }

            if (vaccinated.Total != convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.Total))
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total vaccinated is not equal to sum of vaccinated for all regions!");
                conditionResult.Add("vaccinated/total ", new BsonDocument
                {
                    { "expected", vaccinated.Total },
                    { "actual", convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.Total) },
                });
            }

            if (vaccinated.Last != convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.Last))
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total vaccinated for last 24h is not equal to sum of vaccinated for all regions for last 24h!");
                conditionResult.Add("vaccinated/last ", new BsonDocument
                {
                    { "expected", vaccinated.Last },
                    { "actual", convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.Last) },
                });
            }

            if (vaccinated.Last != convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.LastByType.AstraZeneca) +
                convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.LastByType.Comirnaty) +
                convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.LastByType.Janssen) +
                convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.LastByType.Moderna))
            {
                condition = "discrepancy";
                sb.AppendLine($"Sum of total vaccinated for last 24h is not equal to sum of vaccinated for all regions by type for last 24h!");
                conditionResult.Add("vaccinated/last ", new BsonDocument
                {
                    { "expected", vaccinated.Last },
                    {
                        "actual", convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.LastByType.AstraZeneca) +
                         convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.LastByType.Comirnaty) +
                         convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.LastByType.Janssen) +
                         convertedRegions.Sum(x => x.RegionStatistics.Vaccinated.LastByType.Moderna)
                    },
                });
            }

            var result = new BsonDocument
            {
                { "condition", condition },
            };

            if (condition == "discrepancy")
            {
                result.Add("condition-description", sb.ToString());
                result.Add("tested-fields", conditionResult);
            }

            return result;
        }

        private BsonDocument GetAllRegionsData(IElement[] allTebles)
        {
            var regionsNames = new List<string>();
            var regionsStatistics = new List<BsonDocument>();
            var regionsStatisticsData
                = new Dictionary<string, BsonDocument>();

            var vaccinatedByRegions = allTebles[5]
                .QuerySelectorAll("td")
                .SkipLast(7)
                .Select(x => x.TextContent)
                .ToArray();
            var confirmedByRegionTableRecords = allTebles[3]
                .QuerySelectorAll("td")
                .SkipLast(3)
                .Select(x => x.TextContent)
                .ToArray();

            for (int i = 0; i < confirmedByRegionTableRecords.Length; i += 3)
            {
                var regionCode = Conversion.RegionЕКАТТЕCodeConversion(confirmedByRegionTableRecords[i]);
                var confirmed = this.IntParser(confirmedByRegionTableRecords[i + 1]);
                var confirmed24 = this.IntParser(confirmedByRegionTableRecords[i + 2]);

                regionsNames.Add(regionCode);
                regionsStatistics.Add(new BsonDocument
                {
                    {
                        "confirmed", new BsonDocument
                        {
                            { "total", confirmed }, { "last", confirmed24 },
                        }
                    },
                });
            }

            var counter = 0;
            for (int i = 0; i < vaccinatedByRegions.Length - 1; i += 8)
            {
                var totalVaccinated = this.IntParser(vaccinatedByRegions[i + 1]);
                var comirnaty = this.IntParser(vaccinatedByRegions[i + 2]);
                var moderna = this.IntParser(vaccinatedByRegions[i + 3]);
                var astrazeneca = this.IntParser(vaccinatedByRegions[i + 4]);
                var janssen = this.IntParser(vaccinatedByRegions[i + 5]);
                var totalVaccinedComplate = this.IntParser(vaccinatedByRegions[i + 6]);
                var totalVaccinated24 = comirnaty + moderna + astrazeneca + janssen;

                regionsStatistics[counter].Add("vaccinated", new BsonDocument
                {
                    { "total", totalVaccinated },
                    { "last", totalVaccinated24 },
                    {
                        "last_by_type", new BsonDocument
                        {
                            { "comirnaty", comirnaty },
                            { "moderna", moderna },
                            { "astrazeneca", astrazeneca },
                            { "janssen", janssen },
                        }
                    },
                    { "total_completed", totalVaccinedComplate },
                });
                counter++;
            }

            for (int i = 0; i < regionsNames.Count; i++)
            {
                regionsStatisticsData.Add(regionsNames[i], regionsStatistics[i]);
            }

            return regionsStatisticsData.ToBsonDocument();
        }

        private Medical GetMedicalStatistics(int totalMedical, string[] medicalTableRecords, Medical medicalPrevDay)
        {
            var totalDoctors = this.IntParser(medicalTableRecords[1]);
            var totalNurces = this.IntParser(medicalTableRecords[3]);
            var totalParamedics1 = this.IntParser(medicalTableRecords[5]);
            var totalParamedics2 = this.IntParser(medicalTableRecords[7]);
            var others = this.IntParser(medicalTableRecords[9]);

            if (medicalPrevDay == null)
            {
                return new Medical
                {
                    Total = totalMedical,
                    TotalByType = new MedicalTypes
                    {
                        Doctror = totalDoctors,
                        Nurces = totalNurces,
                        Paramedics_1 = totalParamedics1,
                        Paramedics_2 = totalParamedics2,
                        Others = others,
                    },
                    Last24 = 0,
                    LastByType24 = new MedicalTypes
                    {
                        Doctror = 0,
                        Nurces = 0,
                        Paramedics_1 = 0,
                        Paramedics_2 = 0,
                        Others = 0,
                    },
                };
            }

            return new Medical
            {
                Total = totalMedical,
                TotalByType = new MedicalTypes
                {
                    Doctror = totalDoctors,
                    Nurces = totalNurces,
                    Paramedics_1 = totalParamedics1,
                    Paramedics_2 = totalParamedics2,
                    Others = others,
                },
                Last24 = totalMedical - medicalPrevDay.Total,
                LastByType24 = new MedicalTypes
                {
                    Doctror = totalDoctors - medicalPrevDay.TotalByType.Doctror,
                    Nurces = totalNurces - medicalPrevDay.TotalByType.Nurces,
                    Paramedics_1 = totalParamedics1 - medicalPrevDay.TotalByType.Paramedics_1,
                    Paramedics_2 = totalParamedics2 - medicalPrevDay.TotalByType.Paramedics_2,
                    Others = others - medicalPrevDay.TotalByType.Others,
                },
            };
        }

        private int IntParser(string num)
        {
            if (num == "-")
            {
                return 0;
            }

            return int.Parse(num.Trim().Replace(" ", string.Empty));
        }

        private double DevideTwoIntiger(int num1, int num2)
        {
            return Math.Round((double)num1 / num2, 4);
        }
    }
}
